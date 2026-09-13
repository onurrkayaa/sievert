namespace Sievert.Data.Entities;

/// <summary>Arka planda kosan is turleri. Ikisi de gercek is yapiyor; demo isi yok.</summary>
public enum AnalysisJobKind
{
    /// <summary>Reponun calisma agacini SV kurallariyla tarar.</summary>
    StaticScan,

    /// <summary>Reponun metrikli butun commit'lerini kendi profiliyle skorlar.</summary>
    RiskScoreAll,

    /// <summary>Kaydedilmis bir risk sonucundan PDF raporu uretir.</summary>
    ReportGenerate,
}

/// <summary>
/// Bir isin durumu. <see cref="Queued"/> ve <see cref="Running"/> aktif sayiliyor;
/// geri kalan uc durum terminal ve geri donusu yok.
/// </summary>
public enum AnalysisJobStatus
{
    Queued,
    Running,
    Succeeded,
    Failed,
    Canceled,
}

/// <summary>
/// Arka plan isi. Kuyruk degil, **kayit**: kuyruk yalnizca uyandirma mekanizmasi, isin
/// gercekten var olup olmadiginin tek kaynagi bu tablo (ADR 0024).
///
/// Sonuclar bu ise bagli ayri tablolarda duruyor, ana <c>Commits</c> tablosunun uzerine
/// yazilmiyor. Sebep: ayni repo birden fazla kez skorlanabiliyor, is yarida kalabiliyor
/// ve ileride farkli model surumleri cikabilir; uzerine yazmak hangi sonucun hangi
/// kosuldan geldigini kaybetmek olurdu.
/// </summary>
public sealed class AnalysisJobRow
{
    public Guid Id { get; set; }

    public int RepositoryId { get; set; }

    public RepositoryRow? Repository { get; set; }

    public AnalysisJobKind Kind { get; set; }

    public AnalysisJobStatus Status { get; set; }

    public DateTimeOffset RequestedAtUtc { get; set; }

    public DateTimeOffset? StartedAtUtc { get; set; }

    public DateTimeOffset? CompletedAtUtc { get; set; }

    /// <summary>
    /// Iptal isteginin geldigi an. Iptalin tek kaynagi bu sutun; surec icindeki iptal
    /// jetonu yalnizca hizlandirici, cunku isi kosan surec istegi alan surecten farkli
    /// olabilir.
    /// </summary>
    public DateTimeOffset? CancellationRequestedAtUtc { get; set; }

    /// <summary>Isin o an hangi asamada oldugu; ilerleme cubugunun yanindaki metin.</summary>
    public string CurrentPhase { get; set; } = string.Empty;

    public int ProcessedItems { get; set; }

    /// <summary>Toplam oge sayisi. Kesfedilmeden once null.</summary>
    public int? TotalItems { get; set; }

    public double? ProgressPercent { get; set; }

    public DateTimeOffset? HeartbeatAtUtc { get; set; }

    public string? ErrorCode { get; set; }

    /// <summary>Kullaniciya gosterilebilir hata metni. Yol, baglanti dizesi ve yigin izi ICERMEZ.</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>Istemcinin verdigi tekrar anahtari. Tam hali gunluge yazilmaz.</summary>
    public string? IdempotencyKey { get; set; }

    /// <summary>
    /// Aktifken <c>"{repositoryId}:{kind}"</c>, terminal durumda null.
    ///
    /// Benzersiz indeksli. Boylece ayni repo ve tur icin ayni anda iki is acilmasi
    /// yarisi uygulama koduna birakilmiyor; iki es zamanli istek yarisirsa veritabani
    /// ikincisini reddediyor.
    /// </summary>
    public string? ActiveDeduplicationKey { get; set; }

    /// <summary>Kaydedilen sonuc satiri sayisi.</summary>
    public int ResultCount { get; set; }

    /// <summary>Sonuc tam mi. Yalnizca <see cref="AnalysisJobStatus.Succeeded"/> durumunda true.</summary>
    public bool IsResultComplete { get; set; }

    /// <summary>
    /// Is turune ozel sayilar, JSON nesnesi olarak. Static scan icin susturma, muafiyet,
    /// elenen dosya ve atlanan klasor sayilari; risk skorlamasi icin kullanilan profil.
    ///
    /// Her tur icin ayri sutun acmak yerine tek bir alan: sutunlar acilsaydi tablonun
    /// yarisi her satirda bos kalirdi ve ucuncu bir is turu eklemek semayi degistirirdi.
    /// </summary>
    public string? ResultSummary { get; set; }

    /// <summary>Isi kosan surecin kimligi. Hangi surecin yarida biraktigi gorulsun diye.</summary>
    public string? WorkerInstanceId { get; set; }

    /// <summary>
    /// Taramanin basladigi andaki HEAD commit'i. Yalnizca <c>static-scan</c> doldurur.
    ///
    /// Risk skorlamasi calisma agacini hic okumuyor - onun girdisi veritabanindaki
    /// commit'ler ve model dosyasi, ikisinin de ozeti sonuc satirlarinda duruyor.
    /// </summary>
    public string? SourceHeadSha { get; set; }

    /// <summary>HEAD'in ilk 12 karakteri; gosterimde kullanilan kisa hali.</summary>
    public string? SourceHeadShortSha { get; set; }

    /// <summary>
    /// Calisma agacinin durumu: <c>clean</c>, <c>dirty</c>,
    /// <c>changed-during-analysis</c> ya da <c>unavailable</c>.
    /// </summary>
    public string? SourceTreeState { get; set; }

    /// <summary>Durumun ilk okundugu an; tarama baslamadan once.</summary>
    public DateTimeOffset? SourceStateCheckedAtUtc { get; set; }

    /// <summary>Durumun yeniden okundugu an; tarama bittikten sonra.</summary>
    public DateTimeOffset? SourceStateVerifiedAtUtc { get; set; }

    /// <summary>Taranan deponun uzak adresi. Tam dosya yolu DEGIL; o cevaba girmiyor.</summary>
    public string? SourceRepositoryIdentity { get; set; }

    /// <summary>Tarama sirasinda HEAD ya da calisma agaci degisti mi.</summary>
    public bool SourceCommitChangedDuringAnalysis { get; set; }

    /// <summary>
    /// Ilk okumada bulunan kaydedilmemis degisiklik sayisi. Dosya adlari saklanmiyor:
    /// adlar cevaba sizarsa sunucudaki calisma agacinin icerigi disari cikardi.
    /// </summary>
    public int? SourceDirtyFileCount { get; set; }

    /// <summary>Iyimser es zamanlilik jetonu; iki worker ayni isi alamasin diye.</summary>
    public uint Version { get; set; }

    /// <summary>Aktif bir is icin tekillik anahtari.</summary>
    public static string DeduplicationKeyFor(int repositoryId, AnalysisJobKind kind) =>
        $"{repositoryId}:{Name(kind)}";

    /// <summary>Tur adinin kablo uzerindeki yazilisi.</summary>
    public static string Name(AnalysisJobKind kind) => kind switch
    {
        AnalysisJobKind.StaticScan => "static-scan",
        AnalysisJobKind.RiskScoreAll => "risk-score-all",
        AnalysisJobKind.ReportGenerate => "report-generate",
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Bilinmeyen is turu."),
    };

    /// <summary>
    /// Genel is baslatma ucundan **baslatilabilen** turler. Taninmayan deger null.
    ///
    /// <see cref="AnalysisJobKind.ReportGenerate"/> bilerek yok: rapor isinin kendi
    /// zorunlu parametreleri var ve onlarsiz baslatilirsa uretecek bir sey bulamaz.
    /// Rapor yalniz kendi ucundan baslatiliyor.
    /// </summary>
    public static AnalysisJobKind? Parse(string? kind) => kind switch
    {
        "static-scan" => AnalysisJobKind.StaticScan,
        "risk-score-all" => AnalysisJobKind.RiskScoreAll,
        _ => null,
    };

    /// <summary>
    /// Listeleme suzgecinde kullanilan tur cozumu: baslatilamayan turleri de taniyor.
    /// Rapor isleri de listede gorunmeli, yoksa kullanicinin gecmisi eksik gorunur.
    /// </summary>
    public static AnalysisJobKind? ParseFilter(string? kind) => kind switch
    {
        "report-generate" => AnalysisJobKind.ReportGenerate,
        _ => Parse(kind),
    };

    /// <summary>Durumun kablodaki yazilisi.</summary>
    public static string Name(AnalysisJobStatus status) => status switch
    {
        AnalysisJobStatus.Queued => "queued",
        AnalysisJobStatus.Running => "running",
        AnalysisJobStatus.Succeeded => "succeeded",
        AnalysisJobStatus.Failed => "failed",
        AnalysisJobStatus.Canceled => "canceled",
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Bilinmeyen durum."),
    };

    /// <summary>Aktif durumlar: kuyrukta ya da kosuyor.</summary>
    public static bool IsActive(AnalysisJobStatus status) =>
        status is AnalysisJobStatus.Queued or AnalysisJobStatus.Running;
}
