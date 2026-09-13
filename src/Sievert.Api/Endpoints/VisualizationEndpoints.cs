using Microsoft.EntityFrameworkCore;

using Sievert.Api.Visualizations;
using Sievert.Contracts;
using Sievert.Data;
using Sievert.Data.Entities;
using Sievert.Modeling;

namespace Sievert.Api.Endpoints;

/// <summary>
/// Gorsellestirme uclari.
///
/// Ikisi de **bir risk isine** bagli. Sebep: harita ve cizelge, belirli bir kosuda
/// kaydedilmis skorlari gosteriyor. Depoya bagli olsalardi "hangi model, hangi kosu"
/// sorusunun cevabi kaybolur ve iki farkli kosunun sonucu tek bir resimde karisirdi.
/// </summary>
public static class VisualizationEndpoints
{
    public static void MapVisualizations(this RouteGroupBuilder api)
    {
        api.MapGet("/repositories/{repositoryId:int}/visualizations/file-activity", FileActivityAsync)
            .WithName("FileActivity")
            .WithSummary("Secilen commit penceresinde dosyalara dokunan commit'lerin endeks ozeti")
            .WithDescription(
                "Renk icin kullanilan deger meanRiskIndex'tir: dosyaya dokunan commit'lerin "
                + "goreli risk endekslerinin ortalamasi. Dosyanin kendisi skorlanmiyor. "
                + "Statik bulgu sayisi istege bagli olarak eklenebilir ama renge ve varsayilan "
                + "siralamaya girmez. Cevaptaki rankingScope siralamanin hangi kumede "
                + "yapildigini soyler: kismi bir iste siralama yalniz yazilmis satirlar "
                + "icindedir, deponun tamami icinde degil.")
            .Produces<FileActivityResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        api.MapGet("/repositories/{repositoryId:int}/visualizations/risk-timeline", TimelineAsync)
            .WithName("RiskTimeline")
            .WithSummary("Secilen isteki en yeni N commit'in goreli risk endeksi, kronolojik")
            .WithDescription(
                "Y ekseni yalniz goreli risk endeksi; ham model skoru ayni eksende cizilmez. "
                + "Iki esik cizgisinin endeks karsiligi API tarafinda ayni skor referansiyla "
                + "hesaplanip doner. X ekseni commit tarihine gore olcekleniyor; pencerede "
                + "butun commit'ler ayni tarihteyse eksen sira numarasina dusuyor ve cevapta "
                + "TIMELINE_USES_ORDINAL_AXIS uyarisi donuyor.")
            .Produces<RiskTimelineResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);
    }

    private static async Task<IResult> FileActivityAsync(
        int repositoryId,
        HttpContext context,
        DatabaseSettings settings,
        CancellationToken cancellation)
    {
        Prepared ready = await PrepareAsync(repositoryId, context, settings, cancellation);

        if (ready.Job is null)
        {
            return ready.Failure!;
        }

        if (!Number(context, "commitWindow", VisualizationLimits.DefaultCommitWindow, out int window)
            || !VisualizationLimits.IsWindow(window))
        {
            return Problems.BadRequest(
                context,
                $"commitWindow {VisualizationLimits.MinimumCommitWindow} ile "
                + $"{VisualizationLimits.MaximumCommitWindow} arasinda olmali.",
                ApiError.VisualizationWindowInvalid);
        }

        if (!Number(context, "limit", VisualizationLimits.DefaultFileLimit, out int limit)
            || !VisualizationLimits.IsFileLimit(limit))
        {
            return Problems.BadRequest(
                context,
                $"limit {VisualizationLimits.MinimumFileLimit} ile "
                + $"{VisualizationLimits.MaximumFileLimit} arasinda olmali.",
                ApiError.VisualizationLimitInvalid);
        }

        string sort = context.Request.Query["sort"].FirstOrDefault() is string given && given.Length > 0
            ? given
            : FileActivitySort.MeanRiskDescending;

        if (!VisualizationLimits.IsSort(sort))
        {
            return Problems.BadRequest(
                context,
                $"sort su degerlerden biri olmali: {string.Join(", ", FileActivitySort.All)}.",
                ApiError.VisualizationSortInvalid);
        }

        VisualizationQueries queries = ready.Queries!;
        Guid? staticJobId = null;
        Dictionary<string, int>? staticCounts = null;

        if (context.Request.Query["staticAnalysisJobId"].FirstOrDefault() is string rawStatic && rawStatic.Length > 0)
        {
            if (!Guid.TryParse(rawStatic, out Guid parsed))
            {
                return Problems.Unprocessable(
                    context,
                    "staticAnalysisJobId bir kimlik olarak okunamadi.",
                    ApiError.StaticAnalysisJobIncompatible);
            }

            if (await queries.JobAsync(parsed, cancellation) is not AnalysisJobRow staticJob
                || staticJob.RepositoryId != repositoryId
                || staticJob.Kind != AnalysisJobKind.StaticScan
                || staticJob.Status != AnalysisJobStatus.Succeeded
                || !staticJob.IsResultComplete)
            {
                return Problems.Unprocessable(
                    context,
                    "Ustuste bindirilecek tarama ayni depoya ait, tamamlanmis bir static-scan isi olmali.",
                    ApiError.StaticAnalysisJobIncompatible);
            }

            staticJobId = parsed;
            staticCounts = await queries.StaticFindingCountsAsync(parsed, cancellation);
        }

        List<WindowCommit> commits = await queries.WindowAsync(ready.Job.Id, window, cancellation);
        List<WindowFile> files = await queries.FilesAsync(ready.Job.Id, window, cancellation);

        List<FileActivityItem> items = FileActivityBuilder.Build(
            commits, files, limit, sort, staticCounts, staticJobId, out int beforeLimit);

        List<string> warnings = [];

        if (!ready.Job.IsResultComplete)
        {
            warnings.Add(VisualizationWarning.PartialAnalysisResult);
        }

        if (commits.Count < window)
        {
            warnings.Add(VisualizationWarning.WindowLargerThanResults);
        }

        if (beforeLimit > items.Count)
        {
            warnings.Add(VisualizationWarning.FileLimitApplied);
        }

        return Results.Ok(new FileActivityResponse(
            repositoryId,
            ready.Job.Id,
            ready.Job.SourceHeadSha,
            window,
            commits.Count,
            beforeLimit,
            items.Count,
            limit,
            sort,
            ready.Job.IsResultComplete,
            !ready.Job.IsResultComplete,
            Scope(ready.Job),
            ready.Profile,
            IsCalibrated: false,
            items,
            warnings));
    }

    private static async Task<IResult> TimelineAsync(
        int repositoryId,
        HttpContext context,
        DatabaseSettings settings,
        ScoreReference reference,
        CancellationToken cancellation)
    {
        Prepared ready = await PrepareAsync(repositoryId, context, settings, cancellation);

        if (ready.Job is null)
        {
            return ready.Failure!;
        }

        if (!Number(context, "count", VisualizationLimits.DefaultTimelineCount, out int count)
            || !VisualizationLimits.IsTimelineCount(count))
        {
            return Problems.BadRequest(
                context,
                $"count {VisualizationLimits.MinimumTimelineCount} ile "
                + $"{VisualizationLimits.MaximumTimelineCount} arasinda olmali.",
                ApiError.VisualizationCountInvalid);
        }

        List<WindowCommit> commits = await ready.Queries!.WindowAsync(ready.Job.Id, count, cancellation);
        List<RiskTimelinePoint> points = TimelineBuilder.Build(commits);

        // Esik cizgilerinin endeks karsiligi API'de hesaplaniyor. Panelde hesaplansaydi
        // ayni tanimin ikinci bir kopyasi olurdu ve iki taraf zamanla ayrisirdi.
        double at05 = 0;
        double atTrain = 0;
        double trainThreshold = points.Count > 0 ? points[0].TrainThreshold : 0;

        if (reference.For(ready.Profile) is ScoreDistribution distribution)
        {
            at05 = distribution.RiskIndex(0.5);
            atTrain = distribution.RiskIndex(trainThreshold);
        }

        List<string> warnings = [];

        if (!ready.Job.IsResultComplete)
        {
            warnings.Add(VisualizationWarning.PartialAnalysisResult);
        }

        if (Math.Abs(at05 - atTrain) < 0.05)
        {
            warnings.Add(VisualizationWarning.ThresholdsCoincide);
        }

        if (points.Count > 1 && points[0].AuthorDateUtc == points[^1].AuthorDateUtc)
        {
            warnings.Add(VisualizationWarning.TimelineUsesOrdinalAxis);
        }

        return Results.Ok(new RiskTimelineResponse(
            repositoryId,
            ready.Job.Id,
            count,
            points.Count,
            ready.Job.IsResultComplete,
            !ready.Job.IsResultComplete,
            Scope(ready.Job),
            ready.Profile,
            IsCalibrated: false,
            at05,
            atTrain,
            points.Count > 0 ? points[0].AuthorDateUtc : null,
            points.Count > 0 ? points[^1].AuthorDateUtc : null,
            points,
            warnings));
    }

    /// <summary>
    /// Siralamanin neye dayandigi.
    ///
    /// Yarida kalmis bir iste "en yuksek endeks" cumlesi butun depoyu degil, o ana kadar
    /// yazilmis satirlari anlatiyor. Bu farki alan olarak donmek, arayuzun cumleyi dogru
    /// kurmasini saglamanin tek yolu.
    /// </summary>
    private static string Scope(AnalysisJobRow job) => job.IsResultComplete
        ? RankingScope.CompleteAnalysis
        : RankingScope.WrittenResultsOnly;

    private static bool Number(HttpContext context, string name, int fallback, out int value)
    {
        value = fallback;

        string? raw = context.Request.Query[name].FirstOrDefault();

        return string.IsNullOrEmpty(raw) || int.TryParse(raw, out value);
    }

    /// <summary>Ortak dogrulamalarin sonucu.</summary>
    private sealed record Prepared(AnalysisJobRow? Job, VisualizationQueries? Queries, string Profile, IResult? Failure);

    private static async Task<Prepared> PrepareAsync(
        int repositoryId,
        HttpContext context,
        DatabaseSettings settings,
        CancellationToken cancellation)
    {
        if (Database.NotReady(context, settings) is IResult unavailable)
        {
            return new Prepared(null, null, string.Empty, unavailable);
        }

        SievertContext database = Database.Open(context);
        VisualizationQueries queries = new(database);

        if (!await database.Repositories.AsNoTracking().AnyAsync(row => row.Id == repositoryId, cancellation))
        {
            return Refused(queries, Problems.NotFound(
                context, $"{repositoryId} numarali depo yok.", ApiError.RepositoryNotFound));
        }

        if (context.Request.Query["analysisJobId"].FirstOrDefault() is not string rawJob || rawJob.Length == 0)
        {
            return Refused(queries, Problems.BadRequest(
                context,
                "analysisJobId zorunlu: harita ve cizelge belirli bir risk isinin sonucunu gosteriyor.",
                ApiError.VisualizationJobRequired));
        }

        if (!Guid.TryParse(rawJob, out Guid jobId)
            || await queries.JobAsync(jobId, cancellation) is not AnalysisJobRow job)
        {
            return Refused(queries, Problems.NotFound(
                context, "Verilen analiz isi yok.", ApiError.VisualizationJobNotFound));
        }

        if (job.RepositoryId != repositoryId)
        {
            return Refused(queries, Problems.Unprocessable(
                context, "Verilen analiz isi baska bir depoya ait.", ApiError.VisualizationJobRepositoryMismatch));
        }

        if (job.Kind != AnalysisJobKind.RiskScoreAll)
        {
            return Refused(queries, Problems.Unprocessable(
                context,
                "Gorsellestirme icin risk-score-all turunde bir is gerekiyor.",
                ApiError.VisualizationJobKindMismatch));
        }

        if (await queries.ProfileAsync(jobId, cancellation) is not string profile)
        {
            return Refused(queries, Problems.Unprocessable(
                context,
                "Bu iste kaydedilmis hicbir commit degerlendirmesi yok.",
                ApiError.VisualizationNoResults));
        }

        return new Prepared(job, queries, profile, null);
    }

    private static Prepared Refused(VisualizationQueries queries, IResult failure) =>
        new(null, queries, string.Empty, failure);
}
