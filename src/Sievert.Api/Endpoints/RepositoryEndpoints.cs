using Microsoft.EntityFrameworkCore;

using Sievert.Contracts;
using Sievert.Data;
using Sievert.Data.Entities;
using Sievert.Modeling;

namespace Sievert.Api.Endpoints;

public static class RepositoryEndpoints
{
    public static void MapRepositories(this RouteGroupBuilder api)
    {
        api.MapGet("/repositories", async (
            HttpContext context,
            DatabaseSettings settings,
            ModelRegistry registry,
            ApiOptions options,
            CancellationToken cancellation) =>
        {
            if (Database.NotReady(context, settings) is IResult unavailable)
            {
                return unavailable;
            }

            SievertContext database = Database.Open(context);

            if (!Paging.TryParse(context, options, out Paging paging, out string? error))
            {
                return Problems.BadRequest(context, error!, ApiError.InvalidPagination);
            }

            IQueryable<RepositoryRow> query = database.Repositories.OrderBy(row => row.Identity);
            int total = await query.CountAsync(cancellation);

            List<RepositoryRow> rows = await query
                .Skip(paging.Skip)
                .Take(paging.PageSize)
                .ToListAsync(cancellation);

            List<RepositoryListItem> items = [.. rows.Select(row => Describe(row, registry))];

            return Results.Ok(new PagedResponse<RepositoryListItem>(paging.Page, paging.PageSize, total, items));
        })
        .WithName("Repositories")
        .WithSummary("Taranmis depolar")
        .Produces<PagedResponse<RepositoryListItem>>()
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        api.MapGet("/repositories/{id:int}", async (
            int id,
            HttpContext context,
            DatabaseSettings settings,
            ModelRegistry registry,
            CancellationToken cancellation) =>
        {
            if (Database.NotReady(context, settings) is IResult unavailable)
            {
                return unavailable;
            }

            SievertContext database = Database.Open(context);

            RepositoryRow? repository = await database.Repositories
                .FirstOrDefaultAsync(row => row.Id == id, cancellation);

            if (repository is null)
            {
                return Problems.NotFound(context, $"{id} numarali depo yok.", ApiError.RepositoryNotFound);
            }

            IQueryable<CommitRow> commits = database.Commits.Where(row => row.RepositoryId == id);

            int count = await commits.CountAsync(cancellation);
            int bugIntroducing = await commits.CountAsync(row => row.IsBugIntroducing, cancellation);
            int bots = await commits.CountAsync(row => row.IsBot, cancellation);
            int withMetrics = await database.CommitMetrics
                .CountAsync(metric => metric.Commit!.RepositoryId == id, cancellation);

            ModelProfile? profile = registry.ForRepository(repository.Identity);

            List<string> notes = [];

            if (profile is null)
            {
                notes.Add(RiskWarning.Text(RiskWarning.UnknownRepositoryModel));
            }

            if (withMetrics < count)
            {
                notes.Add($"{count - withMetrics} commit icin turetilmis olcu yok; bunlar skorlanamaz.");
            }

            return Results.Ok(new RepositoryDetail(
                Describe(repository, registry),
                count,
                bugIntroducing,
                count == 0 ? 0.0 : (double)bugIntroducing / count,
                bots,
                withMetrics,
                profile?.ProfileCode,
                notes));
        })
        .WithName("Repository")
        .WithSummary("Tek bir depo ve etiket sayimlari")
        .Produces<RepositoryDetail>()
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        api.MapGet("/repositories/{id:int}/commits", async (
            int id,
            HttpContext context,
            DatabaseSettings settings,
            ApiOptions options,
            CancellationToken cancellation) =>
        {
            if (Database.NotReady(context, settings) is IResult unavailable)
            {
                return unavailable;
            }

            SievertContext database = Database.Open(context);

            if (!Paging.TryParse(context, options, out Paging paging, out string? error))
            {
                return Problems.BadRequest(context, error!, ApiError.InvalidPagination);
            }

            if (!await database.Repositories.AnyAsync(row => row.Id == id, cancellation))
            {
                return Problems.NotFound(context, $"{id} numarali depo yok.", ApiError.RepositoryNotFound);
            }

            // Siralama CommitOrdering kuraliyla ayni: tarih artan, esitlikte madencilik
            // sirasi (Id) artan. Burada en yeni once istendigi icin ikisi de tersine
            // ceviriliyor; kural ayni kural.
            IQueryable<CommitRow> query = database.Commits
                .Where(row => row.RepositoryId == id)
                .OrderByDescending(row => row.AuthorDateUtc)
                .ThenByDescending(row => row.Id);

            int total = await query.CountAsync(cancellation);

            List<CommitListItem> items = await query
                .Skip(paging.Skip)
                .Take(paging.PageSize)
                .Select(row => new CommitListItem(
                    row.Sha,
                    row.Sha.Substring(0, 12),
                    row.AuthorDateUtc,
                    row.MessageSubject,
                    database.CommitMetrics.Any(metric => metric.CommitId == row.Id && metric.IsFix),
                    row.IsBugIntroducing,
                    row.IsBot,
                    row.LinesAdded,
                    row.LinesDeleted,
                    row.ChangedFiles,
                    row.ChangedCSharpFiles))
                .ToListAsync(cancellation);

            return Results.Ok(new PagedResponse<CommitListItem>(paging.Page, paging.PageSize, total, items));
        })
        .WithName("RepositoryCommits")
        .WithSummary("Bir deponun commit'leri, en yeniden eskiye")
        .Produces<PagedResponse<CommitListItem>>()
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable);
    }

    private static RepositoryListItem Describe(RepositoryRow row, ModelRegistry registry) => new(
        row.Id,
        row.Name,
        row.Identity,
        row.IdentitySource,
        row.RemoteUrl,
        row.TotalCommits,
        row.FirstCommitDate,
        row.LastCommitDate,
        row.ScannedAt,
        registry.ForRepository(row.Identity) is not null);
}
