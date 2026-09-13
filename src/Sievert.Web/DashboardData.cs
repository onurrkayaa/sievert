using Sievert.Contracts;

namespace Sievert.Web;

/// <summary>
/// Genel bakis sayfasinin tek parcada tasidigi veri.
///
/// Sozlesme tipi DEGIL: API'de "genel bakis" diye bir uc yok, sayfa birkac ucun sonucunu
/// kendisi birlestiriyor. Bu kayit yalnizca on-islemeden etkilesimli asamaya tasinirken
/// paketin butun halde kalmasi icin var.
/// </summary>
public sealed record DashboardData(
    HealthResponse Health,
    int RepositoryCount,
    int CommitCount,
    int ModelCount,
    IReadOnlyList<JobSummary> Recent,
    IReadOnlyDictionary<int, string> Names);

/// <summary>
/// Is tablolarinin tasidigi alanlar: <c>AnalysisJobResponse</c> eksi <c>resultSummary</c>.
///
/// <c>resultSummary</c> her is icin ayri bir JSON blogu ve bu tablolarin hicbirinde
/// gosterilmiyor. Gostermedigin bayti on-isleme durumunda tasimak, durumu bosuna
/// sisiriyor ve devrenin acilis mesaji sinirini zorluyor - olculdu, genel bakis paketi
/// 19,5 KB'dan 1,2 KB'a dustu.
/// </summary>
public sealed record JobSummary(
    Guid Id,
    int RepositoryId,
    string Kind,
    string Status,
    DateTimeOffset RequestedAtUtc,
    int ProcessedItems,
    int? TotalItems,
    int ResultCount,
    bool IsResultComplete,
    string CurrentPhase,
    DateTimeOffset? CompletedAtUtc)
{
    public static JobSummary From(AnalysisJobResponse job) => new(
        job.Id,
        job.RepositoryId,
        job.Kind,
        job.Status,
        job.RequestedAtUtc,
        job.ProcessedItems,
        job.TotalItems,
        job.ResultCount,
        job.IsResultComplete,
        job.CurrentPhase,
        job.CompletedAtUtc);
}
