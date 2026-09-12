using Sievert.Modeling;

namespace Sievert.Tests;

/// <summary>
/// Goreli risk endeksinin testleri. Sozlesme: docs/urun/risk-sozlesmesi.md surum 1.0.
///
/// Endeks, skorun egitim dagilimindaki yuzdelik sirasi. <c>RawModelScore * 100</c> degil;
/// buradaki testlerin cogu tam olarak bu ikisinin karistirilmadigini siniyor.
/// </summary>
public sealed class RiskIndexTests
{
    private static ScoreDistribution Distribution(params double[] scores) =>
        ScoreDistribution.FromScores("test", "github.com/ornek/depo", scores);

    [Fact]
    public void AScoreBelowEveryTrainingScore_IsZero()
    {
        Assert.Equal(0.0, Distribution(0.2, 0.4, 0.6).RiskIndex(0.1));
    }

    [Fact]
    public void AScoreAboveEveryTrainingScore_IsHundred()
    {
        Assert.Equal(100.0, Distribution(0.2, 0.4, 0.6).RiskIndex(0.9));
    }

    [Fact]
    public void AScoreInTheMiddle_CountsWhatIsBelowIt()
    {
        // 4 skorun 2'si altinda, esit yok: 2/4 = %50.
        Assert.Equal(50.0, Distribution(0.1, 0.2, 0.8, 0.9).RiskIndex(0.5));
    }

    [Fact]
    public void EqualScores_ShareTheMiddleRank()
    {
        // 0,5'in altinda 1, esit 2: (1 + 0,5*2) / 4 = %50.
        Assert.Equal(50.0, Distribution(0.1, 0.5, 0.5, 0.9).RiskIndex(0.5));
    }

    [Fact]
    public void TheIndexIsNotTheScoreTimesHundred()
    {
        // Egitim dagilimi yukarida toplanmis: 0,5 dagilimin en altinda kaliyor.
        ScoreDistribution distribution = Distribution(0.90, 0.91, 0.92, 0.93);

        Assert.Equal(0.0, distribution.RiskIndex(0.5));
        Assert.NotEqual(50.0, distribution.RiskIndex(0.5));
    }

    [Fact]
    public void TheIndexNeverFallsWhenTheScoreRises()
    {
        ScoreDistribution distribution = Distribution(0.05, 0.11, 0.11, 0.30, 0.72, 0.98);

        double previous = -1.0;

        for (int step = 0; step <= 1000; step++)
        {
            double index = distribution.RiskIndex(step / 1000.0);

            Assert.True(index >= previous, $"skor {step / 1000.0} icinde endeks dustu: {previous} -> {index}");
            Assert.InRange(index, 0.0, 100.0);

            previous = index;
        }
    }

    [Fact]
    public void TheIndexIsRoundedToOneDecimal()
    {
        // 3 skorun 1'i altinda: 1/3 = %33,333... -> 33,3.
        Assert.Equal(33.3, Distribution(0.1, 0.5, 0.6).RiskIndex(0.4));
    }

    [Fact]
    public void AnEmptyDistribution_IsRefusedInsteadOfDividingByZero()
    {
        Assert.Throws<ArgumentException>(() => Distribution());
    }

    [Fact]
    public void TheStoredScoresAreSortedWhateverOrderTheyArriveIn()
    {
        ScoreDistribution distribution = Distribution(0.9, 0.1, 0.5);

        Assert.Equal([0.1, 0.5, 0.9], distribution.SortedScores);
        Assert.Equal(0.1, distribution.Minimum);
        Assert.Equal(0.9, distribution.Maximum);
        Assert.Equal(3, distribution.TrainCount);
    }
}
