namespace Sievert.Data.Entities;

/// <summary>Bir rapor dosyasinin durumu.</summary>
public enum ReportArtifactStatus
{
    /// <summary>Kayit acildi, dosya henuz yok.</summary>
    Pending,

    /// <summary>Dosya yazildi ve ozeti hesaplandi.</summary>
    Ready,

    Failed,

    /// <summary>Dosya kayip ya da ozeti tutmuyor. Indirilmiyor.</summary>
    Corrupted,
}

/// <summary>
/// Uretilmis bir rapor dosyasinin kaydi.
///
/// Dosyanin kendisi veritabaninda degil: bir PDF megabaytlarca yer tutuyor ve her
/// sorguda satirla birlikte tasinmasi anlamsiz. Burada duran sey **dosyanin kimligi**:
/// hangi girdilerden uretildi, ozeti ne, kac bayt, ne zaman dogrulandi.
///
/// <see cref="StorageKey"/> kullanicidan gelen hicbir metinden turetilmiyor. Dosya adi
/// ayri bir alan (<see cref="SafeFileName"/>) ve yalniz gosterimde kullaniliyor; depolama
/// yolu olarak kullanilsaydi istek atan kisi sunucunun dosya sisteminde gezinebilirdi.
/// </summary>
public sealed class ReportArtifactRow
{
    public Guid Id { get; set; }

    /// <summary>Raporu ureten arka plan isi. Her raporun tam bir isi var.</summary>
    public Guid AnalysisJobId { get; set; }

    public AnalysisJobRow? AnalysisJob { get; set; }

    public int RepositoryId { get; set; }

    public RepositoryRow? Repository { get; set; }

    /// <summary>Raporun dayandigi risk skorlama isi.</summary>
    public Guid RiskAnalysisJobId { get; set; }

    /// <summary>Rapora eklenen statik tarama isi; eklenmediyse null.</summary>
    public Guid? StaticAnalysisJobId { get; set; }

    public ReportArtifactStatus Status { get; set; }

    /// <summary>Bu turda yalniz <c>pdf</c>. Alan duruyor ki ikinci bir bicim eklenirse gorunsun.</summary>
    public string Format { get; set; } = "pdf";

    public string Culture { get; set; } = string.Empty;

    /// <summary>Kullaniciya gosterilen dosya adi. Yol ayraci ve kontrol karakteri icermez.</summary>
    public string SafeFileName { get; set; } = string.Empty;

    /// <summary>Depodaki dahili anahtar. Kullanici girdisinden turetilmiyor.</summary>
    public string StorageKey { get; set; } = string.Empty;

    public string ContentType { get; set; } = "application/pdf";

    public long? ByteLength { get; set; }

    /// <summary>Dosyanin SHA-256'si, kucuk harf onaltilik. Indirmeden once yeniden dogrulaniyor.</summary>
    public string? Sha256 { get; set; }

    /// <summary>Canonical girdi manifesti; raporun neyden uretildiginin tek kaynagi.</summary>
    public string ManifestJson { get; set; } = string.Empty;

    public string ManifestSha256 { get; set; } = string.Empty;

    public DateTimeOffset RequestedAtUtc { get; set; }

    /// <summary>Dosyanin yazildigi an. Manifest ozetine DAHIL DEGIL.</summary>
    public DateTimeOffset? GeneratedAtUtc { get; set; }

    /// <summary>Ozetin en son dogrulandigi an.</summary>
    public DateTimeOffset? VerifiedAtUtc { get; set; }

    public string? ErrorCode { get; set; }

    /// <summary>Kullaniciya gosterilebilir hata metni. Yol ve baglanti dizesi ICERMEZ.</summary>
    public string? ErrorMessage { get; set; }

    public string? IdempotencyKey { get; set; }

    /// <summary>
    /// Istek govdesinin ozeti. Ayni tekrar anahtarinin farkli bir govdeyle kullanilmasini
    /// yakalamak icin var; anahtar esleyip parmak izi eslemezse istek reddediliyor.
    /// </summary>
    public string RequestFingerprint { get; set; } = string.Empty;

    public bool IsPartial { get; set; }

    public int? PageCount { get; set; }

    public string SchemaVersion { get; set; } = string.Empty;

    public string GeneratorVersion { get; set; } = string.Empty;

    /// <summary>Iyimser es zamanlilik jetonu.</summary>
    public uint Version { get; set; }

    /// <summary>Durumun kablodaki yazilisi.</summary>
    public static string Name(ReportArtifactStatus status) => status switch
    {
        ReportArtifactStatus.Pending => "pending",
        ReportArtifactStatus.Ready => "ready",
        ReportArtifactStatus.Failed => "failed",
        ReportArtifactStatus.Corrupted => "corrupted",
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Bilinmeyen rapor durumu."),
    };
}
