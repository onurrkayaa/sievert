using System.Text.Json;

using Sievert.Analysis;
using Sievert.Cli;
using Sievert.Core.Analysis;
using Sievert.Core.Rules;

using static Sievert.Tests.Samples;

namespace Sievert.Tests;

public class JsonFormatterTests
{
    [Fact]
    public void TypeKind_IsWrittenWithTheCSharpKeyword()
    {
        FileAnalysis analysis = new(
            "a.cs",
            [
                new SievertType("Bir", SievertTypeKind.Class, 1, []),
                new SievertType("Iki", SievertTypeKind.Record, 2, []),
                new SievertType("Uc", SievertTypeKind.Struct, 3, []),
                new SievertType("Dort", SievertTypeKind.Interface, 4, []),
            ],
            10,
            []);

        JsonElement[] types = Parse(analysis).GetProperty("files")[0].GetProperty("types").EnumerateArray().ToArray();

        Assert.Equal(
            ["class", "record", "struct", "interface"],
            types.Select(type => type.GetProperty("kind").GetString()!).ToArray());
    }

    [Fact]
    public void TypeKind_UsesTheSameWordAsTheTreeOutput()
    {
        FileAnalysis[] analyses = [Sample("a.cs", Type("Bir", Method("Calis")))];

        string treeLine = TreeFormatter
            .Format(analyses, Summarizer.Summarize(analyses), [])
            .Select(line => line.PlainText)
            .Single(line => line.Contains("Bir ", StringComparison.Ordinal));

        string jsonKind = Parse(analyses[0])
            .GetProperty("files")[0].GetProperty("types")[0].GetProperty("kind").GetString()!;

        Assert.Contains($"({jsonKind})", treeLine, StringComparison.Ordinal);
    }

    [Fact]
    public void ParseErrors_AllOfThemAreWrittenEvenPastTheScreenLimit()
    {
        string[] errors = Enumerable
            .Range(1, TreeFormatter.MaxErrorsOnScreen + 4)
            .Select(index => $"Satir {index}: ; bekleniyor")
            .ToArray();

        FileAnalysis analysis = BrokenSample("bozuk.cs", errors);

        JsonElement jsonErrors = Parse(analysis).GetProperty("files")[0].GetProperty("parseErrors");
        int errorLinesOnScreen = TreeFormatter
            .Format([analysis], Summarizer.Summarize([analysis]), [])
            .Count(line => line.PlainText.Contains("! Satir", StringComparison.Ordinal));

        Assert.Equal(errors.Length, jsonErrors.GetArrayLength());
        Assert.Equal(TreeFormatter.MaxErrorsOnScreen, errorLinesOnScreen);
    }

    [Fact]
    public void ScanRoot_IsWrittenOnceAsAnAbsolutePath()
    {
        JsonElement json = Parse(Sample("Ic/Bir.cs", Type("Bir", Method("Calis"))), "/repo/kok");

        Assert.Equal("/repo/kok", json.GetProperty("scanRoot").GetString());
        Assert.Equal("Ic/Bir.cs", json.GetProperty("files")[0].GetProperty("filePath").GetString());
    }

    [Fact]
    public void LongestMethods_KeyIsNotWrittenUnlessAsked()
    {
        FileAnalysis analysis = Sample("a.cs", Type("Bir", Method("Calis")));

        Assert.False(Parse(analysis).TryGetProperty("longestMethods", out _));

        string json = JsonFormatter.Format(
            "/kok",
            [analysis],
            Summarizer.Summarize([analysis]),
            Summarizer.LongestMethods([analysis], 1));

        Assert.Equal(1, JsonDocument.Parse(json).RootElement.GetProperty("longestMethods").GetArrayLength());
    }

    [Fact]
    public void Check_TopLevelKeysFollowTheScanOutput()
    {
        JsonElement json = ParseCheck([Found()]);

        Assert.Equal("/kok", json.GetProperty("scanRoot").GetString());
        Assert.Equal(1, json.GetProperty("findings").GetArrayLength());
        Assert.True(json.TryGetProperty("summary", out _));
    }

    [Fact]
    public void Check_FindingCarriesEveryFieldItNeeds()
    {
        JsonElement finding = ParseCheck([Found(methodName: "Tick", filePath: "Patients/AsyncVoid.cs", line: 28)])
            .GetProperty("findings")[0];

        Assert.Equal("SV001", finding.GetProperty("ruleCode").GetString());
        Assert.Equal("async void metot", finding.GetProperty("title").GetString());
        Assert.Equal("Patients/AsyncVoid.cs", finding.GetProperty("filePath").GetString());
        Assert.Equal(28, finding.GetProperty("line").GetInt32());
        Assert.Equal("Tick", finding.GetProperty("methodName").GetString());
        Assert.False(string.IsNullOrWhiteSpace(finding.GetProperty("description").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(finding.GetProperty("rationale").GetString()));
    }

    [Fact]
    public void Check_SeverityIsWrittenAsAName()
    {
        // Sayi yazsak cikti okunmaz olurdu, enum adini camelCase yaziyoruz.
        Assert.Equal("error", ParseCheck([Found(severity: Severity.Error)]).GetProperty("findings")[0].GetProperty("severity").GetString());
        Assert.Equal("info", ParseCheck([Found(severity: Severity.Info)]).GetProperty("findings")[0].GetProperty("severity").GetString());
    }

    [Fact]
    public void Check_SummaryCarriesCountsAndDistribution()
    {
        JsonElement summary = ParseCheck([Found(), Found(ruleCode: "SV002")]).GetProperty("summary");

        Assert.Equal(3, summary.GetProperty("fileCount").GetInt32());
        Assert.Equal(2, summary.GetProperty("findingCount").GetInt32());
        JsonElement byRuleCode = summary.GetProperty("byRuleCode");

        Assert.Equal(2, byRuleCode.GetArrayLength());
        Assert.Equal("SV001", byRuleCode[0].GetProperty("ruleCode").GetString());
        Assert.Equal(1, byRuleCode[0].GetProperty("count").GetInt32());
        Assert.Equal("SV002", byRuleCode[1].GetProperty("ruleCode").GetString());
        Assert.Equal(1, byRuleCode[1].GetProperty("count").GetInt32());
    }

    [Fact]
    public void Check_ByRuleCodeIsSortedAndDeterministic()
    {
        // Bulgular karisik sirada geliyor, cikti yine kural koduna gore alfabetik.
        Finding[] findings = [Found(ruleCode: "SV003"), Found(ruleCode: "SV001"), Found(ruleCode: "SV002"), Found(ruleCode: "SV001")];

        string[] codes = ParseCheck(findings)
            .GetProperty("summary")
            .GetProperty("byRuleCode")
            .EnumerateArray()
            .Select(entry => entry.GetProperty("ruleCode").GetString()!)
            .ToArray();

        Assert.Equal(["SV001", "SV002", "SV003"], codes);
        Assert.Equal(
            JsonFormatter.FormatCheck("/kok", findings, [], CheckSummary.Of(3, findings)),
            JsonFormatter.FormatCheck("/kok", findings, [], CheckSummary.Of(3, findings)));
    }

    [Fact]
    public void Check_NoFindingsStillProducesTheSameShape()
    {
        JsonElement json = ParseCheck([]);

        Assert.Equal(0, json.GetProperty("findings").GetArrayLength());
        Assert.Equal(0, json.GetProperty("summary").GetProperty("findingCount").GetInt32());
        Assert.Equal(0, json.GetProperty("summary").GetProperty("byRuleCode").GetArrayLength());
    }

    [Fact]
    public void Check_NoExemptions_OmitsTheField()
    {
        // Muafiyetler --json'da opsiyonel: hic yoksa alan yazilmiyor, sema sismiyor.
        Assert.False(ParseCheck([Found()]).TryGetProperty("exemptions", out _));
    }

    [Fact]
    public void Check_ExemptionCarriesItsReason()
    {
        Exemption[] exemptions = [new("SV001", "Patients/AsyncVoid.cs", 16, "OnSaved", ExemptionReason.Signature)];

        JsonElement exemption = JsonDocument
            .Parse(JsonFormatter.FormatCheck("/kok", [], exemptions, CheckSummary.Of(3, [])))
            .RootElement
            .GetProperty("exemptions")[0];

        Assert.Equal("SV001", exemption.GetProperty("ruleCode").GetString());
        Assert.Equal("Patients/AsyncVoid.cs", exemption.GetProperty("filePath").GetString());
        Assert.Equal(16, exemption.GetProperty("line").GetInt32());
        Assert.Equal("OnSaved", exemption.GetProperty("methodName").GetString());
        Assert.Equal("signature", exemption.GetProperty("reason").GetString());
    }

    private static JsonElement ParseCheck(Finding[] findings, string root = "/kok") =>
        JsonDocument.Parse(JsonFormatter.FormatCheck(root, findings, [], CheckSummary.Of(3, findings))).RootElement;

    private static JsonElement Parse(FileAnalysis analysis, string root = "/kok") =>
        JsonDocument.Parse(JsonFormatter.Format(root, [analysis], Summarizer.Summarize([analysis]), [])).RootElement;
}
