using Microsoft.EntityFrameworkCore;

using Sievert.Core.Mining;
using Sievert.Data;
using Sievert.Data.Entities;

namespace Sievert.Tests;

[Collection(PostgresCollection.Name)]
public class CommitStoreTests(PostgresFixture postgres)
{
    [DockerFact]
    public void WritingTheSameRepositoryTwice_AddsNothingTheSecondTime()
    {
        using SievertContext context = postgres.NewDatabase();
        CommitStore store = new(context);
        StoreOptions options = new("polly", "https://example.com/polly.git", "abc1234", Rewrite: false);

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

        store.Write(Commits(3), new StoreOptions("polly", null, "abc1234", Rewrite: false));
        StoreResult again = store.Write(Commits(2), new StoreOptions("polly", null, "abc1234", Rewrite: true));

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
            store.Write(FailsAfter(2), new StoreOptions("polly", null, "abc1234", Rewrite: false)));

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

        store.Write(Commits(3), new StoreOptions("polly", null, "abc1234", Rewrite: false));

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

        store.Write(Commits(4), new StoreOptions("polly", "https://example.com/polly.git", "abc1234", Rewrite: false));

        RepositoryRow repository = context.Repositories.Single();

        Assert.Equal(4, repository.TotalCommits);
        Assert.Equal("abc1234", repository.ScannedSha);
        Assert.NotNull(repository.FirstCommitDate);
        Assert.NotNull(repository.LastCommitDate);
        Assert.True(repository.FirstCommitDate <= repository.LastCommitDate);
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
}
