namespace Sievert.Mining;

/// <summary>
/// Yazar bot mu. Bot commit'leri veriden CIKARILMIYOR, sadece bayraklaniyor; hangi
/// commit'in bot'a ait oldugu sonraki asamalarda modelin kendi karari olsun diye
/// veride kaliyor.
/// </summary>
public static class BotAuthor
{
    /// <summary>
    /// Ad ya da epostada <c>bot</c> ayri bir kelime olarak geciyorsa bot sayiliyor.
    /// Kelime siniri harf ve rakam disindaki her karakter; bu sayede <c>[bot]</c>,
    /// <c>-bot</c> ve <c>bot@</c> bicimlerinin ucu de tek kuralla yakalaniyor ve
    /// <c>Botwick</c> gibi gercek adlar yakalanmiyor. Eski hâli ve neden degistigi
    /// ADR 0011'de.
    /// </summary>
    public static bool Looks(string name, string email) => HasBotWord(name) || HasBotWord(email);

    private static bool HasBotWord(string value)
    {
        int start = -1;

        for (int i = 0; i <= value.Length; i++)
        {
            bool partOfWord = i < value.Length && char.IsLetterOrDigit(value[i]);

            if (partOfWord && start < 0)
            {
                start = i;
                continue;
            }

            if (partOfWord || start < 0)
            {
                continue;
            }

            if (value.AsSpan(start, i - start).Equals("bot", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            start = -1;
        }

        return false;
    }
}
