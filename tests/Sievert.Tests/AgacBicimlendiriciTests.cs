using Sievert.Analysis;
using Sievert.Cli;
using Sievert.Core.Cozumleme;

using static Sievert.Tests.OrnekAnaliz;

namespace Sievert.Tests;

public class AgacBicimlendiriciTests
{
    [Fact]
    public void Agac_DosyaTipMetotSirasiylaYazilir()
    {
        string[] satirlar = Bicimlendir(Dosya("a.cs", Tip("Bir", Metot("Calis"))));

        Assert.Equal("a.cs", satirlar[0]);
        Assert.Contains("Bir (class)", satirlar[1], StringComparison.Ordinal);
        Assert.Contains("Calis", satirlar[2], StringComparison.Ordinal);
        Assert.StartsWith("`- ", satirlar[1], StringComparison.Ordinal);
    }

    [Fact]
    public void Agac_AsyncEtiketiVeSatirSayisiAyniSutundaDurur()
    {
        string[] satirlar = Bicimlendir(Dosya(
            "a.cs",
            Tip("Bir", Metot("Kisa"), Metot("CokUzunBirMetotAdiVar", asyncMi: true)),
            Tip("Iki", Metot("Digeri", asyncMi: true))));

        // Ozet bolumunde de " satir" gecen bir satir var, oraya bakmiyoruz.
        string[] agacSatirlari = satirlar.TakeWhile(satir => satir != "Ozet").ToArray();

        int[] satirSutunlari = agacSatirlari
            .Where(satir => satir.Contains(" satir", StringComparison.Ordinal))
            .Select(satir => satir.IndexOf(" satir", StringComparison.Ordinal))
            .ToArray();

        int[] asyncSutunlari = agacSatirlari
            .Where(satir => satir.Contains("[async]", StringComparison.Ordinal))
            .Select(satir => satir.IndexOf("[async]", StringComparison.Ordinal))
            .ToArray();

        Assert.Equal(3, satirSutunlari.Length);
        Assert.Single(satirSutunlari.Distinct());
        Assert.Equal(2, asyncSutunlari.Length);
        Assert.Single(asyncSutunlari.Distinct());
    }

    [Fact]
    public void Agac_UzunMetodaUyariIsaretiKonur()
    {
        string[] satirlar = Bicimlendir(Dosya(
            "a.cs",
            Tip("Bir", Metot("TamEsikte", satirSayisi: AgacBicimlendirici.UzunMetotEsigi), Metot("EsigiGecen", satirSayisi: 41))));

        Assert.DoesNotContain("(!)", Sadece(satirlar, "TamEsikte"), StringComparison.Ordinal);
        Assert.Contains("(!)", Sadece(satirlar, "EsigiGecen"), StringComparison.Ordinal);
    }

    [Fact]
    public void Agac_TipBulunamayanDosyaBelirtilir()
    {
        string[] satirlar = Bicimlendir(TipsizDosya("ustseviye.cs"));

        Assert.Contains(satirlar, satir => satir.Contains("hic tip bulunamadi", StringComparison.Ordinal));
    }

    [Fact]
    public void Agac_AyristirmaHatalariYazilir()
    {
        string[] satirlar = Bicimlendir(BozukDosya("bozuk.cs", "Satir 3: ; bekleniyor"));

        Assert.Contains(satirlar, satir => satir.Contains("! Satir 3: ; bekleniyor", StringComparison.Ordinal));
        // Hata varken ayrica "hic tip bulunamadi" demiyoruz, sebebi belli.
        Assert.DoesNotContain(satirlar, satir => satir.Contains("hic tip bulunamadi", StringComparison.Ordinal));
    }

    [Fact]
    public void Ozet_SonBolumdeButunSayilariGosterir()
    {
        string[] satirlar = Bicimlendir(Dosya("a.cs", Tip("Bir", Metot("Calis", asyncMi: true), Metot("Dur", satirSayisi: 3))));

        Assert.Contains("Ozet", satirlar);
        Assert.Contains("  Dosya                   : 1", satirlar);
        Assert.Contains("  Tip                     : 1", satirlar);
        Assert.Contains("  Metot                   : 2", satirlar);
        Assert.Contains("  Async orani             : %50.0 (1/2)", satirlar);
        Assert.Contains("  Ortalama metot uzunlugu : 2.0 satir", satirlar);
        Assert.Contains("  Ayristirilamayan dosya  : 0", satirlar);
        Assert.Contains("  Tip bulunamayan dosya   : 0", satirlar);
    }

    [Fact]
    public void EnUzunMetotlar_IstenmezseBolumHicYazilmaz()
    {
        DosyaAnalizi analiz = Dosya("a.cs", Tip("Bir", Metot("Calis")));

        Assert.DoesNotContain(Bicimlendir(analiz), satir => satir.StartsWith("En uzun", StringComparison.Ordinal));
        Assert.Contains(
            BicimlendirEnUzunlarla(analiz, 1),
            satir => satir.StartsWith("En uzun 1 metot", StringComparison.Ordinal));
    }

    [Fact]
    public void EnUzunMetotlar_DosyaVeSatirNumarasiniGosterir()
    {
        DosyaAnalizi analiz = new("a.cs", [Tip("Bir", Metot("Calis", satirSayisi: 7, baslangic: 12))], 100, []);

        Assert.Contains(
            BicimlendirEnUzunlarla(analiz, 1),
            satir => satir.Contains("Bir.Calis", StringComparison.Ordinal) && satir.Contains("a.cs:12", StringComparison.Ordinal));
    }

    [Fact]
    public void Bicimlendirme_RenkBilgisiniAyriTutar()
    {
        DosyaAnalizi[] analizler = [Dosya("a.cs", Tip("Bir", Metot("Calis", asyncMi: true)))];

        CiktiSatiri satir = AgacBicimlendirici
            .Bicimlendir(analizler, Ozetleyici.Ozetle(analizler), [])
            .First(satir => satir.DuzMetin.Contains("[async]", StringComparison.Ordinal));

        // Duz metinde ANSI kacis dizisi yok, renk sadece parcanin etiketinde duruyor.
        Assert.DoesNotContain("\u001b", satir.DuzMetin, StringComparison.Ordinal);
        Assert.Contains(satir.Parcalar, parca => parca.Renk == CiktiRengi.Etiket && parca.Metin == "[async]");
    }

    private static string[] Bicimlendir(params DosyaAnalizi[] analizler) =>
        AgacBicimlendirici.Bicimlendir(analizler, Ozetleyici.Ozetle(analizler), [])
            .Select(satir => satir.DuzMetin)
            .ToArray();

    private static string[] BicimlendirEnUzunlarla(DosyaAnalizi analiz, int adet)
    {
        DosyaAnalizi[] analizler = [analiz];
        return AgacBicimlendirici
            .Bicimlendir(analizler, Ozetleyici.Ozetle(analizler), Ozetleyici.EnUzunMetotlar(analizler, adet))
            .Select(satir => satir.DuzMetin)
            .ToArray();
    }

    private static string Sadece(string[] satirlar, string metotAdi) =>
        satirlar.Single(satir => satir.Contains(metotAdi, StringComparison.Ordinal));
}
