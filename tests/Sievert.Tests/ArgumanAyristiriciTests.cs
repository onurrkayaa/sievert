using Sievert.Cli;

namespace Sievert.Tests;

public class ArgumanAyristiriciTests
{
    [Fact]
    public void Scan_YolOkunur()
    {
        TaramaArgumanlari ayarlar = Basarili(["scan", "src"]);

        Assert.Equal("src", ayarlar.Yol);
        Assert.False(ayarlar.Json);
        Assert.Null(ayarlar.EnUzunKac);
    }

    [Fact]
    public void Scan_BayraklarBirlikteVerilebilir()
    {
        TaramaArgumanlari ayarlar = Basarili(["scan", "src", "--json", "--top", "5"]);

        Assert.True(ayarlar.Json);
        Assert.Equal(5, ayarlar.EnUzunKac);
    }

    [Fact]
    public void Scan_BayraklarinSirasiOnemsiz()
    {
        Assert.Equal(Basarili(["scan", "src", "--json", "--top", "3"]), Basarili(["scan", "src", "--top", "3", "--json"]));
    }

    [Theory]
    [InlineData]
    [InlineData("bilinmeyen", "src")]
    [InlineData("scan")]
    [InlineData("scan", "--json")]
    [InlineData("scan", "src", "--top")]
    [InlineData("scan", "src", "--top", "sifir")]
    [InlineData("scan", "src", "--top", "0")]
    [InlineData("scan", "src", "--top", "-2")]
    [InlineData("scan", "src", "--yanlis")]
    public void YanlisKullanim_HataDoner(params string[] argumanlar)
    {
        AyristirmaSonucu sonuc = ArgumanAyristirici.Ayristir(argumanlar);

        Assert.False(sonuc.Basarili);
        Assert.Null(sonuc.Ayarlar);
        Assert.False(string.IsNullOrWhiteSpace(sonuc.Hata));
    }

    [Fact]
    public void YardimMetni_KullanimSatiriniIcerir()
    {
        Assert.Contains("sievert scan <yol>", ArgumanAyristirici.YardimMetni, StringComparison.Ordinal);
    }

    private static TaramaArgumanlari Basarili(string[] argumanlar)
    {
        AyristirmaSonucu sonuc = ArgumanAyristirici.Ayristir(argumanlar);

        Assert.True(sonuc.Basarili, sonuc.Hata);
        return sonuc.Ayarlar!;
    }
}
