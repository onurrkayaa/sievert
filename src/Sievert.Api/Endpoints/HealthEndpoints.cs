using Microsoft.EntityFrameworkCore;

using Sievert.Api.Analysis;

using Sievert.Data;
using Sievert.Data.Entities;
using Sievert.Modeling;

namespace Sievert.Api.Endpoints;

public static class HealthEndpoints
{
    public static void MapHealth(this RouteGroupBuilder api) =>
        api.MapGet("/health", async (
            HttpContext context,
            DatabaseSettings settings,
            ModelRegistry registry,
            IAnalysisJobQueue queue,
            AnalysisOptions options,
            RecoveryState recovery,
            CancellationToken cancellation) =>
        {
            DatabaseHealth health = await CheckDatabase(context, settings, cancellation);

            List<ModelHealth> models = [];

            foreach (ModelProfile profile in registry.Profiles)
            {
                models.Add(new ModelHealth(
                    profile.ProfileCode,
                    registry.StatusOf(profile.ProfileCode).ToString(),
                    registry.FailureOf(profile.ProfileCode)));
            }

            return Results.Ok(new HealthResponse(
                health.Reachable ? "ok" : "degraded",
                typeof(Program).Assembly.GetName().Version?.ToString() ?? "bilinmiyor",
                health,
                registry.ModelResultsChecksum[..12],
                models,
                await CheckAnalysis(context, health, queue, options, recovery, cancellation),
                CheckProcess()));
        })
        .WithName("Health")
        .WithSummary("Veritabani ve model profillerinin durumu")
        .Produces<HealthResponse>();

    /// <summary>
    /// Surecin kendi bellegi. Zorlanmis toplama YOK: <c>GC.Collect</c> cagirmak olculen
    /// sayiyi guzellestirirdi ve olculen sey artik normal calisma olmazdi.
    /// </summary>
    private static ProcessHealth CheckProcess()
    {
        using System.Diagnostics.Process current = System.Diagnostics.Process.GetCurrentProcess();

        return new ProcessHealth(
            current.WorkingSet64,
            GC.GetTotalMemory(forceFullCollection: false),
            GC.CollectionCount(0),
            GC.CollectionCount(1),
            GC.CollectionCount(2),
            Math.Round((DateTime.UtcNow - current.StartTime.ToUniversalTime()).TotalSeconds, 1));
    }

    /// <summary>
    /// Kuyruk ve worker durumu. Veritabani hazir degilse is sayilari sorulmuyor; sayilar
    /// null degil sifir olurdu ve "is yok" ile "bakamadim" ayni gorunurdu.
    /// </summary>
    private static async Task<AnalysisHealth> CheckAnalysis(
        HttpContext context,
        DatabaseHealth database,
        IAnalysisJobQueue queue,
        AnalysisOptions options,
        RecoveryState recovery,
        CancellationToken cancellation)
    {
        int queued = 0;
        int running = 0;
        DateTimeOffset? lastCompleted = null;

        if (database.Reachable)
        {
            SievertContext store = Database.Open(context);

            queued = await store.AnalysisJobs.CountAsync(job => job.Status == AnalysisJobStatus.Queued, cancellation);
            running = await store.AnalysisJobs.CountAsync(job => job.Status == AnalysisJobStatus.Running, cancellation);
            lastCompleted = await store.AnalysisJobs
                .Where(job => job.CompletedAtUtc != null)
                .MaxAsync(job => job.CompletedAtUtc, cancellation);
        }

        return new AnalysisHealth(
            queue.Capacity,
            queue.Count,
            options.WorkerConcurrency,
            queued,
            running,
            lastCompleted,
            recovery.At is DateTimeOffset at
                ? new RecoveryHealth(
                    at,
                    recovery.Last.Requeued.Count,
                    recovery.Last.Interrupted,
                    recovery.Last.CanceledBeforeStart)
                : null);
    }

    private static async Task<DatabaseHealth> CheckDatabase(
        HttpContext context,
        DatabaseSettings settings,
        CancellationToken cancellation)
    {
        // Hata metni ayar rehberligi iceriyor, baglanti dizesini degil.
        // Baglam bu kontrolden SONRA cozuluyor: dize yokken kurulmasi istisna atiyor.
        if (!settings.IsConfigured)
        {
            return new DatabaseHealth(false, false, settings.Error);
        }

        try
        {
            SievertContext database = Database.Open(context);

            if (!await database.Database.CanConnectAsync(cancellation))
            {
                return new DatabaseHealth(false, false, "Veritabanina baglanilamadi.");
            }

            bool pending = (await database.Database.GetPendingMigrationsAsync(cancellation)).Any();

            return new DatabaseHealth(true, !pending, pending ? "Uygulanmamis gecis var." : null);
        }
        catch (Exception error) when (error is not OperationCanceledException)
        {
            // Istisnanin kendi metni sunucu adi ve kullanici adi tasiyabilir; disari cikmiyor.
            return new DatabaseHealth(false, false, "Veritabanina baglanilamadi.");
        }
    }
}
