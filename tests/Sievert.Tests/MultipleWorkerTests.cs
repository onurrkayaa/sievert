using Microsoft.Extensions.DependencyInjection;

using Sievert.Api.Analysis;
using Sievert.Data;
using Sievert.Data.Entities;

namespace Sievert.Tests;

/// <summary>
/// Ayni veritabanina bagli **iki** worker ornegi.
///
/// Bu testler "dagitik kuyruk kurdum" demek degil. Kanal hala surec ici; iki ornek
/// arasindaki tek ortak sey veritabani. Sinanan sey tam olarak bu: tekillik ve iptal
/// bayragi veritabaninda oldugu icin ikinci bir surec eklendiginde de calisiyor mu.
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class MultipleWorkerTests(PostgresFixture postgres)
{
    private static readonly TimeSpan Patience = TimeSpan.FromSeconds(60);

    /// <summary>
    /// Iki worker ayni isi kuyruklarinda goruyor. Yalniz biri <c>running</c> gecisini
    /// kazaniyor ve isleyici bir kez kosuyor: sonuc satirlari iki katina cikmiyor.
    /// </summary>
    [DockerFact]
    public async Task OnlyOneOfTwoWorkersRunsTheSameJob()
    {
        string connection = postgres.NewDatabaseConnectionString();
        int known;
        Guid jobId;

        using (SievertContext seed = SievertContextBuilder.Create(connection))
        {
            (known, _) = ApiSeed.Write(seed);

            AnalysisJobStore store = new(seed);
            jobId = (await store.CreateAsync(known, AnalysisJobKind.RiskScoreAll, null, DateTimeOffset.UtcNow))
                .Job!.Id;
        }

        await using WorkerInstance first = new(connection);
        await using WorkerInstance second = new(connection);

        Assert.NotEqual(first.InstanceId, second.InstanceId);

        await first.StartAsync();
        await second.StartAsync();

        // Ayni kimlik iki ayri kuyruga giriyor; iki surecte de olabilecek durum bu.
        await first.Queue.EnqueueAsync(jobId);
        await second.Queue.EnqueueAsync(jobId);

        AnalysisJobRow job = await WaitForTerminalAsync(connection, jobId);

        Assert.Equal(AnalysisJobStatus.Succeeded, job.Status);
        Assert.Equal(5, job.ResultCount);

        using SievertContext check = SievertContextBuilder.Create(connection);

        // Isleyici iki kez kossaydi ya satir sayisi 10 olurdu ya da benzersiz indeks
        // patlardi; ikisi de olmadi.
        Assert.Equal(5, check.CommitRiskSnapshots.Count(row => row.AnalysisJobId == jobId));
        Assert.Contains(job.WorkerInstanceId, (string?[])[first.InstanceId, second.InstanceId]);
    }

    /// <summary>
    /// Iptal istegi **oteki** ornekten geliyor. Isi kosan worker'in yerel jeton defterine
    /// erisim yok; tek haber kaynagi veritabanindaki bayrak ve is onu bir sonraki obek
    /// sinirinda goruyor.
    /// </summary>
    [DockerFact]
    public async Task ACancellationFromAnotherInstanceIsSeenAtTheNextBatch()
    {
        string connection = postgres.NewDatabaseConnectionString();
        int known;
        Guid jobId;

        using (SievertContext seed = SievertContextBuilder.Create(connection))
        {
            (known, _) = ApiSeed.Write(seed);

            // Is, iptal istegi yetisecek kadar uzun surmeli.
            ApiSeed.AddManyCommits(seed, known, 600);

            AnalysisJobStore store = new(seed);
            jobId = (await store.CreateAsync(known, AnalysisJobKind.RiskScoreAll, null, DateTimeOffset.UtcNow))
                .Job!.Id;
        }

        await using WorkerInstance runner = new(connection, new AnalysisOptions { RiskBatchSize = 25 });
        await using WorkerInstance other = new(connection);

        await runner.StartAsync();
        await runner.Queue.EnqueueAsync(jobId);

        // Is gercekten baslasin; yoksa olculen sey kuyruktaki isin iptali olur.
        await WaitForAsync(connection, jobId, job => job.Status == AnalysisJobStatus.Running);

        AnalysisJobStore remote = other.NewStore(out IServiceScope scope);
        using IServiceScope owned = scope;

        Assert.True(await remote.RequestCancellationAsync(jobId, DateTimeOffset.UtcNow));

        // Iptali isteyen ornek, isi kosan ornegin jetonunu tetikleyemiyor.
        other.Cancellations.Cancel(jobId);

        AnalysisJobRow job = await WaitForTerminalAsync(connection, jobId);

        Assert.Equal(AnalysisJobStatus.Canceled, job.Status);
        Assert.False(job.IsResultComplete);

        using SievertContext check = SievertContextBuilder.Create(connection);
        int rows = check.CommitRiskSnapshots.Count(row => row.AnalysisJobId == jobId);

        // En fazla o anki obek tamamlanmis olabilir; hepsi yazilmis olamaz.
        Assert.Equal(rows, job.ResultCount);
        Assert.True(rows < 605, $"iptal edilmesine ragmen butun satirlar yazildi: {rows}");
    }

    /// <summary>
    /// Iki ornek ayni anda kurtarma kosarsa is yine bir kez calisiyor. Kurtarma
    /// idempotent ve <c>queued -> running</c> gecisi atomik.
    /// </summary>
    [DockerFact]
    public async Task TwoRecoveriesStillLeaveTheJobRunningOnce()
    {
        string connection = postgres.NewDatabaseConnectionString();
        int known;
        Guid jobId;

        using (SievertContext seed = SievertContextBuilder.Create(connection))
        {
            (known, _) = ApiSeed.Write(seed);

            AnalysisJobStore store = new(seed);
            jobId = (await store.CreateAsync(known, AnalysisJobKind.RiskScoreAll, null, DateTimeOffset.UtcNow))
                .Job!.Id;
        }

        await using WorkerInstance first = new(connection);
        await using WorkerInstance second = new(connection);

        AnalysisJobStore firstStore = first.NewStore(out IServiceScope firstScope);
        using IServiceScope ownedFirst = firstScope;
        AnalysisJobStore secondStore = second.NewStore(out IServiceScope secondScope);
        using IServiceScope ownedSecond = secondScope;

        RecoveryReport left = await firstStore.RecoverAsync(DateTimeOffset.UtcNow);
        RecoveryReport right = await secondStore.RecoverAsync(DateTimeOffset.UtcNow);

        // Ikisi de ayni kuyruktaki isi goruyor ve ikisi de hicbir satir degistirmiyor.
        Assert.Equal([jobId], left.Requeued);
        Assert.Equal([jobId], right.Requeued);
        Assert.Equal(0, left.ChangedRows);
        Assert.Equal(0, right.ChangedRows);

        await first.StartAsync();
        await second.StartAsync();
        await first.Queue.EnqueueAsync(jobId);
        await second.Queue.EnqueueAsync(jobId);

        AnalysisJobRow job = await WaitForTerminalAsync(connection, jobId);

        Assert.Equal(AnalysisJobStatus.Succeeded, job.Status);

        using SievertContext check = SievertContextBuilder.Create(connection);
        Assert.Equal(5, check.CommitRiskSnapshots.Count(row => row.AnalysisJobId == jobId));
    }

    private static Task<AnalysisJobRow> WaitForTerminalAsync(string connection, Guid jobId) =>
        WaitForAsync(connection, jobId, job => !AnalysisJobRow.IsActive(job.Status));

    private static async Task<AnalysisJobRow> WaitForAsync(
        string connection,
        Guid jobId,
        Func<AnalysisJobRow, bool> ready)
    {
        using SievertContext context = SievertContextBuilder.Create(connection);
        AnalysisJobStore store = new(context);

        DateTimeOffset deadline = DateTimeOffset.UtcNow + Patience;

        while (DateTimeOffset.UtcNow < deadline)
        {
            if (await store.FindAsync(jobId) is AnalysisJobRow job && ready(job))
            {
                return job;
            }

            await Task.Delay(20);
        }

        throw new TimeoutException($"{jobId} beklenen duruma {Patience.TotalSeconds} saniyede gelmedi.");
    }
}
