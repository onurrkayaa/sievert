using Sievert.Analysis;
using Sievert.Cli;
using Sievert.Core.Cozumleme;

using static Sievert.Tests.OrnekAnaliz;

namespace Sievert.Tests;

public class DagilimVeTestAyrimiTests
{
    [Fact]
    public void Uzunluk_TekSayidaMetottaOrtadakiDeger()
    {
        MetotUzunlugu uzunluk = Uzunluklar(1, 5, 100);

        Assert.Equal(5, uzunluk.Medyan);
        Assert.Equal(100, uzunluk.EnUzun);
    }

    [Fact]
    public void Uzunluk_CiftSayidaMetottaIkiOrtancaninOrtalamasi()
    {
        Assert.Equal(7.5, Uzunluklar(1, 5, 10, 100).Medyan);
    }

    [Fact]
    public void Uzunluk_OrtalamaMedyandanBuyukOlabilir()
    {
        MetotUzunlugu uzunluk = Uzunluklar(1, 1, 1, 1, 200);

        Assert.Equal(1, uzunluk.Medyan);
        Assert.Equal(40.8, uzunluk.Ortalama);
    }

    [Fact]
    public void Uzunluk_YuzdelikSiradakiDegeriDondurur()
    {
        // 1..100 arasi 100 metot: p90 90, p95 95.
        MetotUzunlugu uzunluk = Uzunluklar([.. Enumerable.Range(1, 100)]);

        Assert.Equal(90, uzunluk.P90);
        Assert.Equal(95, uzunluk.P95);
        Assert.Equal(100, uzunluk.EnUzun);
    }

    [Fact]
    public void Uzunluk_HicMetotYoksaHepsiSifir()
    {
        MetotUzunlugu uzunluk = Ozetleyici.Ozetle([]).MetotUzunlugu;

        Assert.Equal(0, uzunluk.Ortalama);
        Assert.Equal(0, uzunluk.Medyan);
        Assert.Equal(0, uzunluk.P90);
        Assert.Equal(0, uzunluk.P95);
        Assert.Equal(0, uzunluk.EnUzun);
    }

    [Theory]
    [InlineData("test/Polly.Specs/CacheSpecs.cs", true)]
    [InlineData("tests/Sievert.Tests/BannerTests.cs", true)]
    [InlineData("src/Uygulama/BannerTests.cs", true)]
    [InlineData("src/Uygulama/OdemeTest.cs", true)]
    [InlineData("src/Uygulama/Odeme.cs", false)]
    [InlineData("src/Testler/Odeme.cs", false)]
    [InlineData("src/Contest/Odeme.cs", false)]
    public void TestKodu_YolaVeAdaBakarakTahminEdilir(string yol, bool beklenen)
    {
        Assert.Equal(beklenen, DosyaAnalizi.TestKoduMu(yol));
    }

    [Fact]
    public void Ozet_UretimVeTestKodunuAyriSayar()
    {
        KodAyrimi ayrim = Ozetleyici.Ozetle(
        [
            Dosya("src/Odeme.cs", Tip("Odeme", Metot("Calis"), Metot("Dur"))),
            Dosya("test/OdemeSpecs.cs", Tip("OdemeSpecs", Metot("Bir"))),
            Dosya("src/OdemeTests.cs", Tip("OdemeTests", Metot("Iki"), Metot("Uc"), Metot("Dort"))),
        ]).KodAyrimi;

        Assert.Equal(1, ayrim.UretimDosyaSayisi);
        Assert.Equal(2, ayrim.TestDosyaSayisi);
        Assert.Equal(2, ayrim.UretimMetotSayisi);
        Assert.Equal(4, ayrim.TestMetotSayisi);
    }

    [Fact]
    public void Agac_OzetindeDagilimVeKorNoktaYerAlir()
    {
        DosyaAnalizi[] analizler =
        [
            new("a.cs", [Tip("Bir", Metot("Calis", satirSayisi: 10))], 50, [], KorNoktaSatirlari: 5, KosulluDerlemeVarMi: true),
        ];

        string[] satirlar = AgacBicimlendirici
            .Bicimlendir(analizler, Ozetleyici.Ozetle(analizler), [])
            .Select(satir => satir.DuzMetin)
            .ToArray();

        Assert.Contains(satirlar, satir => satir.StartsWith("a.cs  [#if - 5 satir gorulmedi]", StringComparison.Ordinal));
        Assert.Contains(satirlar, satir => satir.Contains("medyan 10.0", StringComparison.Ordinal) && satir.Contains("p95 10", StringComparison.Ordinal));
        Assert.Contains(satirlar, satir => satir.Contains("1 dosyada #if, 5 satir gorulmedi (%10.0)", StringComparison.Ordinal));
    }

    [Fact]
    public void Agac_KorNoktasiOlmayanKosulluDosyaSadeceIsaretlenir()
    {
        DosyaAnalizi[] analizler = [new("a.cs", [], 10, [], KorNoktaSatirlari: 0, KosulluDerlemeVarMi: true)];

        string[] satirlar = AgacBicimlendirici
            .Bicimlendir(analizler, Ozetleyici.Ozetle(analizler), [])
            .Select(satir => satir.DuzMetin)
            .ToArray();

        Assert.Equal("a.cs  [#if]", satirlar[0]);
    }

    private static MetotUzunlugu Uzunluklar(params int[] satirSayilari)
    {
        DosyaAnalizi analiz = Dosya(
            "a.cs",
            Tip("Bir", [.. satirSayilari.Select((sayi, sira) => Metot($"M{sira}", satirSayisi: sayi))]));

        return Ozetleyici.Ozetle([analiz]).MetotUzunlugu;
    }
}
