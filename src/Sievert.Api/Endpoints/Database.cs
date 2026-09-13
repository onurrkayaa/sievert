using Sievert.Contracts;
using Sievert.Data;

namespace Sievert.Api.Endpoints;

/// <summary>
/// Veritabani hazir degilse ayni cevabi uretmek icin tek yer.
///
/// Baglam endpoint imzasina parametre olarak YAZILMIYOR: baglanti dizesi yokken
/// <c>SievertContext</c> kurulurken patliyor ve istek endpoint'e hic gelmiyor. O zaman
/// da kullanici "servis hazir degil" yerine "beklenmeyen hata" goruyor. Once kontrol,
/// sonra cozum.
/// </summary>
internal static class Database
{
    public static IResult? NotReady(HttpContext context, DatabaseSettings settings) => settings.IsConfigured
        ? null
        : Problems.Unavailable(
            context,
            settings.Error ?? $"{Sievert.Data.ConnectionString.EnvironmentVariable} ayarlanmadi.",
            ApiError.DatabaseNotReady);

    public static SievertContext Open(HttpContext context) =>
        context.RequestServices.GetRequiredService<SievertContext>();
}
