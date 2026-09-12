using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

using Microsoft.EntityFrameworkCore;

using Sievert.Analysis;
using Sievert.Core.Rules;
using Sievert.Data;
using Sievert.Data.Entities;

namespace Sievert.Measure;

/// <summary>
/// Asama 6 Adim 3: arka plan islerinin olcumu.
///
/// Gercek API sureci, gercek PostgreSQL, gercek uc depo. Sahte is yok: static-scan
/// depolarin calisma agacini tariyor, risk-score-all veritabanindaki butun metrikli
/// commit'leri skorluyor.
/// </summary>
public static class BackgroundJobsCommand
{
    private const string BaseUrl = "http://127.0.0.1:5199";

    /// <summary>Esdegerlik ornekleminin tohumu; ayni kosu ayni commit'leri seciyor.</summary>
    private const int Seed = 20260913;

    private const int EquivalenceSample = 100;

    // sievert:disable SV006 komut satirindan tek sefer kosuyor; iptali Ctrl+C yapiyor
    public static async Task<int> RunAsync(SievertContext context, string[] args)
    {
        string repositoryRoot = Path.GetFullPath(args[1]);
        string codeCommit = args[2];
        string outputPath = args[3];

        List<Target> targets =
        [
            .. context.Repositories
                .AsNoTracking()
                .OrderBy(row => row.Identity)
                .Select(row => new Target(row.Id, row.Identity, row.Name, row.LocalPath))
                .ToList()
        ];

        Console.WriteLine($"Depolar: {string.Join(", ", targets.Select(target => target.Name))}");
        Console.WriteLine();

        List<StaticResult> statics = [];
        List<RiskResult> risks = [];
        CancellationResult cancellation;
        IdempotencyResult idempotency;
        RecoveryResult recovery;
        long peak;

        using (ApiProcess api = ApiProcess.Start(repositoryRoot, logQueries: false))
        {
            using HttpClient client = Client();

            await api.WaitUntilHealthyAsync(client);

            foreach (Target target in targets)
            {
                statics.Add(await MeasureStaticAsync(client, target));
            }

            foreach (Target target in targets)
            {
                risks.Add(await MeasureRiskAsync(client, context, target));
            }

            cancellation = await MeasureCancellationAsync(client, targets);
            idempotency = await MeasureIdempotencyAsync(client, context, targets[0]);

            // Onceki asamalarin actigi isler bitmeden yeniden baslatma olcumune
            // gecilemez: ayni repo ve tur icin tekillik kisiti yeni isi reddeder.
            await WaitForIdleAsync(client);

            peak = api.PeakWorkingSetBytes;
            api.Stop();
        }

        recovery = await MeasureRecoveryAsync(repositoryRoot, context, targets);

        string json = Render(codeCommit, statics, risks, cancellation, idempotency, recovery, peak);
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        File.WriteAllText(outputPath, json, new UTF8Encoding(false));

        Console.WriteLine();
        Console.WriteLine($"{outputPath} yazildi.");

        return 0;
    }

    private static HttpClient Client() =>
        new() { BaseAddress = new Uri(BaseUrl), Timeout = TimeSpan.FromMinutes(30) };

    // ---------------------------------------------------------------- static-scan

    private static async Task<StaticResult> MeasureStaticAsync(HttpClient client, Target target)
    {
        Console.WriteLine($"== static-scan: {target.Name} ==");

        if (target.LocalPath is null || !Directory.Exists(target.LocalPath))
        {
            Console.WriteLine("  yerel klasor yok, atlandi");

            return StaticResult.Skipped(target.Name);
        }

        Stopwatch total = Stopwatch.StartNew();
        JobHandle handle = await StartAsync(client, target.Id, "static-scan");
        JsonElement job = await WaitAsync(client, handle.Id);
        total.Stop();

        string status = job.GetProperty("status").GetString()!;

        if (status != "succeeded")
        {
            Console.WriteLine($"  basarisiz: {job.GetProperty("errorCode").GetString()}");

            return StaticResult.Skipped(target.Name);
        }

        JsonElement summary = job.GetProperty("resultSummary");

        // Ayni tarama dogrudan calistirilinca ne veriyor?
        Stopwatch directWatch = Stopwatch.StartNew();
        ScanOutcome direct = ScanService.Run(target.LocalPath);
        directWatch.Stop();

        List<JsonElement> reported = await AllFindingsAsync(client, handle.Id);

        HashSet<string> expected = [.. direct.Findings.Select(Key)];
        HashSet<string> actual = [.. reported.Select(Key)];

        List<string> differences = [.. expected.Except(expected.Intersect(actual)).Take(20)];
        differences.AddRange(actual.Except(expected).Take(20));

        double queueWait = (job.GetProperty("startedAtUtc").GetDateTimeOffset()
            - job.GetProperty("requestedAtUtc").GetDateTimeOffset()).TotalMilliseconds;

        double run = (job.GetProperty("completedAtUtc").GetDateTimeOffset()
            - job.GetProperty("startedAtUtc").GetDateTimeOffset()).TotalMilliseconds;

        StaticResult result = new(
            target.Name,
            true,
            summary.GetProperty("scannedFiles").GetInt32(),
            job.GetProperty("resultCount").GetInt32(),
            summary.GetProperty("suppressedCount").GetInt32(),
            summary.GetProperty("exemptionCount").GetInt32(),
            summary.GetProperty("excludedFileCount").GetInt32(),
            summary.GetProperty("skippedDirectoryCount").GetInt32(),
            queueWait,
            run,
            total.Elapsed.TotalMilliseconds,
            directWatch.Elapsed.TotalMilliseconds,
            summary.GetProperty("progressWrites").GetInt32(),
            direct.Findings.Count,
            differences.Count,
            differences);

        Console.WriteLine(
            $"  dosya {result.ScannedFiles}, bulgu {result.FindingCount}, susturma {result.SuppressedCount}, "
            + $"muafiyet {result.ExemptionCount}");
        Console.WriteLine(
            $"  kuyruk {result.QueueWaitMs:F0} ms, calisma {result.RunMs:F0} ms, toplam {result.TotalMs:F0} ms, "
            + $"ilerleme yazimi {result.ProgressWrites}");
        Console.WriteLine($"  dogrudan tarama {result.DirectMs:F0} ms, CLI farki {result.DifferenceCount}");

        foreach (string difference in differences)
        {
            Console.WriteLine($"    fark: {difference}");
        }

        return result;
    }

    // ------------------------------------------------------------ risk-score-all

    private static async Task<RiskResult> MeasureRiskAsync(HttpClient client, SievertContext context, Target target)
    {
        Console.WriteLine($"== risk-score-all: {target.Name} ==");

        Stopwatch total = Stopwatch.StartNew();
        JobHandle handle = await StartAsync(client, target.Id, "risk-score-all");

        if (handle.Status != HttpStatusCode.Accepted)
        {
            Console.WriteLine($"  is acilmadi: {handle.Status}");

            return RiskResult.Skipped(target.Name);
        }

        JsonElement job = await WaitAsync(client, handle.Id);
        total.Stop();

        string status = job.GetProperty("status").GetString()!;

        if (status != "succeeded")
        {
            Console.WriteLine($"  basarisiz: {job.GetProperty("errorCode").GetString()}");

            return RiskResult.Skipped(target.Name);
        }

        JsonElement summary = job.GetProperty("resultSummary");

        int expected = await context.Commits
            .AsNoTracking()
            .CountAsync(commit => commit.RepositoryId == target.Id
                && context.CommitMetrics.Any(metric => metric.CommitId == commit.Id));

        (int compared, int mismatched, double worst) = await CompareAsync(client, context, target, handle.Id);

        double queueWait = (job.GetProperty("startedAtUtc").GetDateTimeOffset()
            - job.GetProperty("requestedAtUtc").GetDateTimeOffset()).TotalMilliseconds;

        double run = (job.GetProperty("completedAtUtc").GetDateTimeOffset()
            - job.GetProperty("startedAtUtc").GetDateTimeOffset()).TotalMilliseconds;

        RiskResult result = new(
            target.Name,
            true,
            expected,
            job.GetProperty("resultCount").GetInt32(),
            queueWait,
            run,
            total.Elapsed.TotalMilliseconds,
            run <= 0 ? 0 : job.GetProperty("resultCount").GetInt32() / (run / 1000.0),
            summary.GetProperty("progressWrites").GetInt32(),
            summary.GetProperty("modelLoads").GetInt32(),
            summary.GetProperty("explanationMismatches").GetInt32(),
            compared,
            mismatched,
            worst);

        Console.WriteLine(
            $"  commit {result.ExpectedCommits}, yazilan {result.ResultCount}, kuyruk {result.QueueWaitMs:F0} ms, "
            + $"calisma {result.RunMs:F0} ms");
        Console.WriteLine(
            $"  {result.CommitsPerSecond:F0} commit/sn, ilerleme yazimi {result.ProgressWrites}, "
            + $"model yukleme {result.ModelLoads}, aciklama uyusmazligi {result.ExplanationMismatches}");
        Console.WriteLine(
            $"  esdegerlik: {result.Compared} commit karsilastirildi, farkli {result.Mismatched}, "
            + $"en buyuk fark {result.WorstScoreDifference:E3}");

        return result;
    }

    /// <summary>Sabit tohumla secilmis commit'leri risk ucuyle karsilastirir.</summary>
    private static async Task<(int Compared, int Mismatched, double Worst)> CompareAsync(
        HttpClient client,
        SievertContext context,
        Target target,
        Guid jobId)
    {
        List<string> shas = await context.Commits
            .AsNoTracking()
            .Where(commit => commit.RepositoryId == target.Id
                && context.CommitRiskSnapshots.Any(snapshot =>
                    snapshot.AnalysisJobId == jobId && snapshot.CommitId == commit.Id))
            .OrderBy(commit => commit.Sha)
            .Select(commit => commit.Sha)
            .ToListAsync();

        Random random = new(Seed);

        for (int index = shas.Count - 1; index > 0; index--)
        {
            int other = random.Next(index + 1);
            (shas[index], shas[other]) = (shas[other], shas[index]);
        }

        Dictionary<string, JsonElement> snapshots = await SnapshotsAsync(client, jobId);

        int compared = 0;
        int mismatched = 0;
        double worst = 0.0;

        foreach (string sha in shas.Take(EquivalenceSample))
        {
            if (!snapshots.TryGetValue(sha, out JsonElement snapshot))
            {
                continue;
            }

            JsonElement live = await ReadAsync(client, $"/api/v1/repositories/{target.Id}/commits/{sha}/risk");

            double difference = Math.Abs(
                live.GetProperty("rawModelScore").GetDouble() - snapshot.GetProperty("rawModelScore").GetDouble());

            worst = Math.Max(worst, difference);
            compared++;

            if (difference != 0.0
                || live.GetProperty("riskIndex").GetDouble() != snapshot.GetProperty("riskIndex").GetDouble()
                || live.GetProperty("decisionAt05").GetBoolean() != snapshot.GetProperty("decisionAt05").GetBoolean()
                || live.GetProperty("decisionAtTrainThreshold").GetBoolean()
                    != snapshot.GetProperty("decisionAtTrainThreshold").GetBoolean()
                || live.GetProperty("trainThreshold").GetDouble() != snapshot.GetProperty("trainThreshold").GetDouble()
                || live.GetProperty("modelProfile").GetString() != snapshot.GetProperty("modelProfile").GetString()
                || !WarningsMatch(live, snapshot))
            {
                mismatched++;

                Console.WriteLine($"    esdegerlik farki: {sha[..12]} fark {difference:E3}");
            }
        }

        return (compared, mismatched, worst);
    }

    private static bool WarningsMatch(JsonElement live, JsonElement snapshot)
    {
        List<string?> first = [.. live.GetProperty("warnings").EnumerateArray().Select(item => item.GetString())];
        List<string?> second = [.. snapshot.GetProperty("warningCodes").EnumerateArray().Select(item => item.GetString())];

        return first.SequenceEqual(second);
    }

    private static async Task<Dictionary<string, JsonElement>> SnapshotsAsync(HttpClient client, Guid jobId)
    {
        Dictionary<string, JsonElement> all = new(StringComparer.Ordinal);
        int page = 1;

        while (true)
        {
            JsonElement body = await ReadAsync(
                client,
                $"/api/v1/analyses/{jobId}/risks?page={page}&pageSize=100&order=newest");

            int count = 0;

            foreach (JsonElement item in body.GetProperty("items").EnumerateArray())
            {
                all[item.GetProperty("sha").GetString()!] = item.Clone();
                count++;
            }

            if (count == 0 || all.Count >= body.GetProperty("totalCount").GetInt32())
            {
                return all;
            }

            page++;
        }
    }

    // --------------------------------------------------------------------- iptal

    private static async Task<CancellationResult> MeasureCancellationAsync(HttpClient client, List<Target> targets)
    {
        Console.WriteLine("== iptal ==");

        Target longest = targets.MaxBy(target => target.Id)!;
        Target other = targets.First(target => target.Id != longest.Id);

        // Uzun is kosarken ikincisi kuyrukta bekliyor (es zamanlilik 1).
        JobHandle running = await StartAsync(client, longest.Id, "risk-score-all");
        JobHandle queued = await StartAsync(client, other.Id, "risk-score-all");

        Stopwatch queuedWatch = Stopwatch.StartNew();
        using HttpResponseMessage queuedCancel = await client.PostAsync($"/api/v1/analyses/{queued.Id}/cancel", null);
        JsonElement queuedJob = JsonDocument.Parse(await queuedCancel.Content.ReadAsStringAsync()).RootElement;
        queuedWatch.Stop();

        bool queuedNeverStarted = queuedJob.GetProperty("startedAtUtc").ValueKind == JsonValueKind.Null;

        // Kosan isin o ana kadar yazdigi satir sayisi.
        JsonElement before = await ReadAsync(client, $"/api/v1/analyses/{running.Id}");
        int processedBefore = before.GetProperty("processedItems").GetInt32();

        Stopwatch runningWatch = Stopwatch.StartNew();
        using HttpResponseMessage runningCancel = await client.PostAsync($"/api/v1/analyses/{running.Id}/cancel", null);
        HttpStatusCode runningCode = runningCancel.StatusCode;

        JsonElement terminal = await WaitAsync(client, running.Id);
        runningWatch.Stop();

        int processedAfter = terminal.GetProperty("processedItems").GetInt32();

        // Terminal ise iptal istegi: durum degismemeli.
        using HttpResponseMessage again = await client.PostAsync($"/api/v1/analyses/{running.Id}/cancel", null);
        JsonElement unchanged = JsonDocument.Parse(await again.Content.ReadAsStringAsync()).RootElement;

        using HttpResponseMessage third = await client.PostAsync($"/api/v1/analyses/{running.Id}/cancel", null);
        JsonElement stillUnchanged = JsonDocument.Parse(await third.Content.ReadAsStringAsync()).RootElement;

        CancellationResult result = new(
            (int)queuedCancel.StatusCode,
            queuedJob.GetProperty("status").GetString()!,
            queuedWatch.Elapsed.TotalMilliseconds,
            queuedNeverStarted,
            (int)runningCode,
            terminal.GetProperty("status").GetString()!,
            runningWatch.Elapsed.TotalMilliseconds,
            processedBefore,
            processedAfter,
            processedAfter - processedBefore,
            terminal.GetProperty("isResultComplete").GetBoolean(),
            unchanged.GetProperty("status").GetString()!,
            string.Equals(
                unchanged.GetProperty("status").GetString(),
                stillUnchanged.GetProperty("status").GetString(),
                StringComparison.Ordinal));

        Console.WriteLine(
            $"  kuyruktaki is: {result.QueuedStatusCode} {result.QueuedStatus}, {result.QueuedCancelMs:F0} ms, "
            + $"hic baslamadi: {result.QueuedNeverStarted}");
        Console.WriteLine(
            $"  kosan is: {result.RunningStatusCode} -> {result.RunningStatus}, {result.RunningCancelMs:F0} ms, "
            + $"iptal sonrasi islenen ek oge {result.ProcessedAfterCancel}");
        Console.WriteLine(
            $"  sonuc tam mi: {result.IsResultComplete}, terminal iptal: {result.TerminalStatus}, "
            + $"idempotent: {result.TerminalIdempotent}");

        return result;
    }

    // --------------------------------------------------------------- idempotency

    private static async Task<IdempotencyResult> MeasureIdempotencyAsync(
        HttpClient client,
        SievertContext context,
        Target target)
    {
        Console.WriteLine("== tekrar anahtari ve yaris ==");

        int before = await context.AnalysisJobs.CountAsync();

        // 1) Ayni anahtarla ardisik 10 istek.
        List<Guid> sequential = [];

        for (int attempt = 0; attempt < 10; attempt++)
        {
            sequential.Add(await PostWithKeyAsync(client, target.Id, "olcum-ardisik"));
        }

        // 2) Ayni anahtarla es zamanli 10 istek.
        List<Task<Guid>> concurrent = [];

        for (int attempt = 0; attempt < 10; attempt++)
        {
            concurrent.Add(PostWithKeyAsync(client, target.Id, "olcum-eszamanli"));
        }

        Guid[] sameKey = await Task.WhenAll(concurrent);

        // 3) Farkli anahtarlarla es zamanli 10 istek.
        List<Task<HttpStatusCode>> racing = [];

        for (int attempt = 0; attempt < 10; attempt++)
        {
            int index = attempt;
            racing.Add(PostStatusAsync(client, target.Id, $"olcum-yaris-{index}"));
        }

        HttpStatusCode[] codes = await Task.WhenAll(racing);

        int created = await context.AnalysisJobs.CountAsync() - before;

        IdempotencyResult result = new(
            sequential.Distinct().Count(),
            sameKey.Distinct().Count(),
            codes.Count(code => code is HttpStatusCode.Accepted or HttpStatusCode.OK),
            codes.Count(code => code == HttpStatusCode.Conflict),
            created);

        Console.WriteLine($"  ardisik 10 ayni anahtar -> farkli is: {result.SequentialDistinctJobs}");
        Console.WriteLine($"  es zamanli 10 ayni anahtar -> farkli is: {result.ConcurrentSameKeyDistinctJobs}");
        Console.WriteLine(
            $"  es zamanli 10 farkli anahtar -> kabul {result.RaceAccepted}, catisma {result.RaceConflicted}");
        Console.WriteLine($"  toplam yeni is satiri: {result.NewJobRows}");

        return result;
    }

    private static async Task<Guid> PostWithKeyAsync(HttpClient client, int repositoryId, string key)
    {
        using HttpRequestMessage request = new(HttpMethod.Post, $"/api/v1/repositories/{repositoryId}/analyses")
        {
            Content = JsonContent.Create(new { kind = "static-scan" }),
        };

        request.Headers.Add("Idempotency-Key", key);

        using HttpResponseMessage response = await client.SendAsync(request);
        JsonElement body = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;

        return body.TryGetProperty("id", out JsonElement id) ? id.GetGuid() : Guid.Empty;
    }

    private static async Task<HttpStatusCode> PostStatusAsync(HttpClient client, int repositoryId, string key)
    {
        using HttpRequestMessage request = new(HttpMethod.Post, $"/api/v1/repositories/{repositoryId}/analyses")
        {
            Content = JsonContent.Create(new { kind = "risk-score-all" }),
        };

        request.Headers.Add("Idempotency-Key", key);

        using HttpResponseMessage response = await client.SendAsync(request);

        return response.StatusCode;
    }

    // ------------------------------------------------------------------ kurtarma

    /// <summary>
    /// Yeniden baslatma kurtarmasi. Is kosarken surec oldurulup API yeniden aciliyor.
    /// </summary>
    private static async Task<RecoveryResult> MeasureRecoveryAsync(
        string repositoryRoot,
        SievertContext context,
        List<Target> targets)
    {
        Console.WriteLine("== yeniden baslatma ==");

        Target biggest = targets.MaxBy(target => target.Id)!;
        Target other = targets.First(target => target.Id != biggest.Id);

        Guid running;
        Guid queued;

        using (ApiProcess api = ApiProcess.Start(repositoryRoot, logQueries: false))
        {
            using HttpClient client = Client();
            await api.WaitUntilHealthyAsync(client);

            running = (await StartAsync(client, biggest.Id, "risk-score-all")).Id;
            queued = (await StartAsync(client, other.Id, "risk-score-all")).Id;

            // Is gercekten kosmaya baslasin diye bekleniyor.
            await WaitForStatusAsync(client, running, "running");

            // Sureci oldur: "uygulama duzgun kapandi" degil, "surec gitti" durumu.
            api.Stop();
        }

        using (ApiProcess restarted = ApiProcess.Start(repositoryRoot, logQueries: false))
        {
            using HttpClient client = Client();
            await restarted.WaitUntilHealthyAsync(client);

            JsonElement interrupted = await ReadAsync(client, $"/api/v1/analyses/{running}");
            JsonElement health = await ReadAsync(client, "/api/v1/health");
            JsonElement recovery = health.GetProperty("analysis").GetProperty("lastRecovery");

            // Ikinci kurtarma: degisiklik 0 olmali.
            using SievertContext direct = SievertContextBuilder.Create(
                ConnectionString.Find(Directory.GetCurrentDirectory()).Value!);

            RecoveryReport second = await new AnalysisJobStore(direct).RecoverAsync(DateTimeOffset.UtcNow);

            JsonElement queuedJob = await ReadAsync(client, $"/api/v1/analyses/{queued}");

            RecoveryResult result = new(
                interrupted.GetProperty("status").GetString()!,
                interrupted.GetProperty("errorCode").GetString(),
                interrupted.GetProperty("isResultComplete").GetBoolean(),
                recovery.ValueKind == JsonValueKind.Null ? 0 : recovery.GetProperty("interrupted").GetInt32(),
                recovery.ValueKind == JsonValueKind.Null ? 0 : recovery.GetProperty("requeued").GetInt32(),
                second.ChangedRows,
                queuedJob.GetProperty("status").GetString()!);

            Console.WriteLine(
                $"  kosan is -> {result.InterruptedStatus} / {result.InterruptedErrorCode}, "
                + $"sonuc tam mi: {result.InterruptedResultComplete}");
            Console.WriteLine(
                $"  kurtarma: yarida kalan {result.RecoveredInterrupted}, yeniden kuyruga {result.RecoveredRequeued}");
            Console.WriteLine($"  ikinci kurtarma degisiklik: {result.SecondRecoveryChanges}");
            Console.WriteLine($"  kuyruktaki isin durumu: {result.QueuedStatus}");

            restarted.Stop();

            return result;
        }
    }

    // -------------------------------------------------------------------- yardim

    private static string Key(Finding finding) =>
        $"{finding.RuleCode}|{finding.Severity.ToString().ToLowerInvariant()}|{finding.FilePath}|{finding.Line}";

    private static string Key(JsonElement finding) =>
        $"{finding.GetProperty("ruleCode").GetString()}|{finding.GetProperty("severity").GetString()}"
        + $"|{finding.GetProperty("relativePath").GetString()}|{finding.GetProperty("line").GetInt32()}";

    private static async Task<List<JsonElement>> AllFindingsAsync(HttpClient client, Guid jobId)
    {
        List<JsonElement> all = [];
        int page = 1;

        while (true)
        {
            JsonElement body = await ReadAsync(client, $"/api/v1/analyses/{jobId}/findings?page={page}&pageSize=100");
            int count = 0;

            foreach (JsonElement item in body.GetProperty("items").EnumerateArray())
            {
                all.Add(item.Clone());
                count++;
            }

            if (count == 0 || all.Count >= body.GetProperty("totalCount").GetInt32())
            {
                return all;
            }

            page++;
        }
    }

    /// <summary>Butun isler bitene kadar bekler.</summary>
    private static async Task WaitForIdleAsync(HttpClient client)
    {
        while (true)
        {
            JsonElement analysis = (await ReadAsync(client, "/api/v1/health")).GetProperty("analysis");

            if (analysis.GetProperty("queuedJobs").GetInt32() == 0
                && analysis.GetProperty("runningJobs").GetInt32() == 0)
            {
                return;
            }
        }
    }

    private static async Task<JobHandle> StartAsync(HttpClient client, int repositoryId, string kind)
    {
        using HttpResponseMessage response = await client.PostAsJsonAsync(
            $"/api/v1/repositories/{repositoryId}/analyses",
            new { kind });

        string raw = await response.Content.ReadAsStringAsync();
        JsonElement body = JsonDocument.Parse(raw).RootElement;

        // Kimlik yoksa istek reddedilmistir. Guid.Empty ile devam etmek, sonraki
        // olcumu sessizce anlamsiz kilardi.
        return body.TryGetProperty("id", out JsonElement id)
            ? new JobHandle(id.GetGuid(), response.StatusCode)
            : throw new InvalidOperationException($"Is acilamadi ({(int)response.StatusCode}): {raw}");
    }

    private static async Task<JsonElement> WaitAsync(HttpClient client, Guid jobId)
    {
        while (true)
        {
            JsonElement job = await ReadAsync(client, $"/api/v1/analyses/{jobId}");

            if (job.GetProperty("status").GetString() is "succeeded" or "failed" or "canceled")
            {
                return job;
            }
        }
    }

    private static async Task WaitForStatusAsync(HttpClient client, Guid jobId, string status)
    {
        while (true)
        {
            JsonElement job = await ReadAsync(client, $"/api/v1/analyses/{jobId}");

            if (string.Equals(job.GetProperty("status").GetString(), status, StringComparison.Ordinal))
            {
                return;
            }
        }
    }

    private static async Task<JsonElement> ReadAsync(HttpClient client, string path)
    {
        using HttpResponseMessage response = await client.GetAsync(path);
        string body = await response.Content.ReadAsStringAsync();

        return response.IsSuccessStatusCode
            ? JsonDocument.Parse(body).RootElement.Clone()
            : throw new InvalidOperationException($"{path} -> {(int)response.StatusCode}: {body}");
    }

    private static string Number(double value) => value.ToString("R", CultureInfo.InvariantCulture);

    private static string Render(
        string codeCommit,
        List<StaticResult> statics,
        List<RiskResult> risks,
        CancellationResult cancellation,
        IdempotencyResult idempotency,
        RecoveryResult recovery,
        long peak)
    {
        StringBuilder text = new();
        text.Append("{\n");
        text.Append("  \"codeCommit\": \"").Append(codeCommit).Append("\",\n");
        text.Append("  \"seed\": ").Append(Seed).Append(",\n");
        text.Append("  \"peakSampledWorkingSetBytes\": ").Append(peak).Append(",\n");
        text.Append("  \"staticScan\": [\n");

        for (int index = 0; index < statics.Count; index++)
        {
            StaticResult item = statics[index];

            text.Append("    { \"repository\": \"").Append(item.Repository).Append("\", ");
            text.Append("\"ran\": ").Append(item.Ran ? "true" : "false").Append(", ");
            text.Append("\"scannedFiles\": ").Append(item.ScannedFiles).Append(", ");
            text.Append("\"findingCount\": ").Append(item.FindingCount).Append(", ");
            text.Append("\"suppressedCount\": ").Append(item.SuppressedCount).Append(", ");
            text.Append("\"exemptionCount\": ").Append(item.ExemptionCount).Append(", ");
            text.Append("\"excludedFileCount\": ").Append(item.ExcludedFileCount).Append(", ");
            text.Append("\"skippedDirectoryCount\": ").Append(item.SkippedDirectoryCount).Append(", ");
            text.Append("\"queueWaitMs\": ").Append(Number(item.QueueWaitMs)).Append(", ");
            text.Append("\"runMs\": ").Append(Number(item.RunMs)).Append(", ");
            text.Append("\"totalMs\": ").Append(Number(item.TotalMs)).Append(", ");
            text.Append("\"directScanMs\": ").Append(Number(item.DirectMs)).Append(", ");
            text.Append("\"progressWrites\": ").Append(item.ProgressWrites).Append(", ");
            text.Append("\"directFindingCount\": ").Append(item.DirectFindingCount).Append(", ");
            text.Append("\"differenceCount\": ").Append(item.DifferenceCount).Append(" }");
            text.Append(index == statics.Count - 1 ? "\n" : ",\n");
        }

        text.Append("  ],\n");
        text.Append("  \"riskScoreAll\": [\n");

        for (int index = 0; index < risks.Count; index++)
        {
            RiskResult item = risks[index];

            text.Append("    { \"repository\": \"").Append(item.Repository).Append("\", ");
            text.Append("\"ran\": ").Append(item.Ran ? "true" : "false").Append(", ");
            text.Append("\"expectedCommits\": ").Append(item.ExpectedCommits).Append(", ");
            text.Append("\"resultCount\": ").Append(item.ResultCount).Append(", ");
            text.Append("\"queueWaitMs\": ").Append(Number(item.QueueWaitMs)).Append(", ");
            text.Append("\"runMs\": ").Append(Number(item.RunMs)).Append(", ");
            text.Append("\"totalMs\": ").Append(Number(item.TotalMs)).Append(", ");
            text.Append("\"commitsPerSecond\": ").Append(Number(item.CommitsPerSecond)).Append(", ");
            text.Append("\"progressWrites\": ").Append(item.ProgressWrites).Append(", ");
            text.Append("\"modelLoads\": ").Append(item.ModelLoads).Append(", ");
            text.Append("\"explanationMismatches\": ").Append(item.ExplanationMismatches).Append(", ");
            text.Append("\"comparedCommits\": ").Append(item.Compared).Append(", ");
            text.Append("\"mismatchedCommits\": ").Append(item.Mismatched).Append(", ");
            text.Append("\"worstScoreDifference\": ").Append(Number(item.WorstScoreDifference)).Append(" }");
            text.Append(index == risks.Count - 1 ? "\n" : ",\n");
        }

        text.Append("  ],\n");
        text.Append("  \"cancellation\": {\n");
        text.Append("    \"queuedStatusCode\": ").Append(cancellation.QueuedStatusCode).Append(",\n");
        text.Append("    \"queuedStatus\": \"").Append(cancellation.QueuedStatus).Append("\",\n");
        text.Append("    \"queuedCancelMs\": ").Append(Number(cancellation.QueuedCancelMs)).Append(",\n");
        text.Append("    \"queuedNeverStarted\": ").Append(cancellation.QueuedNeverStarted ? "true" : "false").Append(",\n");
        text.Append("    \"runningStatusCode\": ").Append(cancellation.RunningStatusCode).Append(",\n");
        text.Append("    \"runningStatus\": \"").Append(cancellation.RunningStatus).Append("\",\n");
        text.Append("    \"runningCancelMs\": ").Append(Number(cancellation.RunningCancelMs)).Append(",\n");
        text.Append("    \"processedBeforeCancel\": ").Append(cancellation.ProcessedBefore).Append(",\n");
        text.Append("    \"processedAtTerminal\": ").Append(cancellation.ProcessedAfter).Append(",\n");
        text.Append("    \"processedAfterCancel\": ").Append(cancellation.ProcessedAfterCancel).Append(",\n");
        text.Append("    \"isResultComplete\": ").Append(cancellation.IsResultComplete ? "true" : "false").Append(",\n");
        text.Append("    \"terminalCancelStatus\": \"").Append(cancellation.TerminalStatus).Append("\",\n");
        text.Append("    \"terminalCancelIdempotent\": ").Append(cancellation.TerminalIdempotent ? "true" : "false").Append('\n');
        text.Append("  },\n");
        text.Append("  \"idempotency\": {\n");
        text.Append("    \"sequentialDistinctJobs\": ").Append(idempotency.SequentialDistinctJobs).Append(",\n");
        text.Append("    \"concurrentSameKeyDistinctJobs\": ").Append(idempotency.ConcurrentSameKeyDistinctJobs).Append(",\n");
        text.Append("    \"raceAccepted\": ").Append(idempotency.RaceAccepted).Append(",\n");
        text.Append("    \"raceConflicted\": ").Append(idempotency.RaceConflicted).Append(",\n");
        text.Append("    \"newJobRows\": ").Append(idempotency.NewJobRows).Append('\n');
        text.Append("  },\n");
        text.Append("  \"restartRecovery\": {\n");
        text.Append("    \"interruptedStatus\": \"").Append(recovery.InterruptedStatus).Append("\",\n");
        text.Append("    \"interruptedErrorCode\": \"").Append(recovery.InterruptedErrorCode).Append("\",\n");
        text.Append("    \"interruptedResultComplete\": ").Append(recovery.InterruptedResultComplete ? "true" : "false").Append(",\n");
        text.Append("    \"recoveredInterrupted\": ").Append(recovery.RecoveredInterrupted).Append(",\n");
        text.Append("    \"recoveredRequeued\": ").Append(recovery.RecoveredRequeued).Append(",\n");
        text.Append("    \"secondRecoveryChanges\": ").Append(recovery.SecondRecoveryChanges).Append(",\n");
        text.Append("    \"queuedJobStatus\": \"").Append(recovery.QueuedStatus).Append("\"\n");
        text.Append("  }\n");
        text.Append("}\n");

        return text.ToString();
    }

    private sealed record Target(int Id, string Identity, string Name, string? LocalPath);

    private sealed record JobHandle(Guid Id, HttpStatusCode Status);

    private sealed record StaticResult(
        string Repository,
        bool Ran,
        int ScannedFiles,
        int FindingCount,
        int SuppressedCount,
        int ExemptionCount,
        int ExcludedFileCount,
        int SkippedDirectoryCount,
        double QueueWaitMs,
        double RunMs,
        double TotalMs,
        double DirectMs,
        int ProgressWrites,
        int DirectFindingCount,
        int DifferenceCount,
        IReadOnlyList<string> Differences)
    {
        public static StaticResult Skipped(string repository) =>
            new(repository, false, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, []);
    }

    private sealed record RiskResult(
        string Repository,
        bool Ran,
        int ExpectedCommits,
        int ResultCount,
        double QueueWaitMs,
        double RunMs,
        double TotalMs,
        double CommitsPerSecond,
        int ProgressWrites,
        int ModelLoads,
        int ExplanationMismatches,
        int Compared,
        int Mismatched,
        double WorstScoreDifference)
    {
        public static RiskResult Skipped(string repository) =>
            new(repository, false, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
    }

    private sealed record CancellationResult(
        int QueuedStatusCode,
        string QueuedStatus,
        double QueuedCancelMs,
        bool QueuedNeverStarted,
        int RunningStatusCode,
        string RunningStatus,
        double RunningCancelMs,
        int ProcessedBefore,
        int ProcessedAfter,
        int ProcessedAfterCancel,
        bool IsResultComplete,
        string TerminalStatus,
        bool TerminalIdempotent);

    private sealed record IdempotencyResult(
        int SequentialDistinctJobs,
        int ConcurrentSameKeyDistinctJobs,
        int RaceAccepted,
        int RaceConflicted,
        int NewJobRows);

    private sealed record RecoveryResult(
        string InterruptedStatus,
        string? InterruptedErrorCode,
        bool InterruptedResultComplete,
        int RecoveredInterrupted,
        int RecoveredRequeued,
        int SecondRecoveryChanges,
        string QueuedStatus);
}
