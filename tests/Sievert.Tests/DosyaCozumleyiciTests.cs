using Sievert.Analysis;
using Sievert.Core.Cozumleme;

namespace Sievert.Tests;

public class DosyaCozumleyiciTests
{
    private static DosyaAnalizi OrnegiCozumle(string dosyaAdi) =>
        DosyaCozumleyici.DosyayiCozumle(Path.Combine(AppContext.BaseDirectory, "Hastalar", dosyaAdi));

    [Fact]
    public void Normal_TipSayisiDogru()
    {
        DosyaAnalizi analiz = OrnegiCozumle("Normal.cs");

        // Partial olan HastaServisi tek tip sayilir.
        Assert.Equal(
            ["HastaServisi", "HastaServisi.Randevu", "IKayitDefteri", "Olcum", "Tani"],
            analiz.Tipler.Select(tip => tip.Ad).Order(StringComparer.Ordinal).ToArray());
    }

    [Fact]
    public void Normal_MetotSayisiDogru()
    {
        DosyaAnalizi analiz = OrnegiCozumle("Normal.cs");

        Assert.Equal(8, analiz.Tipler.Sum(tip => tip.Metotlar.Count));
    }

    [Fact]
    public void Normal_AsyncMetotlarIsaretlenir()
    {
        DosyaAnalizi analiz = OrnegiCozumle("Normal.cs");

        string[] asyncOlanlar = analiz.Tipler
            .SelectMany(tip => tip.Metotlar)
            .Where(metot => metot.AsyncMi)
            .Select(metot => metot.Ad)
            .ToArray();

        Assert.Equal(2, asyncOlanlar.Length);
        Assert.Contains("HastaSayisiniGetirAsync", asyncOlanlar);
        Assert.Contains("IptalEtAsync", asyncOlanlar);
    }

    [Fact]
    public void Normal_IcIceSinifBulunur()
    {
        DosyaAnalizi analiz = OrnegiCozumle("Normal.cs");

        SievertTip randevu = analiz.Tipler.Single(tip => tip.Ad == "HastaServisi.Randevu");

        Assert.Equal(TipTuru.Sinif, randevu.Turu);
        Assert.Equal(["Kod", "IptalEtAsync"], randevu.Metotlar.Select(metot => metot.Ad).ToArray());
    }

    [Fact]
    public void Normal_YerelFonksiyonVeConstructorMetotSayilmaz()
    {
        DosyaAnalizi analiz = OrnegiCozumle("Normal.cs");

        SievertTip servis = analiz.Tipler.Single(tip => tip.Ad == "HastaServisi");

        // Partial'in iki parcasindaki metotlar birlesir: iki normal metot + Aktif.
        Assert.Equal(["HastaSayisiniGetirAsync", "AdiniBicimlendir", "Aktif"], servis.Metotlar.Select(metot => metot.Ad).ToArray());
        Assert.DoesNotContain(servis.Metotlar, metot => metot.Ad == "Temizle");
        Assert.DoesNotContain(servis.Metotlar, metot => metot.Ad == "HastaServisi");
        Assert.DoesNotContain(servis.Metotlar, metot => metot.Ad == "BaglantiMetni");
    }

    [Fact]
    public void Normal_IfadeGovdeliMetotSayilir()
    {
        DosyaAnalizi analiz = OrnegiCozumle("Normal.cs");

        SievertMetot yuzde = analiz.Tipler.Single(tip => tip.Ad == "Olcum").Metotlar.Single();

        Assert.Equal("Yuzde", yuzde.Ad);
        Assert.Equal(1, yuzde.ParametreSayisi);
        Assert.Equal("double", yuzde.DonusTipi);
        Assert.False(yuzde.AsyncMi);
    }

    [Fact]
    public void Bozuk_ExceptionFirlatmaz_HatalariListeler()
    {
        DosyaAnalizi analiz = OrnegiCozumle("Bozuk.cs");

        Assert.NotEmpty(analiz.AyristirmaHatalari);
        // Cozulebilen kisim yine de geri gelmeli.
        Assert.Contains(analiz.Tipler, tip => tip.Ad == "BozukServis");
        Assert.Contains(analiz.Tipler.SelectMany(tip => tip.Metotlar), metot => metot.Ad == "Topla");
    }

    [Fact]
    public void Normal_HataYok()
    {
        DosyaAnalizi analiz = OrnegiCozumle("Normal.cs");

        Assert.Empty(analiz.AyristirmaHatalari);
    }
}
