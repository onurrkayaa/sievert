using Sievert.Analysis;
using Sievert.Core.Cozumleme;

using static Sievert.Tests.OrnekAnaliz;

namespace Sievert.Tests;

public class TaramaKokuTests : IDisposable
{
    private readonly string _kok = Path.Combine(Path.GetTempPath(), "sievert-kok-" + Guid.NewGuid().ToString("N"));

    public TaramaKokuTests()
    {
        Directory.CreateDirectory(Path.Combine(_kok, "Ic"));
        File.WriteAllText(Path.Combine(_kok, "Bir.cs"), "// bos");
        File.WriteAllText(Path.Combine(_kok, "Ic", "Iki.cs"), "// bos");
    }

    public void Dispose()
    {
        Directory.Delete(_kok, recursive: true);
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void Klasor_VerilenKlasorunKendisi()
    {
        Assert.Equal(Path.GetFullPath(_kok), TaramaKoku.Bul(_kok));
    }

    [Fact]
    public void Dosya_DosyaninBulunduguKlasor()
    {
        Assert.Equal(Path.GetFullPath(_kok), TaramaKoku.Bul(Path.Combine(_kok, "Bir.cs")));
    }

    [Fact]
    public void Kok_HerZamanMutlakYol()
    {
        Assert.True(Path.IsPathFullyQualified(TaramaKoku.Bul(".")));
    }

    [Fact]
    public void Yollar_KokeGoreGoreliYazilir()
    {
        IReadOnlyList<DosyaAnalizi> goreli = TaramaKoku.YollariGoreliles(
            [Dosya(Path.Combine(_kok, "Bir.cs")), Dosya(Path.Combine(_kok, "Ic", "Iki.cs"))],
            TaramaKoku.Bul(_kok));

        Assert.Equal(["Bir.cs", Path.Combine("Ic", "Iki.cs")], goreli.Select(analiz => analiz.DosyaYolu).ToArray());
    }

    [Fact]
    public void Yollar_TekDosyaTaramasindaSadeceDosyaAdiKalir()
    {
        string yol = Path.Combine(_kok, "Bir.cs");

        IReadOnlyList<DosyaAnalizi> goreli = TaramaKoku.YollariGoreliles([Dosya(yol)], TaramaKoku.Bul(yol));

        Assert.Equal("Bir.cs", goreli.Single().DosyaYolu);
    }

    [Fact]
    public void Yollar_AnalizinDigerAlanlariBozulmaz()
    {
        DosyaAnalizi once = Dosya(Path.Combine(_kok, "Bir.cs"), Tip("Bir", Metot("Calis")));

        DosyaAnalizi sonra = TaramaKoku.YollariGoreliles([once], TaramaKoku.Bul(_kok)).Single();

        Assert.Equal(once.Tipler, sonra.Tipler);
        Assert.Equal(once.ToplamSatirSayisi, sonra.ToplamSatirSayisi);
    }
}
