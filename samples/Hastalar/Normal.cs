// Test verisi. Hicbir projeye dahil degil, derlenmesi gerekmiyor.
using System.Threading.Tasks;

namespace Hastane;

public interface IKayitDefteri
{
    Task<int> HastaSayisiniGetirAsync();
}

public partial class HastaServisi : IKayitDefteri
{
    private readonly string _baglantiMetni;

    public HastaServisi(string baglantiMetni)
    {
        _baglantiMetni = baglantiMetni;
    }

    public string BaglantiMetni => _baglantiMetni;

    public async Task<int> HastaSayisiniGetirAsync()
    {
        await Task.Delay(10);
        return 42;
    }

    public string AdiniBicimlendir(string ad, string soyad)
    {
        string Temizle(string deger) => deger.Trim();

        return $"{Temizle(ad)} {Temizle(soyad)}";
    }

    public class Randevu
    {
        public string Kod() => "R-1";

        public async Task IptalEtAsync()
        {
            await Task.CompletedTask;
        }
    }
}

public partial class HastaServisi
{
    public bool Aktif() => true;
}

public record Tani(string Kod, string Aciklama)
{
    public string Ozet() => $"{Kod}: {Aciklama}";
}

public struct Olcum
{
    public double Deger;

    public double Yuzde(double toplam) => toplam == 0 ? 0 : Deger / toplam * 100;
}
