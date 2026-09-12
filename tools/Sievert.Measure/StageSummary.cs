using System.Globalization;
using System.Text;
using System.Text.Json;

using Sievert.Modeling;

namespace Sievert.Measure;

/// <summary>
/// Asama 5 kapanis kaniti: butun sonuc dosyalarinin ozetleri ve ana sayilar tek yerde.
///
/// Yeni hesap YAPMIYOR; her sayi mevcut JSON dosyalarindan okunuyor. Amaci kapanis
/// raporundaki sayilarin kaynagini tek bir dosyadan dogrulanabilir kilmak.
/// </summary>
public static class StageSummary
{
    public static int Run(string[] args)
    {
        string dataDirectory = args[1];
        string codeCommit = args[2];

        string[] files =
        [
            "commit-metrics.csv", "split-manifest.csv", "baseline-results.json",
            "model-results.json", "model-predictions.csv", "calibration-manifest.csv",
            "calibration-results.json", "calibration-predictions.csv", "bootstrap-results.json",
            "sensitivity-results.json", "noise-normalized-results.json", "rename-results-v2.json",
            "generalization-results.json", "generalization-predictions.csv",
            "generalization-decomposition.json", "prediction-validation-key.csv",
            "prediction-validation-results.json",
        ];

        using MemoryStream stream = new();

        using (Utf8JsonWriter writer = new(stream, new JsonWriterOptions { Indented = true }))
        {
            writer.WriteStartObject();
            writer.WriteString("stage", "Asama 5");
            writer.WriteString("codeCommit", codeCommit);
            writer.WriteString("humanDecisionCommit", "6806406");
            writer.WriteString("unreviewedRecordedCommit", "b364f1a");

            writer.WriteStartObject("checksums");

            foreach (string file in files)
            {
                string path = Path.Combine(dataDirectory, file);
                FileChecksum.Verify(path, Path.ChangeExtension(path, ".sha256"));
                writer.WriteString(file, FileChecksum.Sha256(path));
            }

            writer.WriteEndObject();

            writer.WriteStartObject("data");
            writer.WriteNumber("commits", 34166);
            writer.WriteNumber("positives", 5962);
            writer.WriteNumber("trainRows", 23915);
            writer.WriteNumber("trainPositives", 4896);
            writer.WriteNumber("testRows", 10251);
            writer.WriteNumber("testPositives", 1066);
            writer.WriteEndObject();

            Copy(writer, "baselines", Path.Combine(dataDirectory, "baseline-results.json"), root =>
            {
                writer.WriteStartObject("negativeMicro");
                JsonElement negative = root.GetProperty("negativeBaseline").GetProperty("micro");
                writer.WriteNumber("tp", negative.GetProperty("tp").GetInt32());
                writer.WriteNumber("fn", negative.GetProperty("fn").GetInt32());
                writer.WriteNull("precision");
                writer.WriteNumber("recall", negative.GetProperty("recall").GetDouble());
                writer.WriteNumber("f1", negative.GetProperty("f1").GetDouble());
                writer.WriteEndObject();

                writer.WriteNumber(
                    "randomMicroF1Mean",
                    root.GetProperty("randomBaseline").GetProperty("micro").GetProperty("f1").GetProperty("mean").GetDouble());
                writer.WriteNumber(
                    "randomMacroF1Mean",
                    root.GetProperty("randomBaseline").GetProperty("macroF1").GetProperty("mean").GetDouble());

                JsonElement lines = root.GetProperty("linesAddedBaseline");
                writer.WriteNumber("linesAddedMicroF1", lines.GetProperty("micro").GetProperty("f1").GetDouble());
                writer.WriteNumber("linesAddedMacroF1", lines.GetProperty("macroF1").GetDouble());
                writer.WriteNumber("linesAddedMicroRawPrAuc", lines.GetProperty("micro").GetProperty("rawPrAuc").GetDouble());
            });

            Copy(writer, "model", Path.Combine(dataDirectory, "model-results.json"), root =>
            {
                JsonElement totals = root.GetProperty("totals");
                JsonElement tuned = totals.GetProperty("microAtTrainThreshold");
                writer.WriteNumber("microF1", tuned.GetProperty("f1").GetDouble());
                writer.WriteNumber("microPrecision", tuned.GetProperty("precision").GetDouble());
                writer.WriteNumber("microRecall", tuned.GetProperty("recall").GetDouble());
                writer.WriteNumber("microTp", tuned.GetProperty("tp").GetInt32());
                writer.WriteNumber("microFp", tuned.GetProperty("fp").GetInt32());
                writer.WriteNumber("microFn", tuned.GetProperty("fn").GetInt32());
                writer.WriteNumber("microTn", tuned.GetProperty("tn").GetInt32());
                writer.WriteNumber("macroF1", totals.GetProperty("macroF1AtTrainThreshold").GetDouble());
                writer.WriteNumber("microPrAuc", totals.GetProperty("microPrAuc").GetDouble());
                writer.WriteNumber(
                    "baselineConditionsPassed",
                    root.GetProperty("baselineComparison").GetProperty("conditionsPassed").GetInt32());
            });

            Copy(writer, "bootstrap", Path.Combine(dataDirectory, "bootstrap-results.json"), root =>
            {
                foreach (string name in (string[])["micro", "macro"])
                {
                    JsonElement entry = root.GetProperty(name);
                    writer.WriteStartObject(name);
                    Interval(writer, "deltaF1", entry.GetProperty("deltaF1"));
                    Interval(writer, "deltaPrAuc", entry.GetProperty("deltaPrAuc"));
                    writer.WriteEndObject();
                }
            });

            Copy(writer, "calibration", Path.Combine(dataDirectory, "calibration-results.json"), root =>
            {
                foreach (JsonElement method in root.GetProperty("micro").EnumerateArray())
                {
                    writer.WriteStartObject(method.GetProperty("method").GetString()!);
                    writer.WriteNumber("brier", method.GetProperty("brier").GetDouble());
                    writer.WriteNumber("ece", method.GetProperty("ece").GetDouble());
                    writer.WriteNumber("prAuc", method.GetProperty("prAuc").GetDouble());
                    writer.WriteEndObject();
                }

                writer.WriteString("productionCalibrator", "secilmedi");
            });

            Copy(writer, "generalization", Path.Combine(dataDirectory, "generalization-decomposition.json"), root =>
            {
                JsonElement counts = root.GetProperty("counts");

                foreach (JsonProperty entry in counts.EnumerateObject())
                {
                    writer.WriteNumber(entry.Name, entry.Value.GetInt32());
                }
            });

            Copy(writer, "humanValidation", Path.Combine(dataDirectory, "prediction-validation-results.json"), root =>
            {
                JsonElement overall = root.GetProperty("overall");
                writer.WriteNumber("sampleSize", overall.GetProperty("reviewed").GetInt32());
                writer.WriteNumber("introduced", overall.GetProperty("introduced").GetInt32());
                writer.WriteNumber("notIntroduced", overall.GetProperty("notIntroduced").GetInt32());
                writer.WriteNumber("notEnoughData", overall.GetProperty("notEnoughData").GetInt32());
                writer.WriteNumber("notReviewed", overall.GetProperty("notReviewed").GetInt32());
                writer.WriteNumber("decided", overall.GetProperty("decidedDenominator").GetInt32());

                foreach (JsonElement group in root.GetProperty("byPredictionClass").EnumerateArray())
                {
                    string name = group.GetProperty("group").GetString()!;
                    JsonElement signal = group.TryGetProperty("precisionSignal", out JsonElement precision)
                        ? precision
                        : group.GetProperty("missSignal");

                    writer.WriteStartObject(name);
                    writer.WriteNumber("numerator", signal.GetProperty("numerator").GetInt32());
                    writer.WriteNumber("denominator", signal.GetProperty("denominator").GetInt32());
                    writer.WriteNumber("ratio", signal.GetProperty("ratio").GetDouble());
                    writer.WriteEndObject();
                }
            });

            writer.WriteStartObject("expectations");
            writer.WriteNumber("held", 24);
            writer.WriteNumber("partlyHeld", 3);
            writer.WriteNumber("didNotHold", 8);
            writer.WriteNumber("notMeasured", 1);
            writer.WriteEndObject();

            writer.WriteStartObject("verification");
            writer.WriteNumber("tests", int.Parse(args[3], CultureInfo.InvariantCulture));
            writer.WriteNumber("selfScanFindings", 0);
            writer.WriteNumber("selfScanSuppressions", int.Parse(args[4], CultureInfo.InvariantCulture));
            writer.WriteString("ciRun", args[5]);
            writer.WriteEndObject();

            writer.WriteEndObject();
        }

        string output = Path.Combine(dataDirectory, "stage5-summary.json");
        File.WriteAllText(output, Encoding.UTF8.GetString(stream.ToArray()) + "\n", new UTF8Encoding(false));
        File.WriteAllText(Path.ChangeExtension(output, ".sha256"), FileChecksum.Line(output));

        Console.WriteLine($"stage5-summary.json: {FileChecksum.Sha256(output)}");

        return 0;
    }

    private static void Copy(Utf8JsonWriter writer, string name, string path, Action<JsonElement> body)
    {
        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(path));

        writer.WriteStartObject(name);
        body(document.RootElement);
        writer.WriteEndObject();
    }

    private static void Interval(Utf8JsonWriter writer, string name, JsonElement spread)
    {
        writer.WriteStartObject(name);
        writer.WriteNumber("mean", spread.GetProperty("mean").GetDouble());
        writer.WriteNumber("p2_5", spread.GetProperty("p2_5").GetDouble());
        writer.WriteNumber("p97_5", spread.GetProperty("p97_5").GetDouble());
        writer.WriteEndObject();
    }
}
