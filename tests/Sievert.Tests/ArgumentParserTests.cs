using Sievert.Cli;
using Sievert.Core.Rules;

namespace Sievert.Tests;

public class ArgumentParserTests
{
    [Fact]
    public void Scan_ReadsPath()
    {
        ScanOptions options = SuccessfulScan(["scan", "src"]);

        Assert.Equal("src", options.TargetPath);
        Assert.False(options.Json);
        Assert.Null(options.TopCount);
    }

    [Fact]
    public void Scan_FlagsCanBeGivenTogether()
    {
        ScanOptions options = SuccessfulScan(["scan", "src", "--json", "--top", "5"]);

        Assert.True(options.Json);
        Assert.Equal(5, options.TopCount);
    }

    [Fact]
    public void Scan_FlagOrderDoesNotMatter()
    {
        Assert.Equal(SuccessfulScan(["scan", "src", "--json", "--top", "3"]), SuccessfulScan(["scan", "src", "--top", "3", "--json"]));
    }

    [Theory]
    [InlineData]
    [InlineData("bilinmeyen", "src")]
    [InlineData("scan")]
    [InlineData("scan", "--json")]
    [InlineData("scan", "src", "--top")]
    [InlineData("scan", "src", "--top", "sifir")]
    [InlineData("scan", "src", "--top", "0")]
    [InlineData("scan", "src", "--top", "-2")]
    [InlineData("scan", "src", "--yanlis")]
    [InlineData("check")]
    [InlineData("check", "--json")]
    [InlineData("check", "src", "--fail-on")]
    [InlineData("check", "src", "--fail-on", "olumcul")]
    [InlineData("check", "src", "--fail-on", "hata")]
    [InlineData("check", "src", "--top", "3")]
    [InlineData("check", "src", "--yanlis")]
    public void WrongUsage_ReturnsError(params string[] args)
    {
        ParseResult result = ArgumentParser.Parse(args);

        Assert.False(result.Success);
        Assert.Null(result.Options);
        Assert.False(string.IsNullOrWhiteSpace(result.Error));
    }

    [Fact]
    public void Check_ReadsPath()
    {
        CheckOptions options = SuccessfulCheck(["check", "src"]);

        Assert.Equal("src", options.TargetPath);
        Assert.False(options.Json);
    }

    [Fact]
    public void Check_FailOnDefaultsToWarning()
    {
        Assert.Equal(Severity.Warning, SuccessfulCheck(["check", "src"]).FailOn);
        Assert.Equal(Severity.Warning, ArgumentParser.DefaultFailOn);
    }

    [Theory]
    [InlineData("info", Severity.Info)]
    [InlineData("warning", Severity.Warning)]
    [InlineData("error", Severity.Error)]
    public void Check_FailOnReadsEveryLevel(string value, Severity expected)
    {
        Assert.Equal(expected, SuccessfulCheck(["check", "src", "--fail-on", value]).FailOn);
    }

    [Fact]
    public void Check_FlagsCanBeGivenTogether()
    {
        CheckOptions options = SuccessfulCheck(["check", "src", "--json", "--fail-on", "error"]);

        Assert.True(options.Json);
        Assert.Equal(Severity.Error, options.FailOn);
    }

    [Fact]
    public void HelpText_MentionsBothCommands()
    {
        Assert.Contains("scan <yol>", ArgumentParser.HelpText, StringComparison.Ordinal);
        Assert.Contains("check <yol>", ArgumentParser.HelpText, StringComparison.Ordinal);
    }

    private static ScanOptions SuccessfulScan(string[] args) => Assert.IsType<ScanOptions>(Successful(args));

    private static CheckOptions SuccessfulCheck(string[] args) => Assert.IsType<CheckOptions>(Successful(args));

    private static CommandOptions Successful(string[] args)
    {
        ParseResult result = ArgumentParser.Parse(args);

        Assert.True(result.Success, result.Error);
        return result.Options!;
    }
}
