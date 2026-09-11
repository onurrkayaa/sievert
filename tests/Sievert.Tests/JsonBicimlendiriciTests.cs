using System.Text.Json;

using Sievert.Analysis;
using Sievert.Cli;
using Sievert.Core.Cozumleme;

using static Sievert.Tests.OrnekAnaliz;

namespace Sievert.Tests;

public class JsonBicimlendiriciTests
{
    [Fact]
    public void TipTuru_CSharpAnahtarKelimesiyleYazilir()
    {
        DosyaAnalizi analiz = new(
            "a.cs",
            [
                new SievertTip("Bir", TipTuru.Sinif, 1, []),
                new SievertTip("Iki", TipTuru.Record, 2, []),
                new SievertTip("Uc", TipTuru.Struct, 3, []),
                new SievertTip("Dort", TipTuru.Interface, 4, []),
            ],
            10,
            []);

        JsonElement[] tipler = Cozumle(analiz).GetProperty("dosyalar")[0].GetProperty("tipler").EnumerateArray().ToArray();

        Assert.Equal(
            ["class", "record", "struct", "interface"],
            tipler.Select(tip => tip.GetProperty("turu").GetString()!).ToArray());
    }

    [Fact]
    public void TipTuru_AgacCiktisiyleAyniKelimeyiKullanir()
    {
        DosyaAnalizi[] analizler = [Dosya("a.cs", Tip("Bir", Metot("Calis")))];

        string agacSatiri = AgacBicimlendirici
            .Bicimlendir(analizler, Ozetleyici.Ozetle(analizler), [])
            .Select(satir => satir.DuzMetin)
            .Single(satir => satir.Contains("Bir ", StringComparison.Ordinal));

        string jsonTuru = Cozumle(analizler[0])
            .GetProperty("dosyalar")[0].GetProperty("tipler")[0].GetProperty("turu").GetString()!;

        Assert.Contains($"({jsonTuru})", agacSatiri, StringComparison.Ordinal);
    }

    [Fact]
    public void AyristirmaHatalari_EkrandakiSinirdanFazlaOlsaDaHepsiYazilir()
    {
        string[] hatalar = Enumerable
            .Range(1, AgacBicimlendirici.EkrandaGosterilecekHataSayisi + 4)
            .Select(sira => $"Satir {sira}: ; bekleniyor")
            .ToArray();

        DosyaAnalizi analiz = BozukDosya("bozuk.cs", hatalar);

        JsonElement jsonHatalari = Cozumle(analiz).GetProperty("dosyalar")[0].GetProperty("ayristirmaHatalari");
        int ekrandakiHataSatiri = AgacBicimlendirici
            .Bicimlendir([analiz], Ozetleyici.Ozetle([analiz]), [])
            .Count(satir => satir.DuzMetin.Contains("! Satir", StringComparison.Ordinal));

        Assert.Equal(hatalar.Length, jsonHatalari.GetArrayLength());
        Assert.Equal(AgacBicimlendirici.EkrandaGosterilecekHataSayisi, ekrandakiHataSatiri);
    }

    [Fact]
    public void TaramaKoku_MutlakYolOlarakTekSeferYazilir()
    {
        JsonElement json = Cozumle(Dosya("Ic/Bir.cs", Tip("Bir", Metot("Calis"))), "/repo/kok");

        Assert.Equal("/repo/kok", json.GetProperty("taramaKoku").GetString());
        Assert.Equal("Ic/Bir.cs", json.GetProperty("dosyalar")[0].GetProperty("dosyaYolu").GetString());
    }

    [Fact]
    public void EnUzunMetotlar_IstenmezseAnahtarHicYazilmaz()
    {
        DosyaAnalizi analiz = Dosya("a.cs", Tip("Bir", Metot("Calis")));

        Assert.False(Cozumle(analiz).TryGetProperty("enUzunMetotlar", out _));

        string json = JsonBicimlendirici.Bicimlendir(
            "/kok",
            [analiz],
            Ozetleyici.Ozetle([analiz]),
            Ozetleyici.EnUzunMetotlar([analiz], 1));

        Assert.Equal(1, JsonDocument.Parse(json).RootElement.GetProperty("enUzunMetotlar").GetArrayLength());
    }

    private static JsonElement Cozumle(DosyaAnalizi analiz, string kok = "/kok") =>
        JsonDocument.Parse(JsonBicimlendirici.Bicimlendir(kok, [analiz], Ozetleyici.Ozetle([analiz]), [])).RootElement;
}
