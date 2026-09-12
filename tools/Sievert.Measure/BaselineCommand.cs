using System.Globalization;

using Sievert.Modeling;

namespace Sievert.Measure;

/// <summary>
/// Uc taban cizgisini dondurulmus veri kumesi ve dondurulmus bolme uzerinde calistirip
/// sonuc dosyasini yazar.
///
/// Urunun parcasi degil ve veriye dokunmuyor: iki dosyayi da ozetleriyle dogrulayip
/// yalnizca okuyor. Bolme burada yeniden hesaplanmiyor, manifestten okunuyor.
/// </summary>
public static class BaselineCommand
{
    /// <summary>Adim 1'de olculen ve <c>asama5-bolme.md</c> icinde yazili sayilar.</summary>
    private static readonly Expected[] Repositories =
    [
        new("github.com/app-vnext/polly", 1931, 251, 828, 10),
        new("github.com/jellyfin/jellyfin", 16041, 3761, 6876, 927),
        new("github.com/sharex/sharex", 5943, 884, 2547, 129),
    ];

    public static int Run(
        string snapshotPath,
        string snapshotChecksum,
        string manifestPath,
        string manifestChecksum,
        string codeCommit,
        string outputPath)
    {
        IReadOnlyList<SnapshotRow> rows = SnapshotReader.ReadVerified(snapshotPath, snapshotChecksum);
        IReadOnlyList<SplitEntry> manifest = SplitManifestReader.ReadVerified(manifestPath, manifestChecksum);
        IReadOnlyList<RepositorySplit> repositories = SplitData.Build(rows, manifest);

        Console.WriteLine($"anlik goruntu: {FileChecksum.Sha256(snapshotPath)}");
        Console.WriteLine($"manifest     : {FileChecksum.Sha256(manifestPath)}");
        Console.WriteLine($"kod commit'i : {codeCommit}");
        Console.WriteLine($"metrik sozlesmesi: {BaselineRunner.MetricContractVersion}");
        Console.WriteLine();

        if (!CheckSplit(repositories))
        {
            Console.Error.WriteLine("Bolme sayilari beklenenle ayni degil; sonuc yazilmadi.");

            return 2;
        }

        BaselineReport report = BaselineRunner.Run(
            repositories,
            FileChecksum.Sha256(snapshotPath),
            FileChecksum.Sha256(manifestPath),
            codeCommit);

        Print(report);
        PrintOperatorCheck(repositories, report);
        BaselineJson.Write(outputPath, report);
        File.WriteAllText(Path.ChangeExtension(outputPath, ".sha256"), FileChecksum.Line(outputPath));

        Console.WriteLine();
        Console.WriteLine($"sonuc: {outputPath}");
        Console.WriteLine($"SHA-256: {FileChecksum.Sha256(outputPath)}");

        return 0;
    }

    private static bool CheckSplit(IReadOnlyList<RepositorySplit> repositories)
    {
        Console.WriteLine("== Bolme dogrulamasi ==");

        bool same = repositories.Count == Repositories.Length;

        foreach (RepositorySplit repository in repositories)
        {
            Expected? expected = Array.Find(Repositories, item => item.Identity == repository.Identity);

            bool match = expected is not null
                && repository.Train.Count == expected.TrainRows
                && repository.TrainPositives == expected.TrainPositives
                && repository.Test.Count == expected.TestRows
                && repository.TestPositives == expected.TestPositives;

            same = same && match;

            Console.WriteLine(
                $"  {repository.Identity}: train {repository.TrainPositives} / {repository.Train.Count}, "
                + $"test {repository.TestPositives} / {repository.Test.Count}  {(match ? "ayni" : "FARKLI")}");
        }

        Console.WriteLine();

        return same;
    }

    private static void Print(BaselineReport report)
    {
        Console.WriteLine("== A) Her seye negatif ==");

        foreach (RepositoryOutcome outcome in report.Negative.Repositories)
        {
            Console.WriteLine($"  {outcome.Identity}: {Counts(outcome.Counts)}");
            Console.WriteLine($"    {Rates(outcome.Counts)}  PR-AUC {Number(outcome.PrAuc)} (sabit skor)");
        }

        Console.WriteLine($"  mikro: {Counts(report.Negative.Micro)}");
        Console.WriteLine($"    {Rates(report.Negative.Micro)}  PR-AUC {Number(report.Negative.MicroPrAuc)} (sabit skor)");
        Console.WriteLine($"  makro F1: {Number(report.Negative.MacroF1)}");
        Console.WriteLine();

        Console.WriteLine($"== B) Oranla rastgele ({report.Random.Repeats} tekrar, tohum {report.Random.Seed}) ==");

        foreach (RandomSummary summary in report.Random.Repositories)
        {
            PrintRandom(summary);
        }

        PrintRandom(report.Random.Micro);
        Console.WriteLine($"  makro F1  {Spread(report.Random.MacroF1)}");
        Console.WriteLine();

        Console.WriteLine("== C) LinesAdded esigi ==");

        foreach (ThresholdRepositoryOutcome outcome in report.LinesAdded.Repositories)
        {
            Console.WriteLine($"  {outcome.Identity}: esik {outcome.Threshold} ({outcome.CandidateCount} aday)");
            Console.WriteLine($"    train {Counts(outcome.Train.Counts)}");
            Console.WriteLine($"      {Rates(outcome.Train.Counts)}");
            Console.WriteLine(
                $"      PR-AUC ham {Number(outcome.Train.RawPrAuc)}, esikli {Number(outcome.Train.BinaryPrAuc)}");
            Console.WriteLine($"    test  {Counts(outcome.Test.Counts)}");
            Console.WriteLine($"      {Rates(outcome.Test.Counts)}");
            Console.WriteLine(
                $"      PR-AUC ham {Number(outcome.Test.RawPrAuc)}, esikli {Number(outcome.Test.BinaryPrAuc)}");
        }

        Console.WriteLine($"  mikro: {Counts(report.LinesAdded.Micro)}");
        Console.WriteLine($"    {Rates(report.LinesAdded.Micro)}");
        Console.WriteLine(
            $"    PR-AUC ham {Number(report.LinesAdded.MicroRawPrAuc)}, "
            + $"esikli {Number(report.LinesAdded.MicroBinaryPrAuc)}");
        Console.WriteLine($"  makro F1: {Number(report.LinesAdded.MacroF1)}");
    }

    /// <summary>
    /// ">" ile ">=" ayni tahminleri mi uretiyor. Ana sonucu degistirmiyor; yalnizca
    /// beklenti dosyasindaki yazimla uygulanan protokolun farkini sayiyor.
    /// </summary>
    private static void PrintOperatorCheck(
        IReadOnlyList<RepositorySplit> repositories,
        BaselineReport report)
    {
        Console.WriteLine();
        Console.WriteLine("== Protokol kontrolu: > ile >= ==");

        Dictionary<string, double> thresholds = new(StringComparer.Ordinal);

        foreach (ThresholdRepositoryOutcome outcome in report.LinesAdded.Repositories)
        {
            thresholds[outcome.Identity] = outcome.Threshold;
        }

        int total = 0;

        foreach (RepositorySplit repository in repositories)
        {
            OperatorCheck check = ThresholdOperatorCheck.Compare(repository, thresholds[repository.Identity]);
            total += check.TrainDifferences + check.TestDifferences;

            string greater = check.Greater is double value
                ? value.ToString("F0", CultureInfo.InvariantCulture)
                : "yok (esik egitimin en kucuk degeri)";

            Console.WriteLine($"  {repository.Identity}: >= {check.GreaterOrEqual} ile > {greater}");
            Console.WriteLine(
                $"    train farkli tahmin: {check.TrainDifferences} / {check.TrainRows}");
            Console.WriteLine(
                $"    test  farkli tahmin: {check.TestDifferences} / {check.TestRows}");
        }

        Console.WriteLine($"  toplam fark: {total}");
    }

    private static void PrintRandom(RandomSummary summary)
    {
        string probability = double.IsFinite(summary.Probability)
            ? Number(summary.Probability)
            : "her repo kendi orani";

        Console.WriteLine($"  {summary.Name} (p = {probability})");
        Console.WriteLine($"    precision {Spread(summary.Precision)}");
        Console.WriteLine($"    recall    {Spread(summary.Recall)}");
        Console.WriteLine($"    F1        {Spread(summary.F1)}");
        Console.WriteLine($"    PR-AUC    {Spread(summary.PrAuc)}");
        Console.WriteLine(
            $"    pozitif tahmin ort {Number(summary.PredictedPositives.Mean)}, "
            + $"min {Number(summary.PredictedPositives.Min)}, max {Number(summary.PredictedPositives.Max)}");
    }

    private static string Counts(Confusion counts) =>
        $"TP {counts.TruePositives}, FP {counts.FalsePositives}, "
        + $"FN {counts.FalseNegatives}, TN {counts.TrueNegatives}";

    private static string Rates(Confusion counts) =>
        $"precision {Ratio(counts.Precision, counts.TruePositives, counts.PredictedPositives)}, "
        + $"recall {Ratio(counts.Recall, counts.TruePositives, counts.ActualPositives)}, "
        + $"F1 {Number(counts.F1)}";

    private static string Ratio(double? value, int numerator, int denominator) =>
        value is double number
            ? $"{number.ToString("F4", CultureInfo.InvariantCulture)} ({numerator} / {denominator})"
            : $"N/A ({numerator} / {denominator})";

    private static string Number(double? value) =>
        value is double number && double.IsFinite(number)
            ? number.ToString("F4", CultureInfo.InvariantCulture)
            : "N/A";

    private static string Spread(Distribution spread) =>
        $"ort {Number(spread.Mean)}, p2,5 {Number(spread.Low)}, "
        + $"medyan {Number(spread.Median)}, p97,5 {Number(spread.High)}"
        + (spread.NotAvailable > 0 ? $", N/A {spread.NotAvailable} tekrar" : string.Empty);

    private sealed record Expected(
        string Identity,
        int TrainRows,
        int TrainPositives,
        int TestRows,
        int TestPositives);
}
