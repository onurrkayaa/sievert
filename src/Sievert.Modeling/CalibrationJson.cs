using System.Globalization;
using System.Text;
using System.Text.Json;

namespace Sievert.Modeling;

/// <summary>Kalibrasyon ve bootstrap sonuc dosyalari. Alan sirasi sabit, sayilar invariant.</summary>
public static class CalibrationJson
{
    public static string Render(CalibrationReport report)
    {
        using MemoryStream stream = new();

        using (Utf8JsonWriter writer = new(stream, new JsonWriterOptions { Indented = true }))
        {
            writer.WriteStartObject();
            writer.WriteString("contractVersion", report.ContractVersion);
            writer.WriteString("metricContractVersion", BaselineRunner.MetricContractVersion);
            writer.WriteString("snapshotSha256", report.SnapshotSha256);
            writer.WriteString("manifestSha256", report.ManifestSha256);
            writer.WriteString("modelResultsSha256", report.ModelResultsSha256);
            writer.WriteString("codeCommit", report.CodeCommit);
            writer.WriteString("note", "azaltilmis-egitim modeli; Adim 3 tam-train modelinin yerine gecmez");

            writer.WriteStartArray("repositories");

            foreach (RepositoryCalibration repository in report.Repositories)
            {
                writer.WriteStartObject();
                writer.WriteString("repository", repository.Identity);
                writer.WriteNumber("modelFitRows", repository.ModelFitRows);
                writer.WriteNumber("modelFitPositives", repository.ModelFitPositives);
                writer.WriteNumber("calibrationRows", repository.CalibrationRows);
                writer.WriteNumber("calibrationPositives", repository.CalibrationPositives);
                writer.WriteNumber("testRows", repository.TestRows);
                writer.WriteNumber("testPositives", repository.TestPositives);
                writer.WriteNumber("plattSlope", repository.PlattSlope);
                writer.WriteNumber("plattOffset", repository.PlattOffset);
                writer.WriteNumber("plattIterations", repository.PlattIterations);
                writer.WriteNumber("isotonicBlocks", repository.IsotonicBlocks);

                writer.WriteStartArray("methods");

                foreach (CalibrationOutcome outcome in repository.Outcomes)
                {
                    WriteOutcome(writer, outcome);
                }

                writer.WriteEndArray();
                writer.WriteEndObject();
            }

            writer.WriteEndArray();

            writer.WriteStartArray("micro");

            foreach (CalibrationOutcome outcome in report.Micro)
            {
                WriteOutcome(writer, outcome);
            }

            writer.WriteEndArray();

            writer.WriteStartObject("macro");
            WriteMacro(writer, "brier", report.MacroBrier);
            WriteMacro(writer, "ece", report.MacroEce);
            WriteMacro(writer, "prAuc", report.MacroPrAuc);
            WriteMacro(writer, "f1AtHalf", report.MacroF1);
            writer.WriteEndObject();

            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(stream.ToArray()) + "\n";
    }

    public static string RenderBootstrap(
        BootstrapReport report,
        string modelPredictionsSha256,
        string baselineSha256,
        string codeCommit)
    {
        using MemoryStream stream = new();

        using (Utf8JsonWriter writer = new(stream, new JsonWriterOptions { Indented = true }))
        {
            writer.WriteStartObject();
            writer.WriteString("modelPredictionsSha256", modelPredictionsSha256);
            writer.WriteString("baselineSha256", baselineSha256);
            writer.WriteString("codeCommit", codeCommit);
            writer.WriteNumber("seed", report.Seed);
            writer.WriteNumber("repeats", report.Repeats);
            writer.WriteString("method", "paired circular moving-block bootstrap");
            writer.WriteString(
                "scope",
                "model egitim belirsizligini kapsamaz; yalnizca sabit tahminler uzerindeki "
                + "zamansal test ornekleme belirsizligini olcer");

            writer.WriteStartArray("repositories");

            foreach (BootstrapSummary summary in report.Repositories)
            {
                WriteSummary(writer, summary);
            }

            writer.WriteEndArray();

            writer.WritePropertyName("micro");
            WriteSummary(writer, report.Micro);
            writer.WritePropertyName("macro");
            WriteSummary(writer, report.Macro);

            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(stream.ToArray()) + "\n";
    }

    private static void WriteSummary(Utf8JsonWriter writer, BootstrapSummary summary)
    {
        writer.WriteStartObject();
        writer.WriteString("name", summary.Name);

        if (summary.BlockLength > 0)
        {
            writer.WriteNumber("blockLength", summary.BlockLength);
        }
        else
        {
            writer.WriteNull("blockLength");
        }

        writer.WriteNumber("validRepeats", summary.ValidRepeats);
        writer.WriteNumber("notAvailableF1", summary.NotAvailableF1);
        writer.WriteNumber("notAvailablePrAuc", summary.NotAvailablePrAuc);
        WriteDistribution(writer, "deltaF1", summary.DeltaF1);
        WriteDistribution(writer, "deltaPrAuc", summary.DeltaPrAuc);
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

    private static void WriteOutcome(Utf8JsonWriter writer, CalibrationOutcome outcome)
    {
        writer.WriteStartObject();
        writer.WriteString("method", outcome.Method);
        writer.WriteNumber("brier", outcome.Calibration.Brier);
        writer.WriteNumber("ece", outcome.Calibration.Ece);
        writer.WriteNumber("meanPrediction", outcome.MeanPrediction);
        writer.WriteNumber("positives", outcome.Positives);
        writer.WriteNumber("count", outcome.Count);
        WriteNumberOrNull(writer, "prAuc", outcome.PrAuc);

        writer.WriteStartObject("atHalf");
        writer.WriteNumber("tp", outcome.CountsAtHalf.TruePositives);
        writer.WriteNumber("fp", outcome.CountsAtHalf.FalsePositives);
        writer.WriteNumber("fn", outcome.CountsAtHalf.FalseNegatives);
        writer.WriteNumber("tn", outcome.CountsAtHalf.TrueNegatives);
        WriteNumberOrNull(writer, "precision", outcome.CountsAtHalf.Precision);
        WriteNumberOrNull(writer, "recall", outcome.CountsAtHalf.Recall);
        WriteNumberOrNull(writer, "f1", outcome.CountsAtHalf.F1);
        writer.WriteEndObject();

        writer.WriteStartArray("bins");

        foreach (CalibrationBin bin in outcome.Calibration.Bins)
        {
            writer.WriteStartObject();
            writer.WriteNumber("lower", bin.Lower);
            writer.WriteNumber("upper", bin.Upper);
            writer.WriteNumber("count", bin.Count);
            writer.WriteNumber("meanPrediction", bin.MeanPrediction);
            writer.WriteNumber("positives", bin.Positives);
            WriteNumberOrNull(writer, "observedRate", bin.ObservedRate);
            WriteNumberOrNull(writer, "gap", bin.Gap);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
        writer.WriteEndObject();
    }

    private static void WriteMacro(Utf8JsonWriter writer, string name, IReadOnlyDictionary<string, double> values)
    {
        writer.WriteStartObject(name);

        foreach (string method in (string[])[CalibrationRunner.Raw, CalibrationRunner.Platt, CalibrationRunner.Isotonic])
        {
            WriteNumberOrNull(writer, method, values[method]);
        }

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
}

/// <summary>Kalibrasyon alt bolme manifesti ve test tahminleri dosyalari.</summary>
public static class CalibrationFiles
{
    public const string ManifestHeader = "RepositoryIdentity,Sha,Partition,AuthorDateUtc";

    public const string PredictionsHeader =
        "RepositoryIdentity,Sha,AuthorDateUtc,ActualLabel,RawProbability,PlattProbability,IsotonicProbability";

    public static string RenderManifest(IReadOnlyList<CalibrationSplit> splits)
    {
        StringBuilder text = new();
        text.Append(ManifestHeader).Append('\n');

        foreach (CalibrationSplit split in splits)
        {
            Write(text, split.Identity, split.ModelFit, CalibrationSplit.ModelFitName);
            Write(text, split.Identity, split.Calibration, CalibrationSplit.CalibrationName);
            Write(text, split.Identity, split.Test, CalibrationSplit.TestName);
        }

        return text.ToString();
    }

    public static string RenderPredictions(
        IReadOnlyList<RepositorySplit> repositories,
        IReadOnlyList<RepositoryCalibration> results)
    {
        StringBuilder text = new();
        text.Append(PredictionsHeader).Append('\n');

        for (int index = 0; index < repositories.Count; index++)
        {
            IReadOnlyList<SnapshotRow> test = repositories[index].Test;
            RepositoryCalibration result = results[index];

            for (int row = 0; row < test.Count; row++)
            {
                text.Append(test[row].RepositoryIdentity).Append(',');
                text.Append(test[row].Sha).Append(',');
                text.Append(Date(test[row].AuthorDateUtc)).Append(',');
                text.Append(test[row].IsBugIntroducing ? '1' : '0').Append(',');
                text.Append(Number(result.RawTest[row].Probability)).Append(',');
                text.Append(Number(result.PlattTest[row].Probability)).Append(',');
                text.Append(Number(result.IsotonicTest[row].Probability));
                text.Append('\n');
            }
        }

        return text.ToString();
    }

    private static void Write(StringBuilder text, string identity, IReadOnlyList<SnapshotRow> rows, string partition)
    {
        foreach (SnapshotRow row in rows)
        {
            text.Append(identity).Append(',');
            text.Append(row.Sha).Append(',');
            text.Append(partition).Append(',');
            text.Append(Date(row.AuthorDateUtc));
            text.Append('\n');
        }
    }

    private static string Date(DateTimeOffset value) =>
        value.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);

    private static string Number(double value) => value.ToString("R", CultureInfo.InvariantCulture);
}
