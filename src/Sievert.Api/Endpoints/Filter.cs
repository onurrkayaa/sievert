namespace Sievert.Api.Endpoints;

/// <summary>
/// Sorgu dizesindeki ucuncu durumlu bayraklar.
///
/// Uc durum var ve ucu de ayri: parametre hic verilmemis (suzme yok), <c>true</c>,
/// <c>false</c>. Tanimadigimiz bir degeri sessizce "verilmemis" saymak, kullanicinin
/// suzdugunu sanip suzulmemis bir liste gormesi demek olurdu.
/// </summary>
internal static class Filter
{
    public static bool TryRead(HttpContext context, string name, out bool? value, out string? error)
    {
        value = null;
        error = null;

        if (context.Request.Query[name].FirstOrDefault() is not string raw || raw.Length == 0)
        {
            return true;
        }

        if (!bool.TryParse(raw, out bool parsed))
        {
            error = $"{name} true ya da false olmali.";

            return false;
        }

        value = parsed;

        return true;
    }
}
