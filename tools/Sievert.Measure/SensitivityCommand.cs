using System.Globalization;
using System.Text;
using System.Text.Json;

using Sievert.Modeling;

namespace Sievert.Measure;

/// <summary>
/// Adim 4 duyarlilik deneyleri. Ana modeli, ana bolmeyi ve dondurulmus dosyalari
/// degistirmiyor; her deney ayri bir "ya soyle olsaydi" hesabi.
/// </summary>
public static class SensitivityCommand
{
    private static readonly double[] FlipRates = [0.05, 0.10, 0.20];

    private const int FlipRepeats = 100;

    private const int Seed = 20260912;

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

        string snapshot = Path.Combine(dataDirectory, "commit-metrics.csv");
        string manifest = Path.Combine(dataDirectory, "split-manifest.csv");
        string predictionsPath = Path.Combine(dataDirectory, "model-predictions.csv");

        foreach (string file in (string[])[snapshot, manifest, predictionsPath])
        {
            FileChecksum.Verify(file, Path.ChangeExtension(file, ".sha256"));
        }

        IReadOnlyList<RepositorySplit> repositories = SplitData.Build(
            SnapshotReader.Read(snapshot),
            SplitManifestReader.Read(manifest));

        Dictionary<string, double> probabilities = new(StringComparer.Ordinal);
        Dictionary<string, double> thresholds = new(StringComparer.Ordinal);

        foreach (string line in File.ReadLines(predictionsPath).Skip(1))
        {
            string[] fields = line.Split(',');
            probabilities[fields[0] + " " + fields[1]] = double.Parse(fields[4], CultureInfo.InvariantCulture);
            thresholds[fields[0]] = double.Parse(fields[7], CultureInfo.InvariantCulture);
        }

        using MemoryStream stream = new();
        Utf8JsonWriter writer = new(stream, new JsonWriterOptions { Indented = true });

        writer.WriteStartObject();
        writer.WriteString("snapshotSha256", FileChecksum.Sha256(snapshot));
        writer.WriteString("manifestSha256", FileChecksum.Sha256(manifest));
        writer.WriteString("modelPredictionsSha256", FileChecksum.Sha256(predictionsPath));
        writer.WriteString("codeCommit", codeCommit);
        writer.WriteString("note", "ana model ve ana bolme degistirilmedi");

        Bots(writer, repositories, probabilities, thresholds);
        Maturity(writer, repositories, probabilities, thresholds);
        CsharpCoverage(writer, repositories, probabilities, thresholds);
        Ranges(writer, repositories, probabilities, thresholds);
        SyntheticNoise(writer, repositories);

        writer.WriteEndObject();
        writer.Flush();

        string json = Encoding.UTF8.GetString(stream.ToArray()) + "\n";
        string output = Path.Combine(dataDirectory, "sensitivity-results.json");
        File.WriteAllText(output, json, new UTF8Encoding(false));
        File.WriteAllText(Path.ChangeExtension(output, ".sha256"), FileChecksum.Line(output));

        Console.WriteLine();
        Console.WriteLine($"sensitivity-results.json: {FileChecksum.Sha256(output)}");

        return 0;
    }

    private static List<(SnapshotRow Row, double Probability)> Pair(
        IReadOnlyList<SnapshotRow> rows,
        Dictionary<string, double> probabilities)
    {
        List<(SnapshotRow, double)> paired = new(rows.Count);

        foreach (SnapshotRow row in rows)
        {
            paired.Add((row, probabilities[row.RepositoryIdentity + " " + row.Sha]));
        }

        return paired;
    }

    // --- 4.1 Bot ---

    private static void Bots(
        Utf8JsonWriter writer,
        IReadOnlyList<RepositorySplit> repositories,
        Dictionary<string, double> probabilities,
        Dictionary<string, double> thresholds)
    {
        Console.WriteLine("== 4.1 Bot duyarliligi ==");
        writer.WriteStartArray("botSensitivity");

        foreach (RepositorySplit repository in repositories)
        {
            double threshold = thresholds[repository.Identity];
            // sievert:disable SV004 Train bellekte bir liste, veritabani sorgusu degil
            int trainBots = repository.Train.Count(row => row.IsBot);
            // sievert:disable SV004 Test bellekte bir liste, veritabani sorgusu degil
            int testBots = repository.Test.Count(row => row.IsBot);

            IReadOnlyList<SnapshotRow> humanTrain = [.. repository.Train.Where(row => !row.IsBot)];
            IReadOnlyList<SnapshotRow> humanTest = [.. repository.Test.Where(row => !row.IsBot)];

            SubsetOutcome a = Sensitivity.Evaluate("A", Pair(repository.Test, probabilities), threshold);
            SubsetOutcome b = Sensitivity.Evaluate("B", Pair(humanTest, probabilities), threshold);

            SubsetOutcome? c = humanTrain.Count > 0 && humanTest.Count > 0
                ? Sensitivity.Retrain("C", repository.Identity, humanTrain, humanTest, ModelFeatures.Candidates)
                : null;

            Console.WriteLine(
                $"  {repository.Identity}: train bot {trainBots} / {repository.Train.Count}, "
                + $"test bot {testBots} / {repository.Test.Count}");
            Print("    A", a);
            Print("    B", b);

            if (c is not null)
            {
                Print("    C", c);
            }

            writer.WriteStartObject();
            writer.WriteString("repository", repository.Identity);
            writer.WriteNumber("trainBots", trainBots);
            writer.WriteNumber("trainRows", repository.Train.Count);
            writer.WriteNumber("testBots", testBots);
            writer.WriteNumber("testRows", repository.Test.Count);
            WriteOutcome(writer, a);
            WriteOutcome(writer, b);

            if (c is not null)
            {
                WriteOutcome(writer, c);
            }

            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }

    // --- 4.2 Sag sansur ---

    private static void Maturity(
        Utf8JsonWriter writer,
        IReadOnlyList<RepositorySplit> repositories,
        Dictionary<string, double> probabilities,
        Dictionary<string, double> thresholds)
    {
        Console.WriteLine();
        Console.WriteLine("== 4.2 Sag sansur (90 gun) ==");
        writer.WriteStartArray("maturitySensitivity");

        foreach (RepositorySplit repository in repositories)
        {
            double threshold = thresholds[repository.Identity];

            // sievert:disable SV004 iki liste de bellekte, veritabani sorgusu degil
            DateTimeOffset last = repository.Train
                .Concat(repository.Test)
                .Max(row => row.AuthorDateUtc);

            List<(SnapshotRow, double)> mature = [];

            foreach ((SnapshotRow row, double probability) in Pair(repository.Test, probabilities))
            {
                if (Sensitivity.Maturity(last, row.AuthorDateUtc) >= Sensitivity.MaturityDays)
                {
                    mature.Add((row, probability));
                }
            }

            SubsetOutcome all = Sensitivity.Evaluate("ana", Pair(repository.Test, probabilities), threshold);
            SubsetOutcome ripe = Sensitivity.Evaluate("olgun", mature, threshold);

            Console.WriteLine($"  {repository.Identity}: cikarilan {all.Rows - ripe.Rows} satir, "
                + $"{all.Positives - ripe.Positives} pozitif");
            Print("    ana  ", all);
            Print("    olgun", ripe);

            writer.WriteStartObject();
            writer.WriteString("repository", repository.Identity);
            writer.WriteNumber("removedRows", all.Rows - ripe.Rows);
            writer.WriteNumber("removedPositives", all.Positives - ripe.Positives);
            WriteOutcome(writer, all);
            WriteOutcome(writer, ripe);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }

    // --- 4.3 C# uygunlugu ---

    private static void CsharpCoverage(
        Utf8JsonWriter writer,
        IReadOnlyList<RepositorySplit> repositories,
        Dictionary<string, double> probabilities,
        Dictionary<string, double> thresholds)
    {
        Console.WriteLine();
        Console.WriteLine("== 4.3 CsFilesChanged ==");
        writer.WriteStartArray("csharpCoverage");

        IReadOnlyList<string> ablated =
            [.. ModelFeatures.Candidates.Where(name => name != "CsFilesChanged")];

        foreach (RepositorySplit repository in repositories)
        {
            double threshold = thresholds[repository.Identity];

            IReadOnlyList<SnapshotRow> all = [.. repository.Train, .. repository.Test];
            // sievert:disable SV004 all bellekte birlestirilmis bir liste, veritabani sorgusu degil
            int zero = all.Count(row => row.CsFilesChanged == 0);
            // sievert:disable SV004 ayni bellekteki liste uzerinde sayim
            int zeroPositive = all.Count(row => row.CsFilesChanged == 0 && row.IsBugIntroducing);
            int nonZero = all.Count - zero;
            // sievert:disable SV004 ayni bellekteki liste uzerinde sayim
            int nonZeroPositive = all.Count(row => row.CsFilesChanged > 0 && row.IsBugIntroducing);

            IReadOnlyList<SnapshotRow> csTrain = [.. repository.Train.Where(row => row.CsFilesChanged > 0)];
            IReadOnlyList<SnapshotRow> csTest = [.. repository.Test.Where(row => row.CsFilesChanged > 0)];

            SubsetOutcome a = Sensitivity.Evaluate("A", Pair(repository.Test, probabilities), threshold);
            SubsetOutcome b = Sensitivity.Retrain(
                "B", repository.Identity, repository.Train, repository.Test, ablated);
            SubsetOutcome c = Sensitivity.Retrain(
                "C", repository.Identity, csTrain, csTest, ModelFeatures.Candidates);

            ThresholdChoice baseline = LinesAddedBaseline.Choose(csTrain);
            ThresholdOutcome baselineOutcome = LinesAddedBaseline.Apply(csTest, baseline.Threshold);

            Console.WriteLine(
                $"  {repository.Identity}: CsFilesChanged=0 {zeroPositive} / {zero} pozitif, "
                + $">0 {nonZeroPositive} / {nonZero}");
            Print("    A", a);
            Print("    B", b);
            Print("    C", c);
            Console.WriteLine(
                $"    C tabani (LinesAdded >= {baseline.Threshold}): F1 {Number(baselineOutcome.Counts.F1)}, "
                + $"PR-AUC {Number(baselineOutcome.RawPrAuc)}");

            writer.WriteStartObject();
            writer.WriteString("repository", repository.Identity);
            writer.WriteNumber("csZeroRows", zero);
            writer.WriteNumber("csZeroPositives", zeroPositive);
            writer.WriteNumber("csNonZeroRows", nonZero);
            writer.WriteNumber("csNonZeroPositives", nonZeroPositive);
            WriteOutcome(writer, a);
            WriteOutcome(writer, b);
            WriteOutcome(writer, c);
            writer.WriteStartObject("csharpSubsetBaseline");
            writer.WriteNumber("threshold", baseline.Threshold);
            WriteNumberOrNull(writer, "f1", baselineOutcome.Counts.F1);
            WriteNumberOrNull(writer, "prAuc", baselineOutcome.RawPrAuc);
            writer.WriteNumber("tp", baselineOutcome.Counts.TruePositives);
            writer.WriteNumber("fp", baselineOutcome.Counts.FalsePositives);
            writer.WriteNumber("fn", baselineOutcome.Counts.FalseNegatives);
            writer.WriteNumber("tn", baselineOutcome.Counts.TrueNegatives);
            writer.WriteEndObject();
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }

    // --- 4.4 Train araligi disi ---

    private static void Ranges(
        Utf8JsonWriter writer,
        IReadOnlyList<RepositorySplit> repositories,
        Dictionary<string, double> probabilities,
        Dictionary<string, double> thresholds)
    {
        Console.WriteLine();
        Console.WriteLine("== 4.4 Train araligi disi degerler ==");
        writer.WriteStartArray("outOfRange");

        foreach (RepositorySplit repository in repositories)
        {
            double threshold = thresholds[repository.Identity];
            FeatureScaler scaler = FeatureScaler.Fit(repository.Identity, repository.Train);
            IReadOnlyList<RangeCount> counts = Sensitivity.Ranges(scaler, repository.Test);

            List<(SnapshotRow, double)> outside = [];
            List<(SnapshotRow, double)> inside = [];

            foreach ((SnapshotRow row, double probability) in Pair(repository.Test, probabilities))
            {
                if (scaler.OutsideTrainRangeRows([row]) > 0)
                {
                    outside.Add((row, probability));
                }
                else
                {
                    inside.Add((row, probability));
                }
            }

            SubsetOutcome outsideOutcome = Sensitivity.Evaluate("aralik-disi", outside, threshold);
            SubsetOutcome insideOutcome = Sensitivity.Evaluate("aralik-ici", inside, threshold);

            Console.WriteLine($"  {repository.Identity}: aralik disi {outside.Count} / {repository.Test.Count} satir");
            Print("    disi", outsideOutcome);
            Print("    ici ", insideOutcome);

            writer.WriteStartObject();
            writer.WriteString("repository", repository.Identity);
            writer.WriteNumber("testRows", repository.Test.Count);
            writer.WriteNumber("rowsOutside", outside.Count);
            writer.WriteNumber("rowsInside", inside.Count);
            writer.WriteStartArray("features");

            foreach (RangeCount count in counts)
            {
                writer.WriteStartObject();
                writer.WriteString("feature", count.Feature);
                writer.WriteNumber("belowMinimum", count.BelowMinimum);
                writer.WriteNumber("aboveMaximum", count.AboveMaximum);
                writer.WriteNumber("total", count.BelowMinimum + count.AboveMaximum);
                writer.WriteNumber(
                    "percentOfTest",
                    Math.Round((count.BelowMinimum + count.AboveMaximum) * 100.0 / repository.Test.Count, 4));
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
            WriteOutcome(writer, outsideOutcome);
            WriteOutcome(writer, insideOutcome);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }

    // --- 4.6 Sentetik gurultu ---

    private static void SyntheticNoise(Utf8JsonWriter writer, IReadOnlyList<RepositorySplit> repositories)
    {
        Console.WriteLine();
        Console.WriteLine($"== 4.6 Sentetik etiket gurultusu ({FlipRepeats} tekrar) ==");
        writer.WriteStartArray("syntheticNoise");

        foreach (RepositorySplit repository in repositories)
        {
            foreach (double rate in FlipRates)
            {
                List<double?> f1 = [];
                List<double?> area = [];
                List<double?> brier = [];
                List<double?> ece = [];
                List<double?> chosen = [];
                List<double?> positives = [];

                for (int repeat = 0; repeat < FlipRepeats; repeat++)
                {
                    Random random = new(Seed + repeat);
                    IReadOnlyList<SnapshotRow> train = Sensitivity.Flip(repository.Train, rate, random);
                    IReadOnlyList<SnapshotRow> test = Sensitivity.Flip(repository.Test, rate, random);

                    SubsetOutcome outcome = Sensitivity.Retrain(
                        "sentetik", repository.Identity, train, test, ModelFeatures.Candidates);

                    f1.Add(outcome.Counts.F1);
                    area.Add(outcome.PrAuc);
                    brier.Add(outcome.Brier);
                    ece.Add(outcome.Ece);
                    chosen.Add(outcome.Threshold);
                    positives.Add(outcome.Positives);
                }

                Console.WriteLine(
                    $"  {repository.Identity} %{rate * 100:F0}: "
                    + $"F1 ort {Number(Distribution.Of(f1).Mean)}, PR-AUC ort {Number(Distribution.Of(area).Mean)}, "
                    + $"sentetik pozitif ort {Number(Distribution.Of(positives).Mean)}");

                writer.WriteStartObject();
                writer.WriteString("repository", repository.Identity);
                writer.WriteNumber("rate", rate);
                writer.WriteNumber("repeats", FlipRepeats);
                writer.WriteNumber("trainCandidates", Sensitivity.Candidates(repository.Train));
                writer.WriteNumber("testCandidates", Sensitivity.Candidates(repository.Test));
                WriteDistribution(writer, "f1", Distribution.Of(f1));
                WriteDistribution(writer, "prAuc", Distribution.Of(area));
                WriteDistribution(writer, "brier", Distribution.Of(brier));
                WriteDistribution(writer, "ece", Distribution.Of(ece));
                WriteDistribution(writer, "threshold", Distribution.Of(chosen));
                WriteDistribution(writer, "syntheticTestPositives", Distribution.Of(positives));
                writer.WriteEndObject();
            }
        }

        writer.WriteEndArray();
    }

    private static void Print(string label, SubsetOutcome outcome) =>
        Console.WriteLine(
            $"{label}: N {outcome.Rows}, pozitif {outcome.Positives}, esik {Number(outcome.Threshold)}, "
            + $"TP {outcome.Counts.TruePositives} FP {outcome.Counts.FalsePositives} "
            + $"FN {outcome.Counts.FalseNegatives} TN {outcome.Counts.TrueNegatives}, "
            + $"P {Number(outcome.Counts.Precision)} R {Number(outcome.Counts.Recall)} "
            + $"F1 {Number(outcome.Counts.F1)}, PR-AUC {Number(outcome.PrAuc)}, "
            + $"Brier {Number(outcome.Brier)}, ECE {Number(outcome.Ece)}");

    private static void WriteOutcome(Utf8JsonWriter writer, SubsetOutcome outcome)
    {
        writer.WriteStartObject(outcome.Name);
        writer.WriteNumber("rows", outcome.Rows);
        writer.WriteNumber("positives", outcome.Positives);
        writer.WriteNumber("threshold", outcome.Threshold);
        writer.WriteNumber("tp", outcome.Counts.TruePositives);
        writer.WriteNumber("fp", outcome.Counts.FalsePositives);
        writer.WriteNumber("fn", outcome.Counts.FalseNegatives);
        writer.WriteNumber("tn", outcome.Counts.TrueNegatives);
        WriteNumberOrNull(writer, "precision", outcome.Counts.Precision);
        WriteNumberOrNull(writer, "recall", outcome.Counts.Recall);
        WriteNumberOrNull(writer, "f1", outcome.Counts.F1);
        WriteNumberOrNull(writer, "prAuc", outcome.PrAuc);
        writer.WriteNumber("brier", outcome.Brier);
        writer.WriteNumber("ece", outcome.Ece);
        writer.WriteNumber("meanPrediction", outcome.MeanPrediction);
        writer.WriteEndObject();
    }

    private static void WriteDistribution(Utf8JsonWriter writer, string name, Distribution spread)
    {
        writer.WriteStartObject(name);
        WriteNumberOrNull(writer, "mean", spread.Mean);
        WriteNumberOrNull(writer, "p2_5", spread.Low);
        WriteNumberOrNull(writer, "median", spread.Median);
        WriteNumberOrNull(writer, "p97_5", spread.High);
        writer.WriteEndObject();
    }

    private static void WriteNumberOrNull(Utf8JsonWriter writer, string name, double? value)
    {
        if (value is double number && double.IsFinite(number))
        {
            writer.WriteNumber(name, number);
        }
        else
        {
            writer.WriteNull(name);
        }
    }

    private static string Number(double? value) =>
        value is double number && double.IsFinite(number)
            ? number.ToString("F4", CultureInfo.InvariantCulture)
            : "N/A";
}
