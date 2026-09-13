using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using Sievert.Analysis;
using Sievert.Api;
using Sievert.Core.Rules;
using Sievert.Data;
using Sievert.Data.Entities;

namespace Sievert.Tests;

/// <summary>
/// Arka plan islerinin uctan uca testleri. Gercek PostgreSQL, gercek worker, gercek
/// tarama ve gercek model. Sahte is yok: static-scan gercekten gecici bir git deposunu
/// tariyor, risk-score-all gercekten veritabanindaki commit'leri skorluyor.
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class AnalysisApiTests(PostgresFixture postgres)
{
    private static readonly TimeSpan Patience = TimeSpan.FromSeconds(60);

    [DockerFact]
    public async Task AStaticScanJobRunsAndItsFindingsMatchTheSameScanRunDirectly()
    {
        using TemporaryRepository repository = NewCodeRepository();
        (SievertApiFactory factory, int known, int _, string connection) = Setup(repository.Path);
        using SievertApiFactory owner = factory;
        using HttpClient client = factory.CreateClient();

        JsonElement started = await Start(client, known, "static-scan", HttpStatusCode.Accepted);
        Guid jobId = started.GetProperty("id").GetGuid();

        Assert.Equal("queued", started.GetProperty("status").GetString());

        JsonElement job = await WaitForTerminal(client, jobId);

        AssertSucceeded(job);
        Assert.True(job.GetProperty("isResultComplete").GetBoolean());
        Assert.Null(job.GetProperty("errorCode").GetString());

        // Ayni tarama dogrudan calistirilinca ne veriyorsa job da onu vermeli.
        ScanOutcome direct = ScanService.Run(repository.Path);

        Assert.True(direct.Ok, direct.Error);
        Assert.Equal(direct.Findings.Count, job.GetProperty("resultCount").GetInt32());

        JsonElement summary = job.GetProperty("resultSummary");

        Assert.Equal(direct.Summary.FileCount, summary.GetProperty("scannedFiles").GetInt32());
        Assert.Equal(direct.Summary.SuppressedCount, summary.GetProperty("suppressedCount").GetInt32());
        Assert.Equal(direct.Exemptions.Count, summary.GetProperty("exemptionCount").GetInt32());
        Assert.Equal(direct.Summary.ExcludedFileCount, summary.GetProperty("excludedFileCount").GetInt32());
        Assert.Equal(
            direct.Summary.SkippedDirectories.Count,
            summary.GetProperty("skippedDirectoryCount").GetInt32());

        HashSet<string> expected = [.. direct.Findings.Select(Key)];
        HashSet<string> actual = [.. (await AllFindings(client, jobId)).Select(Key)];

        Assert.Equal(expected, actual);

        // Yollar goreli; sunucunun klasor duzeni sizmiyor.
        foreach (JsonElement finding in await AllFindings(client, jobId))
        {
            Assert.False(Path.IsPathRooted(finding.GetProperty("relativePath").GetString()!));
        }

        using SievertContext check = SievertContextBuilder.Create(connection);

        Assert.DoesNotContain(check.StaticAnalysisFindings, row => row.IsSuppressed);
    }

    [DockerFact]
    public async Task ARiskScoreJobScoresEveryCommitExactlyLikeTheRiskEndpoint()
    {
        (SievertApiFactory factory, int known, int _, string _) = Setup(localPath: null);
        using SievertApiFactory owner = factory;
        using HttpClient client = factory.CreateClient();

        Guid jobId = (await Start(client, known, "risk-score-all", HttpStatusCode.Accepted))
            .GetProperty("id").GetGuid();

        JsonElement job = await WaitForTerminal(client, jobId);

        AssertSucceeded(job);
        Assert.True(job.GetProperty("isResultComplete").GetBoolean());

        // Tohumdaki bes commit'in hepsinin olcusu var.
        Assert.Equal(5, job.GetProperty("resultCount").GetInt32());
        Assert.Equal("polly", job.GetProperty("resultSummary").GetProperty("modelProfile").GetString());

        JsonElement page = await Read(client, $"/api/v1/analyses/{jobId}/risks?pageSize=100");

        Assert.False(page.GetProperty("partial").GetBoolean());
        Assert.Equal(5, page.GetProperty("totalCount").GetInt32());

        foreach (JsonElement snapshot in page.GetProperty("items").EnumerateArray())
        {
            string sha = snapshot.GetProperty("sha").GetString()!;
            JsonElement live = await Read(client, $"/api/v1/repositories/{known}/commits/{sha}/risk");

            // Ayni hesaptan gectikleri icin bit duzeyinde ayni olmalari gerekiyor.
            Assert.Equal(live.GetProperty("rawModelScore").GetDouble(), snapshot.GetProperty("rawModelScore").GetDouble());
            Assert.Equal(live.GetProperty("riskIndex").GetDouble(), snapshot.GetProperty("riskIndex").GetDouble());
            Assert.Equal(live.GetProperty("decisionAt05").GetBoolean(), snapshot.GetProperty("decisionAt05").GetBoolean());
            Assert.Equal(
                live.GetProperty("decisionAtTrainThreshold").GetBoolean(),
                snapshot.GetProperty("decisionAtTrainThreshold").GetBoolean());
            Assert.Equal(live.GetProperty("trainThreshold").GetDouble(), snapshot.GetProperty("trainThreshold").GetDouble());
            Assert.Equal(live.GetProperty("modelProfile").GetString(), snapshot.GetProperty("modelProfile").GetString());
            Assert.False(snapshot.GetProperty("isCalibrated").GetBoolean());

            List<string?> expectedWarnings = [.. live.GetProperty("warnings").EnumerateArray().Select(item => item.GetString())];

            List<string?> actualWarnings = [.. snapshot.GetProperty("warningCodes").EnumerateArray().Select(item => item.GetString())];

            Assert.Equal(expectedWarnings, actualWarnings);
        }
    }

    [DockerFact]
    public async Task ProgressNeverGoesBackwardsAndEndsAtTheTotal()
    {
        (SievertApiFactory factory, int known, int _, string _) = Setup(localPath: null);
        using SievertApiFactory owner = factory;
        using HttpClient client = factory.CreateClient();

        Guid jobId = (await Start(client, known, "risk-score-all", HttpStatusCode.Accepted))
            .GetProperty("id").GetGuid();

        int previous = 0;

        while (true)
        {
            JsonElement job = await Read(client, $"/api/v1/analyses/{jobId}");
            int processed = job.GetProperty("processedItems").GetInt32();

            Assert.True(processed >= previous, $"ilerleme geri gitti: {previous} -> {processed}");
            previous = processed;

            if (IsTerminal(job.GetProperty("status").GetString()!))
            {
                Assert.Equal(job.GetProperty("totalItems").GetInt32(), processed);
                Assert.Equal(100.0, job.GetProperty("progressPercent").GetDouble());

                return;
            }
        }
    }

    [DockerFact]
    public async Task AnUnknownRepositoryGetsNoRiskJobAtAll()
    {
        (SievertApiFactory factory, int _, int unknown, string connection) = Setup(localPath: null);
        using SievertApiFactory owner = factory;
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            $"/api/v1/repositories/{unknown}/analyses",
            new { kind = "risk-score-all" });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);

        JsonElement body = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;

        Assert.Equal("UNKNOWN_REPOSITORY_MODEL", body.GetProperty("errorCode").GetString());

        // Is HIC acilmadi; kuyrukta bastan basarisiz olacagi belli bir is durmuyor.
        using SievertContext check = SievertContextBuilder.Create(connection);

        Assert.Empty(check.AnalysisJobs);
    }

    [DockerFact]
    public async Task AnUnknownKindIsRefused()
    {
        (SievertApiFactory factory, int known, int _, string _) = Setup(localPath: null);
        using SievertApiFactory owner = factory;
        using HttpClient client = factory.CreateClient();

        foreach (object body in (object[])[new { kind = "demo" }, new { kind = (string?)null }, new { }])
        {
            using HttpResponseMessage response = await client.PostAsJsonAsync(
                $"/api/v1/repositories/{known}/analyses",
                body);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            JsonElement problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;

            Assert.Equal("ANALYSIS_KIND_INVALID", problem.GetProperty("errorCode").GetString());
        }
    }

    [DockerFact]
    public async Task AnAlreadyActiveJobBlocksTheNextRequest()
    {
        (SievertApiFactory factory, int known, int _, string connection) = Setup(localPath: null);
        using SievertApiFactory owner = factory;
        using HttpClient client = factory.CreateClient();

        // Is dogrudan kayda yaziliyor, kuyruga girmiyor: boylece worker onu almiyor ve
        // "aktif" durumu testin suresi boyunca kesin olarak duruyor.
        Guid active = await QueueDirectly(connection, known, AnalysisJobKind.StaticScan);

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            $"/api/v1/repositories/{known}/analyses",
            new { kind = "static-scan" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        JsonElement problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;

        Assert.Equal("ANALYSIS_ALREADY_ACTIVE", problem.GetProperty("errorCode").GetString());
        Assert.Equal(active, problem.GetProperty("activeJobId").GetGuid());
        Assert.Equal($"/api/v1/analyses/{active}", problem.GetProperty("activeJobUrl").GetString());
    }

    [DockerFact]
    public async Task TheSameIdempotencyKeyNeverOpensASecondJob()
    {
        (SievertApiFactory factory, int known, int _, string connection) = Setup(localPath: null);
        using SievertApiFactory owner = factory;
        using HttpClient client = factory.CreateClient();

        List<Guid> ids = [];

        for (int attempt = 0; attempt < 10; attempt++)
        {
            using HttpRequestMessage request = new(HttpMethod.Post, $"/api/v1/repositories/{known}/analyses")
            {
                Content = JsonContent.Create(new { kind = "risk-score-all" }),
            };

            request.Headers.Add("Idempotency-Key", "ayni-anahtar");

            using HttpResponseMessage response = await client.SendAsync(request);

            Assert.True(
                response.StatusCode is HttpStatusCode.Accepted or HttpStatusCode.OK,
                $"beklenmeyen durum: {response.StatusCode}");

            Assert.Equal($"/api/v1/analyses/{await IdOf(response)}", response.Headers.Location?.ToString());

            ids.Add(await IdOf(response));
        }

        Assert.Single(ids.Distinct());

        using SievertContext check = SievertContextBuilder.Create(connection);

        Assert.Equal(1, check.AnalysisJobs.Count());
    }

    [DockerFact]
    public async Task AnIdempotencyKeyCannotBeMovedToAnotherRepository()
    {
        (SievertApiFactory factory, int known, int unknown, string _) = Setup(localPath: null);
        using SievertApiFactory owner = factory;
        using HttpClient client = factory.CreateClient();

        await Start(client, known, "risk-score-all", HttpStatusCode.Accepted, "tasinmaz");

        using HttpRequestMessage request = new(HttpMethod.Post, $"/api/v1/repositories/{unknown}/analyses")
        {
            Content = JsonContent.Create(new { kind = "static-scan" }),
        };

        request.Headers.Add("Idempotency-Key", "tasinmaz");

        using HttpResponseMessage response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        JsonElement problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;

        Assert.Equal("IDEMPOTENCY_KEY_REUSED", problem.GetProperty("errorCode").GetString());
    }

    /// <summary>
    /// Yalniz bosluktan olusan anahtar reddediliyor. Tamamen bos bir baslik degeri bu
    /// testte yok, cunku HttpClient bos degerli basligi hic gondermiyor; o durum "anahtar
    /// verilmedi" ile ayni sey oluyor. Bos dizenin reddedildigi AnalysisJobStoreTests
    /// icinde, HTTP katmanina hic girmeden sinaniyor.
    /// </summary>
    [DockerTheory]
    [InlineData("   ")]
    [InlineData("\t")]
    public async Task AWhitespaceIdempotencyKeyIsRefused(string key)
    {
        (SievertApiFactory factory, int known, int _, string _) = Setup(localPath: null);
        using SievertApiFactory owner = factory;
        using HttpClient client = factory.CreateClient();

        using HttpRequestMessage request = new(HttpMethod.Post, $"/api/v1/repositories/{known}/analyses")
        {
            Content = JsonContent.Create(new { kind = "static-scan" }),
        };

        request.Headers.TryAddWithoutValidation("Idempotency-Key", key);

        using HttpResponseMessage response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(
            "IDEMPOTENCY_KEY_INVALID",
            JsonDocument.Parse(await response.Content.ReadAsStringAsync())
                .RootElement.GetProperty("errorCode").GetString());
    }

    [DockerFact]
    public async Task AQueuedJobCanBeCancelledBeforeItStarts()
    {
        (SievertApiFactory factory, int known, int _, string connection) = Setup(localPath: null);
        using SievertApiFactory owner = factory;
        using HttpClient client = factory.CreateClient();

        Guid jobId = await QueueDirectly(connection, known, AnalysisJobKind.StaticScan);

        using HttpResponseMessage cancel = await client.PostAsync($"/api/v1/analyses/{jobId}/cancel", null);

        Assert.Equal(HttpStatusCode.OK, cancel.StatusCode);

        JsonElement job = JsonDocument.Parse(await cancel.Content.ReadAsStringAsync()).RootElement;

        Assert.Equal("canceled", job.GetProperty("status").GetString());
        Assert.False(job.GetProperty("isResultComplete").GetBoolean());
        Assert.True(job.GetProperty("cancellationRequested").GetBoolean());

        // Iptal idempotent: ikinci istek durumu degistirmiyor.
        using HttpResponseMessage again = await client.PostAsync($"/api/v1/analyses/{jobId}/cancel", null);

        Assert.Equal(HttpStatusCode.OK, again.StatusCode);
        Assert.Equal(
            "canceled",
            JsonDocument.Parse(await again.Content.ReadAsStringAsync()).RootElement
                .GetProperty("status").GetString());
    }

    [DockerFact]
    public async Task CancellingAFinishedJobDoesNotRewriteIt()
    {
        (SievertApiFactory factory, int known, int _, string _) = Setup(localPath: null);
        using SievertApiFactory owner = factory;
        using HttpClient client = factory.CreateClient();

        Guid jobId = (await Start(client, known, "risk-score-all", HttpStatusCode.Accepted))
            .GetProperty("id").GetGuid();

        await WaitForTerminal(client, jobId);

        using HttpResponseMessage cancel = await client.PostAsync($"/api/v1/analyses/{jobId}/cancel", null);

        Assert.Equal(HttpStatusCode.OK, cancel.StatusCode);

        JsonElement job = JsonDocument.Parse(await cancel.Content.ReadAsStringAsync()).RootElement;

        Assert.Equal("succeeded", job.GetProperty("status").GetString());
        Assert.True(job.GetProperty("isResultComplete").GetBoolean());
    }

    [DockerFact]
    public async Task TheWrongResultEndpointIsRefused()
    {
        (SievertApiFactory factory, int known, int _, string _) = Setup(localPath: null);
        using SievertApiFactory owner = factory;
        using HttpClient client = factory.CreateClient();

        Guid jobId = (await Start(client, known, "risk-score-all", HttpStatusCode.Accepted))
            .GetProperty("id").GetGuid();

        await WaitForTerminal(client, jobId);

        using HttpResponseMessage response = await client.GetAsync($"/api/v1/analyses/{jobId}/findings");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(
            "ANALYSIS_RESULT_TYPE_MISMATCH",
            JsonDocument.Parse(await response.Content.ReadAsStringAsync())
                .RootElement.GetProperty("errorCode").GetString());
    }

    [DockerFact]
    public async Task AMissingJobIsNotFound()
    {
        (SievertApiFactory factory, int _, int _, string _) = Setup(localPath: null);
        using SievertApiFactory owner = factory;
        using HttpClient client = factory.CreateClient();

        foreach (string path in (string[])
        [
            $"/api/v1/analyses/{Guid.Empty}",
            $"/api/v1/analyses/{Guid.Empty}/findings",
            $"/api/v1/analyses/{Guid.Empty}/risks",
        ])
        {
            using HttpResponseMessage response = await client.GetAsync(path);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Equal(
                "ANALYSIS_NOT_FOUND",
                JsonDocument.Parse(await response.Content.ReadAsStringAsync())
                    .RootElement.GetProperty("errorCode").GetString());
        }
    }

    [DockerFact]
    public async Task AJobWithoutALocalPathFailsWithAClearCode()
    {
        (SievertApiFactory factory, int known, int _, string _) = Setup(localPath: null);
        using SievertApiFactory owner = factory;
        using HttpClient client = factory.CreateClient();

        Guid jobId = (await Start(client, known, "static-scan", HttpStatusCode.Accepted))
            .GetProperty("id").GetGuid();

        JsonElement job = await WaitForTerminal(client, jobId);

        Assert.Equal("failed", job.GetProperty("status").GetString());
        Assert.Equal("REPOSITORY_PATH_UNAVAILABLE", job.GetProperty("errorCode").GetString());
        Assert.False(job.GetProperty("isResultComplete").GetBoolean());
    }

    [DockerFact]
    public async Task AFolderThatIsNotAGitRepositoryIsRefused()
    {
        string folder = Path.Combine(Path.GetTempPath(), "sievert-not-git-" + Guid.NewGuid().ToString("n"));
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, "Ornek.cs"), "public class Ornek { }");

        try
        {
            (SievertApiFactory factory, int known, int _, string _) = Setup(folder);
            using SievertApiFactory owner = factory;
            using HttpClient client = factory.CreateClient();

            Guid jobId = (await Start(client, known, "static-scan", HttpStatusCode.Accepted))
                .GetProperty("id").GetGuid();

            JsonElement job = await WaitForTerminal(client, jobId);

            Assert.Equal("failed", job.GetProperty("status").GetString());
            Assert.Equal("REPOSITORY_NOT_GIT", job.GetProperty("errorCode").GetString());
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    [DockerFact]
    public async Task TheJobListIsPagedAndFilterable()
    {
        using TemporaryRepository repository = NewCodeRepository();
        (SievertApiFactory factory, int known, int _, string _) = Setup(repository.Path);
        using SievertApiFactory owner = factory;
        using HttpClient client = factory.CreateClient();

        Guid scan = (await Start(client, known, "static-scan", HttpStatusCode.Accepted)).GetProperty("id").GetGuid();
        await WaitForTerminal(client, scan);

        Guid risk = (await Start(client, known, "risk-score-all", HttpStatusCode.Accepted)).GetProperty("id").GetGuid();
        await WaitForTerminal(client, risk);

        JsonElement all = await Read(client, $"/api/v1/repositories/{known}/analyses");

        Assert.Equal(2, all.GetProperty("totalCount").GetInt32());

        JsonElement onlyScan = await Read(client, $"/api/v1/repositories/{known}/analyses?kind=static-scan");

        Assert.Equal(1, onlyScan.GetProperty("totalCount").GetInt32());

        JsonElement succeeded = await Read(client, $"/api/v1/repositories/{known}/analyses?status=succeeded");

        Assert.Equal(2, succeeded.GetProperty("totalCount").GetInt32());

        using HttpResponseMessage bad = await client.GetAsync($"/api/v1/repositories/{known}/analyses?status=uydurma");

        Assert.Equal(HttpStatusCode.BadRequest, bad.StatusCode);
    }

    [DockerFact]
    public async Task NoAnalysisResponseLeaksTheServerLayout()
    {
        using TemporaryRepository repository = NewCodeRepository();
        (SievertApiFactory factory, int known, int _, string _) = Setup(repository.Path);
        using SievertApiFactory owner = factory;
        using HttpClient client = factory.CreateClient();

        Guid jobId = (await Start(client, known, "static-scan", HttpStatusCode.Accepted)).GetProperty("id").GetGuid();
        await WaitForTerminal(client, jobId);

        foreach (string path in (string[])
        [
            "/api/v1/health",
            $"/api/v1/analyses/{jobId}",
            $"/api/v1/analyses/{jobId}/findings?pageSize=100",
            $"/api/v1/repositories/{known}/analyses",
        ])
        {
            string body = await client.GetStringAsync(path);

            Assert.DoesNotContain(repository.Path, body, StringComparison.Ordinal);
            Assert.DoesNotContain(Path.GetTempPath(), body, StringComparison.Ordinal);
            Assert.DoesNotContain("Password=", body, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("localPath", body, StringComparison.OrdinalIgnoreCase);
        }
    }

    [DockerFact]
    public async Task HealthReportsTheQueueAndTheWorker()
    {
        (SievertApiFactory factory, int _, int _, string _) = Setup(localPath: null);
        using SievertApiFactory owner = factory;
        using HttpClient client = factory.CreateClient();

        JsonElement analysis = (await Read(client, "/api/v1/health")).GetProperty("analysis");

        Assert.Equal(100, analysis.GetProperty("queueCapacity").GetInt32());
        Assert.Equal(1, analysis.GetProperty("workerConcurrency").GetInt32());
        Assert.Equal(0, analysis.GetProperty("runningJobs").GetInt32());
        Assert.Equal(0, analysis.GetProperty("queuedJobs").GetInt32());
        Assert.NotEqual(JsonValueKind.Undefined, analysis.GetProperty("lastRecovery").ValueKind);
    }

    /// <summary>
    /// Acilis tanilama kodu hata katalogunda degil, o yuzden OpenAPI'de de olmamali.
    /// Bir istek karsiliginda hicbir zaman donmeyen bir kodu sozlesmeye koymak, istemciye
    /// olmayan bir cevabi bekletmek olurdu.
    /// </summary>
    [DockerFact]
    public async Task TheStartupOnlyRemoteCodeIsNotInTheContract()
    {
        (SievertApiFactory factory, int _, int _, string _) = Setup(localPath: null);
        using SievertApiFactory owner = factory;
        using HttpClient client = factory.CreateClient();

        string document = await client.GetStringAsync("/openapi/v1.json");

        Assert.DoesNotContain(RemoteAccessGuard.DiagnosticCode, document, StringComparison.Ordinal);

        // Yeni kaynak kodlari ise gercekten donebiliyor; katalogda duruyorlar.
        Assert.Equal("REPOSITORY_WORKTREE_DIRTY", ApiError.RepositoryWorktreeDirty);
        Assert.Equal("REPOSITORY_CHANGED_DURING_ANALYSIS", ApiError.RepositoryChangedDuringAnalysis);
        Assert.Equal("REPOSITORY_HEAD_UNAVAILABLE", ApiError.RepositoryHeadUnavailable);
    }

    private static string Key(Finding finding) =>
        $"{finding.RuleCode}|{finding.Severity.ToString().ToLowerInvariant()}|{finding.FilePath}|{finding.Line}";

    private static string Key(JsonElement finding) =>
        $"{finding.GetProperty("ruleCode").GetString()}|{finding.GetProperty("severity").GetString()}"
        + $"|{finding.GetProperty("relativePath").GetString()}|{finding.GetProperty("line").GetInt32()}";

    private static bool IsTerminal(string status) =>
        status is "succeeded" or "failed" or "canceled";

    /// <summary>Basarisiz oldugunda hata kodunu da yazar; yoksa "failed" tek basina sebep soylemiyor.</summary>
    private static void AssertSucceeded(JsonElement job)
    {
        Assert.True(
            job.GetProperty("status").GetString() == "succeeded",
            $"durum {job.GetProperty("status").GetString()}, "
            + $"kod {job.GetProperty("errorCode").GetString()}, "
            + $"mesaj {job.GetProperty("errorMessage").GetString()}");
    }

    /// <summary>
    /// Kurallarin gercekten bulgu urettigi kucuk bir depo. Icerik uydurma degil: her
    /// dosya bilinen bir kalibin ornegi.
    /// </summary>
    private static TemporaryRepository NewCodeRepository()
    {
        TemporaryRepository repository = new();

        repository.CommitMany(
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["src/Islem.cs"] = """
                using System.Threading.Tasks;

                public class Islem
                {
                    public async void Baslat() => await Calis();

                    public Task Calis() => Task.CompletedTask;

                    public void Bekle() => Calis().Wait();
                }
                """,
                ["src/Dongu.cs"] = """
                using System.Collections.Generic;
                using System.Linq;

                public class Dongu
                {
                    public void Tara(List<int> kimlikler, IQueryable<string> kayitlarDb)
                    {
                        foreach (int kimlik in kimlikler)
                        {
                            _ = kayitlarDb.Where(kayit => kayit.Length == kimlik).ToList();
                        }
                    }
                }
                """,
                ["tests/IslemTests.cs"] = """
                using System.Threading.Tasks;

                public class IslemTests
                {
                    public Task CalisTask() => Task.CompletedTask;

                    public void Bekle() => CalisTask().Wait();
                }
                """,
            },
            "ilk kod");

        return repository;
    }

    private (SievertApiFactory Factory, int Known, int Unknown, string Connection) Setup(string? localPath)
    {
        string connection = postgres.NewDatabaseConnectionString();

        using SievertContext context = SievertContextBuilder.Create(connection);
        (int known, int unknown) = ApiSeed.Write(context, localPath);

        return (new SievertApiFactory(connection), known, unknown, connection);
    }

    /// <summary>Kuyruga girmeden dogrudan kayda yazilan is; worker onu almaz.</summary>
    private static async Task<Guid> QueueDirectly(string connection, int repositoryId, AnalysisJobKind kind)
    {
        using SievertContext context = SievertContextBuilder.Create(connection);

        JobCreateResult result = await new AnalysisJobStore(context).CreateAsync(
            repositoryId,
            kind,
            null,
            DateTimeOffset.UtcNow);

        return result.Job!.Id;
    }

    private static async Task<Guid> IdOf(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.GetProperty("id").GetGuid();

    private static async Task<JsonElement> Start(
        HttpClient client,
        int repositoryId,
        string kind,
        HttpStatusCode expected,
        string? idempotencyKey = null)
    {
        using HttpRequestMessage request = new(HttpMethod.Post, $"/api/v1/repositories/{repositoryId}/analyses")
        {
            Content = JsonContent.Create(new { kind }),
        };

        if (idempotencyKey is not null)
        {
            request.Headers.Add("Idempotency-Key", idempotencyKey);
        }

        using HttpResponseMessage response = await client.SendAsync(request);
        string body = await response.Content.ReadAsStringAsync();

        Assert.True(expected == response.StatusCode, $"{(int)response.StatusCode}: {body}");
        Assert.NotNull(response.Headers.Location);

        return JsonDocument.Parse(body).RootElement.Clone();
    }

    private static async Task<JsonElement> WaitForTerminal(HttpClient client, Guid jobId)
    {
        DateTimeOffset deadline = DateTimeOffset.UtcNow + Patience;

        while (DateTimeOffset.UtcNow < deadline)
        {
            JsonElement job = await Read(client, $"/api/v1/analyses/{jobId}");

            if (IsTerminal(job.GetProperty("status").GetString()!))
            {
                return job;
            }
        }

        throw new TimeoutException($"Is {Patience.TotalSeconds} saniyede bitmedi: {jobId}");
    }

    private static async Task<List<JsonElement>> AllFindings(HttpClient client, Guid jobId)
    {
        JsonElement page = await Read(client, $"/api/v1/analyses/{jobId}/findings?pageSize=100");

        return [.. page.GetProperty("items").EnumerateArray().Select(item => item.Clone())];
    }

    private static async Task<JsonElement> Read(HttpClient client, string path)
    {
        using HttpResponseMessage response = await client.GetAsync(path);
        string body = await response.Content.ReadAsStringAsync();

        Assert.True(response.IsSuccessStatusCode, $"{path} -> {(int)response.StatusCode}: {body}");

        return JsonDocument.Parse(body).RootElement.Clone();
    }
}
