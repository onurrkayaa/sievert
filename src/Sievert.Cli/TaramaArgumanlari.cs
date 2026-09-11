namespace Sievert.Cli;

/// <summary>Komut satirindan okunan ayarlar.</summary>
/// <param name="Yol">Taranacak dosya ya da klasor.</param>
/// <param name="Json">Cikti duz JSON olsun mu.</param>
/// <param name="EnUzunKac">--top ile istenen metot sayisi, verilmediyse null.</param>
public sealed record TaramaArgumanlari(string Yol, bool Json, int? EnUzunKac);

/// <summary>Ayristirma sonucu: ya ayarlar ya da kullaniciya gosterilecek bir hata.</summary>
/// <param name="Ayarlar">Basarili ayristirmada dolu olur.</param>
/// <param name="Hata">Basarisiz ayristirmada dolu olur.</param>
public sealed record AyristirmaSonucu(TaramaArgumanlari? Ayarlar, string? Hata)
{
    public bool Basarili => Ayarlar is not null;
}

/// <summary>Komut satiri argumanlarini elle ayristirir, harici paket kullanmiyoruz.</summary>
public static class ArgumanAyristirici
{
    public const string YardimMetni = """
        Kullanim: sievert scan <yol> [--json] [--top N]

          <yol>      taranacak .cs dosyasi ya da klasor
          --json     ciktiyi JSON olarak bas, baska hicbir sey yazma
          --top N    en uzun N metodu ayrica listele
        """;

    public static AyristirmaSonucu Ayristir(string[] argumanlar)
    {
        if (argumanlar.Length == 0)
        {
            return new AyristirmaSonucu(null, "Komut verilmedi.");
        }

        if (argumanlar[0] != "scan")
        {
            return new AyristirmaSonucu(null, $"Bilinmeyen komut: {argumanlar[0]}");
        }

        if (argumanlar.Length < 2 || argumanlar[1].StartsWith("--", StringComparison.Ordinal))
        {
            return new AyristirmaSonucu(null, "scan komutu bir yol bekliyor.");
        }

        string yol = argumanlar[1];
        bool json = false;
        int? enUzunKac = null;

        for (int i = 2; i < argumanlar.Length; i++)
        {
            switch (argumanlar[i])
            {
                case "--json":
                    json = true;
                    break;

                case "--top":
                    if (i + 1 >= argumanlar.Length)
                    {
                        return new AyristirmaSonucu(null, "--top bir sayi bekliyor.");
                    }

                    if (!int.TryParse(argumanlar[i + 1], out int adet) || adet < 1)
                    {
                        return new AyristirmaSonucu(null, $"--top icin gecersiz sayi: {argumanlar[i + 1]}");
                    }

                    enUzunKac = adet;
                    i++;
                    break;

                default:
                    return new AyristirmaSonucu(null, $"Bilinmeyen secenek: {argumanlar[i]}");
            }
        }

        return new AyristirmaSonucu(new TaramaArgumanlari(yol, json, enUzunKac), null);
    }
}
