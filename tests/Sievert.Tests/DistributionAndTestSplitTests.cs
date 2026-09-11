using Sievert.Analysis;
using Sievert.Cli;
using Sievert.Core.Analysis;

using static Sievert.Tests.Samples;

namespace Sievert.Tests;

public class DistributionAndTestSplitTests
{
    [Fact]
    public void Length_OddNumberOfMethodsGivesTheMiddleValue()
    {
        MethodLength length = Lengths(1, 5, 100);

        Assert.Equal(5, length.Median);
        Assert.Equal(100, length.Longest);
    }

    [Fact]
    public void Length_EvenNumberOfMethodsAveragesTheTwoMiddleValues()
    {
        Assert.Equal(7.5, Lengths(1, 5, 10, 100).Median);
    }

    [Fact]
    public void Length_AverageCanBeLargerThanTheMedian()
    {
        MethodLength length = Lengths(1, 1, 1, 1, 200);

        Assert.Equal(1, length.Median);
        Assert.Equal(40.8, length.Average);
    }

    [Fact]
    public void Length_PercentileReturnsTheValueAtThatRank()
    {
        // 1..100 arasi 100 metot: p90 90, p95 95.
        MethodLength length = Lengths([.. Enumerable.Range(1, 100)]);

        Assert.Equal(90, length.P90);
        Assert.Equal(95, length.P95);
        Assert.Equal(100, length.Longest);
    }

    [Fact]
    public void Length_EverythingIsZeroWhenThereAreNoMethods()
    {
        MethodLength length = Summarizer.Summarize([]).MethodLength;

        Assert.Equal(0, length.Average);
        Assert.Equal(0, length.Median);
        Assert.Equal(0, length.P90);
        Assert.Equal(0, length.P95);
        Assert.Equal(0, length.Longest);
    }

    [Theory]
    [InlineData("test/Polly.Specs/CacheSpecs.cs", true)]
    [InlineData("tests/Sievert.Tests/BannerTests.cs", true)]
    [InlineData("src/Uygulama/BannerTests.cs", true)]
    [InlineData("src/Uygulama/OdemeTest.cs", true)]
    [InlineData("src/Uygulama/Odeme.cs", false)]
    [InlineData("src/Testler/Odeme.cs", false)]
    [InlineData("src/Contest/Odeme.cs", false)]
    public void TestCode_IsGuessedFromThePathAndTheName(string path, bool expected)
    {
        Assert.Equal(expected, FileAnalysis.IsTestCodePath(path));
    }

    [Fact]
    public void Summary_CountsProductionAndTestCodeSeparately()
    {
        CodeSplit split = Summarizer.Summarize(
        [
            Sample("src/Odeme.cs", Type("Odeme", Method("Calis"), Method("Dur"))),
            Sample("test/OdemeSpecs.cs", Type("OdemeSpecs", Method("Bir"))),
            Sample("src/OdemeTests.cs", Type("OdemeTests", Method("Iki"), Method("Uc"), Method("Dort"))),
        ]).CodeSplit;

        Assert.Equal(1, split.ProductionFileCount);
        Assert.Equal(2, split.TestFileCount);
        Assert.Equal(2, split.ProductionMethodCount);
        Assert.Equal(4, split.TestMethodCount);
    }

    [Fact]
    public void Tree_SummaryIncludesTheDistributionAndTheBlindSpot()
    {
        FileAnalysis[] analyses =
        [
            new("a.cs", [Type("Bir", Method("Calis", lineCount: 10))], 50, [], BlindSpotLines: 5, HasConditionalCompilation: true),
        ];

        string[] lines = TreeFormatter
            .Format(analyses, Summarizer.Summarize(analyses), [])
            .Select(line => line.PlainText)
            .ToArray();

        Assert.Contains(lines, line => line.StartsWith("a.cs  [#if - 5 satir gorulmedi]", StringComparison.Ordinal));
        Assert.Contains(lines, line => line.Contains("medyan 10.0", StringComparison.Ordinal) && line.Contains("p95 10", StringComparison.Ordinal));
        Assert.Contains(lines, line => line.Contains("1 dosyada #if, 5 satir gorulmedi (%10.0)", StringComparison.Ordinal));
    }

    [Fact]
    public void Tree_ConditionalFileWithoutBlindSpotIsOnlyMarked()
    {
        FileAnalysis[] analyses = [new("a.cs", [], 10, [], BlindSpotLines: 0, HasConditionalCompilation: true)];

        string[] lines = TreeFormatter
            .Format(analyses, Summarizer.Summarize(analyses), [])
            .Select(line => line.PlainText)
            .ToArray();

        Assert.Equal("a.cs  [#if]", lines[0]);
    }

    private static MethodLength Lengths(params int[] lineCounts)
    {
        FileAnalysis analysis = Sample(
            "a.cs",
            Type("Bir", [.. lineCounts.Select((count, index) => Method($"M{index}", lineCount: count))]));

        return Summarizer.Summarize([analysis]).MethodLength;
    }
}
