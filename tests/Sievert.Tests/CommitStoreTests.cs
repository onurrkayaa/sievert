using Microsoft.EntityFrameworkCore;

using Sievert.Core.Mining;
using Sievert.Data;
using Sievert.Data.Entities;
using Sievert.Data.Metrics;

namespace Sievert.Tests;

[Collection(PostgresCollection.Name)]
public class CommitStoreTests(PostgresFixture postgres)
{
    [DockerFact]
    public void WritingTheSameRepositoryTwice_AddsNothingTheSecondTime()
    {
        using SievertContext context = postgres.NewDatabase();
        CommitStore store = new(context);
        StoreOptions options = Options();

        StoreResult first = store.Write(Commits(3), options);
        StoreResult second = store.Write(Commits(3), options);

        Assert.Equal(3, first.Written);
        Assert.Equal(0, first.Skipped);
        Assert.Equal(0, second.Written);
        Assert.Equal(3, second.Skipped);
        Assert.Equal(3, context.Commits.Count());
        Assert.Single(context.Repositories);
    }

    [DockerFact]
    public void RewriteDeletesTheOldRowsAndWritesAgain()
    {
        using SievertContext context = postgres.NewDatabase();
        CommitStore store = new(context);

        store.Write(Commits(3), Options());
        StoreResult again = store.Write(Commits(2), Options(rewrite: true));

        Assert.Equal(3, again.Deleted);
        Assert.Equal(2, again.Written);
        Assert.Equal(2, context.Commits.Count());

        // Basamakli silme dosya satirlarini da goturmus olmali.
        Assert.Equal(2, context.CommitFiles.Count());
    }

    [DockerFact]
    public void AnInterruptedWrite_LeavesNothingBehind()
    {
        using SievertContext context = postgres.NewDatabase();
        CommitStore store = new(context);

        Assert.Throws<InvalidOperationException>(() =>
            store.Write(FailsAfter(2), Options()));

        context.ChangeTracker.Clear();

        // Islem geri alindigi icin depo satiri bile kalmamali.
        Assert.Empty(context.Commits);
        Assert.Empty(context.CommitFiles);
        Assert.Empty(context.Repositories);
    }

    [DockerFact]
    public void PathsGoInRelative()
    {
        using SievertContext context = postgres.NewDatabase();
        CommitStore store = new(context);

        store.Write(Commits(3), Options());

        List<string> paths = [.. context.CommitFiles.Select(row => row.Path)];

        Assert.NotEmpty(paths);
        Assert.DoesNotContain(paths, path => path.StartsWith('/'));
        Assert.DoesNotContain(paths, path => path.Contains(':', StringComparison.Ordinal));
    }

    [DockerFact]
    public void TheSummaryRowIsFilledFromWhatWasWritten()
    {
        using SievertContext context = postgres.NewDatabase();
        CommitStore store = new(context);

        store.Write(Commits(4), Options());

        RepositoryRow repository = context.Repositories.Single();

        Assert.Equal(4, repository.TotalCommits);
        Assert.Equal("abc1234", repository.ScannedSha);
        Assert.NotNull(repository.FirstCommitDate);
        Assert.NotNull(repository.LastCommitDate);
        Assert.True(repository.FirstCommitDate <= repository.LastCommitDate);
    }

    [DockerFact]
    public void TheSameRepositoryUnderTwoFolderNames_IsOneRow()
    {
        using SievertContext context = postgres.NewDatabase();
        CommitStore store = new(context);

        // Ayni uzak adres, iki farkli klasor adi ve iki farkli yazim bicimi.
        store.Write(Commits(3), Options(folder: "polly-full", remote: "https://github.com/App-vNext/Polly.git"));
        StoreResult second = store.Write(Commits(3), Options(folder: "polly", remote: "git@github.com:App-vNext/Polly"));

        Assert.Single(context.Repositories);
        Assert.Equal(0, second.Written);
        Assert.Equal(3, second.Skipped);
        Assert.Equal(3, context.Commits.Count());
        Assert.Equal("remote", context.Repositories.Single().IdentitySource);
    }

    [DockerFact]
    public void WithoutARemote_TheIdentityFallsBackToTheFolderAndSaysSo()
    {
        using SievertContext context = postgres.NewDatabase();
        CommitStore store = new(context);

        store.Write(Commits(2), Options(remote: null));

        Assert.Equal("folder", context.Repositories.Single().IdentitySource);
    }

    /// <summary>
    /// CLI'in yaptigi kimlik turetmesinin aynisi: uzak adres varsa normalize edilmis
    /// hâli, yoksa klasor adi.
    /// </summary>
    private static StoreOptions Options(
        string folder = "polly",
        string? remote = "https://github.com/App-vNext/Polly.git",
        bool rewrite = false) =>
        Sievert.Mining.RemoteIdentity.Normalize(remote) is string identity
            ? new StoreOptions(identity, "remote", folder, remote, "abc1234", rewrite)
            : new StoreOptions(folder, "folder", folder, null, "abc1234", rewrite);

    /// <summary>Hepsi ayni dosyaya dokunan commit'ler; gecmis metrikleri icin.</summary>
    private static IEnumerable<CommitRecord> SameFileCommits(int count)
    {
        foreach (CommitRecord commit in Commits(count))
        {
            yield return commit with
            {
                Files = [new FileChange("src/Ayni.cs", null, 10, 2, FileChangeKind.Modified)],
            };
        }
    }

    /// <summary>Ikinci commit'ten sonra patlayan bir akis; yarida kesilmeyi taklit ediyor.</summary>
    private static IEnumerable<CommitRecord> FailsAfter(int count)
    {
        int written = 0;

        foreach (CommitRecord commit in Commits(10))
        {
            if (written++ == count)
            {
                throw new InvalidOperationException("akis yarida kesildi");
            }

            yield return commit;
        }
    }

    private static IEnumerable<CommitRecord> Commits(int count)
    {
        for (int i = 0; i < count; i++)
        {
            yield return new CommitRecord(
                Sha: $"{i:x40}",
                ShortSha: $"{i:x7}",
                AuthorName: "Onur",
                AuthorEmail: "onur@example.com",
                AuthorDateUtc: new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero).AddDays(i),
                MessageSubject: $"commit {i}",
                Message: $"commit {i}\n\nCo-Authored-By: Ayse <ayse@example.com>\n",
                ParentCount: 1,
                AuthorLooksLikeBot: false,
                CoAuthors: [new CoAuthor("Ayse", "ayse@example.com")],
                Files: [new FileChange($"src/Dosya{i}.cs", null, 10, 2, FileChangeKind.Modified)],
                Summary: new CommitChangeSummary(10, 2, 1, 1));
        }
    }

    [DockerFact]
    public void MetricsAreWrittenForEveryCommitAndRecomputingGivesTheSameRows()
    {
        using SievertContext context = postgres.NewDatabase();
        new CommitStore(context).Write(Commits(5), Options());

        int repositoryId = context.Repositories.Single().Id;
        MetricsRunner runner = new(context);

        MetricsResult first = runner.Run(repositoryId, MetricOptions.Default);
        List<int> firstPrior = [.. context.CommitMetrics.OrderBy(row => row.CommitId).Select(row => row.PriorChanges)];

        MetricsResult second = runner.Run(repositoryId, MetricOptions.Default);
        List<int> secondPrior = [.. context.CommitMetrics.OrderBy(row => row.CommitId).Select(row => row.PriorChanges)];

        Assert.Equal(5, first.CommitCount);
        Assert.Equal(5, second.CommitCount);

        // Yeniden hesaplama eski satirlari silip yeniden yaziyor, ikiye katlamiyor.
        Assert.Equal(5, context.CommitMetrics.Count());
        Assert.Equal(firstPrior, secondPrior);
    }

    [DockerFact]
    public void MetricsReadCommitsInDateOrderSoHistoryGrows()
    {
        using SievertContext context = postgres.NewDatabase();
        new CommitStore(context).Write(SameFileCommits(4), Options());

        int repositoryId = context.Repositories.Single().Id;
        new MetricsRunner(context).Run(repositoryId, MetricOptions.Default);

        // Dort commit de ayni dosyaya dokunuyor, yani tarih sirasinda PriorChanges
        // 0, 1, 2, 3 olmali. Sira bozulsaydi bu dizi bozulurdu.
        List<int> prior =
        [
            .. context.CommitMetrics
                .Join(context.Commits, metric => metric.CommitId, commit => commit.Id, (metric, commit) => new
                {
                    commit.AuthorDateUtc,
                    metric.PriorChanges,
                })
                .OrderBy(row => row.AuthorDateUtc)
                .Select(row => row.PriorChanges),
        ];

        Assert.Equal([0, 1, 2, 3], prior);
    }
}
