using Microsoft.EntityFrameworkCore;

using Sievert.Contracts;
using Sievert.Data;
using Sievert.Data.Entities;

namespace Sievert.Api.Visualizations;

/// <summary>
/// Gorsellestirme uclarinin veritabani sorgulari.
///
/// Iki sorgu var ve ikisi de pencereyle sinirli:
///
/// 1. Isin **en yeni N** commit anlik goruntusu, commit ve olcu satirlariyla tek
///    birlestirmede. En fazla 1000 satir.
/// 2. Yalnizca o commit'lerin C# dosya satirlari. Commit kimlikleri alt sorgu olarak
///    gidiyor, bin parametre olarak degil.
///
/// Toplama bellekte yapiliyor. Sebep sunlar: ayni commit ayni dosya icin birden fazla
/// satir uretmisse once tekillestirmek gerekiyor (yoksa endeks iki kez sayiliyor), "son
/// dokunus" ve "son bes commit" ayni gezinmede cikiyor, ve okunan satir sayisi pencereyle
/// sinirli - butun tarih degil. Olculen satir sayilari
/// <c>docs/olcumler/asama6-gorsellestirme.md</c> icinde.
/// </summary>
public sealed class VisualizationQueries(SievertContext context)
{
    /// <summary>
    /// Penceredeki commit'ler: en yeniden eskiye, sonra kronolojik siraya cevrilmek uzere.
    ///
    /// Siralama <c>CommitOrdering</c> kuraliyla ayni - tarih, esitlikte madencilik sirasi -
    /// yalniz yonu ters, cunku istenen "en yeni N".
    /// </summary>
    public async Task<List<WindowCommit>> WindowAsync(
        Guid analysisJobId,
        int window,
        CancellationToken cancellation)
    {
        List<WindowCommit> newest = await (
            from snapshot in context.CommitRiskSnapshots.AsNoTracking()
            join commit in context.Commits.AsNoTracking() on snapshot.CommitId equals commit.Id
            join metric in context.CommitMetrics.AsNoTracking() on commit.Id equals metric.CommitId into metrics
            from metric in metrics.DefaultIfEmpty()
            where snapshot.AnalysisJobId == analysisJobId
            orderby commit.AuthorDateUtc descending, commit.Id descending
            select new WindowCommit(
                commit.Id,
                commit.Sha,
                commit.AuthorDateUtc,
                commit.MessageSubject,
                snapshot.RawModelScore,
                snapshot.RiskIndex,
                snapshot.DecisionAt05,
                snapshot.DecisionAtTrainThreshold,
                snapshot.TrainThreshold,
                snapshot.WarningCodes,
                commit.IsBugIntroducing,
                commit.IsBot,
                commit.LinesAdded,
                commit.LinesDeleted,
                commit.ChangedFiles,
                commit.ChangedCSharpFiles,
                metric != null && metric.IsFix))
            .Take(window)
            .ToListAsync(cancellation);

        // Gosterim kronolojik: eski soldan yeniye.
        newest.Reverse();

        return newest;
    }

    /// <summary>
    /// Penceredeki commit'lerin C# dosya satirlari.
    ///
    /// Commit kimlikleri alt sorgu olarak gidiyor: bin tane parametre gondermek hem sorgu
    /// planini bozar hem de PostgreSQL'in parametre sinirina yaklasir.
    /// </summary>
    public Task<List<WindowFile>> FilesAsync(
        Guid analysisJobId,
        int window,
        CancellationToken cancellation)
    {
        IQueryable<int> commitIds =
            (from snapshot in context.CommitRiskSnapshots.AsNoTracking()
             join commit in context.Commits.AsNoTracking() on snapshot.CommitId equals commit.Id
             where snapshot.AnalysisJobId == analysisJobId
             orderby commit.AuthorDateUtc descending, commit.Id descending
             select commit.Id)
            .Take(window);

        return context.CommitFiles
            .AsNoTracking()
            .Where(file => file.IsCSharp && commitIds.Contains(file.CommitId))
            .Select(file => new WindowFile(file.CommitId, file.Path, file.LinesAdded, file.LinesDeleted))
            .ToListAsync(cancellation);
    }

    /// <summary>
    /// Bir static-scan isinin bulgularini yola gore sayar.
    ///
    /// Sayim SQL'de yapiliyor; butun bulgulari cekip uygulamada saymak 687 satirlik bir
    /// iste de calisirdi ama sayinin nereden geldigi sorgudan okunamazdi.
    /// </summary>
    public async Task<Dictionary<string, int>> StaticFindingCountsAsync(
        Guid staticJobId,
        CancellationToken cancellation) =>
        await context.StaticAnalysisFindings
            .AsNoTracking()
            .Where(finding => finding.AnalysisJobId == staticJobId)
            .GroupBy(finding => finding.RelativePath)
            .Select(group => new { Path = group.Key, Count = group.Count() })
            .ToDictionaryAsync(row => row.Path, row => row.Count, StringComparer.Ordinal, cancellation);

    public Task<bool> HasResultsAsync(Guid analysisJobId, CancellationToken cancellation) =>
        context.CommitRiskSnapshots.AsNoTracking().AnyAsync(row => row.AnalysisJobId == analysisJobId, cancellation);

    public Task<AnalysisJobRow?> JobAsync(Guid analysisJobId, CancellationToken cancellation) =>
        context.AnalysisJobs.AsNoTracking().FirstOrDefaultAsync(job => job.Id == analysisJobId, cancellation);

    public Task<string?> ProfileAsync(Guid analysisJobId, CancellationToken cancellation) =>
        context.CommitRiskSnapshots
            .AsNoTracking()
            .Where(row => row.AnalysisJobId == analysisJobId)
            .Select(row => row.ModelProfile)
            .FirstOrDefaultAsync(cancellation);
}
