using System.Globalization;

using Sievert.Modeling;

namespace Sievert.Tests;

/// <summary>
/// Zaman sirali bolmenin ve dondurulmus veri kumesini okumanin testleri. Hepsi kendi
/// gecici dosyasiyla calisiyor; repodaki 34 166 satirlik anlik goruntuye dokunmuyorlar.
/// </summary>
public class TimeOrderedSplitTests
{
    private const string Header =
        "Repository,RepositoryIdentity,Sha,AuthorDateUtc,"
        + "LinesAdded,LinesDeleted,FilesChanged,CsFilesChanged,Entropy,"
        + "DirectoryCount,SubsystemCount,MaxFileAgeDays,MinFileAgeDays,"
        + "PriorChanges,PriorFixes,DistinctAuthorsOnFiles,"
        + "AuthorCommitCount,AuthorFileExperience,IsFix,"
        + "IsBugIntroducing,LabelSource,BotMu";

    // --- Bolme siniri ---

    [Theory]
    [InlineData(2759, 1931, 828)]
    [InlineData(8490, 5943, 2547)]
    [InlineData(22917, 16041, 6876)]
    [InlineData(10, 7, 3)]
    [InlineData(1, 0, 1)]
    public void Split_TrainIsTheFirstFloorOfSeventyPercent(int total, int train, int test)
    {
        IReadOnlyList<SplitEntry> entries = TimeOrderedSplit.Apply(Rows("a/b", total));

        Assert.Equal(train, entries.Count(entry => entry.Split == SplitEntry.Train));
        Assert.Equal(test, entries.Count(entry => entry.Split == SplitEntry.Test));
    }

    [Fact]
    public void Split_EachRepositoryIsSplitOnItsOwn()
    {
        List<SnapshotRow> rows = [.. Rows("a/one", 10), .. Rows("a/two", 100)];

        IReadOnlyList<SplitEntry> entries = TimeOrderedSplit.Apply(rows);

        Assert.Equal(7, entries.Count(entry => entry.RepositoryIdentity == "a/one" && entry.Split == SplitEntry.Train));
        Assert.Equal(70, entries.Count(entry => entry.RepositoryIdentity == "a/two" && entry.Split == SplitEntry.Train));
    }

    [Fact]
    public void Split_TrainComesBeforeTestInTime()
    {
        IReadOnlyList<SplitEntry> entries = TimeOrderedSplit.Apply(Rows("a/b", 100));

        DateTimeOffset lastTrain = entries.Where(entry => entry.Split == SplitEntry.Train).Max(entry => entry.AuthorDateUtc);
        DateTimeOffset firstTest = entries.Where(entry => entry.Split == SplitEntry.Test).Min(entry => entry.AuthorDateUtc);

        Assert.True(lastTrain <= firstTest);
    }

    [Fact]
    public void Split_TrainAndTestShareNoKey()
    {
        IReadOnlyList<SplitEntry> entries = TimeOrderedSplit.Apply([.. Rows("a/one", 40), .. Rows("a/two", 40)]);

        HashSet<string> train =
        [
            .. entries.Where(entry => entry.Split == SplitEntry.Train)
                .Select(entry => entry.RepositoryIdentity + " " + entry.Sha)
        ];

        HashSet<string> test =
        [
            .. entries.Where(entry => entry.Split == SplitEntry.Test)
                .Select(entry => entry.RepositoryIdentity + " " + entry.Sha)
        ];

        Assert.Empty(train.Intersect(test));
        Assert.Equal(80, train.Count + test.Count);
    }

    // --- Siralama ---

    [Fact]
    public void Split_RowsComeOutInDateOrderEvenWhenTheFileIsShuffled()
    {
        string path = Csv(
            Line("a/b", "cc", "2020-03-01T00:00:00Z"),
            Line("a/b", "aa", "2020-01-01T00:00:00Z"),
            Line("a/b", "bb", "2020-02-01T00:00:00Z"));

        IReadOnlyList<SplitEntry> entries = TimeOrderedSplit.Apply(SnapshotReader.Read(path));

        Assert.Equal(["aa", "bb", "cc"], entries.Select(entry => entry.Sha));
    }

    [Fact]
    public void Split_EqualTimestampsAreOrderedByShaOrdinal()
    {
        // Ayni saniyede iki commit. Ordinal siralamada buyuk harf kucuk harften once
        // gelir; kulture duyarli bir karsilastirma tersini soylerdi, o yuzden test
        // bilerek harf buyuklugu karisik.
        string path = Csv(
            Line("a/b", "aa", "2020-01-01T00:00:00Z"),
            Line("a/b", "AB", "2020-01-01T00:00:00Z"),
            Line("a/b", "Aa", "2020-01-01T00:00:00Z"));

        IReadOnlyList<SplitEntry> entries = TimeOrderedSplit.Apply(SnapshotReader.Read(path));

        Assert.Equal(["AB", "Aa", "aa"], entries.Select(entry => entry.Sha));
    }

    [Fact]
    public void Split_RepositoriesAreOrderedOrdinally()
    {
        IReadOnlyList<SplitEntry> entries = TimeOrderedSplit.Apply([.. Rows("b/one", 2), .. Rows("a/two", 2)]);

        Assert.Equal(["a/two", "a/two", "b/one", "b/one"], entries.Select(entry => entry.RepositoryIdentity));
    }

    // --- Manifest ---

    [Fact]
    public void Manifest_SameInputGivesTheSameBytes()
    {
        string path = Csv(
            Line("a/b", "cc", "2020-03-01T00:00:00Z"),
            Line("a/b", "aa", "2020-01-01T00:00:00Z"),
            Line("a/b", "bb", "2020-02-01T00:00:00Z"));

        string first = SplitManifest.Render(TimeOrderedSplit.Apply(SnapshotReader.Read(path)));
        string second = SplitManifest.Render(TimeOrderedSplit.Apply(SnapshotReader.Read(path)));

        Assert.Equal(first, second);
    }

    [Fact]
    public void Manifest_CarriesOnlyTheFourAgreedColumns()
    {
        string text = SplitManifest.Render(TimeOrderedSplit.Apply(Rows("a/b", 4)));

        string[] lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        Assert.Equal("RepositoryIdentity,Sha,Split,AuthorDateUtc", lines[0]);
        Assert.Equal(4, lines[1].Split(',').Length);
        Assert.Equal(5, lines.Length);
    }

    // --- Oznitelik sozlesmesi ---

    [Fact]
    public void Features_AreTheFifteenCandidates()
    {
        Assert.Equal(15, ModelFeatures.Candidates.Count);
        Assert.Equal(ModelFeatures.Candidates.Count, ModelFeatures.Values(Row("a/b", "aa", DateTimeOffset.UnixEpoch)).Count);
    }

    [Theory]
    [InlineData("IsBugIntroducing")]
    [InlineData("LabelSource")]
    [InlineData("Sha")]
    [InlineData("Repository")]
    [InlineData("RepositoryIdentity")]
    [InlineData("AuthorDateUtc")]
    [InlineData("BotMu")]
    [InlineData("IsBot")]
    public void Features_ExcludedFieldsAreNotCandidates(string name)
    {
        Assert.DoesNotContain(name, ModelFeatures.Candidates);

        InvalidOperationException error = Assert.Throws<InvalidOperationException>(
            () => ModelFeatures.EnsureNoExcluded([.. ModelFeatures.Candidates, name]));

        Assert.Contains(name, error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Features_TheCandidateListItselfPasses()
    {
        ModelFeatures.EnsureNoExcluded(ModelFeatures.Candidates);
    }

    // --- Okuma ---

    [Fact]
    public void Reader_MapsTheBotMuColumnOntoIsBot()
    {
        string path = Csv(Line("a/b", "aa", "2020-01-01T00:00:00Z", bot: "1"));

        SnapshotRow row = SnapshotReader.Read(path).Single();

        Assert.True(row.IsBot);
    }

    [Fact]
    public void Reader_RejectsAHeaderThatRenamedBotMu()
    {
        string path = Write(Header.Replace("BotMu", "IsBot", StringComparison.Ordinal) + "\n");

        InvalidDataException error = Assert.Throws<InvalidDataException>(() => SnapshotReader.Read(path));

        Assert.Contains("BotMu", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Reader_HandlesQuotedFields()
    {
        string line = "\"repo,adi\",a/b,aa,2020-01-01T00:00:00Z,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,\"say \"\"szz\"\"\",0";
        string path = Write(Header + "\n" + line + "\n");

        SnapshotRow row = SnapshotReader.Read(path).Single();

        Assert.Equal("repo,adi", row.Repository);
        Assert.Equal("say \"szz\"", row.LabelSource);
    }

    [Fact]
    public void Reader_ReadsAnEmptyLabelSourceAsUnlabelled()
    {
        SnapshotRow row = SnapshotReader.Read(Csv(Line("a/b", "aa", "2020-01-01T00:00:00Z"))).Single();

        Assert.Null(row.LabelSource);
    }

    [Theory]
    [InlineData("")]
    [InlineData("NaN")]
    [InlineData("Infinity")]
    [InlineData("-Infinity")]
    public void Reader_RejectsMissingNotANumberAndInfiniteValues(string entropy)
    {
        string path = Csv(Line("a/b", "aa", "2020-01-01T00:00:00Z", entropy: entropy));

        InvalidDataException error = Assert.Throws<InvalidDataException>(() => SnapshotReader.Read(path));

        Assert.Contains("Entropy", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Reader_RejectsARowWithTooFewFields()
    {
        string path = Write(Header + "\na/b,aa,2020-01-01T00:00:00Z\n");

        Assert.Throws<InvalidDataException>(() => SnapshotReader.Read(path));
    }

    [Theory]
    [InlineData("true")]
    [InlineData("")]
    [InlineData("2")]
    public void Reader_RejectsATargetThatIsNeitherZeroNorOne(string target)
    {
        string path = Csv(Line("a/b", "aa", "2020-01-01T00:00:00Z", target: target));

        InvalidDataException error = Assert.Throws<InvalidDataException>(() => SnapshotReader.Read(path));

        Assert.Contains("IsBugIntroducing", error.Message, StringComparison.Ordinal);
    }

    // --- Checksum ---

    [Fact]
    public void Checksum_ReadsTheFileWhenTheRecordedValueMatches()
    {
        string path = Csv(Line("a/b", "aa", "2020-01-01T00:00:00Z"));
        string checksum = Write(FileChecksum.Sha256(path) + "  " + Path.GetFileName(path) + "\n");

        Assert.Single(SnapshotReader.ReadVerified(path, checksum));
    }

    [Fact]
    public void Checksum_RefusesAFileThatDoesNotMatchTheRecordedValue()
    {
        string path = Csv(Line("a/b", "aa", "2020-01-01T00:00:00Z"));
        string checksum = Write(new string('0', 64) + "  " + Path.GetFileName(path) + "\n");

        InvalidDataException error = Assert.Throws<InvalidDataException>(() => SnapshotReader.ReadVerified(path, checksum));

        Assert.Contains("SHA-256", error.Message, StringComparison.Ordinal);
    }

    // --- Yardimcilar ---

    private static IReadOnlyList<SnapshotRow> Rows(string identity, int count) =>
    [
        .. Enumerable.Range(0, count).Select(index => Row(
            identity,
            index.ToString("x8", CultureInfo.InvariantCulture),
            DateTimeOffset.UnixEpoch.AddDays(index)))
    ];

    private static SnapshotRow Row(string identity, string sha, DateTimeOffset date) =>
        new(identity, identity, sha, date, 0, 0, 0, 0, 0.0, 0, 0, 0, 0, 0, 0, 0, 0, 0, false, false, null, false);

    private static string Line(
        string identity,
        string sha,
        string date,
        string entropy = "0",
        string target = "0",
        string bot = "0") =>
        $"klasor,{identity},{sha},{date},0,0,0,0,{entropy},0,0,0,0,0,0,0,0,0,0,{target},,{bot}";

    private static string Csv(params string[] lines) =>
        Write(Header + "\n" + string.Join('\n', lines) + "\n");

    private static string Write(string text)
    {
        string path = Path.Combine(Path.GetTempPath(), "sievert-split-" + Guid.NewGuid().ToString("n") + ".csv");
        File.WriteAllText(path, text);

        return path;
    }
}
