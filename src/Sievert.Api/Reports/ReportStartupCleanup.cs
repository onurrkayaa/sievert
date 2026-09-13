using Microsoft.EntityFrameworkCore;

using Sievert.Api;
using Sievert.Contracts;
using Sievert.Data;
using Sievert.Data.Entities;

namespace Sievert.Api.Reports;

/// <summary>Acilis temizliginin sonucu; saglik ucunda gorunuyor.</summary>
public sealed class ReportCleanupState
{
    public int TemporaryFilesRemoved { get; private set; }

    public int InterruptedArtifacts { get; private set; }

    public DateTimeOffset? At { get; private set; }

    public void Record(int temporaryFiles, int interrupted, DateTimeOffset at)
    {
        TemporaryFilesRemoved = temporaryFiles;
        InterruptedArtifacts = interrupted;
        At = at;
    }
}

/// <summary>
/// Acilista yarida kalmis rapor uretimlerini toparlar.
///
/// Iki is yapiyor:
/// 1. Rapor klasorundeki **bu uygulamanin** gecici dosyalarini siler. Baska dosyalara
///    dokunmuyor; bir temizlik rutini yanlis klasorde kosarsa kullanicinin dosyalarini
///    silmis olur.
/// 2. Isi terminal duruma dusmus ama kaydi hala <c>pending</c> duran raporlari
///    <c>failed</c> yapar. Hazir dosyalara dokunmuyor.
///
/// <see cref="Analysis.AnalysisJobRecovery"/>'den **sonra** kayit ediliyor: once isler
/// toparlaniyor, sonra o islere bagli rapor kayitlari.
/// </summary>
public sealed class ReportStartupCleanup(
    IServiceScopeFactory scopes,
    IReportArtifactStore store,
    ReportCleanupState state,
    DatabaseSettings settings,
    TimeProvider clock,
    ILogger<ReportStartupCleanup> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellation)
    {
        int temporaryFiles = 0;

        try
        {
            temporaryFiles = store.CleanTemporaryFiles();
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            logger.LogWarning(error, "Gecici rapor dosyalari temizlenemedi.");
        }

        int interrupted = 0;

        if (settings.IsConfigured)
        {
            try
            {
                using IServiceScope scope = scopes.CreateScope();
                SievertContext context = scope.ServiceProvider.GetRequiredService<SievertContext>();

                interrupted = await context.ReportArtifacts
                    .Where(artifact => artifact.Status == ReportArtifactStatus.Pending
                        && context.AnalysisJobs.Any(job => job.Id == artifact.AnalysisJobId
                            && (job.Status == AnalysisJobStatus.Failed
                                || job.Status == AnalysisJobStatus.Canceled)))
                    .ExecuteUpdateAsync(
                        update => update
                            .SetProperty(artifact => artifact.Status, ReportArtifactStatus.Failed)
                            .SetProperty(artifact => artifact.ErrorCode, ApiError.ReportGenerationInterrupted)
                            .SetProperty(artifact => artifact.ErrorMessage,
                                "Rapor uretimi yarida kaldi; yeniden istenebilir."),
                        cancellation);
            }
            catch (Exception error) when (error is not OperationCanceledException)
            {
                logger.LogWarning(error, "Yarida kalmis rapor kayitlari toparlanamadi.");
            }
        }

        state.Record(temporaryFiles, interrupted, clock.GetUtcNow());

        if (temporaryFiles > 0 || interrupted > 0)
        {
            logger.LogInformation(
                "Rapor temizligi: {Temporary} gecici dosya, {Interrupted} yarida kalmis kayit.",
                temporaryFiles,
                interrupted);
        }
    }

    public Task StopAsync(CancellationToken cancellation) => Task.CompletedTask;
}
