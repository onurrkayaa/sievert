using System.Globalization;

using Sievert.Core.Cozumleme;

namespace Sievert.Cli;

/// <summary>Tarama sonucunu ASCII agac olarak satirlara cevirir.</summary>
public static class AgacBicimlendirici
{
    /// <summary>Bu satirdan uzun metotlara uyari isareti konur.</summary>
    public const int UzunMetotEsigi = 40;

    /// <summary>Ekranda bir dosya icin en fazla kac ayristirma hatasi gosterilecegi. JSON ciktisinda hepsi yer alir.</summary>
    public const int EkrandaGosterilecekHataSayisi = 3;

    private const string AsyncEtiketi = "[async]";

    /// <summary>
    /// Dosyalari, tipleri ve metotlari agac halinde yazar; istenirse en uzun metotlari
    /// ve en sonda ozeti ekler. Renk burada secilmiyor, her parcaya sadece rolu yaziliyor.
    /// </summary>
    public static IReadOnlyList<CiktiSatiri> Bicimlendir(
        IReadOnlyList<DosyaAnalizi> analizler,
        TaramaOzeti ozet,
        IReadOnlyList<MetotYeri> enUzunMetotlar)
    {
        int adSutunu = MetotAdiSutunGenisligi(analizler);
        int satirSutunu = SatirSayisiSutunGenisligi(analizler);

        List<CiktiSatiri> satirlar = [];

        foreach (DosyaAnalizi analiz in analizler)
        {
            satirlar.Add(DosyaBasligi(analiz));
            satirlar.AddRange(DosyayiYaz(analiz, adSutunu, satirSutunu));
        }

        if (enUzunMetotlar.Count > 0)
        {
            satirlar.Add(BosSatir());
            satirlar.Add(Satir(new CiktiParcasi($"En uzun {enUzunMetotlar.Count} metot", CiktiRengi.Baslik)));
            satirlar.AddRange(EnUzunlariYaz(enUzunMetotlar, satirSutunu));
        }

        satirlar.Add(BosSatir());
        satirlar.Add(Satir(new CiktiParcasi("Ozet", CiktiRengi.Baslik)));
        satirlar.AddRange(OzetiYaz(ozet));

        return satirlar;
    }

    private static IEnumerable<CiktiSatiri> DosyayiYaz(DosyaAnalizi analiz, int adSutunu, int satirSutunu)
    {
        for (int i = 0; i < analiz.Tipler.Count; i++)
        {
            SievertTip tip = analiz.Tipler[i];
            bool sonTip = i == analiz.Tipler.Count - 1 && analiz.AyristirmaHatalari.Count == 0;

            yield return Satir(
                new CiktiParcasi(sonTip ? "`- " : "+- ", CiktiRengi.Soluk),
                new CiktiParcasi(tip.Ad, CiktiRengi.Normal),
                new CiktiParcasi($" ({tip.Turu.AnahtarKelime()})", CiktiRengi.Soluk));

            for (int j = 0; j < tip.Metotlar.Count; j++)
            {
                yield return MetotSatiri(
                    tip.Metotlar[j],
                    (sonTip ? "   " : "|  ") + (j == tip.Metotlar.Count - 1 ? "`- " : "+- "),
                    adSutunu,
                    satirSutunu);
            }
        }

        if (analiz.TipBulunamadi && !analiz.AyristirilamadiMi)
        {
            yield return Satir(
                new CiktiParcasi("`- ", CiktiRengi.Soluk),
                new CiktiParcasi("(hic tip bulunamadi)", CiktiRengi.Soluk));
        }

        foreach (CiktiSatiri satir in HatalariYaz(analiz))
        {
            yield return satir;
        }
    }

    private static IEnumerable<CiktiSatiri> HatalariYaz(DosyaAnalizi analiz)
    {
        for (int i = 0; i < Math.Min(EkrandaGosterilecekHataSayisi, analiz.AyristirmaHatalari.Count); i++)
        {
            bool sonuncu = i == analiz.AyristirmaHatalari.Count - 1;
            yield return Satir(
                new CiktiParcasi(sonuncu ? "`- " : "+- ", CiktiRengi.Soluk),
                new CiktiParcasi("! " + analiz.AyristirmaHatalari[i], CiktiRengi.Uyari));
        }

        int kalan = analiz.AyristirmaHatalari.Count - EkrandaGosterilecekHataSayisi;
        if (kalan > 0)
        {
            yield return Satir(
                new CiktiParcasi("`- ", CiktiRengi.Soluk),
                new CiktiParcasi($"! ve {kalan} hata daha", CiktiRengi.Uyari));
        }
    }

    private static CiktiSatiri MetotSatiri(SievertMetot metot, string onEk, int adSutunu, int satirSutunu)
    {
        List<CiktiParcasi> parcalar =
        [
            new CiktiParcasi(onEk, CiktiRengi.Soluk),
            new CiktiParcasi(metot.Ad.PadRight(adSutunu - onEk.Length + 2), CiktiRengi.Normal),
            new CiktiParcasi(
                metot.AsyncMi ? AsyncEtiketi : new string(' ', AsyncEtiketi.Length),
                metot.AsyncMi ? CiktiRengi.Etiket : CiktiRengi.Normal),
            new CiktiParcasi(
                "  " + metot.SatirSayisi.ToString(CultureInfo.InvariantCulture).PadLeft(satirSutunu) + " satir",
                CiktiRengi.Soluk),
        ];

        if (metot.SatirSayisi > UzunMetotEsigi)
        {
            parcalar.Add(new CiktiParcasi("  (!)", CiktiRengi.Uyari));
        }

        return new CiktiSatiri(parcalar);
    }

    private static IEnumerable<CiktiSatiri> EnUzunlariYaz(
        IReadOnlyList<MetotYeri> enUzunMetotlar,
        int satirSutunu)
    {
        int siraSutunu = enUzunMetotlar.Count.ToString(CultureInfo.InvariantCulture).Length;
        int adSutunu = enUzunMetotlar.Max(yer => $"{yer.TipAdi}.{yer.Metot.Ad}".Length);

        for (int i = 0; i < enUzunMetotlar.Count; i++)
        {
            MetotYeri yer = enUzunMetotlar[i];
            string sira = (i + 1).ToString(CultureInfo.InvariantCulture).PadLeft(siraSutunu);
            string uzunluk = yer.Metot.SatirSayisi.ToString(CultureInfo.InvariantCulture).PadLeft(satirSutunu);

            yield return Satir(
                new CiktiParcasi($"  {sira}. ", CiktiRengi.Soluk),
                new CiktiParcasi($"{uzunluk} satir  ", yer.Metot.SatirSayisi > UzunMetotEsigi ? CiktiRengi.Uyari : CiktiRengi.Normal),
                new CiktiParcasi($"{yer.TipAdi}.{yer.Metot.Ad}".PadRight(adSutunu + 2), CiktiRengi.Normal),
                new CiktiParcasi(
                    $"{yer.DosyaYolu}:{yer.Metot.BaslangicSatiri}",
                    CiktiRengi.Soluk));
        }
    }

    private static IEnumerable<CiktiSatiri> OzetiYaz(TaramaOzeti ozet)
    {
        MetotUzunlugu uzunluk = ozet.MetotUzunlugu;
        KodAyrimi ayrim = ozet.KodAyrimi;

        (string Etiket, string Deger)[] satirlar =
        [
            ("Dosya", $"{Sayi(ozet.DosyaSayisi)}  (uretim {ayrim.UretimDosyaSayisi} / test {ayrim.TestDosyaSayisi})"),
            ("Tip", Sayi(ozet.TipSayisi)),
            ("Metot", $"{Sayi(ozet.MetotSayisi)}  (uretim {ayrim.UretimMetotSayisi} / test {ayrim.TestMetotSayisi})"),
            ("Async orani", $"%{Ondalik(ozet.AsyncOrani * 100)} ({ozet.AsyncMetotSayisi}/{ozet.MetotSayisi})"),
            ("Metot uzunlugu", $"ortalama {Ondalik(uzunluk.Ortalama)}  medyan {Ondalik(uzunluk.Medyan)}  p90 {uzunluk.P90}  p95 {uzunluk.P95}  en uzun {uzunluk.EnUzun}"),
            ("Kor nokta", KorNoktaOzeti(ozet.KorNokta)),
            ("Ayristirilamayan dosya", Sayi(ozet.AyristirilamayanDosyaSayisi)),
            ("Tip bulunamayan dosya", Sayi(ozet.TipBulunamayanDosyaSayisi)),
        ];

        int etiketSutunu = satirlar.Max(satir => satir.Etiket.Length);

        foreach ((string etiket, string deger) in satirlar)
        {
            yield return Satir(
                new CiktiParcasi("  " + etiket.PadRight(etiketSutunu) + " : ", CiktiRengi.Soluk),
                new CiktiParcasi(deger, CiktiRengi.Normal));
        }
    }

    /// <summary>Metot adlari hangi dosyada olursa olsun ayni sutunda dursun diye toplu olcum.</summary>
    private static int MetotAdiSutunGenisligi(IReadOnlyList<DosyaAnalizi> analizler)
    {
        // 3 karakter agac on eki + 2 karakter bosluk payi.
        int enUzunAd = analizler
            .SelectMany(analiz => analiz.Tipler)
            .SelectMany(tip => tip.Metotlar)
            .Select(metot => metot.Ad.Length)
            .DefaultIfEmpty(0)
            .Max();

        return enUzunAd + 6;
    }

    private static int SatirSayisiSutunGenisligi(IReadOnlyList<DosyaAnalizi> analizler) =>
        analizler
            .SelectMany(analiz => analiz.Tipler)
            .SelectMany(tip => tip.Metotlar)
            .Select(metot => metot.SatirSayisi.ToString(CultureInfo.InvariantCulture).Length)
            .DefaultIfEmpty(1)
            .Max();

    /// <summary>Dosya adi, kosullu derleme varsa yaninda kucuk bir isaret.</summary>
    private static CiktiSatiri DosyaBasligi(DosyaAnalizi analiz)
    {
        if (!analiz.KosulluDerlemeVarMi)
        {
            return Satir(new CiktiParcasi(analiz.DosyaYolu, CiktiRengi.Baslik));
        }

        string isaret = analiz.KorNoktaSatirlari > 0
            ? $"  [#if - {analiz.KorNoktaSatirlari} satir gorulmedi]"
            : "  [#if]";

        return Satir(
            new CiktiParcasi(analiz.DosyaYolu, CiktiRengi.Baslik),
            new CiktiParcasi(isaret, CiktiRengi.Uyari));
    }

    private static string KorNoktaOzeti(KorNokta korNokta) =>
        korNokta.DosyaSayisi == 0
            ? "yok"
            : $"{korNokta.DosyaSayisi} dosyada #if, {korNokta.SatirSayisi} satir gorulmedi (%{Ondalik(korNokta.Orani * 100)})";

    private static string Sayi(int deger) => deger.ToString(CultureInfo.InvariantCulture);

    private static string Ondalik(double deger) => deger.ToString("0.0", CultureInfo.InvariantCulture);

    private static CiktiSatiri Satir(params CiktiParcasi[] parcalar) => new(parcalar);

    private static CiktiSatiri BosSatir() => new([]);
}
