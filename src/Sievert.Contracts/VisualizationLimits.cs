namespace Sievert.Contracts;

/// <summary>
/// Gorsellestirme uclarinin sinirlari.
///
/// Hicbiri olcum sonucuna bakilarak secilmedi; hepsi sorgu yazilmadan once sabitlendi ve
/// gerekceleri ADR 0026'da.
///
/// Sozlesme projesinde duruyor cunku iki taraf da ayni sinirlari bilmek zorunda: panel,
/// API'nin reddedecegi bir secenegi listeye koymamali. Adim 4'te ayni sebeple sayfa
/// boyutu ust siniri da istemci tarafina tasinmisti.
/// </summary>
public static class VisualizationLimits
{
    /// <summary>Varsayilan commit penceresi.</summary>
    public const int DefaultCommitWindow = 200;

    public const int MinimumCommitWindow = 10;

    public const int MaximumCommitWindow = 1000;

    /// <summary>Haritada gosterilen en fazla dosya.</summary>
    public const int DefaultFileLimit = 100;

    public const int MinimumFileLimit = 10;

    public const int MaximumFileLimit = 200;

    /// <summary>Zaman cizelgesindeki varsayilan nokta sayisi.</summary>
    public const int DefaultTimelineCount = 100;

    public const int MinimumTimelineCount = 10;

    public const int MaximumTimelineCount = 500;

    /// <summary>Bir hucrenin tasidigi son dokunus sayisi.</summary>
    public const int RecentCommitsPerFile = 5;

    /// <summary>Panelin sundugu pencere secenekleri; API bunlarla sinirli degil.</summary>
    public static readonly IReadOnlyList<int> WindowChoices = [50, 100, 200, 500, 1000];

    public static readonly IReadOnlyList<int> TimelineChoices = [50, 100, 200, 500];

    public static readonly IReadOnlyList<int> FileLimitChoices = [50, 100, 200];

    public static bool IsWindow(int value) => value is >= MinimumCommitWindow and <= MaximumCommitWindow;

    public static bool IsFileLimit(int value) => value is >= MinimumFileLimit and <= MaximumFileLimit;

    public static bool IsTimelineCount(int value) => value is >= MinimumTimelineCount and <= MaximumTimelineCount;

    public static bool IsSort(string? value) => FileActivitySort.IsKnown(value);
}
