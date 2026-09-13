using System.Text.Json;

using Microsoft.EntityFrameworkCore;

using Sievert.Api.Analysis;
using Sievert.Contracts;
using Sievert.Data;
using Sievert.Data.Entities;
using Sievert.Modeling;

namespace Sievert.Api.Endpoints;

/// <summary>
/// Arka plan islerinin uclari.
///
/// Is baslatma istegi dosya sistemi yolu ALMIYOR. Taranacak klasor depo kaydindan
/// geliyor; istekten almak, kimlik dogrulamasi olmayan bir servise sunucudaki herhangi
/// bir klasoru okutmak olurdu.
/// </summary>
public static class AnalysisEndpoints
{
    /// <summary>Tekrar anahtarinin okundugu baslik.</summary>
    public const string IdempotencyHeader = "Idempotency-Key";

    public static void MapAnalyses(this RouteGroupBuilder api)
    {
        api.MapPost("/repositories/{repositoryId:int}/analyses", StartAsync)
            .WithName("StartAnalysis")
            .WithSummary("Bir depo icin arka plan analiz isi baslatir")
            .Produces<AnalysisJobResponse>(StatusCodes.Status202Accepted)
            .Produces<AnalysisJobResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        api.MapGet("/analyses/{jobId:guid}", GetAsync)
            .WithName("Analysis")
            .WithSummary("Tek bir isin durumu ve ilerlemesi")
            .Produces<AnalysisJobResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        api.MapGet("/repositories/{repositoryId:int}/analyses", ListAsync)
            .WithName("RepositoryAnalyses")
            .WithSummary("Bir deponun isleri, sayfali")
            .Produces<PagedResponse<AnalysisJobResponse>>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        api.MapPost("/analyses/{jobId:guid}/cancel", CancelAsync)
            .WithName("CancelAnalysis")
            .WithSummary("Isi iptal eder; kuyruktaysa hemen, kosuyorsa obek sinirinda")
            .Produces<AnalysisJobResponse>(StatusCodes.Status200OK)
            .Produces<AnalysisJobResponse>(StatusCodes.Status202Accepted)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        api.MapGet("/analyses/{jobId:guid}/findings", FindingsAsync)
            .WithName("AnalysisFindings")
            .WithSummary("Bir static-scan isinin bulgulari, sayfali")
            .Produces<AnalysisResultPage<StaticFindingResponse>>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        api.MapGet("/analyses/{jobId:guid}/risks", RisksAsync)
            .WithName("AnalysisRisks")
            .WithSummary("Bir risk-score-all isinin commit degerlendirmeleri, sayfali")
            .Produces<AnalysisResultPage<CommitRiskSnapshotResponse>>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);
    }

    private static async Task<IResult> StartAsync(
        int repositoryId,
        StartAnalysisRequest? request,
        HttpContext context,
        DatabaseSettings settings,
        AnalysisJobStore store,
        IAnalysisJobQueue queue,
        ModelRegistry registry,
        TimeProvider clock,
        CancellationToken cancellation)
    {
        if (Database.NotReady(context, settings) is IResult unavailable)
        {
            return unavailable;
        }

        if (AnalysisJobRow.Parse(request?.Kind) is not AnalysisJobKind kind)
        {
            return Problems.BadRequest(
                context,
                "kind alani static-scan ya da risk-score-all olmali.",
                ApiError.AnalysisKindInvalid);
        }

        SievertContext database = Database.Open(context);

        RepositoryRow? repository = await database.Repositories
            .FirstOrDefaultAsync(row => row.Id == repositoryId, cancellation);

        if (repository is null)
        {
            return Problems.NotFound(context, $"{repositoryId} numarali depo yok.", ApiError.RepositoryNotFound);
        }

        // Bilinmeyen depo icin is HIC ACILMIYOR. Acilip sonra basarisiz olmasi, kuyrukta
        // bastan basarisiz oldugu belli bir is tutmak olurdu.
        if (kind == AnalysisJobKind.RiskScoreAll && registry.ForRepository(repository.Identity) is null)
        {
            return Problems.Unprocessable(
                context,
                RiskWarning.Text(RiskWarning.UnknownRepositoryModel),
                ApiError.UnknownRepositoryModel);
        }

        string? idempotencyKey = context.Request.Headers[IdempotencyHeader].FirstOrDefault();

        JobCreateResult result = await store.CreateAsync(
            repositoryId,
            kind,
            idempotencyKey,
            clock.GetUtcNow(),
            cancellation);

        switch (result.Outcome)
        {
            case JobCreateOutcome.IdempotencyKeyInvalid:
                return Problems.BadRequest(
                    context,
                    $"{IdempotencyHeader} bos olamaz ve en fazla {AnalysisJobStore.MaximumIdempotencyKeyLength} "
                    + "karakter olabilir.",
                    ApiError.IdempotencyKeyInvalid);

            case JobCreateOutcome.IdempotencyKeyReused:
                return Problems.Conflict(
                    context,
                    $"Bu {IdempotencyHeader} baska bir depo ya da is turu icin kullanilmis.",
                    ApiError.IdempotencyKeyReused);

            case JobCreateOutcome.AlreadyActive:
                return Conflict(context, result.Job!);

            case JobCreateOutcome.ReturnedExisting:
                // Yeni satir acilmadi, o yuzden 202 degil 200: "kabul edildi" demek
                // yanlis olurdu, kabul edilen sey zaten vardi.
                Location(context, result.Job!.Id);

                return Results.Ok(Describe(result.Job));

            default:
                await queue.EnqueueAsync(result.Job!.Id, cancellation);
                Location(context, result.Job.Id);

                return Results.Accepted($"/api/v1/analyses/{result.Job.Id}", Describe(result.Job));
        }
    }

    private static async Task<IResult> GetAsync(
        Guid jobId,
        HttpContext context,
        DatabaseSettings settings,
        AnalysisJobStore store,
        CancellationToken cancellation)
    {
        if (Database.NotReady(context, settings) is IResult unavailable)
        {
            return unavailable;
        }

        return await store.FindAsync(jobId, cancellation) is AnalysisJobRow job
            ? Results.Ok(Describe(job))
            : Problems.NotFound(context, $"{jobId} numarali is yok.", ApiError.AnalysisNotFound);
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

        IQueryable<AnalysisJobRow> query = database.AnalysisJobs
            .Where(job => job.RepositoryId == repositoryId);

        if (context.Request.Query["status"].FirstOrDefault() is string rawStatus && rawStatus.Length > 0)
        {
            if (!Enum.TryParse(rawStatus, ignoreCase: true, out AnalysisJobStatus status)
                || !string.Equals(AnalysisJobRow.Name(status), rawStatus, StringComparison.OrdinalIgnoreCase))
            {
                return Problems.BadRequest(
                    context,
                    "status queued, running, succeeded, failed ya da canceled olmali.",
                    ApiError.InvalidPagination);
            }

            query = query.Where(job => job.Status == status);
        }

        if (context.Request.Query["kind"].FirstOrDefault() is string rawKind && rawKind.Length > 0)
        {
            if (AnalysisJobRow.Parse(rawKind) is not AnalysisJobKind kind)
            {
                return Problems.BadRequest(
                    context,
                    "kind static-scan ya da risk-score-all olmali.",
                    ApiError.AnalysisKindInvalid);
            }

            query = query.Where(job => job.Kind == kind);
        }

        bool oldest = string.Equals(
            context.Request.Query["order"].FirstOrDefault(),
            "oldest",
            StringComparison.OrdinalIgnoreCase);

        query = oldest
            ? query.OrderBy(job => job.RequestedAtUtc).ThenBy(job => job.Id)
            : query.OrderByDescending(job => job.RequestedAtUtc).ThenByDescending(job => job.Id);

        int total = await query.CountAsync(cancellation);

        List<AnalysisJobRow> rows = await query
            .Skip(paging.Skip)
            .Take(paging.PageSize)
            .ToListAsync(cancellation);

        return Results.Ok(new PagedResponse<AnalysisJobResponse>(
            paging.Page,
            paging.PageSize,
            total,
            [.. rows.Select(Describe)]));
    }

    /// <summary>
    /// Iptal.
    ///
    /// Kuyruktaysa <c>200</c> ve is aninda iptal oluyor. Kosuyorsa <c>202</c>: istek
    /// kaydedildi ama isin gercekten durmasi bir sonraki obek sinirinda. Terminal duruma
    /// gecmisse <c>200</c> ve is **oldugu gibi** donuyor; iptal istegi idempotent, bitmis
    /// bir isi "iptal edildi" diye yeniden yazmiyoruz (ADR 0024).
    /// </summary>
    private static async Task<IResult> CancelAsync(
        Guid jobId,
        HttpContext context,
        DatabaseSettings settings,
        AnalysisJobStore store,
        JobCancellationRegistry cancellations,
        TimeProvider clock,
        CancellationToken cancellation)
    {
        if (Database.NotReady(context, settings) is IResult unavailable)
        {
            return unavailable;
        }

        if (await store.FindAsync(jobId, cancellation) is not AnalysisJobRow job)
        {
            return Problems.NotFound(context, $"{jobId} numarali is yok.", ApiError.AnalysisNotFound);
        }

        if (AnalysisJobTransitions.IsTerminal(job.Status))
        {
            return Results.Ok(Describe(job));
        }

        DateTimeOffset now = clock.GetUtcNow();

        if (await store.CancelQueuedAsync(jobId, now, cancellation))
        {
            return Results.Ok(Describe((await store.FindAsync(jobId, cancellation))!));
        }

        // Kuyruktan cikip kosmaya baslamis olabilir; istegi kaydedip jetonu tetikliyoruz.
        await store.RequestCancellationAsync(jobId, now, cancellation);
        cancellations.Cancel(jobId);

        if (await store.FindAsync(jobId, cancellation) is not AnalysisJobRow updated)
        {
            return Problems.NotFound(context, $"{jobId} numarali is yok.", ApiError.AnalysisNotFound);
        }

        if (AnalysisJobTransitions.IsTerminal(updated.Status))
        {
            // Biz bakarken bitmis; iptal istegi bir sey degistirmedi.
            return Results.Ok(Describe(updated));
        }

        return updated.Status == AnalysisJobStatus.Running
            ? Results.Accepted($"/api/v1/analyses/{jobId}", Describe(updated))
            : Problems.Conflict(
                context,
                "Is su an iptal edilebilecek bir durumda degil; tekrar dene.",
                ApiError.AnalysisNotCancelable);
    }

    private static async Task<IResult> FindingsAsync(
        Guid jobId,
        HttpContext context,
        DatabaseSettings settings,
        AnalysisJobStore store,
        ApiOptions options,
        CancellationToken cancellation)
    {
        Preparation prepared = await Prepare(
            jobId, AnalysisJobKind.StaticScan, context, settings, store, options, cancellation);

        if (prepared.Failure is IResult failure)
        {
            return failure;
        }

        (AnalysisJobRow job, Paging paging) = (prepared.Job!, prepared.Paging);

        SievertContext database = Database.Open(context);

        IQueryable<StaticAnalysisFindingRow> query = database.StaticAnalysisFindings
            .Where(finding => finding.AnalysisJobId == jobId);

        if (context.Request.Query["ruleCode"].FirstOrDefault() is string ruleCode && ruleCode.Length > 0)
        {
            query = query.Where(finding => finding.RuleCode == ruleCode);
        }

        if (context.Request.Query["severity"].FirstOrDefault() is string severity && severity.Length > 0)
        {
            string normalised = severity.ToLowerInvariant();
            query = query.Where(finding => finding.Severity == normalised);
        }

        if (context.Request.Query["isTestCode"].FirstOrDefault() is string rawTest && rawTest.Length > 0)
        {
            if (!bool.TryParse(rawTest, out bool isTestCode))
            {
                return Problems.BadRequest(context, "isTestCode true ya da false olmali.", ApiError.InvalidPagination);
            }

            query = query.Where(finding => finding.IsTestCode == isTestCode);
        }

        if (context.Request.Query["pathPrefix"].FirstOrDefault() is string prefix && prefix.Length > 0)
        {
            // StartsWith kullaniliyor; EF bunu parametreli ve joker karakterleri
            // kacisli bir LIKE'a ceviriyor, yani kullanicinin yazdigi % ya da _ desen
            // olarak calismiyor.
            query = query.Where(finding => finding.RelativePath.StartsWith(prefix));
        }

        int total = await query.CountAsync(cancellation);

        List<StaticFindingResponse> items = await query
            .OrderBy(finding => finding.RelativePath)
            .ThenBy(finding => finding.Line)
            .ThenBy(finding => finding.RuleCode)
            .Skip(paging.Skip)
            .Take(paging.PageSize)
            .Select(finding => new StaticFindingResponse(
                finding.RuleCode,
                finding.Severity,
                finding.RelativePath,
                finding.Line,
                finding.Column,
                finding.MemberName,
                finding.Message,
                finding.Rationale,
                finding.IsTestCode))
            .ToListAsync(cancellation);

        return Results.Ok(Page(job, paging, total, items));
    }

    private static async Task<IResult> RisksAsync(
        Guid jobId,
        HttpContext context,
        DatabaseSettings settings,
        AnalysisJobStore store,
        ApiOptions options,
        CancellationToken cancellation)
    {
        Preparation prepared = await Prepare(
            jobId, AnalysisJobKind.RiskScoreAll, context, settings, store, options, cancellation);

        if (prepared.Failure is IResult failure)
        {
            return failure;
        }

        (AnalysisJobRow job, Paging paging) = (prepared.Job!, prepared.Paging);

        SievertContext database = Database.Open(context);

        IQueryable<CommitRiskSnapshotRow> query = database.CommitRiskSnapshots
            .Where(snapshot => snapshot.AnalysisJobId == jobId);

        query = context.Request.Query["order"].FirstOrDefault() switch
        {
            "oldest" => query.OrderBy(snapshot => snapshot.Commit!.AuthorDateUtc).ThenBy(snapshot => snapshot.CommitId),
            "newest" => query
                .OrderByDescending(snapshot => snapshot.Commit!.AuthorDateUtc)
                .ThenByDescending(snapshot => snapshot.CommitId),
            _ => query.OrderByDescending(snapshot => snapshot.RawModelScore).ThenBy(snapshot => snapshot.CommitId),
        };

        int total = await query.CountAsync(cancellation);

        List<CommitRiskSnapshotResponse> items = await query
            .Skip(paging.Skip)
            .Take(paging.PageSize)
            .Select(snapshot => new CommitRiskSnapshotResponse(
                snapshot.Commit!.Sha,
                snapshot.Commit.Sha.Substring(0, 12),
                snapshot.Commit.AuthorDateUtc,
                snapshot.Commit.MessageSubject,
                snapshot.RawModelScore,
                snapshot.RiskIndex,
                snapshot.DecisionAt05,
                snapshot.DecisionAtTrainThreshold,
                snapshot.TrainThreshold,
                snapshot.ModelProfile,
                snapshot.ModelChecksum,
                snapshot.IsCalibrated,
                new List<string>()))
            .ToListAsync(cancellation);

        // Uyari kodlari JSON metin olarak duruyor; cozumu bellekte yapiliyor ki sorgu
        // tarafinda JSON ayristirmaya bagimli olmayalim.
        List<string> raw = await query
            .Skip(paging.Skip)
            .Take(paging.PageSize)
            .Select(snapshot => snapshot.WarningCodes)
            .ToListAsync(cancellation);

        for (int index = 0; index < items.Count; index++)
        {
            items[index] = items[index] with
            {
                WarningCodes = JsonSerializer.Deserialize<List<string>>(raw[index]) ?? [],
            };
        }

        return Results.Ok(Page(job, paging, total, items));
    }

    /// <summary>Ortak kontrollerin sonucu: ya bir is ve sayfalama, ya da hata cevabi.</summary>
    private sealed record Preparation(AnalysisJobRow? Job, Paging Paging, IResult? Failure)
    {
        public static Preparation Refused(IResult failure) => new(null, default, failure);
    }

    /// <summary>
    /// Sonuc uclarinin ortak kontrolleri: veritabani hazir mi, sayfalama gecerli mi, is
    /// var mi, turu dogru mu.
    /// </summary>
    private static async Task<Preparation> Prepare(
        Guid jobId,
        AnalysisJobKind expected,
        HttpContext context,
        DatabaseSettings settings,
        AnalysisJobStore store,
        ApiOptions options,
        CancellationToken cancellation)
    {
        if (Database.NotReady(context, settings) is IResult unavailable)
        {
            return Preparation.Refused(unavailable);
        }

        if (!Paging.TryParse(context, options, out Paging paging, out string? error))
        {
            return Preparation.Refused(Problems.BadRequest(context, error!, ApiError.InvalidPagination));
        }

        if (await store.FindAsync(jobId, cancellation) is not AnalysisJobRow job)
        {
            return Preparation.Refused(
                Problems.NotFound(context, $"{jobId} numarali is yok.", ApiError.AnalysisNotFound));
        }

        return job.Kind == expected
            ? new Preparation(job, paging, null)
            : Preparation.Refused(Problems.Conflict(
                context,
                $"Bu is {AnalysisJobRow.Name(job.Kind)} turunde; {AnalysisJobRow.Name(expected)} sonucu istendi.",
                ApiError.AnalysisResultTypeMismatch));
    }

    private static AnalysisResultPage<T> Page<T>(AnalysisJobRow job, Paging paging, int total, IReadOnlyList<T> items)
    {
        bool partial = !job.IsResultComplete;

        return new AnalysisResultPage<T>(
            job.Id,
            AnalysisJobRow.Name(job.Status),
            job.IsResultComplete,
            partial,
            partial
                ? "Is basariyla bitmedi; bu satirlar kismi olabilir ve tam sonuc gibi kullanilmamali."
                : null,
            job.SourceHeadSha,
            job.SourceHeadShortSha,
            job.SourceTreeState,
            job.SourceStateVerifiedAtUtc is not null,
            job.SourceCommitChangedDuringAnalysis,
            paging.Page,
            paging.PageSize,
            total,
            items);
    }

    private static IResult Conflict(HttpContext context, AnalysisJobRow active) =>
        Problems.Create(
            context,
            StatusCodes.Status409Conflict,
            "Zaten aktif bir is var",
            $"Bu depo ve is turu icin {AnalysisJobRow.Name(active.Status)} durumda bir is var.",
            ApiError.AnalysisAlreadyActive,
            new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["activeJobId"] = active.Id,
                ["activeJobUrl"] = $"/api/v1/analyses/{active.Id}",
            });

    private static void Location(HttpContext context, Guid jobId) =>
        context.Response.Headers.Location = $"/api/v1/analyses/{jobId}";

    private static AnalysisJobResponse Describe(AnalysisJobRow job)
    {
        List<AnalysisLink> links = [new AnalysisLink("self", $"/api/v1/analyses/{job.Id}")];

        if (AnalysisJobTransitions.IsCancelable(job.Status))
        {
            links.Add(new AnalysisLink("cancel", $"/api/v1/analyses/{job.Id}/cancel"));
        }

        links.Add(job.Kind == AnalysisJobKind.StaticScan
            ? new AnalysisLink("findings", $"/api/v1/analyses/{job.Id}/findings")
            : new AnalysisLink("risks", $"/api/v1/analyses/{job.Id}/risks"));

        return new AnalysisJobResponse(
            job.Id,
            job.RepositoryId,
            AnalysisJobRow.Name(job.Kind),
            AnalysisJobRow.Name(job.Status),
            job.RequestedAtUtc,
            job.StartedAtUtc,
            job.CompletedAtUtc,
            job.CurrentPhase,
            job.ProcessedItems,
            job.TotalItems,
            job.ProgressPercent,
            job.CancellationRequestedAtUtc is not null,
            job.ResultCount,
            job.IsResultComplete,
            job.ErrorCode,
            job.ErrorMessage,
            job.ResultSummary is null ? null : JsonDocument.Parse(job.ResultSummary).RootElement.Clone(),
            job.SourceHeadSha,
            job.SourceHeadShortSha,
            job.SourceTreeState,
            job.SourceStateVerifiedAtUtc is not null,
            job.SourceCommitChangedDuringAnalysis,
            links);
    }
}
