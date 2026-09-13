using Sievert.Data;
using Sievert.Data.Entities;

namespace Sievert.Tests;

/// <summary>
/// Is kayitlarinin testleri. Gercek PostgreSQL kullaniyor cunku sinanan seylerin cogu
/// (benzersiz indeks, kosullu UPDATE, es zamanli yaris) veritabaninin kendi davranisi;
/// bellekte taklit edilse test gecer ama uretimde kirilirdi.
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class AnalysisJobStoreTests(PostgresFixture postgres)
{
    private static readonly DateTimeOffset Now = new(2026, 9, 13, 10, 0, 0, TimeSpan.Zero);

    [DockerFact]
    public async Task ANewJobStartsQueuedAndHoldsTheActiveKey()
    {
        (AnalysisJobStore store, int repository, string _) = Open();

        JobCreateResult result = await store.CreateAsync(repository, AnalysisJobKind.StaticScan, null, Now);

        Assert.Equal(JobCreateOutcome.Created, result.Outcome);
        Assert.Equal(AnalysisJobStatus.Queued, result.Job!.Status);
        Assert.Equal($"{repository}:static-scan", result.Job.ActiveDeduplicationKey);
        Assert.False(result.Job.IsResultComplete);
        Assert.Null(result.Job.CompletedAtUtc);
    }

    [DockerFact]
    public async Task ASecondJobForTheSameRepositoryAndKindIsRefused()
    {
        (AnalysisJobStore store, int repository, string _) = Open();

        JobCreateResult first = await store.CreateAsync(repository, AnalysisJobKind.StaticScan, null, Now);
        JobCreateResult second = await store.CreateAsync(repository, AnalysisJobKind.StaticScan, null, Now);

        Assert.Equal(JobCreateOutcome.AlreadyActive, second.Outcome);
        Assert.Equal(first.Job!.Id, second.Job!.Id);

        // Ayni repo, BASKA tur: engel yok.
        Assert.Equal(
            JobCreateOutcome.Created,
            (await store.CreateAsync(repository, AnalysisJobKind.RiskScoreAll, null, Now)).Outcome);
    }

    [DockerFact]
    public async Task TheSameIdempotencyKeyReturnsTheSameJob()
    {
        (AnalysisJobStore store, int repository, string _) = Open();

        JobCreateResult first = await store.CreateAsync(repository, AnalysisJobKind.StaticScan, "abc", Now);
        JobCreateResult second = await store.CreateAsync(repository, AnalysisJobKind.StaticScan, "abc", Now);

        Assert.Equal(JobCreateOutcome.Created, first.Outcome);
        Assert.Equal(JobCreateOutcome.ReturnedExisting, second.Outcome);
        Assert.Equal(first.Job!.Id, second.Job!.Id);
    }

    [DockerFact]
    public async Task AKeyUsedForAnotherRepositoryIsRefused()
    {
        (AnalysisJobStore store, int known, string _) = Open(out int unknown);

        await store.CreateAsync(known, AnalysisJobKind.StaticScan, "ayni-anahtar", Now);

        JobCreateResult other = await store.CreateAsync(unknown, AnalysisJobKind.StaticScan, "ayni-anahtar", Now);

        Assert.Equal(JobCreateOutcome.IdempotencyKeyReused, other.Outcome);
    }

    [DockerFact]
    public async Task AKeyUsedForAnotherKindIsRefused()
    {
        (AnalysisJobStore store, int repository, string _) = Open();

        await store.CreateAsync(repository, AnalysisJobKind.StaticScan, "anahtar", Now);

        JobCreateResult other = await store.CreateAsync(repository, AnalysisJobKind.RiskScoreAll, "anahtar", Now);

        Assert.Equal(JobCreateOutcome.IdempotencyKeyReused, other.Outcome);
    }

    [DockerTheory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task AnEmptyKeyIsInvalid(string key)
    {
        (AnalysisJobStore store, int repository, string _) = Open();

        Assert.Equal(
            JobCreateOutcome.IdempotencyKeyInvalid,
            (await store.CreateAsync(repository, AnalysisJobKind.StaticScan, key, Now)).Outcome);
    }

    [DockerFact]
    public async Task AKeyLongerThanTheLimitIsInvalid()
    {
        (AnalysisJobStore store, int repository, string _) = Open();

        string tooLong = new('k', AnalysisJobStore.MaximumIdempotencyKeyLength + 1);
        string atTheLimit = new('k', AnalysisJobStore.MaximumIdempotencyKeyLength);

        Assert.Equal(
            JobCreateOutcome.IdempotencyKeyInvalid,
            (await store.CreateAsync(repository, AnalysisJobKind.StaticScan, tooLong, Now)).Outcome);

        Assert.Equal(
            JobCreateOutcome.Created,
            (await store.CreateAsync(repository, AnalysisJobKind.StaticScan, atTheLimit, Now)).Outcome);
    }

    /// <summary>
    /// Onemli olan: bu yaris uygulama kontrolune degil benzersiz indekse dayaniyor.
    /// On kontrol tek basina olsaydi, on istek ayni anda kontrolden gecip on is acardi.
    /// </summary>
    [DockerFact]
    public async Task TenSimultaneousRequestsWithDifferentKeysProduceOneJob()
    {
        string connection = postgres.NewDatabaseConnectionString();
        int repository = Seed(connection);

        List<Task<JobCreateResult>> attempts = [];

        for (int index = 0; index < 10; index++)
        {
            string key = $"anahtar-{index}";

            attempts.Add(Task.Run(async () =>
            {
                using SievertContext own = SievertContextBuilder.Create(connection);

                return await new AnalysisJobStore(own).CreateAsync(
                    repository,
                    AnalysisJobKind.StaticScan,
                    key,
                    Now);
            }));
        }

        JobCreateResult[] results = await Task.WhenAll(attempts);

        Assert.Equal(1, results.Count(result => result.Outcome == JobCreateOutcome.Created));
        Assert.Equal(9, results.Count(result => result.Outcome == JobCreateOutcome.AlreadyActive));

        using SievertContext check = SievertContextBuilder.Create(connection);

        Assert.Equal(1, check.AnalysisJobs.Count());
    }

    [DockerFact]
    public async Task TenSimultaneousRequestsWithTheSameKeyProduceOneJob()
    {
        string connection = postgres.NewDatabaseConnectionString();
        int repository = Seed(connection);

        List<Task<JobCreateResult>> attempts = [];

        for (int index = 0; index < 10; index++)
        {
            attempts.Add(Task.Run(async () =>
            {
                using SievertContext own = SievertContextBuilder.Create(connection);

                return await new AnalysisJobStore(own).CreateAsync(
                    repository,
                    AnalysisJobKind.StaticScan,
                    "tek-anahtar",
                    Now);
            }));
        }

        await Task.WhenAll(attempts);

        using SievertContext check = SievertContextBuilder.Create(connection);

        Assert.Equal(1, check.AnalysisJobs.Count());
    }

    [DockerFact]
    public async Task OnlyOneWorkerCanStartAJob()
    {
        (AnalysisJobStore store, int repository, string _) = Open();

        Guid id = (await store.CreateAsync(repository, AnalysisJobKind.StaticScan, null, Now)).Job!.Id;

        Assert.True(await store.TryStartAsync(id, "worker-1", Now));
        Assert.False(await store.TryStartAsync(id, "worker-2", Now));

        AnalysisJobRow job = (await store.FindAsync(id))!;

        Assert.Equal(AnalysisJobStatus.Running, job.Status);
        Assert.Equal("worker-1", job.WorkerInstanceId);
        Assert.NotNull(job.StartedAtUtc);
        Assert.NotNull(job.HeartbeatAtUtc);
    }

    [DockerTheory]
    [InlineData(AnalysisJobStatus.Succeeded, true)]
    [InlineData(AnalysisJobStatus.Failed, false)]
    [InlineData(AnalysisJobStatus.Canceled, false)]
    public async Task CompletingClearsTheActiveKeyAndSetsCompleteness(AnalysisJobStatus status, bool complete)
    {
        (AnalysisJobStore store, int repository, string _) = Open();

        Guid id = (await store.CreateAsync(repository, AnalysisJobKind.StaticScan, null, Now)).Job!.Id;
        await store.TryStartAsync(id, "worker-1", Now);

        Assert.True(await store.CompleteAsync(id, status, 42, 42, null, null, Now));

        AnalysisJobRow job = (await store.FindAsync(id))!;

        Assert.Equal(status, job.Status);
        Assert.Equal(complete, job.IsResultComplete);
        Assert.Equal(42, job.ResultCount);
        Assert.NotNull(job.CompletedAtUtc);
        Assert.Null(job.ActiveDeduplicationKey);

        // Anahtar birakildigi icin ayni repo ve tur yeniden acilabiliyor.
        Assert.Equal(
            JobCreateOutcome.Created,
            (await store.CreateAsync(repository, AnalysisJobKind.StaticScan, null, Now)).Outcome);
    }

    [DockerFact]
    public async Task ATerminalJobCannotMoveAgain()
    {
        (AnalysisJobStore store, int repository, string _) = Open();

        Guid id = (await store.CreateAsync(repository, AnalysisJobKind.StaticScan, null, Now)).Job!.Id;
        await store.TryStartAsync(id, "worker-1", Now);
        await store.CompleteAsync(id, AnalysisJobStatus.Succeeded, 1, 1, null, null, Now);

        Assert.False(await store.CompleteAsync(id, AnalysisJobStatus.Failed, 0, 0, "X", "y", Now));
        Assert.False(await store.CancelQueuedAsync(id, Now));
        Assert.False(await store.RequestCancellationAsync(id, Now));
        Assert.False(await store.TryStartAsync(id, "worker-2", Now));
        Assert.False(await store.UpdateProgressAsync(id, "scanning", 5, 10, Now));

        Assert.Equal(AnalysisJobStatus.Succeeded, (await store.FindAsync(id))!.Status);
    }

    [DockerFact]
    public async Task AQueuedJobCannotJumpStraightToSucceeded()
    {
        (AnalysisJobStore store, int repository, string _) = Open();

        Guid id = (await store.CreateAsync(repository, AnalysisJobKind.StaticScan, null, Now)).Job!.Id;

        Assert.False(await store.CompleteAsync(id, AnalysisJobStatus.Succeeded, 1, 1, null, null, Now));
        Assert.Equal(AnalysisJobStatus.Queued, (await store.FindAsync(id))!.Status);
    }

    /// <summary>
    /// Kuyruktaki bir is basarisiz YAPILAMAZ. Hic denenmemis bir isi basarisiz yazmak,
    /// denenmis gibi gostermek olurdu; altyapida bir sorun varsa is kuyrukta kaliyor ve
    /// bir sonraki kurtarma onu geri aliyor.
    /// </summary>
    [DockerFact]
    public async Task AQueuedJobCannotBeFailedByTheStore()
    {
        (AnalysisJobStore store, int repository, string _) = Open();

        Guid id = (await store.CreateAsync(repository, AnalysisJobKind.StaticScan, null, Now)).Job!.Id;

        Assert.False(AnalysisJobTransitions.IsAllowed(AnalysisJobStatus.Queued, AnalysisJobStatus.Failed));
        Assert.False(await store.CompleteAsync(id, AnalysisJobStatus.Failed, 0, 0, "X", "y", Now));

        AnalysisJobRow job = (await store.FindAsync(id))!;

        Assert.Equal(AnalysisJobStatus.Queued, job.Status);
        Assert.Null(job.ErrorCode);
        Assert.NotNull(job.ActiveDeduplicationKey);
    }

    /// <summary>
    /// Kurtarma kuyruktaki isi yalniz yeniden kuyruga aliyor; durumunu degistirmiyor.
    /// Kuyrukta bekleyen bir isin surec kapandi diye basarisiz olmasi icin bir sebep yok.
    /// </summary>
    [DockerFact]
    public async Task RecoveryLeavesAQueuedJobQueued()
    {
        (AnalysisJobStore store, int repository, string _) = Open();

        Guid id = (await store.CreateAsync(repository, AnalysisJobKind.StaticScan, null, Now)).Job!.Id;

        RecoveryReport report = await store.RecoverAsync(Now.AddHours(1));

        Assert.Equal([id], report.Requeued);
        Assert.Equal(0, report.ChangedRows);

        AnalysisJobRow job = (await store.FindAsync(id))!;

        Assert.Equal(AnalysisJobStatus.Queued, job.Status);
        Assert.Null(job.ErrorCode);
        Assert.Null(job.CompletedAtUtc);
    }

    [DockerFact]
    public async Task CancellingAQueuedJobIsImmediateAndStopsItFromStarting()
    {
        (AnalysisJobStore store, int repository, string _) = Open();

        Guid id = (await store.CreateAsync(repository, AnalysisJobKind.StaticScan, null, Now)).Job!.Id;

        Assert.True(await store.CancelQueuedAsync(id, Now));
        Assert.False(await store.TryStartAsync(id, "worker-1", Now));

        AnalysisJobRow job = (await store.FindAsync(id))!;

        Assert.Equal(AnalysisJobStatus.Canceled, job.Status);
        Assert.False(job.IsResultComplete);
        Assert.Null(job.ActiveDeduplicationKey);
    }

    [DockerFact]
    public async Task CancellingARunningJobOnlyRecordsTheRequest()
    {
        (AnalysisJobStore store, int repository, string _) = Open();

        Guid id = (await store.CreateAsync(repository, AnalysisJobKind.StaticScan, null, Now)).Job!.Id;
        await store.TryStartAsync(id, "worker-1", Now);

        Assert.True(await store.RequestCancellationAsync(id, Now));
        Assert.True(await store.IsCancellationRequestedAsync(id));
        Assert.Equal(AnalysisJobStatus.Running, (await store.FindAsync(id))!.Status);

        // Ikinci istek yeni bir sey yazmiyor; tarih ezilmiyor.
        Assert.False(await store.RequestCancellationAsync(id, Now.AddMinutes(5)));
        Assert.Equal(Now, (await store.FindAsync(id))!.CancellationRequestedAtUtc);
    }

    [DockerFact]
    public async Task ProgressNeverGoesBackwards()
    {
        (AnalysisJobStore store, int repository, string _) = Open();

        Guid id = (await store.CreateAsync(repository, AnalysisJobKind.StaticScan, null, Now)).Job!.Id;
        await store.TryStartAsync(id, "worker-1", Now);

        Assert.True(await store.UpdateProgressAsync(id, "scanning", 50, 100, Now));
        Assert.Equal(50.0, (await store.FindAsync(id))!.ProgressPercent);

        Assert.False(await store.UpdateProgressAsync(id, "scanning", 20, 100, Now));

        AnalysisJobRow job = (await store.FindAsync(id))!;

        Assert.Equal(50, job.ProcessedItems);
        Assert.Equal(50.0, job.ProgressPercent);
    }

    [DockerFact]
    public async Task ProgressPercentIsNullUntilTheTotalIsKnown()
    {
        (AnalysisJobStore store, int repository, string _) = Open();

        Guid id = (await store.CreateAsync(repository, AnalysisJobKind.StaticScan, null, Now)).Job!.Id;
        await store.TryStartAsync(id, "worker-1", Now);

        await store.UpdateProgressAsync(id, "discovering-files", 0, null, Now);

        AnalysisJobRow job = (await store.FindAsync(id))!;

        Assert.Null(job.TotalItems);
        Assert.Null(job.ProgressPercent);
        Assert.Equal("discovering-files", job.CurrentPhase);
    }

    [DockerFact]
    public async Task RecoveryFailsInterruptedJobsAndLeavesTerminalOnesAlone()
    {
        (AnalysisJobStore store, int repository, string _) = Open(out int other);

        Guid running = (await store.CreateAsync(repository, AnalysisJobKind.StaticScan, null, Now)).Job!.Id;
        await store.TryStartAsync(running, "olen-surec", Now);

        Guid queued = (await store.CreateAsync(repository, AnalysisJobKind.RiskScoreAll, null, Now)).Job!.Id;

        Guid finished = (await store.CreateAsync(other, AnalysisJobKind.StaticScan, null, Now)).Job!.Id;
        await store.TryStartAsync(finished, "olen-surec", Now);
        await store.CompleteAsync(finished, AnalysisJobStatus.Succeeded, 3, 3, null, null, Now);

        RecoveryReport report = await store.RecoverAsync(Now.AddHours(1));

        Assert.Equal(1, report.Interrupted);
        Assert.Equal(0, report.CanceledBeforeStart);
        Assert.Equal([queued], report.Requeued);

        AnalysisJobRow interrupted = (await store.FindAsync(running))!;

        Assert.Equal(AnalysisJobStatus.Failed, interrupted.Status);
        Assert.Equal("PROCESS_INTERRUPTED", interrupted.ErrorCode);
        Assert.False(interrupted.IsResultComplete);
        Assert.Null(interrupted.ActiveDeduplicationKey);

        // Terminal is hic dokunulmadi.
        AnalysisJobRow untouched = (await store.FindAsync(finished))!;

        Assert.Equal(AnalysisJobStatus.Succeeded, untouched.Status);
        Assert.True(untouched.IsResultComplete);
        Assert.Equal(3, untouched.ResultCount);
    }

    [DockerFact]
    public async Task RecoveryCancelsQueuedJobsThatWereAlreadyAskedToStop()
    {
        (AnalysisJobStore store, int repository, string connection) = Open();

        Guid id = (await store.CreateAsync(repository, AnalysisJobKind.StaticScan, null, Now)).Job!.Id;

        // Kuyruktaki bir ise iptal istegi dusmus ama surec kapanmis.
        using (SievertContext direct = SievertContextBuilder.Create(connection))
        {
            AnalysisJobRow row = direct.AnalysisJobs.First(job => job.Id == id);
            row.CancellationRequestedAtUtc = Now;
            direct.SaveChanges();
        }

        RecoveryReport report = await store.RecoverAsync(Now.AddHours(1));

        Assert.Equal(1, report.CanceledBeforeStart);
        Assert.Empty(report.Requeued);
        Assert.Equal(AnalysisJobStatus.Canceled, (await store.FindAsync(id))!.Status);
    }

    [DockerFact]
    public async Task RunningRecoveryTwiceChangesNothingTheSecondTime()
    {
        (AnalysisJobStore store, int repository, string _) = Open();

        Guid running = (await store.CreateAsync(repository, AnalysisJobKind.StaticScan, null, Now)).Job!.Id;
        await store.TryStartAsync(running, "olen-surec", Now);
        await store.CreateAsync(repository, AnalysisJobKind.RiskScoreAll, null, Now);

        RecoveryReport first = await store.RecoverAsync(Now.AddHours(1));
        RecoveryReport second = await store.RecoverAsync(Now.AddHours(2));

        Assert.Equal(1, first.ChangedRows);
        Assert.Equal(0, second.ChangedRows);
        Assert.Equal(first.Requeued, second.Requeued);
    }

    private (AnalysisJobStore Store, int Repository, string Connection) Open() => Open(out _);

    private (AnalysisJobStore Store, int Repository, string Connection) Open(out int otherRepository)
    {
        string connection = postgres.NewDatabaseConnectionString();
        SievertContext context = SievertContextBuilder.Create(connection);
        (int known, int unknown) = ApiSeed.Write(context);

        otherRepository = unknown;

        return (new AnalysisJobStore(context), known, connection);
    }

    private static int Seed(string connection)
    {
        using SievertContext context = SievertContextBuilder.Create(connection);

        return ApiSeed.Write(context).Known;
    }
}
