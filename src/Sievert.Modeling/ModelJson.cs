using System.Globalization;
using System.Text;
using System.Text.Json;

namespace Sievert.Modeling;

/// <summary>LinesAdded tabaninin test degerleri; gecis kosullari bunlara karsi.</summary>
public sealed record BaselineComparison(
    double MicroF1,
    double MacroF1,
    double MicroPrAuc,
    IReadOnlyDictionary<string, double> RepositoryF1,
    IReadOnlyDictionary<string, double> RepositoryPrAuc);

/// <summary>
/// Model sonuc dosyasi. Deterministik: alan sirasi sabit, depo sirasi verildigi gibi,
/// sayilar invariant.
/// </summary>
public static class ModelJson
{
    public static string Render(ModelReport report, BaselineComparison baseline, string package, string trainer)
    {
        using MemoryStream stream = new();

        using (Utf8JsonWriter writer = new(stream, new JsonWriterOptions { Indented = true }))
        {
            writer.WriteStartObject();

            writer.WriteString("metricContractVersion", report.MetricContractVersion);
            writer.WriteString("snapshotSha256", report.SnapshotSha256);
            writer.WriteString("manifestSha256", report.ManifestSha256);
            writer.WriteString("baselineSha256", report.BaselineSha256);
            writer.WriteString("codeCommit", report.CodeCommit);
            writer.WriteString("mlPackage", package);

            WriteTrainer(writer, trainer);
            WriteTransform(writer);

            writer.WriteStartArray("repositories");

            foreach (RepositoryModelResult result in report.Repositories)
            {
                WriteRepository(writer, result);
            }

            writer.WriteEndArray();

            writer.WriteStartObject("totals");
            writer.WriteStartObject("microAtFixed");
            WriteCounts(writer, report.MicroAtFixed);
            writer.WriteEndObject();
            writer.WriteStartObject("microAtTrainThreshold");
            WriteCounts(writer, report.MicroAtTuned);
            writer.WriteEndObject();
            WriteNumberOrNull(writer, "microPrAuc", report.MicroPrAuc);
            WriteNumberOrNull(writer, "macroF1AtTrainThreshold", report.MacroF1AtTuned);
            WriteNumberOrNull(writer, "macroPrAuc", report.MacroPrAuc);
            writer.WriteEndObject();

            writer.WriteStartObject("calibration");
            writer.WriteNumber("microBrier", report.MicroCalibration.Brier);
            writer.WriteNumber("microEce", report.MicroCalibration.Ece);
            writer.WriteNumber("macroBrier", report.MacroBrier);
            writer.WriteNumber("macroEce", report.MacroEce);
            writer.WriteStartArray("microBins");
            WriteBins(writer, report.MicroCalibration.Bins);
            writer.WriteEndArray();
            writer.WriteEndObject();

            WriteBaseline(writer, report, baseline);

            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(stream.ToArray()) + "\n";
    }

    public static void Write(string path, ModelReport report, BaselineComparison baseline, string package, string trainer) =>
        File.WriteAllText(path, Render(report, baseline, package, trainer), new UTF8Encoding(false));

    private static void WriteTrainer(Utf8JsonWriter writer, string trainer)
    {
        writer.WriteStartObject("trainer");
        writer.WriteString("name", trainer);
        writer.WriteNumber("mlContextSeed", LogisticRegressionModel.Seed);
        writer.WriteNumber("l1Regularization", LogisticRegressionModel.L1Regularization);
        writer.WriteNumber("l2Regularization", LogisticRegressionModel.L2Regularization);
        writer.WriteNumber("numberOfThreads", LogisticRegressionModel.NumberOfThreads);
        writer.WriteString("classWeighting", "yok");
        writer.WriteString("resampling", "yok");
        writer.WriteString("cacheCheckpoint", "yok");
        writer.WriteEndObject();
    }

    private static void WriteTransform(Utf8JsonWriter writer)
    {
        writer.WriteStartObject("transform");
        writer.WriteStartArray("log1p");

        foreach (string name in FeatureTransform.LogFeatures)
        {
            writer.WriteStringValue(name);
        }

        writer.WriteEndArray();
        writer.WriteString("standardisedWithoutLog", FeatureTransform.RawContinuousFeature);
        writer.WriteString("untouched", FeatureTransform.FlagFeature);
        writer.WriteString("fittedOn", "yalnizca train");
        writer.WriteString("clipping", "yok");
        writer.WriteEndObject();
    }

    private static void WriteRepository(Utf8JsonWriter writer, RepositoryModelResult result)
    {
        writer.WriteStartObject();
        writer.WriteString("repository", result.Identity);
        writer.WriteNumber("trainThreshold", result.TrainThreshold);
        writer.WriteNumber("trainThresholdCandidates", result.TrainThresholdCandidates);
        writer.WriteNumber("testValuesOutsideTrainRange", result.TestValuesOutsideTrainRange);
        writer.WriteNumber("testRowsOutsideTrainRange", result.TestRowsOutsideTrainRange);

        WriteOutcome(writer, "trainAtTrainThreshold", result.TrainAtTuned);
        WriteOutcome(writer, "testAtFixed", result.TestAtFixed);
        WriteOutcome(writer, "testAtTrainThreshold", result.TestAtTuned);

        writer.WriteStartObject("calibration");
        writer.WriteNumber("brier", result.Calibration.Brier);
        writer.WriteNumber("ece", result.Calibration.Ece);
        writer.WriteStartArray("bins");
        WriteBins(writer, result.Calibration.Bins);
        writer.WriteEndArray();
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

        writer.WriteStartArray("trainStatistics");

        foreach (FeatureStatistics statistics in result.Statistics.OrderBy(entry => entry.Name, StringComparer.Ordinal))
        {
            writer.WriteStartObject();
            writer.WriteString("feature", statistics.Name);
            writer.WriteNumber("min", statistics.Min);
            writer.WriteNumber("max", statistics.Max);
            writer.WriteNumber("mean", statistics.Mean);
            writer.WriteNumber("standardDeviation", statistics.StandardDeviation);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();

        writer.WriteStartArray("highCorrelations");

        foreach (CorrelatedPair pair in result.HighCorrelations)
        {
            writer.WriteStartObject();
            writer.WriteString("first", pair.First);
            writer.WriteString("second", pair.Second);
            writer.WriteNumber("rho", pair.Rho);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
        writer.WriteEndObject();
    }

    private static void WriteBins(Utf8JsonWriter writer, IReadOnlyList<CalibrationBin> bins)
    {
        foreach (CalibrationBin bin in bins)
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
    }

    private static void WriteOutcome(Utf8JsonWriter writer, string name, ThresholdedOutcome outcome)
    {
        writer.WriteStartObject(name);
        writer.WriteNumber("threshold", outcome.Threshold);
        WriteCounts(writer, outcome.Counts);
        WriteNumberOrNull(writer, "prAuc", outcome.PrAuc);
        writer.WriteEndObject();
    }

    private static void WriteBaseline(Utf8JsonWriter writer, ModelReport report, BaselineComparison baseline)
    {
        writer.WriteStartObject("baselineComparison");
        writer.WriteString("baseline", "LinesAdded esigi (Adim 2)");
        writer.WriteNumber("baselineMicroF1", baseline.MicroF1);
        writer.WriteNumber("baselineMacroF1", baseline.MacroF1);
        writer.WriteNumber("baselineMicroPrAuc", baseline.MicroPrAuc);

        bool micro = report.MicroAtTuned.F1 > baseline.MicroF1;
        bool macro = report.MacroF1AtTuned > baseline.MacroF1;
        bool area = report.MicroPrAuc > baseline.MicroPrAuc;

        writer.WriteBoolean("microF1Passed", micro);
        writer.WriteBoolean("macroF1Passed", macro);
        writer.WriteBoolean("microPrAucPassed", area);
        writer.WriteNumber("conditionsPassed", (micro ? 1 : 0) + (macro ? 1 : 0) + (area ? 1 : 0));

        writer.WriteStartArray("perRepository");

        foreach (RepositoryModelResult result in report.Repositories)
        {
            writer.WriteStartObject();
            writer.WriteString("repository", result.Identity);
            WriteNumberOrNull(writer, "modelF1", result.TestAtTuned.Counts.F1);
            writer.WriteNumber("baselineF1", baseline.RepositoryF1[result.Identity]);
            WriteNumberOrNull(writer, "modelPrAuc", result.TestAtTuned.PrAuc);
            writer.WriteNumber("baselinePrAuc", baseline.RepositoryPrAuc[result.Identity]);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
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

/// <summary>Test tahminlerinin satir satir dosyasi. Sira manifestteki test sirasi.</summary>
public static class PredictionsFile
{
    public const string Header =
        "RepositoryIdentity,Sha,AuthorDateUtc,ActualLabel,Probability,"
        + "PredictionAt05,PredictionAtTrainThreshold,TrainThreshold";

    public static string Render(
        IReadOnlyList<RepositorySplit> repositories,
        IReadOnlyList<RepositoryModelResult> results)
    {
        StringBuilder text = new();
        text.Append(Header).Append('\n');

        for (int index = 0; index < repositories.Count; index++)
        {
            RepositorySplit repository = repositories[index];
            RepositoryModelResult result = results[index];

            for (int row = 0; row < repository.Test.Count; row++)
            {
                SnapshotRow test = repository.Test[row];
                double probability = result.TestProbabilities[row].Probability;

                text.Append(test.RepositoryIdentity).Append(',');
                text.Append(test.Sha).Append(',');
                text.Append(test.AuthorDateUtc.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture)).Append(',');
                text.Append(test.IsBugIntroducing ? '1' : '0').Append(',');
                text.Append(probability.ToString("R", CultureInfo.InvariantCulture)).Append(',');
                text.Append(probability >= ProbabilityThreshold.Fixed ? '1' : '0').Append(',');
                text.Append(probability >= result.TrainThreshold ? '1' : '0').Append(',');
                text.Append(result.TrainThreshold.ToString("R", CultureInfo.InvariantCulture));
                text.Append('\n');
            }
        }

        return text.ToString();
    }

    public static void Write(
        string path,
        IReadOnlyList<RepositorySplit> repositories,
        IReadOnlyList<RepositoryModelResult> results) =>
        File.WriteAllText(path, Render(repositories, results), new UTF8Encoding(false));
}
