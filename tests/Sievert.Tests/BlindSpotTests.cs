using Sievert.Analysis;
using Sievert.Core.Analysis;

namespace Sievert.Tests;

public class BlindSpotTests
{
    [Fact]
    public void NoConditionalCompilation_BlindSpotIsZero()
    {
        FileAnalysis analysis = Analyze("public class Bir { public void Calis() { } }");

        Assert.False(analysis.HasConditionalCompilation);
        Assert.Equal(0, analysis.BlindSpotLines);
    }

    [Fact]
    public void ClosedBranch_LinesAreCounted()
    {
        FileAnalysis analysis = Analyze("""
            public class Bir
            {
            #if NETFRAMEWORK
                public void Eski() { }
                public void Daha() { }
            #endif
                public void Yeni() { }
            }
            """);

        Assert.True(analysis.HasConditionalCompilation);
        Assert.Equal(2, analysis.BlindSpotLines);
    }

    [Fact]
    public void TypesInClosedBranch_DoNotAppearInResults()
    {
        FileAnalysis analysis = Analyze("""
            #if NETFRAMEWORK
            public class Gizli
            {
                public void Calis() { }
            }
            #endif
            public class Gorunen { }
            """);

        // Kaybi telafi etmiyoruz, sadece olcuyoruz: gizli sinif sonucta yok ama satirlari sayiliyor.
        Assert.Equal(["Gorunen"], analysis.Types.Select(type => type.Name).ToArray());
        Assert.Equal(4, analysis.BlindSpotLines);
    }

    [Fact]
    public void ElseBranch_StaysOpenAndIsNotCounted()
    {
        FileAnalysis analysis = Analyze("""
            public class Bir
            {
            #if NETFRAMEWORK
                public void Eski() { }
            #else
                public void Yeni() { }
            #endif
            }
            """);

        Assert.Equal(["Yeni"], analysis.Types.Single().Methods.Select(method => method.Name).ToArray());
        Assert.Equal(1, analysis.BlindSpotLines);
    }

    [Fact]
    public void OpenBranch_BlindSpotIsZeroButConditionalIsMarked()
    {
        // Hicbir sembol tanimli olmadigi icin !NETFRAMEWORK dogru, hicbir sey kaybolmuyor.
        FileAnalysis analysis = Analyze("""
            #if !NETFRAMEWORK
            public class Bir { public void Calis() { } }
            #endif
            """);

        Assert.True(analysis.HasConditionalCompilation);
        Assert.Equal(0, analysis.BlindSpotLines);
        Assert.Single(analysis.Types);
    }

    [Fact]
    public void Summary_AddsUpBlindSpotsAndComputesRatio()
    {
        FileAnalysis[] analyses =
        [
            new("a.cs", [], TotalLineCount: 60, ParseErrors: [], BlindSpotLines: 10, HasConditionalCompilation: true),
            new("b.cs", [], TotalLineCount: 40, ParseErrors: [], BlindSpotLines: 0, HasConditionalCompilation: true),
            new("c.cs", [], TotalLineCount: 100, ParseErrors: [], BlindSpotLines: 0, HasConditionalCompilation: false),
        ];

        BlindSpot blindSpot = Summarizer.Summarize(analyses).BlindSpot;

        Assert.Equal(2, blindSpot.FileCount);
        Assert.Equal(10, blindSpot.LineCount);
        Assert.Equal(0.05, blindSpot.Ratio); // 10 / 200
    }

    [Fact]
    public void Summary_RatioIsZeroForEmptyScan()
    {
        Assert.Equal(0, Summarizer.Summarize([]).BlindSpot.Ratio);
    }

    private static FileAnalysis Analyze(string source) => FileAnalyzer.AnalyzeText(source, "test.cs");
}
