using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Net.Http.Headers;

using Sievert.Api.Reports;
using Sievert.Contracts;
using Sievert.Data;
using Sievert.Data.Entities;

namespace Sievert.Api.Endpoints;

/// <summary>
/// PDF rapor uclari.
///
/// Uretim burada yapilmiyor: istek bir arka plan isi aciyor ve 202 donuyor. Indirme ucu
/// dosyayi vermeden once ozetini yeniden dogruluyor - diskteki dosya degismisse indirme
/// **baslamiyor**, cunku akis basladiktan sonra <c>ProblemDetails</c> donulemez.
/// </summary>
public static class ReportEndpoints
{
    public const string IdempotencyHeader = "Idempotency-Key";

    public static void MapReports(this RouteGroupBuilder api)
    {
        api.MapPost("/repositories/{repositoryId:int}/reports", CreateAsync)
            .WithName("CreateReport")
            .WithSummary("Kaydedilmis bir risk sonucundan PDF rapor uretimini baslatir")
            .WithDescription(
                "Rapor arka planda uretiliyor; cevap 202 ve isin adresi. Varsayilan olarak "
                + "yalniz tamamlanmis bir risk isi raporlanir; kismi sonuc icin "
                + "includePartial=true acikca verilmeli ve rapor her ilgili bolumde kismi "
                + "oldugunu yazar. Ayni Idempotency-Key ayni govdeyle tekrar gonderilirse "
                + "yeni is acilmaz, ayni rapor doner.")
            .Produces<ReportAcceptedResponse>(StatusCodes.Status202Accepted)
            .Produces<ReportAcceptedResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        api.MapGet("/repositories/{repositoryId:int}/reports", ListAsync)
            .WithName("ListReports")
            .WithSummary("Bir deponun raporlari, en yeniden eskiye")
            .Produces<PagedResponse<ReportSummaryResponse>>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        api.MapGet("/reports/{reportId:guid}", GetAsync)
            .WithName("GetReport")
            .WithSummary("Rapor artefaktinin durumu ve ozeti")
            .WithDescription(
                "Fiziksel dosya yolu donmez. PDF'in SHA-256 ozeti yalniz artefakt hazir "
                + "oldugunda dolu; indirme cevabinin ETag basliginda da ayni deger var.")
            .Produces<ReportResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        api.MapGet("/reports/{reportId:guid}/manifest", ManifestAsync)
            .WithName("GetReportManifest")
            .WithSummary("Raporun canonical girdi manifesti")
            .WithDescription(
                "Raporun neyden uretildigini soyleyen canonical JSON. Ayni veri ve ayni "
                + "parametre ayni baytlari ve ayni SHA-256'yi verir; uretim zamani bu "
                + "ozete dahil degildir.")
            .Produces<string>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        api.MapGet("/reports/{reportId:guid}/download", DownloadAsync)
            .WithName("DownloadReport")
            .WithSummary("Hazir raporun PDF dosyasi")
            .WithDescription(
                "Dosya verilmeden once boyutu ve SHA-256 ozeti yeniden dogrulanir. "
                + "Uymazsa kayit corrupted isaretlenir ve dosya gonderilmez.")
            .Produces<byte[]>(StatusCodes.Status200OK, "application/pdf")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);
    }

    private static async Task<IResult> CreateAsync(
        int repositoryId,
        ReportRequest? request,
        HttpContext context,
        DatabaseSettings settings,
        ReportService service,
        CancellationToken cancellation)
    {
        if (Database.NotReady(context, settings) is IResult unavailable)
        {
            return unavailable;
        }

        string? key = context.Request.Headers[IdempotencyHeader].FirstOrDefault();

        ReportRequestResult result = await service.RequestAsync(repositoryId, request, key, cancellation);

        if (result.Artifact is not ReportArtifactRow artifact)
        {
            return result.ErrorCode switch
            {
                ApiError.RepositoryNotFound => Problems.NotFound(context, result.Detail!, result.ErrorCode),
                ApiError.ReportRiskJobNotFound => Problems.NotFound(context, result.Detail!, result.ErrorCode),
                ApiError.IdempotencyKeyReused => Problems.Conflict(context, result.Detail!, result.ErrorCode),
                ApiError.ReportRequestInvalid => Problems.BadRequest(context, result.Detail!, result.ErrorCode),
                ApiError.IdempotencyKeyInvalid => Problems.BadRequest(context, result.Detail!, result.ErrorCode),
                ApiError.ReportParameterInvalid => Problems.BadRequest(context, result.Detail!, result.ErrorCode),
                ApiError.ReportCultureNotSupported => Problems.BadRequest(context, result.Detail!, result.ErrorCode),
                _ => Problems.Unprocessable(context, result.Detail!, result.ErrorCode!),
            };
        }

        ReportAcceptedResponse accepted = new(
            artifact.Id,
            artifact.AnalysisJobId,
            ReportArtifactRow.Name(artifact.Status),
            artifact.ManifestSha256,
            artifact.IsPartial,
            Links(artifact));

        // Ayni istek tekrar geldiyse yeni bir sey yaratilmadi; 202 yerine 200 donuyor ki
        // istemci "yeni is acildi" sanmasin.
        return result.Created
            ? Results.Accepted($"/api/v1/reports/{artifact.Id}", accepted)
            : Results.Ok(accepted);
    }

    private static async Task<IResult> ListAsync(
        int repositoryId,
        HttpContext context,
        DatabaseSettings settings,
        ApiOptions options,
        CancellationToken cancellation)
    {
        if (Database.NotReady(context, settings) is IResult unavailable)
        {
            return unavailable;
        }

        if (!Paging.TryParse(context, options, out Paging paging, out string? error))
        {
            return Problems.BadRequest(context, error!, ApiError.InvalidPagination);
        }

        SievertContext database = Database.Open(context);

        if (!await database.Repositories.AnyAsync(row => row.Id == repositoryId, cancellation))
        {
            return Problems.NotFound(context, $"{repositoryId} numarali depo yok.", ApiError.RepositoryNotFound);
        }

        IQueryable<ReportArtifactRow> query = database.ReportArtifacts
            .AsNoTracking()
            .Where(row => row.RepositoryId == repositoryId);

        if (context.Request.Query["status"].FirstOrDefault() is string rawStatus && rawStatus.Length > 0)
        {
            if (!Enum.TryParse(rawStatus, ignoreCase: true, out ReportArtifactStatus status)
                || !Enum.IsDefined(status))
            {
                return Problems.BadRequest(
                    context,
                    "status pending, ready, failed ya da corrupted olmali.",
                    ApiError.ReportParameterInvalid);
            }

            query = query.Where(row => row.Status == status);
        }

        if (context.Request.Query["culture"].FirstOrDefault() is string rawCulture && rawCulture.Length > 0)
        {
            if (ReportCulture.Normalize(rawCulture) is not string culture)
            {
                return Problems.BadRequest(
                    context,
                    $"culture yalniz {string.Join(" ya da ", ReportCulture.Supported)} olabilir.",
                    ApiError.ReportCultureNotSupported);
            }

            query = query.Where(row => row.Culture == culture);
        }

        if (context.Request.Query["partial"].FirstOrDefault() is string rawPartial && rawPartial.Length > 0)
        {
            if (!bool.TryParse(rawPartial, out bool partial))
            {
                return Problems.BadRequest(
                    context, "partial true ya da false olmali.", ApiError.ReportParameterInvalid);
            }

            query = query.Where(row => row.IsPartial == partial);
        }

        bool oldest = context.Request.Query["order"].FirstOrDefault() == "oldest";

        query = oldest
            ? query.OrderBy(row => row.RequestedAtUtc).ThenBy(row => row.Id)
            : query.OrderByDescending(row => row.RequestedAtUtc).ThenByDescending(row => row.Id);

        int total = await query.CountAsync(cancellation);

        List<ReportSummaryResponse> items = await query
            .Skip((paging.Page - 1) * paging.PageSize)
            .Take(paging.PageSize)
            .Select(row => new ReportSummaryResponse(
                row.Id,
                row.RepositoryId,
                row.RiskAnalysisJobId,
                ReportArtifactRow.Name(row.Status),
                row.Culture,
                row.SafeFileName,
                row.IsPartial,
                row.ByteLength,
                row.PageCount,
                row.RequestedAtUtc,
                row.GeneratedAtUtc,
                row.ErrorCode))
            .ToListAsync(cancellation);

        return Results.Ok(new PagedResponse<ReportSummaryResponse>(
            paging.Page, paging.PageSize, total, items));
    }

    private static async Task<IResult> GetAsync(
        Guid reportId,
        HttpContext context,
        DatabaseSettings settings,
        CancellationToken cancellation)
    {
        if (Database.NotReady(context, settings) is IResult unavailable)
        {
            return unavailable;
        }

        if (await Find(context, reportId, cancellation) is not ReportArtifactRow artifact)
        {
            return Problems.NotFound(context, "Rapor bulunamadi.", ApiError.ReportNotFound);
        }

        ResolvedReportRequest parameters = ReportService.Parameters(artifact);

        return Results.Ok(new ReportResponse(
            artifact.Id,
            artifact.RepositoryId,
            artifact.AnalysisJobId,
            artifact.RiskAnalysisJobId,
            artifact.StaticAnalysisJobId,
            ReportArtifactRow.Name(artifact.Status),
            artifact.Format,
            artifact.Culture,
            artifact.SafeFileName,
            artifact.IsPartial,
            artifact.ManifestSha256,
            artifact.Sha256,
            artifact.ByteLength,
            artifact.PageCount,
            artifact.RequestedAtUtc,
            artifact.GeneratedAtUtc,
            artifact.VerifiedAtUtc,
            artifact.ErrorCode,
            artifact.ErrorMessage,
            artifact.SchemaVersion,
            artifact.GeneratorVersion,
            new ReportParameters(
                parameters.IncludePartial,
                parameters.CommitWindow,
                parameters.FileLimit,
                parameters.TimelineCount,
                parameters.TopCommitCount,
                parameters.FindingLimit,
                parameters.Title,
                parameters.Notes),
            Links(artifact)));
    }

    private static async Task<IResult> ManifestAsync(
        Guid reportId,
        HttpContext context,
        DatabaseSettings settings,
        CancellationToken cancellation)
    {
        if (Database.NotReady(context, settings) is IResult unavailable)
        {
            return unavailable;
        }

        if (await Find(context, reportId, cancellation) is not ReportArtifactRow artifact)
        {
            return Problems.NotFound(context, "Rapor bulunamadi.", ApiError.ReportNotFound);
        }

        // ETag manifest ozeti: manifest degismez, o yuzden istemci onbellekleyebilir.
        context.Response.Headers.ETag = $"\"{artifact.ManifestSha256}\"";

        return Results.Text(artifact.ManifestJson, "application/json", System.Text.Encoding.UTF8);
    }

    private static async Task<IResult> DownloadAsync(
        Guid reportId,
        HttpContext context,
        DatabaseSettings settings,
        IReportArtifactStore store,
        TimeProvider clock,
        CancellationToken cancellation)
    {
        if (Database.NotReady(context, settings) is IResult unavailable)
        {
            return unavailable;
        }

        SievertContext database = Database.Open(context);

        // AsTracking: bu akista kayit guncellenebiliyor (dogrulama basarisizsa
        // corrupted, basariliysa VerifiedAtUtc). Baglamin varsayilani NoTracking.
        ReportArtifactRow? artifact = await database.ReportArtifacts
            .AsTracking()
            .FirstOrDefaultAsync(row => row.Id == reportId, cancellation);

        if (artifact is null)
        {
            return Problems.NotFound(context, "Rapor bulunamadi.", ApiError.ReportNotFound);
        }

        switch (artifact.Status)
        {
            case ReportArtifactStatus.Pending:
                return Problems.Conflict(context, "Rapor henuz hazir degil.", ApiError.ReportNotReady);

            case ReportArtifactStatus.Failed:
                return Problems.Conflict(
                    context,
                    artifact.ErrorMessage ?? "Rapor uretilemedi.",
                    ApiError.ReportGenerationFailed);

            case ReportArtifactStatus.Corrupted:
                return Problems.Create(
                    context,
                    StatusCodes.Status500InternalServerError,
                    "Rapor dosyasi bozuk",
                    "Rapor dosyasi dogrulanamadi ve gonderilmedi.",
                    ApiError.ReportArtifactCorrupted);
        }

        if (artifact.Sha256 is not string expected || artifact.ByteLength is not long length)
        {
            return Problems.Conflict(context, "Rapor henuz hazir degil.", ApiError.ReportNotReady);
        }

        // Akis baslamadan once dogrulama: baslarsa artik hata cevabi donulemez.
        ArtifactCheck check = await store.VerifyAsync(artifact.StorageKey, length, expected, cancellation);

        if (!check.Ok)
        {
            artifact.Status = ReportArtifactStatus.Corrupted;
            artifact.ErrorCode = check.ErrorCode;
            artifact.ErrorMessage = "Dosya diskte bulunamadi ya da ozeti tutmuyor.";
            artifact.VerifiedAtUtc = clock.GetUtcNow();

            await database.SaveChangesAsync(cancellation);

            return check.ErrorCode == ApiError.ReportArtifactTooLarge
                ? Problems.Conflict(
                    context, "Rapor dosyasi izin verilen boyutu asiyor.", ApiError.ReportArtifactTooLarge)
                : Problems.Create(
                    context,
                    StatusCodes.Status500InternalServerError,
                    "Rapor dosyasi bozuk",
                    "Rapor dosyasi dogrulanamadi ve gonderilmedi.",
                    ApiError.ReportArtifactCorrupted);
        }

        artifact.VerifiedAtUtc = clock.GetUtcNow();
        await database.SaveChangesAsync(cancellation);

        byte[] content = await store.ReadAsync(artifact.StorageKey, cancellation);

        // Dosya adi RFC 6266'ya gore yaziliyor; ad zaten kontrol karakterinden arindirilmis.
        context.Response.Headers.ContentDisposition =
            new ContentDispositionHeaderValue("attachment")
            {
                FileName = artifact.SafeFileName,
            }.ToString();

        context.Response.Headers.ETag = $"\"{expected}\"";

        return Results.File(content, artifact.ContentType, artifact.SafeFileName, enableRangeProcessing: false);
    }

    private static async Task<ReportArtifactRow?> Find(
        HttpContext context, Guid reportId, CancellationToken cancellation) =>
        await Database.Open(context).ReportArtifacts
            .AsNoTracking()
            .FirstOrDefaultAsync(row => row.Id == reportId, cancellation);

    private static IReadOnlyList<AnalysisLink> Links(ReportArtifactRow artifact)
    {
        List<AnalysisLink> links =
        [
            new AnalysisLink("self", $"/api/v1/reports/{artifact.Id}"),
            new AnalysisLink("manifest", $"/api/v1/reports/{artifact.Id}/manifest"),
            new AnalysisLink("job", $"/api/v1/analyses/{artifact.AnalysisJobId}"),
        ];

        // Indirme adresi yalniz hazir raporda: olmayan bir dosyanin adresini vermek,
        // istemciyi hataya davet etmek olurdu.
        if (artifact.Status == ReportArtifactStatus.Ready)
        {
            links.Add(new AnalysisLink("download", $"/api/v1/reports/{artifact.Id}/download"));
        }

        return links;
    }
}
