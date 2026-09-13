namespace Sievert.Contracts;

/// <summary>
/// PDF rapor istegi. Sinirlari <see cref="ReportLimits"/> soyluyor; hepsi sonuc
/// gorulmeden secildi ve <c>docs/urun/analiz-raporu-sozlesmesi.md</c> icinde yaziyor.
///
/// Dosya sistemi yolu ALMIYOR: raporun nereye yazilacagina yalniz uygulama karar
/// veriyor. Istekten gelen bir yol, istegi atan kisiye sunucunun dosya sistemini acardi.
/// </summary>
/// <param name="RiskAnalysisJobId">Raporun dayandigi <c>risk-score-all</c> isi.</param>
/// <param name="StaticAnalysisJobId">Istege bagli <c>static-scan</c> isi.</param>
/// <param name="IncludePartial">
/// Kismi bir isten rapor uretilmesine acik izin. Varsayilani <c>false</c>: kismi sonuc
/// kazayla tam sonuc gibi raporlanmasin.
/// </param>
public sealed record ReportRequest(
    Guid RiskAnalysisJobId,
    Guid? StaticAnalysisJobId = null,
    bool IncludePartial = false,
    int? CommitWindow = null,
    int? FileLimit = null,
    int? TimelineCount = null,
    int? TopCommitCount = null,
    int? FindingLimit = null,
    string? Culture = null,
    string? Title = null,
    string? Notes = null);

/// <summary>Rapor istegi kabul edildiginde donen ozet.</summary>
public sealed record ReportAcceptedResponse(
    Guid ReportId,
    Guid AnalysisJobId,
    string Status,
    string ManifestSha256,
    bool IsPartial,
    IReadOnlyList<AnalysisLink> Links);

/// <summary>
/// Bir rapor artefaktinin durumu.
///
/// Fiziksel dosya yolu **yok** ve olmayacak: depolama anahtari dahili bir kimlik,
/// disari cikan tek adres indirme ucunun kendisi.
/// </summary>
public sealed record ReportResponse(
    Guid Id,
    int RepositoryId,
    Guid AnalysisJobId,
    Guid RiskAnalysisJobId,
    Guid? StaticAnalysisJobId,
    string Status,
    string Format,
    string Culture,
    string FileName,
    bool IsPartial,
    string ManifestSha256,
    string? Sha256,
    long? ByteLength,
    int? PageCount,
    DateTimeOffset RequestedAtUtc,
    DateTimeOffset? GeneratedAtUtc,
    DateTimeOffset? VerifiedAtUtc,
    string? ErrorCode,
    string? ErrorMessage,
    string SchemaVersion,
    string GeneratorVersion,
    ReportParameters Parameters,
    IReadOnlyList<AnalysisLink> Links);

/// <summary>Raporu ureten parametreler; cevapta duruyor ki rapor tekrar uretilebilsin.</summary>
public sealed record ReportParameters(
    bool IncludePartial,
    int CommitWindow,
    int FileLimit,
    int TimelineCount,
    int TopCommitCount,
    int FindingLimit,
    string? Title,
    string? Notes);

/// <summary>Rapor listesindeki bir satir. Tam cevaptan daha kucuk.</summary>
public sealed record ReportSummaryResponse(
    Guid Id,
    int RepositoryId,
    Guid RiskAnalysisJobId,
    string Status,
    string Culture,
    string FileName,
    bool IsPartial,
    long? ByteLength,
    int? PageCount,
    DateTimeOffset RequestedAtUtc,
    DateTimeOffset? GeneratedAtUtc,
    string? ErrorCode);

/// <summary>Rapor artefaktinin durumlari.</summary>
public static class ReportStatus
{
    /// <summary>Kayit acildi, dosya henuz yok.</summary>
    public const string Pending = "pending";

    /// <summary>Dosya yazildi, ozeti hesaplandi, indirilebilir.</summary>
    public const string Ready = "ready";

    public const string Failed = "failed";

    /// <summary>Dosya kayip ya da ozeti tutmuyor. Indirilmez.</summary>
    public const string Corrupted = "corrupted";
}

/// <summary>Raporun desteklenen kulturleri. Ucuncu bir kultur eklemek ceviri isi.</summary>
public static class ReportCulture
{
    public const string Turkish = "tr-TR";

    public const string English = "en-US";

    public static readonly IReadOnlyList<string> Supported = [Turkish, English];

    public static bool IsSupported(string? culture) =>
        culture is not null && Supported.Contains(culture, StringComparer.OrdinalIgnoreCase);

    /// <summary>Girilen yazimi sozlesmedeki yazima cevirir; taninmazsa null.</summary>
    public static string? Normalize(string? culture) =>
        Supported.FirstOrDefault(known => string.Equals(known, culture, StringComparison.OrdinalIgnoreCase));
}
