using Sievert.Core.Mining;

namespace Sievert.Mining;

/// <summary>
/// Commit mesajindaki <c>Co-Authored-By: Ad &lt;eposta&gt;</c> satirlarini ayristirir.
/// Mesajin kendisine dokunulmuyor; satirlar mesajda kaliyor, ayrica listeye cikiyor.
/// </summary>
public static class CoAuthorParser
{
    private const string Prefix = "Co-Authored-By:";

    public static IReadOnlyList<CoAuthor> Parse(string message)
    {
        List<CoAuthor> found = [];

        foreach (string line in message.Split('\n'))
        {
            string trimmed = line.Trim();

            if (!trimmed.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (ReadPerson(trimmed[Prefix.Length..].Trim()) is CoAuthor person)
            {
                found.Add(person);
            }
        }

        return found;
    }

    /// <summary>
    /// <c>Ad &lt;eposta&gt;</c> parcasini boler. Koseli parantez yoksa satiri atliyoruz:
    /// epostasi olmayan bir ortak yazar kaydi kimlik olarak kullanilamaz ve bu projede
    /// kimlik epostayla belirleniyor.
    /// </summary>
    private static CoAuthor? ReadPerson(string value)
    {
        int open = value.LastIndexOf('<');
        int close = value.LastIndexOf('>');

        if (open < 0 || close < open)
        {
            return null;
        }

        string name = value[..open].Trim();
        string email = value[(open + 1)..close].Trim().ToLowerInvariant();

        return email.Length == 0 ? null : new CoAuthor(name, email);
    }
}
