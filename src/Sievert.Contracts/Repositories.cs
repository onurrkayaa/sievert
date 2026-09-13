namespace Sievert.Contracts;

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
    bool ModelProfileAvailable,

    /// <summary>
    /// Sabit demo veri kumesinden gelen bir depo mu. Yalniz kaynak sunumu icin;
    /// skorlari ve siralamayi degistirmiyor.
    /// </summary>
    bool IsDemoData = false);

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
