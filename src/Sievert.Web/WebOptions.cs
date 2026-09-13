using Sievert.Contracts;

namespace Sievert.Web;

/// <summary>
/// Panelin ayarlari.
///
/// Gercek bir parola ya da dosya yolu burada yok ve olmayacak: panelin bildigi tek sey
/// API'nin adresi. Veritabanina hic baglanmiyor, o yuzden baglanti dizesine de ihtiyaci
/// yok.
/// </summary>
public sealed class WebOptions
{
    /// <summary>API adresinin okundugu ortam degiskeni.</summary>
    public const string ApiUrlVariable = "SIEVERT_API_URL";

    /// <summary>
    /// Gelistirme varsayilani. Loopback: kimlik dogrulama olmadigi icin API'nin de
    /// paneli de disari acilmamasi gerekiyor.
    /// </summary>
    public const string DefaultApiUrl = "http://127.0.0.1:5000";

    public Uri ApiBaseUrl { get; init; } = new(DefaultApiUrl);

    /// <summary>Panelin surumu; sistem sayfasinda ve altbilgide gorunuyor.</summary>
    public string Version { get; init; } = "0.1.0";

    /// <summary>API adresinin gosterilebilir hali: yalniz sema, host ve port.</summary>
    public string SafeApiAddress => $"{ApiBaseUrl.Scheme}://{ApiBaseUrl.Host}:{ApiBaseUrl.Port}";

    /// <summary>API loopback'te mi dinliyor; sistem sayfasi bunu soyluyor.</summary>
    public bool ApiIsLocal => ApiBaseUrl.IsLoopback;

    public static WebOptions Read(IConfiguration configuration)
    {
        string raw = Environment.GetEnvironmentVariable(ApiUrlVariable) is string fromEnvironment
            && !string.IsNullOrWhiteSpace(fromEnvironment)
                ? fromEnvironment
                : configuration["Sievert:ApiUrl"] ?? DefaultApiUrl;

        // Anlasilmayan bir adresi sessizce varsayilana cevirmiyoruz: kullanici verdigi
        // adrese baglandigini sanirdi.
        if (!Uri.TryCreate(raw, UriKind.Absolute, out Uri? parsed))
        {
            throw new InvalidOperationException(
                $"{ApiUrlVariable} bir adres olarak okunamadi: {raw}. Ornek: {DefaultApiUrl}");
        }

        return new WebOptions
        {
            ApiBaseUrl = parsed,
            Version = typeof(WebOptions).Assembly.GetName().Version?.ToString(3) ?? "0.1.0",
        };
    }

    /// <summary>
    /// Kimlik dogrulama olmadigi icin uzaktaki bir API'ye baglanmak uyari gerektiriyor.
    /// Sistem sayfasi bu metni gosteriyor.
    /// </summary>
    public string? RemoteWarning => ApiIsLocal ? null : RemoteAccessGuard.RemoteWarning;
}
