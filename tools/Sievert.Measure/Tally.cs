using System.Globalization;
using System.Text.RegularExpressions;

namespace Sievert.Measure;

/// <summary>
/// Malzeme dosyasindaki "Karar:" satirlarini sayar. **Siniflandirma yapmaz**, yalnizca
/// yazilmis olani okur; bos birakilmis bir karari doldurmaz ve tahmin etmez.
///
/// Iki kumenin karar sozlugu ayri, cunku ayni harf ikisinde zit anlam tasiyordu.
/// Etiketli satirlar: E / H / Belirsiz. Etiketsiz satirlar: KACIRDI / DOGRU-RET /
/// BELIRSIZ. Ayristirici her satiri kendi kumesinin sozluguyle okuyor.
///
/// Bos karar varsa oran hesaplanmaz; kural KUME BAZINDA isliyor, yani bir kumedeki
/// eksik satir digerinin oranini engellemiyor.
/// </summary>
public static partial class Tally
{
    public static void Report(string materialPath)
    {
        List<Row> rows = Read(materialPath);

        if (rows.Count == 0)
        {
            Console.WriteLine("Malzeme dosyasinda satir bulunamadi.");
            return;
        }

        int empty = rows.Count(row => row.Decision is null);

        Console.WriteLine($"satir           : {rows.Count}");
        Console.WriteLine($"karar dolu      : {rows.Count - empty}");
        Console.WriteLine($"karar bos       : {empty}");
        Console.WriteLine();

        WriteByRepository(rows);

        WriteSet(
            "Etiketli satirlar - dogru suclama orani",
            [.. rows.Where(row => row.Labelled)],
            "E (suclama dogru)",
            "H (suclama yanlis)",
            "Belirsiz");

        WriteSet(
            "Etiketsiz satirlar - kacirma orani",
            [.. rows.Where(row => !row.Labelled)],
            "KACIRDI (suclanmaliydi)",
            "DOGRU-RET (suclamamak dogru)",
            "BELIRSIZ");

        Console.WriteLine("Iki oran birlestirilmedi: paydalari farkli kumeler.");
    }

    private static void WriteByRepository(List<Row> rows)
    {
        Console.WriteLine("Repo kirilimi (birinci / ikinci / belirsiz / bos):");

        foreach (IGrouping<string, Row> group in rows
            .GroupBy(row => row.Repository)
            .OrderBy(group => group.Key, StringComparer.Ordinal))
        {
            Console.WriteLine("  " + Summary(group.Key.PadRight(10), group));
        }

        Console.WriteLine();
    }

    /// <summary>Bir kumenin karar dagilimini tek satirda yazar.</summary>
    private static string Summary(string label, IEnumerable<Row> rows)
    {
        List<Row> all = [.. rows];

        return $"{label} satir {all.Count}, "
            + $"{all.Count(row => row.Decision == Decision.First)} / "
            + $"{all.Count(row => row.Decision == Decision.Second)} / "
            + $"{all.Count(row => row.Decision == Decision.Unclear)} / "
            + $"{all.Count(row => row.Decision is null)}";
    }

    /// <summary>
    /// Tek bir kume icin pay ve paydayi AYRI yazar. Belirsizler paydadan cikariliyor ve
    /// kac tane cikarildigi yaziliyor. Bos karar varsa yalnizca BU kumenin orani
    /// hesaplanmiyor.
    /// </summary>
    private static void WriteSet(
        string title,
        List<Row> rows,
        string firstLabel,
        string secondLabel,
        string unclearLabel)
    {
        int first = rows.Count(row => row.Decision == Decision.First);
        int second = rows.Count(row => row.Decision == Decision.Second);
        int unclear = rows.Count(row => row.Decision == Decision.Unclear);
        int empty = rows.Count(row => row.Decision is null);

        Console.WriteLine($"{title}:");
        Console.WriteLine($"  {"satir",-36}: {rows.Count}");
        Console.WriteLine($"  {firstLabel,-36}: {first}");
        Console.WriteLine($"  {secondLabel,-36}: {second}");
        Console.WriteLine($"  {unclearLabel + " (paydadan cikarildi)",-36}: {unclear}");
        Console.WriteLine($"  {"bos",-36}: {empty}");

        if (empty > 0)
        {
            Console.WriteLine($"  {"oran",-36}: hesaplanmadi, eksik: {empty} satir");
            Console.WriteLine();

            return;
        }

        int denominator = first + second;

        Console.WriteLine($"  {"pay",-36}: {first}");
        Console.WriteLine($"  {"payda",-36}: {denominator}");

        Console.WriteLine(denominator == 0
            ? $"  {"oran",-36}: hesaplanamadi, payda sifir"
            : $"  {"oran",-36}: {first}/{denominator} = "
              + $"%{(100.0 * first / denominator).ToString("0.0", CultureInfo.InvariantCulture)}");

        Console.WriteLine();
    }

    private static List<Row> Read(string path)
    {
        List<Row> rows = [];
        string repository = "?";
        bool labelled = true;
        bool inRow = false;

        foreach (string line in File.ReadLines(path))
        {
            if (HeaderPattern().Match(line) is { Success: true } header)
            {
                repository = header.Groups[1].Value.Trim();
                labelled = header.Groups[2].Value.Trim() == "EVET";
                inRow = true;
                continue;
            }

            if (!inRow || !line.StartsWith("Karar:", StringComparison.Ordinal))
            {
                continue;
            }

            rows.Add(new Row(repository, labelled, Parse(line["Karar:".Length..].Trim(), labelled)));
            inRow = false;
        }

        return rows;
    }

    /// <summary>
    /// Yazilan karari kendi kumesinin sozluguyle okur. Tanimadigi bir metni sessizce bos
    /// saymiyor, uyariyor: yanlis kumenin sozluguyle yazilmis bir karar boylece gorunur
    /// oluyor.
    /// </summary>
    private static Decision? Parse(string text, bool labelled)
    {
        if (text.Length == 0)
        {
            return null;
        }

        string value = text.ToUpperInvariant().Replace('İ', 'I').Replace('Ç', 'C').Replace('Ğ', 'G');

        Decision? decision = labelled
            ? value switch
            {
                "E" => Decision.First,
                "H" => Decision.Second,
                "BELIRSIZ" => Decision.Unclear,
                _ => null,
            }
            : value switch
            {
                "KACIRDI" => Decision.First,
                "DOGRU-RET" => Decision.Second,
                "BELIRSIZ" => Decision.Unclear,
                _ => null,
            };

        if (decision is null)
        {
            Console.WriteLine(
                $"  UYARI: {(labelled ? "etiketli" : "etiketsiz")} kumede taninmayan karar metni, "
                + $"bos sayildi: \"{text}\"");
        }

        return decision;
    }

    [GeneratedRegex(@"^### Satir \d+ / \d+\s+\[([^\]]+)\]\s+\[etiketli: ([^\]]+)\]")]
    private static partial Regex HeaderPattern();

    /// <summary>
    /// Kumeye gore anlami degisen uc karar. Birinci deger etiketli kumede "suclama dogru",
    /// etiketsiz kumede "arac kacirdi" demek; iki kumede de oranin payi bu deger.
    /// </summary>
    private enum Decision
    {
        First,
        Second,
        Unclear,
    }

    private sealed record Row(string Repository, bool Labelled, Decision? Decision);
}
