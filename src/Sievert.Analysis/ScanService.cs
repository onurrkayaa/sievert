using Sievert.Analysis.Rules;
using Sievert.Core.Configuration;
using Sievert.Core.Rules;

namespace Sievert.Analysis;

/// <summary>Tarama sirasindaki asamalar. Ilerleme cubugunun yanindaki metin bunlar.</summary>
public static class ScanPhase
{
    public const string DiscoveringFiles = "discovering-files";

    public const string Scanning = "scanning";

    public const string SavingResults = "saving-results";
}

/// <summary>Taramanin o anki durumu.</summary>
/// <param name="Phase">Hangi asamada.</param>
/// <param name="Processed">Islenen dosya sayisi.</param>
/// <param name="Total">Toplam dosya sayisi; kesif bitmeden null.</param>
public sealed record ScanProgress(string Phase, int Processed, int? Total);

/// <summary>
/// Bir taramanin sonucu. Basarisizlik istisna degil, alan: cagiran taraf konsola mi
/// yazacak yoksa HTTP cevabi mi uretecek kendi karar veriyor.
/// </summary>
public sealed record ScanOutcome(
    string? Error,
    string Root,
    IReadOnlyList<Finding> Findings,
    IReadOnlyList<Exemption> Exemptions,
    IReadOnlyList<Suppression> Suppressions,
    CheckSummary Summary,
    IReadOnlyList<GlobPattern> UnmatchedPatterns)
{
    public bool Ok => Error is null;

    public static ScanOutcome Failed(string error) =>
        new(error, string.Empty, [], [], [], CheckSummary.Of(0, []), []);
}

/// <summary>
/// <c>check</c> taramasinin tamami: dosya bulma, yapilandirma, eleme, kural secimi ve
/// kural calistirma.
///
/// Bu sinif Asama 6 Adim 3'te CLI'dan cikarildi. Sebebi dogrudan: arka plan isi de ayni
/// taramayi yapacak ve iki ayri kopya zamanla ayrisir. Ayrisirsa "panel ile CLI farkli
/// sey soyluyor" diye bir kusur cikar ve hangisinin dogru oldugu bilinmez. CLI process'i
/// baslatmak da bir secenekti; onu da istemedim, cunku o zaman API'nin sonucu baska bir
/// surecin cikti bicimini ayristirmaya baglanirdi.
/// </summary>
public static class ScanService
{
    /// <summary>Iki ilerleme raporu arasinda islenen dosya sayisi.</summary>
    public const int ProgressBatchSize = 25;

    /// <summary>
    /// Taramayi calistirir.
    /// </summary>
    /// <param name="targetPath">Taranacak klasor ya da dosya.</param>
    /// <param name="configPath">Acikca verilen yapilandirma dosyasi; yoksa kokte aranir.</param>
    /// <param name="commandLineExcludes">Komut satirindan gelen eleme kaliplari.</param>
    /// <param name="progress">Ilerleme bildirimi; CLI icin null.</param>
    /// <param name="cancellation">Iptal jetonu; dosya oberklerinin sinirinda gozleniyor.</param>
    public static ScanOutcome Run(
        string targetPath,
        string? configPath = null,
        IReadOnlyList<GlobPattern>? commandLineExcludes = null,
        IProgress<ScanProgress>? progress = null,
        CancellationToken cancellation = default)
    {
        progress?.Report(new ScanProgress(ScanPhase.DiscoveringFiles, 0, null));

        SourceFileSearch search = SourceFileFinder.Search(targetPath);

        if (search.Files.Count == 0)
        {
            return ScanOutcome.Failed($"Taranacak .cs dosyasi yok: {targetPath}");
        }

        // Kok elemeden once hesaplaniyor: kaliplar goreli yola uygulaniyor.
        string root = ScanRoot.Find(targetPath);

        ConfigLoadResult configResult = configPath is not null
            ? ConfigLoader.LoadFile(configPath)
            : ConfigLoader.LoadFromRoot(root);

        if (configResult.Config is not SievertConfig config)
        {
            return ScanOutcome.Failed(configResult.Error ?? "Yapilandirma okunamadi.");
        }

        if (Merge(commandLineExcludes ?? [], config.Exclude) is not IReadOnlyList<GlobPattern> patterns)
        {
            return ScanOutcome.Failed($"{ConfigLoader.FileName} icinde gecersiz eleme kalibi var.");
        }

        ExcludeResult excluded = ExcludeFilter.Apply(search.Files, root, patterns);
        int excludedCount = search.Files.Count - excluded.Files.Count;

        if (excluded.Files.Count == 0)
        {
            // Sessizce "temiz" demek tehlikeli olurdu: hicbir sey taranmadigi halde
            // sorun yokmus gibi gorunurdu.
            return ScanOutcome.Failed($"Eleme kaliplari butun dosyalari cikardi ({excludedCount} dosya).");
        }

        RuleSelectionResult selectionResult = RuleCatalog.Select(config);

        if (selectionResult.Selection is not RuleSelection selection)
        {
            return ScanOutcome.Failed(selectionResult.Error ?? "Kural secimi yapilamadi.");
        }

        IReadOnlyList<string> skipped = [.. search.SkippedDirectories
            .Select(directory => ScanRoot.RelativePath(directory, root))];

        cancellation.ThrowIfCancellationRequested();

        RuleResult result = RunInBatches(
            selection,
            excluded.Files,
            root,
            progress,
            cancellation);

        IReadOnlyList<Finding> findings = selection.ApplySeverity(result.Findings);

        return new ScanOutcome(
            null,
            root,
            findings,
            result.Exemptions,
            result.Suppressions,
            CheckSummary.Of(
                excluded.Files.Count,
                findings,
                excludedCount,
                skipped,
                new RuleUsage([.. selection.Enabled.Select(rule => rule.Code)], selection.DisabledCodes),
                result.Suppressions.Count),
            excluded.UnmatchedPatterns);
    }

    /// <summary>
    /// Kurallari dosya obekleri halinde calistirir.
    ///
    /// Obekleme sonucu DEGISTIRMIYOR: her dosyanin baglami butun kumeden bir kez
    /// kuruluyor (klasor komsulari dahil), kurallar da dosya dosya calisiyor. Obek
    /// yalnizca ilerleme bildirmek ve iptali gozlemek icin bir duraklama noktasi.
    /// </summary>
    private static RuleResult RunInBatches(
        RuleSelection selection,
        IReadOnlyList<string> files,
        string root,
        IProgress<ScanProgress>? progress,
        CancellationToken cancellation)
    {
        IReadOnlyList<RuleContext> contexts = ScannedFileSet.Contexts(ScannedFileSet.Parse(files, root));
        RuleRunner runner = new(selection.Enabled);

        progress?.Report(new ScanProgress(ScanPhase.Scanning, 0, contexts.Count));

        List<Finding> findings = [];
        List<Exemption> exemptions = [];
        List<Suppression> suppressions = [];

        for (int start = 0; start < contexts.Count; start += ProgressBatchSize)
        {
            cancellation.ThrowIfCancellationRequested();

            int size = Math.Min(ProgressBatchSize, contexts.Count - start);
            RuleResult batch = runner.Run([.. contexts.Skip(start).Take(size)]);

            findings.AddRange(batch.Findings);
            exemptions.AddRange(batch.Exemptions);
            suppressions.AddRange(batch.Suppressions);

            progress?.Report(new ScanProgress(ScanPhase.Scanning, start + size, contexts.Count));
        }

        return new RuleResult(findings, exemptions) { Suppressions = suppressions };
    }

    /// <summary>
    /// Komut satiri ve dosya kaliplarini birlestirir; biri digerini ezmiyor. Ezme olsaydi
    /// dosyaya yazilmis bir kalip tek bir komut satiri kalibiyla sessizce kaybolurdu.
    /// </summary>
    private static IReadOnlyList<GlobPattern>? Merge(
        IReadOnlyList<GlobPattern> fromCommandLine,
        IReadOnlyList<string> fromConfig)
    {
        List<GlobPattern> merged = [.. fromCommandLine];

        foreach (string text in fromConfig)
        {
            if (GlobPattern.TryParse(text) is not GlobPattern pattern)
            {
                return null;
            }

            merged.Add(pattern);
        }

        return merged;
    }
}
