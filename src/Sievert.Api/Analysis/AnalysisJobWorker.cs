using Microsoft.Extensions.Options;

using Sievert.Data;
using Sievert.Data.Entities;

namespace Sievert.Api.Analysis;

/// <summary>
/// Kuyruktan is alip kosan arka plan servisi.
///
/// Veritabani tek dogru kaynak: kuyruktan gelen yalnizca bir kimlik, isin gercekten
/// kuyrukta olup olmadigi her seferinde veritabanindan sorulup atomik olarak
/// <c>running</c>'e cevriliyor. Kaybeden worker isi birakip devam ediyor.
/// </summary>
public sealed class AnalysisJobWorker(
    IAnalysisJobQueue queue,
    IServiceScopeFactory scopes,
    JobCancellationRegistry cancellations,
    AnalysisOptions options,
    TimeProvider clock,
    ILogger<AnalysisJobWorker> logger) : BackgroundService
{
    /// <summary>Bu surecin kimligi. Hangi surecin isi yarida biraktigi gorulsun diye.</summary>
    public static readonly string InstanceId =
        $"{Environment.MachineName}:{Environment.ProcessId}".Length <= 80
            ? $"{Environment.MachineName}:{Environment.ProcessId}"
            : Environment.ProcessId.ToString(System.Globalization.CultureInfo.InvariantCulture);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        List<Task> loops = [];

        for (int index = 0; index < options.WorkerConcurrency; index++)
        {
            loops.Add(LoopAsync(stoppingToken));
        }

        await Task.WhenAll(loops);
    }

    private async Task LoopAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            Guid jobId;

            try
            {
                jobId = await queue.DequeueAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            try
            {
                await RunAsync(jobId, stoppingToken);
            }
            catch (Exception error)
            {
                // Dongu bir isin hatasi yuzunden durmamali; yoksa tek bozuk is butun
                // kuyrugu oldururdu.
                logger.LogError(error, "Is kosulurken beklenmeyen hata. JobId={JobId}", jobId);
            }
        }
    }

    private async Task RunAsync(Guid jobId, CancellationToken stoppingToken)
    {
        using IServiceScope scope = scopes.CreateScope();

        AnalysisJobStore store = scope.ServiceProvider.GetRequiredService<AnalysisJobStore>();

        if (await store.FindAsync(jobId, stoppingToken) is not AnalysisJobRow job)
        {
            logger.LogWarning("Kuyruktan gelen is bulunamadi. JobId={JobId}", jobId);

            return;
        }

        if (job.Status != AnalysisJobStatus.Queued)
        {
            // Iptal edilmis ya da baska bir worker almis olabilir; ikisi de normal.
            logger.LogInformation(
                "Is kuyrukta degil, atlandi. JobId={JobId} Status={Status}",
                jobId,
                AnalysisJobRow.Name(job.Status));

            return;
        }

        if (job.CancellationRequestedAtUtc is not null)
        {
            await store.CancelQueuedAsync(jobId, clock.GetUtcNow(), stoppingToken);

            logger.LogInformation("Is baslamadan iptal edildi. JobId={JobId}", jobId);

            return;
        }

        if (!await store.TryStartAsync(jobId, InstanceId, clock.GetUtcNow(), stoppingToken))
        {
            logger.LogInformation("Isi baska bir worker aldi. JobId={JobId}", jobId);

            return;
        }

        logger.LogInformation(
            "Is basladi. JobId={JobId} RepositoryId={RepositoryId} Kind={Kind}",
            jobId,
            job.RepositoryId,
            AnalysisJobRow.Name(job.Kind));

        DateTimeOffset started = clock.GetUtcNow();
        CancellationTokenSource source = cancellations.Register(jobId, stoppingToken);

        JobProgress progress = new(store, jobId, options.ProgressInterval, clock);
        JobOutcome outcome;

        try
        {
            IAnalysisJobHandler handler = Handler(scope, job.Kind);

            outcome = await handler.RunAsync(
                new AnalysisJobRun(jobId, job.RepositoryId, progress),
                source.Token);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Uygulama kapaniyor. Isi basarili yazmiyoruz; yeniden baslatmada kurtarma
            // bunu PROCESS_INTERRUPTED olarak isaretleyecek.
            logger.LogInformation("Uygulama kapanirken is yarida kaldi. JobId={JobId}", jobId);
            cancellations.Release(jobId);

            return;
        }
        catch (OperationCanceledException)
        {
            // Isleyiciler kendi iptallerini sayilariyla birlikte dondurmeli; buraya
            // dusmek beklenmiyor. Duserse de sifir YAZMIYORUZ: isin en son bildirdigi
            // ilerleme korunuyor, cunku sifir yazmak gercekte islenmis isi yok saymak olur.
            logger.LogWarning("Is jeton uzerinden iptal edildi; sayilar isleyiciden gelmedi. JobId={JobId}", jobId);

            AnalysisJobRow? current = await store.FindAsync(jobId, CancellationToken.None);

            outcome = JobOutcome.Canceled(current?.ResultCount ?? 0, current?.ProcessedItems ?? 0);
        }
        catch (Exception error)
        {
            // Istisnanin kendi metni yol ya da baglanti dizesi tasiyabilir; cevaba
            // girmiyor, yalnizca gunlukte kaliyor.
            logger.LogError(error, "Is basarisiz. JobId={JobId}", jobId);

            outcome = JobOutcome.Failed("ANALYSIS_FAILED", "Is calisirken hata olustu. Ayrinti sunucu gunlugunde.");
        }
        finally
        {
            cancellations.Release(jobId);
        }

        await store.CompleteAsync(
            jobId,
            outcome.Status,
            outcome.ResultCount,
            outcome.ProcessedItems,
            outcome.ErrorCode,
            outcome.ErrorMessage,
            clock.GetUtcNow(),
            outcome.ResultSummary,
            stoppingToken);

        logger.LogInformation(
            "Is bitti. JobId={JobId} Kind={Kind} Status={Status} ResultCount={ResultCount} "
            + "ErrorCode={ErrorCode} DurationMs={DurationMs}",
            jobId,
            AnalysisJobRow.Name(job.Kind),
            AnalysisJobRow.Name(outcome.Status),
            outcome.ResultCount,
            outcome.ErrorCode,
            (clock.GetUtcNow() - started).TotalMilliseconds);
    }

    private static IAnalysisJobHandler Handler(IServiceScope scope, AnalysisJobKind kind)
    {
        foreach (IAnalysisJobHandler handler in scope.ServiceProvider.GetServices<IAnalysisJobHandler>())
        {
            if (handler.Kind == kind)
            {
                return handler;
            }
        }

        throw new InvalidOperationException($"Bu is turu icin isleyici yok: {AnalysisJobRow.Name(kind)}");
    }
}
