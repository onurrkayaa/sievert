using System.Globalization;

using Sievert.Data.Metrics;

namespace Sievert.Measure;

/// <summary>
/// Bot commit'lerinin metriklere etkisini olcer: ayni veri, biri bot commit'leri iceren
/// digeri icermeyen iki hesap. Haric tutma karari VERILMIYOR; sadece kararin ne kadar
/// sey degistirecegi olculuyor.
/// </summary>
public static class BotImpact
{
    public static void Report(IReadOnlyList<CommitForMetrics> commits, HashSet<int> bots)
    {
        List<CommitForMetrics> withoutBots = [.. commits.Where(commit => !bots.Contains(commit.Id))];

        Dictionary<int, CommitMetrics> all = new MetricCalculator(MetricOptions.Default)
            .Compute(commits)
            .ToDictionary(metrics => metrics.CommitId);

        Dictionary<int, CommitMetrics> human = new MetricCalculator(MetricOptions.Default)
            .Compute(withoutBots)
            .ToDictionary(metrics => metrics.CommitId);

        Console.WriteLine($"commit                  : {commits.Count}");
        Console.WriteLine($"bot bayrakli            : {bots.Count} (%{Percent(bots.Count, commits.Count)})");
        Console.WriteLine($"bot disi                : {withoutBots.Count}");
        Console.WriteLine();

        int changed = human.Keys.Count(id => Differs(all[id], human[id]));

        Console.WriteLine(
            $"metrikleri degisen bot disi commit: {changed} / {human.Count} "
            + $"(%{Percent(changed, human.Count)})");
        Console.WriteLine();

        Console.WriteLine("| Olcu | Hesap | Min | Medyan | P95 | Max |");
        Console.WriteLine("|---|---|---|---|---|---|");

        Compare("AuthorCommitCount", all, human, metrics => metrics.AuthorCommitCount);
        Compare("PriorChanges", all, human, metrics => metrics.PriorChanges);
        Compare("DistinctAuthorsOnFiles", all, human, metrics => metrics.DistinctAuthorsOnFiles);
    }

    /// <summary>
    /// Karsilastirma yalnizca bot DISI commit'ler uzerinden; bot commit'leri ikinci
    /// hesapta hic yok, onlari karsilastirmanin anlami olmaz.
    /// </summary>
    private static void Compare(
        string metric,
        Dictionary<int, CommitMetrics> all,
        Dictionary<int, CommitMetrics> human,
        Func<CommitMetrics, int> pick)
    {
        Write(metric, "bot dahil", [.. human.Keys.Select(id => (double)pick(all[id]))]);
        Write(metric, "bot haric", [.. human.Keys.Select(id => (double)pick(human[id]))]);
    }

    private static void Write(string metric, string label, List<double> values)
    {
        values.Sort();

        Console.WriteLine(
            $"| {metric} | {label} | {Number(values[0])} | {Number(Quantile(values, 0.50))} "
            + $"| {Number(Quantile(values, 0.95))} | {Number(values[^1])} |");
    }

    private static bool Differs(CommitMetrics left, CommitMetrics right) =>
        left.AuthorCommitCount != right.AuthorCommitCount
        || left.PriorChanges != right.PriorChanges
        || left.DistinctAuthorsOnFiles != right.DistinctAuthorsOnFiles;

    private static double Quantile(List<double> sorted, double fraction)
    {
        int index = (int)Math.Ceiling(fraction * sorted.Count) - 1;

        return sorted[Math.Clamp(index, 0, sorted.Count - 1)];
    }

    private static string Number(double value) => value.ToString("0", CultureInfo.InvariantCulture);

    private static string Percent(int part, int whole) =>
        (100.0 * part / whole).ToString("0.0", CultureInfo.InvariantCulture);
}
