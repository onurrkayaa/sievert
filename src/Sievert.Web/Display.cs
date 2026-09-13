using System.Globalization;

namespace Sievert.Web;

/// <summary>
/// Ekranda gorunen metinler ve bicimler.
///
/// Iki kural burada toplaniyor. Birincisi: sozlesme degeri asla degismiyor - Turkce
/// yazilan sey yalnizca etiket, <c>status</c> alani hala <c>succeeded</c>. Ikincisi:
/// tanimadigimiz bir deger geldiginde sayfa cokmuyor, "Bilinmiyor" yaziyor. API'ye yeni
/// bir durum eklenirse panelin eski surumu calismaya devam etmeli.
/// </summary>
public static class Display
{
    /// <summary>Ham model skorunun ondalik basamak sayisi. Yuzde isareti YOK.</summary>
    public const int ScoreDecimals = 4;

    public static string Status(string? status) => status switch
    {
        "queued" => "Kuyrukta",
        "running" => "Calisiyor",
        "succeeded" => "Tamamlandi",
        "failed" => "Basarisiz",
        "canceled" => "Iptal edildi",
        null or "" => "Bilinmiyor",
        _ => "Bilinmiyor",
    };

    /// <summary>Durumun gorsel sinifi. Renk tek basina bilgi tasimiyor; metin de var.</summary>
    public static string StatusTone(string? status) => status switch
    {
        "queued" => "tone-idle",
        "running" => "tone-active",
        "succeeded" => "tone-done",
        "failed" => "tone-danger",
        "canceled" => "tone-warning",
        _ => "tone-unknown",
    };

    public static string Kind(string? kind) => kind switch
    {
        "static-scan" => "Statik tarama",
        "risk-score-all" => "Tum commit'leri skorla",
        null or "" => "Bilinmiyor",
        _ => "Bilinmiyor",
    };

    public static string Severity(string? severity) => severity switch
    {
        "error" => "Hata",
        "warning" => "Uyari",
        "info" => "Bilgi",
        _ => "Bilinmiyor",
    };

    public static string SeverityTone(string? severity) => severity switch
    {
        "error" => "tone-danger",
        "warning" => "tone-warning",
        "info" => "tone-idle",
        _ => "tone-unknown",
    };

    /// <summary>
    /// Calisma agacinin durumu. <c>clean</c> disindaki her sey sonucun tam olmadigini
    /// anlatiyor.
    /// </summary>
    public static string TreeState(string? state) => state switch
    {
        "clean" => "Temiz calisma agaci",
        "dirty" => "Kaydedilmemis degisiklik vardi",
        "changed-during-analysis" => "Tarama sirasinda degisti",
        "unavailable" => "Okunamadi",
        null or "" => "Kaydedilmedi",
        _ => "Bilinmiyor",
    };

    /// <summary>
    /// Ham model skoru. Dort ondalik ve **yuzde isareti yok**: skor kalibre edilmis bir
    /// olasilik degil ve yuzde isareti onu olasilik gibi gosterirdi (risk sozlesmesi 1.0).
    /// </summary>
    public static string Score(double value) =>
        value.ToString("F" + ScoreDecimals.ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);

    /// <summary>Goreli endeks, tek ondalikli. Yine yuzde degil.</summary>
    public static string Index(double value) => value.ToString("F1", CultureInfo.InvariantCulture);

    public static string Decision(bool value) => value ? "Esigin ustunde" : "Esigin altinda";

    public static string Count(int value) => value.ToString("N0", new CultureInfo("tr-TR"));

    /// <summary>Dosya boyutu; bin degil 1024 tabaninda, cunku dosya boyutu boyle okunuyor.</summary>
    public static string Size(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:F1} KB".Replace('.', ','),
        _ => $"{bytes / (1024.0 * 1024):F1} MB".Replace('.', ','),
    };

    public static string Moment(DateTimeOffset? value) => value is DateTimeOffset moment
        ? moment.UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) + " UTC"
        : "-";

    public static string Day(DateTimeOffset? value) => value is DateTimeOffset moment
        ? moment.UtcDateTime.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
        : "-";

    /// <summary>Iki an arasindaki sure; biri yoksa tire.</summary>
    public static string Duration(DateTimeOffset? from, DateTimeOffset? to)
    {
        if (from is not DateTimeOffset start || to is not DateTimeOffset end)
        {
            return "-";
        }

        double milliseconds = (end - start).TotalMilliseconds;

        return milliseconds < 1000
            ? $"{Math.Round(milliseconds)} ms"
            : $"{(milliseconds / 1000).ToString("F1", CultureInfo.InvariantCulture)} sn";
    }

    /// <summary>Saniye, tek ondalikli. Kultura bagli ondalik ayraci sayfada karisiklik yaratiyordu.</summary>
    public static string Seconds(double value) =>
        value.ToString("F1", CultureInfo.InvariantCulture) + " sn";

    public static string Megabytes(long bytes) =>
        (bytes / 1024.0 / 1024.0).ToString("F1", CultureInfo.InvariantCulture) + " MB";

    /// <summary>Uyari kodunun okunabilir metni; tanimsiz kod sayfayi cokertmiyor.</summary>
    public static string Warning(string code)
    {
        try
        {
            return Contracts.RiskWarning.Text(code);
        }
        catch (ArgumentOutOfRangeException)
        {
            return "Bu uyari kodu panelin bildigi listede yok: " + code;
        }
    }
}
