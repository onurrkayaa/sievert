using System.Diagnostics;
using System.Text;
using System.Text.Json;

using Microsoft.EntityFrameworkCore;

using Sievert.Contracts;
using Sievert.Data;
using Sievert.Data.Entities;

namespace Sievert.Measure;

/// <summary>
/// Asama 6 Adim 5: gorsellestirme uclarinin sureleri ve panel sayfalarinin HTML suresi.
///
/// Kosullar <see cref="PanelCommand"/> ile ayni: Release yayim ciktisi, API ve panel
/// ayri sureclerde, loopback, tek istemci. Boylece Adim 4 tablosuyla ayni tabloya
/// konabiliyor.
///
/// Olculen kombinasyonlar <b>sonuc gorulmeden</b> secildi ve
/// <c>docs/olcumler/asama6-gorsellestirme.md</c> icinde olcumden once yaziliydi.
/// Butun pencere x limit x siralama carpimi uc depoda bes kosuyla binlerce istek
/// ederdi; temsilci matris sabitlendi.
/// </summary>
public static class VisualizationPerformanceCommand
{
    private const string ApiUrl = "http://127.0.0.1:5198";

    private const string WebUrl = "http://127.0.0.1:5197";

    /// <summary>Her kombinasyon icin kosu sayisi. Tek kosu bir performans sonucu degildir.</summary>
    private const int Runs = 5;

    private static readonly int[] Windows = [50, 200, 1000];

    private static readonly int[] Limits = [50, 100, 200];

    private static readonly int[] Counts = [50, 100, 500];

    // sievert:disable SV006 komut satirindan tek sefer kosuyor; iptali Ctrl+C yapiyor
    public static async Task<int> RunAsync(SievertContext context, string[] args)
    {
        string repositoryRoot = Path.GetFullPath(args[1]);
        string codeCommit = args[2];
        string outputPath = args[3];

        List<Target> targets = Targets(context);

        if (targets.Count == 0)
        {
            Console.Error.WriteLine("Tamamlanmis risk isi olan depo yok.");

            return 1;
        }

        string publishRoot = Path.Combine(Path.GetTempPath(), "sievert-gorsellestirme-olcum");

        Console.WriteLine("Release yayimi hazirlaniyor...");

        string apiDll = HostProcess.Publish(repositoryRoot, "Sievert.Api", publishRoot);
        string webDll = HostProcess.Publish(repositoryRoot, "Sievert.Web", publishRoot);

        Dictionary<string, string> queryLog = new(StringComparer.Ordinal)
        {
            ["Logging__LogLevel__Microsoft.EntityFrameworkCore.Database.Command"] = "Information",
        };

        List<Measurement> endpoints = [];
        List<PageMeasurement> pages = [];

        using (HostProcess api = HostProcess.Start(apiDll, ApiUrl, repositoryRoot, null, queryLog))
        using (HostProcess web = HostProcess.Start(webDll, WebUrl, contentRoot: null, ApiUrl))
        {
            using HttpClient client = new() { Timeout = TimeSpan.FromMinutes(5) };

            await WaitAsync(client, $"{ApiUrl}/api/v1/health");
            await WaitAsync(client, $"{WebUrl}/");

            foreach (Target target in targets)
            {
                Console.WriteLine();
                Console.WriteLine($"{target.Name} (is {target.JobId})");

                // Isinma: ilk istek JIT'i ve sorgu planini isitiyor. Sonuclara girmiyor.
                await client.GetStringAsync(MapUrl(target, 50, 50));

                foreach (int window in Windows)
                {
                    endpoints.Add(await MeasureAsync(
                        client,
                        api,
                        "file-activity",
                        target.Name,
                        $"pencere {window}",
                        MapUrl(target, window, VisualizationLimits.DefaultFileLimit),
                        "items"));
                }

                foreach (int limit in Limits)
                {
                    endpoints.Add(await MeasureAsync(
                        client,
                        api,
                        "file-activity",
                        target.Name,
                        $"limit {limit}",
                        MapUrl(target, VisualizationLimits.DefaultCommitWindow, limit),
                        "items"));
                }

                foreach (int count in Counts)
                {
                    endpoints.Add(await MeasureAsync(
                        client,
                        api,
                        "risk-timeline",
                        target.Name,
                        $"nokta {count}",
                        $"{ApiUrl}/api/v1/repositories/{target.Id}/visualizations/risk-timeline"
                        + $"?analysisJobId={target.JobId}&count={count}",
                        "points"));
                }
            }

            // Panel tarafi: sayfanin HTML'i. Tarayici yok, olculen sey sunucunun
            // on-isleme suresi; ciziminin suresi tarayici olcumunde ayrica yaziyor.
            Target primary = targets[0];

            pages.Add(await MeasurePageAsync(client, web, "Dosya haritasi",
                $"{WebUrl}/repositories/{primary.Id}?tab=dosyalar&job={primary.JobId}"));
            pages.Add(await MeasurePageAsync(client, web, "Zaman cizelgesi",
                $"{WebUrl}/repositories/{primary.Id}?tab=zaman&job={primary.JobId}"));
        }

        string json = JsonSerializer.Serialize(
            new
            {
                kod = codeCommit,
                uretildi = DateTimeOffset.UtcNow,
                kosullar = new
                {
                    derleme = "Release yayim ciktisi",
                    surecler = "API ve panel ayri surecte, loopback",
                    kosuSayisi = Runs,
                    isinma = "her depo icin bir istek, sonuclara dahil degil",
                    not = "Veritabani komut sayisi API gunlugundeki 'Executed DbCommand' satirlarindan.",
                },
                depolar = targets.Select(row => new { row.Id, row.Name, Job = row.JobId }),
                uclar = endpoints,
                sayfalar = pages,
            },
            new JsonSerializerOptions { WriteIndented = true });

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outputPath))!);
        File.WriteAllText(outputPath, json, new UTF8Encoding(false));

        Console.WriteLine();
        Console.WriteLine($"{outputPath} yazildi.");

        return 0;
    }

    private static List<Target> Targets(SievertContext context)
    {
        var rows = context.AnalysisJobs
            .AsNoTracking()
            .Where(job => job.Kind == AnalysisJobKind.RiskScoreAll
                && job.Status == AnalysisJobStatus.Succeeded
                && job.IsResultComplete)
            .OrderByDescending(job => job.CompletedAtUtc)
            .Select(job => new { job.Id, job.RepositoryId })
            .ToList();

        Dictionary<int, string> names = context.Repositories
            .AsNoTracking()
            .ToDictionary(row => row.Id, row => row.Name);

        List<Target> targets = [];

        foreach (var row in rows)
        {
            if (targets.Exists(existing => existing.Id == row.RepositoryId))
            {
                continue;
            }

            targets.Add(new Target(row.RepositoryId, names[row.RepositoryId], row.Id));
        }

        return [.. targets.OrderBy(row => row.Name, StringComparer.Ordinal)];
    }

    private static string MapUrl(Target target, int window, int limit) =>
        $"{ApiUrl}/api/v1/repositories/{target.Id}/visualizations/file-activity"
        + $"?analysisJobId={target.JobId}&commitWindow={window}&limit={limit}"
        + $"&sort={FileActivitySort.MeanRiskDescending}";

    private static async Task<Measurement> MeasureAsync(
        HttpClient client,
        HostProcess api,
        string endpoint,
        string repository,
        string parameters,
        string url,
        string arrayField)
    {
        List<double> durations = [];
        long bytes = 0;
        int rows = 0;
        int commands = 0;

        for (int run = 0; run < Runs; run++)
        {
            int before = api.DatabaseCommandCount;

            Stopwatch clock = Stopwatch.StartNew();
            string body = await client.GetStringAsync(url);
            clock.Stop();

            durations.Add(Math.Round(clock.Elapsed.TotalMilliseconds, 1));
            bytes = Encoding.UTF8.GetByteCount(body);

            JsonElement root = JsonDocument.Parse(body).RootElement;
            rows = root.GetProperty(arrayField).GetArrayLength();

            // Gunluk satirlari asenkron geliyor; kisa bir bekleme sayimi tamamliyor.
            await Task.Delay(150);

            commands = api.DatabaseCommandCount - before;
        }

        List<double> sorted = [.. durations.Order()];

        Measurement measurement = new(
            endpoint,
            repository,
            parameters,
            rows,
            bytes,
            sorted[0],
            sorted[sorted.Count / 2],
            sorted[^1],
            durations,
            commands);

        Console.WriteLine(
            $"  {endpoint,-14} {parameters,-12} ortanca {measurement.MedianMs,7:F1} ms  "
            + $"max {measurement.MaxMs,7:F1} ms  {rows,4} satir  {bytes / 1024.0,7:F1} KB  "
            + $"{commands} komut");

        return measurement;
    }

    private static async Task<PageMeasurement> MeasurePageAsync(
        HttpClient client,
        HostProcess web,
        string name,
        string url)
    {
        List<double> durations = [];
        long bytes = 0;
        int requests = 0;

        for (int run = 0; run < Runs; run++)
        {
            int before = web.ApiRequestCount;

            Stopwatch clock = Stopwatch.StartNew();
            string body = await client.GetStringAsync(url);
            clock.Stop();

            durations.Add(Math.Round(clock.Elapsed.TotalMilliseconds, 1));
            bytes = Encoding.UTF8.GetByteCount(body);

            await Task.Delay(150);

            requests = web.ApiRequestCount - before;
        }

        List<double> sorted = [.. durations.Order()];

        PageMeasurement measurement = new(
            name, durations[0], sorted[sorted.Count / 2], sorted[^1], bytes, requests, durations);

        Console.WriteLine(
            $"  {name,-20} ilk {measurement.FirstMs,7:F1} ms  ortanca {measurement.MedianMs,7:F1} ms  "
            + $"{bytes / 1024.0,7:F1} KB HTML  {requests} API istegi");

        return measurement;
    }

    // sievert:disable SV006 kendi zaman asimi var (60 sn); disaridan iptal edilecek bir cagiran yok
    private static async Task WaitAsync(HttpClient client, string url)
    {
        for (int attempt = 0; attempt < 600; attempt++)
        {
            try
            {
                using HttpResponseMessage response = await client.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    return;
                }
            }
            catch (HttpRequestException)
            {
                // Sunucu henuz dinlemiyor.
            }

            await Task.Delay(100);
        }

        throw new TimeoutException($"{url} 60 saniyede cevap vermedi.");
    }

    private sealed record Target(int Id, string Name, Guid JobId);

    private sealed record Measurement(
        string Endpoint,
        string Repository,
        string Parameters,
        int Rows,
        long Bytes,
        double MinMs,
        double MedianMs,
        double MaxMs,
        IReadOnlyList<double> RunsMs,
        int DatabaseCommands);

    private sealed record PageMeasurement(
        string Name,
        double FirstMs,
        double MedianMs,
        double MaxMs,
        long HtmlBytes,
        int ApiRequests,
        IReadOnlyList<double> RunsMs);
}
