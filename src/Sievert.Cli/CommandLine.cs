using Sievert.Core.Rules;

namespace Sievert.Cli;

/// <summary>Iki komutun da ortak ayarlari.</summary>
/// <param name="TargetPath">Taranacak dosya ya da klasor.</param>
/// <param name="Json">Cikti duz JSON olsun mu.</param>
public abstract record CommandOptions(string TargetPath, bool Json);

/// <summary>scan komutunun ayarlari.</summary>
/// <param name="TopCount">--top ile istenen metot sayisi, verilmediyse null.</param>
public sealed record ScanOptions(string TargetPath, bool Json, int? TopCount)
    : CommandOptions(TargetPath, Json);

/// <summary>check komutunun ayarlari.</summary>
/// <param name="FailOn">Bu seviyede ya da ustunde bulgu varsa cikis kodu 1 olur.</param>
public sealed record CheckOptions(string TargetPath, bool Json, Severity FailOn)
    : CommandOptions(TargetPath, Json);

/// <summary>Ayristirma sonucu: ya ayarlar ya da kullaniciya gosterilecek bir hata.</summary>
/// <param name="Options">Basarili ayristirmada dolu olur.</param>
/// <param name="Error">Basarisiz ayristirmada dolu olur.</param>
public sealed record ParseResult(CommandOptions? Options, string? Error)
{
    public bool Success => Options is not null;
}

/// <summary>Komut satiri argumanlarini elle ayristirir, harici paket kullanmiyoruz.</summary>
public static class ArgumentParser
{
    /// <summary>--fail-on verilmediginde kullanilan seviye.</summary>
    public const Severity DefaultFailOn = Severity.Warning;

    public const string HelpText = """
        Kullanim: sievert <komut> <yol> [secenekler]

          scan <yol> [--json] [--top N]
            dosyalari okuyup tipleri ve metotlari agac halinde yazar
            --json     ciktiyi JSON olarak bas, baska hicbir sey yazma
            --top N    en uzun N metodu ayrica listele

          check <yol> [--json] [--fail-on <seviye>]
            kurallari calistirip bulgulari teshis karti halinde yazar
            --json              ciktiyi JSON olarak bas, baska hicbir sey yazma
            --fail-on <seviye>  bu seviyede ya da ustunde bulgu varsa cikis
                                kodu 1 olur. info / warning / error,
                                varsayilan warning
        """;

    public static ParseResult Parse(string[] args)
    {
        if (args.Length == 0)
        {
            return new ParseResult(null, "Komut verilmedi.");
        }

        string command = args[0];

        if (command is not ("scan" or "check"))
        {
            return new ParseResult(null, $"Bilinmeyen komut: {command}");
        }

        if (args.Length < 2 || args[1].StartsWith("--", StringComparison.Ordinal))
        {
            return new ParseResult(null, $"{command} komutu bir yol bekliyor.");
        }

        return command == "scan" ? ParseScan(args) : ParseCheck(args);
    }

    private static ParseResult ParseScan(string[] args)
    {
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

        return new ParseResult(new ScanOptions(args[1], json, topCount), null);
    }

    private static ParseResult ParseCheck(string[] args)
    {
        bool json = false;
        Severity failOn = DefaultFailOn;

        for (int i = 2; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--json":
                    json = true;
                    break;

                case "--fail-on":
                    if (i + 1 >= args.Length)
                    {
                        return new ParseResult(null, "--fail-on bir seviye bekliyor.");
                    }

                    if (ParseSeverity(args[i + 1]) is not Severity level)
                    {
                        return new ParseResult(null, $"--fail-on icin gecersiz seviye: {args[i + 1]}");
                    }

                    failOn = level;
                    i++;
                    break;

                default:
                    return new ParseResult(null, $"Bilinmeyen secenek: {args[i]}");
            }
        }

        return new ParseResult(new CheckOptions(args[1], json, failOn), null);
    }

    /// <summary>Seviye adlari enum uyeleriyle ve JSON ciktisiyla ayni yazilsin diye Ingilizce.</summary>
    private static Severity? ParseSeverity(string value) => value switch
    {
        "info" => Severity.Info,
        "warning" => Severity.Warning,
        "error" => Severity.Error,
        _ => null,
    };
}
