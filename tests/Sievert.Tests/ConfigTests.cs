using System.Text.Json;

using Sievert.Analysis.Rules;
using Sievert.Cli;
using Sievert.Core.Configuration;
using Sievert.Core.Rules;

using static Sievert.Tests.Samples;

namespace Sievert.Tests;

public class ConfigLoaderTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "sievert-config-" + Guid.NewGuid().ToString("N"));

    public ConfigLoaderTests() => Directory.CreateDirectory(_root);

    public void Dispose()
    {
        Directory.Delete(_root, recursive: true);
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void NoFile_IsNotAnError_AndGivesTheDefaults()
    {
        ConfigLoadResult result = ConfigLoader.LoadFromRoot(_root);

        Assert.Null(result.Error);
        Assert.Equal(SievertConfig.Default, result.Config);
    }

    [Fact]
    public void AFullFile_IsRead()
    {
        Write("""
            {
              "rules": [
                { "code": "SV001", "enabled": true, "severity": "warning" }
              ],
              "exclude": ["samples/**"]
            }
            """);

        SievertConfig config = ConfigLoader.LoadFromRoot(_root).Config!;

        Assert.Equal("SV001", config.Rules[0].Code);
        Assert.True(config.Rules[0].Enabled);
        Assert.Equal(Severity.Warning, config.Rules[0].Severity);
        Assert.Equal(["samples/**"], config.Exclude);
    }

    [Fact]
    public void EnabledDefaultsToTrue_WhenLeftOut()
    {
        Write("""{ "rules": [ { "code": "SV001" } ] }""");

        Assert.True(ConfigLoader.LoadFromRoot(_root).Config!.Rules[0].Enabled);
        Assert.Null(ConfigLoader.LoadFromRoot(_root).Config!.Rules[0].Severity);
    }

    [Fact]
    public void BrokenJson_SaysWhichLine()
    {
        Write("""
            {
              "rules": [
                { "code": "SV001", }
            }
            """);

        string error = ConfigLoader.LoadFromRoot(_root).Error!;

        Assert.Contains("sievert.json", error, StringComparison.Ordinal);
        Assert.Contains("satir", error, StringComparison.Ordinal);
    }

    [Fact]
    public void AnUnknownProperty_IsAnErrorNotASilentNoOp()
    {
        // "excludes" yazan biri hicbir sey dislanmadigini fark etmezdi.
        Write("""{ "excludes": ["samples/**"] }""");

        Assert.NotNull(ConfigLoader.LoadFromRoot(_root).Error);
    }

    [Fact]
    public void ARuleWithoutACode_IsAnError()
    {
        Write("""{ "rules": [ { "enabled": false } ] }""");

        Assert.NotNull(ConfigLoader.LoadFromRoot(_root).Error);
    }

    [Fact]
    public void AnExplicitPathThatDoesNotExist_IsAnError()
    {
        // Kok'teki dosyanin olmamasi normal, ama --config ile yol verildiyse bulunmali.
        Assert.NotNull(ConfigLoader.LoadFile(Path.Combine(_root, "yok.json")).Error);
    }

    private void Write(string json) => File.WriteAllText(Path.Combine(_root, ConfigLoader.FileName), json);
}

public class RuleCatalogTests
{
    [Fact]
    public void WithoutConfig_OnlyTheDefaultSetRuns()
    {
        // Varsayilan kume "her sey acik" degil: olculen precision'a gore secildi.
        RuleSelection selection = RuleCatalog.Select(SievertConfig.Default).Selection!;

        // SV007 de varsayilan kumede: susturmalari denetleyen kural, susturulacak
        // bulgulari ureten kurallardan once kapatilamamali.
        Assert.Equal(
            ["SV001", "SV002", "SV004", "SV006", "SV007"],
            selection.Enabled.Select(rule => rule.Code).ToArray());
        Assert.Equal(RuleCatalog.DefaultOffCodes, selection.DisabledCodes);
    }

    [Fact]
    public void ADefaultOffRule_CanBeTurnedOn()
    {
        RuleSelection selection = RuleCatalog
            .Select(new SievertConfig([new RuleSetting("SV003")], []))
            .Selection!;

        Assert.Contains(selection.Enabled, rule => rule.Code == "SV003");
        Assert.DoesNotContain("SV003", selection.DisabledCodes);
    }

    [Fact]
    public void ADefaultOffRule_StaysOffIfNotMentioned()
    {
        RuleSelection selection = RuleCatalog
            .Select(new SievertConfig([new RuleSetting("SV001")], []))
            .Selection!;

        Assert.DoesNotContain(selection.Enabled, rule => rule.Code == "SV005");
    }

    [Fact]
    public void ADefaultOnRule_CanStillBeTurnedOff()
    {
        RuleSelection selection = RuleCatalog
            .Select(new SievertConfig([new RuleSetting("SV001", Enabled: false)], []))
            .Selection!;

        Assert.DoesNotContain(selection.Enabled, rule => rule.Code == "SV001");
        Assert.Contains("SV001", selection.DisabledCodes);
    }

    [Fact]
    public void EnabledFalse_TakesTheRuleOutAndRecordsIt()
    {
        RuleSelection selection = Select(new RuleSetting("SV001", Enabled: false)).Selection!;

        Assert.DoesNotContain(selection.Enabled, rule => rule.Code == "SV001");
        // Varsayilan kapali olanlar da listede: kapali kume = varsayilanlar + acikca kapatilan.
        Assert.Equal(["SV001", "SV003", "SV005"], selection.DisabledCodes);
    }

    [Fact]
    public void AnUnknownCode_IsAnErrorThatNamesTheCode()
    {
        RuleSelectionResult result = Select(new RuleSetting("SV999"));

        Assert.Null(result.Selection);
        Assert.Contains("SV999", result.Error!, StringComparison.Ordinal);
        Assert.Contains("SV001", result.Error!, StringComparison.Ordinal);
    }

    [Fact]
    public void Severity_CanBeOverridden()
    {
        RuleSelection selection = Select(new RuleSetting("SV001", Severity: Severity.Info)).Selection!;

        IReadOnlyList<Finding> rewritten = selection.ApplySeverity([Found(severity: Severity.Error)]);

        Assert.Equal(Severity.Info, rewritten[0].Severity);
    }

    [Fact]
    public void WithoutAnOverride_SeverityIsLeftAlone()
    {
        RuleSelection selection = RuleCatalog.Select(SievertConfig.Default).Selection!;

        Assert.Equal(Severity.Error, selection.ApplySeverity([Found(severity: Severity.Error)])[0].Severity);
    }

    private static RuleSelectionResult Select(params RuleSetting[] rules) =>
        RuleCatalog.Select(new SievertConfig(rules, []));
}

/// <summary>Yapilandirmanin komut seviyesinde ucundan ucuna calistigini gosteren testler.</summary>
[Collection(ConsoleCollection.Name)]
public class ConfigEndToEndTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "sievert-e2e-" + Guid.NewGuid().ToString("N"));
    private readonly TextWriter _stdout = Console.Out;
    private readonly TextWriter _stderr = Console.Error;

    public ConfigEndToEndTests()
    {
        Directory.CreateDirectory(Path.Combine(_root, "src"));
        Directory.CreateDirectory(Path.Combine(_root, "samples"));

        File.WriteAllText(Path.Combine(_root, "src", "Hasta.cs"), """
            using System.Threading.Tasks;

            public class Hasta
            {
                public async void Calis()
                {
                    await Task.CompletedTask;
                }
            }
            """);
        File.WriteAllText(Path.Combine(_root, "samples", "Ornek.cs"), "public class Ornek { }");

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
    public void NoConfigFile_TheDefaultsRun()
    {
        // Yapilandirma opsiyonel: dosya yokken arac hata vermeden calisiyor ve
        // butun kurallar etkin gorunuyor.
        using JsonDocument json = JsonDocument.Parse(Capture(["check", _root, "--json"]));

        Assert.Equal(ExitCodes.FindingsFound, CommandRunner.Run(["check", _root]));
        JsonElement rules = json.RootElement.GetProperty("summary").GetProperty("rules");

        Assert.Equal(
            ["SV001", "SV002", "SV004", "SV006", "SV007"],
            rules.GetProperty("activeCodes").EnumerateArray().Select(code => code.GetString()!).ToArray());
        Assert.Equal(
            RuleCatalog.DefaultOffCodes,
            rules.GetProperty("disabledCodes").EnumerateArray().Select(code => code.GetString()!).ToArray());
    }

    [Fact]
    public void EnabledFalse_TheRuleDoesNotRun()
    {
        WriteConfig("""{ "rules": [ { "code": "SV001", "enabled": false } ] }""");

        using JsonDocument json = JsonDocument.Parse(Capture(["check", _root, "--json"]));
        JsonElement rules = json.RootElement.GetProperty("summary").GetProperty("rules");

        Assert.Empty(json.RootElement.GetProperty("findings").EnumerateArray());
        Assert.Equal(
            ["SV001", "SV003", "SV005"],
            rules.GetProperty("disabledCodes").EnumerateArray().Select(code => code.GetString()!).ToArray());
        Assert.DoesNotContain(
            "SV001",
            rules.GetProperty("activeCodes").EnumerateArray().Select(code => code.GetString()!).ToArray());
        Assert.Equal(ExitCodes.Clean, CommandRunner.Run(["check", _root]));
    }

    [Fact]
    public void SeverityOverride_ChangesTheFindingAndTheExitCode()
    {
        // SV001 kodda error. info'ya cekilince varsayilan esik olan warning'in altina
        // dusuyor ve build kirilmiyor; esik info'ya indirilince yine kiriliyor.
        WriteConfig("""{ "rules": [ { "code": "SV001", "severity": "info" } ] }""");

        using JsonDocument json = JsonDocument.Parse(Capture(["check", _root, "--json"]));

        Assert.Equal("info", json.RootElement.GetProperty("findings")[0].GetProperty("severity").GetString());
        Assert.Equal(ExitCodes.Clean, CommandRunner.Run(["check", _root]));
        Assert.Equal(ExitCodes.FindingsFound, CommandRunner.Run(["check", _root, "--fail-on", "info"]));
    }

    [Fact]
    public void SeverityOverrideUpwards_AlsoWorks()
    {
        WriteConfig("""{ "rules": [ { "code": "SV001", "severity": "warning" } ] }""");

        Assert.Equal(ExitCodes.FindingsFound, CommandRunner.Run(["check", _root]));
        Assert.Equal(ExitCodes.Clean, CommandRunner.Run(["check", _root, "--fail-on", "error"]));
    }

    [Fact]
    public void AnUnknownRuleCode_IsAToolError()
    {
        WriteConfig("""{ "rules": [ { "code": "SV404", "enabled": false } ] }""");

        Assert.Equal(ExitCodes.ToolError, CommandRunner.Run(["check", _root]));
    }

    [Fact]
    public void BrokenJson_IsAToolError()
    {
        WriteConfig("""{ "rules": [ { "code": "SV001" ] }""");

        Assert.Equal(ExitCodes.ToolError, CommandRunner.Run(["check", _root]));
    }

    [Fact]
    public void ExcludeFromConfigAndCommandLine_AreBothApplied()
    {
        WriteConfig("""{ "exclude": ["samples/**"] }""");

        // Dosyadaki kalip samples'i, komut satirindaki src'yi eliyor. Biri digerini
        // ezseydi geriye bir dosya kalirdi; ikisi birlestigi icin hicbir sey kalmiyor.
        Assert.Equal(1, ExcludedCount(["check", _root, "--json"]));
        Assert.Equal(ExitCodes.ToolError, CommandRunner.Run(["check", _root, "--exclude", "src/**"]));
    }

    [Fact]
    public void ConfigPath_CanBeGivenExplicitly()
    {
        string elsewhere = Path.Combine(_root, "baska.json");
        File.WriteAllText(elsewhere, """{ "rules": [ { "code": "SV001", "enabled": false } ] }""");

        Assert.Equal(ExitCodes.Clean, CommandRunner.Run(["check", _root, "--config", elsewhere]));
    }

    [Fact]
    public void ConfigPathThatDoesNotExist_IsAToolError() =>
        Assert.Equal(ExitCodes.ToolError, CommandRunner.Run(["check", _root, "--config", Path.Combine(_root, "yok.json")]));

    private int ExcludedCount(string[] args)
    {
        using JsonDocument json = JsonDocument.Parse(Capture(args));

        return json.RootElement.GetProperty("summary").GetProperty("excludedFileCount").GetInt32();
    }

    private void WriteConfig(string json) => File.WriteAllText(Path.Combine(_root, ConfigLoader.FileName), json);

    private static string Capture(string[] args)
    {
        StringWriter output = new();
        Console.SetOut(output);

        CommandRunner.Run(args);

        return output.ToString();
    }
}

/// <summary>Varsayilan kume daraltildi; bunun uctan uca gorunur oldugunu dogrulayan testler.</summary>
[Collection(ConsoleCollection.Name)]
public class DefaultRuleSetTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "sievert-varsayilan-" + Guid.NewGuid().ToString("N"));
    private readonly TextWriter _stdout = Console.Out;
    private readonly TextWriter _stderr = Console.Error;

    public DefaultRuleSetTests()
    {
        Directory.CreateDirectory(_root);

        // Icinde sadece SV003 (kayip gorev) ve SV005 (atilmayan nesne) bulgusu olan bir dosya.
        File.WriteAllText(Path.Combine(_root, "Kapali.cs"), """
            using System.IO;
            using System.Threading.Tasks;

            public class Kapali
            {
                public void Calis()
                {
                    GonderAsync();
                    var akis = new MemoryStream();
                }

                private Task GonderAsync() => Task.CompletedTask;
            }
            """);

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
    public void ByDefault_SV003AndSV005DoNotRun()
    {
        using JsonDocument json = JsonDocument.Parse(Capture(["check", _root, "--json"]));

        Assert.Empty(json.RootElement.GetProperty("findings").EnumerateArray());
        Assert.Equal(ExitCodes.Clean, CommandRunner.Run(["check", _root]));
    }

    [Fact]
    public void TurnedOnInTheConfigFile_TheyRun()
    {
        File.WriteAllText(
            Path.Combine(_root, ConfigLoader.FileName),
            """{ "rules": [ { "code": "SV003" }, { "code": "SV005" } ] }""");

        using JsonDocument json = JsonDocument.Parse(Capture(["check", _root, "--json"]));

        Assert.Equal(
            ["SV003", "SV005"],
            json.RootElement.GetProperty("findings").EnumerateArray()
                .Select(finding => finding.GetProperty("ruleCode").GetString()!)
                .Distinct().Order(StringComparer.Ordinal).ToArray());
    }

    [Fact]
    public void TheScreenSummary_NamesTheRulesThatAreOff()
    {
        // Sessiz yapilandirma tehlikeli: kapatilan kural ciktida gorunmeli.
        Assert.Contains("Kapali kural   : SV003, SV005", Capture(["check", _root]), StringComparison.Ordinal);
    }

    private static string Capture(string[] args)
    {
        StringWriter output = new();
        Console.SetOut(output);

        CommandRunner.Run(args);

        return output.ToString();
    }
}
