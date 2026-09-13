namespace Sievert.Contracts;

/// <summary>
/// Rapor parametrelerinin sinirlari.
///
/// Gorsellestirme sinirlariyla ayni degil ve bu bilerek boyle: ekranda kaydirilabilen
/// yuz satirlik bir tablo kagitta kaydirilamiyor, iki sayfa suruyor ve okunmuyor.
/// Varsayilanlar sonuc gorulmeden secildi.
/// </summary>
public static class ReportLimits
{
    public const int DefaultCommitWindow = 200;

    public const int MinimumCommitWindow = 50;

    public const int MaximumCommitWindow = 1000;

    public const int DefaultFileLimit = 50;

    public const int MinimumFileLimit = 10;

    public const int MaximumFileLimit = 100;

    public const int DefaultTimelineCount = 100;

    public const int MinimumTimelineCount = 20;

    public const int MaximumTimelineCount = 500;

    public const int DefaultTopCommitCount = 20;

    public const int MinimumTopCommitCount = 5;

    public const int MaximumTopCommitCount = 100;

    public const int DefaultFindingLimit = 50;

    /// <summary>Sifir gecerli: "bulgulari sayiyla ozetle, listeleme" demek.</summary>
    public const int MinimumFindingLimit = 0;

    public const int MaximumFindingLimit = 200;

    public const int MaximumTitleLength = 120;

    public const int MaximumNotesLength = 2000;

    /// <summary>Aciklama bolumunde katkisi yazilan commit sayisi.</summary>
    public const int ExplainedCommitCount = 5;

    /// <summary>Bir commit icin yazilan pozitif ve negatif katki sayisi.</summary>
    public const int ContributionsPerSide = 5;

    /// <summary>Uretilen PDF'in ust siniri. Asilirsa artefakt kabul edilmiyor.</summary>
    public const long MaximumArtifactBytes = 20L * 1024 * 1024;

    /// <summary>Panelin sundugu secenekler; API bunlarla sinirli degil.</summary>
    public static readonly IReadOnlyList<int> WindowChoices = [50, 100, 200, 500, 1000];

    public static readonly IReadOnlyList<int> FileLimitChoices = [10, 25, 50, 100];

    public static readonly IReadOnlyList<int> TimelineChoices = [20, 50, 100, 200, 500];

    public static readonly IReadOnlyList<int> TopCommitChoices = [5, 10, 20, 50, 100];

    public static readonly IReadOnlyList<int> FindingChoices = [0, 25, 50, 100, 200];

    public static bool IsCommitWindow(int value) =>
        value is >= MinimumCommitWindow and <= MaximumCommitWindow;

    public static bool IsFileLimit(int value) =>
        value is >= MinimumFileLimit and <= MaximumFileLimit;

    public static bool IsTimelineCount(int value) =>
        value is >= MinimumTimelineCount and <= MaximumTimelineCount;

    public static bool IsTopCommitCount(int value) =>
        value is >= MinimumTopCommitCount and <= MaximumTopCommitCount;

    public static bool IsFindingLimit(int value) =>
        value is >= MinimumFindingLimit and <= MaximumFindingLimit;
}
