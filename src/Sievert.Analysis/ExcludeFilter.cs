namespace Sievert.Analysis;

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
    public static IReadOnlyList<string> Apply(
        IReadOnlyList<string> filePaths,
        string scanRoot,
        IReadOnlyList<GlobPattern> patterns)
    {
        if (patterns.Count == 0)
        {
            return filePaths;
        }

        return filePaths
            .Where(filePath => !IsExcluded(ScanRoot.RelativePath(filePath, scanRoot), patterns))
            .ToList();
    }

    private static bool IsExcluded(string relativePath, IReadOnlyList<GlobPattern> patterns) =>
        patterns.Any(pattern => pattern.Matches(relativePath));
}
