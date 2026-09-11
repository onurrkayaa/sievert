using Sievert.Core.Cozumleme;

namespace Sievert.Tests;

/// <summary>Testlerde elle analiz nesnesi kurmak icin kisa yollar.</summary>
internal static class OrnekAnaliz
{
    public static SievertMetot Metot(string ad, int satirSayisi = 1, bool asyncMi = false, int baslangic = 1) =>
        new(ad, baslangic, satirSayisi, asyncMi, ParametreSayisi: 0, DonusTipi: "void");

    public static SievertTip Tip(string ad, params SievertMetot[] metotlar) =>
        new(ad, TipTuru.Sinif, BaslangicSatiri: 1, metotlar);

    public static DosyaAnalizi Dosya(string yol, params SievertTip[] tipler) =>
        new(yol, tipler, ToplamSatirSayisi: 100, AyristirmaHatalari: []);

    public static DosyaAnalizi BozukDosya(string yol, params string[] hatalar) =>
        new(yol, [], ToplamSatirSayisi: 10, hatalar);

    public static DosyaAnalizi TipsizDosya(string yol) =>
        new(yol, [], ToplamSatirSayisi: 5, AyristirmaHatalari: []);
}
