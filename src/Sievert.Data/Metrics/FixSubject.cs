namespace Sievert.Data.Metrics;

/// <summary>
/// Commit mesajinin BASLIGI bir duzeltme imasi tasiyor mu. Kelimeler tam kelime olarak
/// araniyor, alt-dizgi olarak degil: <c>prefix</c> icindeki <c>fix</c> eslesmemeli.
/// Bot tespitinde ayni hata bir kez yapildi (ADR 0011), tekrarlamasin.
/// </summary>
public static class FixSubject
{
    private static readonly HashSet<string> Words = new(StringComparer.OrdinalIgnoreCase)
    {
        "fix", "bug", "hata", "patch", "defect", "error", "crash", "issue", "resolve", "correct",
    };

    public static bool Looks(string subject)
    {
        int start = -1;

        for (int i = 0; i <= subject.Length; i++)
        {
            bool partOfWord = i < subject.Length && char.IsLetterOrDigit(subject[i]);

            if (partOfWord && start < 0)
            {
                start = i;
                continue;
            }

            if (partOfWord || start < 0)
            {
                continue;
            }

            if (Words.Contains(subject[start..i]))
            {
                return true;
            }

            start = -1;
        }

        return false;
    }
}
