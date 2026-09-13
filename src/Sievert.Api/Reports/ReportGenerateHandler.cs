using Microsoft.EntityFrameworkCore;

using Sievert.Api.Analysis;
using Sievert.Contracts;
using Sievert.Data;
using Sievert.Data.Entities;
using Sievert.Modeling;

namespace Sievert.Api.Reports;

/// <summary>
/// Rapor uretim isi.
///
/// Uretimin tamami arka planda: HTTP istegi yalniz isi aciyor. Iptal, bolumler arasinda
/// degil, **asamalar arasinda** kontrol ediliyor - PDF olusturma tek bir kutuphane
/// cagrisi ve o cagrinin ortasinda durdurulamiyor. Bu bir sinir ve ADR 0027'de yaziyor:
/// iptal edilen bir raporun uretimi baslamissa, kutuphane isini bitirene kadar suruyor,
/// ama sonucu diske **yazilmiyor**.
/// </summary>
public sealed class ReportGenerateHandler(
    SievertContext context,
    ModelRegistry registry,
    ScoreReference reference,
    IReportArtifactStore store,
    TimeProvider clock,
    ILogger<ReportGenerateHandler> logger) : IAnalysisJobHandler
{
    public AnalysisJobKind Kind => AnalysisJobKind.ReportGenerate;

    public async Task<JobOutcome> RunAsync(AnalysisJobRun run, CancellationToken cancellation)
    {
        // AsTracking acikca: API baglaminin varsayilani NoTracking (salt-okunur uclar
        // icin dogru), ama bu kayit guncellenecek. Varsayilana guvenmek, kaydin sessizce
        // hic guncellenmemesi demekti - tam olarak bu yasandi.
        ReportArtifactRow? artifact = await context.ReportArtifacts
            .AsTracking()
            .FirstOrDefaultAsync(row => row.AnalysisJobId == run.JobId, cancellation);

        if (artifact is null)
        {
            return JobOutcome.Failed(
                ApiError.ReportNotFound, "Bu ise bagli bir rapor kaydi bulunamadi.");
        }

        await run.Progress.ReportAsync("hazirlik", 0, 4, cancellation);

        RepositoryRow? repository = await context.Repositories
            .AsNoTracking()
            .FirstOrDefaultAsync(row => row.Id == artifact.RepositoryId, cancellation);

        if (repository is null)
        {
            return await FailAsync(artifact, ApiError.RepositoryNotFound, "Depo bulunamadi.", cancellation);
        }

        ResolvedReportRequest request = ReportService.Parameters(artifact);

        ReportService service = new(context, registry, reference, new NullQueue(), store, clock);
        ReportPlanResult plan = await service.PlanAsync(repository, request, cancellation);

        if (plan.Plan is not ReportPlan ready)
        {
            return await FailAsync(artifact, plan.ErrorCode!, plan.Detail!, cancellation);
        }

        if (await run.Progress.IsCancellationRequestedAsync(cancellation))
        {
            return await CanceledAsync(artifact, 0, cancellation);
        }

        await run.Progress.ReportAsync("veri", 1, 4, cancellation);

        ReportText text = ReportText.For(artifact.Culture);
        DateTimeOffset generatedAt = clock.GetUtcNow();

        ReportModel model = await new ReportDataBuilder(context, registry, reference)
            .BuildAsync(ready, artifact.Id, generatedAt, artifact.ManifestSha256, cancellation);

        if (await run.Progress.IsCancellationRequestedAsync(cancellation))
        {
            return await CanceledAsync(artifact, 1, cancellation);
        }

        await run.Progress.ReportAsync("pdf", 2, 4, cancellation);

        RenderedReport rendered;

        try
        {
            rendered = ReportGenerator.Render(model, text);
        }
        catch (Exception error) when (error is not OperationCanceledException)
        {
            logger.LogError(error, "PDF uretilemedi. ReportId={ReportId}", artifact.Id);

            return await FailAsync(
                artifact, ApiError.ReportGenerationFailed, "PDF uretilemedi.", cancellation);
        }

        if (rendered.Content.LongLength > ReportLimits.MaximumArtifactBytes)
        {
            return await FailAsync(
                artifact,
                ApiError.ReportArtifactTooLarge,
                "Uretilen rapor izin verilen boyutu asti.",
                cancellation);
        }

        // Iptal PDF uretiminden sonra da kontrol ediliyor: buraya kadar gelen bir is
        // iptal edilmisse dosya diske **yazilmiyor**, yani ortada hazir bir artefakt
        // kalmiyor.
        if (await run.Progress.IsCancellationRequestedAsync(cancellation))
        {
            return await CanceledAsync(artifact, 2, cancellation);
        }

        await run.Progress.ReportAsync("yazma", 3, 4, cancellation);

        StoredArtifact stored;

        try
        {
            stored = await store.WriteAsync(artifact.StorageKey, rendered.Content, cancellation);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            logger.LogError(error, "Rapor dosyasi yazilamadi. ReportId={ReportId}", artifact.Id);

            return await FailAsync(
                artifact, ApiError.ReportStorageFailed, "Rapor dosyasi yazilamadi.", cancellation);
        }

        artifact.Status = ReportArtifactStatus.Ready;
        artifact.ByteLength = stored.ByteLength;
        artifact.Sha256 = stored.Sha256;
        artifact.PageCount = rendered.PageCount;
        artifact.GeneratedAtUtc = generatedAt;
        artifact.VerifiedAtUtc = clock.GetUtcNow();
        artifact.ErrorCode = null;
        artifact.ErrorMessage = null;

        await context.SaveChangesAsync(cancellation);

        await run.Progress.FlushAsync("bitti", 4, 4, cancellation);

        // ResultCount rapor icin "kac sayfa" ya da "kac commit" degil: bir is bir rapor
        // uretiyor, o yuzden hazir bir artefakt varsa 1, yoksa 0.
        return new JobOutcome(
            AnalysisJobStatus.Succeeded,
            1,
            4,
            ResultSummary: Summary(artifact, rendered));
    }

    private static string Summary(ReportArtifactRow artifact, RenderedReport rendered) =>
        System.Text.Json.JsonSerializer.Serialize(new
        {
            reportId = artifact.Id,
            culture = artifact.Culture,
            isPartial = artifact.IsPartial,
            pageCount = rendered.PageCount,
            byteLength = artifact.ByteLength,
            manifestSha256 = artifact.ManifestSha256,
        });

    /// <summary>
    /// Iptal edilen uretimde rapor kaydi da kapaniyor.
    ///
    /// Ilk surumde yalniz is iptal ediliyordu ve kayit sonsuza kadar <c>pending</c>
    /// kaliyordu: kullanici hic hazir olmayacak bir raporu bekliyordu. Olcum kosusunda
    /// gorundu.
    /// </summary>
    private async Task<JobOutcome> CanceledAsync(
        ReportArtifactRow artifact, int processed, CancellationToken cancellation)
    {
        artifact.Status = ReportArtifactStatus.Failed;
        artifact.ErrorCode = JobOutcome.CanceledCode;
        artifact.ErrorMessage = "Rapor uretimi iptal edildi.";

        await context.SaveChangesAsync(cancellation);

        store.Delete(artifact.StorageKey);

        return JobOutcome.Canceled(0, processed);
    }

    private async Task<JobOutcome> FailAsync(
        ReportArtifactRow artifact, string code, string message, CancellationToken cancellation)
    {
        artifact.Status = ReportArtifactStatus.Failed;
        artifact.ErrorCode = code;
        artifact.ErrorMessage = message;

        await context.SaveChangesAsync(cancellation);

        // Yarim kalmis bir dosya birakilmasin; yazim basarisiz olduysa zaten yok.
        store.Delete(artifact.StorageKey);

        return JobOutcome.Failed(code, message);
    }

    /// <summary>
    /// Handler icinde yeni bir is acilmiyor, o yuzden kuyruk gerekmiyor.
    /// <see cref="ReportService"/> ayni plan kodunu paylasabilsin diye bos bir uygulama.
    /// </summary>
    private sealed class NullQueue : IAnalysisJobQueue
    {
        public int Capacity => 0;

        public int Count => 0;

        public ValueTask EnqueueAsync(Guid jobId, CancellationToken cancellation = default) =>
            throw new NotSupportedException("Rapor isi icinden yeni is acilmiyor.");

        public ValueTask<Guid> DequeueAsync(CancellationToken cancellation) =>
            throw new NotSupportedException("Rapor isi kuyruktan okumuyor.");
    }
}
