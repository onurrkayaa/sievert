using System.Text.Json;

using Sievert.Modeling;

namespace Sievert.Measure;

/// <summary>
/// Asama 6 Adim 2: goreli risk endeksi icin egitim skor dagilimini uretir.
///
/// **Yeni model egitilmiyor.** Asama 5'te dondurulmus model dosyalari oldugu gibi
/// yukleniyor ve egitim bolumunun satirlari onlarla skorlaniyor. Donusum de yeniden
/// ogrenilmiyor: olcekleyici <c>model-results.json</c> icindeki egitim istatistiklerinden
/// kuruluyor.
///
/// Yazmadan once bir kapi var: bu yolla uretilen skorlar, egitim esiginde
/// <c>model-results.json</c> icinde kayitli TP/FP/FN/TN sayilarini aynen vermek zorunda.
/// Vermezse dosya yazilmiyor. Amaci, "model dosyasi + kayitli istatistikler" yolunun
/// Asama 5'te olculen seyi gercekten yeniden urettigini gostermek.
/// </summary>
public static class ScoreReferenceCommand
{
    public static int Run(string[] args)
    {
        string dataDirectory = args[1];
        string codeCommit = args[2];
        string outputPath = args[3];

        string snapshot = Path.Combine(dataDirectory, "commit-metrics.csv");
        string manifest = Path.Combine(dataDirectory, "split-manifest.csv");
        string modelResults = Path.Combine(dataDirectory, "model-results.json");
        string modelDirectory = Path.Combine(dataDirectory, "models");

        foreach (string file in (string[])[snapshot, manifest, modelResults])
        {
            FileChecksum.Verify(file, Path.ChangeExtension(file, ".sha256"));
        }

        Console.WriteLine("Dondurulmus dosyalarin ozetleri dogrulandi.");

        ModelRegistry registry = ModelRegistry.Create(modelResults, modelDirectory);
        IReadOnlyList<SnapshotRow> rows = SnapshotReader.Read(snapshot);
        IReadOnlyList<SplitEntry> entries = SplitManifestReader.Read(manifest);
        IReadOnlyList<RepositorySplit> repositories = SplitData.Build(rows, entries);

        Dictionary<string, Confusion> recorded = ReadRecordedTrainConfusions(modelResults);

        List<ScoreDistribution> distributions = [];
        Dictionary<string, string> sources = new(StringComparer.Ordinal)
        {
            ["snapshotSha256"] = FileChecksum.Sha256(snapshot),
            ["manifestSha256"] = FileChecksum.Sha256(manifest),
            ["modelResultsSha256"] = FileChecksum.Sha256(modelResults),
        };

        foreach (RepositorySplit repository in repositories)
        {
            if (registry.ForRepository(repository.Identity) is not ModelProfile profile)
            {
                Console.Error.WriteLine($"{repository.Identity} icin model profili yok; dosya yazilmadi.");

                return 2;
            }

            if (registry.StatusOf(profile.ProfileCode) == ModelStatus.ChecksumMismatch)
            {
                Console.Error.WriteLine($"{profile.ProfileCode}: model ozeti tutmuyor; dosya yazilmadi.");

                return 2;
            }

            FeatureScaler scaler = registry.ScalerFor(profile);
            LoadedModel model = registry.Load(profile.ProfileCode);

            List<float[]> features = [.. repository.Train.Select(scaler.Apply)];
            IReadOnlyList<double> scores = model.Score(features);

            Confusion produced = Confusion.From(repository.Train
                .Select((row, index) => new Prediction(scores[index] >= profile.TrainThreshold, row.IsBugIntroducing)));

            Confusion expected = recorded[repository.Identity];

            Console.WriteLine(
                $"{profile.ProfileCode,-9} egitim {repository.Train.Count,6} satir, esik "
                + $"{ModelRegistry.Number(profile.TrainThreshold)}");
            Console.WriteLine(
                $"           uretilen TP {produced.TruePositives} FP {produced.FalsePositives} "
                + $"FN {produced.FalseNegatives} TN {produced.TrueNegatives}");
            Console.WriteLine(
                $"           kayitli  TP {expected.TruePositives} FP {expected.FalsePositives} "
                + $"FN {expected.FalseNegatives} TN {expected.TrueNegatives}");

            if (produced != expected)
            {
                Console.Error.WriteLine(
                    $"{profile.ProfileCode}: yeniden uretilen egitim sayimi kayitli sayimla ayni degil. "
                    + "Referans dosyasi YAZILMADI.");

                return 2;
            }

            sources[profile.ProfileCode + "ModelSha256"] = profile.ModelChecksum;
            distributions.Add(ScoreDistribution.FromScores(profile.ProfileCode, repository.Identity, scores));
        }

        distributions.Sort((left, right) =>
            string.CompareOrdinal(left.ProfileCode, right.ProfileCode));

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        File.WriteAllText(
            outputPath,
            ScoreReference.Render(codeCommit, sources, distributions),
            new System.Text.UTF8Encoding(false));

        File.WriteAllText(Path.ChangeExtension(outputPath, ".sha256"), FileChecksum.Line(outputPath));

        Console.WriteLine();
        Console.WriteLine($"{outputPath}: {FileChecksum.Sha256(outputPath)}");

        return 0;
    }

    private static Dictionary<string, Confusion> ReadRecordedTrainConfusions(string modelResults)
    {
        Dictionary<string, Confusion> recorded = new(StringComparer.Ordinal);

        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(modelResults));

        foreach (JsonElement entry in document.RootElement.GetProperty("repositories").EnumerateArray())
        {
            JsonElement train = entry.GetProperty("trainAtTrainThreshold");

            recorded[entry.GetProperty("repository").GetString()!] = new Confusion(
                train.GetProperty("tp").GetInt32(),
                train.GetProperty("fp").GetInt32(),
                train.GetProperty("fn").GetInt32(),
                train.GetProperty("tn").GetInt32());
        }

        return recorded;
    }
}
