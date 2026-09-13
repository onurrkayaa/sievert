using System.Diagnostics;
using System.Globalization;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

using Microsoft.EntityFrameworkCore;

using Sievert.Data;

namespace Sievert.Measure;

/// <summary>
/// Asama 6 Adim 3b: bellegin nereden geldigini ayristirma ve es zamanlilik olcumu.
///
/// Adim 3'te butun isler tek bir API surecinde kosmustu ve olculen en yuksek deger
/// 1267 MB cikmisti; o sayinin ne kadarinin hangi isten geldigi bilinmiyordu. Burada her
/// depo ve her is turu **taze bir surecte** kosuyor, yani her sayi tek bir isin sayisi.
///
/// Eski olcum silinmiyor; bu dosya onun yerine gecmiyor, yanina yaziliyor.
/// </summary>
public static class MemoryCommand
{
    private const string BaseUrl = "http://127.0.0.1:5199";

    /// <summary>Is kosarken iki ornek arasindaki sure.</summary>
    private const int SampleIntervalMs = 50;

    /// <summary>Saglik ucu daha pahali (birkac sayim sorgusu), o yuzden daha seyrek.</summary>
    private const int HealthIntervalMs = 500;

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
                .Select(row => new Target(row.Id, row.Name))
                .ToList()
        ];

        List<ModelLoadResult> loads = [];

        foreach (Target target in targets)
        {
            loads.Add(await MeasureModelLoadAsync(repositoryRoot, context, target));
        }

        List<RunResult> runs = [];

        foreach (string kind in (string[])["static-scan", "risk-score-all"])
        {
            foreach (Target target in targets)
            {
                runs.Add(await MeasureRunAsync(repositoryRoot, target, kind));
            }
        }

        // Es zamanlilik: ayni is cifti, once 1 sonra 2 worker ile.
        List<Target> pair = [.. targets.Where(target => !target.Name.Contains("jellyfin", StringComparison.Ordinal))];

        ConcurrencyResult one = await MeasureConcurrencyAsync(repositoryRoot, context, pair, 1);
        ConcurrencyResult two = await MeasureConcurrencyAsync(repositoryRoot, context, pair, 2);

        string json = Render(codeCommit, loads, runs, one, two);
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        File.WriteAllText(outputPath, json, new UTF8Encoding(false));

        Console.WriteLine();
        Console.WriteLine($"{outputPath} yazildi.");

        return 0;
    }

    private static HttpClient Client() =>
        new() { BaseAddress = new Uri(BaseUrl), Timeout = TimeSpan.FromMinutes(30) };

    /// <summary>
    /// Model yuklemesinin tek basina maliyeti.
    ///
    /// Ayri bir surec, ayri bir olcum: is kosarken faz gecisi milisaniyeler suruyor ve
    /// saglik ornekleri arasina sigmiyor. Burada yapilan tek sey bir tek commit'i
    /// skorlamak, yani modeli diskten okutmak.
    /// </summary>
    private static async Task<ModelLoadResult> MeasureModelLoadAsync(
        string repositoryRoot,
        SievertContext context,
        Target target)
    {
        string? sha = await context.Commits
            .AsNoTracking()
            .Where(commit => commit.RepositoryId == target.Id)
            .OrderBy(commit => commit.Id)
            .Select(commit => commit.Sha)
            .FirstOrDefaultAsync();

        if (sha is null)
        {
            return new ModelLoadResult(target.Name, 0, 0, 0, 0, 0);
        }

        using ApiProcess api = ApiProcess.Start(repositoryRoot, logQueries: false);
        using HttpClient client = Client();

        await api.WaitUntilHealthyAsync(client);

        Health cold = await ReadHealthAsync(client);

        Stopwatch clock = Stopwatch.StartNew();
        using HttpResponseMessage response = await client.GetAsync(
            $"/api/v1/repositories/{target.Id}/commits/{sha}/risk");
        clock.Stop();

        Health warm = await ReadHealthAsync(client);

        Console.WriteLine(
            $"== model yuklemesi: {target.Name} == {Mb(warm.WorkingSetBytes - cold.WorkingSetBytes)} MB, "
            + $"{clock.ElapsedMilliseconds} ms, {(int)response.StatusCode}");

        api.Stop();

        return new ModelLoadResult(
            target.Name,
            cold.WorkingSetBytes,
            warm.WorkingSetBytes,
            cold.ManagedHeapBytes,
            warm.ManagedHeapBytes,
            Math.Round(clock.Elapsed.TotalMilliseconds, 1));
    }

    /// <summary>
    /// Tek bir is, taze bir surecte. Model onbellegi soguk: surec yeni acildi ve is
    /// baslamadan once hicbir skorlama yapilmadi.
    /// </summary>
    private static async Task<RunResult> MeasureRunAsync(string repositoryRoot, Target target, string kind)
    {
        Console.WriteLine($"== {kind}: {target.Name} (taze surec) ==");

        using ApiProcess api = ApiProcess.Start(repositoryRoot, logQueries: false);
        using HttpClient client = Client();

        await api.WaitUntilHealthyAsync(client);

        double readyAt = api.ElapsedMilliseconds;
        Health ready = await ReadHealthAsync(client);

        Guid jobId = await StartAsync(client, target.Id, kind);
        double startedAt = api.ElapsedMilliseconds;

        List<Tick> ticks = [];
        Health health = ready;
        double lastHealth = readyAt;
        JsonElement job;

        while (true)
        {
            job = await ReadAsync(client, $"/api/v1/analyses/{jobId}");

            double now = api.ElapsedMilliseconds;

            if (now - lastHealth >= HealthIntervalMs)
            {
                health = await ReadHealthAsync(client);
                lastHealth = now;
            }

            ticks.Add(new Tick(
                Math.Round(now, 1),
                job.GetProperty("currentPhase").GetString() ?? string.Empty,
                job.GetProperty("processedItems").GetInt32(),
                job.GetProperty("resultCount").GetInt32(),
                health.WorkingSetBytes,
                health.ManagedHeapBytes,
                health.Gen0,
                health.Gen1,
                health.Gen2));

            if (job.GetProperty("status").GetString() is "succeeded" or "failed" or "canceled")
            {
                break;
            }

            await Task.Delay(SampleIntervalMs);
        }

        double terminalAt = api.ElapsedMilliseconds;
        Health terminal = await ReadHealthAsync(client);

        // Is bittikten sonra bellegin geri verilip verilmedigi. Toplama ZORLANMIYOR.
        await Task.Delay(5000);

        Health after = await ReadHealthAsync(client);

        string status = job.GetProperty("status").GetString()!;
        int resultCount = job.GetProperty("resultCount").GetInt32();
        int processed = job.GetProperty("processedItems").GetInt32();

        JsonElement? summary = job.TryGetProperty("resultSummary", out JsonElement raw)
            && raw.ValueKind == JsonValueKind.Object
                ? raw
                : null;

        int? tracked = Number(summary, "maxChangeTrackerEntries");
        int? trackedAfterClear = Number(summary, "maxChangeTrackerEntriesAfterClear");

        List<RssSample> samples = [.. api.Samples];
        long baseline = samples.Where(sample => sample.ElapsedMs <= readyAt).Select(sample => sample.WorkingSetBytes)
            .DefaultIfEmpty(0).Max();

        long peak = samples.Select(sample => sample.WorkingSetBytes).DefaultIfEmpty(0).Max();
        long terminalRss = terminal.WorkingSetBytes;

        Console.WriteLine(
            $"  durum {status}, {resultCount} satir, {Math.Round(terminalAt - startedAt)} ms, "
            + $"tepe {Mb(peak)} MB, taban {Mb(baseline)} MB");

        api.Stop();

        return new RunResult(
            target.Name,
            kind,
            status,
            processed,
            resultCount,
            Math.Round(terminalAt - startedAt, 1),
            samples.FirstOrDefault()?.WorkingSetBytes ?? 0,
            baseline,
            ready.WorkingSetBytes,
            peak,
            terminalRss,
            after.WorkingSetBytes,
            terminal.ManagedHeapBytes,
            after.ManagedHeapBytes,
            terminal.Gen0 - ready.Gen0,
            terminal.Gen1 - ready.Gen1,
            terminal.Gen2 - ready.Gen2,
            tracked,
            trackedAfterClear,
            Trend(ticks));
    }

    private static int? Number(JsonElement? summary, string field) =>
        summary is JsonElement value && value.TryGetProperty(field, out JsonElement found)
            ? found.GetInt32()
            : null;

    /// <summary>Her bin ogede o ana kadar goruleni degil, o andaki calisma kumesini yazar.</summary>
    private static List<TrendPoint> Trend(IReadOnlyList<Tick> ticks)
    {
        List<TrendPoint> points = [];
        int next = 1000;

        foreach (Tick tick in ticks)
        {
            if (tick.Processed >= next)
            {
                points.Add(new TrendPoint(next, tick.WorkingSetBytes, tick.ManagedHeapBytes));
                next += 1000;
            }
        }

        return points;
    }

    /// <summary>
    /// Ayni is cifti iki farkli es zamanlilikta. Taze surec, bos kuyruk, ayni iki depo.
    /// Jellyfin disarida: olculmek istenen sey iki isin birbirini nasil etkiledigi,
    /// en buyuk repoyu eklemek sureyi uzatmaktan baska bir sey degistirmiyor.
    /// </summary>
    private static async Task<ConcurrencyResult> MeasureConcurrencyAsync(
        string repositoryRoot,
        SievertContext context,
        IReadOnlyList<Target> targets,
        int concurrency)
    {
        Console.WriteLine($"== es zamanlilik {concurrency}: {string.Join(" + ", targets.Select(t => t.Name))} ==");

        using ApiProcess api = ApiProcess.Start(
            repositoryRoot,
            logQueries: false,
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["Sievert__Analysis__WorkerConcurrency"] = concurrency.ToString(CultureInfo.InvariantCulture),
            });

        using HttpClient client = Client();

        await api.WaitUntilHealthyAsync(client);

        Health ready = await ReadHealthAsync(client);

        Stopwatch wall = Stopwatch.StartNew();
        List<Guid> jobs = [];

        foreach (Target target in targets)
        {
            jobs.Add(await StartAsync(client, target.Id, "risk-score-all"));
        }

        using CancellationTokenSource connectionSampling = new();
        Task<int> connections = SampleConnectionsAsync(context, connectionSampling.Token);

        List<JobTiming> timings = [];

        foreach (Guid jobId in jobs)
        {
            JsonElement job = await WaitAsync(client, jobId);

            timings.Add(new JobTiming(
                job.GetProperty("repositoryId").GetInt32(),
                job.GetProperty("status").GetString()!,
                job.GetProperty("resultCount").GetInt32(),
                Duration(job),
                job.GetProperty("errorCode").ValueKind == JsonValueKind.String
                    ? job.GetProperty("errorCode").GetString()
                    : null));
        }

        wall.Stop();
        await connectionSampling.CancelAsync();

        int peakConnections = await connections;
        Health terminal = await ReadHealthAsync(client);
        long peak = api.Samples.Select(sample => sample.WorkingSetBytes).DefaultIfEmpty(0).Max();

        Console.WriteLine(
            $"  toplam {wall.ElapsedMilliseconds} ms, tepe {Mb(peak)} MB, en cok {peakConnections} baglanti");

        api.Stop();

        return new ConcurrencyResult(
            concurrency,
            Math.Round(wall.Elapsed.TotalMilliseconds, 1),
            timings,
            ready.WorkingSetBytes,
            peak,
            terminal.WorkingSetBytes,
            peakConnections);
    }

    /// <summary>
    /// Veritabani baglanti sayisi. Olcum aracinin kendi baglantisi da bu sayinin icinde;
    /// dosyada oyle yaziyor.
    /// </summary>
    private static async Task<int> SampleConnectionsAsync(SievertContext context, CancellationToken cancellation)
    {
        int peak = 0;

        while (!cancellation.IsCancellationRequested)
        {
            try
            {
                int current = await context.Database
                    .SqlQuery<int>($"select count(*)::int as \"Value\" from pg_stat_activity where datname = current_database()")
                    .SingleAsync(cancellation);

                peak = Math.Max(peak, current);

                await Task.Delay(250, cancellation);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        return peak;
    }

    private static double Duration(JsonElement job)
    {
        if (job.GetProperty("startedAtUtc").ValueKind != JsonValueKind.String
            || job.GetProperty("completedAtUtc").ValueKind != JsonValueKind.String)
        {
            return 0;
        }

        return Math.Round(
            (job.GetProperty("completedAtUtc").GetDateTimeOffset()
                - job.GetProperty("startedAtUtc").GetDateTimeOffset()).TotalMilliseconds,
            1);
    }

    private static async Task<Guid> StartAsync(HttpClient client, int repositoryId, string kind)
    {
        using HttpResponseMessage response = await client.PostAsJsonAsync(
            $"/api/v1/repositories/{repositoryId}/analyses",
            new { kind });

        string raw = await response.Content.ReadAsStringAsync();
        JsonElement body = JsonDocument.Parse(raw).RootElement;

        return body.TryGetProperty("id", out JsonElement id)
            ? id.GetGuid()
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

            await Task.Delay(SampleIntervalMs);
        }
    }

    private static async Task<JsonElement> ReadAsync(HttpClient client, string path) =>
        JsonDocument.Parse(await client.GetStringAsync(path)).RootElement.Clone();

    private static async Task<Health> ReadHealthAsync(HttpClient client)
    {
        JsonElement process = (await ReadAsync(client, "/api/v1/health")).GetProperty("process");

        return new Health(
            process.GetProperty("workingSetBytes").GetInt64(),
            process.GetProperty("managedHeapBytes").GetInt64(),
            process.GetProperty("gen0Collections").GetInt32(),
            process.GetProperty("gen1Collections").GetInt32(),
            process.GetProperty("gen2Collections").GetInt32());
    }

    private static double Mb(long bytes) => Math.Round(bytes / 1024.0 / 1024.0, 1);

    private static string Render(
        string codeCommit,
        IReadOnlyList<ModelLoadResult> loads,
        IReadOnlyList<RunResult> runs,
        ConcurrencyResult one,
        ConcurrencyResult two) =>
        JsonSerializer.Serialize(
            new
            {
                kod = codeCommit,
                uretildi = DateTimeOffset.UtcNow,
                kosullar = new
                {
                    derleme = "Debug",
                    esZamanlilik = 1,
                    ornekAraligiMs = SampleIntervalMs,
                    saglikAraligiMs = HealthIntervalMs,
                    not = "Her kosu taze bir API sureci. Toplama zorlanmadi, GC ayari degistirilmedi.",
                },
                modelYuklemesi = loads,
                kosular = runs,
                esZamanlilik = new { bir = one, iki = two },
            },
            new JsonSerializerOptions { WriteIndented = true });

    private sealed record Target(int Id, string Name);

    private sealed record Health(
        long WorkingSetBytes,
        long ManagedHeapBytes,
        int Gen0,
        int Gen1,
        int Gen2);

    private sealed record Tick(
        double ElapsedMs,
        string Phase,
        int Processed,
        int ResultCount,
        long WorkingSetBytes,
        long ManagedHeapBytes,
        int Gen0,
        int Gen1,
        int Gen2);

    private sealed record ModelLoadResult(
        string Repository,
        long ColdWorkingSetBytes,
        long WarmWorkingSetBytes,
        long ColdManagedHeapBytes,
        long WarmManagedHeapBytes,
        double DurationMs);

    private sealed record TrendPoint(int Items, long WorkingSetBytes, long ManagedHeapBytes);

    private sealed record JobTiming(int RepositoryId, string Status, int ResultCount, double DurationMs, string? ErrorCode);

    private sealed record RunResult(
        string Repository,
        string Kind,
        string Status,
        int ProcessedItems,
        int ResultCount,
        double DurationMs,
        long FirstSampleBytes,
        long BaselineBytes,
        long DatabaseReadyBytes,
        long PeakSampledBytes,
        long TerminalBytes,
        long FiveSecondsLaterBytes,
        long TerminalManagedHeapBytes,
        long FiveSecondsLaterManagedHeapBytes,
        int Gen0Collections,
        int Gen1Collections,
        int Gen2Collections,
        int? MaxChangeTrackerEntries,
        int? MaxChangeTrackerEntriesAfterClear,
        IReadOnlyList<TrendPoint> Trend);

    private sealed record ConcurrencyResult(
        int WorkerConcurrency,
        double WallClockMs,
        IReadOnlyList<JobTiming> Jobs,
        long BaselineBytes,
        long PeakSampledBytes,
        long TerminalBytes,
        int PeakDatabaseConnections);
}
