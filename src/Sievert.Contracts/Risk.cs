namespace Sievert.Contracts;

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
