using Sievert.Analysis;
using Sievert.Core.Cozumleme;

using static Sievert.Tests.OrnekAnaliz;

namespace Sievert.Tests;

public class OzetleyiciTests
{
    private static readonly DosyaAnalizi[] Analizler =
    [
        Dosya("a.cs", Tip("Bir", Metot("Kisa", satirSayisi: 2), Metot("Uzun", satirSayisi: 10, asyncMi: true))),
        Dosya("b.cs", Tip("Iki", Metot("Orta", satirSayisi: 6, asyncMi: true)), Tip("Uc", Metot("Tek", satirSayisi: 2))),
        BozukDosya("bozuk.cs", "Satir 3: ; bekleniyor"),
        TipsizDosya("ustseviye.cs"),
    ];

    [Fact]
    public void Sayilar_Toplanir()
    {
        TaramaOzeti ozet = Ozetleyici.Ozetle(Analizler);

        Assert.Equal(4, ozet.DosyaSayisi);
        Assert.Equal(3, ozet.TipSayisi);
        Assert.Equal(4, ozet.MetotSayisi);
        Assert.Equal(2, ozet.AsyncMetotSayisi);
    }

    [Fact]
    public void AsyncOraniVeOrtalamaUzunluk_Hesaplanir()
    {
        TaramaOzeti ozet = Ozetleyici.Ozetle(Analizler);

        Assert.Equal(0.5, ozet.AsyncOrani);
        Assert.Equal(5.0, ozet.OrtalamaMetotUzunlugu); // (2 + 10 + 6 + 2) / 4
    }

    [Fact]
    public void BozukVeTipsizDosyalar_AyriAyriSayilir()
    {
        TaramaOzeti ozet = Ozetleyici.Ozetle(Analizler);

        Assert.Equal(1, ozet.AyristirilamayanDosyaSayisi);
        // Bozuk dosyada da tip yok, o yuzden ikisi de tipsiz sayiliyor.
        Assert.Equal(2, ozet.TipBulunamayanDosyaSayisi);
    }

    [Fact]
    public void BosTarama_SifiraBolmez()
    {
        TaramaOzeti ozet = Ozetleyici.Ozetle([]);

        Assert.Equal(0, ozet.MetotSayisi);
        Assert.Equal(0, ozet.AsyncOrani);
        Assert.Equal(0, ozet.OrtalamaMetotUzunlugu);
    }

    [Fact]
    public void EnUzunMetotlar_UzundanKisayaSiralanir()
    {
        IReadOnlyList<MetotYeri> enUzunlar = Ozetleyici.EnUzunMetotlar(Analizler, 2);

        Assert.Equal(["Uzun", "Orta"], enUzunlar.Select(yer => yer.Metot.Ad).ToArray());
        Assert.Equal("a.cs", enUzunlar[0].DosyaYolu);
        Assert.Equal("Bir", enUzunlar[0].TipAdi);
    }

    [Fact]
    public void EnUzunMetotlar_IstenenSayidanFazlasiniDondurmez()
    {
        Assert.Equal(3, Ozetleyici.EnUzunMetotlar(Analizler, 3).Count);
        Assert.Equal(4, Ozetleyici.EnUzunMetotlar(Analizler, 99).Count);
    }
}
