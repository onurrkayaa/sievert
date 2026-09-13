using System.Text.Json;

using Microsoft.EntityFrameworkCore;

using Sievert.Analysis;
using Sievert.Contracts;
using Sievert.Core.Rules;
using Sievert.Data;
using Sievert.Data.Entities;
using Sievert.Mining;

namespace Sievert.Api.Analysis;

/// <summary>
/// Reponun calisma agacini SV kurallariyla tarar.
///
/// Taranacak yol **istekten gelmiyor**, veritabanindaki <c>Repository.LocalPath</c>'ten
/// geliyor. Istekten yol almak, HTTP'si acik olan herkese sunucudaki herhangi bir
/// klasoru okutmak olurdu; kimlik dogrulama da yok.
///
/// Tarama CLI ile **ayni servisten** geciyor (<see cref="ScanService"/>). CLI'yi ayri
/// bir surec olarak baslatmiyoruz.
///
/// Tarama diskteki dosyalari okuyor, veritabanindaki commit tarihini degil. O yuzden
/// hangi surumun tarandigi is kaydina yaziliyor ve tarama yalnizca **temiz** bir calisma
/// agacinda basliyor: kaydedilen SHA diskteki dosyalari anlatmiyorsa sonuc tekrar
/// uretilemez.
/// </summary>
public sealed class StaticScanHandler(
    SievertContext context,
    AnalysisJobStore jobs,
    AnalysisOptions options,
    TimeProvider clock) : IAnalysisJobHandler
{
    public AnalysisJobKind Kind => AnalysisJobKind.StaticScan;

    /// <summary>
    /// Obek yaziminda takipcide gorulen en yuksek kayit sayisi. Olcumun sordugu soru:
    /// bulgu sayisiyla dogrusal buyuyor mu, yoksa obek boyutunda mi kaliyor.
    /// </summary>
    private int MaxTrackedEntries { get; set; }

    /// <summary>Temizlikten sonra takipcide kalan en yuksek kayit sayisi; tabana donuyor mu.</summary>
    private int MaxTrackedAfterClear { get; set; }

    public async Task<JobOutcome> RunAsync(AnalysisJobRun run, CancellationToken cancellation)
    {
        RepositoryRow? repository = await context.Repositories
            .AsNoTracking()
            .FirstOrDefaultAsync(row => row.Id == run.RepositoryId, cancellation);

        if (repository?.LocalPath is not string localPath || string.IsNullOrWhiteSpace(localPath))
        {
            return JobOutcome.Failed(
                ApiError.RepositoryPathUnavailable,
                "Bu depo icin yerel klasor kayitli degil; tarama yapilamadi.");
        }

        // Tam yol yalnizca burada, surec icinde kullaniliyor; cevaba ve gunluge girmiyor.
        string fullPath = Path.GetFullPath(localPath);

        if (!Directory.Exists(fullPath))
        {
            return JobOutcome.Failed(
                ApiError.RepositoryPathUnavailable,
                "Depo icin kayitli klasor bu makinede bulunamadi.");
        }

        WorktreeSnapshot before = WorktreeState.Read(fullPath);

        if (!before.IsGitRepository)
        {
            return JobOutcome.Failed(
                ApiError.RepositoryNotGit,
                "Kayitli klasor bir git deposu degil.");
        }

        DateTimeOffset checkedAt = clock.GetUtcNow();

        await jobs.RecordSourceStartAsync(
            run.JobId,
            before.State,
            before.HeadSha,
            before.ShortSha,
            before.Identity,
            before.DirtyFileCount,
            checkedAt,
            cancellation);

        if (before.HeadSha is null)
        {
            return JobOutcome.Failed(
                ApiError.RepositoryHeadUnavailable,
                "Deponun HEAD commit'i okunamadi; taranacak bir surum yok.");
        }

        if (!before.IsClean)
        {
            // Kirli agacta taramak calisir ama sonucu kimse tekrar uretemez: kaydedilen
            // SHA diskteki dosyalari anlatmiyor. Degisen dosyalarin adlari cevaba
            // girmiyor, yalniz sayilari.
            return JobOutcome.Failed(
                ApiError.RepositoryWorktreeDirty,
                $"Calisma agacinda kaydedilmemis {before.DirtyFileCount} degisiklik var. "
                + "Statik tarama yalniz temiz bir calisma agacinda calisir.");
        }

        // Tarama, isleyicinin gozunden tek ve bolunmez bir cagri: basladiktan sonraki
        // duraklama noktalari ScanService'in icinde. O yuzden iptal BASLAMADAN once de
        // soruluyor. Risk isinde bu gerekmiyor, orada her obek zaten bir duraklama noktasi.
        if (await run.Progress.IsCancellationRequestedAsync(cancellation))
        {
            return JobOutcome.Canceled(0, 0);
        }

        ScanRelay relay = new();

        using CancellationTokenSource linked = CancellationTokenSource.CreateLinkedTokenSource(cancellation);

        Task<ScanOutcome> scan = Task.Run(
            () => ScanService.Run(fullPath, progress: relay, cancellation: linked.Token),
            CancellationToken.None);

        bool canceled = await SuperviseAsync(run, relay, scan, linked, cancellation);

        ScanOutcome outcome;

        try
        {
            outcome = await scan;
        }
        catch (OperationCanceledException)
        {
            // Iptal edildi; hicbir bulgu yazilmadi, yani sonuc bos ve kismi.
            return JobOutcome.Canceled(0, relay.Snapshot().Processed);
        }

        if (canceled)
        {
            return JobOutcome.Canceled(0, relay.Snapshot().Processed);
        }

        if (!outcome.Ok)
        {
            return JobOutcome.Failed(ApiError.AnalysisFailed, outcome.Error!);
        }

        // Kaynak hala ayni mi. Tarama dakikalar surebiliyor ve bu sure icinde biri
        // checkout yapabilir; o zaman elimizdeki bulgular iki ayri agacin karisimi olur.
        WorktreeSnapshot after = WorktreeState.Read(fullPath);

        bool changed = !after.IsClean
            || !string.Equals(after.HeadSha, before.HeadSha, StringComparison.Ordinal);

        await jobs.RecordSourceVerifiedAsync(
            run.JobId,
            changed ? WorktreeState.ChangedDuringAnalysis : WorktreeState.Clean,
            changed,
            clock.GetUtcNow(),
            cancellation);

        await run.Progress.FlushAsync(
            ScanPhase.SavingResults,
            outcome.Summary.FileCount,
            outcome.Summary.FileCount,
            cancellation);

        // Bulgular degisiklik halinde de yaziliyor. Gercekten hesaplanmis satirlari atip
        // "0 sonuc" demek, Adim 3'te bir kez yaptigim hatanin aynisi olurdu; satirlar
        // duruyor ama is basarili sayilmiyor ve sonuc kismi isaretleniyor.
        int saved = await SaveAsync(run.JobId, outcome, cancellation);

        if (changed)
        {
            return JobOutcome.Failed(
                ApiError.RepositoryChangedDuringAnalysis,
                "Tarama sirasinda deponun HEAD'i ya da calisma agaci degisti; sonuc tam degil.",
                saved,
                outcome.Summary.FileCount);
        }

        return new JobOutcome(
            AnalysisJobStatus.Succeeded,
            saved,
            outcome.Summary.FileCount,
            ResultSummary: Summarise(outcome, run.Progress, before, MaxTrackedEntries, MaxTrackedAfterClear));
    }

    /// <summary>
    /// Tarama ayri bir is parcaciginda kosarken ilerlemeyi yazar ve iptali gozler.
    ///
    /// Tarama senkron bir hesap; ilerleme yazimi ise veritabani islemi. Ikisini ayirmanin
    /// sebebi bu: senkron geri cagirmanin icinde veritabani beklemek, ya bloke etmek ya
    /// da sync-over-async yazmak olurdu.
    /// </summary>
    private async Task<bool> SuperviseAsync(
        AnalysisJobRun run,
        ScanRelay relay,
        Task<ScanOutcome> scan,
        CancellationTokenSource linked,
        CancellationToken cancellation)
    {
        while (!scan.IsCompleted)
        {
            await Task.WhenAny(scan, Task.Delay(options.ProgressInterval, cancellation));

            (string phase, int processed, int? total) = relay.Snapshot();

            await run.Progress.ReportAsync(phase, processed, total, cancellation);

            if (!scan.IsCompleted && await run.Progress.IsCancellationRequestedAsync(cancellation))
            {
                // Tarama jetonu dosya obeklerinin sinirinda gozluyor.
                await linked.CancelAsync();

                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Bulgulari obekler halinde yazar.
    ///
    /// Susturulan bulgular yazilmiyor, sayiliyor: "susturuldu" ile "bulundu" ayni listede
    /// durursa bulgu sayisi sisirilmis olur.
    /// </summary>
    private async Task<int> SaveAsync(Guid jobId, ScanOutcome outcome, CancellationToken cancellation)
    {
        DateTimeOffset now = clock.GetUtcNow();
        int saved = 0;

        MaxTrackedEntries = 0;
        MaxTrackedAfterClear = 0;

        for (int start = 0; start < outcome.Findings.Count; start += options.FindingBatchSize)
        {
            List<StaticAnalysisFindingRow> batch = [];

            foreach (Finding finding in outcome.Findings.Skip(start).Take(options.FindingBatchSize))
            {
                batch.Add(new StaticAnalysisFindingRow
                {
                    AnalysisJobId = jobId,
                    RuleCode = finding.RuleCode,
                    Severity = finding.Severity.ToString().ToLowerInvariant(),

                    // ScanService yollari zaten tarama kokune gore goreli veriyor.
                    RelativePath = finding.FilePath,
                    Line = finding.Line,
                    Column = null,
                    MemberName = string.IsNullOrWhiteSpace(finding.MethodName) ? null : finding.MethodName,
                    Message = finding.Description,
                    Rationale = finding.Rationale,
                    IsTestCode = finding.IsTestCode,
                    IsSuppressed = false,
                    CreatedAtUtc = now,
                });
            }

            context.StaticAnalysisFindings.AddRange(batch);
            await context.SaveChangesAsync(cancellation);

            // sievert:disable SV004 ChangeTracker.Entries() bellekteki takipci listesini geziyor, veritabanina gitmiyor
            MaxTrackedEntries = Math.Max(MaxTrackedEntries, context.ChangeTracker.Entries().Count());

            // Takipci temizlenmezse obek obek buyuyor ve her kayit oncekileri de tariyor.
            context.ChangeTracker.Clear();

            // sievert:disable SV004 ayni sebep: temizlik sonrasi da bellekteki liste sayiliyor
            MaxTrackedAfterClear = Math.Max(MaxTrackedAfterClear, context.ChangeTracker.Entries().Count());

            saved += batch.Count;
        }

        return saved;
    }

    private static string Summarise(
        ScanOutcome outcome,
        JobProgress progress,
        WorktreeSnapshot source,
        int tracked,
        int trackedAfterClear)
    {
        CheckSummary summary = outcome.Summary;

        return JsonSerializer.Serialize(new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["sourceHeadShortSha"] = source.ShortSha,
            ["maxChangeTrackerEntries"] = tracked,
            ["maxChangeTrackerEntriesAfterClear"] = trackedAfterClear,
            // Ilerlemenin kac kez yazildigi: oge basina yazilmadigini gosteren sayi.
            ["progressWrites"] = progress.WriteCount,
            ["cancellationChecks"] = progress.CancellationCheckCount,
            ["scannedFiles"] = summary.FileCount,
            ["findingCount"] = summary.FindingCount,
            ["suppressedCount"] = summary.SuppressedCount,
            ["exemptionCount"] = outcome.Exemptions.Count,
            ["excludedFileCount"] = summary.ExcludedFileCount,
            ["skippedDirectoryCount"] = summary.SkippedDirectories.Count,
            ["activeRules"] = summary.Rules.ActiveCodes,
            ["disabledRules"] = summary.Rules.DisabledCodes,
            ["errorCount"] = summary.BySeverity.Error,
            ["warningCount"] = summary.BySeverity.Warning,
            ["infoCount"] = summary.BySeverity.Info,
        });
    }

    /// <summary>Senkron taramadan gelen ilerlemeyi tutar; yazmayi cagiran taraf yapiyor.</summary>
    private sealed class ScanRelay : IProgress<ScanProgress>
    {
        private readonly Lock gate = new();

        private ScanProgress current = new(ScanPhase.DiscoveringFiles, 0, null);

        public void Report(ScanProgress value)
        {
            lock (gate)
            {
                current = value;
            }
        }

        public (string Phase, int Processed, int? Total) Snapshot()
        {
            lock (gate)
            {
                return (current.Phase, current.Processed, current.Total);
            }
        }
    }
}
