using Sievert.Analysis;
using Sievert.Cli;
using Sievert.Core.Analysis;

using static Sievert.Tests.Samples;

namespace Sievert.Tests;

public class TreeFormatterTests
{
    [Fact]
    public void Tree_WritesFileThenTypeThenMethod()
    {
        string[] lines = Format(Sample("a.cs", Type("Bir", Method("Calis"))));

        Assert.Equal("a.cs", lines[0]);
        Assert.Contains("Bir (class)", lines[1], StringComparison.Ordinal);
        Assert.Contains("Calis", lines[2], StringComparison.Ordinal);
        Assert.StartsWith("`- ", lines[1], StringComparison.Ordinal);
    }

    [Fact]
    public void Tree_AsyncTagAndLineCountStayInTheSameColumn()
    {
        string[] lines = Format(Sample(
            "a.cs",
            Type("Bir", Method("Kisa"), Method("CokUzunBirMetotAdiVar", isAsync: true)),
            Type("Iki", Method("Digeri", isAsync: true))));

        // Ozet bolumunde de " satir" gecen bir satir var, oraya bakmiyoruz.
        string[] treeLines = lines.TakeWhile(line => line != "Ozet").ToArray();

        int[] lineCountColumns = treeLines
            .Where(line => line.Contains(" satir", StringComparison.Ordinal))
            .Select(line => line.IndexOf(" satir", StringComparison.Ordinal))
            .ToArray();

        int[] asyncColumns = treeLines
            .Where(line => line.Contains("[async]", StringComparison.Ordinal))
            .Select(line => line.IndexOf("[async]", StringComparison.Ordinal))
            .ToArray();

        Assert.Equal(3, lineCountColumns.Length);
        Assert.Single(lineCountColumns.Distinct());
        Assert.Equal(2, asyncColumns.Length);
        Assert.Single(asyncColumns.Distinct());
    }

    [Fact]
    public void Tree_LongMethodGetsAWarningMark()
    {
        string[] lines = Format(Sample(
            "a.cs",
            Type("Bir", Method("TamEsikte", lineCount: TreeFormatter.LongMethodThreshold), Method("EsigiGecen", lineCount: 41))));

        Assert.DoesNotContain("(!)", Only(lines, "TamEsikte"), StringComparison.Ordinal);
        Assert.Contains("(!)", Only(lines, "EsigiGecen"), StringComparison.Ordinal);
    }

    [Fact]
    public void Tree_FileWithoutTypesIsMarked()
    {
        string[] lines = Format(TypelessSample("ustseviye.cs"));

        Assert.Contains(lines, line => line.Contains("hic tip bulunamadi", StringComparison.Ordinal));
    }

    [Fact]
    public void Tree_ParseErrorsAreWritten()
    {
        string[] lines = Format(BrokenSample("bozuk.cs", "Satir 3: ; bekleniyor"));

        Assert.Contains(lines, line => line.Contains("! Satir 3: ; bekleniyor", StringComparison.Ordinal));
        // Hata varken ayrica "hic tip bulunamadi" demiyoruz, sebebi belli.
        Assert.DoesNotContain(lines, line => line.Contains("hic tip bulunamadi", StringComparison.Ordinal));
    }

    [Fact]
    public void Summary_ShowsEveryCountInTheLastSection()
    {
        string[] lines = Format(Sample("a.cs", Type("Bir", Method("Calis", isAsync: true), Method("Dur", lineCount: 3))));

        Assert.Contains("Ozet", lines);
        Assert.Contains("  Dosya                  : 1  (uretim 1 / test 0)", lines);
        Assert.Contains("  Tip                    : 1", lines);
        Assert.Contains("  Metot                  : 2  (uretim 2 / test 0)", lines);
        Assert.Contains("  Async orani            : %50.0 (1/2)", lines);
        Assert.Contains("  Metot uzunlugu         : ortalama 2.0  medyan 2.0  p90 3  p95 3  en uzun 3", lines);
        Assert.Contains("  Kor nokta              : yok", lines);
        Assert.Contains("  Ayristirilamayan dosya : 0", lines);
        Assert.Contains("  Tip bulunamayan dosya  : 0", lines);
    }

    [Fact]
    public void LongestMethods_SectionIsNotWrittenUnlessAsked()
    {
        FileAnalysis analysis = Sample("a.cs", Type("Bir", Method("Calis")));

        Assert.DoesNotContain(Format(analysis), line => line.StartsWith("En uzun", StringComparison.Ordinal));
        Assert.Contains(
            FormatWithLongest(analysis, 1),
            line => line.StartsWith("En uzun 1 metot", StringComparison.Ordinal));
    }

    [Fact]
    public void LongestMethods_ShowFileAndLineNumber()
    {
        FileAnalysis analysis = new("a.cs", [Type("Bir", Method("Calis", lineCount: 7, startLine: 12))], 100, []);

        Assert.Contains(
            FormatWithLongest(analysis, 1),
            line => line.Contains("Bir.Calis", StringComparison.Ordinal) && line.Contains("a.cs:12", StringComparison.Ordinal));
    }

    [Fact]
    public void Formatting_KeepsColorInformationSeparate()
    {
        FileAnalysis[] analyses = [Sample("a.cs", Type("Bir", Method("Calis", isAsync: true)))];

        OutputLine line = TreeFormatter
            .Format(analyses, Summarizer.Summarize(analyses), [])
            .First(line => line.PlainText.Contains("[async]", StringComparison.Ordinal));

        // Duz metinde ANSI kacis dizisi yok, renk sadece parcanin etiketinde duruyor.
        Assert.DoesNotContain("\u001b", line.PlainText, StringComparison.Ordinal);
        Assert.Contains(line.Spans, span => span.Color == OutputColor.Tag && span.Text == "[async]");
    }

    private static string[] Format(params FileAnalysis[] analyses) =>
        TreeFormatter.Format(analyses, Summarizer.Summarize(analyses), [])
            .Select(line => line.PlainText)
            .ToArray();

    private static string[] FormatWithLongest(FileAnalysis analysis, int count)
    {
        FileAnalysis[] analyses = [analysis];
        return TreeFormatter
            .Format(analyses, Summarizer.Summarize(analyses), Summarizer.LongestMethods(analyses, count))
            .Select(line => line.PlainText)
            .ToArray();
    }

    private static string Only(string[] lines, string methodName) =>
        lines.Single(line => line.Contains(methodName, StringComparison.Ordinal));
}
