using Sievert.Modeling;

using static Sievert.Tests.EvaluationMetricTests;

namespace Sievert.Tests;

/// <summary>
/// Adim 2b: rastgele tabanin makro F1 dagilimi ve <c>&gt;</c> / <c>&gt;=</c> protokol
/// farkinin kontrolu. Ikisi de mevcut sonuclari degistirmiyor, uzerine ekliyor.
/// </summary>
public class MacroAndOperatorTests
{
    // --- Rastgele tabanin makro F1'i ---

    [Fact]
    public void Macro_IsTheArithmeticMeanOfTheRepositoryScoresInEveryRepeat()
    {
        RandomBaselineResult result = RandomBaseline.Run(Repositories(), seed: 20260912, repeats: 20);

        Assert.Equal(20, result.MacroF1.Values.Count);

        for (int repeat = 0; repeat < result.MacroF1.Values.Count; repeat++)
        {
            double expected = result.Repositories.Average(summary => summary.F1.Values[repeat]);

            Assert.Equal(expected, result.MacroF1.Values[repeat], 12);
        }
    }

    [Fact]
    public void Macro_IsNotTheSameAsTheMicroNumber()
    {
        RandomBaselineResult result = RandomBaseline.Run(Repositories(), seed: 20260912, repeats: 50);

        Assert.NotEqual(result.Micro.F1.Mean, result.MacroF1.Mean, 6);
    }

    [Fact]
    public void Macro_TheSameSeedGivesTheSameDistribution()
    {
        RandomBaselineResult first = RandomBaseline.Run(Repositories(), seed: 20260912, repeats: 30);
        RandomBaselineResult second = RandomBaseline.Run(Repositories(), seed: 20260912, repeats: 30);

        Assert.Equal(first.MacroF1.Values, second.MacroF1.Values);
        Assert.Equal(first.MacroF1.Mean, second.MacroF1.Mean, 12);
        Assert.Equal(first.MacroF1.Low, second.MacroF1.Low, 12);
        Assert.Equal(first.MacroF1.Median, second.MacroF1.Median, 12);
        Assert.Equal(first.MacroF1.High, second.MacroF1.High, 12);
    }

    [Fact]
    public void Macro_DoesNotDisturbTheRepositoryNumbers()
    {
        // Makro, repo F1'lerinden TURETILIYOR; fazladan rastgele sayi cekmiyor.
        // Iki kosuda repo dagilimlari birebir ayni kaliyor.
        RandomBaselineResult first = RandomBaseline.Run(Repositories(), seed: 20260912, repeats: 30);
        RandomBaselineResult second = RandomBaseline.Run(Repositories(), seed: 20260912, repeats: 30);

        for (int index = 0; index < first.Repositories.Count; index++)
        {
            Assert.Equal(first.Repositories[index].F1.Values, second.Repositories[index].F1.Values);
            Assert.Equal(
                first.Repositories[index].PredictedPositives.Values,
                second.Repositories[index].PredictedPositives.Values);
            Assert.Equal(first.Repositories[index].PrAuc.Values, second.Repositories[index].PrAuc.Values);
        }

        Assert.Equal(first.Micro.F1.Values, second.Micro.F1.Values);
    }

    // --- Operator farki ---

    [Fact]
    public void Operator_TheEquivalentGreaterThresholdIsTheNextObservedValueBelow()
    {
        IReadOnlyList<SnapshotRow> train =
        [
            Row(10, sha: "a"),
            Row(25, sha: "b"),
            Row(40, sha: "c"),
        ];

        Assert.Equal(25, ThresholdOperatorCheck.EquivalentGreaterThreshold(train, 40));
        Assert.Equal(10, ThresholdOperatorCheck.EquivalentGreaterThreshold(train, 25));
    }

    [Fact]
    public void Operator_ThereIsNoGreaterThresholdBelowTheSmallestObservedValue()
    {
        IReadOnlyList<SnapshotRow> train = [Row(10, sha: "a"), Row(40, sha: "b")];

        // Bos-pozitif uc noktasi: en kucuk degerde ">=" ifadesi ">" ile kurulamiyor.
        Assert.Null(ThresholdOperatorCheck.EquivalentGreaterThreshold(train, 10));
    }

    [Fact]
    public void Operator_TheTwoVectorsAgreeWhenNoValueFallsBetweenTheThresholds()
    {
        IReadOnlyList<SnapshotRow> rows = [Row(10, sha: "a"), Row(25, sha: "b"), Row(40, sha: "c")];

        Assert.Equal(0, ThresholdOperatorCheck.Differences(rows, greaterOrEqual: 40, greater: 25));
    }

    [Fact]
    public void Operator_TheDifferenceIsCountedForEveryRowBetweenTheThresholds()
    {
        // 30 ve 35, "25 <" dogru ama "40 >=" yanlis: ikisi de fark.
        IReadOnlyList<SnapshotRow> rows =
        [
            Row(10, sha: "a"),
            Row(30, sha: "b"),
            Row(35, sha: "c"),
            Row(40, sha: "d"),
        ];

        Assert.Equal(2, ThresholdOperatorCheck.Differences(rows, greaterOrEqual: 40, greater: 25));
    }

    [Fact]
    public void Operator_ComparesBothSidesOfTheSplit()
    {
        RepositorySplit split = new(
            "a/b",
            [Row(10, sha: "t1"), Row(40, sha: "t2")],
            [Row(25, sha: "s1"), Row(40, sha: "s2")]);

        OperatorCheck check = ThresholdOperatorCheck.Compare(split, threshold: 40);

        Assert.Equal(40, check.GreaterOrEqual);
        Assert.Equal(10, check.Greater);
        Assert.Equal(0, check.TrainDifferences);

        // Testteki 25, egitimde gorulmeyen bir degerin arasina dustugu icin fark uretiyor.
        Assert.Equal(1, check.TestDifferences);
    }

    private static IReadOnlyList<RepositorySplit> Repositories() =>
    [
        BaselineTests.Split("a/one", 3, 7, 3, 7),
        BaselineTests.Split("b/two", 2, 8, 2, 8),
        BaselineTests.Split("c/three", 4, 6, 4, 6),
    ];
}
