using System.Text.Json;

using Sievert.Analysis;
using Sievert.Cli;
using Sievert.Core.Analysis;

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

    private static JsonElement Parse(FileAnalysis analysis, string root = "/kok") =>
        JsonDocument.Parse(JsonFormatter.Format(root, [analysis], Summarizer.Summarize([analysis]), [])).RootElement;
}
