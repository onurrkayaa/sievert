using System.Globalization;

using Sievert.Modeling;

namespace Sievert.Measure;

/// <summary>
/// Adim 3b: kalibrasyon alt bolmesi, Platt/isotonic olcumu, guvenilirlik diyagramlari ve
/// eslesmis blok bootstrap. Dondurulmus dosyalarin hicbirine yazmiyor.
/// </summary>
public static class CalibrationCommand
{
    private static readonly (string Identity, int ModelFit, int Calibration, int Test)[] Expected =
    [
        ("github.com/app-vnext/polly", 1544, 387, 828),
        ("github.com/jellyfin/jellyfin", 12832, 3209, 6876),
        ("github.com/sharex/sharex", 4754, 1189, 2547),
    ];

    /// <summary>Adim 2'de dondurulan LinesAdded esikleri.</summary>
    private static readonly Dictionary<string, double> BaselineThresholds = new(StringComparer.Ordinal)
    {
        ["github.com/app-vnext/polly"] = 126,
        ["github.com/jellyfin/jellyfin"] = 25,
        ["github.com/sharex/sharex"] = 35,
    };

    public static int Run(string[] args)
    {
        string dataDirectory = args[1];
        string codeCommit = args[2];
        string graphicsDirectory = args[3];

        string snapshot = Path.Combine(dataDirectory, "commit-metrics.csv");
        string manifest = Path.Combine(dataDirectory, "split-manifest.csv");
        string modelResults = Path.Combine(dataDirectory, "model-results.json");
        string modelPredictions = Path.Combine(dataDirectory, "model-predictions.csv");
        string baseline = Path.Combine(dataDirectory, "baseline-results.json");

        foreach (string file in (string[])[snapshot, manifest, modelResults, modelPredictions, baseline])
        {
            FileChecksum.Verify(file, Path.ChangeExtension(file, ".sha256"));
        }

        Console.WriteLine("Dondurulmus dosyalarin ozetleri dogrulandi.");

        IReadOnlyList<SnapshotRow> rows = SnapshotReader.Read(snapshot);
        IReadOnlyList<SplitEntry> entries = SplitManifestReader.Read(manifest);
        IReadOnlyList<RepositorySplit> repositories = SplitData.Build(rows, entries);

        List<CalibrationSplit> splits = [.. repositories.Select(CalibrationSplit.From)];

        if (!CheckSubSplit(splits))
        {
            Console.Error.WriteLine("Alt bolme sayilari beklenenle ayni degil; kalibrasyon calistirilmadi.");

            return 2;
        }

        File.WriteAllText(
            Path.Combine(dataDirectory, "calibration-manifest.csv"),
            CalibrationFiles.RenderManifest(splits),
            new System.Text.UTF8Encoding(false));

        Checksum(Path.Combine(dataDirectory, "calibration-manifest.csv"));

        CalibrationReport report = CalibrationRunner.Run(
            repositories,
            FileChecksum.Sha256(snapshot),
            FileChecksum.Sha256(manifest),
            FileChecksum.Sha256(modelResults),
            codeCommit);

        PrintCalibration(report);

        File.WriteAllText(
            Path.Combine(dataDirectory, "calibration-results.json"),
            CalibrationJson.Render(report),
            new System.Text.UTF8Encoding(false));

        File.WriteAllText(
            Path.Combine(dataDirectory, "calibration-predictions.csv"),
            CalibrationFiles.RenderPredictions(repositories, report.Repositories),
            new System.Text.UTF8Encoding(false));

        Checksum(Path.Combine(dataDirectory, "calibration-results.json"));
        Checksum(Path.Combine(dataDirectory, "calibration-predictions.csv"));

        WriteDiagrams(report, graphicsDirectory);

        BootstrapReport bootstrap = RunBootstrap(repositories, modelPredictions, codeCommit, dataDirectory);
        PrintBootstrap(bootstrap);

        return 0;
    }

    private static bool CheckSubSplit(IReadOnlyList<CalibrationSplit> splits)
    {
        Console.WriteLine();
        Console.WriteLine("== Kalibrasyon alt bolmesi ==");

        bool same = splits.Count == Expected.Length;

        foreach (CalibrationSplit split in splits)
        {
            (string Identity, int ModelFit, int Calibration, int Test) expected =
                Array.Find(Expected, item => item.Identity == split.Identity);

            bool match = expected.Identity is not null
                && split.ModelFit.Count == expected.ModelFit
                && split.Calibration.Count == expected.Calibration
                && split.Test.Count == expected.Test;

            same = same && match && split.CalibrationHasBothClasses;

            Console.WriteLine($"  {split.Identity}  {(match ? "ayni" : "FARKLI")}");
            Console.WriteLine(
                $"    model-fit   : {split.ModelFitPositives} / {split.ModelFit.Count} (beklenen {expected.ModelFit})");
            Console.WriteLine(
                $"    calibration : {split.CalibrationPositives} / {split.Calibration.Count} (beklenen {expected.Calibration})");
            Console.WriteLine(
                $"    test        : {split.TestPositives} / {split.Test.Count} (beklenen {expected.Test})");
            Console.WriteLine(
                $"    sinir: model-fit son {Date(split.ModelFit[^1].AuthorDateUtc)} {split.ModelFit[^1].Sha}");
            Console.WriteLine(
                $"           calibration ilk {Date(split.Calibration[0].AuthorDateUtc)} {split.Calibration[0].Sha}");
            Console.WriteLine(
                $"           calibration son {Date(split.Calibration[^1].AuthorDateUtc)} {split.Calibration[^1].Sha}");
            Console.WriteLine($"           test ilk {Date(split.Test[0].AuthorDateUtc)} {split.Test[0].Sha}");
            Console.WriteLine($"    iki sinif da var: {(split.CalibrationHasBothClasses ? "evet" : "HAYIR")}");

            HashSet<string> fit = [.. split.ModelFit.Select(row => row.Sha)];
            HashSet<string> calibration = [.. split.Calibration.Select(row => row.Sha)];
            HashSet<string> test = [.. split.Test.Select(row => row.Sha)];

            // sievert:disable SV004 uc kume de bellekteki HashSet, veritabani sorgusu degil
            int overlap = fit.Intersect(calibration).Count()
                // sievert:disable SV004 ayni kesisim, bellekteki HashSet uzerinde
                + fit.Intersect(test).Count()
                // sievert:disable SV004 ayni kesisim, bellekteki HashSet uzerinde
                + calibration.Intersect(test).Count();

            int inversions = Inversions(split.ModelFit) + Inversions(split.Calibration) + Inversions(split.Test);

            Console.WriteLine($"    cakisma: {overlap}, ters zaman cifti: {inversions}");
            same = same && overlap == 0 && inversions == 0;
        }

        return same;
    }

    private static int Inversions(IReadOnlyList<SnapshotRow> rows)
    {
        int inversions = 0;

        for (int index = 1; index < rows.Count; index++)
        {
            if (rows[index].AuthorDateUtc < rows[index - 1].AuthorDateUtc)
            {
                inversions++;
            }
        }

        return inversions;
    }

    private static void PrintCalibration(CalibrationReport report)
    {
        Console.WriteLine();
        Console.WriteLine("== Kalibrasyon sonuclari ==");

        foreach (RepositoryCalibration repository in report.Repositories)
        {
            Console.WriteLine($"  {repository.Identity}");
            Console.WriteLine(
                $"    Platt a={Number(repository.PlattSlope)} b={Number(repository.PlattOffset)} "
                + $"({repository.PlattIterations} yineleme), isotonic blok {repository.IsotonicBlocks}");

            foreach (CalibrationOutcome outcome in repository.Outcomes)
            {
                Console.WriteLine(
                    $"    {outcome.Method,-9} Brier {Number(outcome.Calibration.Brier)}, "
                    + $"ECE {Number(outcome.Calibration.Ece)}, PR-AUC {Number(outcome.PrAuc)}, "
                    + $"ort tahmin {Number(outcome.MeanPrediction)}, gercek {outcome.Positives} / {outcome.Count}");
                Console.WriteLine(
                    $"              0,5'te TP {outcome.CountsAtHalf.TruePositives}, FP {outcome.CountsAtHalf.FalsePositives}, "
                    + $"FN {outcome.CountsAtHalf.FalseNegatives}, TN {outcome.CountsAtHalf.TrueNegatives}, "
                    + $"F1 {Number(outcome.CountsAtHalf.F1)}");
            }
        }

        Console.WriteLine("  mikro:");

        foreach (CalibrationOutcome outcome in report.Micro)
        {
            Console.WriteLine(
                $"    {outcome.Method,-9} Brier {Number(outcome.Calibration.Brier)}, "
                + $"ECE {Number(outcome.Calibration.Ece)}, PR-AUC {Number(outcome.PrAuc)}, "
                + $"F1 {Number(outcome.CountsAtHalf.F1)}");
        }

        Console.WriteLine("  makro:");

        foreach (string method in (string[])[CalibrationRunner.Raw, CalibrationRunner.Platt, CalibrationRunner.Isotonic])
        {
            Console.WriteLine(
                $"    {method,-9} Brier {Number(report.MacroBrier[method])}, ECE {Number(report.MacroEce[method])}, "
                + $"PR-AUC {Number(report.MacroPrAuc[method])}, F1 {Number(report.MacroF1[method])}");
        }
    }

    private static void WriteDiagrams(CalibrationReport report, string graphicsDirectory)
    {
        Directory.CreateDirectory(graphicsDirectory);

        foreach (RepositoryCalibration repository in report.Repositories)
        {
            string name = repository.Identity[(repository.Identity.LastIndexOf('/') + 1)..];

            File.WriteAllText(
                Path.Combine(graphicsDirectory, "asama5-kalibrasyon-" + name + ".svg"),
                ReliabilityDiagram.Render(
                    "Guvenilirlik diyagrami - " + repository.Identity,
                    repository.TestRows,
                    repository.Outcomes[0].Calibration,
                    repository.Outcomes[1].Calibration,
                    repository.Outcomes[2].Calibration),
                new System.Text.UTF8Encoding(false));
        }

        File.WriteAllText(
            Path.Combine(graphicsDirectory, "asama5-kalibrasyon-mikro.svg"),
            ReliabilityDiagram.Render(
                "Guvenilirlik diyagrami - uc repo birlikte",
                report.Micro[0].Count,
                report.Micro[0].Calibration,
                report.Micro[1].Calibration,
                report.Micro[2].Calibration),
            new System.Text.UTF8Encoding(false));

        Console.WriteLine();
        Console.WriteLine($"guvenilirlik diyagramlari: {graphicsDirectory}");
    }

    private static BootstrapReport RunBootstrap(
        IReadOnlyList<RepositorySplit> repositories,
        string modelPredictions,
        string codeCommit,
        string dataDirectory)
    {
        Dictionary<string, (double Probability, bool Predicted)> predictions = new(StringComparer.Ordinal);

        foreach (string line in File.ReadLines(modelPredictions).Skip(1))
        {
            string[] fields = line.Split(',');
            predictions[fields[0] + " " + fields[1]] = (
                double.Parse(fields[4], CultureInfo.InvariantCulture),
                fields[6] == "1");
        }

        List<(string, IReadOnlyList<PairedRow>)> paired = [];

        foreach (RepositorySplit repository in repositories)
        {
            double threshold = BaselineThresholds[repository.Identity];
            List<PairedRow> rows = [];

            foreach (SnapshotRow row in repository.Test)
            {
                (double probability, bool predicted) = predictions[row.RepositoryIdentity + " " + row.Sha];
                double linesAdded = ModelFeatures.Value(row, LinesAddedBaseline.Feature);

                rows.Add(new PairedRow(
                    predicted,
                    probability,
                    linesAdded >= threshold,
                    linesAdded,
                    row.IsBugIntroducing));
            }

            paired.Add((repository.Identity, rows));
        }

        BootstrapReport report = BlockBootstrap.Run(paired);

        File.WriteAllText(
            Path.Combine(dataDirectory, "bootstrap-results.json"),
            CalibrationJson.RenderBootstrap(
                report,
                FileChecksum.Sha256(modelPredictions),
                FileChecksum.Sha256(Path.Combine(dataDirectory, "baseline-results.json")),
                codeCommit),
            new System.Text.UTF8Encoding(false));

        Checksum(Path.Combine(dataDirectory, "bootstrap-results.json"));

        return report;
    }

    private static void PrintBootstrap(BootstrapReport report)
    {
        Console.WriteLine();
        Console.WriteLine($"== Bootstrap ({report.Repeats} tekrar, tohum {report.Seed}) ==");

        foreach (BootstrapSummary summary in report.Repositories.Append(report.Micro).Append(report.Macro))
        {
            Console.WriteLine(
                $"  {summary.Name} (blok {(summary.BlockLength > 0 ? summary.BlockLength.ToString(CultureInfo.InvariantCulture) : "-")})");
            Console.WriteLine(
                $"    delta F1     ort {Number(summary.DeltaF1.Mean)}, p2,5 {Number(summary.DeltaF1.Low)}, "
                + $"medyan {Number(summary.DeltaF1.Median)}, p97,5 {Number(summary.DeltaF1.High)}, N/A {summary.NotAvailableF1}");
            Console.WriteLine(
                $"    delta PR-AUC ort {Number(summary.DeltaPrAuc.Mean)}, p2,5 {Number(summary.DeltaPrAuc.Low)}, "
                + $"medyan {Number(summary.DeltaPrAuc.Median)}, p97,5 {Number(summary.DeltaPrAuc.High)}, N/A {summary.NotAvailablePrAuc}");
        }
    }

    private static void Checksum(string path) =>
        File.WriteAllText(Path.ChangeExtension(path, ".sha256"), FileChecksum.Line(path));

    private static string Date(DateTimeOffset value) =>
        value.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);

    private static string Number(double? value) =>
        value is double number && double.IsFinite(number)
            ? number.ToString("F4", CultureInfo.InvariantCulture)
            : "N/A";
}
