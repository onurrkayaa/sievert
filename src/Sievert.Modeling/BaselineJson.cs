using System.Text;
using System.Text.Json;

namespace Sievert.Modeling;

/// <summary>
/// Sonuc dosyasini yazar. Deterministik olmasi sart: alan sirasi sabit, depo sirasi
/// verildigi gibi, sayilar invariant. Ayni girdiden iki kez ayni baytlar cikmazsa
/// dosyanin ozeti bir sey ifade etmez.
///
/// Elle JSON kurulmuyor; <see cref="Utf8JsonWriter"/> kacislari ve sayi bicimini
/// kulturden bagimsiz hallediyor. Alan sirasini yazan taraf belirliyor.
/// </summary>
public static class BaselineJson
{
    public static string Render(BaselineReport report)
    {
        using MemoryStream stream = new();

        using (Utf8JsonWriter writer = new(stream, new JsonWriterOptions { Indented = true }))
        {
            writer.WriteStartObject();

            writer.WriteString("metricContractVersion", report.MetricContractVersion);
            writer.WriteString("snapshotSha256", report.SnapshotSha256);
            writer.WriteString("manifestSha256", report.ManifestSha256);
            writer.WriteString("codeCommit", report.CodeCommit);

            writer.WriteStartArray("split");

            foreach (SplitCounts counts in report.Split)
            {
                writer.WriteStartObject();
                writer.WriteString("repository", counts.Identity);
                writer.WriteNumber("trainRows", counts.TrainRows);
                writer.WriteNumber("trainPositives", counts.TrainPositives);
                writer.WriteNumber("testRows", counts.TestRows);
                writer.WriteNumber("testPositives", counts.TestPositives);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();

            WriteNegative(writer, report.Negative);
            WriteRandom(writer, report.Random);
            WriteThreshold(writer, report.LinesAdded);

            writer.WriteEndObject();
        }

        // BOM yok: ozet degeri BOM'dan etkilenmesin.
        return Encoding.UTF8.GetString(stream.ToArray()) + "\n";
    }

    public static void Write(string path, BaselineReport report) =>
        File.WriteAllText(path, Render(report), new UTF8Encoding(false));

    private static void WriteNegative(Utf8JsonWriter writer, NegativeResult result)
    {
        writer.WriteStartObject("negativeBaseline");
        writer.WriteStartArray("repositories");

        foreach (RepositoryOutcome outcome in result.Repositories)
        {
            writer.WriteStartObject();
            writer.WriteString("repository", outcome.Identity);
            WriteCounts(writer, outcome.Counts);
            WriteNumberOrNull(writer, "prAuc", outcome.PrAuc);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();

        writer.WriteStartObject("micro");
        WriteCounts(writer, result.Micro);
        WriteNumberOrNull(writer, "prAuc", result.MicroPrAuc);
        writer.WriteEndObject();

        WriteNumberOrNull(writer, "macroF1", result.MacroF1);
        writer.WriteEndObject();
    }

    private static void WriteRandom(Utf8JsonWriter writer, RandomBaselineResult result)
    {
        writer.WriteStartObject("randomBaseline");
        writer.WriteNumber("seed", result.Seed);
        writer.WriteNumber("repeats", result.Repeats);
        writer.WriteStartArray("repositories");

        foreach (RandomSummary summary in result.Repositories)
        {
            WriteSummary(writer, summary);
        }

        writer.WriteEndArray();

        writer.WritePropertyName("micro");
        WriteSummary(writer, result.Micro);

        // Makro, her tekrarda depo F1'lerinin basit ortalamasi. Mikro'dan ayri duruyor:
        // mikro buyuk repoyu agirlikliyor, makro uc repoyu esit sayiyor.
        WriteDistribution(writer, "macroF1", result.MacroF1);

        writer.WriteEndObject();
    }

    private static void WriteSummary(Utf8JsonWriter writer, RandomSummary summary)
    {
        writer.WriteStartObject();
        writer.WriteString("name", summary.Name);

        if (double.IsFinite(summary.Probability))
        {
            writer.WriteNumber("probability", summary.Probability);
        }
        else
        {
            // Mikro toplamin tek bir olasiligi yok: her repo kendi oranini kullandi.
            writer.WriteNull("probability");
        }

        WriteDistribution(writer, "precision", summary.Precision);
        WriteDistribution(writer, "recall", summary.Recall);
        WriteDistribution(writer, "f1", summary.F1);
        WriteDistribution(writer, "prAuc", summary.PrAuc);
        WriteDistribution(writer, "predictedPositives", summary.PredictedPositives);

        writer.WriteEndObject();
    }

    private static void WriteDistribution(Utf8JsonWriter writer, string name, Distribution spread)
    {
        writer.WriteStartObject(name);
        WriteNumberOrNull(writer, "mean", Finite(spread.Mean));
        WriteNumberOrNull(writer, "p2_5", Finite(spread.Low));
        WriteNumberOrNull(writer, "median", Finite(spread.Median));
        WriteNumberOrNull(writer, "p97_5", Finite(spread.High));
        WriteNumberOrNull(writer, "min", Finite(spread.Min));
        WriteNumberOrNull(writer, "max", Finite(spread.Max));
        writer.WriteNumber("notAvailable", spread.NotAvailable);
        writer.WriteEndObject();
    }

    private static void WriteThreshold(Utf8JsonWriter writer, ThresholdResult result)
    {
        writer.WriteStartObject("linesAddedBaseline");
        writer.WriteString("feature", LinesAddedBaseline.Feature);
        writer.WriteString("rule", "LinesAdded >= threshold");
        writer.WriteStartArray("repositories");

        foreach (ThresholdRepositoryOutcome outcome in result.Repositories)
        {
            writer.WriteStartObject();
            writer.WriteString("repository", outcome.Identity);
            writer.WriteNumber("threshold", outcome.Threshold);
            writer.WriteNumber("candidateCount", outcome.CandidateCount);

            writer.WriteStartObject("train");
            WriteCounts(writer, outcome.Train.Counts);
            WriteNumberOrNull(writer, "rawPrAuc", outcome.Train.RawPrAuc);
            WriteNumberOrNull(writer, "binaryPrAuc", outcome.Train.BinaryPrAuc);
            writer.WriteEndObject();

            writer.WriteStartObject("test");
            WriteCounts(writer, outcome.Test.Counts);
            WriteNumberOrNull(writer, "rawPrAuc", outcome.Test.RawPrAuc);
            WriteNumberOrNull(writer, "binaryPrAuc", outcome.Test.BinaryPrAuc);
            writer.WriteEndObject();

            writer.WriteEndObject();
        }

        writer.WriteEndArray();

        writer.WriteStartObject("micro");
        WriteCounts(writer, result.Micro);
        WriteNumberOrNull(writer, "rawPrAuc", result.MicroRawPrAuc);
        WriteNumberOrNull(writer, "binaryPrAuc", result.MicroBinaryPrAuc);
        writer.WriteEndObject();

        WriteNumberOrNull(writer, "macroF1", result.MacroF1);
        writer.WriteEndObject();
    }

    private static void WriteCounts(Utf8JsonWriter writer, Confusion counts)
    {
        writer.WriteNumber("tp", counts.TruePositives);
        writer.WriteNumber("fp", counts.FalsePositives);
        writer.WriteNumber("fn", counts.FalseNegatives);
        writer.WriteNumber("tn", counts.TrueNegatives);
        writer.WriteNumber("predictedPositives", counts.PredictedPositives);
        writer.WriteNumber("actualPositives", counts.ActualPositives);
        WriteNumberOrNull(writer, "precision", counts.Precision);
        WriteNumberOrNull(writer, "recall", counts.Recall);
        WriteNumberOrNull(writer, "f1", counts.F1);
    }

    /// <summary>N/A degerler JSON'da null; sessizce 0 yazilmiyor.</summary>
    private static void WriteNumberOrNull(Utf8JsonWriter writer, string name, double? value)
    {
        if (value is double number)
        {
            writer.WriteNumber(name, number);
        }
        else
        {
            writer.WriteNull(name);
        }
    }

    private static double? Finite(double value) => double.IsFinite(value) ? value : null;
}
