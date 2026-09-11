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
    IReadOnlyList<string> AyristirmaHatalari);
