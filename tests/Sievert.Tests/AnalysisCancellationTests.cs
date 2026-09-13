using System.Text.Json;

using Sievert.Api;
using Sievert.Api.Analysis;
using Sievert.Data;
using Sievert.Data.Entities;
using Sievert.Modeling;

namespace Sievert.Tests;

/// <summary>
/// Iptal testleri.
///
/// Isleyiciler dogrudan cagriliyor, HTTP uzerinden degil. Sebebi belirlilik: HTTP'den
/// gidip "isin ortasinda iptal et" demek yarisa dayanir ve bes commit'lik bir is cogu
/// zaman istek gitmeden biter. Burada iptal istegi ONCEDEN yaziliyor, isleyici ilk obek
/// sinirinda gormek zorunda.
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class AnalysisCancellationTests(PostgresFixture postgres)
{
    private static readonly string ResultsPath = ProjectRoot.Combine("data", "asama5", "model-results.json");

    private static readonly string ModelDirectory = ProjectRoot.Combine("data", "asama5", "models");

    private static readonly string ReferencePath =
        ProjectRoot.Combine("data", "asama6", "model-score-reference.json");

    [DockerFact]
    public async Task ARunningRiskJobStopsAtTheNextBatchAndKeepsItsPartialResult()
    {
        string connection = postgres.NewDatabaseConnectionString();
        using SievertContext context = SievertContextBuilder.Create(connection);

        (int known, int _) = ApiSeed.Write(context);

        AnalysisJobStore store = new(context);
        Guid jobId = (await store.CreateAsync(known, AnalysisJobKind.RiskScoreAll, null, DateTimeOffset.UtcNow)).Job!.Id;

        await store.TryStartAsync(jobId, "test", DateTimeOffset.UtcNow);
        await store.RequestCancellationAsync(jobId, DateTimeOffset.UtcNow);

        // Obek 2: bes commit'in hepsi tek obekte bitmesin, iptal arada gorulsun.
        AnalysisOptions options = new() { RiskBatchSize = 2 };

        JobOutcome outcome = await Handler(context, options).RunAsync(
            new AnalysisJobRun(jobId, known, Progress(store, jobId, options)),
            CancellationToken.None);

        Assert.Equal(AnalysisJobStatus.Canceled, outcome.Status);
        Assert.Equal("ANALYSIS_CANCELED", outcome.ErrorCode);

        // Ilk obek yazildi, geri kalani yazilmadi: sonuc kismi.
        Assert.Equal(2, outcome.ResultCount);
        Assert.Equal(2, context.CommitRiskSnapshots.Count(row => row.AnalysisJobId == jobId));

        await store.CompleteAsync(
            jobId,
            outcome.Status,
            outcome.ResultCount,
            outcome.ProcessedItems,
            outcome.ErrorCode,
            outcome.ErrorMessage,
            DateTimeOffset.UtcNow);

        AnalysisJobRow job = (await store.FindAsync(jobId))!;

        Assert.False(job.IsResultComplete);
        Assert.Null(job.ActiveDeduplicationKey);
    }

    [DockerFact]
    public async Task ThePartialResultIsMarkedAsPartialOnTheApi()
    {
        string connection = postgres.NewDatabaseConnectionString();
        Guid jobId;
        int known;

        using (SievertContext context = SievertContextBuilder.Create(connection))
        {
            (known, _) = ApiSeed.Write(context);

            AnalysisJobStore store = new(context);
            jobId = (await store.CreateAsync(known, AnalysisJobKind.RiskScoreAll, null, DateTimeOffset.UtcNow)).Job!.Id;

            await store.TryStartAsync(jobId, "test", DateTimeOffset.UtcNow);
            await store.RequestCancellationAsync(jobId, DateTimeOffset.UtcNow);

            AnalysisOptions options = new() { RiskBatchSize = 2 };

            JobOutcome outcome = await Handler(context, options).RunAsync(
                new AnalysisJobRun(jobId, known, Progress(store, jobId, options)),
                CancellationToken.None);

            await store.CompleteAsync(
                jobId,
                outcome.Status,
                outcome.ResultCount,
                outcome.ProcessedItems,
                outcome.ErrorCode,
                outcome.ErrorMessage,
                DateTimeOffset.UtcNow);
        }

        using SievertApiFactory factory = new(connection);
        using HttpClient client = factory.CreateClient();

        JsonElement page = JsonDocument
            .Parse(await client.GetStringAsync($"/api/v1/analyses/{jobId}/risks"))
            .RootElement;

        Assert.True(page.GetProperty("partial").GetBoolean());
        Assert.False(page.GetProperty("isResultComplete").GetBoolean());
        Assert.Equal("canceled", page.GetProperty("status").GetString());
        Assert.False(string.IsNullOrWhiteSpace(page.GetProperty("partialWarning").GetString()));

        // Satirlar duruyor ama tam sonuc gibi sunulmuyorlar.
        Assert.Equal(2, page.GetProperty("totalCount").GetInt32());
    }

    [DockerFact]
    public async Task ACanceledStaticScanWritesNoFindings()
    {
        using TemporaryRepository repository = new();
        repository.Commit("Ornek.cs", "public class Ornek { public async void Bos() { } }", "kod");

        string connection = postgres.NewDatabaseConnectionString();
        using SievertContext context = SievertContextBuilder.Create(connection);

        (int known, int _) = ApiSeed.Write(context, repository.Path);

        AnalysisJobStore store = new(context);
        Guid jobId = (await store.CreateAsync(known, AnalysisJobKind.StaticScan, null, DateTimeOffset.UtcNow)).Job!.Id;

        await store.TryStartAsync(jobId, "test", DateTimeOffset.UtcNow);
        await store.RequestCancellationAsync(jobId, DateTimeOffset.UtcNow);

        AnalysisOptions options = new() { ProgressInterval = TimeSpan.Zero };

        StaticScanHandler handler = new(context, store, options, TimeProvider.System);

        JobOutcome outcome = await handler.RunAsync(
            new AnalysisJobRun(jobId, known, Progress(store, jobId, options)),
            CancellationToken.None);

        Assert.Equal(AnalysisJobStatus.Canceled, outcome.Status);
        Assert.Equal(0, outcome.ResultCount);
        Assert.Empty(context.StaticAnalysisFindings.Where(row => row.AnalysisJobId == jobId));
    }

    /// <summary>
    /// Jeton uzerinden iptal.
    ///
    /// Olcumde cikan gercek bir kusurun testi: is jetonla iptal edildiginde sayilar
    /// isleyiciden cikamiyordu ve is "0 satir" diye kaydediliyordu, oysa veritabaninda
    /// 2250 satir duruyordu. Kismi sonucun buyuklugunu yanlis soylemek, kismi sonucu hic
    /// soylememekten kotu.
    /// </summary>
    [DockerFact]
    public async Task CancellingThroughTheTokenStillReportsTheRowsThatWereWritten()
    {
        string connection = postgres.NewDatabaseConnectionString();
        using SievertContext context = SievertContextBuilder.Create(connection);

        (int known, int _) = ApiSeed.Write(context);

        AnalysisJobStore store = new(context);
        Guid jobId = (await store.CreateAsync(known, AnalysisJobKind.RiskScoreAll, null, DateTimeOffset.UtcNow)).Job!.Id;

        await store.TryStartAsync(jobId, "test", DateTimeOffset.UtcNow);

        AnalysisOptions options = new() { RiskBatchSize = 2 };

        using CancellationTokenSource source = new();

        // Ilk obek yazildiktan sonra jetonu iptal et.
        JobProgress progress = Progress(store, jobId, options);

        // Gozcu KENDI baglamini kullaniyor: DbContext is parcaciklari arasinda
        // paylasilmaz, paylasilirsa "ikinci islem baslatildi" hatasi aliniyor.
        Task watcher = Task.Run(async () =>
        {
            using SievertContext own = SievertContextBuilder.Create(connection);

            while (own.CommitRiskSnapshots.Count(row => row.AnalysisJobId == jobId) < 2)
            {
                await Task.Delay(5);
            }

            await source.CancelAsync();
        });

        JobOutcome outcome = await Handler(context, options).RunAsync(
            new AnalysisJobRun(jobId, known, progress),
            source.Token);

        await watcher;

        int actual = context.CommitRiskSnapshots.Count(row => row.AnalysisJobId == jobId);

        // Is ya bitti ya iptal edildi; iki durumda da bildirdigi sayi gercek satir
        // sayisiyla ayni olmali.
        Assert.Equal(actual, outcome.ResultCount);

        if (outcome.Status == AnalysisJobStatus.Canceled)
        {
            Assert.True(actual > 0, "hicbir satir yazilmadan iptal edildi; test bir sey sinamadi");
            Assert.True(actual < 5, $"iptal edilmesine ragmen butun satirlar yazildi: {actual}");
        }
    }

    [DockerFact]
    public async Task ProgressIsNotWrittenOncePerCommit()
    {
        string connection = postgres.NewDatabaseConnectionString();
        using SievertContext context = SievertContextBuilder.Create(connection);

        (int known, int _) = ApiSeed.Write(context);

        AnalysisJobStore store = new(context);
        Guid jobId = (await store.CreateAsync(known, AnalysisJobKind.RiskScoreAll, null, DateTimeOffset.UtcNow)).Job!.Id;

        await store.TryStartAsync(jobId, "test", DateTimeOffset.UtcNow);

        AnalysisOptions options = new() { RiskBatchSize = 1 };
        JobProgress progress = Progress(store, jobId, options);

        JobOutcome outcome = await Handler(context, options).RunAsync(
            new AnalysisJobRun(jobId, known, progress),
            CancellationToken.None);

        Assert.Equal(AnalysisJobStatus.Succeeded, outcome.Status);
        Assert.Equal(5, outcome.ResultCount);

        // Bes commit tek tek islendi ama yazim sayisi bes obekten az: sayim, asama
        // yazimlari ve sondaki zorunlu yazim disinda araliga takiliyor.
        Assert.True(progress.WriteCount <= 4, $"ilerleme {progress.WriteCount} kez yazildi");
    }

    private static RiskScoreAllHandler Handler(SievertContext context, AnalysisOptions options) => new(
        context,
        ModelRegistry.Create(ResultsPath, ModelDirectory),
        ScoreReference.Load(ReferencePath),
        options,
        TimeProvider.System);

    private static JobProgress Progress(AnalysisJobStore store, Guid jobId, AnalysisOptions options) =>
        new(store, jobId, options.ProgressInterval, TimeProvider.System);
}
