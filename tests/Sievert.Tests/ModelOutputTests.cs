using Sievert.Modeling;

namespace Sievert.Tests;

/// <summary>Model sonuc ve tahmin dosyalarinin bicimi ve deterministikligi.</summary>
public class ModelOutputTests
{
    [Fact]
    public void Predictions_CarryOnlyTheTestRows()
    {
        (IReadOnlyList<RepositorySplit> repositories, ModelReport report) = Run();

        string[] lines = PredictionsFile
            .Render(repositories, report.Repositories)
            .Split('\n', StringSplitOptions.RemoveEmptyEntries);

        int expected = repositories.Sum(repository => repository.Test.Count);

        Assert.Equal(expected + 1, lines.Length);

        HashSet<string> trainShas = [.. repositories.SelectMany(repository => repository.Train).Select(row => row.Sha)];

        foreach (string line in lines.Skip(1))
        {
            Assert.DoesNotContain(line.Split(',')[1], trainShas);
        }
    }

    [Fact]
    public void Predictions_ListEveryTestKeyExactlyOnce()
    {
        (IReadOnlyList<RepositorySplit> repositories, ModelReport report) = Run();

        string[] lines = PredictionsFile
            .Render(repositories, report.Repositories)
            .Split('\n', StringSplitOptions.RemoveEmptyEntries);

        List<string> keys = [.. lines.Skip(1).Select(line => line.Split(',')[0] + " " + line.Split(',')[1])];

        Assert.Equal(keys.Count, keys.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void Predictions_HaveTheAgreedColumns()
    {
        (IReadOnlyList<RepositorySplit> repositories, ModelReport report) = Run();

        string header = PredictionsFile.Render(repositories, report.Repositories).Split('\n')[0];

        Assert.Equal(
            "RepositoryIdentity,Sha,AuthorDateUtc,ActualLabel,Probability,"
            + "PredictionAt05,PredictionAtTrainThreshold,TrainThreshold",
            header);
    }

    [Fact]
    public void Predictions_AreTheSameTextTwice()
    {
        (IReadOnlyList<RepositorySplit> repositories, ModelReport report) = Run();

        Assert.Equal(
            PredictionsFile.Render(repositories, report.Repositories),
            PredictionsFile.Render(repositories, report.Repositories));
    }

    [Fact]
    public void Results_AreTheSameTextTwice()
    {
        (_, ModelReport report) = Run();
        BaselineComparison baseline = Baseline();

        Assert.Equal(
            ModelJson.Render(report, baseline, "paket", "trainer"),
            ModelJson.Render(report, baseline, "paket", "trainer"));
    }

    [Fact]
    public void Results_KeepTheRepositoryOrderTheyWereGiven()
    {
        (_, ModelReport report) = Run();

        string text = ModelJson.Render(report, Baseline(), "paket", "trainer");

        Assert.True(
            text.IndexOf("a/one", StringComparison.Ordinal) < text.IndexOf("b/two", StringComparison.Ordinal));
    }

    [Fact]
    public void Results_WriteNotAvailableAsNull()
    {
        (_, ModelReport report) = Run();

        Assert.Contains("\"gap\": null", ModelJson.Render(report, Baseline(), "paket", "trainer"), StringComparison.Ordinal);
    }

    [Fact]
    public void Checksum_MismatchStopsBeforeAnyTraining()
    {
        string path = Path.Combine(Path.GetTempPath(), "sievert-model-" + Guid.NewGuid().ToString("n") + ".csv");
        File.WriteAllText(path, "veri");

        string checksum = Path.ChangeExtension(path, ".sha256");
        File.WriteAllText(checksum, new string('0', 64) + "  " + Path.GetFileName(path) + "\n");

        Assert.Throws<InvalidDataException>(() => FileChecksum.Verify(path, checksum));

        File.Delete(path);
        File.Delete(checksum);
    }

    private static BaselineComparison Baseline() =>
        new(
            0.3,
            0.3,
            0.3,
            new Dictionary<string, double>(StringComparer.Ordinal) { ["a/one"] = 0.1, ["b/two"] = 0.2 },
            new Dictionary<string, double>(StringComparer.Ordinal) { ["a/one"] = 0.1, ["b/two"] = 0.2 });

    private static (IReadOnlyList<RepositorySplit> Repositories, ModelReport Report) Run()
    {
        IReadOnlyList<RepositorySplit> repositories = [Split("a/one"), Split("b/two")];

        return (repositories, ModelRunner.Run(repositories, "anlik", "manifest", "taban", "commit"));
    }

    private static RepositorySplit Split(string identity)
    {
        List<SnapshotRow> train = [];
        List<SnapshotRow> test = [];

        for (int index = 0; index < 100; index++)
        {
            train.Add(Row(identity, index + 1, index % 6, index % 3 == 0, identity + "-t" + index));
        }

        for (int index = 0; index < 30; index++)
        {
            test.Add(Row(identity, index + 4, index % 4, index % 3 == 0, identity + "-s" + index));
        }

        return new RepositorySplit(identity, train, test);
    }

    private static SnapshotRow Row(string identity, int size, int history, bool positive, string sha) =>
        new("klasor", identity, sha, DateTimeOffset.UnixEpoch.AddDays(size),
            size * (positive ? 30 : 1), size, size % 5 + 1, size % 3, size % 4 * 0.5,
            size % 6 + 1, size % 2 + 1, size * 3, size, history * 2 + 1, history,
            history + 1, size * 2, history * 3, positive, positive, positive ? "szz" : null, false);
}
