namespace Sievert.Web;

/// <summary>
/// Isin durumunu tekrar tekrar sorma kurallari.
///
/// Kurallar burada, bilesende degil: bir sayfanin ne siklikta istek attigi gozle
/// kontrol edilemez, ama bir siniftaki sayi sinanabilir.
/// </summary>
public static class JobPolling
{
    /// <summary>
    /// Iki sorgu arasindaki sure.
    ///
    /// Bir saniye. Daha kisasi isin hizini degistirmiyor, yalniz API'ye daha cok istek
    /// atiyor; daha uzunu ise ilerleme cubugunu kekeme gosteriyor. SignalR ile anlik
    /// bildirim bu turda yok (ADR 0025).
    /// </summary>
    public static readonly TimeSpan Interval = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Hata sonrasi bekleme merdiveni.
    ///
    /// API cevap vermiyorsa saniyede bir denemeye devam etmek, dusmus bir servise yuk
    /// bindirmekten baska bir ise yaramaz. Merdiven sinirli: bes saniyede duruyor,
    /// sonsuz buyumuyor, cunku API geri geldiginde panel makul bir surede fark etmeli.
    /// </summary>
    public static readonly TimeSpan[] Backoff =
    [
        TimeSpan.FromSeconds(1),
        TimeSpan.FromSeconds(2),
        TimeSpan.FromSeconds(5),
    ];

    /// <summary>Terminal duruma gecen is bir daha sorulmuyor.</summary>
    public static bool IsTerminal(string? status) => status is "succeeded" or "failed" or "canceled";

    /// <summary>Kacinci basarisiz denemede ne kadar beklenecek.</summary>
    public static TimeSpan Wait(int consecutiveFailures) => consecutiveFailures <= 0
        ? Interval
        : Backoff[Math.Min(consecutiveFailures, Backoff.Length) - 1];
}
