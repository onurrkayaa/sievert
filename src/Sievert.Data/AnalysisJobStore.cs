using Microsoft.EntityFrameworkCore;

using Npgsql;

using Sievert.Data.Entities;

namespace Sievert.Data;

/// <summary>Is acma istegi nasil sonuclandi.</summary>
public enum JobCreateOutcome
{
    /// <summary>Yeni is acildi.</summary>
    Created,

    /// <summary>Ayni tekrar anahtariyla acilmis is zaten vardi; yenisi acilmadi.</summary>
    ReturnedExisting,

    /// <summary>Ayni tekrar anahtari baska bir repo ya da tur icin kullanilmis.</summary>
    IdempotencyKeyReused,

    /// <summary>Ayni repo ve tur icin zaten aktif bir is var.</summary>
    AlreadyActive,

    /// <summary>Tekrar anahtari bos ya da cok uzun.</summary>
    IdempotencyKeyInvalid,
}

/// <param name="Outcome">Istegin sonucu.</param>
/// <param name="Job">Olusan ya da catisilan is; gecersiz anahtarda null.</param>
public sealed record JobCreateResult(JobCreateOutcome Outcome, AnalysisJobRow? Job);

/// <summary>Yeniden baslatma kurtarmasinin ne yaptigi.</summary>
/// <param name="Requeued">Kuyrukta bekleyip yeniden kuyruga alinan isler.</param>
/// <param name="Interrupted">Onceki surecte yarida kalmis ve basarisiz isaretlenen is sayisi.</param>
/// <param name="CanceledBeforeStart">Calismaya baslamadan iptal istegi almis is sayisi.</param>
public sealed record RecoveryReport(IReadOnlyList<Guid> Requeued, int Interrupted, int CanceledBeforeStart)
{
    public static readonly RecoveryReport Empty = new([], 0, 0);

    /// <summary>Kurtarmanin bir sey degistirip degistirmedigi; idempotentlik testi bunu okuyor.</summary>
    public int ChangedRows => Interrupted + CanceledBeforeStart;
}

/// <summary>
/// Is kayitlarinin tek giris noktasi.
///
/// Butun durum gecisleri **kosullu tek bir UPDATE** ile yapiliyor: beklenen onceki durum
/// sorgunun <c>WHERE</c> kismina giriyor ve etkilenen satir sayisi geciste basarili olup
/// olmadigimizi soyluyor. Once okuyup sonra yazmak, iki worker'in ayni isi almasina acik
/// kapi birakirdi.
/// </summary>
public sealed class AnalysisJobStore(SievertContext context)
{
    public const int MaximumIdempotencyKeyLength = 128;

    /// <summary>Onceki surecte yarida kalan isin hata kodu.</summary>
    public const string ProcessInterruptedCode = "PROCESS_INTERRUPTED";

    private const string ActiveIndexName = "IX_AnalysisJobs_ActiveDeduplicationKey";

    private const string IdempotencyIndexName = "IX_AnalysisJobs_IdempotencyKey";

    /// <summary>
    /// Yeni is acar. Tekillik iki katmanli: once acik kontrol, sonra veritabaninin
    /// benzersiz indeksi. Ikincisi olmadan iki es zamanli istek ayni anda kontrolden
    /// gecip iki is acabilirdi.
    /// </summary>
    public async Task<JobCreateResult> CreateAsync(
        int repositoryId,
        AnalysisJobKind kind,
        string? idempotencyKey,
        DateTimeOffset now,
        CancellationToken cancellation = default)
    {
        if (idempotencyKey is not null && !IsValidKey(idempotencyKey))
        {
            return new JobCreateResult(JobCreateOutcome.IdempotencyKeyInvalid, null);
        }

        if (idempotencyKey is not null
            && await FindByKeyAsync(idempotencyKey, cancellation) is AnalysisJobRow existing)
        {
            return Existing(existing, repositoryId, kind);
        }

        string deduplicationKey = AnalysisJobRow.DeduplicationKeyFor(repositoryId, kind);

        if (await FindActiveAsync(deduplicationKey, cancellation) is AnalysisJobRow active)
        {
            return new JobCreateResult(JobCreateOutcome.AlreadyActive, active);
        }

        AnalysisJobRow job = new()
        {
            Id = Guid.CreateVersion7(),
            RepositoryId = repositoryId,
            Kind = kind,
            Status = AnalysisJobStatus.Queued,
            RequestedAtUtc = now,
            CurrentPhase = "queued",
            IdempotencyKey = idempotencyKey,
            ActiveDeduplicationKey = deduplicationKey,
        };

        context.AnalysisJobs.Add(job);

        try
        {
            await context.SaveChangesAsync(cancellation);
        }
        catch (DbUpdateException error) when (Violated(error, ActiveIndexName))
        {
            context.Entry(job).State = EntityState.Detached;

            return new JobCreateResult(
                JobCreateOutcome.AlreadyActive,
                await FindActiveAsync(deduplicationKey, cancellation));
        }
        catch (DbUpdateException error) when (Violated(error, IdempotencyIndexName))
        {
            context.Entry(job).State = EntityState.Detached;

            AnalysisJobRow? winner = idempotencyKey is null
                ? null
                : await FindByKeyAsync(idempotencyKey, cancellation);

            return winner is null
                ? new JobCreateResult(JobCreateOutcome.IdempotencyKeyReused, null)
                : Existing(winner, repositoryId, kind);
        }

        context.Entry(job).State = EntityState.Detached;

        return new JobCreateResult(JobCreateOutcome.Created, job);
    }

    public Task<AnalysisJobRow?> FindAsync(Guid id, CancellationToken cancellation = default) =>
        context.AnalysisJobs.AsNoTracking().FirstOrDefaultAsync(job => job.Id == id, cancellation);

    /// <summary>Kuyruktaki isi calisir yapar. Baskasi kaptiysa false doner.</summary>
    public async Task<bool> TryStartAsync(
        Guid id,
        string workerInstanceId,
        DateTimeOffset now,
        CancellationToken cancellation = default) =>
        await context.AnalysisJobs
            .Where(job => job.Id == id && job.Status == AnalysisJobStatus.Queued)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(job => job.Status, AnalysisJobStatus.Running)
                    .SetProperty(job => job.StartedAtUtc, now)
                    .SetProperty(job => job.HeartbeatAtUtc, now)
                    .SetProperty(job => job.WorkerInstanceId, workerInstanceId)
                    .SetProperty(job => job.CurrentPhase, "starting"),
                cancellation) == 1;

    /// <summary>
    /// Calisan isi terminal duruma tasir. Terminal geciste ilerleme de yaziliyor ve
    /// tekillik anahtari temizleniyor; ikisi ayri UPDATE olsaydi arada bir catlak kalirdi.
    /// </summary>
    public async Task<bool> CompleteAsync(
        Guid id,
        AnalysisJobStatus status,
        int resultCount,
        int processedItems,
        string? errorCode,
        string? errorMessage,
        DateTimeOffset now,
        string? resultSummary = null,
        CancellationToken cancellation = default)
    {
        if (!AnalysisJobTransitions.IsTerminal(status))
        {
            throw new ArgumentOutOfRangeException(nameof(status), status, "Terminal olmayan durum.");
        }

        bool complete = AnalysisJobTransitions.IsResultComplete(status);

        return await context.AnalysisJobs
            .Where(job => job.Id == id && job.Status == AnalysisJobStatus.Running)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(job => job.Status, status)
                    .SetProperty(job => job.CompletedAtUtc, now)
                    .SetProperty(job => job.HeartbeatAtUtc, now)
                    .SetProperty(job => job.ResultCount, resultCount)
                    .SetProperty(job => job.ProcessedItems, processedItems)
                    .SetProperty(job => job.IsResultComplete, complete)
                    .SetProperty(job => job.ErrorCode, errorCode)
                    .SetProperty(job => job.ErrorMessage, errorMessage)
                    .SetProperty(job => job.CurrentPhase, AnalysisJobRow.Name(status))
                    .SetProperty(job => job.ResultSummary, resultSummary)
                    .SetProperty(job => job.ActiveDeduplicationKey, (string?)null),
                cancellation) == 1;
    }

    /// <summary>Kuyruktaki isi dogrudan iptal eder; calismaya hic baslamaz.</summary>
    public async Task<bool> CancelQueuedAsync(
        Guid id,
        DateTimeOffset now,
        CancellationToken cancellation = default) =>
        await context.AnalysisJobs
            .Where(job => job.Id == id && job.Status == AnalysisJobStatus.Queued)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(job => job.Status, AnalysisJobStatus.Canceled)
                    .SetProperty(job => job.CancellationRequestedAtUtc, now)
                    .SetProperty(job => job.CompletedAtUtc, now)
                    .SetProperty(job => job.IsResultComplete, false)
                    .SetProperty(job => job.CurrentPhase, "canceled")
                    .SetProperty(job => job.ActiveDeduplicationKey, (string?)null),
                cancellation) == 1;

    /// <summary>
    /// Calisan is icin iptal istegini kaydeder. Isi durdurmaz; durdurmayi worker
    /// batch sinirinda yapiyor. Zaten istenmisse tarihi ezmiyor.
    /// </summary>
    public async Task<bool> RequestCancellationAsync(
        Guid id,
        DateTimeOffset now,
        CancellationToken cancellation = default) =>
        await context.AnalysisJobs
            .Where(job => job.Id == id
                && job.Status == AnalysisJobStatus.Running
                && job.CancellationRequestedAtUtc == null)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(job => job.CancellationRequestedAtUtc, now),
                cancellation) == 1;

    public Task<bool> IsCancellationRequestedAsync(Guid id, CancellationToken cancellation = default) =>
        context.AnalysisJobs
            .AsNoTracking()
            .AnyAsync(job => job.Id == id && job.CancellationRequestedAtUtc != null, cancellation);

    /// <summary>
    /// Ilerlemeyi yazar. Yalnizca calisan isi gunceller: terminal duruma gecmis bir isin
    /// ilerlemesini geriye dogru degistirmek, biten bir isi yeniden calisiyor gibi
    /// gosterirdi.
    /// </summary>
    public async Task<bool> UpdateProgressAsync(
        Guid id,
        string phase,
        int processedItems,
        int? totalItems,
        DateTimeOffset now,
        CancellationToken cancellation = default)
    {
        double? percent = totalItems is int total && total > 0
            ? Math.Clamp(Math.Round(processedItems * 100.0 / total, 1), 0.0, 100.0)
            : null;

        return await context.AnalysisJobs
            .Where(job => job.Id == id
                && job.Status == AnalysisJobStatus.Running
                && job.ProcessedItems <= processedItems)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(job => job.CurrentPhase, phase)
                    .SetProperty(job => job.ProcessedItems, processedItems)
                    .SetProperty(job => job.TotalItems, totalItems)
                    .SetProperty(job => job.ProgressPercent, percent)
                    .SetProperty(job => job.HeartbeatAtUtc, now),
                cancellation) == 1;
    }

    /// <summary>
    /// Uygulama acilirken onceki surecten kalan isleri toparlar.
    ///
    /// Calisan bir is otomatik DEVAM ETMIYOR. Nerede kaldigini bilmiyoruz ve yarim kalan
    /// sonucu tamamlanmis gibi surdurmek, kismi bir sonucu tam sonuc yapardi.
    ///
    /// Idempotent: ikinci kosuda degistirecek satir kalmiyor.
    /// </summary>
    public async Task<RecoveryReport> RecoverAsync(
        DateTimeOffset now,
        CancellationToken cancellation = default)
    {
        int interrupted = await context.AnalysisJobs
            .Where(job => job.Status == AnalysisJobStatus.Running)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(job => job.Status, AnalysisJobStatus.Failed)
                    .SetProperty(job => job.CompletedAtUtc, now)
                    .SetProperty(job => job.IsResultComplete, false)
                    .SetProperty(job => job.ErrorCode, ProcessInterruptedCode)
                    .SetProperty(job => job.ErrorMessage, "Is calisirken surec kapandi; otomatik devam edilmedi.")
                    .SetProperty(job => job.CurrentPhase, "interrupted")
                    .SetProperty(job => job.ActiveDeduplicationKey, (string?)null),
                cancellation);

        int canceled = await context.AnalysisJobs
            .Where(job => job.Status == AnalysisJobStatus.Queued && job.CancellationRequestedAtUtc != null)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(job => job.Status, AnalysisJobStatus.Canceled)
                    .SetProperty(job => job.CompletedAtUtc, now)
                    .SetProperty(job => job.IsResultComplete, false)
                    .SetProperty(job => job.CurrentPhase, "canceled")
                    .SetProperty(job => job.ActiveDeduplicationKey, (string?)null),
                cancellation);

        List<Guid> requeued = await context.AnalysisJobs
            .AsNoTracking()
            .Where(job => job.Status == AnalysisJobStatus.Queued)
            .OrderBy(job => job.RequestedAtUtc)
            .Select(job => job.Id)
            .ToListAsync(cancellation);

        return new RecoveryReport(requeued, interrupted, canceled);
    }

    /// <summary>Bos ya da yalniz bosluk olan anahtar gecersiz; ust sinir 128.</summary>
    public static bool IsValidKey(string key) =>
        !string.IsNullOrWhiteSpace(key) && key.Length <= MaximumIdempotencyKeyLength;

    private static JobCreateResult Existing(AnalysisJobRow job, int repositoryId, AnalysisJobKind kind) =>
        job.RepositoryId == repositoryId && job.Kind == kind
            ? new JobCreateResult(JobCreateOutcome.ReturnedExisting, job)
            : new JobCreateResult(JobCreateOutcome.IdempotencyKeyReused, job);

    private Task<AnalysisJobRow?> FindByKeyAsync(string key, CancellationToken cancellation) =>
        context.AnalysisJobs.AsNoTracking().FirstOrDefaultAsync(job => job.IdempotencyKey == key, cancellation);

    private Task<AnalysisJobRow?> FindActiveAsync(string deduplicationKey, CancellationToken cancellation) =>
        context.AnalysisJobs
            .AsNoTracking()
            .FirstOrDefaultAsync(job => job.ActiveDeduplicationKey == deduplicationKey, cancellation);

    /// <summary>Benzersizlik ihlali mi, ve hangi indekste.</summary>
    private static bool Violated(DbUpdateException error, string indexName) =>
        error.InnerException is PostgresException { SqlState: "23505" } postgres
        && string.Equals(postgres.ConstraintName, indexName, StringComparison.Ordinal);
}
