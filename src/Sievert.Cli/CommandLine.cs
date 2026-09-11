using Sievert.Analysis;
using Sievert.Core.Rules;

namespace Sievert.Cli;

/// <summary>Iki komutun da ortak ayarlari.</summary>
/// <param name="TargetPath">Taranacak dosya ya da klasor.</param>
/// <param name="Json">Cikti duz JSON olsun mu.</param>
/// <param name="Exclude">--exclude ile verilen kaliplar. Verilmediyse bos.</param>
/// <param name="ConfigPath">--config ile verilen yapilandirma yolu. Verilmediyse null.</param>
public abstract record CommandOptions(
    string TargetPath,
    bool Json,
    IReadOnlyList<GlobPattern> Exclude,
    string? ConfigPath);

/// <summary>scan komutunun ayarlari.</summary>
/// <param name="TopCount">--top ile istenen metot sayisi, verilmediyse null.</param>
public sealed record ScanOptions(
    string TargetPath,
    bool Json,
    IReadOnlyList<GlobPattern> Exclude,
    string? ConfigPath,
    int? TopCount)
    : CommandOptions(TargetPath, Json, Exclude, ConfigPath);

/// <summary>check komutunun ayarlari.</summary>
/// <param name="FailOn">Bu seviyede ya da ustunde bulgu varsa cikis kodu 1 olur.</param>
public sealed record CheckOptions(
    string TargetPath,
    bool Json,
    IReadOnlyList<GlobPattern> Exclude,
    string? ConfigPath,
    Severity FailOn)
    : CommandOptions(TargetPath, Json, Exclude, ConfigPath);

/// <summary>mine komutunun ayarlari.</summary>
/// <param name="OutputPath">--out ile verilen dosya yolu. Verilmediyse null, yani sadece ozet basilir.</param>
/// <param name="Since">--since ile verilen tarih. Verilmediyse null.</param>
/// <param name="MaxCommits">--max-commits ile verilen sinir. Verilmediyse null.</param>
/// <param name="Database">--db verildi mi, yani cikti veritabanina da yazilacak mi.</param>
/// <param name="Rewrite">--overwrite verildi mi, yani deponun eski kayitlari silinecek mi.</param>
public sealed record MineOptions(
    string TargetPath,
    string? OutputPath,
    DateTimeOffset? Since,
    int? MaxCommits,
    bool Database,
    bool Rewrite)
    : CommandOptions(TargetPath, OutputPath is not null, [], null);

/// <summary>metrics komutunun ayarlari.</summary>
/// <param name="TargetPath">Depo adi ya da kimligi. Yol degil, veritabanindaki kayit.</param>
/// <param name="OutputPath">--out ile verilen dosya yolu; dagilim ozeti oraya yaziliyor.</param>
public sealed record MetricsOptions(string TargetPath, string? OutputPath)
    : CommandOptions(TargetPath, OutputPath is not null, [], null);

/// <summary>label komutunun ayarlari.</summary>
/// <param name="TargetPath">Depo adi ya da kimligi. Yol degil, veritabanindaki kayit.</param>
/// <param name="OutputPath">--out ile verilen dosya yolu; etiketleme ozeti oraya yaziliyor.</param>
public sealed record LabelOptions(string TargetPath, string? OutputPath)
    : CommandOptions(TargetPath, OutputPath is not null, [], null);

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

          scan <yol> [--json] [--top N] [--exclude <kalip>]
            dosyalari okuyup tipleri ve metotlari agac halinde yazar
            --json     ciktiyi JSON olarak bas, baska hicbir sey yazma
            --top N    en uzun N metodu ayrica listele

          check <yol> [--json] [--fail-on <seviye>] [--exclude <kalip>]
            kurallari calistirip bulgulari teshis karti halinde yazar
            --json              ciktiyi JSON olarak bas, baska hicbir sey yazma
            --fail-on <seviye>  bu seviyede ya da ustunde bulgu varsa cikis
                                kodu 1 olur. info / warning / error,
                                varsayilan warning

          mine <repo-yolu> [--out <dosya>] [--db] [--overwrite]
                           [--since <tarih>] [--max-commits N]
            git tarihini yurur, commit basina veriyi cikarir
            --out <dosya>       tam veriyi bu dosyaya JSONL yazar: her satir
                                bir commit. Verilmezse sadece ozet basilir.
                                scan ve check'teki --json bayragiyla
                                karistirilmasin, bu bir dosya yolu bekliyor
            --db                veriyi PostgreSQL'e de yazar. Baglanti dizesi
                                SIEVERT_DB ortam degiskeninden ya da
                                appsettings.json'dan okunur, koda yazilmaz
            --overwrite         --db ile birlikte: deponun mevcut kayitlarini
                                silip bastan yazar. Verilmezse zaten kayitli
                                commit'ler atlanir
            --since <tarih>     bu tarihten onceki commit'leri okuma.
                                ISO bicimi, ornegin 2025-01-01
            --max-commits N     en fazla N commit oku (en yeniden eskiye)

          metrics <repo-adi> [--out <dosya>]
            veritabanindaki ham veriden commit olculerini hesaplar ve yazar.
            Git'e gitmez, once mine --db ile veri yazilmis olmali
            --out <dosya>       her olcunun min/medyan/p95/max dagilimini
                                bu dosyaya JSON yazar

          label <repo-adi> [--out <dosya>]
            SZZ ile hangi commit'in hata getirdigini etiketler. Duzeltme
            commit'lerinin degistirdigi satirlari git blame ile en son
            kimin yazdigina bakar. Once mine --db ve metrics calismis olmali
            --out <dosya>       etiketleme ozetini bu dosyaya JSON yazar

        scan ve check icin gecerli:
          --config <yol>      yapilandirma dosyasi. Verilmezse taranan kokteki
                              sievert.json okunur, o da yoksa varsayilanlar calisir
          --exclude <kalip>   bu kaliba uyan dosyalari tarama. Birden fazla kez
                              verilebilir. * bir yol parcasi icinde, ** sifir ya
                              da daha fazla yol parcasi eslestirir. Ornek:
                              --exclude 'samples/**' --exclude '**/*.Designer.cs'

        bin, obj, .git ve node_modules klasorlerinin icine hicbir zaman girilmiyor;
        bunlar icin --exclude yazmaya gerek yok ve ozetteki dislanan sayisina da
        girmiyorlar.
        """;

    public static ParseResult Parse(string[] args)
    {
        if (args.Length == 0)
        {
            return new ParseResult(null, "Komut verilmedi.");
        }

        string command = args[0];

        if (command is not ("scan" or "check" or "mine" or "metrics" or "label"))
        {
            return new ParseResult(null, $"Bilinmeyen komut: {command}");
        }

        if (args.Length < 2 || args[1].StartsWith("--", StringComparison.Ordinal))
        {
            return new ParseResult(null, $"{command} komutu bir yol bekliyor.");
        }

        return command switch
        {
            "scan" => ParseScan(args),
            "check" => ParseCheck(args),
            "mine" => ParseMine(args),
            "metrics" => ParseMetrics(args),
            _ => ParseLabel(args),
        };
    }

    private static ParseResult ParseScan(string[] args)
    {
        bool json = false;
        int? topCount = null;
        List<GlobPattern> exclude = [];
        string? configPath = null;

        for (int i = 2; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--json":
                    json = true;
                    break;

                case "--exclude":
                    if (ReadExclude(args, ref i, exclude) is string scanError)
                    {
                        return new ParseResult(null, scanError);
                    }

                    break;

                case "--config":
                    if (i + 1 >= args.Length)
                    {
                        return new ParseResult(null, "--config bir yol bekliyor.");
                    }

                    configPath = args[++i];
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

        return new ParseResult(new ScanOptions(args[1], json, exclude, configPath, topCount), null);
    }

    private static ParseResult ParseCheck(string[] args)
    {
        bool json = false;
        Severity failOn = DefaultFailOn;
        List<GlobPattern> exclude = [];
        string? configPath = null;

        for (int i = 2; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--json":
                    json = true;
                    break;

                case "--exclude":
                    if (ReadExclude(args, ref i, exclude) is string checkError)
                    {
                        return new ParseResult(null, checkError);
                    }

                    break;

                case "--config":
                    if (i + 1 >= args.Length)
                    {
                        return new ParseResult(null, "--config bir yol bekliyor.");
                    }

                    configPath = args[++i];
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

        return new ParseResult(new CheckOptions(args[1], json, exclude, configPath, failOn), null);
    }

    private static ParseResult ParseMine(string[] args)
    {
        string? outputPath = null;
        DateTimeOffset? since = null;
        int? maxCommits = null;
        bool database = false;
        bool rewrite = false;

        for (int i = 2; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--out":
                    // Bayragin adi bilerek --json degil: scan ve check'te --json bir bayrak
                    // ve cikti ekrana gidiyor. Ayni adin iki anlami olsaydi bu komutlari
                    // birlikte cagiran betikler sessizce yanlis is yapardi.
                    if (i + 1 >= args.Length)
                    {
                        return new ParseResult(null, "--out bir dosya yolu bekliyor.");
                    }

                    outputPath = args[++i];
                    break;

                case "--db":
                    database = true;
                    break;

                case "--overwrite":
                    rewrite = true;
                    break;

                case "--since":
                    if (i + 1 >= args.Length)
                    {
                        return new ParseResult(null, "--since bir tarih bekliyor.");
                    }

                    if (ParseDate(args[i + 1]) is not DateTimeOffset date)
                    {
                        return new ParseResult(null, $"--since icin gecersiz tarih: {args[i + 1]}");
                    }

                    since = date;
                    i++;
                    break;

                case "--max-commits":
                    if (i + 1 >= args.Length)
                    {
                        return new ParseResult(null, "--max-commits bir sayi bekliyor.");
                    }

                    if (!int.TryParse(args[i + 1], out int count) || count < 1)
                    {
                        return new ParseResult(null, $"--max-commits icin gecersiz sayi: {args[i + 1]}");
                    }

                    maxCommits = count;
                    i++;
                    break;

                default:
                    return new ParseResult(null, $"Bilinmeyen secenek: {args[i]}");
            }
        }

        if (rewrite && !database)
        {
            // Sessizce yok saymak yaniltici olurdu: --overwrite yazan biri bir seyin
            // silinip yeniden yazilmasini bekliyor.
            return new ParseResult(null, "--overwrite sadece --db ile birlikte anlamli.");
        }

        return new ParseResult(
            new MineOptions(args[1], outputPath, since, maxCommits, database, rewrite),
            null);
    }

    /// <summary>
    /// metrics ve label ayni secenegi aliyor: sadece --out. Iki ayri dongu yazmak yerine
    /// ortak ayristirici.
    /// </summary>
    private static ParseResult ParseOutOnly(string[] args, Func<string, string?, CommandOptions> build)
    {
        string? outputPath = null;

        for (int i = 2; i < args.Length; i++)
        {
            if (args[i] != "--out")
            {
                return new ParseResult(null, $"Bilinmeyen secenek: {args[i]}");
            }

            if (i + 1 >= args.Length)
            {
                return new ParseResult(null, "--out bir dosya yolu bekliyor.");
            }

            outputPath = args[++i];
        }

        return new ParseResult(build(args[1], outputPath), null);
    }

    private static ParseResult ParseLabel(string[] args) =>
        ParseOutOnly(args, (target, output) => new LabelOptions(target, output));

    private static ParseResult ParseMetrics(string[] args) =>
        ParseOutOnly(args, (target, output) => new MetricsOptions(target, output));

    /// <summary>
    /// Saat dilimi yazilmamis bir tarih UTC sayiliyor. Yerel saate gore yorumlasaydim
    /// ayni komut iki makinede farkli commit kumesi okurdu.
    /// </summary>
    private static DateTimeOffset? ParseDate(string value) =>
        DateTimeOffset.TryParse(
            value,
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal,
            out DateTimeOffset parsed)
            ? parsed
            : null;

    /// <summary>
    /// Bir --exclude kalibini okuyup listeye ekler. Sorun varsa hata metnini doner, yoksa null.
    /// Gecersiz kalip sessizce atlanmiyor: kullanim hatasi sayilip cikis kodu 2 uretiyor,
    /// cunku yanlis yazilmis bir kalip hicbir dosyayi elemez ve bu fark edilmeden gecerdi.
    /// </summary>
    private static string? ReadExclude(string[] args, ref int index, List<GlobPattern> exclude)
    {
        if (index + 1 >= args.Length)
        {
            return "--exclude bir kalip bekliyor.";
        }

        string text = args[index + 1];

        if (GlobPattern.TryParse(text) is not GlobPattern pattern)
        {
            return $"--exclude icin gecersiz kalip: {text}";
        }

        exclude.Add(pattern);
        index++;
        return null;
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
