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
    /// <c>queued -> failed</c> listede var ama normal yol degil: worker her zaman once
    /// <c>running</c> yapiyor. Bu gecis yalnizca acik bir servis karari icin duruyor -
    /// ornegin yeniden baslatma sirasinda kuyruktaki bir isin hic calistirilamayacagi
    /// anlasilirsa.
    /// </summary>
    private static readonly (AnalysisJobStatus From, AnalysisJobStatus To)[] Valid =
    [
        (AnalysisJobStatus.Queued, AnalysisJobStatus.Running),
        (AnalysisJobStatus.Queued, AnalysisJobStatus.Canceled),
        (AnalysisJobStatus.Queued, AnalysisJobStatus.Failed),
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
