using Sievert.Cli;
using Sievert.Core.Rules;

using static Sievert.Tests.Samples;

namespace Sievert.Tests;

public class DiagnosticCardFormatterTests
{
    [Fact]
    public void Card_ShowsCodeSeverityAndTitleOnTheFirstLine()
    {
        string[] lines = Format(Found());

        Assert.Equal("SV001  [hata]  async void metot", lines[0]);
    }

    [Fact]
    public void Card_ShowsFileLineAndMethodName()
    {
        string[] lines = Format(Found(methodName: "Tick", filePath: "Patients/AsyncVoid.cs", line: 28));

        Assert.Equal("  Patients/AsyncVoid.cs:28  Tick", lines[1]);
    }

    [Fact]
    public void Card_ShowsTheDescriptionAndWhyItMatters()
    {
        string[] lines = Format(Found(methodName: "Save"));

        Assert.Contains("Save metodu async void.", lines[2], StringComparison.Ordinal);
        Assert.StartsWith("  Neden onemli: ", lines[3], StringComparison.Ordinal);
    }

    [Fact]
    public void Card_SeverityLabelFollowsTheLevel()
    {
        Assert.Contains("[bilgi]", Format(Found(severity: Severity.Info))[0], StringComparison.Ordinal);
        Assert.Contains("[uyari]", Format(Found(severity: Severity.Warning))[0], StringComparison.Ordinal);
    }

    [Fact]
    public void Cards_AreSeparatedByABlankLine()
    {
        string[] lines = Format(Found(methodName: "Save", line: 10), Found(methodName: "Tick", line: 28));

        int[] cardStarts = lines
            .Select((line, index) => (Line: line, Index: index))
            .Where(pair => pair.Line.StartsWith("SV001", StringComparison.Ordinal))
            .Select(pair => pair.Index)
            .ToArray();

        Assert.Equal(2, cardStarts.Length);
        Assert.Equal(string.Empty, lines[cardStarts[1] - 1]);
    }

    [Fact]
    public void Summary_ShowsFileCountFindingCountAndDistribution()
    {
        string[] lines = Format(Found(), Found(ruleCode: "SV002", methodName: "Tick"));

        Assert.Contains("Ozet", lines);
        Assert.Contains("  Taranan dosya : 3", lines);
        Assert.Contains("  Bulgu         : 2", lines);
        Assert.Contains("  SV001         : 1", lines);
        Assert.Contains("  SV002         : 1", lines);
    }

    [Fact]
    public void NoFindings_SaysSoAndStillWritesTheSummary()
    {
        string[] lines = Format();

        Assert.Contains("Bulgu yok.", lines);
        Assert.Contains("  Bulgu         : 0", lines);
    }

    [Fact]
    public void Formatting_KeepsColorInformationSeparate()
    {
        OutputLine line = DiagnosticCardFormatter
            .Format([Found()], CheckSummary.Of(3, [Found()]))
            .First(line => line.PlainText.StartsWith("SV001", StringComparison.Ordinal));

        Assert.Contains(line.Spans, span => span.Color == OutputColor.Heading && span.Text == "SV001");
        Assert.Contains(line.Spans, span => span.Color == OutputColor.Warning && span.Text.Contains("[hata]", StringComparison.Ordinal));
    }

    private static string[] Format(params Finding[] findings) =>
        DiagnosticCardFormatter.Format(findings, CheckSummary.Of(3, findings))
            .Select(line => line.PlainText)
            .ToArray();
}
