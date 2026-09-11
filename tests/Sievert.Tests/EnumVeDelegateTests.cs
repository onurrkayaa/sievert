using Sievert.Analysis;
using Sievert.Core.Cozumleme;

namespace Sievert.Tests;

public class EnumVeDelegateTests
{
    [Fact]
    public void Enum_TipOlarakSayilir()
    {
        SievertTip tip = Cozumle("public enum Durum { Acik, Kapali }").Tipler.Single();

        Assert.Equal("Durum", tip.Ad);
        Assert.Equal(TipTuru.Enum, tip.Turu);
        Assert.Equal(1, tip.BaslangicSatiri);
    }

    [Fact]
    public void Enum_MetotListesiBosKalir()
    {
        Assert.Empty(Cozumle("public enum Durum { Acik, Kapali }").Tipler.Single().Metotlar);
    }

    [Fact]
    public void Delegate_TipOlarakSayilir()
    {
        SievertTip tip = Cozumle("public delegate int Secici(string deger);").Tipler.Single();

        Assert.Equal("Secici", tip.Ad);
        Assert.Equal(TipTuru.Delegate, tip.Turu);
        Assert.Empty(tip.Metotlar);
    }

    [Fact]
    public void IcIceEnumVeDelegate_UstTipinAdiylaNitelenir()
    {
        DosyaAnalizi analiz = Cozumle("""
            public class Dis
            {
                public enum Durum { Acik }

                public delegate void Haberci();

                public void Calis() { }
            }
            """);

        Assert.Equal(
            ["Dis", "Dis.Durum", "Dis.Haberci"],
            analiz.Tipler.Select(tip => tip.Ad).Order(StringComparer.Ordinal).ToArray());

        // Enum ve delegate araya girse de metot hala dogru tipe yaziliyor.
        Assert.Equal(["Calis"], analiz.Tipler.Single(tip => tip.Ad == "Dis").Metotlar.Select(metot => metot.Ad).ToArray());
    }

    [Fact]
    public void SadeceEnumIcerenDosya_TipBulunamadiSayilmaz()
    {
        Assert.False(Cozumle("public enum Durum { Acik }").TipBulunamadi);
    }

    [Theory]
    [InlineData(TipTuru.Sinif, "class")]
    [InlineData(TipTuru.Record, "record")]
    [InlineData(TipTuru.Struct, "struct")]
    [InlineData(TipTuru.Interface, "interface")]
    [InlineData(TipTuru.Enum, "enum")]
    [InlineData(TipTuru.Delegate, "delegate")]
    public void AnahtarKelime_HerTipIcinCSharpAdiniVerir(TipTuru turu, string beklenen)
    {
        Assert.Equal(beklenen, turu.AnahtarKelime());
        Assert.Equal(turu, TipTuruAdlari.Cozumle(beklenen));
    }

    private static DosyaAnalizi Cozumle(string kaynak) => DosyaCozumleyici.MetniCozumle(kaynak, "test.cs");
}
