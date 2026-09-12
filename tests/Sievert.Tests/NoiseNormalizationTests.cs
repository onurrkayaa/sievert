using Sievert.Modeling;

namespace Sievert.Tests;

/// <summary>
/// Sentetik gurultu sonuclarini taban oranindan ayiran olculer. Sozlesme
/// <c>asama5-gurultu-normalizasyon-sozlesmesi.md</c> surum 1.0.
/// </summary>
public class NoiseNormalizationTests
{
    [Fact]
    public void Lift_IsPrAucMinusThePositiveRate()
    {
        Assert.Equal(0.3, NoiseNormalization.Lift(0.5, 0.2)!.Value, 12);
        Assert.Equal(0.0, NoiseNormalization.Lift(0.2, 0.2)!.Value, 12);
        Assert.Equal(-0.1, NoiseNormalization.Lift(0.1, 0.2)!.Value, 12);
    }

    [Fact]
    public void Lift_IsNotAvailableWithoutPrAuc()
    {
        Assert.Null(NoiseNormalization.Lift(null, 0.2));
    }

    [Fact]
    public void Normalised_ScalesTheLiftByTheRemainingRoom()
    {
        // (0,5 - 0,2) / (1 - 0,2) = 0,375
        Assert.Equal(0.375, NoiseNormalization.Normalised(0.5, 0.2)!.Value, 12);

        // Mukemmel siralama: (1 - 0,2) / (1 - 0,2) = 1
        Assert.Equal(1.0, NoiseNormalization.Normalised(1.0, 0.2)!.Value, 12);
    }

    [Fact]
    public void Normalised_IsNotAvailableWhenEveryRowIsPositive()
    {
        Assert.Null(NoiseNormalization.Normalised(1.0, 1.0));
    }

    [Fact]
    public void Climatology_UsesTheTrainRateForEveryRow()
    {
        // Iki satir, biri pozitif; taban orani 0,5 -> (0,5-1)^2 + (0,5-0)^2 = 0,5, / 2 = 0,25
        double climatology = NoiseNormalization.Climatology(
            [new(0.9, true), new(0.1, false)],
            trainPositiveRate: 0.5);

        Assert.Equal(0.25, climatology, 12);
    }

    [Fact]
    public void Climatology_IsZeroWhenTheRateMatchesPerfectly()
    {
        Assert.Equal(0.0, NoiseNormalization.Climatology([new(0.4, false), new(0.6, false)], 0.0), 12);
    }

    [Fact]
    public void SkillScore_IsPositiveWhenTheModelBeatsClimatology()
    {
        Assert.Equal(0.5, NoiseNormalization.SkillScore(0.1, 0.2)!.Value, 12);
        Assert.Equal(0.0, NoiseNormalization.SkillScore(0.2, 0.2)!.Value, 12);
        Assert.Equal(-1.0, NoiseNormalization.SkillScore(0.4, 0.2)!.Value, 12);
    }

    [Fact]
    public void SkillScore_IsNotAvailableWhenClimatologyIsZero()
    {
        Assert.Null(NoiseNormalization.SkillScore(0.1, 0.0));
    }

    [Fact]
    public void LinesAdded_ChoosesItsThresholdOnlyFromTheTrainLabels()
    {
        IReadOnlyList<SnapshotRow> train =
        [
            Row("t1", linesAdded: 100, positive: true),
            Row("t2", linesAdded: 10, positive: false),
            Row("t3", linesAdded: 5, positive: false),
        ];

        double before = NoiseNormalization.LinesAddedFor(train, train).Threshold;

        // Test tarafi tamamen ters etiketlendi; esik degismemeli.
        IReadOnlyList<SnapshotRow> flipped =
        [
            Row("s1", linesAdded: 100, positive: false),
            Row("s2", linesAdded: 5, positive: true),
        ];

        Assert.Equal(before, NoiseNormalization.LinesAddedFor(train, flipped).Threshold, 12);
    }

    [Fact]
    public void LinesAdded_UsesTheBinaryScoreForBrierNotTheRawValue()
    {
        IReadOnlyList<SnapshotRow> train =
        [
            Row("t1", linesAdded: 100, positive: true),
            Row("t2", linesAdded: 10, positive: false),
        ];

        (double threshold, Confusion counts, double? area, double brier) =
            NoiseNormalization.LinesAddedFor(train, train);

        // Ham LinesAdded olasilik gibi kullanilsaydi Brier 1'in cok uzerine cikardi.
        Assert.InRange(brier, 0.0, 1.0);
        Assert.Equal(100, threshold);
        Assert.Equal(1, counts.TruePositives);
        Assert.NotNull(area);
    }

    [Fact]
    public void RandomBaseline_IsDeterministicForTheSameSeed()
    {
        IReadOnlyList<SnapshotRow> test = [Row("a", positive: true), Row("b", positive: false)];

        Assert.Equal(
            NoiseNormalization.RandomBaselineFor(test, 0.5, new Random(20260912)).Select(row => row.Score),
            NoiseNormalization.RandomBaselineFor(test, 0.5, new Random(20260912)).Select(row => row.Score));
    }

    [Fact]
    public void PositiveRate_CountsTheLabelledRows()
    {
        Assert.Equal(0.5, NoiseNormalization.PositiveRate([Row("a", positive: true), Row("b", positive: false)]), 12);
    }

    private static SnapshotRow Row(string sha, int linesAdded = 10, bool positive = false) =>
        new("klasor", "a/b", sha, DateTimeOffset.UnixEpoch, linesAdded, 1, 1, 1, 0.5, 1, 1, 1, 1, 1, 1, 1, 1, 1,
            false, positive, positive ? "szz" : null, false);
}
