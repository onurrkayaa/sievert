namespace Sievert.Cli;

/// <summary>Komut satirindan okunan ayarlar.</summary>
/// <param name="TargetPath">Taranacak dosya ya da klasor.</param>
/// <param name="Json">Cikti duz JSON olsun mu.</param>
/// <param name="TopCount">--top ile istenen metot sayisi, verilmediyse null.</param>
public sealed record ScanOptions(string TargetPath, bool Json, int? TopCount);

/// <summary>Ayristirma sonucu: ya ayarlar ya da kullaniciya gosterilecek bir hata.</summary>
/// <param name="Options">Basarili ayristirmada dolu olur.</param>
/// <param name="Error">Basarisiz ayristirmada dolu olur.</param>
public sealed record ParseResult(ScanOptions? Options, string? Error)
{
    public bool Success => Options is not null;
}

/// <summary>Komut satiri argumanlarini elle ayristirir, harici paket kullanmiyoruz.</summary>
public static class ArgumentParser
{
    public const string HelpText = """
        Kullanim: sievert scan <yol> [--json] [--top N]

          <yol>      taranacak .cs dosyasi ya da klasor
          --json     ciktiyi JSON olarak bas, baska hicbir sey yazma
          --top N    en uzun N metodu ayrica listele
        """;

    public static ParseResult Parse(string[] args)
    {
        if (args.Length == 0)
        {
            return new ParseResult(null, "Komut verilmedi.");
        }

        if (args[0] != "scan")
        {
            return new ParseResult(null, $"Bilinmeyen komut: {args[0]}");
        }

        if (args.Length < 2 || args[1].StartsWith("--", StringComparison.Ordinal))
        {
            return new ParseResult(null, "scan komutu bir yol bekliyor.");
        }

        string targetPath = args[1];
        bool json = false;
        int? topCount = null;

        for (int i = 2; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--json":
                    json = true;
                    break;

                case "--top":
                    if (i + 1 >= args.Length)
                    {
                        return new ParseResult(null, "--top bir sayi bekliyor.");
                    }

                    if (!int.TryParse(args[i + 1], out int count) || count < 1)
                    {
                        return new ParseResult(null, $"--top icin gecersiz sayi: {args[i + 1]}");
                    }

                    topCount = count;
                    i++;
                    break;

                default:
                    return new ParseResult(null, $"Bilinmeyen secenek: {args[i]}");
            }
        }

        return new ParseResult(new ScanOptions(targetPath, json, topCount), null);
    }
}
