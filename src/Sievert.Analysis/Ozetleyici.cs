using Sievert.Core.Cozumleme;

namespace Sievert.Analysis;

/// <summary>Dosya analizlerinden toplu sayilari cikarir.</summary>
public static class Ozetleyici
{
    public static TaramaOzeti Ozetle(IReadOnlyList<DosyaAnalizi> analizler)
    {
        List<SievertMetot> metotlar = analizler.SelectMany(MetotlariAl).ToList();
        int asyncSayisi = metotlar.Count(metot => metot.AsyncMi);
        int korNoktaSatirlari = analizler.Sum(analiz => analiz.KorNoktaSatirlari);
        int toplamSatir = analizler.Sum(analiz => analiz.ToplamSatirSayisi);

        return new TaramaOzeti(
            DosyaSayisi: analizler.Count,
            TipSayisi: analizler.Sum(analiz => analiz.Tipler.Count),
            MetotSayisi: metotlar.Count,
            AsyncMetotSayisi: asyncSayisi,
            AsyncOrani: Oran(asyncSayisi, metotlar.Count),
            MetotUzunlugu: UzunlukIstatistigi(metotlar),
            AyristirilamayanDosyaSayisi: analizler.Count(analiz => analiz.AyristirilamadiMi),
            TipBulunamayanDosyaSayisi: analizler.Count(analiz => analiz.TipBulunamadi),
            KorNokta: new KorNokta(
                analizler.Count(analiz => analiz.KosulluDerlemeVarMi),
                korNoktaSatirlari,
                Oran(korNoktaSatirlari, toplamSatir)),
            KodAyrimi: new KodAyrimi(
                UretimDosyaSayisi: analizler.Count(analiz => !analiz.TestKodu),
                TestDosyaSayisi: analizler.Count(analiz => analiz.TestKodu),
                UretimMetotSayisi: analizler.Where(analiz => !analiz.TestKodu).Sum(analiz => MetotlariAl(analiz).Count()),
                TestMetotSayisi: analizler.Where(analiz => analiz.TestKodu).Sum(analiz => MetotlariAl(analiz).Count())));
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

    private static IEnumerable<SievertMetot> MetotlariAl(DosyaAnalizi analiz) =>
        analiz.Tipler.SelectMany(tip => tip.Metotlar);

    private static MetotUzunlugu UzunlukIstatistigi(List<SievertMetot> metotlar)
    {
        if (metotlar.Count == 0)
        {
            return new MetotUzunlugu(0, 0, 0, 0, 0);
        }

        int[] uzunluklar = metotlar.Select(metot => metot.SatirSayisi).Order().ToArray();

        return new MetotUzunlugu(
            Ortalama: uzunluklar.Average(),
            Medyan: Medyan(uzunluklar),
            P90: Yuzdelik(uzunluklar, 0.90),
            P95: Yuzdelik(uzunluklar, 0.95),
            EnUzun: uzunluklar[^1]);
    }

    /// <summary>Cift sayida deger varsa ortadaki ikisinin ortalamasi.</summary>
    private static double Medyan(int[] siralanmis) =>
        siralanmis.Length % 2 == 1
            ? siralanmis[siralanmis.Length / 2]
            : (siralanmis[siralanmis.Length / 2 - 1] + siralanmis[siralanmis.Length / 2]) / 2.0;

    /// <summary>En yakin siraya gore yuzdelik: siradaki degeri dondurur, ara deger uretmez.</summary>
    private static int Yuzdelik(int[] siralanmis, double yuzde)
    {
        int sira = (int)Math.Ceiling(yuzde * siralanmis.Length) - 1;
        return siralanmis[Math.Clamp(sira, 0, siralanmis.Length - 1)];
    }

    private static double Oran(int pay, int payda) => payda == 0 ? 0 : (double)pay / payda;
}
