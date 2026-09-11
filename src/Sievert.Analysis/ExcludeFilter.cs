namespace Sievert.Analysis;

/// <summary>Eleme sonucu.</summary>
/// <param name="Files">Geriye kalan dosyalar.</param>
/// <param name="UnmatchedPatterns">
/// Hicbir dosyayla eslesmeyen kaliplar. Sessiz eslesmeme, yanlis yazilmis bir kalibin
/// tek belirtisi; bu yuzden ayrica donuyor.
/// </param>
public sealed record ExcludeResult(IReadOnlyList<string> Files, IReadOnlyList<GlobPattern> UnmatchedPatterns);

/// <summary>Bulunan dosyalardan --exclude kaliplarina uyanlari cikarir.</summary>
public static class ExcludeFilter
{
    /// <summary>
    /// Kaliplar tarama kokune gore goreli yola uygulaniyor, mutlak yola degil. Boylece ayni
    /// kalip her makinede ayni sonucu veriyor; mutlak yola bakilsaydi kullanicinin ev
    /// klasorunun adi eslesmeyi degistirebilirdi.
    ///
    /// Kac dosyanin elendigini ayrica dondurmuyorum, cagiran taraf listelerin uzunluk
    /// farkindan buluyor. Boylece ayni dosyayi birden fazla kalip tutsa da bir kez sayiliyor.
    /// </summary>
    public static ExcludeResult Apply(
        IReadOnlyList<string> filePaths,
        string scanRoot,
        IReadOnlyList<GlobPattern> patterns)
    {
        if (patterns.Count == 0)
        {
            return new ExcludeResult(filePaths, []);
        }

        HashSet<GlobPattern> used = [];
        List<string> kept = [];

        foreach (string filePath in filePaths)
        {
            string relativePath = ScanRoot.RelativePath(filePath, scanRoot);
            bool excluded = false;

            // Her kalibi tek tek deniyoruz, ilk eslesmede durmuyoruz: hangi kalibin
            // gercekten bir ise yaradigini bilmek istiyoruz.
            foreach (GlobPattern pattern in patterns)
            {
                if (pattern.Matches(relativePath))
                {
                    used.Add(pattern);
                    excluded = true;
                }
            }

            if (!excluded)
            {
                kept.Add(filePath);
            }
        }

        return new ExcludeResult(kept, patterns.Where(pattern => !used.Contains(pattern)).ToList());
    }
}
