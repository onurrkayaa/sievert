namespace Sievert.Core.Cozumleme;

/// <summary>Tek bir kaynak dosyanın çözümleme sonucu.</summary>
/// <param name="DosyaYolu">Çözümlenen dosyanın yolu.</param>
/// <param name="Tipler">Dosyada bulunan tipler.</param>
/// <param name="ToplamSatirSayisi">Dosyanın satır sayısı.</param>
/// <param name="AyristirmaHatalari">Sözdizimi hataları. Boş değilse sonuç eksik olabilir.</param>
public sealed record DosyaAnalizi(
    string DosyaYolu,
    IReadOnlyList<SievertTip> Tipler,
    int ToplamSatirSayisi,
    IReadOnlyList<string> AyristirmaHatalari)
{
    /// <summary>
    /// Dosya okundu ama içinde hiç tip yok. Top-level statement içeren dosyalar ve
    /// sadece using/attribute barındıran dosyalar bu duruma düşer; hata sayılmazlar.
    /// </summary>
    public bool TipBulunamadi => Tipler.Count == 0;

    /// <summary>Dosyada en az bir sözdizimi hatası bulundu, sonuç eksik olabilir.</summary>
    public bool AyristirilamadiMi => AyristirmaHatalari.Count > 0;
}
