namespace Sievert.Contracts;

/// <summary>
/// Bir siralamanin neye dayandigi.
///
/// Iki deger arasindaki fark onemli: <c>complete-analysis</c> deponun butun skorlanmis
/// commit'lerine, <c>written-results-only</c> ise yalnizca yarida kalmis bir isin o ana
/// kadar yazdigi satirlara dayaniyor. Ikincisinde "en yuksek" demek, "kaydedilmis
/// sonuclarin en yuksegi" demek.
/// </summary>
public static class RankingScope
{
    public const string CompleteAnalysis = "complete-analysis";

    public const string WrittenResultsOnly = "written-results-only";
}

/// <summary>Gorsellestirme cevaplarinda donen uyari kodlari.</summary>
public static class VisualizationWarning
{
    /// <summary>Is basariyla bitmedi; ozetler yalniz kaydedilmis satirlara ait.</summary>
    public const string PartialAnalysisResult = "PARTIAL_ANALYSIS_RESULT";

    /// <summary>Istenen pencere mevcut satir sayisindan buyuk; hepsi kullanildi.</summary>
    public const string WindowLargerThanResults = "WINDOW_LARGER_THAN_RESULTS";

    /// <summary>Listede gosterilenden daha fazla dosya var; limit uygulandi.</summary>
    public const string FileLimitApplied = "FILE_LIMIT_APPLIED";

    /// <summary>Iki esik cizgisi ayni endekse dusuyor.</summary>
    public const string ThresholdsCoincide = "THRESHOLDS_COINCIDE";

    /// <summary>Butun commit'ler ayni tarihte; X ekseni sira numarasina dusuruldu.</summary>
    public const string TimelineUsesOrdinalAxis = "TIMELINE_USES_ORDINAL_AXIS";
}

/// <summary>Dosya etkinlik haritasinin siralama secenekleri.</summary>
public static class FileActivitySort
{
    public const string MeanRiskDescending = "mean-risk-desc";

    public const string MaxRiskDescending = "max-risk-desc";

    public const string LatestRiskDescending = "latest-risk-desc";

    public const string ChurnDescending = "churn-desc";

    public const string TouchCountDescending = "touch-count-desc";

    public const string PathAscending = "path-asc";

    public static readonly IReadOnlyList<string> All =
    [
        MeanRiskDescending,
        MaxRiskDescending,
        LatestRiskDescending,
        ChurnDescending,
        TouchCountDescending,
        PathAscending,
    ];

    public static bool IsKnown(string? value) => value is not null && All.Contains(value);
}

/// <summary>
/// Dosya etkinlik haritasi.
///
/// "Etkinlik" kelimesi bilincli: bu harita dosyalarin **kusurlu** oldugunu soylemiyor.
/// Renk, secilen commit penceresinde o dosyaya dokunan commit'lerin goreli risk
/// endekslerinin ortalamasi. Dosyanin kendisi skorlanmiyor; skorlanan sey commit.
/// </summary>
/// <param name="CommitWindow">Istenen pencere; kac commit geriye bakildi.</param>
/// <param name="ConsideredCommitCount">Penceredeki gercek commit sayisi.</param>
/// <param name="FileCountBeforeLimit">Limit uygulanmadan once kac dosya vardi.</param>
/// <param name="RankingScope">Siralamanin neye dayandigi.</param>
public sealed record FileActivityResponse(
    int RepositoryId,
    Guid AnalysisJobId,
    string? SourceHeadSha,
    int CommitWindow,
    int ConsideredCommitCount,
    int FileCountBeforeLimit,
    int ReturnedFileCount,
    int Limit,
    string Sort,
    bool IsResultComplete,
    bool IsPartial,
    string RankingScope,
    string ModelProfile,
    bool IsCalibrated,
    IReadOnlyList<FileActivityItem> Items,
    IReadOnlyList<string> Warnings);

/// <summary>
/// Haritadaki bir hucre.
///
/// <paramref name="StaticFindingCount"/> ayri duruyor ve
/// <paramref name="StaticFindingsIncludedInColor"/> her zaman <c>false</c>: statik
/// bulgular model skoruna girmiyor, dolayisiyla renge de girmiyor (risk sozlesmesi 1.0).
/// </summary>
public sealed record FileActivityItem(
    string RelativePath,
    int TouchCount,
    int TotalLinesAdded,
    int TotalLinesDeleted,
    int TotalChurn,
    double MeanRiskIndex,
    double MaxRiskIndex,
    double LatestRiskIndex,
    string LatestCommitSha,
    DateTimeOffset LatestCommitDateUtc,
    int? StaticFindingCount,
    Guid? StaticFindingSourceJobId,
    bool StaticFindingsIncludedInColor,
    IReadOnlyList<FileActivityCommit> Commits);

/// <summary>Bir dosyaya yapilan son dokunuslardan biri.</summary>
public sealed record FileActivityCommit(
    string Sha,
    string ShortSha,
    DateTimeOffset AuthorDateUtc,
    string MessageSubject,
    double RiskIndex,
    double RawModelScore,
    int LinesAdded,
    int LinesDeleted);

/// <summary>
/// Commit risk zaman cizelgesi.
///
/// Y ekseni yalniz <c>RiskIndex</c>: ham skor ile goreli endeks ayni eksene konursa iki
/// farkli sey tek bir egri gibi okunur.
/// </summary>
/// <param name="DecisionAt05RiskIndex">0,5 ham skorunun bu profildeki endeks karsiligi.</param>
/// <param name="DecisionAtTrainThresholdRiskIndex">Egitim esiginin endeks karsiligi.</param>
public sealed record RiskTimelineResponse(
    int RepositoryId,
    Guid AnalysisJobId,
    int RequestedCount,
    int ReturnedCount,
    bool IsResultComplete,
    bool IsPartial,
    string RankingScope,
    string ModelProfile,
    bool IsCalibrated,
    double DecisionAt05RiskIndex,
    double DecisionAtTrainThresholdRiskIndex,
    DateTimeOffset? StartDateUtc,
    DateTimeOffset? EndDateUtc,
    IReadOnlyList<RiskTimelinePoint> Points,
    IReadOnlyList<string> Warnings);

/// <summary>Zaman cizelgesindeki tek bir commit.</summary>
/// <param name="Ordinal">Kronolojik sira, 0'dan baslar. Ayni tarihli noktalari ayirir.</param>
public sealed record RiskTimelinePoint(
    int Ordinal,
    string Sha,
    string ShortSha,
    DateTimeOffset AuthorDateUtc,
    string MessageSubject,
    double RawModelScore,
    double RiskIndex,
    bool DecisionAt05,
    bool DecisionAtTrainThreshold,
    double TrainThreshold,
    bool IsFix,
    bool IsBugIntroducing,
    int LinesAdded,
    int LinesDeleted,
    int FilesChanged,
    int CsFilesChanged,
    bool IsBot,
    IReadOnlyList<string> WarningCodes);
