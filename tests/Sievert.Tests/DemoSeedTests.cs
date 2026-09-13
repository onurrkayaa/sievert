using System.Text;
using System.Text.Json;

using Microsoft.EntityFrameworkCore;

using Sievert.Data;
using Sievert.Data.Entities;
using Sievert.Demo;

namespace Sievert.Tests;

/// <summary>
/// Demo veri kumesinin ve ice aktarmanin testleri.
///
/// Iki soru var: dosyadaki veri gercekten temiz mi (yazar, yol, sir yok mu) ve ayni
/// dosyayi iki kez ice aktarmak veritabanini bozuyor mu.
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class DemoSeedTests(PostgresFixture postgres)
{
    private static readonly string SeedDirectory = Path.Combine(
        DemoOrchestrator.FindRepositoryRoot(Directory.GetCurrentDirectory())
            ?? throw new InvalidOperationException("Depo koku bulunamadi."),
        "data",
        "asama6",
        "demo");

    [Fact]
    public void TheSeedMatchesItsOwnChecksum()
    {
        string expected = File.ReadAllText(Path.Combine(SeedDirectory, "demo-seed.sha256")).Split(' ')[0].Trim();

        Assert.Equal(expected, SeedExporter.Checksum(Path.Combine(SeedDirectory, "demo-seed.json")));
    }

    [Fact]
    public void TheSourceManifestMatchesItsOwnChecksum()
    {
        string expected = File.ReadAllText(
            Path.Combine(SeedDirectory, "source-manifest.sha256")).Split(' ')[0].Trim();

        Assert.Equal(expected, SeedExporter.Checksum(Path.Combine(SeedDirectory, "source-manifest.json")));
    }

    [Fact]
    public void TheSeedHoldsTheDeclaredNumberOfCommits()
    {
        DemoSeedFile seed = Seed();

        Assert.Equal(DemoSelection.CommitCount, seed.Commits.Count);
        Assert.Equal(seed.Commits.Count, seed.Metrics.Count);
        Assert.Equal(seed.Commits.Count, seed.Snapshots.Count);
        Assert.NotEmpty(seed.Files);
        Assert.NotNull(seed.StaticJob);
        Assert.NotEmpty(seed.Findings);
    }

    [Fact]
    public void TheSourceManifestAgreesWithTheSeed()
    {
        DemoSeedFile seed = Seed();

        using JsonDocument manifest = JsonDocument.Parse(
            File.ReadAllText(Path.Combine(SeedDirectory, "source-manifest.json")));

        JsonElement root = manifest.RootElement;

        Assert.Equal(seed.Commits.Count, root.GetProperty("commitCount").GetInt32());
        Assert.Equal(seed.Files.Count, root.GetProperty("commitFileCount").GetInt32());
        Assert.Equal(seed.Metrics.Count, root.GetProperty("metricCount").GetInt32());
        Assert.Equal(seed.Snapshots.Count, root.GetProperty("riskSnapshotCount").GetInt32());
        Assert.Equal(seed.Findings.Count, root.GetProperty("staticFindingCount").GetInt32());
        Assert.Equal(seed.Commits[0].Sha, root.GetProperty("firstCommitSha").GetString());
        Assert.Equal(seed.Commits[^1].Sha, root.GetProperty("lastCommitSha").GetString());
        Assert.Equal(DemoSelection.Rule, root.GetProperty("selectionRule").GetString());
    }

    [Fact]
    public void TheSeedCarriesNoAuthorIdentity()
    {
        string text = File.ReadAllText(Path.Combine(SeedDirectory, "demo-seed.json"));

        Assert.DoesNotContain("authorName", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("authorEmail", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotMatch(@"[A-Za-z0-9._%-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}", text);
    }

    [Fact]
    public void TheSeedCarriesNoPathOrSecret()
    {
        string text = File.ReadAllText(Path.Combine(SeedDirectory, "demo-seed.json"));

        Assert.DoesNotContain("localPath", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Password=", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Host=", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("/Users/", text, StringComparison.Ordinal);
        Assert.DoesNotContain("C:\\", text, StringComparison.Ordinal);
    }

    [Fact]
    public void TheSeedIsUtf8WithoutBomAndUsesLineFeeds()
    {
        byte[] bytes = File.ReadAllBytes(Path.Combine(SeedDirectory, "demo-seed.json"));

        Assert.False(bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF);
        Assert.DoesNotContain((byte)'\r', bytes);
        Assert.Equal(File.ReadAllText(Path.Combine(SeedDirectory, "demo-seed.json")),
            Encoding.UTF8.GetString(bytes), StringComparer.Ordinal);
    }

    [Fact]
    public void CommitSubjectsCarryNoControlCharacters()
    {
        foreach (DemoCommit commit in Seed().Commits)
        {
            Assert.All(commit.MessageSubject, character => Assert.False(char.IsControl(character)));
        }
    }

    [Fact]
    public void TheSeedReferencesTheModelProfileTheRepositoryActuallyHas()
    {
        DemoSeedFile seed = Seed();

        Assert.Equal(DemoSelection.RepositoryIdentity, seed.Repository.Identity);
        Assert.Equal("polly", seed.RiskJob.ModelProfile);
        Assert.All(seed.Snapshots, snapshot => Assert.Equal("polly", snapshot.ModelProfile));
    }

    [DockerFact]
    public async Task ImportingIntoAnEmptyDatabaseWritesEveryRow()
    {
        string connection = postgres.NewDatabaseConnectionString();

        await using SievertContext context = SievertContextBuilder.Create(connection);

        ImportResult result = await SeedImporter.ImportAsync(
            context, SeedDirectory, CancellationToken.None);

        Assert.True(result.Inserted);
        Assert.Equal(DemoSelection.CommitCount, await context.Commits.CountAsync(CancellationToken.None));
        Assert.Equal(result.Files, await context.CommitFiles.CountAsync(CancellationToken.None));
        Assert.Equal(result.Metrics, await context.CommitMetrics.CountAsync(CancellationToken.None));
        Assert.Equal(result.Snapshots, await context.CommitRiskSnapshots.CountAsync(CancellationToken.None));
        Assert.Equal(result.Findings, await context.StaticAnalysisFindings.CountAsync(CancellationToken.None));
    }

    [DockerFact]
    public async Task ImportingTwiceChangesNoRowCount()
    {
        string connection = postgres.NewDatabaseConnectionString();

        await using (SievertContext first = SievertContextBuilder.Create(connection))
        {
            await SeedImporter.ImportAsync(first, SeedDirectory, CancellationToken.None);
        }

        int[] before = await CountsAsync(connection);

        await using (SievertContext second = SievertContextBuilder.Create(connection))
        {
            ImportResult again = await SeedImporter.ImportAsync(
                second, SeedDirectory, CancellationToken.None);

            Assert.False(again.Inserted);
        }

        Assert.Equal(before, await CountsAsync(connection));
    }

    [DockerFact]
    public async Task TheImportedRepositoryIsMarkedAsDemoDataAndCarriesNoLocalPath()
    {
        string connection = postgres.NewDatabaseConnectionString();

        await using SievertContext context = SievertContextBuilder.Create(connection);

        await SeedImporter.ImportAsync(context, SeedDirectory, CancellationToken.None);

        RepositoryRow repository = await context.Repositories.SingleAsync(CancellationToken.None);

        Assert.True(repository.IsDemoData);
        Assert.Null(repository.LocalPath);
        Assert.Equal(DemoSelection.RepositoryIdentity, repository.Identity);
    }

    [DockerFact]
    public async Task NoImportedCommitCarriesAnAuthorIdentity()
    {
        string connection = postgres.NewDatabaseConnectionString();

        await using SievertContext context = SievertContextBuilder.Create(connection);

        await SeedImporter.ImportAsync(context, SeedDirectory, CancellationToken.None);

        Assert.Empty(await context.Commits
            .Where(row => row.AuthorEmail != string.Empty || row.AuthorName != string.Empty)
            .ToListAsync(CancellationToken.None));
    }

    [DockerFact]
    public async Task ABrokenChecksumStopsTheImportBeforeAnyRowIsWritten()
    {
        string connection = postgres.NewDatabaseConnectionString();
        string broken = Path.Combine(Path.GetTempPath(), "sievert-demo-bozuk-" + Guid.NewGuid().ToString("n"));

        Directory.CreateDirectory(broken);

        try
        {
            File.Copy(Path.Combine(SeedDirectory, "demo-seed.json"), Path.Combine(broken, "demo-seed.json"));
            File.WriteAllText(Path.Combine(broken, "demo-seed.sha256"), new string('0', 64) + "  demo-seed.json\n");

            await using SievertContext context = SievertContextBuilder.Create(connection);

            InvalidOperationException error = await Assert.ThrowsAsync<InvalidOperationException>(
                () => SeedImporter.ImportAsync(context, broken, CancellationToken.None));

            Assert.Contains("ozeti tutmuyor", error.Message, StringComparison.Ordinal);
            Assert.Equal(0, await context.Repositories.CountAsync(CancellationToken.None));
        }
        finally
        {
            Directory.Delete(broken, recursive: true);
        }
    }

    [DockerFact]
    public async Task AnUnknownSchemaVersionStopsTheImport()
    {
        string connection = postgres.NewDatabaseConnectionString();
        string folder = Path.Combine(Path.GetTempPath(), "sievert-demo-sema-" + Guid.NewGuid().ToString("n"));

        Directory.CreateDirectory(folder);

        try
        {
            DemoSeedFile seed = Seed() with { SchemaVersion = "9.9" };
            string path = Path.Combine(folder, "demo-seed.json");

            File.WriteAllText(path, JsonSerializer.Serialize(seed, DemoSeedFile.Json) + "\n", new UTF8Encoding(false));
            File.WriteAllText(
                Path.Combine(folder, "demo-seed.sha256"),
                $"{SeedExporter.Checksum(path)}  demo-seed.json\n",
                new UTF8Encoding(false));

            await using SievertContext context = SievertContextBuilder.Create(connection);

            InvalidOperationException error = await Assert.ThrowsAsync<InvalidOperationException>(
                () => SeedImporter.ImportAsync(context, folder, CancellationToken.None));

            Assert.Contains("semasi", error.Message, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    [Fact]
    public void TheOrchestratorFindsTheRepositoryRootFromADeeperDirectory()
    {
        string? root = DemoOrchestrator.FindRepositoryRoot(
            Path.Combine(SeedDirectory, "olmayan-alt-klasor"));

        Assert.NotNull(root);
        Assert.True(File.Exists(Path.Combine(root, "Sievert.slnx")));
    }

    [Theory]
    [InlineData("--no-open")]
    [InlineData("--smoke-test")]
    public void TheSmokeModeNeverOpensABrowser(string flag)
    {
        Assert.False(DemoArguments.Parse([flag]).OpenBrowser);
    }

    [Fact]
    public void ArgumentsAreParsedWithTheirDefaults()
    {
        DemoArguments defaults = DemoArguments.Parse([]);

        Assert.True(defaults.OpenBrowser);
        Assert.False(defaults.KeepDatabase);
        Assert.False(defaults.SmokeTest);
        Assert.Null(defaults.ApiPort);
        Assert.Equal(DemoArguments.DefaultStartupTimeoutSeconds, defaults.StartupTimeoutSeconds);

        DemoArguments given = DemoArguments.Parse(
            ["--api-port", "5100", "--web-port", "5101", "--startup-timeout-seconds", "30", "--keep-database"]);

        Assert.Equal(5100, given.ApiPort);
        Assert.Equal(5101, given.WebPort);
        Assert.Equal(30, given.StartupTimeoutSeconds);
        Assert.True(given.KeepDatabase);
    }

    [Fact]
    public void TwoFreePortsAreDifferent()
    {
        Assert.NotEqual(DemoProcess.FreePort(), DemoProcess.FreePort());
    }

    private static async Task<int[]> CountsAsync(string connection)
    {
        await using SievertContext context = SievertContextBuilder.Create(connection);

        return
        [
            await context.Repositories.CountAsync(CancellationToken.None),
            await context.Commits.CountAsync(CancellationToken.None),
            await context.CommitFiles.CountAsync(CancellationToken.None),
            await context.CommitMetrics.CountAsync(CancellationToken.None),
            await context.AnalysisJobs.CountAsync(CancellationToken.None),
            await context.CommitRiskSnapshots.CountAsync(CancellationToken.None),
            await context.StaticAnalysisFindings.CountAsync(CancellationToken.None),
        ];
    }

    private static DemoSeedFile Seed() => JsonSerializer.Deserialize<DemoSeedFile>(
        File.ReadAllText(Path.Combine(SeedDirectory, "demo-seed.json")), DemoSeedFile.Json)
        ?? throw new InvalidOperationException("Seed okunamadi.");
}
