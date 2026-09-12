using Sievert.Modeling;

using static Sievert.Tests.EvaluationMetricTests;

namespace Sievert.Tests;

/// <summary>
/// Tek oznitelikli <c>LinesAdded</c> esiginin testleri. Kural:
/// <c>LinesAdded &gt;= esik</c> ise pozitif; esik yalnizca egitim bolumunden seciliyor.
/// </summary>
public class ThresholdBaselineTests
{
    // --- Esik yalnizca train'den ---

    [Fact]
    public void Threshold_CandidatesComeOnlyFromTrainValues()
    {
        // Testte cok daha buyuk bir deger var ve etiketi pozitif; aday olmamali.
        RepositorySplit split = new(
            "a/b",
            [Row(10, positive: true, sha: "t1"), Row(5, positive: false, sha: "t2")],
            [Row(1000, positive: true, sha: "s1")]);

        ThresholdChoice choice = LinesAddedBaseline.Choose(split.Train);

        Assert.Contains(choice.Threshold, new double[] { 10, 5 });
        Assert.Equal(2, choice.CandidateCount);
    }

    [Fact]
    public void Threshold_DoesNotMoveWhenTestLabelsAreFlipped()
    {
        IReadOnlyList<SnapshotRow> train =
        [
            Row(40, positive: true, sha: "a"),
            Row(30, positive: false, sha: "b"),
            Row(10, positive: false, sha: "c"),
        ];

        double before = LinesAddedBaseline.Choose(train).Threshold;

        // Test satirlari degisti, egitim ayni: secilen esik ayni kalmali.
        _ = new[] { Row(40, positive: false, sha: "x"), Row(10, positive: true, sha: "y") };

        double after = LinesAddedBaseline.Choose(train).Threshold;

        Assert.Equal(before, after);
    }

    // --- Esitlik kurallari ---

    [Fact]
    public void Threshold_OnEqualF1ThePrecisionOfTheCandidateDecides()
    {
        // Elle hesaplandi: esik 40 ve 30'da F1 = 0,5; precision 1,0'a karsi 0,4.
        IReadOnlyList<SnapshotRow> train = TieTrain();

        ThresholdChoice choice = LinesAddedBaseline.Choose(train);

        Assert.Equal(40, choice.Threshold);
        Assert.Equal(0.5, choice.Counts.F1!.Value, 12);
        Assert.Equal(1.0, choice.Counts.Precision!.Value, 12);
    }

    [Fact]
    public void Threshold_TheTieBreakRuleItselfPrefersHigherPrecision()
    {
        // Kuralin kendisi: ayni F1, farkli precision -> yuksek precision.
        ThresholdCandidate low = new(10, new Confusion(2, 3, 1, 4));
        ThresholdCandidate high = new(40, new Confusion(1, 0, 2, 7));

        Assert.Equal(high, ThresholdSelection.Pick([low, high]));
        Assert.Equal(high, ThresholdSelection.Pick([high, low]));
    }

    [Fact]
    public void Threshold_WhenF1AndPrecisionAreBothEqualTheHigherThresholdWins()
    {
        ThresholdCandidate low = new(10, new Confusion(1, 1, 1, 5));
        ThresholdCandidate high = new(40, new Confusion(1, 1, 1, 5));

        Assert.Equal(high, ThresholdSelection.Pick([low, high]));
        Assert.Equal(high, ThresholdSelection.Pick([high, low]));
    }

    [Fact]
    public void Threshold_PickingIsNotAffectedByTheOrderOfCandidates()
    {
        ThresholdCandidate[] candidates =
        [
            new(10, new Confusion(3, 9, 0, 0)),
            new(30, new Confusion(2, 3, 1, 6)),
            new(40, new Confusion(1, 0, 2, 9)),
        ];

        ThresholdCandidate forwards = ThresholdSelection.Pick(candidates);
        ThresholdCandidate backwards = ThresholdSelection.Pick([.. candidates.Reverse()]);

        Assert.Equal(forwards, backwards);
    }

    // --- Uygulama ---

    [Fact]
    public void Applying_UsesGreaterThanOrEqual()
    {
        ThresholdOutcome outcome = LinesAddedBaseline.Apply(
            [Row(10, positive: true, sha: "a"), Row(9, positive: true, sha: "b")],
            threshold: 10);

        Assert.Equal(1, outcome.Counts.TruePositives);
        Assert.Equal(1, outcome.Counts.FalseNegatives);
    }

    [Fact]
    public void Applying_ReportsTheRawAndTheBinaryAreaSeparately()
    {
        IReadOnlyList<SnapshotRow> rows =
        [
            Row(100, positive: true, sha: "a"),
            Row(50, positive: false, sha: "b"),
            Row(10, positive: true, sha: "c"),
            Row(1, positive: false, sha: "d"),
        ];

        ThresholdOutcome outcome = LinesAddedBaseline.Apply(rows, threshold: 100);

        // Ham skor siralamasi: 100(+), 50(-), 10(+), 1(-) -> sozlesmedeki ornegin aynisi.
        Assert.Equal(19.0 / 24.0, outcome.RawPrAuc!.Value, 12);

        // Esik sonrasi 0/1: tek satir 1, digerleri 0 -> iki grup.
        Assert.NotEqual(outcome.RawPrAuc!.Value, outcome.BinaryPrAuc!.Value);
    }

    [Fact]
    public void Applying_RefusesAForbiddenField()
    {
        Assert.Throws<InvalidOperationException>(
            () => ModelFeatures.Value(Row(10), "IsBugIntroducing"));
    }

    /// <summary>
    /// Esik 40: TP 1, FP 0, FN 2 -> F1 0,5. Esik 30: TP 2, FP 3, FN 1 -> F1 0,5.
    /// Esik 10: TP 3, FP 9 -> F1 0,4.
    /// </summary>
    private static IReadOnlyList<SnapshotRow> TieTrain()
    {
        List<SnapshotRow> train = [Row(40, positive: true, sha: "a0"), Row(30, positive: true, sha: "b0")];

        for (int index = 0; index < 3; index++)
        {
            train.Add(Row(30, positive: false, sha: "b" + (index + 1)));
        }

        train.Add(Row(10, positive: true, sha: "c0"));

        for (int index = 0; index < 6; index++)
        {
            train.Add(Row(10, positive: false, sha: "c" + (index + 1)));
        }

        return train;
    }
}
