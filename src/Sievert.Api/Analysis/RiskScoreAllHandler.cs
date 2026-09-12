using System.Text.Json;

using Microsoft.EntityFrameworkCore;

using Sievert.Data;
using Sievert.Data.Entities;
using Sievert.Modeling;

namespace Sievert.Api.Analysis;

/// <summary>
/// Reponun metrikli butun commit'lerini kendi ayni-repo modeliyle skorlar.
///
/// Yalnizca bilinen uc profil. Bilinmeyen bir depo icin is zaten **acilmiyor**; buraya
/// gelmemesi gerekiyor, gelirse de skorlamak yerine hata donuyor.
/// </summary>
public sealed class RiskScoreAllHandler(
    SievertContext context,
    ModelRegistry registry,
    ScoreReference reference,
    AnalysisOptions options,
    TimeProvider clock) : IAnalysisJobHandler
{
    public const string PhaseCounting = "counting-commits";

    public const string PhaseScoring = "scoring";

    public AnalysisJobKind Kind => AnalysisJobKind.RiskScoreAll;

    public async Task<JobOutcome> RunAsync(AnalysisJobRun run, CancellationToken cancellation)
    {
        RepositoryRow? repository = await context.Repositories
            .AsNoTracking()
            .FirstOrDefaultAsync(row => row.Id == run.RepositoryId, cancellation);

        if (repository is null)
        {
            return JobOutcome.Failed("REPOSITORY_NOT_FOUND", "Depo bulunamadi.");
        }

        if (registry.ForRepository(repository.Identity) is not ModelProfile profile)
        {
            return JobOutcome.Failed(
                ApiError.UnknownRepositoryModel,
                "Bu depo icin egitilmis bir model profili yok; varsayilan profil secilmiyor.");
        }

        if (registry.StatusOf(profile.ProfileCode) == ModelStatus.ChecksumMismatch)
        {
            return JobOutcome.Failed(
                ApiError.ModelChecksumMismatch,
                "Model dosyasinin ozeti kayitli degerle ayni degil; model kullanilmadi.");
        }

        if (reference.For(profile.ProfileCode) is not ScoreDistribution distribution)
        {
            return JobOutcome.Failed(
                ApiError.ScoreReferenceNotReady,
                "Egitim skor dagilimi okunamadi; goreli endeks uretilemedi.");
        }

        await run.Progress.FlushAsync(PhaseCounting, 0, null, cancellation);

        int total = await ScorableCommits(run.RepositoryId).CountAsync(cancellation);

        await run.Progress.FlushAsync(PhaseScoring, 0, total, cancellation);

        // Model ve olcekleyici dongunun DISINDA bir kez aliniyor; commit basina yeniden
        // yuklemek 22 bin commit'te 22 bin kez diske gitmek olurdu.
        FeatureScaler scaler = registry.ScalerFor(profile);
        LoadedModel model = registry.Load(profile.ProfileCode);

        int processed = 0;
        int saved = 0;
        int explanationMismatches = 0;

        while (processed < total)
        {
            cancellation.ThrowIfCancellationRequested();

            // sievert:disable SV004 dongu basina tek sorgu bilincli: 22 bin satiri bellege almamak icin sayfali okuyoruz
            List<CommitWithMetric> batch = await ScorableCommits(run.RepositoryId)
                .OrderBy(pair => pair.Commit.AuthorDateUtc)
                .ThenBy(pair => pair.Commit.Id)
                .Skip(processed)
                .Take(options.RiskBatchSize)
                .ToListAsync(cancellation);

            if (batch.Count == 0)
            {
                break;
            }

            DateTimeOffset now = clock.GetUtcNow();
            List<CommitRiskSnapshotRow> rows = [];

            foreach (CommitWithMetric pair in batch)
            {
                CommitRiskResult assessment = CommitRiskCalculator.Compute(
                    profile,
                    scaler,
                    model,
                    distribution,
                    repository,
                    pair.Commit,
                    pair.Metric);

                if (!assessment.Explanation.IsWithinTolerance)
                {
                    // Aciklama dogrulanamadi. Skor yine de gecerli - aciklama ile skor
                    // ayri seyler - ama sayisi raporlaniyor ki sessiz kalmasin.
                    explanationMismatches++;
                }

                rows.Add(new CommitRiskSnapshotRow
                {
                    AnalysisJobId = run.JobId,
                    CommitId = pair.Commit.Id,
                    RawModelScore = assessment.Explanation.RawModelScore,
                    RiskIndex = assessment.RiskIndex,
                    DecisionAt05 = assessment.Explanation.RawModelScore >= 0.5,
                    DecisionAtTrainThreshold = assessment.Explanation.RawModelScore >= profile.TrainThreshold,
                    TrainThreshold = profile.TrainThreshold,
                    ModelProfile = profile.ProfileCode,
                    ModelChecksum = profile.ShortChecksum,
                    IsCalibrated = profile.IsCalibrated,
                    WarningCodes = JsonSerializer.Serialize(assessment.Warnings),
                    CreatedAtUtc = now,
                });
            }

            context.CommitRiskSnapshots.AddRange(rows);
            await context.SaveChangesAsync(cancellation);
            context.ChangeTracker.Clear();

            processed += batch.Count;
            saved += rows.Count;

            await run.Progress.ReportAsync(PhaseScoring, processed, total, cancellation);

            if (await run.Progress.IsCancellationRequestedAsync(cancellation))
            {
                await run.Progress.FlushAsync(PhaseScoring, processed, total, cancellation);

                return JobOutcome.Canceled(saved, processed);
            }
        }

        await run.Progress.FlushAsync(PhaseScoring, processed, total, cancellation);

        if (saved != total)
        {
            // Beklenenden az satir yazildiysa is basarili SAYILMIYOR. "Tamamlandi" diyen
            // bir isin eksik sonucu, eksik sonuctan daha kotu.
            return JobOutcome.Failed(
                "RESULT_COUNT_MISMATCH",
                $"Beklenen {total} satir, yazilan {saved}. Sonuc tam degil.",
                saved,
                processed);
        }

        return new JobOutcome(
            AnalysisJobStatus.Succeeded,
            saved,
            processed,
            ResultSummary: JsonSerializer.Serialize(new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["modelProfile"] = profile.ProfileCode,
                ["modelChecksum"] = profile.ShortChecksum,
                ["modelCodeCommit"] = profile.ModelCodeCommit,
                ["trainThreshold"] = profile.TrainThreshold,
                ["isCalibrated"] = profile.IsCalibrated,
                ["scoredCommits"] = saved,
                ["explanationMismatches"] = explanationMismatches,
                ["batchSize"] = options.RiskBatchSize,
            }));
    }

    /// <summary>Olcusu hesaplanmis commit'ler; olcusu olmayan skorlanamaz.</summary>
    private IQueryable<CommitWithMetric> ScorableCommits(int repositoryId) =>
        context.Commits
            .AsNoTracking()
            .Where(commit => commit.RepositoryId == repositoryId)
            .Join(
                context.CommitMetrics.AsNoTracking(),
                commit => commit.Id,
                metric => metric.CommitId,
                (commit, metric) => new CommitWithMetric(commit, metric));

    private sealed record CommitWithMetric(CommitRow Commit, CommitMetricRow Metric);
}
