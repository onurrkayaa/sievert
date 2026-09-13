namespace Sievert.Api;

/// <summary>Saglik cevabi. Baglanti dizesi ve dosya yolu ICERMEZ.</summary>
public sealed record HealthResponse(
    string Status,
    string Version,
    DatabaseHealth Database,
    string ModelMetadataVersion,
    IReadOnlyList<ModelHealth> Models,
    AnalysisHealth Analysis,
    ProcessHealth Process);

/// <summary>
/// Surecin bellek durumu.
///
/// Yol, kullanici adi ya da makine adi yok; yalnizca sayilar. Iki yerde ise yariyor:
/// panelin sistem sayfasi ve bellek olcumu. Olcum disaridan yalnizca isletim sisteminin
/// verdigi calisma kumesini gorebiliyor, yonetilen yigin ile toplama sayaclarini goremiyor.
/// </summary>
/// <param name="WorkingSetBytes">Isletim sisteminin gordugu calisma kumesi.</param>
/// <param name="ManagedHeapBytes"><c>GC.GetTotalMemory(false)</c>; toplama zorlanmadan.</param>
public sealed record ProcessHealth(
    long WorkingSetBytes,
    long ManagedHeapBytes,
    int Gen0Collections,
    int Gen1Collections,
    int Gen2Collections,
    double UptimeSeconds);

public sealed record DatabaseHealth(bool Reachable, bool MigrationsApplied, string? Detail);

public sealed record ModelHealth(string Code, string Status, string? Detail);

/// <summary>Model profilinin kamuya acik tanimi.</summary>
public sealed record ModelResponse(
    string Code,
    string DisplayName,
    string RepositoryIdentity,
    string Trainer,
    string MlPackage,
    int FeatureCount,
    string FeatureSchemaVersion,
    double TrainThreshold,
    bool IsCalibrated,
    string ModelCodeCommit,
    string ModelChecksum,
    IReadOnlyList<string> Limitations);

public sealed record PagedResponse<T>(int Page, int PageSize, int TotalCount, IReadOnlyList<T> Items);

public sealed record RepositoryListItem(
    int Id,
    string Name,
    string RepositoryIdentity,
    string IdentitySource,
    string? RemoteUrl,
    int CommitCount,
    DateTimeOffset? FirstCommitDate,
    DateTimeOffset? LastCommitDate,
    DateTimeOffset ScanDate,
    bool ModelProfileAvailable);

public sealed record RepositoryDetail(
    RepositoryListItem Repository,
    int CommitCount,
    int BugIntroducingCount,
    double BugIntroducingRate,
    int BotCount,
    int CommitsWithMetrics,
    string? ModelProfile,
    IReadOnlyList<string> CoverageNotes);

public sealed record CommitListItem(
    string Sha,
    string ShortSha,
    DateTimeOffset AuthorDateUtc,
    string MessageSubject,
    bool IsFix,
    bool IsBugIntroducing,
    bool IsBot,
    int LinesAdded,
    int LinesDeleted,
    int FilesChanged,
    int CsFilesChanged);

/// <summary>Bir ozniteligin model skoruna katkisi. Nedensel bir iddia degil.</summary>
public sealed record FeatureContribution(
    string FeatureName,
    double RawValue,
    double TransformedValue,
    double Coefficient,
    double Contribution,
    string Direction,
    int RankByAbsoluteContribution,
    string ExplanationKey,
    string Explanation,
    bool OutsideTrainRange,
    string? OutsideDirection);

/// <summary>Statik analiz bu turda CALISTIRILMIYOR; bos liste "bulgu yok" demek degil.</summary>
public sealed record StaticAnalysisSection(string Status, IReadOnlyList<string> Findings, bool IncludedInModelScore);

/// <summary>
/// Tek bir commit icin model degerlendirmesi.
///
/// Alan adi <c>probability</c> DEGIL: skor kalibre edilmis bir olasilik degil ve alan
/// adinin kendisi bir iddia olur (risk sozlesmesi 1.0).
/// </summary>
public sealed record CommitRiskAssessment(
    int RepositoryId,
    string RepositoryIdentity,
    string CommitSha,
    string ModelProfile,
    string ModelCodeCommit,
    string ModelChecksum,
    double RawModelScore,
    bool IsCalibrated,
    double RiskIndex,
    string RiskIndexBasis,
    bool DecisionAt05,
    bool DecisionAtTrainThreshold,
    double TrainThreshold,
    string TargetDefinition,
    IReadOnlyList<FeatureValue> FeatureValues,
    IReadOnlyList<FeatureContribution> PositiveContributions,
    IReadOnlyList<FeatureContribution> NegativeContributions,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> Limitations,
    StaticAnalysisSection StaticAnalysis,
    AuditSection Audit);

public sealed record FeatureValue(string FeatureName, double RawValue, double TransformedValue);

/// <summary>
/// Denetim alani: veritabaninda kayitli SZZ etiketi. Modelin dogrulugunun olcusu DEGIL;
/// etiketin kendisi dogrulanmadi (ADR 0022).
/// </summary>
public sealed record AuditSection(bool StoredSzzLabel, string? LabelSourceType);
