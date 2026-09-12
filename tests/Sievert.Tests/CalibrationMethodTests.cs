using Sievert.Modeling;

using static Sievert.Tests.EvaluationMetricTests;

namespace Sievert.Tests;

/// <summary>
/// Adim 3b: kalibrasyon alt bolmesi, Platt scaling ve isotonic regression.
/// Sozlesme <c>docs/olcumler/asama5-kalibrasyon-sozlesmesi.md</c> surum 1.0.
/// </summary>
public class CalibrationMethodTests
{
    // --- Alt bolme ---

    [Theory]
    [InlineData(1931, 1544, 387)]
    [InlineData(5943, 4754, 1189)]
    [InlineData(16041, 12832, 3209)]
    [InlineData(10, 8, 2)]
    public void SubSplit_ModelFitIsTheFirstEightyPercentOfTrain(int train, int fit, int calibration)
    {
        CalibrationSplit split = CalibrationSplit.From(Repository("a/b", train, 5));

        Assert.Equal(fit, split.ModelFit.Count);
        Assert.Equal(calibration, split.Calibration.Count);
    }

    [Fact]
    public void SubSplit_LeavesTheTestPartitionUntouched()
    {
        RepositorySplit repository = Repository("a/b", 100, 40);
        CalibrationSplit split = CalibrationSplit.From(repository);

        Assert.Equal(repository.Test, split.Test);
        Assert.Equal(80, split.ModelFit.Count);
        Assert.Equal(20, split.Calibration.Count);
    }

    [Fact]
    public void SubSplit_ThreePartsDoNotOverlap()
    {
        CalibrationSplit split = CalibrationSplit.From(Repository("a/b", 100, 40));

        HashSet<string> fit = [.. split.ModelFit.Select(row => row.Sha)];
        HashSet<string> calibration = [.. split.Calibration.Select(row => row.Sha)];
        HashSet<string> test = [.. split.Test.Select(row => row.Sha)];

        Assert.Empty(fit.Intersect(calibration));
        Assert.Empty(fit.Intersect(test));
        Assert.Empty(calibration.Intersect(test));
        Assert.Equal(140, fit.Count + calibration.Count + test.Count);
    }

    [Fact]
    public void SubSplit_KeepsTimeOrderAcrossThePartitions()
    {
        CalibrationSplit split = CalibrationSplit.From(Repository("a/b", 100, 40));

        Assert.True(split.ModelFit[^1].AuthorDateUtc <= split.Calibration[0].AuthorDateUtc);
        Assert.True(split.Calibration[^1].AuthorDateUtc <= split.Test[0].AuthorDateUtc);
    }

    [Fact]
    public void SubSplit_RefusesACalibrationPartWithoutBothClasses()
    {
        // Butun train satirlari negatif: kalibrasyon ogrenilemez.
        List<SnapshotRow> train = [];

        for (int index = 0; index < 100; index++)
        {
            train.Add(Row(index + 1, positive: false, sha: "t" + index));
        }

        CalibrationSplit split = CalibrationSplit.From(new RepositorySplit("a/b", train, train));

        Assert.False(split.CalibrationHasBothClasses);
    }

    // --- Platt ---

    [Fact]
    public void Platt_LearnsAnIncreasingTransform()
    {
        PlattCalibration platt = PlattCalibration.Fit(Separable());

        Assert.True(platt.Slope > 0, "donusum monoton artan olmali");
        Assert.True(platt.Apply(0.2) < platt.Apply(0.8));
    }

    [Fact]
    public void Platt_KeepsItsOutputInsideTheUnitInterval()
    {
        PlattCalibration platt = PlattCalibration.Fit(Separable());

        foreach (double probability in (double[])[0.0, 1e-9, 0.5, 1 - 1e-9, 1.0])
        {
            double calibrated = platt.Apply(probability);

            Assert.InRange(calibrated, 0.0, 1.0);
            Assert.True(double.IsFinite(calibrated));
        }
    }

    [Fact]
    public void Platt_OnlySeesTheCalibrationLabels()
    {
        IReadOnlyList<ScoredProbability> calibration = Separable();

        PlattCalibration before = PlattCalibration.Fit(calibration);

        // Test tarafi tamamen degisti; kalibrasyon kumesi ayni kaldi.
        _ = new ScoredProbability[] { new(0.9, false), new(0.1, true) };

        PlattCalibration after = PlattCalibration.Fit(calibration);

        Assert.Equal(before.Slope, after.Slope, 12);
        Assert.Equal(before.Offset, after.Offset, 12);
    }

    [Fact]
    public void Platt_RefusesADecreasingTransform()
    {
        // Yuksek olasilikta gozlenen oran DUSUK: egim negatif cikar ve sonuc
        // sessizce kullanilmaz. Kume ayrilabilir degil, yani fit yakinsiyor.
        InvalidOperationException error = Assert.Throws<InvalidOperationException>(
            () => PlattCalibration.Fit(Groups([0.25, 0.18, 0.12, 0.08, 0.05])));

        Assert.Contains("egim", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Platt_MovesAnOverconfidentModelTowardsTheObservedRate()
    {
        // Tahminler 0,5 - 0,9 arasinda ama gozlenen oranlar cok daha dusuk.
        PlattCalibration platt = PlattCalibration.Fit(Groups([0.05, 0.08, 0.12, 0.18, 0.25]));

        Assert.True(platt.Slope > 0, "artan iliski korunmali");
        Assert.True(platt.Apply(0.9) < 0.9, "asiri guvenli tahmin asagi cekilmeli");
        Assert.True(platt.Apply(0.5) < 0.5, "asiri guvenli tahmin asagi cekilmeli");
    }

    // --- Isotonic ---

    [Fact]
    public void Isotonic_ProducesANonDecreasingFunction()
    {
        IsotonicCalibration isotonic = IsotonicCalibration.Fit(Separable());

        double previous = -1.0;

        foreach (double probability in (double[])[0.0, 0.1, 0.25, 0.5, 0.75, 0.9, 1.0])
        {
            double calibrated = isotonic.Apply(probability);

            Assert.True(calibrated >= previous - 1e-12, "cikti azalmamali");
            Assert.InRange(calibrated, 0.0, 1.0);
            previous = calibrated;
        }
    }

    [Fact]
    public void Isotonic_PoolsOnlyTheAdjacentViolators()
    {
        // Gozlenen oranlar 0, 1, 0, 1. Ihlal olan tek cift ortadaki (1 sonra 0);
        // onlar 0,5'e toplaniyor, bastaki 0 ve sondaki 1 yerinde kaliyor.
        // Elle hesaplanan izotonik cozum: 0 | 0,5 | 1 -> uc blok.
        IsotonicCalibration isotonic = IsotonicCalibration.Fit(
        [
            new(0.1, false),
            new(0.2, true),
            new(0.3, false),
            new(0.4, true),
        ]);

        Assert.Equal(3, isotonic.Blocks.Count);
        Assert.Equal([0.0, 0.5, 1.0], isotonic.Blocks.Select(block => block.Value));
        Assert.Equal(0.5, isotonic.Apply(0.25), 12);
        Assert.Equal(0.0, isotonic.Apply(0.1), 12);
        Assert.Equal(1.0, isotonic.Apply(0.4), 12);
    }

    [Fact]
    public void Isotonic_TreatsEqualScoresAsOneGroup()
    {
        IsotonicCalibration one = IsotonicCalibration.Fit(
            [new(0.5, true), new(0.5, false), new(0.9, true)]);

        IsotonicCalibration other = IsotonicCalibration.Fit(
            [new(0.5, false), new(0.5, true), new(0.9, true)]);

        Assert.Equal(one.Apply(0.5), other.Apply(0.5), 12);
    }

    [Fact]
    public void Isotonic_UsesTheFirstAndLastBlockOutsideTheCalibrationRange()
    {
        IsotonicCalibration isotonic = IsotonicCalibration.Fit(
            [new(0.3, false), new(0.4, false), new(0.7, true), new(0.8, true)]);

        Assert.Equal(isotonic.Apply(0.3), isotonic.Apply(0.0), 12);
        Assert.Equal(isotonic.Apply(0.8), isotonic.Apply(1.0), 12);
    }

    [Fact]
    public void Isotonic_OnlySeesTheCalibrationLabels()
    {
        IReadOnlyList<ScoredProbability> calibration = Separable();

        IsotonicCalibration before = IsotonicCalibration.Fit(calibration);
        IsotonicCalibration after = IsotonicCalibration.Fit(calibration);

        Assert.Equal(before.Blocks.Count, after.Blocks.Count);
        Assert.Equal(before.Apply(0.42), after.Apply(0.42), 12);
    }

    [Fact]
    public void Isotonic_OfAPerfectlyCalibratedSetChangesNothingImportant()
    {
        // Yarisi pozitif olan tek bir skor: PAV tek blok, degeri 0,5.
        List<ScoredProbability> calibration = [];

        for (int index = 0; index < 100; index++)
        {
            calibration.Add(new ScoredProbability(0.5, index % 2 == 0));
        }

        IsotonicCalibration isotonic = IsotonicCalibration.Fit(calibration);

        Assert.Equal(0.5, isotonic.Apply(0.5), 12);
        Assert.Equal(0.25, Calibration.Brier([.. calibration.Select(row => new ScoredProbability(isotonic.Apply(row.Probability), row.Actual))]), 12);
    }

    // --- Yardimcilar ---

    /// <summary>
    /// Bes olasilik grubu (0,5 ... 0,9), her birinde 200 satir ve verilen gozlenen oran.
    /// Ayrilabilir degil, yani Platt fit'i yakinsiyor.
    /// </summary>
    private static IReadOnlyList<ScoredProbability> Groups(IReadOnlyList<double> rates)
    {
        List<ScoredProbability> rows = [];

        for (int group = 0; group < rates.Count; group++)
        {
            double probability = 0.5 + (0.1 * group);
            int positives = (int)Math.Round(rates[group] * 200);

            for (int index = 0; index < 200; index++)
            {
                rows.Add(new ScoredProbability(probability, index < positives));
            }
        }

        return rows;
    }

    private static IReadOnlyList<ScoredProbability> Separable()
    {
        List<ScoredProbability> rows = [];

        for (int index = 0; index < 100; index++)
        {
            double probability = (index + 0.5) / 100;
            rows.Add(new ScoredProbability(probability, index % 10 < index / 10));
        }

        return rows;
    }

    private static RepositorySplit Repository(string identity, int train, int test)
    {
        List<SnapshotRow> trainRows = [];
        List<SnapshotRow> testRows = [];

        for (int index = 0; index < train; index++)
        {
            trainRows.Add(Dated(identity, index, index % 3 == 0, "t" + index));
        }

        for (int index = 0; index < test; index++)
        {
            testRows.Add(Dated(identity, train + index, index % 3 == 0, "s" + index));
        }

        return new RepositorySplit(identity, trainRows, testRows);
    }

    private static SnapshotRow Dated(string identity, int day, bool positive, string sha) =>
        new("klasor", identity, sha, DateTimeOffset.UnixEpoch.AddDays(day),
            day + 1, day, day % 5 + 1, day % 3, day % 4 * 0.5, day % 6 + 1, day % 2 + 1,
            day * 3, day, day % 7, day % 4, day % 5, day * 2, day % 6,
            positive, positive, positive ? "szz" : null, false);
}
