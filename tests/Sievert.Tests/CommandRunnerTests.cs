using Sievert.Cli;

namespace Sievert.Tests;

[Collection(ConsoleCollection.Name)]
public class CommandRunnerTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "sievert-cikis-" + Guid.NewGuid().ToString("N"));
    private readonly TextWriter _stdout = Console.Out;
    private readonly TextWriter _stderr = Console.Error;

    public CommandRunnerTests()
    {
        Directory.CreateDirectory(CleanDirectory);
        Directory.CreateDirectory(FindingDirectory);
        Directory.CreateDirectory(EmptyDirectory);
        Directory.CreateDirectory(ExemptDirectory);
        Directory.CreateDirectory(WarningDirectory);
        Directory.CreateDirectory(InfoDirectory);

        File.WriteAllText(Path.Combine(CleanDirectory, "Temiz.cs"), """
            public class Temiz
            {
                public int Topla(int a, int b) => a + b;
            }
            """);

        File.WriteAllText(Path.Combine(FindingDirectory, "Bulgulu.cs"), """
            using System.Threading.Tasks;

            public class Bulgulu
            {
                public async void Calis()
                {
                    await Task.CompletedTask;
                }
            }
            """);

        File.WriteAllText(Path.Combine(ExemptDirectory, "Muaf.cs"), """
            using System;
            using System.Threading.Tasks;

            public class Muaf
            {
                public async void OnSaved(object sender, EventArgs e)
                {
                    await Task.CompletedTask;
                }
            }
            """);

        File.WriteAllText(Path.Combine(WarningDirectory, "Bekleyen.cs"), """
            using System.Threading.Tasks;

            public class Bekleyen
            {
                public int Oku()
                {
                    return YukleAsync().Result;
                }

                private Task<int> YukleAsync() => Task.FromResult(1);
            }
            """);

        File.WriteAllText(Path.Combine(InfoDirectory, "Servis.cs"), """
            using System.Threading.Tasks;

            public class Servis
            {
                public Task KaydetAsync(string ad)
                {
                    return Task.CompletedTask;
                }
            }
            """);

        // Komutlar ekrana yaziyor; testte sadece cikis koduna bakiyoruz.
        Console.SetOut(TextWriter.Null);
        Console.SetError(TextWriter.Null);
    }

    public void Dispose()
    {
        Console.SetOut(_stdout);
        Console.SetError(_stderr);
        Directory.Delete(_root, recursive: true);
        GC.SuppressFinalize(this);
    }

    private string CleanDirectory => Path.Combine(_root, "temiz");

    private string FindingDirectory => Path.Combine(_root, "bulgulu");

    private string EmptyDirectory => Path.Combine(_root, "bos");

    private string ExemptDirectory => Path.Combine(_root, "muaf");

    /// <summary>Icinde sadece uyari seviyesinde bulgu (SV002) olan bir klasor.</summary>
    private string WarningDirectory => Path.Combine(_root, "uyari");

    /// <summary>Icinde sadece bilgi seviyesinde bulgu (SV006) olan bir klasor.</summary>
    private string InfoDirectory => Path.Combine(_root, "bilgi");

    [Fact]
    public void AnInfoOnlyRule_DoesNotBreakTheBuild()
    {
        // SV006 cok bulgu uretiyor ve info seviyesinde. --fail-on varsayilani warning
        // oldugu icin CI'i kirmamasi gerekiyor; tasarimin dogrulamasi bu.
        StringWriter output = new();
        Console.SetOut(output);

        Assert.Equal(ExitCodes.Clean, CommandRunner.Run(["check", InfoDirectory]));

        // Kirmiyor ama gizlemiyor da: bulgu ekranda duruyor.
        Assert.Contains("SV006", output.ToString(), StringComparison.Ordinal);
        Assert.Contains("bilgi 1", output.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void AnInfoOnlyRule_BreaksTheBuildOnlyIfYouAskForIt() =>
        Assert.Equal(ExitCodes.FindingsFound, CommandRunner.Run(["check", InfoDirectory, "--fail-on", "info"]));

    [Fact]
    public void FindingBelowTheThreshold_DoesNotBreakTheBuild()
    {
        // SV002 warning seviyesinde. --fail-on error verilince bulgu esigin altinda
        // kaliyor: ekrana yaziliyor ama cikis kodu 0. Asama 2'de tek kuralim error
        // oldugu icin bu senaryoyu uctan uca deneyemiyordum.
        Assert.Equal(ExitCodes.FindingsFound, CommandRunner.Run(["check", WarningDirectory]));
        Assert.Equal(ExitCodes.Clean, CommandRunner.Run(["check", WarningDirectory, "--fail-on", "error"]));
    }

    [Fact]
    public void ABelowThresholdFinding_IsStillPrinted()
    {
        StringWriter output = new();
        Console.SetOut(output);

        CommandRunner.Run(["check", WarningDirectory, "--fail-on", "error"]);

        // "Gormezden gelmek" ile "gizlemek" ayni sey degil (ADR 0006).
        Assert.Contains("SV002", output.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void CheckJson_CarriesExemptionsFromTheRule()
    {
        StringWriter output = new();
        Console.SetOut(output);

        Assert.Equal(ExitCodes.Clean, CommandRunner.Run(["check", ExemptDirectory, "--json"]));
        Assert.Contains("\"exemptions\"", output.ToString(), StringComparison.Ordinal);
        Assert.Contains("\"OnSaved\"", output.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void Check_CleanCode_ReturnsClean()
    {
        Assert.Equal(ExitCodes.Clean, CommandRunner.Run(["check", CleanDirectory]));
    }

    [Fact]
    public void Check_WithFindings_ReturnsFindingsFound()
    {
        Assert.Equal(ExitCodes.FindingsFound, CommandRunner.Run(["check", FindingDirectory]));
    }

    [Fact]
    public void Check_FailOnErrorStillBreaksOnAnErrorFinding()
    {
        // SV001 hata seviyesinde, yani en yuksek esikte bile build'i kiriyor.
        // Esigin altinda kalan bulgu durumunu CheckCommand testleri kapsiyor.
        Assert.Equal(ExitCodes.FindingsFound, CommandRunner.Run(["check", FindingDirectory, "--fail-on", "error"]));
    }

    [Fact]
    public void Check_MissingPath_ReturnsToolError()
    {
        Assert.Equal(ExitCodes.ToolError, CommandRunner.Run(["check", Path.Combine(_root, "boyle-bir-yol-yok")]));
    }

    [Fact]
    public void Check_InvalidFlag_ReturnsToolError()
    {
        Assert.Equal(ExitCodes.ToolError, CommandRunner.Run(["check", CleanDirectory, "--yanlis"]));
    }

    [Fact]
    public void Check_InvalidFailOnLevel_ReturnsToolError()
    {
        Assert.Equal(ExitCodes.ToolError, CommandRunner.Run(["check", CleanDirectory, "--fail-on", "olumcul"]));
    }

    [Fact]
    public void Check_JsonOutputUsesTheSameExitCode()
    {
        Assert.Equal(ExitCodes.FindingsFound, CommandRunner.Run(["check", FindingDirectory, "--json"]));
        Assert.Equal(ExitCodes.Clean, CommandRunner.Run(["check", CleanDirectory, "--json"]));
    }

    [Fact]
    public void Scan_ReturnsCleanEvenWhenTheCodeHasProblems()
    {
        // scan kural calistirmiyor, bulgu uretemez.
        Assert.Equal(ExitCodes.Clean, CommandRunner.Run(["scan", FindingDirectory]));
    }

    [Fact]
    public void Scan_MissingPath_ReturnsToolError()
    {
        Assert.Equal(ExitCodes.ToolError, CommandRunner.Run(["scan", Path.Combine(_root, "boyle-bir-yol-yok")]));
    }

    [Fact]
    public void DirectoryWithoutCSharpFiles_ReturnsToolError()
    {
        Assert.Equal(ExitCodes.ToolError, CommandRunner.Run(["check", EmptyDirectory]));
    }

    [Fact]
    public void UnknownCommand_ReturnsToolError()
    {
        Assert.Equal(ExitCodes.ToolError, CommandRunner.Run(["bilinmeyen", CleanDirectory]));
    }

    [Fact]
    public void NoArguments_PrintsTheBannerAndReturnsClean()
    {
        Assert.Equal(ExitCodes.Clean, CommandRunner.Run([]));
    }
}
