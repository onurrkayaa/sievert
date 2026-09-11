using Sievert.Analysis;

namespace Sievert.Tests;

public class KaynakDosyaBulucuTests : IDisposable
{
    private readonly string _kok = Path.Combine(Path.GetTempPath(), "sievert-test-" + Guid.NewGuid().ToString("N"));

    public KaynakDosyaBulucuTests()
    {
        Dosya("Bir.cs");
        Dosya("Okuma.txt");
        Dosya(Path.Combine("Ic", "Iki.cs"));
        Dosya(Path.Combine("Ic", "Daha", "Uc.cs"));
        Dosya(Path.Combine("bin", "Derlenmis.cs"));
        Dosya(Path.Combine("obj", "Ara.cs"));
        Dosya(Path.Combine(".git", "Kanca.cs"));
        Dosya(Path.Combine("node_modules", "paket", "Sasirtici.cs"));
    }

    public void Dispose()
    {
        Directory.Delete(_kok, recursive: true);
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void Klasor_AltKlasorleriDeTarar()
    {
        string[] bulunanlar = Adlar(KaynakDosyaBulucu.Bul(_kok));

        Assert.Equal(["Bir.cs", "Iki.cs", "Uc.cs"], bulunanlar.Order(StringComparer.Ordinal).ToArray());
    }

    [Theory]
    [InlineData("bin")]
    [InlineData("obj")]
    [InlineData(".git")]
    [InlineData("node_modules")]
    public void Klasor_AtlanacakKlasorlereGirmez(string klasorAdi)
    {
        Assert.True(KaynakDosyaBulucu.AtlanacakKlasorMu(klasorAdi));
        Assert.DoesNotContain(KaynakDosyaBulucu.Bul(_kok), yol => yol.Contains(klasorAdi + Path.DirectorySeparatorChar, StringComparison.Ordinal));
    }

    [Fact]
    public void Klasor_CsOlmayanDosyalariAlmaz()
    {
        Assert.DoesNotContain(KaynakDosyaBulucu.Bul(_kok), yol => yol.EndsWith(".txt", StringComparison.Ordinal));
    }

    [Fact]
    public void Dosya_TekBasinaVerilirse_KendisiDondurulur()
    {
        string yol = Path.Combine(_kok, "Bir.cs");

        Assert.Equal([yol], KaynakDosyaBulucu.Bul(yol));
    }

    [Fact]
    public void Dosya_CsDegilse_BosDoner()
    {
        Assert.Empty(KaynakDosyaBulucu.Bul(Path.Combine(_kok, "Okuma.txt")));
    }

    [Fact]
    public void OlmayanYol_BosDoner()
    {
        Assert.Empty(KaynakDosyaBulucu.Bul(Path.Combine(_kok, "yok", "hicbir.cs")));
    }

    [Fact]
    public void Sonuc_HerZamanAyniSirada()
    {
        Assert.Equal(KaynakDosyaBulucu.Bul(_kok), KaynakDosyaBulucu.Bul(_kok));
    }

    private void Dosya(string goreliYol)
    {
        string tamYol = Path.Combine(_kok, goreliYol);
        Directory.CreateDirectory(Path.GetDirectoryName(tamYol)!);
        File.WriteAllText(tamYol, "// bos");
    }

    private static string[] Adlar(IReadOnlyList<string> yollar) => yollar.Select(Path.GetFileName).ToArray()!;
}
