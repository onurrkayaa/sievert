using Sievert.Core.Cozumleme;

namespace Sievert.Analysis;

/// <summary>Dosya analizlerinden toplu sayilari cikarir.</summary>
public static class Ozetleyici
{
    public static TaramaOzeti Ozetle(IReadOnlyList<DosyaAnalizi> analizler)
    {
        List<SievertMetot> metotlar = analizler
            .SelectMany(analiz => analiz.Tipler)
            .SelectMany(tip => tip.Metotlar)
            .ToList();

        int asyncSayisi = metotlar.Count(metot => metot.AsyncMi);

        return new TaramaOzeti(
            DosyaSayisi: analizler.Count,
            TipSayisi: analizler.Sum(analiz => analiz.Tipler.Count),
            MetotSayisi: metotlar.Count,
            AsyncMetotSayisi: asyncSayisi,
            AsyncOrani: metotlar.Count == 0 ? 0 : (double)asyncSayisi / metotlar.Count,
            OrtalamaMetotUzunlugu: metotlar.Count == 0 ? 0 : metotlar.Average(metot => metot.SatirSayisi),
            AyristirilamayanDosyaSayisi: analizler.Count(analiz => analiz.AyristirilamadiMi),
            TipBulunamayanDosyaSayisi: analizler.Count(analiz => analiz.TipBulunamadi));
    }

    /// <summary>En uzun metotlari uzundan kisaya dogru dondurur.</summary>
    public static IReadOnlyList<MetotYeri> EnUzunMetotlar(IReadOnlyList<DosyaAnalizi> analizler, int adet) =>
        analizler
            .SelectMany(analiz => analiz.Tipler
                .SelectMany(tip => tip.Metotlar.Select(metot => new MetotYeri(analiz.DosyaYolu, tip.Ad, metot))))
            .OrderByDescending(yer => yer.Metot.SatirSayisi)
            .ThenBy(yer => yer.DosyaYolu, StringComparer.Ordinal)
            .ThenBy(yer => yer.Metot.BaslangicSatiri)
            .Take(adet)
            .ToList();
}
