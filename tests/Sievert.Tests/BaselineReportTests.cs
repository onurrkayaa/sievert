using Sievert.Modeling;

using static Sievert.Tests.EvaluationMetricTests;

namespace Sievert.Tests;

/// <summary>
/// Manifestten bolme kurma, mikro/makro toplama ve deterministik sonuc dosyasi.
/// </summary>
public class BaselineReportTests
{
    // --- Mikro / makro ---

    [Fact]
    public void Micro_AddsUpEveryRepositoryIntoOnePool()
    {
        Confusion micro = Totals.Micro([new Confusion(1, 2, 3, 4), new Confusion(10, 20, 30, 40)]);

        Assert.Equal(11, micro.TruePositives);
        Assert.Equal(22, micro.FalsePositives);
        Assert.Equal(33, micro.FalseNegatives);
        Assert.Equal(44, micro.TrueNegatives);
    }

    [Fact]
    public void MicroAndMacro_AreNotTheSameNumber()
    {
        // Kucuk repo mukemmel, buyuk repo kotu. Mikro buyugu agirlikliyor,
        // makro ikisini esit sayiyor.
        Confusion small = new(5, 0, 0, 5);
        Confusion large = new(10, 990, 0, 0);

        Confusion micro = Totals.Micro([small, large]);
        double? macro = Totals.MacroF1([small.F1, large.F1]);

        Assert.NotEqual(macro!.Value, micro.F1!.Value, 6);
        Assert.True(macro.Value > micro.F1.Value);
    }

    [Fact]
    public void Macro_IsTheSimpleAverageOfTheRepositoryScores()
    {
        Assert.Equal(0.5, Totals.MacroF1([0.25, 0.75])!.Value, 12);
    }

    [Fact]
    public void Macro_IsNotAvailableWhenARepositoryScoreIsNotAvailable()
    {
        Assert.Null(Totals.MacroF1([0.25, null]));
    }

    // --- Manifestten bolme ---

    [Fact]
    public void Split_IsTakenFromTheManifestNotRecomputed()
    {
        IReadOnlyList<SnapshotRow> rows =
        [
            Row(1, sha: "aa", identity: "a/b"),
            Row(2, sha: "bb", identity: "a/b"),
            Row(3, sha: "cc", identity: "a/b"),
        ];

        SplitEntry[] manifest =
        [
            new("a/b", "aa", SplitEntry.Train, DateTimeOffset.UnixEpoch),
            new("a/b", "bb", SplitEntry.Test, DateTimeOffset.UnixEpoch),
            new("a/b", "cc", SplitEntry.Test, DateTimeOffset.UnixEpoch),
        ];

        IReadOnlyList<RepositorySplit> split = SplitData.Build(rows, manifest);

        Assert.Single(split);
        Assert.Single(split[0].Train);
        Assert.Equal(2, split[0].Test.Count);
    }

    [Fact]
    public void Split_RefusesARowThatTheManifestDoesNotMention()
    {
        IReadOnlyList<SnapshotRow> rows = [Row(1, sha: "aa"), Row(2, sha: "bb")];
        SplitEntry[] manifest = [new("a/b", "aa", SplitEntry.Train, DateTimeOffset.UnixEpoch)];

        Assert.Throws<InvalidDataException>(() => SplitData.Build(rows, manifest));
    }

    [Fact]
    public void Split_OrdersRepositoriesOrdinally()
    {
        IReadOnlyList<SnapshotRow> rows =
        [
            Row(1, sha: "aa", identity: "b/one"),
            Row(2, sha: "bb", identity: "a/two"),
        ];

        SplitEntry[] manifest =
        [
            new("b/one", "aa", SplitEntry.Train, DateTimeOffset.UnixEpoch),
            new("a/two", "bb", SplitEntry.Train, DateTimeOffset.UnixEpoch),
        ];

        IReadOnlyList<RepositorySplit> split = SplitData.Build(rows, manifest);

        Assert.Equal(["a/two", "b/one"], split.Select(entry => entry.Identity));
    }

    [Fact]
    public void Manifest_IsRefusedWhenItsChecksumDoesNotMatch()
    {
        string path = Write(
            "RepositoryIdentity,Sha,Split,AuthorDateUtc\na/b,aa,train,2020-01-01T00:00:00Z\n");
        string checksum = Write(new string('0', 64) + "  " + Path.GetFileName(path) + "\n");

        InvalidDataException error = Assert.Throws<InvalidDataException>(
            () => SplitManifestReader.ReadVerified(path, checksum));

        Assert.Contains("SHA-256", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Manifest_IsRefusedWhenTheHeaderIsWrong()
    {
        string path = Write("Repo,Sha,Split,AuthorDateUtc\na/b,aa,train,2020-01-01T00:00:00Z\n");

        Assert.Throws<InvalidDataException>(() => SplitManifestReader.Read(path));
    }

    [Fact]
    public void Manifest_IsRefusedWhenTheSplitColumnIsNotTrainOrTest()
    {
        string path = Write(
            "RepositoryIdentity,Sha,Split,AuthorDateUtc\na/b,aa,dogrulama,2020-01-01T00:00:00Z\n");

        Assert.Throws<InvalidDataException>(() => SplitManifestReader.Read(path));
    }

    [Fact]
    public void Manifest_ReadsTrainAndTestRows()
    {
        string path = Write(
            "RepositoryIdentity,Sha,Split,AuthorDateUtc\n"
            + "a/b,aa,train,2020-01-01T00:00:00Z\n"
            + "a/b,bb,test,2020-01-02T00:00:00Z\n");

        IReadOnlyList<SplitEntry> entries = SplitManifestReader.Read(path);

        Assert.Equal(2, entries.Count);
        Assert.Equal(SplitEntry.Train, entries[0].Split);
        Assert.Equal(SplitEntry.Test, entries[1].Split);
    }

    // --- Deterministik JSON ---

    [Fact]
    public void Json_TwoRendersOfTheSameResultAreTheSameBytes()
    {
        BaselineReport report = Report();

        Assert.Equal(BaselineJson.Render(report), BaselineJson.Render(report));
    }

    [Fact]
    public void Json_KeepsTheRepositoryOrderItWasGiven()
    {
        string text = BaselineJson.Render(Report());

        Assert.True(
            text.IndexOf("a/one", StringComparison.Ordinal) < text.IndexOf("b/two", StringComparison.Ordinal));
    }

    [Fact]
    public void Json_WritesNotAvailableAsNull()
    {
        string text = BaselineJson.Render(Report());

        // Her seye negatif tabaninda precision N/A; JSON'da null olarak duruyor.
        Assert.Contains("\"precision\": null", text, StringComparison.Ordinal);
    }

    private static BaselineReport Report()
    {
        IReadOnlyList<RepositorySplit> repositories =
        [
            BaselineTests.Split("a/one", 2, 8, 2, 8),
            BaselineTests.Split("b/two", 3, 7, 3, 7),
        ];

        return BaselineRunner.Run(repositories, "snapshot-ozeti", "manifest-ozeti", "commit", repeats: 5);
    }

    private static string Write(string text)
    {
        string path = Path.Combine(Path.GetTempPath(), "sievert-taban-" + Guid.NewGuid().ToString("n") + ".csv");
        File.WriteAllText(path, text);

        return path;
    }
}
