using Sievert.Analysis;
using Sievert.Core.Analysis;

using static Sievert.Tests.Samples;

namespace Sievert.Tests;

public class SummarizerTests
{
    private static readonly FileAnalysis[] Analyses =
    [
        Sample("a.cs", Type("Bir", Method("Kisa", lineCount: 2), Method("Uzun", lineCount: 10, isAsync: true))),
        Sample("b.cs", Type("Iki", Method("Orta", lineCount: 6, isAsync: true)), Type("Uc", Method("Tek", lineCount: 2))),
        BrokenSample("bozuk.cs", "Satir 3: ; bekleniyor"),
        TypelessSample("ustseviye.cs"),
    ];

    [Fact]
    public void Counts_AreAddedUp()
    {
        ScanSummary summary = Summarizer.Summarize(Analyses);

        Assert.Equal(4, summary.FileCount);
        Assert.Equal(3, summary.TypeCount);
        Assert.Equal(4, summary.MethodCount);
        Assert.Equal(2, summary.AsyncMethodCount);
    }

    [Fact]
    public void AsyncRatioAndAverageLength_AreComputed()
    {
        ScanSummary summary = Summarizer.Summarize(Analyses);

        Assert.Equal(0.5, summary.AsyncRatio);
        Assert.Equal(5.0, summary.MethodLength.Average); // (2 + 10 + 6 + 2) / 4
    }

    [Fact]
    public void BrokenAndTypelessFiles_AreCountedSeparately()
    {
        ScanSummary summary = Summarizer.Summarize(Analyses);

        Assert.Equal(1, summary.FilesWithParseErrors);
        // Bozuk dosyada da tip yok, o yuzden ikisi de tipsiz sayiliyor.
        Assert.Equal(2, summary.FilesWithoutTypes);
    }

    [Fact]
    public void EmptyScan_DoesNotDivideByZero()
    {
        ScanSummary summary = Summarizer.Summarize([]);

        Assert.Equal(0, summary.MethodCount);
        Assert.Equal(0, summary.AsyncRatio);
        Assert.Equal(0, summary.MethodLength.Average);
    }

    [Fact]
    public void LongestMethods_AreSortedFromLongToShort()
    {
        IReadOnlyList<MethodLocation> longest = Summarizer.LongestMethods(Analyses, 2);

        Assert.Equal(["Uzun", "Orta"], longest.Select(location => location.Method.Name).ToArray());
        Assert.Equal("a.cs", longest[0].FilePath);
        Assert.Equal("Bir", longest[0].TypeName);
    }

    [Fact]
    public void LongestMethods_DoesNotReturnMoreThanAsked()
    {
        Assert.Equal(3, Summarizer.LongestMethods(Analyses, 3).Count);
        Assert.Equal(4, Summarizer.LongestMethods(Analyses, 99).Count);
    }
}
