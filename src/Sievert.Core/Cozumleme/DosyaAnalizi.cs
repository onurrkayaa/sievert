namespace Sievert.Core.Cozumleme;

/// <summary>Tek bir kaynak dosyanın çözümleme sonucu.</summary>
/// <param name="DosyaYolu">Çözümlenen dosyanın yolu, tarama köküne göre göreli.</param>
/// <param name="Tipler">Dosyada bulunan tipler.</param>
/// <param name="ToplamSatirSayisi">Dosyanın satır sayısı.</param>
/// <param name="AyristirmaHatalari">Sözdizimi hataları. Boş değilse sonuç eksik olabilir.</param>
/// <param name="KorNoktaSatirlari">
/// Kapalı <c>#if</c> dallarında kaldığı için hiç çözümlenmemiş satır sayısı.
/// Bu satırlardaki tipler ve metotlar sonuçta yok; sayı sadece ne kadarını görmediğimizi söylüyor.
/// </param>
/// <param name="KosulluDerlemeVarMi">
/// Dosyada <c>#if</c> var mı. Kör nokta sıfır olsa bile true olabilir: açık kalan dalda kod varsa
/// bir şey kaybetmeyiz ama dosyanın başka bir hedefte farklı derlendiğini bilmek işe yarıyor.
/// </param>
public sealed record DosyaAnalizi(
    string DosyaYolu,
    IReadOnlyList<SievertTip> Tipler,
    int ToplamSatirSayisi,
    IReadOnlyList<string> AyristirmaHatalari,
    int KorNoktaSatirlari = 0,
    bool KosulluDerlemeVarMi = false)
{
    /// <summary>
    /// Dosya okundu ama içinde hiç tip yok. Top-level statement içeren dosyalar ve
    /// sadece using/attribute barındıran dosyalar bu duruma düşer; hata sayılmazlar.
    /// </summary>
    public bool TipBulunamadi => Tipler.Count == 0;

    /// <summary>Dosyada en az bir sözdizimi hatası bulundu, sonuç eksik olabilir.</summary>
    public bool AyristirilamadiMi => AyristirmaHatalari.Count > 0;

    /// <summary>Dosya test kodu gibi duruyor mu. Kesin değil, sadece yola ve ada bakan bir tahmin.</summary>
    public bool TestKodu => TestKoduMu(DosyaYolu);

    /// <summary>
    /// Yolunda test ya da tests klasörü geçen veya adı Test.cs / Tests.cs ile biten dosyaları
    /// test kodu sayar. Heuristik: test projesini başka türlü adlandıran repolarda yanılır.
    /// </summary>
    public static bool TestKoduMu(string dosyaYolu)
    {
        string ad = Path.GetFileName(dosyaYolu);
        if (ad.EndsWith("Test.cs", StringComparison.OrdinalIgnoreCase)
            || ad.EndsWith("Tests.cs", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        string[] klasorler = Path.GetDirectoryName(dosyaYolu)?.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) ?? [];
        return klasorler.Any(klasor =>
            klasor.Equals("test", StringComparison.OrdinalIgnoreCase)
            || klasor.Equals("tests", StringComparison.OrdinalIgnoreCase));
    }
}
