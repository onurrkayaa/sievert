using System.Reflection;
using System.Runtime.InteropServices;

using Sievert.Analysis;
using Sievert.Analysis.Rules;
using Sievert.Core;
using Sievert.Core.Analysis;
using Sievert.Core.Configuration;
using Sievert.Core.Mining;
using Sievert.Core.Rules;
using Microsoft.EntityFrameworkCore;

using Sievert.Data;
using Sievert.Data.Entities;
using Sievert.Data.Metrics;
using Sievert.Mining;

namespace Sievert.Cli;

/// <summary>
/// Argumanlari alip isi yurutur ve cikis kodunu dondurur. Program.cs sadece burayi cagiriyor;
/// boylece cikis kodlari testten dogrulanabiliyor.
/// </summary>
public static class CommandRunner
{
    public static int Run(string[] args)
    {
        try
        {
            return Dispatch(args);
        }
        catch (Exception error)
        {
            // Beklenmeyen her sey arac hatasi sayilir; bulgu varmis gibi gorunmesin.
            Console.Error.WriteLine($"Beklenmeyen hata: {error.Message}");
            return ExitCodes.ToolError;
        }
    }

    private static int Dispatch(string[] args)
    {
        ParseResult result = ArgumentParser.Parse(args);

        if (result.Options is null)
        {
            return args.Length == 0 ? WriteBanner() : WriteUsageError(result.Error);
        }

        CommandOptions options = result.Options;

        // metrics bir yol almiyor, veritabanindaki bir depo adi aliyor; yol kontrolu
        // ondan once gelemez.
        if (options is MetricsOptions metrics)
        {
            return RunMetrics(metrics);
        }

        if (!File.Exists(options.TargetPath) && !Directory.Exists(options.TargetPath))
        {
            Console.Error.WriteLine($"Bulunamadi: {options.TargetPath}");
            return ExitCodes.ToolError;
        }

        // mine kod taramiyor, git tarihini okuyor. Dosya arama ve eleme kalibi ona
        // uygulanmadigi icin akis burada ayriliyor.
        if (options is MineOptions mine)
        {
            return RunMine(mine);
        }

        SourceFileSearch search = SourceFileFinder.Search(options.TargetPath);

        if (search.Files.Count == 0)
        {
            Console.Error.WriteLine($"Taranacak .cs dosyasi yok: {options.TargetPath}");
            return ExitCodes.ToolError;
        }

        // Kok, eleme kaliplari goreli yola uygulanacagi icin elemeden once hesaplaniyor.
        // Yapilandirma da kokte aranyor, o yuzden o da burada okunuyor.
        string root = ScanRoot.Find(options.TargetPath);

        ConfigLoadResult configResult = options.ConfigPath is string configPath
            ? ConfigLoader.LoadFile(configPath)
            : ConfigLoader.LoadFromRoot(root);

        if (configResult.Config is not SievertConfig config)
        {
            Console.Error.WriteLine(configResult.Error);
            return ExitCodes.ToolError;
        }

        if (MergeExcludes(options.Exclude, config.Exclude) is not IReadOnlyList<GlobPattern> patterns)
        {
            return ExitCodes.ToolError;
        }

        ExcludeResult excluded = ExcludeFilter.Apply(search.Files, root, patterns);
        int excludedCount = search.Files.Count - excluded.Files.Count;

        WarnAboutUnmatchedPatterns(excluded.UnmatchedPatterns, root);

        if (excluded.Files.Count == 0)
        {
            // Sessizce 0 donmek tehlikeli olurdu: CI adimi hicbir sey taranmadigi halde
            // "temiz" derdi. Fazla eleyen bir kalip arac hatasi sayiliyor.
            Console.Error.WriteLine($"--exclude butun dosyalari eledi ({excludedCount} dosya): {options.TargetPath}");
            return ExitCodes.ToolError;
        }

        IReadOnlyList<string> skipped = search.SkippedDirectories
            .Select(directory => ScanRoot.RelativePath(directory, root))
            .ToList();

        return options switch
        {
            ScanOptions scan => RunScan(scan, excluded.Files, root, excludedCount, skipped),
            CheckOptions check => RunCheck(check, excluded.Files, root, excludedCount, skipped, config),
            _ => throw new InvalidOperationException("Bilinmeyen komut turu."),
        };
    }

    /// <summary>
    /// --exclude ile sievert.json'daki exclude birlesir, biri digerini ezmez. Ezme olsaydi
    /// dosyaya yazilmis bir kalip komut satirindan tek bir --exclude verilince sessizce
    /// kaybolurdu. Gecersiz kalip burada da kullanim hatasi.
    /// </summary>
    private static IReadOnlyList<GlobPattern>? MergeExcludes(
        IReadOnlyList<GlobPattern> fromCommandLine,
        IReadOnlyList<string> fromConfig)
    {
        List<GlobPattern> merged = [.. fromCommandLine];

        foreach (string text in fromConfig)
        {
            if (GlobPattern.TryParse(text) is not GlobPattern pattern)
            {
                Console.Error.WriteLine($"{ConfigLoader.FileName} icinde gecersiz kalip: {text}");
                return null;
            }

            merged.Add(pattern);
        }

        return merged;
    }

    /// <summary>
    /// Hicbir dosyayla eslesmeyen kalip icin uyarir. Uyari stderr'e gidiyor ki --json
    /// ciktisini bozmasin. Kalip var olan bir klasorun adiysa ne yazilmasi gerektigini
    /// de soyluyoruz, cunku en sik yapilan hata bu.
    /// </summary>
    private static void WarnAboutUnmatchedPatterns(IReadOnlyList<GlobPattern> unmatched, string root)
    {
        foreach (GlobPattern pattern in unmatched)
        {
            string hint = Directory.Exists(Path.Combine(root, pattern.Text))
                ? $" {pattern.Text} bir klasor; altindaki dosyalar icin {pattern.Text}/** dene."
                : string.Empty;

            Console.Error.WriteLine($"Uyari: --exclude kalibi hicbir dosyayla eslesmedi: {pattern.Text}.{hint}");
        }
    }

    private static int RunScan(
        ScanOptions options,
        IReadOnlyList<string> files,
        string root,
        int excludedCount,
        IReadOnlyList<string> skippedDirectories)
    {
        IReadOnlyList<FileAnalysis> analyses = ScanRoot.MakePathsRelative(
            files.Select(FileAnalyzer.AnalyzeFile).ToList(),
            root);

        ScanSummary summary = Summarizer.Summarize(analyses, excludedCount, skippedDirectories);
        IReadOnlyList<MethodLocation> longest = options.TopCount is int count
            ? Summarizer.LongestMethods(analyses, count)
            : [];

        if (options.Json)
        {
            Console.Out.WriteLine(JsonFormatter.Format(root, analyses, summary, longest));
            return ExitCodes.Clean;
        }

        ConsoleWriter.Write(
            TreeFormatter.Format(analyses, summary, longest),
            ConsoleWriter.UseColor());

        // scan kural calistirmiyor, o yuzden bulgu uretemez.
        return ExitCodes.Clean;
    }

    private static int RunCheck(
        CheckOptions options,
        IReadOnlyList<string> files,
        string root,
        int excludedCount,
        IReadOnlyList<string> skippedDirectories,
        SievertConfig config)
    {
        // Kural listesi RuleCatalog'da; burada sadece yapilandirmayla suzuluyor.
        RuleSelectionResult selectionResult = RuleCatalog.Select(config);

        if (selectionResult.Selection is not RuleSelection selection)
        {
            Console.Error.WriteLine(selectionResult.Error);
            return ExitCodes.ToolError;
        }

        RuleResult result = new RuleRunner(selection.Enabled).Run(files, root);

        // Seviye ezmesi bulgular uretildikten sonra uygulaniyor; --fail-on karsilastirmasi
        // da ezilmis seviyeyi goruyor, cunku asagida bu liste kullaniliyor.
        IReadOnlyList<Finding> findings = selection.ApplySeverity(result.Findings);

        CheckSummary summary = CheckSummary.Of(
            files.Count,
            findings,
            excludedCount,
            skippedDirectories,
            new RuleUsage(selection.Enabled.Select(rule => rule.Code).ToArray(), selection.DisabledCodes),
            result.Suppressions.Count);

        if (options.Json)
        {
            Console.Out.WriteLine(
                JsonFormatter.FormatCheck(root, findings, result.Exemptions, result.Suppressions, summary));
        }
        else
        {
            ConsoleWriter.Write(
                DiagnosticCardFormatter.Format(findings, summary),
                ConsoleWriter.UseColor());
        }

        return CheckCommand.ExitCode(findings, options.FailOn);
    }

    private static int RunMine(MineOptions options)
    {
        if (!RepositoryMiner.IsRepository(options.TargetPath))
        {
            Console.Error.WriteLine($"Git deposu degil: {options.TargetPath}");
            return ExitCodes.ToolError;
        }

        bool shallow = RepositoryMiner.IsShallow(options.TargetPath);

        if (shallow)
        {
            // Cikis kodunu degistirmiyorum: komut calisti ve elindeki tarihi dogru okudu.
            // Ama okudugu tarih deponun tamami degil, bunu soylemeden gecmek olcumu bozar.
            Console.Error.WriteLine(
                "Uyari: bu depo shallow (tarihi kesilmis). Okunan commit'ler deponun tamami degil, "
                + "ozetteki ilk commit tarihi de gercek ilk commit degil. Tam tarih icin "
                + "'git fetch --unshallow' calistir.");
        }

        using SievertContext? context = options.Database ? OpenDatabase() : null;

        if (options.Database && context is null)
        {
            return ExitCodes.ToolError;
        }

        RepositoryIdentity identity = RepositoryMiner.Identify(options.TargetPath);

        MineResult result = MineCommand.Run(
            options.TargetPath,
            new MiningOptions(options.Since, options.MaxCommits),
            options.OutputPath,
            context is null ? null : new CommitStore(context),
            context is null
                ? null
                : StoreOptionsFor(identity, options.Rewrite));

        ConsoleWriter.Write(
            MineFormatter.Format(result.Summary, result.Elapsed, options.OutputPath, shallow, result.Store),
            ConsoleWriter.UseColor());

        // mine kural calistirmiyor, o yuzden bulgu uretemez; basariliysa hep 0 (ADR 0006).
        return ExitCodes.Clean;
    }

    private static int RunMetrics(MetricsOptions options)
    {
        using SievertContext? context = OpenDatabase();

        if (context is null)
        {
            return ExitCodes.ToolError;
        }

        MetricsRunner runner = new(context);

        if (runner.FindRepository(options.TargetPath) is not RepositoryRow repository)
        {
            Console.Error.WriteLine(
                $"Veritabaninda boyle bir depo yok: {options.TargetPath}. "
                + "Once 'sievert mine <repo-yolu> --db' calistir.");

            return ExitCodes.ToolError;
        }

        MetricDistribution distribution = new();
        System.Diagnostics.Stopwatch clock = System.Diagnostics.Stopwatch.StartNew();

        MetricsResult result = runner.Run(repository.Id, MetricOptions.Default, distribution.Add);

        clock.Stop();

        if (options.OutputPath is string path)
        {
            File.WriteAllText(path, MetricsJsonFormatter.Distribution(repository.Name, distribution));
        }

        ConsoleWriter.Write(
            MetricsFormatter.Format(repository.Name, result, clock.Elapsed, options.OutputPath),
            ConsoleWriter.UseColor());

        // metrics kural calistirmiyor, bulgu uretemez; basariliysa hep 0 (ADR 0006).
        return ExitCodes.Clean;
    }

    /// <summary>
    /// Depo kimligini uzak adresten turetir. Uzak adres yoksa klasor adina dusuluyor ve
    /// bu durum kayda geciyor: klasor adiyla eslestirme ciftlenmeye acik, gorunur olsun.
    /// </summary>
    private static StoreOptions StoreOptionsFor(RepositoryIdentity identity, bool rewrite) =>
        RemoteIdentity.Normalize(identity.RemoteUrl) is string fromRemote
            ? new StoreOptions(fromRemote, "remote", identity.Name, identity.RemoteUrl, identity.HeadSha, rewrite)
            : new StoreOptions(identity.Name, "folder", identity.Name, null, identity.HeadSha, rewrite);

    /// <summary>
    /// Baglanti dizesini bulup baglami acar. Sema eksikse migration'i kendiliginden
    /// uygulamiyorum: baskasinin veritabaninda sessizce sema degistirmek, aracin
    /// yapmasi gereken bir sey degil. Ne calistirilacagini yaziyorum, karar kullanicinin.
    /// </summary>
    private static SievertContext? OpenDatabase()
    {
        ConnectionStringResult connection = ConnectionString.Find(Directory.GetCurrentDirectory());

        if (connection.Value is not string value)
        {
            Console.Error.WriteLine(connection.Error);
            return null;
        }

        SievertContext context = SievertContextBuilder.Create(value);

        try
        {
            if (context.Database.GetPendingMigrations().Any())
            {
                Console.Error.WriteLine(
                    "Veritabani semasi guncel degil. Once su komutu calistir: "
                    + "dotnet dotnet-ef database update --project src/Sievert.Data");
                context.Dispose();

                return null;
            }
        }
        catch (Exception error)
        {
            Console.Error.WriteLine($"Veritabanina baglanilamadi ({connection.Source}): {error.Message}");
            context.Dispose();

            return null;
        }

        return context;
    }

    private static int WriteBanner()
    {
        string version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.0.0";

        ConsoleWriter.Write(
            Banner.Render(version, RuntimeInformation.FrameworkDescription)
                .Select(line => new OutputLine([new OutputSpan(line.Text, line.IsTitle ? OutputColor.Heading : OutputColor.Dim)]))
                .ToList(),
            ConsoleWriter.UseColor());

        Console.WriteLine();
        Console.WriteLine(ArgumentParser.HelpText);

        return ExitCodes.Clean;
    }

    private static int WriteUsageError(string? error)
    {
        Console.Error.WriteLine(error);
        Console.Error.WriteLine();
        Console.Error.WriteLine(ArgumentParser.HelpText);

        return ExitCodes.ToolError;
    }
}
