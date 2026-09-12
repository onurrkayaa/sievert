using Sievert.Data;

namespace Sievert.Api.Analysis;

/// <summary>Son kurtarmanin sonucu; saglik ucunda gorunuyor.</summary>
public sealed class RecoveryState
{
    public RecoveryReport Last { get; private set; } = RecoveryReport.Empty;

    public DateTimeOffset? At { get; private set; }

    public void Record(RecoveryReport report, DateTimeOffset at)
    {
        Last = report;
        At = at;
    }
}

/// <summary>
/// Uygulama acilirken onceki surecten kalan isleri toparlar ve kuyrukta bekleyenleri
/// yeniden kuyruga alir.
///
/// Calisan bir is otomatik DEVAM ETMIYOR: nerede kaldigini bilmiyoruz ve kismi bir sonucu
/// tamamlanmis gibi surdurmek en kotusu olurdu. <c>PROCESS_INTERRUPTED</c> ile basarisiz
/// isaretleniyor, kullanici isterse yeniden baslatiyor.
/// </summary>
public sealed class AnalysisJobRecovery(
    IServiceScopeFactory scopes,
    IAnalysisJobQueue queue,
    RecoveryState state,
    DatabaseSettings settings,
    TimeProvider clock,
    ILogger<AnalysisJobRecovery> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellation)
    {
        // Baglanti dizesi yoksa API yine ayaga kalkmali; saglik ucu durumu soyleyecek.
        // Acilisi bloke etmek, sebebi gorebilecek tek ucu de kapatirdi.
        if (!settings.IsConfigured)
        {
            logger.LogWarning("Baglanti dizesi yok; kurtarma atlandi.");

            return;
        }

        RecoveryReport report;

        try
        {
            using IServiceScope scope = scopes.CreateScope();

            report = await scope.ServiceProvider
                .GetRequiredService<AnalysisJobStore>()
                .RecoverAsync(clock.GetUtcNow(), cancellation);
        }
        catch (Exception error)
        {
            logger.LogError(error, "Kurtarma calistirilamadi.");

            return;
        }

        foreach (Guid jobId in report.Requeued)
        {
            await queue.EnqueueAsync(jobId, cancellation);
        }

        state.Record(report, clock.GetUtcNow());

        logger.LogInformation(
            "Kurtarma bitti. Requeued={Requeued} Interrupted={Interrupted} CanceledBeforeStart={Canceled}",
            report.Requeued.Count,
            report.Interrupted,
            report.CanceledBeforeStart);
    }

    public Task StopAsync(CancellationToken cancellation) => Task.CompletedTask;
}
