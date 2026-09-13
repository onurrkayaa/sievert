namespace Sievert.Api.Visualizations;

/// <summary>
/// Penceredeki tek bir commit: skoru ve gosterim icin gereken commit alanlari.
///
/// Snapshot, commit ve olcu tek sorguda birlestiriliyor; ayri sorgular yapip uygulamada
/// eslestirmek, pencere buyudukce N+1'e donen bir kalip olurdu.
/// </summary>
public sealed record WindowCommit(
    int CommitId,
    string Sha,
    DateTimeOffset AuthorDateUtc,
    string MessageSubject,
    double RawModelScore,
    double RiskIndex,
    bool DecisionAt05,
    bool DecisionAtTrainThreshold,
    double TrainThreshold,
    string WarningCodes,
    bool IsBugIntroducing,
    bool IsBot,
    int LinesAdded,
    int LinesDeleted,
    int FilesChanged,
    int CsFilesChanged,
    bool IsFix)
{
    public string ShortSha => Sha.Length >= 12 ? Sha[..12] : Sha;
}

/// <summary>Penceredeki bir commit'in bir dosyaya dokunusu.</summary>
public sealed record WindowFile(int CommitId, string Path, int LinesAdded, int LinesDeleted);
