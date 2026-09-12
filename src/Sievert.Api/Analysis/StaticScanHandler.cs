using System.Text.Json;

using Microsoft.EntityFrameworkCore;

using Sievert.Analysis;
using Sievert.Core.Rules;
using Sievert.Data;
using Sievert.Data.Entities;

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
/// </summary>
public sealed class StaticScanHandler(
    SievertContext context,
    AnalysisOptions options,
    TimeProvider clock) : IAnalysisJobHandler
{
    public AnalysisJobKind Kind => AnalysisJobKind.StaticScan;

    public async Task<JobOutcome> RunAsync(AnalysisJobRun run, CancellationToken cancellation)
    {
        RepositoryRow? repository = await context.Repositories
            .AsNoTracking()
            .FirstOrDefaultAsync(row => row.Id == run.RepositoryId, cancellation);

        if (repository?.LocalPath is not string localPath || string.IsNullOrWhiteSpace(localPath))
        {
            return JobOutcome.Failed(
                "REPOSITORY_PATH_UNAVAILABLE",
                "Bu depo icin yerel klasor kayitli degil; tarama yapilamadi.");
        }

        // Tam yol yalnizca burada, surec icinde kullaniliyor; cevaba ve gunluge girmiyor.
        string fullPath = Path.GetFullPath(localPath);

        if (!Directory.Exists(fullPath))
        {
            return JobOutcome.Failed(
                "REPOSITORY_PATH_UNAVAILABLE",
                "Depo icin kayitli klasor bu makinede bulunamadi.");
        }

        if (!IsGitRepository(fullPath))
        {
            return JobOutcome.Failed(
                "REPOSITORY_NOT_GIT",
                "Kayitli klasor bir git deposu degil.");
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
            return JobOutcome.Failed("ANALYSIS_FAILED", outcome.Error!);
        }

        await run.Progress.FlushAsync(
            ScanPhase.SavingResults,
            outcome.Summary.FileCount,
            outcome.Summary.FileCount,
            cancellation);

        int saved = await SaveAsync(run.JobId, outcome, cancellation);

        return new JobOutcome(
            AnalysisJobStatus.Succeeded,
            saved,
            outcome.Summary.FileCount,
            ResultSummary: Summarise(outcome));
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

            // Takipci temizlenmezse obek obek buyuyor ve her kayit oncekileri de tariyor.
            context.ChangeTracker.Clear();

            saved += batch.Count;
        }

        return saved;
    }

    private static string Summarise(ScanOutcome outcome)
    {
        CheckSummary summary = outcome.Summary;

        return JsonSerializer.Serialize(new Dictionary<string, object?>(StringComparer.Ordinal)
        {
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

    /// <summary>
    /// Klasorde <c>.git</c> var mi. LibGit2Sharp'i bu kontrol icin API'ye baglamadim:
    /// tek bir dogru/yanlis icin git kutuphanesi eklemek, repo klonlama ve tarih okumanin
    /// bu turda olmadigi kararini bulaniklastirirdi. Worktree'lerde <c>.git</c> bir dosya
    /// oldugu icin ikisi de kabul ediliyor.
    /// </summary>
    private static bool IsGitRepository(string path) =>
        Directory.Exists(Path.Combine(path, ".git")) || File.Exists(Path.Combine(path, ".git"));

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
