namespace Sievert.Mining;

/// <summary>
/// Uzak adresi karsilastirilabilir bir kimlige cevirir. Ayni repo farkli bicimlerde
/// yazilabiliyor: <c>https://github.com/App-vNext/Polly.git</c>,
/// <c>git@github.com:App-vNext/Polly</c>, sonunda egik cizgiyle ya da farkli harflerle.
/// Bunlarin hepsi ayni depo ve ayni kimlige inmeli.
/// </summary>
public static class RemoteIdentity
{
    /// <summary>
    /// Atilanlar sirasiyla: protokol, <c>kullanici@</c>, sondaki <c>.git</c>, sondaki
    /// egik cizgi. Sonra tamami kucuk harfe cevriliyor ve SCP bicimindeki iki nokta
    /// (<c>github.com:App-vNext</c>) egik cizgiye donuyor.
    /// </summary>
    public static string? Normalize(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return null;
        }

        string value = url.Trim();

        int scheme = value.IndexOf("://", StringComparison.Ordinal);

        if (scheme >= 0)
        {
            value = value[(scheme + 3)..];
        }

        int at = value.IndexOf('@');

        if (at >= 0)
        {
            value = value[(at + 1)..];
        }

        // git@github.com:App-vNext/Polly bicimi: host ile yol arasindaki iki nokta.
        value = value.Replace(':', '/');

        value = value.TrimEnd('/');

        if (value.EndsWith(".git", StringComparison.OrdinalIgnoreCase))
        {
            value = value[..^4];
        }

        value = value.TrimEnd('/');

        return value.Length == 0 ? null : value.ToLowerInvariant();
    }
}
