using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Sievert.Api.Analysis;
using Sievert.Data;
using Sievert.Data.Entities;

namespace Sievert.Tests;

/// <summary>
/// Es zamanlilik ayarinin gercekten kac isi ayni anda kosturdugu.
///
/// Varsayilan 1 ve oyle kaliyor. Ust sinir 4 olarak yazilmisti; burada o sinirin
/// gercekten uygulandigi sinaniyor. Bu test gercek yuku olcmuyor - gercek repolarla
/// es zamanlilik 2 olcumu ayri bir dosyada ve 4 hic olculmedi.
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class WorkerConcurrencyTests(PostgresFixture postgres)
{
    [DockerTheory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(4)]
    public async Task NoMoreThanTheConfiguredNumberOfHandlersRunAtOnce(int concurrency)
    {
        string connection = postgres.NewDatabaseConnectionString();
        List<Guid> jobs = [];

        using (SievertContext seed = SievertContextBuilder.Create(connection))
        {
            AnalysisJobStore store = new(seed);

            // Her is ayri bir depo icin: ayni repo ve tur icin zaten tek aktif is kurali var.
            for (int index = 0; index < 8; index++)
            {
                RepositoryRow repository = new()
                {
                    Identity = $"ornek.test/depo{index}",
                    IdentitySource = "remote",
                    Name = "depo" + index,
                    ScannedAt = DateTimeOffset.UtcNow,
                };

                seed.Repositories.Add(repository);
                seed.SaveChanges();

                jobs.Add((await store.CreateAsync(
                    repository.Id, AnalysisJobKind.StaticScan, null, DateTimeOffset.UtcNow)).Job!.Id);
            }
        }

        CountingHandler counter = new();

        await using WorkerInstance worker = new(
            connection,
            new AnalysisOptions { WorkerConcurrency = concurrency },
            services =>
            {
                services.RemoveAll<IAnalysisJobHandler>();
                services.AddSingleton<IAnalysisJobHandler>(counter);
                services.AddScoped<IAnalysisJobHandler, RiskScoreAllHandler>();
            });

        await worker.StartAsync();

        foreach (Guid jobId in jobs)
        {
            await worker.Queue.EnqueueAsync(jobId);
        }

        DateTimeOffset deadline = DateTimeOffset.UtcNow.AddSeconds(60);

        while (counter.Finished < jobs.Count && DateTimeOffset.UtcNow < deadline)
        {
            await Task.Delay(20);
        }

        Assert.Equal(jobs.Count, counter.Finished);
        Assert.Equal(concurrency, counter.HighWaterMark);
    }

    /// <summary>
    /// Isin kendisi yok; tek yaptigi ayni anda kac tane calistigini saymak. Her kosu
    /// kisa bir sure bekliyor ki es zamanlilik olculebilsin.
    /// </summary>
    private sealed class CountingHandler : IAnalysisJobHandler
    {
        private readonly Lock gate = new();

        private int active;

        public AnalysisJobKind Kind => AnalysisJobKind.StaticScan;

        public int HighWaterMark { get; private set; }

        public int Finished { get; private set; }

        public async Task<JobOutcome> RunAsync(AnalysisJobRun run, CancellationToken cancellation)
        {
            lock (gate)
            {
                active++;
                HighWaterMark = Math.Max(HighWaterMark, active);
            }

            await Task.Delay(80, cancellation);

            lock (gate)
            {
                active--;
                Finished++;
            }

            return new JobOutcome(AnalysisJobStatus.Succeeded, 0, 0);
        }
    }
}
