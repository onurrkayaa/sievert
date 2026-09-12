using System.Globalization;
using System.Text;
using System.Text.Json;

using Sievert.Modeling;

namespace Sievert.Measure;

/// <summary>
/// Adim 5b Bolum B: sentetik gurultu sonuclarini taban oranindan ayirir.
///
/// Mevcut deneyi DEGISTIRMIYOR: ayni oranlar, ayni ana tohum, ayni tekrar tohumlari ve
/// ayni flip'ler kullaniliyor. Uzerine iki eslenmis taban ve dort normalize olcu
/// ekleniyor (sozlesme surum 1.0).
/// </summary>
public static class NoiseNormalizedCommand
{
    private static readonly double[] Rates = [0.05, 0.10, 0.20];

    private const int Repeats = 100;

    private const int Seed = 20260912;

    /// <summary>
    /// Rastgele taban icin ayri bir akis. Flip akisiyla ayni Random kullanilsaydi
    /// flip'ler degisir ve deney mevcut sonucla eslesmezdi.
    /// </summary>
    private const int BaselineSeedOffset = 1_000_000;

    public static int Run(string[] args)
    {
        string dataDirectory = args[1];
        string codeCommit = args[2];

        string snapshot = Path.Combine(dataDirectory, "commit-metrics.csv");
        string manifest = Path.Combine(dataDirectory, "split-manifest.csv");
        string existing = Path.Combine(dataDirectory, "sensitivity-results.json");

        foreach (string file in (string[])[snapshot, manifest, existing])
        {
            FileChecksum.Verify(file, Path.ChangeExtension(file, ".sha256"));
        }

        IReadOnlyList<RepositorySplit> repositories = SplitData.Build(
            SnapshotReader.Read(snapshot),
            SplitManifestReader.Read(manifest));

        Dictionary<string, (double F1, double PrAuc, double Brier)> previous = ReadPrevious(existing);

        using MemoryStream stream = new();
        Utf8JsonWriter writer = new(stream, new JsonWriterOptions { Indented = true });

        writer.WriteStartObject();
        writer.WriteString("contractVersion", "1.0");
        writer.WriteString("snapshotSha256", FileChecksum.Sha256(snapshot));
        writer.WriteString("manifestSha256", FileChecksum.Sha256(manifest));
        writer.WriteString("sensitivitySha256", FileChecksum.Sha256(existing));
        writer.WriteString("codeCommit", codeCommit);
        writer.WriteNumber("seed", Seed);
        writer.WriteNumber("repeats", Repeats);
        writer.WriteString("note", "sentetik etiketlere gore hesaplandi; gercek performans iddiasi degil");
        writer.WriteStartArray("cases");

        bool matched = true;

        foreach (RepositorySplit repository in repositories)
        {
            foreach (double rate in Rates)
            {
                matched &= One(writer, repository, rate, previous);
            }
        }

        writer.WriteEndArray();
        writer.WriteBoolean("existingModelResultsUnchanged", matched);
        writer.WriteEndObject();
        writer.Flush();

        string output = Path.Combine(dataDirectory, "noise-normalized-results.json");
        File.WriteAllText(output, Encoding.UTF8.GetString(stream.ToArray()) + "\n", new UTF8Encoding(false));
        File.WriteAllText(Path.ChangeExtension(output, ".sha256"), FileChecksum.Line(output));

        Console.WriteLine();
        Console.WriteLine($"mevcut model sonuclari degismedi: {(matched ? "evet" : "HAYIR")}");
        Console.WriteLine($"noise-normalized-results.json: {FileChecksum.Sha256(output)}");

        return matched ? 0 : 2;
    }

    private static bool One(
        Utf8JsonWriter writer,
        RepositorySplit repository,
        double rate,
        Dictionary<string, (double F1, double PrAuc, double Brier)> previous)
    {
        List<double?> modelF1 = [];
        List<double?> modelArea = [];
        List<double?> modelLift = [];
        List<double?> modelNormalised = [];
        List<double?> modelBrier = [];
        List<double?> modelSkill = [];
        List<double?> baselineF1 = [];
        List<double?> baselineArea = [];
        List<double?> baselineLift = [];
        List<double?> baselineNormalised = [];
        List<double?> randomF1 = [];
        List<double?> randomArea = [];
        List<double?> randomLift = [];
        List<double?> deltaF1Baseline = [];
        List<double?> deltaAreaBaseline = [];
        List<double?> deltaLiftBaseline = [];
        List<double?> deltaF1Random = [];
        List<double?> deltaAreaRandom = [];
        List<double?> testRate = [];
        int normalisedMissing = 0;
        int skillMissing = 0;

        for (int repeat = 0; repeat < Repeats; repeat++)
        {
            // Flip akisi mevcut deneyle BIREBIR ayni: ayni tohum, once train sonra test.
            Random flips = new(Seed + repeat);
            IReadOnlyList<SnapshotRow> train = Sensitivity.Flip(repository.Train, rate, flips);
            IReadOnlyList<SnapshotRow> test = Sensitivity.Flip(repository.Test, rate, flips);

            double trainRate = NoiseNormalization.PositiveRate(train);
            double positives = NoiseNormalization.PositiveRate(test);
            testRate.Add(positives);

            SubsetOutcome model = Sensitivity.Retrain(
                "sentetik", repository.Identity, train, test, ModelFeatures.Candidates);

            double climatology = NoiseNormalization.Climatology(Pair(test, positives), trainRate);
            double? skill = NoiseNormalization.SkillScore(model.Brier, climatology);
            double? normalised = NoiseNormalization.Normalised(model.PrAuc, positives);

            if (normalised is null)
            {
                normalisedMissing++;
            }

            if (skill is null)
            {
                skillMissing++;
            }

            modelF1.Add(model.Counts.F1);
            modelArea.Add(model.PrAuc);
            modelLift.Add(NoiseNormalization.Lift(model.PrAuc, positives));
            modelNormalised.Add(normalised);
            modelBrier.Add(model.Brier);
            modelSkill.Add(skill);

            (double threshold, Confusion counts, double? area, double brier) =
                NoiseNormalization.LinesAddedFor(train, test);
            _ = threshold;
            _ = brier;

            baselineF1.Add(counts.F1);
            baselineArea.Add(area);
            baselineLift.Add(NoiseNormalization.Lift(area, positives));
            baselineNormalised.Add(NoiseNormalization.Normalised(area, positives));

            Random noise = new(Seed + repeat + BaselineSeedOffset);
            Outcome chance = Evaluation.Of(NoiseNormalization.RandomBaselineFor(test, trainRate, noise));

            randomF1.Add(chance.Counts.F1);
            randomArea.Add(chance.PrAuc);
            randomLift.Add(NoiseNormalization.Lift(chance.PrAuc, positives));

            deltaF1Baseline.Add(Minus(model.Counts.F1, counts.F1));
            deltaAreaBaseline.Add(Minus(model.PrAuc, area));
            deltaLiftBaseline.Add(Minus(
                NoiseNormalization.Lift(model.PrAuc, positives),
                NoiseNormalization.Lift(area, positives)));
            deltaF1Random.Add(Minus(model.Counts.F1, chance.Counts.F1));
            deltaAreaRandom.Add(Minus(model.PrAuc, chance.PrAuc));
        }

        // Mevcut deneyin model sonuclari degismemeli.
        string key = repository.Identity + "|" + rate.ToString("F2", CultureInfo.InvariantCulture);
        Distribution f1 = Distribution.Of(modelF1);
        Distribution area2 = Distribution.Of(modelArea);
        Distribution brier2 = Distribution.Of(modelBrier);
        bool matched = true;

        if (previous.TryGetValue(key, out (double F1, double PrAuc, double Brier) old))
        {
            matched = Math.Abs(old.F1 - f1.Mean) < 1e-12
                && Math.Abs(old.PrAuc - area2.Mean) < 1e-12
                && Math.Abs(old.Brier - brier2.Mean) < 1e-12;
        }

        Console.WriteLine(
            $"  {repository.Identity} %{rate * 100:F0}: model F1 {f1.Mean:F4}, lift {Distribution.Of(modelLift).Mean:F4}, "
            + $"normalize {Distribution.Of(modelNormalised).Mean:F4}, skill {Distribution.Of(modelSkill).Mean:F4}, "
            + $"taban orani {Distribution.Of(testRate).Mean:F4}  (mevcut sonucla ayni: {(matched ? "evet" : "HAYIR")})");

        writer.WriteStartObject();
        writer.WriteString("repository", repository.Identity);
        writer.WriteNumber("rate", rate);
        writer.WriteBoolean("modelResultUnchanged", matched);
        writer.WriteNumber("normalisedNotAvailable", normalisedMissing);
        writer.WriteNumber("skillNotAvailable", skillMissing);
        Write(writer, "testPositiveRate", testRate);

        writer.WriteStartObject("model");
        Write(writer, "f1", modelF1);
        Write(writer, "prAuc", modelArea);
        Write(writer, "prAucLift", modelLift);
        Write(writer, "normalisedPrAuc", modelNormalised);
        Write(writer, "brier", modelBrier);
        Write(writer, "brierSkillScore", modelSkill);
        writer.WriteEndObject();

        writer.WriteStartObject("linesAdded");
        Write(writer, "f1", baselineF1);
        Write(writer, "prAuc", baselineArea);
        Write(writer, "prAucLift", baselineLift);
        Write(writer, "normalisedPrAuc", baselineNormalised);
        writer.WriteEndObject();

        writer.WriteStartObject("random");
        Write(writer, "f1", randomF1);
        Write(writer, "prAuc", randomArea);
        Write(writer, "prAucLift", randomLift);
        writer.WriteEndObject();

        writer.WriteStartObject("differences");
        Write(writer, "modelMinusLinesAddedF1", deltaF1Baseline);
        Write(writer, "modelMinusLinesAddedPrAuc", deltaAreaBaseline);
        Write(writer, "modelMinusLinesAddedLift", deltaLiftBaseline);
        Write(writer, "modelMinusRandomF1", deltaF1Random);
        Write(writer, "modelMinusRandomPrAuc", deltaAreaRandom);
        writer.WriteEndObject();

        writer.WriteEndObject();

        return matched;
    }

    private static IReadOnlyList<ScoredProbability> Pair(IReadOnlyList<SnapshotRow> rows, double probability)
    {
        List<ScoredProbability> paired = new(rows.Count);

        foreach (SnapshotRow row in rows)
        {
            paired.Add(new ScoredProbability(probability, row.IsBugIntroducing));
        }

        return paired;
    }

    private static double? Minus(double? one, double? other) =>
        one is double a && other is double b ? a - b : null;

    private static Dictionary<string, (double F1, double PrAuc, double Brier)> ReadPrevious(string path)
    {
        Dictionary<string, (double, double, double)> previous = new(StringComparer.Ordinal);
        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(path));

        foreach (JsonElement entry in document.RootElement.GetProperty("syntheticNoise").EnumerateArray())
        {
            string key = entry.GetProperty("repository").GetString()!
                + "|"
                + entry.GetProperty("rate").GetDouble().ToString("F2", CultureInfo.InvariantCulture);

            previous[key] = (
                entry.GetProperty("f1").GetProperty("mean").GetDouble(),
                entry.GetProperty("prAuc").GetProperty("mean").GetDouble(),
                entry.GetProperty("brier").GetProperty("mean").GetDouble());
        }

        return previous;
    }

    private static void Write(Utf8JsonWriter writer, string name, IReadOnlyList<double?> values)
    {
        Distribution spread = Distribution.Of(values);

        writer.WriteStartObject(name);
        Number(writer, "mean", spread.Mean);
        Number(writer, "p2_5", spread.Low);
        Number(writer, "median", spread.Median);
        Number(writer, "p97_5", spread.High);
        writer.WriteNumber("notAvailable", spread.NotAvailable);
        writer.WriteEndObject();
    }

    private static void Number(Utf8JsonWriter writer, string name, double value)
    {
        if (double.IsFinite(value))
        {
            writer.WriteNumber(name, value);
        }
        else
        {
            writer.WriteNull(name);
        }
    }
}
