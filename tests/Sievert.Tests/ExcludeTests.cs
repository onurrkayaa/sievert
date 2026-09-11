using System.Text.Json;

using Sievert.Analysis;
using Sievert.Cli;

namespace Sievert.Tests;

public class GlobPatternTests
{
    [Theory]
    // Klasor
    [InlineData("samples/**", "samples/Patients/AsyncVoid.cs", true)]
    [InlineData("samples/**", "samples/Bir.cs", true)]
    [InlineData("samples/**", "src/Sievert.Cli/Program.cs", false)]
    [InlineData("samples/**", "tests/samples/Bir.cs", false)]
    // Uzanti
    [InlineData("**/*.Designer.cs", "src/Formlar/Ana.Designer.cs", true)]
    [InlineData("**/*.Designer.cs", "Ana.Designer.cs", true)]
    [InlineData("**/*.Designer.cs", "src/Formlar/Ana.cs", false)]
    // Ic ice yol
    [InlineData("**/obj/**", "src/Sievert.Cli/obj/Debug/net10.0/Bir.cs", true)]
    [InlineData("**/obj/**", "obj/Bir.cs", true)]
    [InlineData("**/obj/**", "src/objects/Bir.cs", false)]
    // Tek yildiz klasor sinirini gecmiyor
    [InlineData("src/*.cs", "src/Bir.cs", true)]
    [InlineData("src/*.cs", "src/Alt/Bir.cs", false)]
    public void Matches_FollowsGlobRules(string pattern, string path, bool expected)
    {
        GlobPattern glob = GlobPattern.TryParse(pattern)!;

        Assert.NotNull(glob);
        Assert.Equal(expected, glob.Matches(path));
    }

    [Fact]
    public void Matches_UsesForwardSlashesWhateverTheSeparator()
    {
        // Windows'ta gelen yollar ters bolu iceriyor, kalip yine ayni yazilabilsin.
        Assert.True(GlobPattern.TryParse("samples/**")!.Matches(@"samples\Patients\AsyncVoid.cs"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("src/**tests/*.cs")]
    [InlineData("***")]
    [InlineData("src//Bir.cs")]
    [InlineData("/mutlak/yol/**")]
    public void TryParse_RejectsBadPatterns(string pattern) =>
        Assert.Null(GlobPattern.TryParse(pattern));
}

public class ExcludeFilterTests
{
    private static readonly string Root = Path.Combine(Path.GetTempPath(), "kok");

    private static readonly string[] Files =
    [
        Path.Combine(Root, "samples", "Patients", "AsyncVoid.cs"),
        Path.Combine(Root, "samples", "Patients", "Normal.cs"),
        Path.Combine(Root, "src", "Program.cs"),
        Path.Combine(Root, "src", "Formlar", "Ana.Designer.cs"),
    ];

    [Fact]
    public void NoPatterns_KeepsEverything() =>
        Assert.Equal(4, ExcludeFilter.Apply(Files, Root, []).Count);

    [Fact]
    public void OnePattern_DropsWhatItMatches()
    {
        IReadOnlyList<string> kept = ExcludeFilter.Apply(Files, Root, [Glob("samples/**")]);

        Assert.Equal(2, kept.Count);
        Assert.DoesNotContain(kept, path => path.Contains("samples", StringComparison.Ordinal));
    }

    [Fact]
    public void SeveralPatterns_AreAppliedTogether()
    {
        IReadOnlyList<string> kept = ExcludeFilter.Apply(
            Files,
            Root,
            [Glob("samples/**"), Glob("**/*.Designer.cs")]);

        Assert.Equal([Path.Combine(Root, "src", "Program.cs")], kept);
    }

    [Fact]
    public void AFileMatchingTwoPatterns_IsStillJustOneFile()
    {
        // Iki kalip da ayni dosyalari tutuyor. Dislanan sayisi kalip sayisina gore
        // degil dosya sayisina gore cikmali, yoksa ozetteki sayi sisiyor.
        IReadOnlyList<string> kept = ExcludeFilter.Apply(Files, Root, [Glob("samples/**"), Glob("**/*.cs")]);

        Assert.Empty(kept);
        Assert.Equal(4, Files.Length - kept.Count);
    }

    private static GlobPattern Glob(string pattern) => GlobPattern.TryParse(pattern)!;
}

public class ExcludeArgumentTests
{
    [Fact]
    public void Exclude_CanBeGivenMoreThanOnce()
    {
        CheckOptions options = (CheckOptions)ArgumentParser
            .Parse(["check", ".", "--exclude", "samples/**", "--exclude", "**/*.Designer.cs"])
            .Options!;

        Assert.Equal(["samples/**", "**/*.Designer.cs"], options.Exclude.Select(glob => glob.Text).ToArray());
    }

    [Fact]
    public void Exclude_WorksForScanToo()
    {
        ScanOptions options = (ScanOptions)ArgumentParser.Parse(["scan", ".", "--exclude", "samples/**"]).Options!;

        Assert.Single(options.Exclude);
    }

    [Fact]
    public void Exclude_WithoutAPattern_IsAUsageError() =>
        Assert.False(ArgumentParser.Parse(["check", ".", "--exclude"]).Success);

    [Fact]
    public void Exclude_WithABadPattern_IsAUsageError() =>
        Assert.False(ArgumentParser.Parse(["check", ".", "--exclude", "src/**tests/*.cs"]).Success);
}

/// <summary>--exclude'un komut seviyesinde ucundan ucuna calistigini gosteren testler.</summary>
[Collection(ConsoleCollection.Name)]
public class ExcludeEndToEndTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "sievert-exclude-" + Guid.NewGuid().ToString("N"));
    private readonly TextWriter _stdout = Console.Out;
    private readonly TextWriter _stderr = Console.Error;

    public ExcludeEndToEndTests()
    {
        Directory.CreateDirectory(Path.Combine(_root, "src"));
        Directory.CreateDirectory(Path.Combine(_root, "samples"));
        Directory.CreateDirectory(Path.Combine(_root, "obj"));

        File.WriteAllText(Path.Combine(_root, "src", "Saglikli.cs"), "public class Saglikli { }");
        File.WriteAllText(Path.Combine(_root, "samples", "Hasta.cs"), """
            using System.Threading.Tasks;

            public class Hasta
            {
                public async void Calis()
                {
                    await Task.CompletedTask;
                }
            }
            """);
        File.WriteAllText(Path.Combine(_root, "obj", "Uretilmis.cs"), "public class Uretilmis { }");

        Console.SetError(TextWriter.Null);
    }

    public void Dispose()
    {
        Console.SetOut(_stdout);
        Console.SetError(_stderr);
        Directory.Delete(_root, recursive: true);
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void WithoutExclude_DefaultSkipsStillApply()
    {
        // obj klasorune hicbir zaman girilmiyor, ama --exclude verilmedigi icin
        // dislanan sayisi sifir: o dosyalar bu sayiya girmiyor.
        (int fileCount, int excluded) = ScanCounts(["scan", _root, "--json"]);

        Assert.Equal(2, fileCount);
        Assert.Equal(0, excluded);
    }

    [Fact]
    public void WithExclude_DefaultSkipsAreStillApplied()
    {
        // --exclude varsayilanlarin yerine gecmiyor, uzerine ekleniyor: samples elendi,
        // obj yine hic taranmadi. Geriye sadece src kaldi.
        (int fileCount, int excluded) = ScanCounts(["scan", _root, "--exclude", "samples/**", "--json"]);

        Assert.Equal(1, fileCount);
        Assert.Equal(1, excluded);
    }

    [Fact]
    public void ExcludedFileCount_IsAlsoRightForCheck()
    {
        using JsonDocument json = JsonDocument.Parse(Capture(["check", _root, "--exclude", "samples/**", "--json"]));
        JsonElement summary = json.RootElement.GetProperty("summary");

        Assert.Equal(1, summary.GetProperty("fileCount").GetInt32());
        Assert.Equal(1, summary.GetProperty("excludedFileCount").GetInt32());
    }

    [Fact]
    public void Exclude_MakesTheDeliberatelyBrokenSampleStopBreakingTheBuild()
    {
        // CI'in yaptigi sey: hasta ornek dosyalari disla, geri kalan temiz olsun.
        Assert.Equal(ExitCodes.FindingsFound, CommandRunner.Run(["check", _root]));
        Assert.Equal(ExitCodes.Clean, CommandRunner.Run(["check", _root, "--exclude", "samples/**"]));
    }

    [Fact]
    public void InvalidPattern_IsAToolError() =>
        Assert.Equal(ExitCodes.ToolError, CommandRunner.Run(["check", _root, "--exclude", "src/**tests/*.cs"]));

    [Fact]
    public void ExcludingEverything_IsAToolErrorNotACleanRun() =>
        // Hicbir sey taranmadigi halde 0 donseydi CI adimi "temiz" derdi.
        Assert.Equal(ExitCodes.ToolError, CommandRunner.Run(["check", _root, "--exclude", "**/*.cs"]));

    private (int FileCount, int ExcludedFileCount) ScanCounts(string[] args)
    {
        using JsonDocument json = JsonDocument.Parse(Capture(args));
        JsonElement summary = json.RootElement.GetProperty("summary");

        return (summary.GetProperty("fileCount").GetInt32(), summary.GetProperty("excludedFileCount").GetInt32());
    }

    private static string Capture(string[] args)
    {
        StringWriter output = new();
        Console.SetOut(output);

        CommandRunner.Run(args);

        return output.ToString();
    }
}
