using System.Reflection;
using System.Runtime.InteropServices;

using Sievert.Analysis;
using Sievert.Cli;
using Sievert.Core;
using Sievert.Core.Cozumleme;

AyristirmaSonucu sonuc = ArgumanAyristirici.Ayristir(args);

if (sonuc.Ayarlar is null)
{
    if (args.Length == 0)
    {
        string surum = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.0.0";
        KonsolYazici.Yaz(
            Banner.Render(surum, RuntimeInformation.FrameworkDescription)
                .Select(satir => new CiktiSatiri([new CiktiParcasi(satir.Text, satir.IsTitle ? CiktiRengi.Baslik : CiktiRengi.Soluk)]))
                .ToList(),
            KonsolYazici.RenkKullanilsinMi());
        Console.WriteLine();
        Console.WriteLine(ArgumanAyristirici.YardimMetni);
        return 0;
    }

    Console.Error.WriteLine(sonuc.Hata);
    Console.Error.WriteLine();
    Console.Error.WriteLine(ArgumanAyristirici.YardimMetni);
    return 1;
}

TaramaArgumanlari ayarlar = sonuc.Ayarlar;

if (!File.Exists(ayarlar.Yol) && !Directory.Exists(ayarlar.Yol))
{
    Console.Error.WriteLine($"Bulunamadi: {ayarlar.Yol}");
    return 1;
}

IReadOnlyList<string> dosyalar = KaynakDosyaBulucu.Bul(ayarlar.Yol);

if (dosyalar.Count == 0)
{
    Console.Error.WriteLine($"Taranacak .cs dosyasi yok: {ayarlar.Yol}");
    return 1;
}

string kok = TaramaKoku.Bul(ayarlar.Yol);
IReadOnlyList<DosyaAnalizi> analizler = TaramaKoku.YollariGoreliles(
    dosyalar.Select(DosyaCozumleyici.DosyayiCozumle).ToList(),
    kok);

TaramaOzeti ozet = Ozetleyici.Ozetle(analizler);
IReadOnlyList<MetotYeri> enUzunlar = ayarlar.EnUzunKac is int adet
    ? Ozetleyici.EnUzunMetotlar(analizler, adet)
    : [];

if (ayarlar.Json)
{
    Console.Out.WriteLine(JsonBicimlendirici.Bicimlendir(kok, analizler, ozet, enUzunlar));
    return 0;
}

KonsolYazici.Yaz(
    AgacBicimlendirici.Bicimlendir(analizler, ozet, enUzunlar),
    KonsolYazici.RenkKullanilsinMi());

return 0;
