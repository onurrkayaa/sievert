using Sievert.Cli;

namespace Sievert.Tests;

public class ArgumentParserTests
{
    [Fact]
    public void Scan_ReadsPath()
    {
        ScanOptions options = Successful(["scan", "src"]);

        Assert.Equal("src", options.TargetPath);
        Assert.False(options.Json);
        Assert.Null(options.TopCount);
    }

    [Fact]
    public void Scan_FlagsCanBeGivenTogether()
    {
        ScanOptions options = Successful(["scan", "src", "--json", "--top", "5"]);

        Assert.True(options.Json);
        Assert.Equal(5, options.TopCount);
    }

    [Fact]
    public void Scan_FlagOrderDoesNotMatter()
    {
        Assert.Equal(Successful(["scan", "src", "--json", "--top", "3"]), Successful(["scan", "src", "--top", "3", "--json"]));
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
    public void WrongUsage_ReturnsError(params string[] args)
    {
        ParseResult result = ArgumentParser.Parse(args);

        Assert.False(result.Success);
        Assert.Null(result.Options);
        Assert.False(string.IsNullOrWhiteSpace(result.Error));
    }

    [Fact]
    public void HelpText_ContainsUsageLine()
    {
        Assert.Contains("sievert scan <yol>", ArgumentParser.HelpText, StringComparison.Ordinal);
    }

    private static ScanOptions Successful(string[] args)
    {
        ParseResult result = ArgumentParser.Parse(args);

        Assert.True(result.Success, result.Error);
        return result.Options!;
    }
}
