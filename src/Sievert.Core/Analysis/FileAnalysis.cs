namespace Sievert.Core.Analysis;

/// <summary>Tek bir kaynak dosyanın çözümleme sonucu.</summary>
/// <param name="FilePath">Çözümlenen dosyanın yolu, tarama köküne göre göreli.</param>
/// <param name="Types">Dosyada bulunan tipler.</param>
/// <param name="TotalLineCount">Dosyanın satır sayısı.</param>
/// <param name="ParseErrors">Sözdizimi hataları. Boş değilse sonuç eksik olabilir.</param>
/// <param name="BlindSpotLines">
/// Kapalı <c>#if</c> dallarında kaldığı için hiç çözümlenmemiş satır sayısı.
/// Bu satırlardaki tipler ve metotlar sonuçta yok; sayı sadece ne kadarını görmediğimizi söylüyor.
/// </param>
/// <param name="HasConditionalCompilation">
/// Dosyada <c>#if</c> var mı. Kör nokta sıfır olsa bile true olabilir: açık kalan dalda kod varsa
/// bir şey kaybetmeyiz ama dosyanın başka bir hedefte farklı derlendiğini bilmek işe yarıyor.
/// </param>
public sealed record FileAnalysis(
    string FilePath,
    IReadOnlyList<SievertType> Types,
    int TotalLineCount,
    IReadOnlyList<string> ParseErrors,
    int BlindSpotLines = 0,
    bool HasConditionalCompilation = false)
{
    /// <summary>
    /// Dosya okundu ama içinde hiç tip yok. Top-level statement içeren dosyalar ve
    /// sadece using/attribute barındıran dosyalar bu duruma düşer; hata sayılmazlar.
    /// </summary>
    public bool NoTypesFound => Types.Count == 0;

    /// <summary>Dosyada en az bir sözdizimi hatası bulundu, sonuç eksik olabilir.</summary>
    public bool HasParseErrors => ParseErrors.Count > 0;

    /// <summary>Dosya test kodu gibi duruyor mu. Kesin değil, sadece yola ve ada bakan bir tahmin.</summary>
    public bool IsTestCode => IsTestCodePath(FilePath);

    /// <summary>
    /// Yolunda test ya da tests klasörü geçen veya adı Test.cs / Tests.cs ile biten dosyaları
    /// test kodu sayar. Heuristik: test projesini başka türlü adlandıran repolarda yanılır.
    /// </summary>
    public static bool IsTestCodePath(string filePath)
    {
        string name = Path.GetFileName(filePath);
        if (name.EndsWith("Test.cs", StringComparison.OrdinalIgnoreCase)
            || name.EndsWith("Tests.cs", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        string[] directories = Path.GetDirectoryName(filePath)?.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) ?? [];
        return directories.Any(directory =>
            directory.Equals("test", StringComparison.OrdinalIgnoreCase)
            || directory.Equals("tests", StringComparison.OrdinalIgnoreCase));
    }
}
