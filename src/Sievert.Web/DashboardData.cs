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
    IReadOnlyList<AnalysisJobResponse> Recent,
    IReadOnlyDictionary<int, string> Names);
