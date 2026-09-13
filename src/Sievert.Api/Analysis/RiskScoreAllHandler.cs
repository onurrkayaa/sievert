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

        int total = await context.Commits
            .AsNoTracking()
            .CountAsync(
                commit => commit.RepositoryId == run.RepositoryId
                    && context.CommitMetrics.Any(metric => metric.CommitId == commit.Id),
                cancellation);

        await run.Progress.FlushAsync(PhaseScoring, 0, total, cancellation);

        // Model ve olcekleyici dongunun DISINDA bir kez aliniyor; commit basina yeniden
        // yuklemek 22 bin commit'te 22 bin kez diske gitmek olurdu.
        FeatureScaler scaler = registry.ScalerFor(profile);
        LoadedModel model = registry.Load(profile.ProfileCode);

        int processed = 0;
        int saved = 0;
        int explanationMismatches = 0;

        // Obek yaziminin takipcide biriktirip biriktirmedigini olcen iki sayac. Beklenen
        // davranis: her obekte ayni tabana donmek, commit sayisiyla buyumemek.
        int maxTrackedEntries = 0;
        int maxTrackedAfterClear = 0;

        // Jeton dongunun ICINDEKI her await'i kesebilir: obek sorgusu, kayit, ilerleme
        // yazimi. Hepsini tek bir yerde yakaliyoruz ki iptal her durumda gercek
        // sayilarla raporlansin. Sadece dongu basinda bakmak yetmedi; olcumde is 2250
        // satir yazmisken 0 sonucla iptal edilmisti.
        try
        {
            while (processed < total)
            {
                // Jeton iptal edildiginde ISTISNA FIRLATMIYORUZ. Firlatsaydik sayilar
                // isleyiciden cikamaz, is "0 satir yazdi" diye kaydedilirdi; oysa yazilmis
                // satirlar veritabaninda duruyor olurdu. Bir kez tam bunu yasadim: 2250
                // satir yazilmisken is 0 sonucla iptal edildi.
                if (cancellation.IsCancellationRequested)
                {
                    return await CanceledAsync(run, total);
                }

                // sievert:disable SV004 dongu basina tek sorgu bilincli: 22 bin satiri bellege almamak icin sayfali okuyoruz
                List<CommitWithMetric> batch = await ScorableCommits(run.RepositoryId)
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

                maxTrackedEntries = Math.Max(maxTrackedEntries, context.ChangeTracker.Entries().Count());
                context.ChangeTracker.Clear();
                maxTrackedAfterClear = Math.Max(maxTrackedAfterClear, context.ChangeTracker.Entries().Count());

                processed += batch.Count;
                saved += rows.Count;

                await run.Progress.ReportAsync(PhaseScoring, processed, total, cancellation);

                if (await run.Progress.IsCancellationRequestedAsync(cancellation))
                {
                    return await CanceledAsync(run, total);
                }
            }
        }
        catch (OperationCanceledException)
        {
            return await CanceledAsync(run, total);
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
                ["progressWrites"] = run.Progress.WriteCount,
                ["cancellationChecks"] = run.Progress.CancellationCheckCount,
                ["modelLoads"] = registry.LoadCountOf(profile.ProfileCode),
                ["maxChangeTrackerEntries"] = maxTrackedEntries,
                ["maxChangeTrackerEntriesAfterClear"] = maxTrackedAfterClear,
            }));
    }

    /// <summary>
    /// Iptal sonucunu uretir.
    ///
    /// Sayilar veritabanindan SAYILIYOR, bellekteki sayactan degil: obegin ortasinda
    /// iptal edilirse sayac ile gercekte yazilmis satirlar ayrisabilir. Kismi sonucun
    /// kac satir oldugunu yanlis soylemek, kismi sonucu hic soylememekten kotu.
    ///
    /// Iptal jetonu bu noktada zaten iptal edilmis olabilecegi icin son yazim
    /// <see cref="CancellationToken.None"/> ile yapiliyor.
    /// </summary>
    private async Task<JobOutcome> CanceledAsync(AnalysisJobRun run, int total)
    {
        int actual = await context.CommitRiskSnapshots
            .AsNoTracking()
            .CountAsync(row => row.AnalysisJobId == run.JobId, CancellationToken.None);

        await run.Progress.FlushAsync(PhaseScoring, actual, total, CancellationToken.None);

        return JobOutcome.Canceled(actual, actual);
    }

    /// <summary>
    /// Olcusu hesaplanmis commit'ler, metrik hesabiyla ayni sirada (CommitOrdering:
    /// tarih artan, esitlikte madencilik sirasi artan).
    ///
    /// Siralama <c>select</c>'ten ONCE yaziliyor. Sonra yazilinca EF sorguyu ceviremiyor:
    /// kendi kurdugu kaydin icine bakip alan secmesi gerekiyor ve bunu SQL'e dokemiyor.
    /// Bir kez yasandi, hata mesaji tam olarak bunu soyluyordu.
    /// </summary>
    private IQueryable<CommitWithMetric> ScorableCommits(int repositoryId) =>
        from commit in context.Commits.AsNoTracking()
        join metric in context.CommitMetrics.AsNoTracking() on commit.Id equals metric.CommitId
        where commit.RepositoryId == repositoryId
        orderby commit.AuthorDateUtc, commit.Id
        select new CommitWithMetric(commit, metric);

    private sealed record CommitWithMetric(CommitRow Commit, CommitMetricRow Metric);
}
