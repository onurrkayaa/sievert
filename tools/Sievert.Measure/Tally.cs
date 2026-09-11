using System.Globalization;
using System.Text.RegularExpressions;

namespace Sievert.Measure;

/// <summary>
/// Malzeme dosyasindaki "Karar:" satirlarini sayar. **Siniflandirma yapmaz**, yalnizca
/// yazilmis olani okur; bos birakilmis bir karari doldurmaz ve tahmin etmez.
///
/// Bos karar varsa dogruluk orani HESAPLANMAZ. Eksik veriyle hesaplanan bir oran, eksik
/// oldugu unutulunca gercek bir sayi gibi dolasmaya baslar.
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

        WriteCounts("Karar dagilimi", rows);
        WriteByRepository(rows);
        WriteByKind(rows);

        if (empty > 0)
        {
            Console.WriteLine($"Dogruluk orani hesaplanmadi, eksik: {empty} satir.");
            return;
        }

        WriteRatios(rows);
    }

    private static void WriteCounts(string title, List<Row> rows)
    {
        Console.WriteLine($"{title}:");
        Console.WriteLine($"  E        : {rows.Count(row => row.Decision == Decision.Yes)}");
        Console.WriteLine($"  H        : {rows.Count(row => row.Decision == Decision.No)}");
        Console.WriteLine($"  Belirsiz : {rows.Count(row => row.Decision == Decision.Unclear)}");
        Console.WriteLine($"  bos      : {rows.Count(row => row.Decision is null)}");
        Console.WriteLine();
    }

    private static void WriteByRepository(List<Row> rows)
    {
        Console.WriteLine("Repo kirilimi:");

        foreach (IGrouping<string, Row> group in rows
            .GroupBy(row => row.Repository)
            .OrderBy(group => group.Key, StringComparer.Ordinal))
        {
            Console.WriteLine("  " + Summary(group.Key.PadRight(10), group));
        }

        Console.WriteLine();
    }

    private static void WriteByKind(List<Row> rows)
    {
        Console.WriteLine("Etiket kirilimi:");
        Console.WriteLine("  " + Summary("etiketli ", rows.Where(row => row.Labelled)));
        Console.WriteLine("  " + Summary("etiketsiz", rows.Where(row => !row.Labelled)));
        Console.WriteLine();
    }

    /// <summary>
    /// Bir kumenin karar dagilimini tek satirda yazar. Repo ve etiket kirilimi ayni seyi
    /// sayiyordu, tek yere aldim.
    /// </summary>
    private static string Summary(string label, IEnumerable<Row> rows)
    {
        List<Row> all = [.. rows];

        return $"{label} satir {all.Count}, "
            + $"E {all.Count(row => row.Decision == Decision.Yes)}, "
            + $"H {all.Count(row => row.Decision == Decision.No)}, "
            + $"Belirsiz {all.Count(row => row.Decision == Decision.Unclear)}, "
            + $"bos {all.Count(row => row.Decision is null)}";
    }

    /// <summary>
    /// Iki oran AYRI yaziliyor ve birlestirilmiyor. Etiketli satirlardan cikan oran
    /// suclamalarin ne kadarinin dogru oldugunu (precision isareti), etiketsiz
    /// satirlardan cikan oran SZZ'nin ne kadarini kacirdigini (recall isareti)
    /// gosteriyor. Tek sayida birlestirmek ikisini de anlamsizlastirir, cunku paydalari
    /// farkli kumeler.
    ///
    /// Olcut dosyasindaki ters okuma kurali geregi: etiketli satirda E "suclama dogru",
    /// etiketsiz satirda H "suclamamak dogru" demek.
    /// </summary>
    private static void WriteRatios(List<Row> rows)
    {
        WriteRatio(
            "Etiketli satirlar (precision isareti)",
            [.. rows.Where(row => row.Labelled)],
            Decision.Yes,
            "E (suclama dogru)",
            "H (suclama yanlis)");

        WriteRatio(
            "Etiketsiz satirlar (recall isareti)",
            [.. rows.Where(row => !row.Labelled)],
            Decision.No,
            "E (suclanmaliydi, SZZ kacirdi)",
            "H (suclanmamaliydi, SZZ hakli)");

        Console.WriteLine("Iki oran birlestirilmedi: paydalari farkli kumeler.");
    }

    /// <summary>
    /// Tek bir kume icin pay ve paydayi ayri yazar. Belirsizler paydadan cikariliyor ve
    /// kac tane cikarildigi da yaziliyor.
    /// </summary>
    private static void WriteRatio(
        string title,
        List<Row> rows,
        Decision correct,
        string yesLabel,
        string noLabel)
    {
        int yes = rows.Count(row => row.Decision == Decision.Yes);
        int no = rows.Count(row => row.Decision == Decision.No);
        int unclear = rows.Count(row => row.Decision == Decision.Unclear);
        int denominator = yes + no;
        int numerator = correct == Decision.Yes ? yes : no;

        Console.WriteLine($"{title}:");
        Console.WriteLine($"  {yesLabel,-32}: {yes}");
        Console.WriteLine($"  {noLabel,-32}: {no}");
        Console.WriteLine($"  {"Belirsiz (paydadan cikarildi)",-32}: {unclear}");
        Console.WriteLine($"  {"pay",-32}: {numerator}");
        Console.WriteLine($"  {"payda",-32}: {denominator}");

        Console.WriteLine(denominator == 0
            ? $"  {"oran",-32}: hesaplanamadi, payda sifir"
            : $"  {"oran",-32}: {numerator}/{denominator} = "
              + $"%{(100.0 * numerator / denominator).ToString("0.0", CultureInfo.InvariantCulture)}");

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

            rows.Add(new Row(repository, labelled, Parse(line["Karar:".Length..].Trim())));
            inRow = false;
        }

        return rows;
    }

    /// <summary>
    /// Yazilan karari okur. Tanimadigi bir metin gorurse bos saymiyor, hata olarak
    /// yaziyor: sessizce bos saymak, yazilmis ama yanlis yazilmis bir karari kaybeder.
    /// </summary>
    private static Decision? Parse(string text)
    {
        if (text.Length == 0)
        {
            return null;
        }

        return text.ToUpperInvariant() switch
        {
            "E" => Decision.Yes,
            "H" => Decision.No,
            "BELIRSIZ" or "BELİRSİZ" => Decision.Unclear,
            _ => Unknown(text),
        };
    }

    private static Decision? Unknown(string text)
    {
        Console.WriteLine($"  UYARI: taninmayan karar metni, bos sayildi: \"{text}\"");

        return null;
    }

    [GeneratedRegex(@"^### Satir \d+ / \d+\s+\[([^\]]+)\]\s+\[etiketli: ([^\]]+)\]")]
    private static partial Regex HeaderPattern();

    private enum Decision
    {
        Yes,
        No,
        Unclear,
    }

    private sealed record Row(string Repository, bool Labelled, Decision? Decision);
}
