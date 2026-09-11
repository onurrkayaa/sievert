namespace Sievert.Mining;

/// <summary>
/// Yazar bot mu. Bot commit'leri veriden CIKARILMIYOR, sadece bayraklaniyor; hangi
/// commit'in bot'a ait oldugu sonraki asamalarda modelin kendi karari olsun diye
/// veride kaliyor.
/// </summary>
public static class BotAuthor
{
    /// <summary>
    /// Ad ya da epostada gecerse bot sayilan parcalar. Duz "icinde geciyor mu"
    /// karsilastirmasi; gerekcesi ve bilinen bedeli ADR 0011'de.
    /// </summary>
    private static readonly string[] Markers = ["bot", "[bot]", "dependabot", "github-actions"];

    public static bool Looks(string name, string email) =>
        Markers.Any(marker =>
            name.Contains(marker, StringComparison.OrdinalIgnoreCase)
            || email.Contains(marker, StringComparison.OrdinalIgnoreCase));
}
