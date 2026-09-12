using Sievert.Modeling;

namespace Sievert.Tests;

/// <summary>
/// Ham olasilik kalitesi: Brier ve ECE. Sayilar elle hesaplandi ve burada sabit duruyor.
/// </summary>
public class CalibrationTests
{
    [Fact]
    public void Brier_MatchesTheHandComputedValue()
    {
        // (0,9-1)^2 + (0,2-0)^2 + (0,6-1)^2 + (0,1-0)^2 = 0,01 + 0,04 + 0,16 + 0,01 = 0,22
        // 0,22 / 4 = 0,055
        double brier = Calibration.Brier(
        [
            new(0.9, true),
            new(0.2, false),
            new(0.6, true),
            new(0.1, false),
        ]);

        Assert.Equal(0.055, brier, 12);
    }

    [Fact]
    public void Brier_IsZeroForPerfectPredictions()
    {
        Assert.Equal(0.0, Calibration.Brier([new(1.0, true), new(0.0, false)]), 12);
    }

    [Fact]
    public void Brier_IsOneForPerfectlyWrongPredictions()
    {
        Assert.Equal(1.0, Calibration.Brier([new(0.0, true), new(1.0, false)]), 12);
    }

    [Theory]
    [InlineData(0.0, 0)]
    [InlineData(0.05, 0)]
    [InlineData(0.1, 1)]
    [InlineData(0.55, 5)]
    [InlineData(0.9, 9)]
    [InlineData(0.999, 9)]
    [InlineData(1.0, 9)]
    public void Bins_FollowTheDeclaredBoundaries(double probability, int expected)
    {
        Assert.Equal(expected, Calibration.BinOf(probability));
    }

    [Fact]
    public void Bins_AreAlwaysTenEvenWhenMostAreEmpty()
    {
        CalibrationResult result = Calibration.Measure([new(0.05, false), new(0.05, true)]);

        Assert.Equal(10, result.Bins.Count);
        Assert.Equal(2, result.Bins[0].Count);
        Assert.All(result.Bins.Skip(1), bin => Assert.Equal(0, bin.Count));
    }

    [Fact]
    public void Bins_EmptyOnesCarryNoWeightAndNoInventedAverage()
    {
        CalibrationResult result = Calibration.Measure([new(0.05, false), new(0.05, true)]);

        Assert.Null(result.Bins[5].ObservedRate);
        Assert.Null(result.Bins[5].Gap);

        // Tek dolu kutu: ortalama tahmin 0,05, gozlenen oran 0,5 -> ECE = 1,0 * 0,45
        Assert.Equal(0.45, result.Ece, 12);
    }

    [Fact]
    public void Ece_MatchesTheHandComputedValue()
    {
        // Kutu 0 [0,0-0,1): 0,02 ve 0,04 -> ortalama 0,03, pozitif 0 / 2, fark 0,03
        // Kutu 8 [0,8-0,9): 0,85 -> ortalama 0,85, pozitif 1 / 1, fark 0,15
        // ECE = (2/3)*0,03 + (1/3)*0,15 = 0,02 + 0,05 = 0,07
        CalibrationResult result = Calibration.Measure(
        [
            new(0.02, false),
            new(0.04, false),
            new(0.85, true),
        ]);

        Assert.Equal(0.07, result.Ece, 12);
        Assert.Equal(3, result.Count);
    }

    [Fact]
    public void Ece_IsZeroWhenEveryBinMatchesItsObservedRate()
    {
        // Kutu 0: dort satir, hepsi 0,0 tahmin, hicbiri pozitif -> fark 0.
        CalibrationResult result = Calibration.Measure(
        [
            new(0.0, false),
            new(0.0, false),
            new(0.0, false),
            new(0.0, false),
        ]);

        Assert.Equal(0.0, result.Ece, 12);
        Assert.Equal(0.0, result.Brier, 12);
    }

    [Fact]
    public void MicroAndMacro_AreNotTheSameNumber()
    {
        // Kucuk kume iyi kalibre, buyuk kume bozuk.
        ScoredProbability[] small = [new(0.5, true), new(0.5, false)];
        ScoredProbability[] large = [.. Enumerable.Range(0, 100).Select(index => new ScoredProbability(0.9, false))];

        double micro = Calibration.Brier([.. small, .. large]);
        double macro = (Calibration.Brier(small) + Calibration.Brier(large)) / 2;

        Assert.NotEqual(macro, micro, 6);
        Assert.True(micro > macro, "mikro buyuk kumeye agirlik veriyor");
    }

    [Fact]
    public void Ece_MicroAndMacroDifferToo()
    {
        ScoredProbability[] small = [new(0.1, true)];
        ScoredProbability[] large = [.. Enumerable.Range(0, 50).Select(index => new ScoredProbability(0.1, false))];

        double micro = Calibration.Measure([.. small, .. large]).Ece;
        double macro = (Calibration.Measure(small).Ece + Calibration.Measure(large).Ece) / 2;

        Assert.NotEqual(macro, micro, 6);
    }
}
