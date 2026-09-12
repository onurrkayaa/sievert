using System.Globalization;

using Sievert.Modeling;

namespace Sievert.Measure;

/// <summary>
/// Uc repo icin lojistik regresyon egitir, olasilik kalitesini olcer ve sonuc dosyalarini
/// yazar. Veriye dokunmuyor: anlik goruntuyu, manifesti ve taban sonucunu ozetleriyle
/// dogrulayip yalnizca okuyor.
/// </summary>
public static class ModelCommand
{
    private const string Package = "Microsoft.ML 5.0.0";

    private const string Trainer = "LbfgsLogisticRegression";

    /// <summary>Adim 1'de olculen ve <c>asama5-bolme.md</c> icinde yazili sayilar.</summary>
    private static readonly Expected[] Repositories =
    [
        new("github.com/app-vnext/polly", 1931, 251, 828, 10),
        new("github.com/jellyfin/jellyfin", 16041, 3761, 6876, 927),
        new("github.com/sharex/sharex", 5943, 884, 2547, 129),
    ];

    /// <summary>Adim 2'de olculen LinesAdded taban degerleri.</summary>
    private static readonly BaselineComparison Baseline = new(
        MicroF1: 0.3754,
        MacroF1: 0.2999,
        MicroPrAuc: 0.2611,
        RepositoryF1: new Dictionary<string, double>(StringComparer.Ordinal)
        {
            ["github.com/app-vnext/polly"] = 0.2667,
            ["github.com/jellyfin/jellyfin"] = 0.4811,
            ["github.com/sharex/sharex"] = 0.1520,
        },
        RepositoryPrAuc: new Dictionary<string, double>(StringComparer.Ordinal)
        {
            ["github.com/app-vnext/polly"] = 0.2610,
            ["github.com/jellyfin/jellyfin"] = 0.4663,
            ["github.com/sharex/sharex"] = 0.0970,
        });

    public static int Run(string[] args)
    {
        string snapshot = args[1];
        string snapshotChecksum = args[2];
        string manifest = args[3];
        string manifestChecksum = args[4];
        string baselineChecksum = args[5];
        string codeCommit = args[6];
        string outputDirectory = args[7];

        IReadOnlyList<SnapshotRow> rows = SnapshotReader.ReadVerified(snapshot, snapshotChecksum);
        IReadOnlyList<SplitEntry> entries = SplitManifestReader.ReadVerified(manifest, manifestChecksum);

        // Taban sonucu okunmuyor ama ozeti dogrulaniyor: karsilastirilan sayilar o
        // dosyadan geliyor ve dosya degismisse karsilastirma gecersiz olur.
        string baselinePath = Path.Combine(Path.GetDirectoryName(baselineChecksum) ?? ".", "baseline-results.json");
        FileChecksum.Verify(baselinePath, baselineChecksum);

        IReadOnlyList<RepositorySplit> repositories = SplitData.Build(rows, entries);

        Console.WriteLine($"anlik goruntu: {FileChecksum.Sha256(snapshot)}");
        Console.WriteLine($"manifest     : {FileChecksum.Sha256(manifest)}");
        Console.WriteLine($"taban sonucu : {FileChecksum.Sha256(baselinePath)}");
        Console.WriteLine($"paket: {Package}, trainer: {Trainer}, tohum: {LogisticRegressionModel.Seed}");
        Console.WriteLine();

        if (!CheckSplit(repositories))
        {
            Console.Error.WriteLine("Bolme sayilari beklenenle ayni degil; model egitilmedi.");

            return 2;
        }

        ModelReport report = ModelRunner.Run(
            repositories,
            FileChecksum.Sha256(snapshot),
            FileChecksum.Sha256(manifest),
            FileChecksum.Sha256(baselinePath),
            codeCommit);

        Print(report);

        Directory.CreateDirectory(outputDirectory);
        Directory.CreateDirectory(Path.Combine(outputDirectory, "models"));

        string json = Path.Combine(outputDirectory, "model-results.json");
        string predictions = Path.Combine(outputDirectory, "model-predictions.csv");

        ModelJson.Write(json, report, Baseline, Package, Trainer);
        PredictionsFile.Write(predictions, repositories, report.Repositories);
        File.WriteAllText(Path.ChangeExtension(json, ".sha256"), FileChecksum.Line(json));
        File.WriteAllText(Path.ChangeExtension(predictions, ".sha256"), FileChecksum.Line(predictions));

        SaveModels(repositories, outputDirectory);

        Console.WriteLine();
        Console.WriteLine($"model-results.json   : {FileChecksum.Sha256(json)}");
        Console.WriteLine($"model-predictions.csv: {FileChecksum.Sha256(predictions)}");
        Console.WriteLine($"tahmin satiri: {File.ReadLines(predictions).Count() - 1}");

        return 0;
    }

    private static void SaveModels(IReadOnlyList<RepositorySplit> repositories, string outputDirectory)
    {
        List<string> lines = [];

        foreach (RepositorySplit repository in repositories)
        {
            string name = Name(repository.Identity);
            string path = Path.Combine(outputDirectory, "models", name + ".zip");

            LogisticRegressionModel.Save(repository, path);
            lines.Add(FileChecksum.Sha256(path) + "  " + name + ".zip");
        }

        File.WriteAllText(
            Path.Combine(outputDirectory, "models", "models.sha256"),
            string.Join('\n', lines) + "\n");
    }

    private static string Name(string identity) => identity[(identity.LastIndexOf('/') + 1)..];

    private static bool CheckSplit(IReadOnlyList<RepositorySplit> repositories)
    {
        Console.WriteLine("== Bolme dogrulamasi ==");

        bool same = repositories.Count == Repositories.Length;
        int train = 0;
        int test = 0;
        int trainPositives = 0;
        int testPositives = 0;

        foreach (RepositorySplit repository in repositories)
        {
            Expected? expected = Array.Find(Repositories, item => item.Identity == repository.Identity);

            bool match = expected is not null
                && repository.Train.Count == expected.TrainRows
                && repository.TrainPositives == expected.TrainPositives
                && repository.Test.Count == expected.TestRows
                && repository.TestPositives == expected.TestPositives;

            same = same && match;
            train += repository.Train.Count;
            test += repository.Test.Count;
            trainPositives += repository.TrainPositives;
            testPositives += repository.TestPositives;

            Console.WriteLine(
                $"  {repository.Identity}: train {repository.TrainPositives} / {repository.Train.Count}, "
                + $"test {repository.TestPositives} / {repository.Test.Count}  {(match ? "ayni" : "FARKLI")}");
        }

        Console.WriteLine(
            $"  toplam train {trainPositives} / {train}, test {testPositives} / {test} "
            + "(beklenen 4896 / 23915 ve 1066 / 10251)");

        return same && train == 23915 && test == 10251 && trainPositives == 4896 && testPositives == 1066;
    }

    private static void Print(ModelReport report)
    {
        foreach (RepositoryModelResult result in report.Repositories)
        {
            Console.WriteLine($"== {result.Identity} ==");
            Console.WriteLine(
                $"  train esigi: {result.TrainThreshold.ToString("F6", CultureInfo.InvariantCulture)} "
                + $"({result.TrainThresholdCandidates} aday)");
            Console.WriteLine($"  train (esik): {Line(result.TrainAtTuned)}");
            Console.WriteLine($"  test  (0,5) : {Line(result.TestAtFixed)}");
            Console.WriteLine($"  test  (esik): {Line(result.TestAtTuned)}");
            Console.WriteLine(
                $"  Brier {Number(result.Calibration.Brier)}, ECE {Number(result.Calibration.Ece)}");
            Console.WriteLine(
                $"  train araligi disinda: {result.TestValuesOutsideTrainRange} deger, "
                + $"{result.TestRowsOutsideTrainRange} satir");

            List<(string Feature, double Weight)> ranked =
            [
                .. result.Coefficients.Weights
                    .Select((weight, index) => (ModelFeatures.Candidates[index], weight))
                    .OrderByDescending(entry => Math.Abs(entry.weight))
            ];

            Console.WriteLine($"  intercept {Number(result.Coefficients.Intercept)}");
            Console.WriteLine("  en buyuk 3 mutlak katsayi:");

            foreach ((string feature, double weight) in ranked.Take(3))
            {
                Console.WriteLine($"    {feature}: {Number(weight)}");
            }

            Console.WriteLine($"  |rho| >= 0,80 cift sayisi: {result.HighCorrelations.Count}");

            foreach (CorrelatedPair pair in result.HighCorrelations)
            {
                Console.WriteLine($"    {pair.First} - {pair.Second}: {Number(pair.Rho)}");
            }

            Console.WriteLine("  kalibrasyon kutulari:");

            foreach (CalibrationBin bin in result.Calibration.Bins)
            {
                Console.WriteLine(
                    $"    [{bin.Lower.ToString("F1", CultureInfo.InvariantCulture)}-"
                    + $"{bin.Upper.ToString("F1", CultureInfo.InvariantCulture)}) "
                    + $"n {bin.Count}, ort tahmin {Number(bin.MeanPrediction)}, "
                    + $"gercek {bin.Positives} / {bin.Count}, fark {Number(bin.Gap)}");
            }

            Console.WriteLine();
        }

        Console.WriteLine("== Toplam ==");
        Console.WriteLine($"  mikro (0,5) : {Counts(report.MicroAtFixed)} {Rates(report.MicroAtFixed)}");
        Console.WriteLine($"  mikro (esik): {Counts(report.MicroAtTuned)} {Rates(report.MicroAtTuned)}");
        Console.WriteLine($"  mikro PR-AUC: {Number(report.MicroPrAuc)}");
        Console.WriteLine($"  makro F1    : {Number(report.MacroF1AtTuned)}");
        Console.WriteLine($"  makro PR-AUC: {Number(report.MacroPrAuc)}");
        Console.WriteLine(
            $"  mikro Brier {Number(report.MicroCalibration.Brier)}, "
            + $"mikro ECE {Number(report.MicroCalibration.Ece)}");
        Console.WriteLine($"  makro Brier {Number(report.MacroBrier)}, makro ECE {Number(report.MacroEce)}");
        Console.WriteLine();

        Console.WriteLine("== LinesAdded tabanina karsi uc kosul ==");
        bool micro = report.MicroAtTuned.F1 > Baseline.MicroF1;
        bool macro = report.MacroF1AtTuned > Baseline.MacroF1;
        bool area = report.MicroPrAuc > Baseline.MicroPrAuc;

        Console.WriteLine($"  1) mikro F1 {Number(report.MicroAtTuned.F1)} > {Baseline.MicroF1}: {(micro ? "GECTI" : "KALDI")}");
        Console.WriteLine($"  2) makro F1 {Number(report.MacroF1AtTuned)} > {Baseline.MacroF1}: {(macro ? "GECTI" : "KALDI")}");
        Console.WriteLine($"  3) mikro PR-AUC {Number(report.MicroPrAuc)} > {Baseline.MicroPrAuc}: {(area ? "GECTI" : "KALDI")}");
        Console.WriteLine($"  gecen kosul: {(micro ? 1 : 0) + (macro ? 1 : 0) + (area ? 1 : 0)} / 3");
    }

    private static string Line(ThresholdedOutcome outcome) =>
        $"{Counts(outcome.Counts)} {Rates(outcome.Counts)} PR-AUC {Number(outcome.PrAuc)}";

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

    private sealed record Expected(
        string Identity,
        int TrainRows,
        int TrainPositives,
        int TestRows,
        int TestPositives);
}
