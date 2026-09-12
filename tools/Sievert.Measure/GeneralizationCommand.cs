using System.Globalization;
using System.Text;
using System.Text.Json;

using Sievert.Modeling;

namespace Sievert.Measure;

/// <summary>
/// Adim 5: repo-arasi genelleme. Alti tek kaynakli yon ve uc leave-one-repository-out
/// deneyi. Ana model ve dondurulmus dosyalar degistirilmiyor.
/// </summary>
public static class GeneralizationCommand
{
    /// <summary>Adim 3'un ayni-repo sonuclari; karsilastirma icin.</summary>
    private static readonly Dictionary<string, (double F1, double PrAuc, double Brier, double Ece)> SameRepo =
        new(StringComparer.Ordinal)
        {
            ["github.com/app-vnext/polly"] = (0.2500, 0.3033, 0.0132, 0.0292),
            ["github.com/jellyfin/jellyfin"] = (0.4895, 0.5199, 0.0998, 0.0810),
            ["github.com/sharex/sharex"] = (0.1868, 0.1192, 0.0559, 0.0625),
        };

    public static int Run(string[] args)
    {
        string dataDirectory = args[1];
        string codeCommit = args[2];

        string snapshot = Path.Combine(dataDirectory, "commit-metrics.csv");
        string manifest = Path.Combine(dataDirectory, "split-manifest.csv");

        foreach (string file in (string[])[snapshot, manifest])
        {
            FileChecksum.Verify(file, Path.ChangeExtension(file, ".sha256"));
        }

        IReadOnlyList<RepositorySplit> repositories = SplitData.Build(
            SnapshotReader.Read(snapshot),
            SplitManifestReader.Read(manifest));

        List<TransferResult> results = [];

        Console.WriteLine("== 5.1 Alti yonlu tek kaynak ==");

        foreach (RepositorySplit source in repositories)
        {
            foreach (RepositorySplit target in repositories)
            {
                if (string.Equals(source.Identity, target.Identity, StringComparison.Ordinal))
                {
                    continue;
                }

                TransferResult result = Generalization.Transfer("tek-kaynak", [source], target);
                results.Add(result);
                Print(result);
            }
        }

        Console.WriteLine();
        Console.WriteLine("== 5.2 Leave-one-repository-out ==");

        foreach (RepositorySplit target in repositories)
        {
            List<RepositorySplit> sources =
                [.. repositories.Where(entry => !string.Equals(entry.Identity, target.Identity, StringComparison.Ordinal))];

            TransferResult result = Generalization.Transfer("iki-kaynak", sources, target);
            results.Add(result);
            Print(result);
        }

        Write(dataDirectory, codeCommit, snapshot, manifest, repositories, results);

        return 0;
    }

    private static void Print(TransferResult result)
    {
        (double F1, double PrAuc, double Brier, double Ece) same = SameRepo[result.Target];

        Console.WriteLine($"  {string.Join(" + ", result.Sources)} -> {result.Target}");
        Console.WriteLine(
            $"    kaynak {result.SourcePositives} / {result.SourceRows}, hedef {result.TargetPositives} / {result.TargetRows}, "
            + $"esik {Number(result.SourceThreshold)}");
        Console.WriteLine(
            $"    kaynak esigi: TP {result.AtSourceThreshold.TruePositives} FP {result.AtSourceThreshold.FalsePositives} "
            + $"FN {result.AtSourceThreshold.FalseNegatives} TN {result.AtSourceThreshold.TrueNegatives}, "
            + $"P {Number(result.AtSourceThreshold.Precision)} R {Number(result.AtSourceThreshold.Recall)} "
            + $"F1 {Number(result.AtSourceThreshold.F1)}");
        Console.WriteLine(
            $"    0,5 esigi   : TP {result.AtHalf.TruePositives} FP {result.AtHalf.FalsePositives} "
            + $"FN {result.AtHalf.FalseNegatives} TN {result.AtHalf.TrueNegatives}, F1 {Number(result.AtHalf.F1)}");
        Console.WriteLine(
            $"    PR-AUC {Number(result.PrAuc)}, Brier {Number(result.Brier)}, ECE {Number(result.Ece)}, "
            + $"ort tahmin {Number(result.MeanPrediction)}");
        Console.WriteLine(
            $"    ayni-repoya fark: F1 {Number(result.AtSourceThreshold.F1 - same.F1)}, "
            + $"PR-AUC {Number(result.PrAuc - same.PrAuc)}, Brier {Number(result.Brier - same.Brier)}, "
            + $"ECE {Number(result.Ece - same.Ece)}");

        IReadOnlyList<(string Feature, double Weight)> ranked = Generalization.Ranked(result.Coefficients);
        Console.WriteLine(
            "    ilk uc: " + string.Join(", ", ranked.Take(3).Select(entry => $"{entry.Feature} {Number(entry.Weight)}")));
    }

    private static void Write(
        string dataDirectory,
        string codeCommit,
        string snapshot,
        string manifest,
        IReadOnlyList<RepositorySplit> repositories,
        IReadOnlyList<TransferResult> results)
    {
        using MemoryStream stream = new();

        using (Utf8JsonWriter writer = new(stream, new JsonWriterOptions { Indented = true }))
        {
            writer.WriteStartObject();
            writer.WriteString("snapshotSha256", FileChecksum.Sha256(snapshot));
            writer.WriteString("manifestSha256", FileChecksum.Sha256(manifest));
            writer.WriteString("codeCommit", codeCommit);
            writer.WriteString("note", "repo kimligi oznitelik degil; kaynak repolar dogal satir sayilariyla birlesti");
            writer.WriteStartArray("experiments");

            foreach (TransferResult result in results)
            {
                (double F1, double PrAuc, double Brier, double Ece) same = SameRepo[result.Target];

                writer.WriteStartObject();
                writer.WriteString("experiment", result.Experiment);
                writer.WriteStartArray("sources");

                foreach (string source in result.Sources)
                {
                    writer.WriteStringValue(source);
                }

                writer.WriteEndArray();
                writer.WriteString("target", result.Target);
                writer.WriteNumber("sourceRows", result.SourceRows);
                writer.WriteNumber("sourcePositives", result.SourcePositives);
                writer.WriteNumber("targetRows", result.TargetRows);
                writer.WriteNumber("targetPositives", result.TargetPositives);
                writer.WriteNumber("sourceThreshold", result.SourceThreshold);

                WriteCounts(writer, "atSourceThreshold", result.AtSourceThreshold);
                WriteCounts(writer, "atHalf", result.AtHalf);

                WriteNumberOrNull(writer, "prAuc", result.PrAuc);
                writer.WriteNumber("brier", result.Brier);
                writer.WriteNumber("ece", result.Ece);
                writer.WriteNumber("meanPrediction", result.MeanPrediction);

                writer.WriteStartObject("sameRepositoryComparison");
                writer.WriteNumber("sameRepoF1", same.F1);
                writer.WriteNumber("sameRepoPrAuc", same.PrAuc);
                WriteNumberOrNull(writer, "deltaF1", result.AtSourceThreshold.F1 - same.F1);
                WriteNumberOrNull(writer, "deltaPrAuc", result.PrAuc - same.PrAuc);
                writer.WriteNumber("deltaBrier", result.Brier - same.Brier);
                writer.WriteNumber("deltaEce", result.Ece - same.Ece);
                WriteNumberOrNull(writer, "retainedF1", Generalization.Retained(result.AtSourceThreshold.F1, same.F1));
                WriteNumberOrNull(writer, "retainedPrAuc", Generalization.Retained(result.PrAuc, same.PrAuc));
                writer.WriteEndObject();

                writer.WriteStartObject("coefficients");
                writer.WriteNumber("intercept", result.Coefficients.Intercept);
                writer.WriteStartArray("weights");

                for (int index = 0; index < result.Coefficients.Weights.Count; index++)
                {
                    writer.WriteStartObject();
                    writer.WriteString("feature", ModelFeatures.Candidates[index]);
                    writer.WriteNumber("weight", result.Coefficients.Weights[index]);
                    writer.WriteEndObject();
                }

                writer.WriteEndArray();
                writer.WriteEndObject();
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        string json = Encoding.UTF8.GetString(stream.ToArray()) + "\n";
        string output = Path.Combine(dataDirectory, "generalization-results.json");
        File.WriteAllText(output, json, new UTF8Encoding(false));
        File.WriteAllText(Path.ChangeExtension(output, ".sha256"), FileChecksum.Line(output));

        StringBuilder csv = new();
        csv.Append("Experiment,SourceRepositories,TargetRepository,Sha,ActualLabel,Probability,")
            .Append("PredictionAtSourceThreshold,PredictionAt05,SourceThreshold\n");

        Dictionary<string, RepositorySplit> byIdentity = repositories.ToDictionary(
            repository => repository.Identity,
            repository => repository,
            StringComparer.Ordinal);

        foreach (TransferResult result in results)
        {
            IReadOnlyList<SnapshotRow> test = byIdentity[result.Target].Test;

            for (int index = 0; index < test.Count; index++)
            {
                double probability = result.Probabilities[index];

                csv.Append(result.Experiment).Append(',');
                csv.Append('"').Append(string.Join(';', result.Sources)).Append('"').Append(',');
                csv.Append(result.Target).Append(',');
                csv.Append(test[index].Sha).Append(',');
                csv.Append(test[index].IsBugIntroducing ? '1' : '0').Append(',');
                csv.Append(probability.ToString("R", CultureInfo.InvariantCulture)).Append(',');
                csv.Append(probability >= result.SourceThreshold ? '1' : '0').Append(',');
                csv.Append(probability >= ProbabilityThreshold.Fixed ? '1' : '0').Append(',');
                csv.Append(result.SourceThreshold.ToString("R", CultureInfo.InvariantCulture));
                csv.Append('\n');
            }
        }

        string predictions = Path.Combine(dataDirectory, "generalization-predictions.csv");
        File.WriteAllText(predictions, csv.ToString(), new UTF8Encoding(false));
        File.WriteAllText(Path.ChangeExtension(predictions, ".sha256"), FileChecksum.Line(predictions));

        Console.WriteLine();
        Console.WriteLine($"generalization-results.json  : {FileChecksum.Sha256(output)}");
        Console.WriteLine($"generalization-predictions.csv: {FileChecksum.Sha256(predictions)}");
        Console.WriteLine($"tahmin satiri: {File.ReadLines(predictions).Count() - 1}");
    }

    private static void WriteCounts(Utf8JsonWriter writer, string name, Confusion counts)
    {
        writer.WriteStartObject(name);
        writer.WriteNumber("tp", counts.TruePositives);
        writer.WriteNumber("fp", counts.FalsePositives);
        writer.WriteNumber("fn", counts.FalseNegatives);
        writer.WriteNumber("tn", counts.TrueNegatives);
        WriteNumberOrNull(writer, "precision", counts.Precision);
        WriteNumberOrNull(writer, "recall", counts.Recall);
        WriteNumberOrNull(writer, "f1", counts.F1);
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
