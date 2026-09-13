using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sievert.Demo;

/// <summary>
/// Demo veri kumesi.
///
/// Uydurma degil: gercek Polly madenciliginin sabit bir alt kumesi. Secim kurali
/// **sonuc gorulmeden** sabitlendi ve <see cref="DemoSelection"/> icinde yaziyor.
///
/// Icinde yazar adi, e-posta, yerel yol ya da baglanti dizesi **yok**. Dahil edilenler
/// yalnizca panelin ve raporun gostermek zorunda oldugu alanlar.
/// </summary>
public sealed record DemoSeedFile(
    string SchemaVersion,
    DemoRepository Repository,
    IReadOnlyList<DemoCommit> Commits,
    IReadOnlyList<DemoFile> Files,
    IReadOnlyList<DemoMetric> Metrics,
    DemoRiskJob RiskJob,
    IReadOnlyList<DemoSnapshot> Snapshots,
    DemoStaticJob? StaticJob,
    IReadOnlyList<DemoFinding> Findings)
{
    /// <summary>Seed semasinin surumu; alan eklenirse artiyor ve importer kontrol ediyor.</summary>
    public const string CurrentSchemaVersion = "1.0";

    /// <summary>
    /// Seed dosyasinin yazim ayarlari.
    ///
    /// Girintili yaziliyor ki repoda okunabilsin; alan sirasi kayit tanimlarindan
    /// geliyor ve degismez, o yuzden iki uretim ayni baytlari veriyor.
    /// </summary>
    public static readonly JsonSerializerOptions Json = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        NewLine = "\n",
    };
}

/// <param name="Identity">Kaynak deponun kimligi; model profili bununla eslesiyor.</param>
public sealed record DemoRepository(
    string Identity,
    string IdentitySource,
    string Name,
    string? RemoteUrl,
    int TotalCommits,
    string? ScannedSha);

/// <summary>Bir commit. Yazar adi ve e-postasi bilerek yok.</summary>
public sealed record DemoCommit(
    string Sha,
    DateTimeOffset AuthorDateUtc,
    string MessageSubject,
    int ParentCount,
    int LinesAdded,
    int LinesDeleted,
    int ChangedFiles,
    int ChangedCSharpFiles,
    bool IsBugIntroducing,
    bool IsBot,
    string? LabelSource);

public sealed record DemoFile(
    string Sha,
    string Path,
    string? OldPath,
    string ChangeKind,
    int LinesAdded,
    int LinesDeleted,
    bool IsCSharp);

/// <summary>Modelin gordugu 15 oznitelik. Ham veriden yeniden uretilebilir ama kopyalaniyor
/// ki demo, madencilik calistirmadan acilsin.</summary>
public sealed record DemoMetric(
    string Sha,
    int LinesAdded,
    int LinesDeleted,
    int FilesChanged,
    int CsFilesChanged,
    double Entropy,
    int DirectoryCount,
    int SubsystemCount,
    int MaxFileAgeDays,
    int MinFileAgeDays,
    int PriorChanges,
    int PriorFixes,
    int DistinctAuthorsOnFiles,
    int AuthorCommitCount,
    int AuthorFileExperience,
    bool IsFix);

public sealed record DemoRiskJob(
    string Status,
    DateTimeOffset RequestedAtUtc,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset CompletedAtUtc,
    int ResultCount,
    bool IsResultComplete,
    string ModelProfile,
    string ModelChecksum);

public sealed record DemoSnapshot(
    string Sha,
    double RawModelScore,
    double RiskIndex,
    bool DecisionAt05,
    bool DecisionAtTrainThreshold,
    double TrainThreshold,
    string ModelProfile,
    string ModelChecksum,
    string WarningCodes);

public sealed record DemoStaticJob(
    string Status,
    DateTimeOffset RequestedAtUtc,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset CompletedAtUtc,
    int ResultCount,
    bool IsResultComplete,
    string? SourceHeadSha,
    string? SourceTreeState,
    string? ResultSummary);

public sealed record DemoFinding(
    string RuleCode,
    string Severity,
    string RelativePath,
    int Line,
    int? Column,
    string? MemberName,
    string Message,
    string Rationale,
    bool IsTestCode,
    bool IsSuppressed);

/// <summary>
/// Demo alt kumesinin secim kurali. **Sonuc gorulmeden** yazildi.
///
/// Kural: kaynak deponun tarih sirasinda **en yeni 200 commit'i**. Risk dagilimi
/// guzellesin diye hicbir commit elle secilmiyor, hicbiri atilmiyor; ne cikarsa o.
///
/// Neden en yeniler: zaman cizelgesinin bir sey gostermesi icin ardisik commit'ler
/// gerekiyor ve "son 200" hem tek cumleyle anlatilabiliyor hem de kaynak depo
/// buyudukce ayni kalabiliyor.
/// </summary>
public static class DemoSelection
{
    public const string RepositoryIdentity = "github.com/app-vnext/polly";

    public const int CommitCount = 200;

    public const string Rule = "kaynak deponun tarih sirasinda en yeni 200 commit'i; "
        + "risk dagilimina bakilarak secim yapilmadi";
}
