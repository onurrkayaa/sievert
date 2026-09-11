namespace Sievert.Analysis;

/// <summary>
/// Tarama disinda birakilacak yollari eslestiren basit bir glob. Harici paket kullanmadim,
/// ArgumentParser gibi bu da elle yazildi; ihtiyacim olan uc kalip var: <c>samples/**</c>,
/// <c>**/*.Designer.cs</c>, <c>**/obj/**</c>.
///
/// Desteklenenler: <c>*</c> bir yol parcasi icinde herhangi bir metni, <c>**</c> sifir ya da
/// daha fazla yol parcasini eslestirir. <c>?</c> yok, karakter kumesi yok. Eslesme buyuk
/// kucuk harf ayirmiyor, cunku dosya sistemlerinin cogu da ayirmiyor ve atlanacak klasor
/// listesi de ayni sekilde davraniyor.
/// </summary>
public sealed class GlobPattern
{
    private readonly string[] _segments;

    private GlobPattern(string text, string[] segments)
    {
        Text = text;
        _segments = segments;
    }

    /// <summary>Kalibin kullanicinin yazdigi hali. Hata mesajlarinda ve testlerde isimize yariyor.</summary>
    public string Text { get; }

    /// <summary>
    /// Kalibi okur, gecersizse null doner. Gecersiz saydiklarim: bos kalip, mutlak yol
    /// (goreli yollarla hicbir zaman eslesmez), bos yol parcasi (<c>a//b</c>) ve <c>**</c>'in
    /// tek basina bir parca olmadigi kaliplar (<c>a**b</c>, <c>***</c>). Sonuncusunu bilerek
    /// hata sayiyorum: sessizce baska bir anlama gelseydi kullanici yanlis kalipla dosya
    /// atladigini fark etmezdi.
    /// </summary>
    public static GlobPattern? TryParse(string pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern) || Path.IsPathRooted(pattern))
        {
            return null;
        }

        string[] segments = Normalize(pattern).Split('/');

        foreach (string segment in segments)
        {
            if (segment.Length == 0)
            {
                return null;
            }

            if (segment.Contains("**", StringComparison.Ordinal) && segment != "**")
            {
                return null;
            }
        }

        return new GlobPattern(pattern, segments);
    }

    /// <summary>Verilen goreli yol bu kalipla esleyiyor mu.</summary>
    public bool Matches(string relativePath) =>
        MatchSegments(0, Normalize(relativePath).Split('/'), 0);

    /// <summary>Yollar ters bolu ile de gelebiliyor; her sey duz boluye cevriliyor.</summary>
    private static string Normalize(string value) => value.Replace('\\', '/');

    private bool MatchSegments(int patternIndex, string[] path, int pathIndex)
    {
        while (patternIndex < _segments.Length)
        {
            if (_segments[patternIndex] == "**")
            {
                // ** sifir parca da eslestirebilir, o yuzden kalan her baslangic denenmeli.
                for (int skipTo = pathIndex; skipTo <= path.Length; skipTo++)
                {
                    if (MatchSegments(patternIndex + 1, path, skipTo))
                    {
                        return true;
                    }
                }

                return false;
            }

            if (pathIndex >= path.Length || !SegmentMatches(_segments[patternIndex], path[pathIndex]))
            {
                return false;
            }

            patternIndex++;
            pathIndex++;
        }

        return pathIndex == path.Length;
    }

    /// <summary>Tek bir yol parcasini <c>*</c> joker karakteriyle eslestirir.</summary>
    private static bool SegmentMatches(string pattern, string text)
    {
        int patternIndex = 0;
        int textIndex = 0;
        int lastStar = -1;
        int textAtStar = 0;

        while (textIndex < text.Length)
        {
            if (patternIndex < pattern.Length && pattern[patternIndex] == '*')
            {
                lastStar = patternIndex++;
                textAtStar = textIndex;
            }
            else if (patternIndex < pattern.Length && CharsMatch(pattern[patternIndex], text[textIndex]))
            {
                patternIndex++;
                textIndex++;
            }
            else if (lastStar >= 0)
            {
                // Yildiz bir karakter daha yutsun ve bastan denensin.
                patternIndex = lastStar + 1;
                textIndex = ++textAtStar;
            }
            else
            {
                return false;
            }
        }

        while (patternIndex < pattern.Length && pattern[patternIndex] == '*')
        {
            patternIndex++;
        }

        return patternIndex == pattern.Length;
    }

    private static bool CharsMatch(char pattern, char text) =>
        char.ToLowerInvariant(pattern) == char.ToLowerInvariant(text);
}
