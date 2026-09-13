using System.Text.Json;

using Sievert.Analysis;
using Sievert.Api;
using Sievert.Api.Analysis;
using Sievert.Data;
using Sievert.Data.Entities;
using Sievert.Mining;

namespace Sievert.Tests;

/// <summary>
/// Statik taramanin neyi taradigini kaydettigini ve temiz olmayan bir agacta hic
/// baslamadigini sinar.
///
/// Adim 3'te bu eksikti: "203 bulgu" yaziliyordu ama hangi agacta oldugu yazilmiyordu,
/// yani sonuc tekrar uretilemezdi.
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class AnalysisSourceStateTests(PostgresFixture postgres)
{
    [DockerFact]
    public async Task ACleanScanRecordsTheCommitItScanned()
    {
        using TemporaryRepository repository = new();
        string head = repository.Commit("Ornek.cs", "public class Ornek { }", "kod").Sha;

        using SievertContext context = postgres.NewDatabase();
        (AnalysisJobStore store, Guid jobId) = await StartAsync(context, repository.Path);

        JobOutcome outcome = await RunAsync(context, store, jobId);

        Assert.Equal(AnalysisJobStatus.Succeeded, outcome.Status);

        AnalysisJobRow job = (await store.FindAsync(jobId))!;

        Assert.Equal(head, job.SourceHeadSha);
        Assert.Equal(head[..12], job.SourceHeadShortSha);
        Assert.Equal(WorktreeState.Clean, job.SourceTreeState);
        Assert.Equal(0, job.SourceDirtyFileCount);
        Assert.NotNull(job.SourceStateCheckedAtUtc);
        Assert.NotNull(job.SourceStateVerifiedAtUtc);
        Assert.False(job.SourceCommitChangedDuringAnalysis);
    }

    [DockerFact]
    public async Task ADirtyWorktreeStopsTheScanBeforeItStarts()
    {
        using TemporaryRepository repository = new();
        repository.Commit("Ornek.cs", "public class Ornek { }", "kod");

        // Kaydedilmemis bir degisiklik. Bu haliyle taranirsa kaydedilen SHA diskteki
        // dosyalari anlatmaz.
        File.WriteAllText(Path.Combine(repository.Path, "Yeni.cs"), "public class Yeni { }");

        using SievertContext context = postgres.NewDatabase();
        (AnalysisJobStore store, Guid jobId) = await StartAsync(context, repository.Path);

        JobOutcome outcome = await RunAsync(context, store, jobId);

        Assert.Equal(AnalysisJobStatus.Failed, outcome.Status);
        Assert.Equal(ApiError.RepositoryWorktreeDirty, outcome.ErrorCode);
        Assert.Equal(0, outcome.ResultCount);
        Assert.Empty(context.StaticAnalysisFindings.Where(row => row.AnalysisJobId == jobId));

        AnalysisJobRow job = (await store.FindAsync(jobId))!;

        Assert.Equal(WorktreeState.Dirty, job.SourceTreeState);
        Assert.Equal(1, job.SourceDirtyFileCount);

        // Tarama hic basladigi icin dogrulama da yok.
        Assert.Null(job.SourceStateVerifiedAtUtc);
    }

    [DockerFact]
    public async Task TheDirtyRefusalNamesNeitherTheFolderNorTheFiles()
    {
        using TemporaryRepository repository = new();
        repository.Commit("Ornek.cs", "public class Ornek { }", "kod");
        File.WriteAllText(Path.Combine(repository.Path, "Gizli.cs"), "public class Gizli { }");

        using SievertContext context = postgres.NewDatabase();
        (AnalysisJobStore store, Guid jobId) = await StartAsync(context, repository.Path);

        JobOutcome outcome = await RunAsync(context, store, jobId);

        Assert.NotNull(outcome.ErrorMessage);
        Assert.DoesNotContain(repository.Path, outcome.ErrorMessage, StringComparison.Ordinal);
        Assert.DoesNotContain("Gizli.cs", outcome.ErrorMessage, StringComparison.Ordinal);
    }

    [DockerFact]
    public async Task AFolderThatIsNotAGitRepositoryIsRefused()
    {
        string folder = Path.Combine(Path.GetTempPath(), "sievert-git-yok-" + Guid.NewGuid().ToString("n"));
        Directory.CreateDirectory(folder);

        try
        {
            using SievertContext context = postgres.NewDatabase();
            (AnalysisJobStore store, Guid jobId) = await StartAsync(context, folder);

            JobOutcome outcome = await RunAsync(context, store, jobId);

            Assert.Equal(AnalysisJobStatus.Failed, outcome.Status);
            Assert.Equal(ApiError.RepositoryNotGit, outcome.ErrorCode);
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    /// <summary>
    /// Tarama sirasinda depo degisirse is basarili sayilamaz.
    ///
    /// Sira garantili: gozcu, isin kaynak durumunun veritabanina yazildigini ve taramanin
    /// gercekten basladigini (faz <c>scanning</c>) gorene kadar bekliyor. Tarama bittikten
    /// sonra ikinci okuma yapiliyor, yani gozcunun commit'i her halukarda araya giriyor.
    /// </summary>
    [DockerFact]
    public async Task AChangeDuringTheScanStopsTheJobFromSucceeding()
    {
        using TemporaryRepository repository = new();

        // Tarama olculebilir bir sure surmeli; tek dosyalik bir depoda "sirasinda" diye
        // bir an yok. Dosyalar hem cok hem uzun: Roslyn'in ayristirma maliyeti satir
        // sayisiyla buyuyor ve testin guvendigi sey taramanin gozcuden yavas olmasi.
        Dictionary<string, string> files = [];

        for (int index = 0; index < 200; index++)
        {
            IEnumerable<string> members = Enumerable
                .Range(0, 200)
                .Select(member => $"    public int Deger{member}() => {member} + {index};");

            files[$"Dosya{index}.cs"] =
                $"public class Dosya{index}\n{{\n{string.Join("\n", members)}\n}}\n";
        }

        repository.CommitMany(files, "kod");

        string connection = postgres.NewDatabaseConnectionString();
        using SievertContext context = SievertContextBuilder.Create(connection);

        (AnalysisJobStore store, Guid jobId) = await StartAsync(context, repository.Path);

        Task watcher = Task.Run(async () =>
        {
            using SievertContext own = SievertContextBuilder.Create(connection);
            AnalysisJobStore watching = new(own);

            while (true)
            {
                AnalysisJobRow? row = await watching.FindAsync(jobId);

                if (row?.SourceStateCheckedAtUtc is not null && row.CurrentPhase == ScanPhase.Scanning)
                {
                    break;
                }

                await Task.Delay(2);
            }

            repository.Commit("Sonradan.cs", "public class Sonradan { }", "tarama sirasinda");
        });

        JobOutcome outcome = await RunAsync(
            context,
            store,
            jobId,
            new AnalysisOptions { ProgressInterval = TimeSpan.FromMilliseconds(10) });

        await watcher;

        Assert.Equal(AnalysisJobStatus.Failed, outcome.Status);
        Assert.Equal(ApiError.RepositoryChangedDuringAnalysis, outcome.ErrorCode);

        AnalysisJobRow job = (await store.FindAsync(jobId))!;

        Assert.True(job.SourceCommitChangedDuringAnalysis);
        Assert.Equal(WorktreeState.ChangedDuringAnalysis, job.SourceTreeState);
        Assert.False(job.IsResultComplete);

        // Hesaplanmis satirlar atilmadi; sadece tam sonuc sayilmadi.
        Assert.True(outcome.ResultCount >= 0);
        Assert.Equal(outcome.ResultCount, context.StaticAnalysisFindings.Count(row => row.AnalysisJobId == jobId));
    }

    [DockerFact]
    public async Task TheSourceStateIsVisibleOnTheJobAndTheFindings()
    {
        using TemporaryRepository repository = new();
        string head = repository.Commit(
            "Ornek.cs",
            "public class Ornek { public async void Bos() { } }",
            "kod").Sha;

        string connection = postgres.NewDatabaseConnectionString();
        Guid jobId;

        using (SievertContext context = SievertContextBuilder.Create(connection))
        {
            (AnalysisJobStore store, jobId) = await StartAsync(context, repository.Path);
            await RunAsync(context, store, jobId);
        }

        using SievertApiFactory factory = new(connection);
        using HttpClient client = factory.CreateClient();

        JsonElement job = JsonDocument.Parse(await client.GetStringAsync($"/api/v1/analyses/{jobId}")).RootElement;

        Assert.Equal(head, job.GetProperty("sourceHeadSha").GetString());
        Assert.Equal(head[..12], job.GetProperty("sourceHeadShortSha").GetString());
        Assert.Equal(WorktreeState.Clean, job.GetProperty("sourceTreeState").GetString());
        Assert.True(job.GetProperty("sourceVerified").GetBoolean());
        Assert.False(job.GetProperty("sourceCommitChangedDuringAnalysis").GetBoolean());

        JsonElement findings = JsonDocument
            .Parse(await client.GetStringAsync($"/api/v1/analyses/{jobId}/findings"))
            .RootElement;

        Assert.Equal(head, findings.GetProperty("sourceHeadSha").GetString());
        Assert.Equal(WorktreeState.Clean, findings.GetProperty("sourceTreeState").GetString());

        // Yerel klasor hicbir cevapta gecmiyor.
        string raw = await client.GetStringAsync($"/api/v1/analyses/{jobId}");
        Assert.DoesNotContain(repository.Path, raw, StringComparison.Ordinal);
    }

    [DockerFact]
    public async Task ARiskJobCarriesNoSourceHead()
    {
        using SievertContext context = postgres.NewDatabase();

        (int known, int _) = ApiSeed.Write(context);
        AnalysisJobStore store = new(context);

        Guid jobId = (await store.CreateAsync(known, AnalysisJobKind.RiskScoreAll, null, DateTimeOffset.UtcNow))
            .Job!.Id;

        AnalysisJobRow job = (await store.FindAsync(jobId))!;

        Assert.Null(job.SourceHeadSha);
        Assert.Null(job.SourceTreeState);
        Assert.False(job.SourceCommitChangedDuringAnalysis);
    }

    private static async Task<(AnalysisJobStore Store, Guid JobId)> StartAsync(SievertContext context, string localPath)
    {
        (int known, int _) = ApiSeed.Write(context, localPath);

        AnalysisJobStore store = new(context);

        Guid jobId = (await store.CreateAsync(known, AnalysisJobKind.StaticScan, null, DateTimeOffset.UtcNow))
            .Job!.Id;

        await store.TryStartAsync(jobId, "test", DateTimeOffset.UtcNow);

        return (store, jobId);
    }

    private static async Task<JobOutcome> RunAsync(
        SievertContext context,
        AnalysisJobStore store,
        Guid jobId,
        AnalysisOptions? options = null)
    {
        options ??= new AnalysisOptions { ProgressInterval = TimeSpan.FromMilliseconds(10) };

        StaticScanHandler handler = new(context, store, options, TimeProvider.System);

        int repositoryId = (await store.FindAsync(jobId))!.RepositoryId;

        JobOutcome outcome = await handler.RunAsync(
            new AnalysisJobRun(jobId, repositoryId, new JobProgress(store, jobId, options.ProgressInterval, TimeProvider.System)),
            CancellationToken.None);

        await store.CompleteAsync(
            jobId,
            outcome.Status,
            outcome.ResultCount,
            outcome.ProcessedItems,
            outcome.ErrorCode,
            outcome.ErrorMessage,
            DateTimeOffset.UtcNow,
            outcome.ResultSummary);

        return outcome;
    }
}
