using Sievert.Data;
using Sievert.Data.Entities;

namespace Sievert.Tests;

/// <summary>API testleri icin kucuk ve tam kontrol edilebilir bir veri kumesi.</summary>
public static class ApiSeed
{
    public const string KnownIdentity = "github.com/app-vnext/polly";

    public const string UnknownIdentity = "github.com/bilinmeyen/depo";

    /// <summary>Iki depo yazar ve kimliklerine gore id'lerini verir.</summary>
    public static (int Known, int Unknown) Write(SievertContext database, string? localPath = null)
    {
        RepositoryRow known = NewRepository(KnownIdentity, "polly");
        RepositoryRow unknown = NewRepository(UnknownIdentity, "depo");

        known.LocalPath = localPath;
        unknown.LocalPath = localPath;

        database.Repositories.AddRange(known, unknown);
        database.SaveChanges();

        for (int index = 0; index < 5; index++)
        {
            AddCommit(database, known.Id, index);
        }

        AddCommit(database, unknown.Id, 0);
        database.SaveChanges();

        return (known.Id, unknown.Id);
    }

    /// <summary>Olcusu hesaplanmamis bir commit ekler ve sha'sini verir.</summary>
    public static string AddCommitWithoutMetrics(SievertContext database, int repositoryId)
    {
        CommitRow commit = new()
        {
            RepositoryId = repositoryId,
            Sha = ShaFor(90),
            AuthorName = "Yazar 90",
            AuthorEmail = "yazar90@ornek.test",
            AuthorDateUtc = new DateTimeOffset(2020, 2, 1, 0, 0, 0, TimeSpan.Zero),
            MessageSubject = "olcusu hesaplanmamis commit",
            MessageFull = "govde",
            ParentCount = 1,
        };

        database.Commits.Add(commit);
        database.SaveChanges();

        return commit.Sha;
    }

    /// <summary>Sha'lar sayilabilir olsun diye sabit bir kaliptan uretiliyor.</summary>
    public static string ShaFor(int index) => index.ToString("x2").PadLeft(2, '0') + new string('a', 38);

    private static RepositoryRow NewRepository(string identity, string name) => new()
    {
        Identity = identity,
        IdentitySource = "remote",
        Name = name,
        RemoteUrl = "https://" + identity,
        TotalCommits = 5,
        FirstCommitDate = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero),
        LastCommitDate = new DateTimeOffset(2020, 1, 5, 0, 0, 0, TimeSpan.Zero),
        ScannedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
        ScannedSha = ShaFor(0),
    };

    private static void AddCommit(SievertContext database, int repositoryId, int index)
    {
        CommitRow commit = new()
        {
            RepositoryId = repositoryId,
            Sha = ShaFor(index),
            AuthorName = "Yazar " + index,
            AuthorEmail = $"yazar{index}@ornek.test",
            AuthorDateUtc = new DateTimeOffset(2020, 1, 1 + index, 0, 0, 0, TimeSpan.Zero),
            MessageSubject = index % 2 == 0 ? "ozellik ekle" : "hatayi duzelt",
            MessageFull = "govde",
            ParentCount = 1,
            IsBot = index == 4,
            LinesAdded = 10 + index,
            LinesDeleted = 2 + index,
            ChangedFiles = 1 + index,
            ChangedCSharpFiles = index == 3 ? 0 : 1 + index,
            IsBugIntroducing = index == 1,
            LabelSource = "szz",
        };

        database.Commits.Add(commit);
        database.SaveChanges();

        database.CommitMetrics.Add(new CommitMetricRow
        {
            CommitId = commit.Id,
            LinesAdded = commit.LinesAdded,
            LinesDeleted = commit.LinesDeleted,
            FilesChanged = commit.ChangedFiles,
            CsFilesChanged = commit.ChangedCSharpFiles,
            Entropy = 0.5,
            DirectoryCount = 1,
            SubsystemCount = 1,
            MaxFileAgeDays = 30 + index,
            MinFileAgeDays = 1 + index,
            PriorChanges = 4 + index,
            PriorFixes = 1,
            DistinctAuthorsOnFiles = 2,
            AuthorCommitCount = index,
            AuthorFileExperience = index,
            IsFix = index % 2 == 1,
        });

        database.SaveChanges();
    }
}
