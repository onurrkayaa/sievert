using Sievert.Contracts;

namespace Sievert.Api.Reports;

/// <summary>
/// Raporun ara modeli: PDF'e girecek her sayi burada, bicimlenmeden once.
///
/// Ayri bir model olmasinin sebebi PDF kodunu temiz tutmak degil, **dogrulanabilirlik**:
/// bagimsiz dogrulama araci bu modeli ham tablolardan yeniden uretip PDF'ten cikarilan
/// metinle karsilastirabiliyor. PDF'in icine dogrudan sorgu yazilsaydi karsilastirilacak
/// bir ara nokta olmazdi.
/// </summary>
public sealed record ReportModel(
    ReportCover Cover,
    ReportSummary Summary,
    IReadOnlyList<RiskTimelinePoint> Timeline,
    double ThresholdIndexAt05,
    double ThresholdIndexAtTrain,
    IReadOnlyList<FileActivityItem> Files,
    int FileCountBeforeLimit,
    string FileSort,
    IReadOnlyList<ReportCommitRow> TopCommits,
    ReportStaticSection Static,
    IReadOnlyList<ReportExplanation> Explanations,
    IReadOnlyList<string> Limitations,
    ReportProvenance Provenance);

/// <summary>Kapak bilgileri.</summary>
public sealed record ReportCover(
    string RepositoryDisplayName,
    string RepositoryIdentity,
    Guid ReportId,
    DateTimeOffset GeneratedAtUtc,
    Guid RiskJobId,
    Guid? StaticJobId,
    string ModelProfile,
    string ModelShortChecksum,
    bool IsCalibrated,
    bool IsPartial,
    string? Title,
    string? Notes);

/// <summary>Yonetici ozetindeki sayilar.</summary>
public sealed record ReportSummary(
    int CoveredCommitCount,
    DateTimeOffset? FirstCommitUtc,
    DateTimeOffset? LastCommitUtc,
    int CommitWindow,
    double MeanRiskIndex,
    double MedianRiskIndex,
    double MaximumRiskIndex,
    int DecisionAt05Count,
    int DecisionAtTrainCount,
    double TrainThreshold,
    int? StaticFindingCount,
    IReadOnlyList<ReportTouchedFile> MostTouchedFiles,
    IReadOnlyList<ReportCommitRow> HighestCommits,
    string RankingScope);

/// <summary>Ozetteki "en cok dokunulan dosya" satiri.</summary>
public sealed record ReportTouchedFile(string RelativePath, int TouchCount, double MeanRiskIndex);

/// <summary>Onceliklendirilmis listedeki bir commit.</summary>
public sealed record ReportCommitRow(
    string Sha,
    string ShortSha,
    DateTimeOffset AuthorDateUtc,
    string MessageSubject,
    double RiskIndex,
    double RawModelScore,
    bool DecisionAt05,
    bool DecisionAtTrainThreshold,
    int Churn,
    int CsFilesChanged,
    IReadOnlyList<string> WarningCodes);

/// <summary>Statik bulgu bolumu. <paramref name="Included"/> false ise "calistirilmadi".</summary>
public sealed record ReportStaticSection(
    bool Included,
    Guid? JobId,
    string? SourceHeadSha,
    string? SourceTreeState,
    bool SourceVerified,
    DateTimeOffset? CompletedAtUtc,
    int FindingCount,
    int SuppressedCount,
    int ExemptionCount,
    IReadOnlyList<ReportRuleCount> RuleCounts,
    IReadOnlyList<ReportSeverityCount> SeverityCounts,
    IReadOnlyList<StaticFindingResponse> Findings);

public sealed record ReportRuleCount(string RuleCode, int Count);

public sealed record ReportSeverityCount(string Severity, int Count);

/// <summary>Bir commit'in model aciklamasi: katkilar iki yone ayrilmis.</summary>
public sealed record ReportExplanation(
    string ShortSha,
    double RawModelScore,
    double RiskIndex,
    bool ExplanationVerified,
    IReadOnlyList<ReportContribution> Positive,
    IReadOnlyList<ReportContribution> Negative);

/// <summary>Tek bir ozniteligin katkisi. Nedensellik degil, skorun hangi yone itildigi.</summary>
public sealed record ReportContribution(
    string Feature,
    double Value,
    double TransformedValue,
    double Coefficient,
    double Contribution);

/// <summary>Kaynak ve butunluk bolumu.</summary>
public sealed record ReportProvenance(
    string ManifestSha256,
    string ModelChecksum,
    string ScoreReferenceChecksum,
    string ModelResultsChecksum,
    Guid RiskJobId,
    Guid? StaticJobId,
    string SchemaVersion,
    string GeneratorVersion);
