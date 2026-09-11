using Sievert.Core.Mining;

namespace Sievert.Mining;

/// <summary>
/// Akip giden commit'lerden ozeti biriktirir. Commit listesi hicbir zaman bellekte
/// tutulmuyor; burada tutulan tek buyuyen sey farkli epostalar kumesi.
/// </summary>
public sealed class MiningTally(int renameSimilarityThreshold)
{
    private readonly HashSet<string> authors = new(StringComparer.OrdinalIgnoreCase);

    private int commitCount;
    private int botCommitCount;
    private int coAuthorLineCount;
    private DateTimeOffset? first;
    private DateTimeOffset? last;

    public void Add(CommitRecord commit)
    {
        commitCount++;
        authors.Add(commit.AuthorEmail);
        coAuthorLineCount += commit.CoAuthors.Count;

        if (commit.AuthorLooksLikeBot)
        {
            botCommitCount++;
        }

        if (first is null || commit.AuthorDateUtc < first)
        {
            first = commit.AuthorDateUtc;
        }

        if (last is null || commit.AuthorDateUtc > last)
        {
            last = commit.AuthorDateUtc;
        }
    }

    public MiningSummary Build(int skippedMergeCount) =>
        new(
            commitCount,
            skippedMergeCount,
            first,
            last,
            authors.Count,
            botCommitCount,
            coAuthorLineCount,
            renameSimilarityThreshold);
}
