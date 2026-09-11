using Sievert.Data.Metrics;

namespace Sievert.Tests;

public class MetricCalculatorTests
{
    [Fact]
    public void ASingleFileCommit_HasZeroEntropy() =>
        Assert.Equal(0.0, Only(Commit(1, "onur@example.com", 0, "ilk", File("a.cs", 10, 0))).Entropy, 6);

    [Fact]
    public void TwoFilesChangedEqually_HaveEntropyOne() =>
        // Iki esit parca: -2 * (0.5 * log2(0.5)) = 1.0
        Assert.Equal(
            1.0,
            Only(Commit(1, "onur@example.com", 0, "iki dosya", File("a.cs", 5, 0), File("b.cs", 5, 0))).Entropy,
            6);

    [Fact]
    public void ACommitThatChangesNoLines_HasZeroEntropy() =>
        Assert.Equal(0.0, Only(Commit(1, "onur@example.com", 0, "bos")).Entropy, 6);

    [Theory]
    [InlineData("Fix: null reference", true)]
    [InlineData("hata duzeltildi", true)]
    [InlineData("resolve the crash on startup", true)]
    [InlineData("BUG in the parser", true)]
    [InlineData("prefix the header", false)]
    [InlineData("add a suffix to the name", false)]
    [InlineData("refactor the reader", false)]
    public void IsFix_MatchesWholeWordsOnly(string subject, bool expected) =>
        Assert.Equal(expected, Only(Commit(1, "onur@example.com", 0, subject, File("a.cs", 1, 0))).IsFix);

    [Fact]
    public void TheFirstCommit_HasEmptyHistoryAndDoesNotCrash()
    {
        CommitMetrics metrics = Only(Commit(1, "onur@example.com", 0, "ilk", File("src/a.cs", 3, 0)));

        Assert.Equal(0, metrics.PriorChanges);
        Assert.Equal(0, metrics.PriorFixes);
        Assert.Equal(0, metrics.DistinctAuthorsOnFiles);
        Assert.Equal(0, metrics.AuthorCommitCount);
        Assert.Equal(0, metrics.AuthorFileExperience);
        Assert.Equal(0, metrics.MaxFileAgeDays);
        Assert.Equal(0, metrics.MinFileAgeDays);
    }

    [Fact]
    public void PriorChanges_CountsOnlyEarlierCommits()
    {
        List<CommitMetrics> metrics = All(
            Commit(1, "onur@example.com", 0, "bir", File("a.cs", 1, 0)),
            Commit(2, "onur@example.com", 1, "iki", File("a.cs", 1, 0)),
            Commit(3, "ayse@example.com", 2, "uc", File("a.cs", 1, 0)));

        Assert.Equal(0, metrics[0].PriorChanges);
        Assert.Equal(1, metrics[1].PriorChanges);
        Assert.Equal(2, metrics[2].PriorChanges);
    }

    [Fact]
    public void ALaterCommit_DoesNotChangeAnEarlierCommitsMetrics()
    {
        // Zaman sizintisi testi 1: ayni tarihin ilk iki commit'i, ucuncusu eklenince
        // metrikleri degismemeli.
        CommitForMetrics first = Commit(1, "onur@example.com", 0, "bir", File("a.cs", 1, 0));
        CommitForMetrics second = Commit(2, "onur@example.com", 1, "iki", File("a.cs", 1, 0));
        CommitForMetrics third = Commit(3, "ayse@example.com", 2, "uc duzeltme fix", File("a.cs", 1, 0));

        List<CommitMetrics> without = All(first, second);
        List<CommitMetrics> with = All(first, second, third);

        Assert.Equal(without[0], with[0]);
        Assert.Equal(without[1], with[1]);
    }

    [Fact]
    public void AFutureFixDoesNotCountAsAPriorFix()
    {
        // Zaman sizintisi testi 2: duzeltme SONRA geliyor, onceki commit'in PriorFixes'i
        // 0 kalmali. Sizinti olsaydi model "bu dosya duzeltildi" bilgisini gecmise tasirdi.
        List<CommitMetrics> metrics = All(
            Commit(1, "onur@example.com", 0, "ozellik ekle", File("a.cs", 1, 0)),
            Commit(2, "onur@example.com", 1, "fix the thing", File("a.cs", 1, 0)),
            Commit(3, "onur@example.com", 2, "baska bir sey", File("a.cs", 1, 0)));

        Assert.Equal(0, metrics[0].PriorFixes);
        Assert.Equal(0, metrics[1].PriorFixes);
        Assert.Equal(1, metrics[2].PriorFixes);
    }

    [Fact]
    public void CalculatingTwice_GivesTheSameResult()
    {
        CommitForMetrics[] commits =
        [
            Commit(1, "onur@example.com", 0, "bir", File("src/a.cs", 3, 1)),
            Commit(2, "ayse@example.com", 5, "fix bir sey", File("src/a.cs", 1, 1), File("test/b.cs", 2, 0)),
        ];

        Assert.Equal(All(commits), All(commits));
    }

    [Fact]
    public void DirectoriesAndSubsystemsAreCountedFromThePath()
    {
        CommitMetrics metrics = Only(Commit(
            1,
            "onur@example.com",
            0,
            "cok yerde",
            File("src/Core/a.cs", 1, 0),
            File("src/Cli/b.cs", 1, 0),
            File("tests/c.cs", 1, 0)));

        Assert.Equal(3, metrics.DirectoryCount);

        // Alt sistem yolun ilk bileseni: src ve tests.
        Assert.Equal(2, metrics.SubsystemCount);
    }

    [Fact]
    public void AuthorExperienceCountsEarlierTouchesOnTheSameFiles()
    {
        List<CommitMetrics> metrics = All(
            Commit(1, "onur@example.com", 0, "bir", File("a.cs", 1, 0)),
            Commit(2, "onur@example.com", 1, "iki", File("a.cs", 1, 0)),
            Commit(3, "ayse@example.com", 2, "uc", File("a.cs", 1, 0)));

        Assert.Equal(0, metrics[0].AuthorFileExperience);
        Assert.Equal(1, metrics[1].AuthorFileExperience);
        Assert.Equal(1, metrics[1].AuthorCommitCount);

        // Ayse bu dosyaya hic dokunmadi ama iki kisi dokunmus.
        Assert.Equal(0, metrics[2].AuthorFileExperience);
        Assert.Equal(1, metrics[2].DistinctAuthorsOnFiles);
    }

    [Fact]
    public void FileAgeIsCountedFromTheFirstTimeTheFileWasSeen()
    {
        List<CommitMetrics> metrics = All(
            Commit(1, "onur@example.com", 0, "ekle", File("a.cs", 1, 0)),
            Commit(2, "onur@example.com", 10, "degistir", File("a.cs", 1, 0)));

        Assert.Equal(10, metrics[1].MaxFileAgeDays);
        Assert.Equal(10, metrics[1].MinFileAgeDays);
    }

    private static FileForMetrics File(string path, int added, int deleted) =>
        new(path, null, added, deleted, IsRename: false, path.EndsWith(".cs", StringComparison.Ordinal));

    private static CommitForMetrics Commit(
        int id,
        string email,
        int dayOffset,
        string subject,
        params FileForMetrics[] files) =>
        new(id, email, new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero).AddDays(dayOffset), subject, files);

    private static CommitMetrics Only(CommitForMetrics commit) => All(commit)[0];

    private static List<CommitMetrics> All(params CommitForMetrics[] commits) =>
        [.. new MetricCalculator(MetricOptions.Default).Compute(commits)];
}
