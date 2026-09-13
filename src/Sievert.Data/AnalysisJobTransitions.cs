using Sievert.Data.Entities;

namespace Sievert.Data;

/// <summary>
/// Bir isin hangi durumdan hangi duruma gecebilecegi. Kural burada, tek yerde ve
/// veritabanina bagli degil; boylece sinanabiliyor ve iki ayri kodda metin olarak
/// kopyalanmiyor.
/// </summary>
public static class AnalysisJobTransitions
{
    /// <summary>
    /// Gecerli gecisler.
    ///
    /// <c>queued -> failed</c> Adim 3'te listede duruyordu ama hicbir yerden
    /// cagrilmiyordu. Cagiran yeri olmayan bir gecisi acik tutmak, ileride birinin onu
    /// "demek ki serbest" diye kullanmasina davetiye: kuyruktaki bir isi basarisiz
    /// yapmak, hic denenmemis bir isi denenmis gostermek olurdu. Kuyruga yazarken ya da
    /// altyapida hata olursa is veritabaninda <c>queued</c> kaliyor ve bir sonraki
    /// kurtarma onu yeniden aliyor.
    ///
    /// Kurtarmanin <c>running -> failed</c> gecisi bu listede: o gercekten baslamis bir
    /// isin basarisiz bitmesi.
    /// </summary>
    private static readonly (AnalysisJobStatus From, AnalysisJobStatus To)[] Valid =
    [
        (AnalysisJobStatus.Queued, AnalysisJobStatus.Running),
        (AnalysisJobStatus.Queued, AnalysisJobStatus.Canceled),
        (AnalysisJobStatus.Running, AnalysisJobStatus.Succeeded),
        (AnalysisJobStatus.Running, AnalysisJobStatus.Failed),
        (AnalysisJobStatus.Running, AnalysisJobStatus.Canceled),
    ];

    public static bool IsAllowed(AnalysisJobStatus from, AnalysisJobStatus to) =>
        Array.Exists(Valid, pair => pair.From == from && pair.To == to);

    /// <summary>Terminal durumdan cikis yok; ayni duruma yeniden gecmek de yok.</summary>
    public static bool IsTerminal(AnalysisJobStatus status) => !AnalysisJobRow.IsActive(status);

    public static bool IsCancelable(AnalysisJobStatus status) => AnalysisJobRow.IsActive(status);

    /// <summary>
    /// Sonuc tam mi. Yalnizca basariyla bitmis bir is tam sonuc tasiyor; iptal edilmis ya
    /// da basarisiz olmus bir isin satirlari durabilir ama kismidir.
    /// </summary>
    public static bool IsResultComplete(AnalysisJobStatus status) => status == AnalysisJobStatus.Succeeded;
}
