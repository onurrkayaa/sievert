using System.Net;

namespace Sievert.Contracts;

/// <summary>
/// API'nin hangi adreslerde dinleyebilecegine karar verir.
///
/// Bu turda kimlik dogrulama **yok**. Kimlik dogrulamasi olmayan bir servisin butun ag
/// arayuzlerinde dinlemesi, veritabanindaki commit verisini ve model ciktilarini aga
/// acmak demek. O yuzden varsayilan loopback; disari acmak acik bir ayar istiyor ve o
/// ayar verildiginde de gunluge bir uyari yaziliyor.
///
/// Sozlesme projesinde duruyor cunku ayni kurali **iki** surec uyguluyor: API ve panel.
/// Ikisine ayri birer kopya yazmak, guvenlikle ilgili bir kontrolun iki yerde farkli
/// davranmasinin en kisa yolu olurdu.
/// </summary>
public static class RemoteAccessGuard
{
    public const string EnvironmentVariable = "SIEVERT_ALLOW_REMOTE";

    /// <summary>
    /// Acilis tanilama kodu. <see cref="ApiError"/> icinde DEGIL: orada duran kodlar
    /// <c>ProblemDetails</c> cevaplarinin kodlari ve bu kod hicbir istegin cevabinda
    /// cikmiyor - reddedilen sey istek degil, surecin kendisi. Kod yine de adlandirilmis
    /// duruyor ki acilis hatasi belgede ve testte tek bir adla anilabilsin.
    /// </summary>
    public const string DiagnosticCode = "REMOTE_ACCESS_NOT_ALLOWED";

    public const string RemoteWarning =
        "Servis loopback disinda dinliyor. Bu turda kimlik dogrulama yok, yani adrese "
        + "erisebilen herkes butun uclari kullanabilir. Deneysel ve guvensizdir.";

    /// <summary>
    /// Adresler kabul edilebilir mi. Sorun varsa kullaniciya gosterilecek metin doner,
    /// yoksa null.
    /// </summary>
    /// <param name="urls">Noktali virgulle ayrilmis dinleme adresleri; bos ise varsayilan.</param>
    /// <param name="allowRemote">Ortam degiskeninin degeri.</param>
    /// <param name="host">Hangi surec reddediyor; mesajin basinda geciyor.</param>
    public static string? Check(string? urls, string? allowRemote, string host = "API")
    {
        if (string.IsNullOrWhiteSpace(urls))
        {
            // Adres verilmemis; ASP.NET Core varsayilani localhost.
            return null;
        }

        List<string> remote = [.. urls
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(url => !IsLoopback(url))];

        if (remote.Count == 0 || IsAllowed(allowRemote))
        {
            return null;
        }

        return $"{host} yalnizca loopback adreslerinde dinleyebilir. Loopback olmayan adres(ler): "
            + $"{string.Join(", ", remote)}. {RemoteWarning} Yine de acmak istiyorsan "
            + $"{EnvironmentVariable}=true ver.";
    }

    /// <summary>Yalnizca <c>true</c> aciyor; <c>1</c> ya da <c>yes</c> sessizce kabul edilmiyor.</summary>
    public static bool IsAllowed(string? allowRemote) =>
        string.Equals(allowRemote?.Trim(), "true", StringComparison.OrdinalIgnoreCase);

    private static bool IsLoopback(string url)
    {
        // Joker adresler butun arayuzleri kapsiyor; Uri bunlari cozemedigi icin once eleniyor.
        if (url.Contains("//*", StringComparison.Ordinal) || url.Contains("//+", StringComparison.Ordinal))
        {
            return false;
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? parsed))
        {
            // Anlayamadigimiz bir adresi guvenli varsaymak yanlis tarafta hata yapmak olurdu.
            return false;
        }

        if (parsed.IsLoopback)
        {
            return true;
        }

        return IPAddress.TryParse(parsed.Host, out IPAddress? address) && IPAddress.IsLoopback(address);
    }
}
