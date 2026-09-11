using Sievert.Data.Metrics;

namespace Sievert.Measure;

/// <summary>
/// IsFix'in mevcut tanimini (tam kelime) kok eslesmesiyle karsilastirir. Tanimi
/// DEGISTIRMIYOR, sadece iki sayiyi yan yana koyuyor; karar olcumden sonra verilecek.
/// </summary>
public static class IsFixRecall
{
    /// <summary>
    /// Kok eslesmesinde aranan on ek. Mevcut tanimdaki kelimelerin ayni koku; fark,
    /// kelimenin bu kokle BASLAMASININ yetmesi. Boylece fixes, fixed, fixing, hatayi,
    /// hatasi, hatalari da eslesiyor.
    /// </summary>
    private static readonly string[] Stems =
    [
        "fix", "bug", "hata", "patch", "defect", "error", "crash", "issue", "resolve", "correct",
    ];

    /// <summary>Orneklem her kosuda ayni ciksin diye sabit tohum.</summary>
    private const int Seed = 42;

    public static void Report(IReadOnlyList<CommitForMetrics> commits)
    {
        List<CommitForMetrics> current = [.. commits.Where(commit => FixSubject.Looks(commit.Subject))];
        List<CommitForMetrics> stem = [.. commits.Where(commit => MatchesStem(commit.Subject))];
        List<CommitForMetrics> onlyStem =
            [.. stem.Where(commit => !FixSubject.Looks(commit.Subject))];

        Console.WriteLine($"commit                     : {commits.Count}");
        Console.WriteLine($"mevcut tanim (tam kelime)  : {current.Count} (%{Percent(current.Count, commits.Count)})");
        Console.WriteLine($"kok eslesmesi (fix*)       : {stem.Count} (%{Percent(stem.Count, commits.Count)})");
        Console.WriteLine($"sadece kok eslesmesinde    : {onlyStem.Count}");
        Console.WriteLine();

        Write("Sadece kok eslesmesinde yakalananlardan 20 rastgele ornek", Sample(onlyStem));
        Write("Mevcut tanimin yakaladiklarindan 20 rastgele ornek", Sample(current));
    }

    private static void Write(string title, IReadOnlyList<CommitForMetrics> sample)
    {
        Console.WriteLine($"## {title}");
        Console.WriteLine();
        Console.WriteLine("| # | Baslik | Gercekten duzeltme mi (E/H) |");
        Console.WriteLine("|---|---|---|");

        for (int i = 0; i < sample.Count; i++)
        {
            string subject = sample[i].Subject.Replace("|", "\\|", StringComparison.Ordinal);

            Console.WriteLine($"| {i + 1} | {subject} |  |");
        }

        Console.WriteLine();
    }

    private static IReadOnlyList<CommitForMetrics> Sample(IReadOnlyList<CommitForMetrics> source)
    {
        Random random = new(Seed);

        return [.. source.OrderBy(_ => random.Next()).Take(20)];
    }

    private static string Percent(int part, int whole) =>
        (100.0 * part / whole).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>
    /// Baslikta, kelimelerden biri listedeki bir kokle basliyor mu. Kelime siniri yine
    /// harf ve rakam disindaki karakterler; fark sadece tam esitlik yerine on ek aramasi.
    /// </summary>
    private static bool MatchesStem(string subject)
    {
        int start = -1;

        for (int i = 0; i <= subject.Length; i++)
        {
            bool partOfWord = i < subject.Length && char.IsLetterOrDigit(subject[i]);

            if (partOfWord && start < 0)
            {
                start = i;
                continue;
            }

            if (partOfWord || start < 0)
            {
                continue;
            }

            string word = subject[start..i];

            if (Stems.Any(root => word.StartsWith(root, StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            start = -1;
        }

        return false;
    }
}
