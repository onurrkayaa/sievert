using System.Text.Json;

namespace Sievert.Api;

/// <summary>Is baslatma istegi. Dosya sistemi yolu ALMIYOR; yol depo kaydindan geliyor.</summary>
/// <param name="Kind"><c>static-scan</c> ya da <c>risk-score-all</c>.</param>
public sealed record StartAnalysisRequest(string? Kind);

/// <summary>Bir sonraki adimin adresi.</summary>
public sealed record AnalysisLink(string Rel, string Href);

/// <summary>Bir arka plan isinin durumu.</summary>
public sealed record AnalysisJobResponse(
    Guid Id,
    int RepositoryId,
    string Kind,
    string Status,
    DateTimeOffset RequestedAtUtc,
    DateTimeOffset? StartedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    string CurrentPhase,
    int ProcessedItems,
    int? TotalItems,
    double? ProgressPercent,
    bool CancellationRequested,
    int ResultCount,
    bool IsResultComplete,
    string? ErrorCode,
    string? ErrorMessage,
    JsonElement? ResultSummary,
    IReadOnlyList<AnalysisLink> Links);

/// <summary>
/// Sonuc sayfasi. <paramref name="IsResultComplete"/> false ise satirlar kismi:
/// is iptal edildi ya da basarisiz oldu. Kismi sonuc tam sonuc gibi sunulmuyor.
/// </summary>
public sealed record AnalysisResultPage<T>(
    Guid AnalysisJobId,
    string Status,
    bool IsResultComplete,
    bool Partial,
    string? PartialWarning,
    int Page,
    int PageSize,
    int TotalCount,
    IReadOnlyList<T> Items);

/// <summary>Bir static-scan bulgusu.</summary>
public sealed record StaticFindingResponse(
    string RuleCode,
    string Severity,
    string RelativePath,
    int Line,
    int? Column,
    string? MemberName,
    string Message,
    string Rationale,
    bool IsTestCode);

/// <summary>Bir commit icin kaydedilmis risk degerlendirmesi.</summary>
public sealed record CommitRiskSnapshotResponse(
    string Sha,
    string ShortSha,
    DateTimeOffset AuthorDateUtc,
    string MessageSubject,
    double RawModelScore,
    double RiskIndex,
    bool DecisionAt05,
    bool DecisionAtTrainThreshold,
    double TrainThreshold,
    string ModelProfile,
    string ModelChecksum,
    bool IsCalibrated,
    IReadOnlyList<string> WarningCodes);

/// <summary>Kuyruk ve worker durumu; saglik cevabinin bir parcasi.</summary>
public sealed record AnalysisHealth(
    int QueueCapacity,
    int QueuedInChannel,
    int WorkerConcurrency,
    int QueuedJobs,
    int RunningJobs,
    DateTimeOffset? LastCompletedJobAtUtc,
    RecoveryHealth? LastRecovery);

/// <summary>Son kurtarmanin ozeti.</summary>
public sealed record RecoveryHealth(
    DateTimeOffset At,
    int Requeued,
    int Interrupted,
    int CanceledBeforeStart);
