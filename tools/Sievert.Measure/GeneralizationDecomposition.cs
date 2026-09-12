using System.Globalization;
using System.Text;
using System.Text.Json;

using Sievert.Modeling;

namespace Sievert.Measure;

/// <summary>
/// Adim 5b Bolum C: mevcut genelleme sonucunun betimsel ayristirmasi. Yeni model
/// egitilmiyor, yeni esik secilmiyor, hedef testte kalibrasyon yapilmiyor ve mevcut
/// genelleme sonucu degistirilmiyor.
///
/// Bu, genelleme basarisizliginin nedenini KANITLAMAZ; yalnizca farklari ve taban oran
/// uyumsuzlugunu yan yana koyar.
/// </summary>
public static class GeneralizationDecomposition
{
    /// <summary>Sonuc gormeden sabitlenen "belirgin dusus" siniri.</summary>
    public const double MaterialDrop = -0.05;

    /// <summary>Adim 3'un ayni-repo sonuclari.</summary>
    private static readonly Dictionary<string, (double F1, double PrAuc, double F1AtHalf)> SameRepo =
        new(StringComparer.Ordinal)
        {
            ["github.com/app-vnext/polly"] = (0.2500, 0.3033, 0.4706),
            ["github.com/jellyfin/jellyfin"] = (0.4895, 0.5199, 0.4974),
            ["github.com/sharex/sharex"] = (0.1868, 0.1192, 0.0843),
        };

    public static int Run(string[] args)
    {
        string dataDirectory = args[1];
        string codeCommit = args[2];

        string source = Path.Combine(dataDirectory, "generalization-results.json");
        FileChecksum.Verify(source, Path.ChangeExtension(source, ".sha256"));

        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(source));

        using MemoryStream stream = new();
        Utf8JsonWriter writer = new(stream, new JsonWriterOptions { Indented = true });

        writer.WriteStartObject();
        writer.WriteString("generalizationSha256", FileChecksum.Sha256(source));
        writer.WriteString("codeCommit", codeCommit);
        writer.WriteNumber("materialDropThreshold", MaterialDrop);
        writer.WriteString("note", "betimsel ayristirma; neden kanitlamaz, yeni model ya da esik uretmez");
        writer.WriteStartArray("experiments");

        int areaKeptF1Dropped = 0;
        int halfBetter = 0;
        int meanAboveRate = 0;
        int meanBelowRate = 0;
        int singleAboveSame = 0;
        int loroAboveSame = 0;

        Console.WriteLine("== Genelleme ayristirmasi ==");

        foreach (JsonElement entry in document.RootElement.GetProperty("experiments").EnumerateArray())
        {
            string experiment = entry.GetProperty("experiment").GetString()!;
            string target = entry.GetProperty("target").GetString()!;
            List<string> sources = [];

            foreach (JsonElement item in entry.GetProperty("sources").EnumerateArray())
            {
                sources.Add(item.GetString()!);
            }

            (double F1, double PrAuc, double F1AtHalf) same = SameRepo[target];

            double area = entry.GetProperty("prAuc").GetDouble();
            double f1 = entry.GetProperty("atSourceThreshold").GetProperty("f1").GetDouble();
            double f1AtHalf = entry.GetProperty("atHalf").GetProperty("f1").GetDouble();
            double mean = entry.GetProperty("meanPrediction").GetDouble();

            int targetRows = entry.GetProperty("targetRows").GetInt32();
            int targetPositives = entry.GetProperty("targetPositives").GetInt32();
            int sourceRows = entry.GetProperty("sourceRows").GetInt32();
            int sourcePositives = entry.GetProperty("sourcePositives").GetInt32();

            double targetRate = (double)targetPositives / targetRows;
            double sourceRate = (double)sourcePositives / sourceRows;

            double deltaArea = area - same.PrAuc;
            double deltaF1 = f1 - same.F1;
            double deltaHalf = f1AtHalf - same.F1AtHalf;

            bool keptArea = deltaArea > MaterialDrop;
            bool droppedF1 = deltaF1 <= MaterialDrop;

            if (keptArea && droppedF1)
            {
                areaKeptF1Dropped++;
            }

            if (f1AtHalf > f1)
            {
                halfBetter++;
            }

            if (mean > targetRate)
            {
                meanAboveRate++;
            }
            else
            {
                meanBelowRate++;
            }

            if (deltaF1 > 0)
            {
                if (string.Equals(experiment, "tek-kaynak", StringComparison.Ordinal))
                {
                    singleAboveSame++;
                }
                else
                {
                    loroAboveSame++;
                }
            }

            Console.WriteLine(
                $"  {string.Join(" + ", sources.Select(Short))} -> {Short(target)}: "
                + $"dPR-AUC {deltaArea:+0.0000;-0.0000}, dF1(kaynak esigi) {deltaF1:+0.0000;-0.0000}, "
                + $"dF1(0,5) {deltaHalf:+0.0000;-0.0000}, ort tahmin {mean:F4} vs hedef oran {targetRate:F4} "
                + $"(fark {Math.Abs(mean - targetRate):F4})");

            writer.WriteStartObject();
            writer.WriteString("experiment", experiment);
            writer.WriteStartArray("sources");

            foreach (string item in sources)
            {
                writer.WriteStringValue(item);
            }

            writer.WriteEndArray();
            writer.WriteString("target", target);
            writer.WriteNumber("deltaPrAuc", deltaArea);
            writer.WriteNumber("deltaF1AtSourceThreshold", deltaF1);
            writer.WriteNumber("deltaF1AtHalf", deltaHalf);
            writer.WriteNumber("meanPrediction", mean);
            writer.WriteNumber("targetPositiveRate", targetRate);
            writer.WriteNumber("absoluteMeanMinusTargetRate", Math.Abs(mean - targetRate));
            writer.WriteNumber("sourceTrainPositiveRate", sourceRate);
            writer.WriteNumber("sourceMinusTargetRate", sourceRate - targetRate);
            writer.WriteBoolean("prAucKeptF1MateriallyDropped", keptArea && droppedF1);
            writer.WriteBoolean("halfThresholdBetter", f1AtHalf > f1);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();

        writer.WriteStartObject("counts");
        writer.WriteNumber("prAucKeptF1MateriallyDropped", areaKeptF1Dropped);
        writer.WriteNumber("halfThresholdBetter", halfBetter);
        writer.WriteNumber("meanPredictionAboveTargetRate", meanAboveRate);
        writer.WriteNumber("meanPredictionBelowTargetRate", meanBelowRate);
        writer.WriteNumber("singleSourceAboveSameRepoF1", singleAboveSame);
        writer.WriteNumber("leaveOneOutAboveSameRepoF1", loroAboveSame);
        writer.WriteEndObject();

        writer.WriteEndObject();
        writer.Flush();

        Console.WriteLine();
        Console.WriteLine($"  PR-AUC korunup F1 belirgin dusen deney: {areaKeptF1Dropped} / 9");
        Console.WriteLine($"  0,5 esigi kaynak esiginden iyi olan deney: {halfBetter} / 9");
        Console.WriteLine($"  ortalama tahmin hedef orandan yuksek: {meanAboveRate}, dusuk: {meanBelowRate}");
        Console.WriteLine($"  tek kaynakta ayni-repo F1 ustunde: {singleAboveSame} / 6");
        Console.WriteLine($"  LORO'da ayni-repo F1 ustunde: {loroAboveSame} / 3");

        string output = Path.Combine(dataDirectory, "generalization-decomposition.json");
        File.WriteAllText(output, Encoding.UTF8.GetString(stream.ToArray()) + "\n", new UTF8Encoding(false));
        File.WriteAllText(Path.ChangeExtension(output, ".sha256"), FileChecksum.Line(output));

        Console.WriteLine();
        Console.WriteLine($"generalization-decomposition.json: {FileChecksum.Sha256(output)}");

        return 0;
    }

    private static string Short(string identity) => identity[(identity.LastIndexOf('/') + 1)..];
}
