using Sievert.Modeling;

using static Sievert.Tests.EvaluationMetricTests;

namespace Sievert.Tests;

/// <summary>
/// Uc taban cizgisinin testleri. Hepsi kucuk, elle kurulmus bolmelerle calisiyor;
/// dondurulmus veri kumesine dokunmuyorlar.
/// </summary>
public class BaselineTests
{
    // --- Her seye negatif ---

    [Fact]
    public void Negative_PredictsNothingAndScoresEveryRowTheSame()
    {
        IReadOnlyList<Scored> scored = NegativeBaseline.Apply(Rows(positives: 1, negatives: 3));

        Assert.All(scored, row => Assert.False(row.Predicted));
        Assert.Single(scored.Select(row => row.Score).Distinct());
    }

    [Fact]
    public void Negative_HasNoPrecisionAndZeroRecall()
    {
        Outcome outcome = Evaluation.Of(NegativeBaseline.Apply(Rows(positives: 1, negatives: 3)));

        Assert.Equal(0, outcome.Counts.TruePositives);
        Assert.Equal(0, outcome.Counts.FalsePositives);
        Assert.Equal(1, outcome.Counts.FalseNegatives);
        Assert.Equal(3, outcome.Counts.TrueNegatives);
        Assert.Null(outcome.Counts.Precision);
        Assert.Equal(0.0, outcome.Counts.Recall);
        Assert.Equal(0.0, outcome.Counts.F1);
    }

    [Fact]
    public void Negative_ScoresAsAFlatCurveWorthThePositiveRate()
    {
        // Sozlesmede yazili sonuc: sabit skorda alan = P / N, siralama yetenegi yok.
        Outcome outcome = Evaluation.Of(NegativeBaseline.Apply(Rows(positives: 1, negatives: 3)));

        Assert.Equal(0.25, outcome.PrAuc!.Value, 12);
    }

    // --- Oranla rastgele ---

    [Fact]
    public void Random_UsesTheTrainPositiveRateAsItsProbability()
    {
        RepositorySplit split = Split("a/b", trainPositives: 1, trainNegatives: 3, testPositives: 4, testNegatives: 0);

        Assert.Equal(0.25, split.TrainPositiveRate);
    }

    [Fact]
    public void Random_ProbabilityDoesNotMoveWhenTestLabelsChange()
    {
        RepositorySplit before = Split("a/b", trainPositives: 1, trainNegatives: 3, testPositives: 0, testNegatives: 4);
        RepositorySplit after = Split("a/b", trainPositives: 1, trainNegatives: 3, testPositives: 4, testNegatives: 0);

        Assert.Equal(before.TrainPositiveRate, after.TrainPositiveRate);
    }

    [Fact]
    public void Random_TheSameSeedGivesTheSameNumbers()
    {
        RepositorySplit[] repositories = [Split("a/b", 3, 7, 3, 7), Split("c/d", 2, 8, 2, 8)];

        RandomBaselineResult first = RandomBaseline.Run(repositories, seed: 20260912, repeats: 25);
        RandomBaselineResult second = RandomBaseline.Run(repositories, seed: 20260912, repeats: 25);

        Assert.Equal(first.Micro.F1.Values, second.Micro.F1.Values);
        Assert.Equal(first.Repositories[0].PredictedPositives.Values, second.Repositories[0].PredictedPositives.Values);
    }

    [Fact]
    public void Random_ADifferentSeedChangesAtLeastOnePrediction()
    {
        RepositorySplit[] repositories = [Split("a/b", 3, 7, 3, 7)];

        RandomBaselineResult first = RandomBaseline.Run(repositories, seed: 20260912, repeats: 25);
        RandomBaselineResult second = RandomBaseline.Run(repositories, seed: 20260913, repeats: 25);

        Assert.NotEqual(
            first.Repositories[0].PredictedPositives.Values,
            second.Repositories[0].PredictedPositives.Values);
    }

    [Fact]
    public void Random_EachRepositoryKeepsItsOwnProbability()
    {
        // Bir repoda p = 1, digerinde p = 0. Oranlar birlestirilip tek bir p
        // uretilseydi ikisi de ortada bir yere duserdi.
        RepositorySplit[] repositories =
        [
            Split("a/hepsi", trainPositives: 10, trainNegatives: 0, testPositives: 5, testNegatives: 5),
            Split("b/hicbiri", trainPositives: 0, trainNegatives: 10, testPositives: 5, testNegatives: 5),
        ];

        RandomBaselineResult result = RandomBaseline.Run(repositories, seed: 20260912, repeats: 10);

        Assert.All(result.Repositories[0].PredictedPositives.Values, count => Assert.Equal(10, count));
        Assert.All(result.Repositories[1].PredictedPositives.Values, count => Assert.Equal(0, count));
    }

    [Fact]
    public void Random_MicroCombinesEveryRepositoryInOneRepeat()
    {
        RepositorySplit[] repositories =
        [
            Split("a/hepsi", trainPositives: 10, trainNegatives: 0, testPositives: 5, testNegatives: 5),
            Split("b/hicbiri", trainPositives: 0, trainNegatives: 10, testPositives: 5, testNegatives: 5),
        ];

        RandomBaselineResult result = RandomBaseline.Run(repositories, seed: 20260912, repeats: 10);

        // 10 pozitif tahmin birinci repodan, 0 ikinciden.
        Assert.All(result.Micro.PredictedPositives.Values, count => Assert.Equal(10, count));
    }

    // --- Dagilim ozeti ---

    [Fact]
    public void Distribution_ReportsMeanMedianAndTheTwoTails()
    {
        Distribution spread = Distribution.Of([1.0, 2.0, 3.0, 4.0, 5.0]);

        Assert.Equal(3.0, spread.Mean, 12);
        Assert.Equal(3.0, spread.Median);
        Assert.Equal(1.0, spread.Low);
        Assert.Equal(5.0, spread.High);
        Assert.Equal(0, spread.NotAvailable);
    }

    [Fact]
    public void Distribution_CountsTheRepeatsWhereAValueWasNotAvailable()
    {
        Distribution spread = Distribution.Of([1.0, null, 3.0]);

        Assert.Equal(1, spread.NotAvailable);
        Assert.Equal(2.0, spread.Mean, 12);
    }

    // --- Yardimcilar ---

    internal static IReadOnlyList<SnapshotRow> Rows(int positives, int negatives)
    {
        List<SnapshotRow> rows = [];

        for (int index = 0; index < positives; index++)
        {
            rows.Add(Row(1, positive: true, sha: "p" + index));
        }

        for (int index = 0; index < negatives; index++)
        {
            rows.Add(Row(1, positive: false, sha: "n" + index));
        }

        return rows;
    }

    internal static RepositorySplit Split(
        string identity,
        int trainPositives,
        int trainNegatives,
        int testPositives,
        int testNegatives) =>
        new(
            identity,
            [.. Rows(trainPositives, trainNegatives).Select(row => row with { RepositoryIdentity = identity })],
            [.. Rows(testPositives, testNegatives).Select(row => row with { RepositoryIdentity = identity })]);
}
