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
        Assert.Equal(4, ExcludeFilter.Apply(Files, Root, []).Files.Count);

    [Fact]
    public void OnePattern_DropsWhatItMatches()
    {
        IReadOnlyList<string> kept = ExcludeFilter.Apply(Files, Root, [Glob("samples/**")]).Files;

        Assert.Equal(2, kept.Count);
        Assert.DoesNotContain(kept, path => path.Contains("samples", StringComparison.Ordinal));
    }

    [Fact]
    public void SeveralPatterns_AreAppliedTogether()
    {
        IReadOnlyList<string> kept = ExcludeFilter.Apply(
            Files,
            Root,
            [Glob("samples/**"), Glob("**/*.Designer.cs")]).Files;

        Assert.Equal([Path.Combine(Root, "src", "Program.cs")], kept);
    }

    [Fact]
    public void AFileMatchingTwoPatterns_IsStillJustOneFile()
    {
        // Iki kalip da ayni dosyalari tutuyor. Dislanan sayisi kalip sayisina gore
        // degil dosya sayisina gore cikmali, yoksa ozetteki sayi sisiyor.
        IReadOnlyList<string> kept = ExcludeFilter.Apply(Files, Root, [Glob("samples/**"), Glob("**/*.cs")]).Files;

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
    public void AnExcludedFile_CannotBeUsedAsEvidenceByARule()
    {
        // Adim 2'de olculen tutarsizlik buydu: SV001 partial parcalari diskten okudugu icin
        // dislanan dosya yine de muafiyet uretiyordu. Artik kural sadece taranan kumeyi
        // goruyor, o yuzden OnShown bulgu olmali.
        string folder = Path.Combine(_root, "Views");
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, "Screen.cs"), "public partial class Screen { public async void OnShown(ShownArgs a) { } }");
        File.WriteAllText(Path.Combine(folder, "Screen.Designer.cs"), "public partial class Screen { void Wire(Source s) { s.Shown += OnShown; } }");

        using JsonDocument json = JsonDocument.Parse(
            Capture(["check", folder, "--exclude", "**/*.Designer.cs", "--json"]));

        Assert.Equal(
            ["OnShown"],
            json.RootElement.GetProperty("findings").EnumerateArray()
                .Select(finding => finding.GetProperty("methodName").GetString()!)
                .ToArray());
        Assert.False(json.RootElement.TryGetProperty("exemptions", out _));
    }

    [Fact]
    public void AFileInTheScanSet_IsStillUsedAsEvidence()
    {
        // Yukaridakinin karsiti: dislama olmayinca kanit yine calismali, yoksa
        // tutarlilik duzeltmesi ADR 0008'in muafiyetini sessizce oldururdu.
        string folder = Path.Combine(_root, "Views2");
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, "Screen.cs"), "public partial class Screen { public async void OnShown(ShownArgs a) { } }");
        File.WriteAllText(Path.Combine(folder, "Screen.Designer.cs"), "public partial class Screen { void Wire(Source s) { s.Shown += OnShown; } }");

        using JsonDocument json = JsonDocument.Parse(Capture(["check", folder, "--json"]));

        Assert.Empty(json.RootElement.GetProperty("findings").EnumerateArray());
        Assert.Equal(
            "subscription",
            json.RootElement.GetProperty("exemptions")[0].GetProperty("reason").GetString());
    }

    [Fact]
    public void APatternThatMatchesNothing_Warns()
    {
        // Sessiz eslesmeme, yanlis yazilmis bir kalibin tek belirtisi.
        string errors = CaptureErrors(["check", _root, "--exclude", "yok/**"]);

        Assert.Contains("hicbir dosyayla eslesmedi", errors, StringComparison.Ordinal);
        Assert.Contains("yok/**", errors, StringComparison.Ordinal);
    }

    [Fact]
    public void APatternThatIsJustAFolderName_SaysWhatToWriteInstead()
    {
        // En sik yapilan hata: "samples" yazip altindaki dosyalarin elenmesini beklemek.
        string errors = CaptureErrors(["check", _root, "--exclude", "samples"]);

        Assert.Contains("samples/**", errors, StringComparison.Ordinal);
    }

    [Fact]
    public void APatternThatMatches_DoesNotWarn() =>
        Assert.DoesNotContain(
            "eslesmedi",
            CaptureErrors(["check", _root, "--exclude", "samples/**"]),
            StringComparison.Ordinal);

    [Fact]
    public void SkippedDirectories_AreReportedByCountAndByPath()
    {
        // obj'nin icine girilmiyor ve icindeki dosyalar "dislanan" sayisina da girmiyor.
        // Sessiz kalmamasi icin klasorun kendisi raporlaniyor.
        using JsonDocument json = JsonDocument.Parse(Capture(["check", _root, "--exclude", "samples/**", "--json"]));

        Assert.Equal(
            ["obj"],
            json.RootElement.GetProperty("summary").GetProperty("skippedDirectories")
                .EnumerateArray().Select(path => path.GetString()!).ToArray());
    }

    [Fact]
    public void SkippedDirectoryCount_IsOnTheScreenSummary()
    {
        Assert.Contains("Atlanan klasor : 1", Capture(["check", _root, "--exclude", "samples/**"]), StringComparison.Ordinal);
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

    private static string CaptureErrors(string[] args)
    {
        StringWriter errors = new();
        Console.SetOut(TextWriter.Null);
        Console.SetError(errors);

        try
        {
            CommandRunner.Run(args);
        }
        finally
        {
            Console.SetError(TextWriter.Null);
        }

        return errors.ToString();
    }
}
