using Sievert.Analysis;
using Sievert.Core.Cozumleme;

namespace Sievert.Tests;

public class KorNoktaTests
{
    [Fact]
    public void KosulluDerlemeYoksa_KorNoktaSifir()
    {
        DosyaAnalizi analiz = Cozumle("public class Bir { public void Calis() { } }");

        Assert.False(analiz.KosulluDerlemeVarMi);
        Assert.Equal(0, analiz.KorNoktaSatirlari);
    }

    [Fact]
    public void KapaliDal_SatirlariSayilir()
    {
        DosyaAnalizi analiz = Cozumle("""
            public class Bir
            {
            #if NETFRAMEWORK
                public void Eski() { }
                public void Daha() { }
            #endif
                public void Yeni() { }
            }
            """);

        Assert.True(analiz.KosulluDerlemeVarMi);
        Assert.Equal(2, analiz.KorNoktaSatirlari);
    }

    [Fact]
    public void KapaliDaldakiTipler_SonuclaraGirmez()
    {
        DosyaAnalizi analiz = Cozumle("""
            #if NETFRAMEWORK
            public class Gizli
            {
                public void Calis() { }
            }
            #endif
            public class Gorunen { }
            """);

        // Kaybi telafi etmiyoruz, sadece olcuyoruz: gizli sinif sonucta yok ama satirlari sayiliyor.
        Assert.Equal(["Gorunen"], analiz.Tipler.Select(tip => tip.Ad).ToArray());
        Assert.Equal(4, analiz.KorNoktaSatirlari);
    }

    [Fact]
    public void ElseDali_AcikKalirVeSayilmaz()
    {
        DosyaAnalizi analiz = Cozumle("""
            public class Bir
            {
            #if NETFRAMEWORK
                public void Eski() { }
            #else
                public void Yeni() { }
            #endif
            }
            """);

        Assert.Equal(["Yeni"], analiz.Tipler.Single().Metotlar.Select(metot => metot.Ad).ToArray());
        Assert.Equal(1, analiz.KorNoktaSatirlari);
    }

    [Fact]
    public void AcikDal_KorNoktaSifirAmaKosulluDerlemeIsaretli()
    {
        // Hicbir sembol tanimli olmadigi icin !NETFRAMEWORK dogru, hicbir sey kaybolmuyor.
        DosyaAnalizi analiz = Cozumle("""
            #if !NETFRAMEWORK
            public class Bir { public void Calis() { } }
            #endif
            """);

        Assert.True(analiz.KosulluDerlemeVarMi);
        Assert.Equal(0, analiz.KorNoktaSatirlari);
        Assert.Single(analiz.Tipler);
    }

    [Fact]
    public void Ozet_KorNoktalariToplarVeOranHesaplar()
    {
        DosyaAnalizi[] analizler =
        [
            new("a.cs", [], ToplamSatirSayisi: 60, AyristirmaHatalari: [], KorNoktaSatirlari: 10, KosulluDerlemeVarMi: true),
            new("b.cs", [], ToplamSatirSayisi: 40, AyristirmaHatalari: [], KorNoktaSatirlari: 0, KosulluDerlemeVarMi: true),
            new("c.cs", [], ToplamSatirSayisi: 100, AyristirmaHatalari: [], KorNoktaSatirlari: 0, KosulluDerlemeVarMi: false),
        ];

        KorNokta korNokta = Ozetleyici.Ozetle(analizler).KorNokta;

        Assert.Equal(2, korNokta.DosyaSayisi);
        Assert.Equal(10, korNokta.SatirSayisi);
        Assert.Equal(0.05, korNokta.Orani); // 10 / 200
    }

    [Fact]
    public void Ozet_BosTaramadaOranSifir()
    {
        Assert.Equal(0, Ozetleyici.Ozetle([]).KorNokta.Orani);
    }

    private static DosyaAnalizi Cozumle(string kaynak) => DosyaCozumleyici.MetniCozumle(kaynak, "test.cs");
}
