using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

using Microsoft.EntityFrameworkCore;

using Sievert.Data;
using Sievert.Data.Entities;

namespace Sievert.Measure;

/// <summary>
/// Asama 6 Adim 6: rapor uretiminin suresi ve rapor uretilirken API'nin durumu.
///
/// Kosu sayilari kosmadan once sabitlendi ve <c>docs/olcumler/asama6-rapor-ve-demo.md</c>
/// icinde yaziyor: Polly 5, ShareX 3, Jellyfin 3.
/// </summary>
public static class ReportPerformanceCommand
{
    private const string ApiUrl = "http://127.0.0.1:5192";

    /// <summary>
    /// Bu kosuya ozel damga.
    ///
    /// Tekrar anahtarlari buna baglaniyor: sabit bir anahtar, ikinci kosuda onceki
    /// kosunun raporunu geri getiriyordu ve dosyasi silinmis oldugu icin olcum
    /// patliyordu.
    /// </summary>
    private static readonly string Stamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss",
        System.Globalization.CultureInfo.InvariantCulture);

    // sievert:disable SV006 komut satirindan tek sefer kosuyor; iptali Ctrl+C yapiyor
    public static async Task<int> RunAsync(SievertContext context, string[] args)
    {
        string repositoryRoot = Path.GetFullPath(args[1]);
        string codeCommit = args[2];
        string outputPath = args[3];

        Console.WriteLine("Release yayimi hazirlaniyor...");

        string apiDll = HostProcess.Publish(
            repositoryRoot, "Sievert.Api", Path.Combine(Path.GetTempPath(), "sievert-rapor-olcum"));

        List<Target> targets = Targets(context);
        List<Measurement> measurements = [];
        object? idempotency = null;
        object? corruption = null;
        object? cancellation = null;

        Dictionary<string, string> queryLog = new(StringComparer.Ordinal)
        {
            ["Logging__LogLevel__Microsoft.EntityFrameworkCore.Database.Command"] = "Information",
        };

        using (HostProcess api = HostProcess.Start(apiDll, ApiUrl, repositoryRoot, null, queryLog))
        {
            using HttpClient client = new() { BaseAddress = new Uri(ApiUrl), Timeout = TimeSpan.FromMinutes(5) };

            await WaitAsync(client);

            foreach (Target target in targets)
            {
                Console.WriteLine();
                Console.WriteLine(target.Name);

                for (int run = 0; run < target.Runs; run++)
                {
                    measurements.Add(await MeasureAsync(client, api, target, run));
                }
            }

            idempotency = await IdempotencyAsync(client, targets[0]);
            corruption = await CorruptionAsync(client, context, targets[0], repositoryRoot);
            cancellation = await CancellationAsync(client, context, targets[0]);
        }

        string json = JsonSerializer.Serialize(
            new
            {
                kod = codeCommit,
                uretildi = DateTimeOffset.UtcNow,
                kosullar = new
                {
                    derleme = "Release yayim ciktisi",
                    not = "Sure, POST istegi ile artefaktin hazir olmasi arasi. "
                        + "Saglik ve risk ucu sureleri rapor uretilirken olculdu.",
                },
                raporlar = measurements,
                idempotent = idempotency,
                bozulma = corruption,
                iptal = cancellation,
            },
            new JsonSerializerOptions { WriteIndented = true });

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outputPath))!);
        File.WriteAllText(outputPath, json, new UTF8Encoding(false));

        Console.WriteLine();
        Console.WriteLine($"{outputPath} yazildi.");

        return 0;
    }

    private static async Task<Measurement> MeasureAsync(
        HttpClient client, HostProcess api, Target target, int run)
    {
        int commandsBefore = api.DatabaseCommandCount;

        List<double> healthDuringGeneration = [];
        List<double> riskDuringGeneration = [];

        Stopwatch clock = Stopwatch.StartNew();

        using HttpRequestMessage request = new(
            HttpMethod.Post, $"/api/v1/repositories/{target.Id}/reports")
        {
            Content = JsonContent.Create(new
            {
                riskAnalysisJobId = target.RiskJobId,
                staticAnalysisJobId = target.StaticJobId,
                culture = "tr-TR",
            }),
        };

        request.Headers.TryAddWithoutValidation("Idempotency-Key", $"olcum-{Stamp}-{target.Id}-{run}");

        using HttpResponseMessage accepted = await client.SendAsync(request);

        accepted.EnsureSuccessStatusCode();

        double acceptedMs = clock.Elapsed.TotalMilliseconds;

        Guid reportId = JsonDocument
            .Parse(await accepted.Content.ReadAsStringAsync())
            .RootElement.GetProperty("reportId").GetGuid();

        JsonElement report;

        while (true)
        {
            // Rapor uretilirken normal istekler olculuyor: rapor uretimi API'yi
            // bekletiyor mu sorusunun cevabi bu iki sayi.
            healthDuringGeneration.Add(await TimedAsync(client, "/api/v1/health"));

            riskDuringGeneration.Add(await TimedAsync(
                client, $"/api/v1/repositories/{target.Id}/commits?pageSize=25"));

            report = JsonDocument
                .Parse(await client.GetStringAsync($"/api/v1/reports/{reportId}")).RootElement;

            if (report.GetProperty("status").GetString() is not "pending")
            {
                break;
            }
        }

        clock.Stop();

        await Task.Delay(200);

        int commands = api.DatabaseCommandCount - commandsBefore;

        Stopwatch download = Stopwatch.StartNew();
        byte[] pdf = await client.GetByteArrayAsync($"/api/v1/reports/{reportId}/download");
        download.Stop();

        Measurement measurement = new(
            target.Name,
            run + 1,
            Math.Round(acceptedMs, 1),
            Math.Round(clock.Elapsed.TotalMilliseconds, 1),
            Math.Round(download.Elapsed.TotalMilliseconds, 1),
            report.GetProperty("pageCount").GetInt32(),
            pdf.LongLength,
            commands,
            Median(healthDuringGeneration),
            healthDuringGeneration.Count == 0 ? 0 : Math.Round(healthDuringGeneration.Max(), 1),
            Median(riskDuringGeneration),
            healthDuringGeneration.Count);

        Console.WriteLine(
            $"  kosu {measurement.Run}: kabul {measurement.AcceptedMs,6:F1} ms, "
            + $"toplam {measurement.TotalMs,7:F1} ms, {measurement.PageCount} sayfa, "
            + $"{measurement.ByteLength / 1024.0:F1} KB, {commands} komut, "
            + $"saglik ortancasi {measurement.HealthMedianMs:F1} ms");

        return measurement;
    }

    /// <summary>Ayni tekrar anahtari: yeni is acilmiyor mu, dosya ayni mi.</summary>
    private static async Task<object> IdempotencyAsync(HttpClient client, Target target)
    {
        object body = new
        {
            riskAnalysisJobId = target.RiskJobId,
            staticAnalysisJobId = target.StaticJobId,
            culture = "tr-TR",
        };

        (HttpStatusCode firstStatus, Guid firstId, double firstMs) = await PostAsync(
            client, target, body, $"olcum-{Stamp}-idempotent");

        await WaitForReadyAsync(client, firstId);

        byte[] first = await client.GetByteArrayAsync($"/api/v1/reports/{firstId}/download");

        (HttpStatusCode secondStatus, Guid secondId, double secondMs) = await PostAsync(
            client, target, body, $"olcum-{Stamp}-idempotent");

        byte[] second = await client.GetByteArrayAsync($"/api/v1/reports/{secondId}/download");

        Console.WriteLine();
        Console.WriteLine($"Idempotent istek: {(int)firstStatus} -> {(int)secondStatus}, "
            + $"ayni rapor {(firstId == secondId ? "evet" : "hayir")}");

        return new
        {
            ilkDurum = (int)firstStatus,
            ikinciDurum = (int)secondStatus,
            ayniRapor = firstId == secondId,
            ayniBaytlar = first.SequenceEqual(second),
            ilkSureMs = Math.Round(firstMs, 1),
            ikinciSureMs = Math.Round(secondMs, 1),
        };
    }

    /// <summary>Bir bayti degistirilmis artefakt indirilebiliyor mu.</summary>
    private static async Task<object> CorruptionAsync(
        HttpClient client, SievertContext context, Target target, string repositoryRoot)
    {
        (_, Guid reportId, _) = await PostAsync(
            client,
            target,
            new { riskAnalysisJobId = target.RiskJobId, culture = "tr-TR" },
            $"olcum-{Stamp}-bozulma");

        await WaitForReadyAsync(client, reportId);

        // Artefakt dosyasi diskte bulunup tek bayti degistiriliyor. Dosya yolu yalniz
        // olcum icinde kullaniliyor; cevaplarda hicbir yerde gorunmuyor.
        string path = Path.Combine(
            repositoryRoot, "data", "runtime", "reports", $"report-{reportId:n}.pdf");

        byte[] content = await File.ReadAllBytesAsync(path);

        content[content.Length / 2] ^= 0xFF;

        await File.WriteAllBytesAsync(path, content);

        using HttpResponseMessage response = await client.GetAsync($"/api/v1/reports/{reportId}/download");

        string body = await response.Content.ReadAsStringAsync();

        JsonElement status = JsonDocument
            .Parse(await client.GetStringAsync($"/api/v1/reports/{reportId}")).RootElement;

        // Olcum kendi cop yigininini birakmiyor: bozulan dosya siliniyor.
        File.Delete(path);

        Console.WriteLine($"Bozuk artefakt: indirme {(int)response.StatusCode}, "
            + $"durum {status.GetProperty("status").GetString()}");

        return new
        {
            indirmeDurumu = (int)response.StatusCode,
            hataKodu = JsonDocument.Parse(body).RootElement.TryGetProperty("errorCode", out JsonElement code)
                ? code.GetString()
                : null,
            kayitDurumu = status.GetProperty("status").GetString(),
        };
    }

    /// <summary>Kuyruktaki bir rapor isi iptal edilebiliyor mu.</summary>
    private static async Task<object> CancellationAsync(
        HttpClient client, SievertContext context, Target target)
    {
        (_, Guid reportId, _) = await PostAsync(
            client,
            target,
            new { riskAnalysisJobId = target.RiskJobId, culture = "en-US" },
            $"olcum-{Stamp}-iptal");

        JsonElement report = JsonDocument
            .Parse(await client.GetStringAsync($"/api/v1/reports/{reportId}")).RootElement;

        Guid jobId = report.GetProperty("analysisJobId").GetGuid();

        Stopwatch clock = Stopwatch.StartNew();

        using HttpResponseMessage cancel = await client.PostAsync($"/api/v1/analyses/{jobId}/cancel", null);

        string terminal = "bilinmiyor";

        for (int attempt = 0; attempt < 600; attempt++)
        {
            JsonElement job = JsonDocument
                .Parse(await client.GetStringAsync($"/api/v1/analyses/{jobId}")).RootElement;

            terminal = job.GetProperty("status").GetString()!;

            if (terminal is "succeeded" or "failed" or "canceled")
            {
                break;
            }

            await Task.Delay(100);
        }

        clock.Stop();

        JsonElement final = JsonDocument
            .Parse(await client.GetStringAsync($"/api/v1/reports/{reportId}")).RootElement;

        Console.WriteLine($"Iptal: istek {(int)cancel.StatusCode}, is {terminal}, "
            + $"rapor {final.GetProperty("status").GetString()}");

        return new
        {
            iptalIstegi = (int)cancel.StatusCode,
            isDurumu = terminal,
            raporDurumu = final.GetProperty("status").GetString(),
            terminalSureMs = Math.Round(clock.Elapsed.TotalMilliseconds, 1),
        };
    }

    private static async Task<(HttpStatusCode Status, Guid ReportId, double Milliseconds)> PostAsync(
        HttpClient client, Target target, object body, string key)
    {
        using HttpRequestMessage request = new(
            HttpMethod.Post, $"/api/v1/repositories/{target.Id}/reports")
        {
            Content = JsonContent.Create(body),
        };

        request.Headers.TryAddWithoutValidation("Idempotency-Key", key);

        Stopwatch clock = Stopwatch.StartNew();

        using HttpResponseMessage response = await client.SendAsync(request);

        clock.Stop();

        JsonElement accepted = JsonDocument
            .Parse(await response.Content.ReadAsStringAsync()).RootElement;

        return (response.StatusCode, accepted.GetProperty("reportId").GetGuid(), clock.Elapsed.TotalMilliseconds);
    }

    private static async Task WaitForReadyAsync(HttpClient client, Guid reportId)
    {
        for (int attempt = 0; attempt < 1200; attempt++)
        {
            JsonElement report = JsonDocument
                .Parse(await client.GetStringAsync($"/api/v1/reports/{reportId}")).RootElement;

            if (report.GetProperty("status").GetString() is not "pending")
            {
                return;
            }

            await Task.Delay(100);
        }

        throw new TimeoutException($"Rapor hazir olmadi: {reportId}");
    }

    private static async Task<double> TimedAsync(HttpClient client, string path)
    {
        Stopwatch clock = Stopwatch.StartNew();

        using HttpResponseMessage response = await client.GetAsync(path);

        clock.Stop();

        return clock.Elapsed.TotalMilliseconds;
    }

    private static double Median(List<double> values)
    {
        if (values.Count == 0)
        {
            return 0;
        }

        List<double> sorted = [.. values.Order()];

        return Math.Round(sorted[sorted.Count / 2], 1);
    }

    private static List<Target> Targets(SievertContext context)
    {
        Dictionary<string, int> runs = new(StringComparer.OrdinalIgnoreCase)
        {
            ["polly-full"] = 5,
            ["sharex-full"] = 3,
            ["jellyfin-full"] = 3,
        };

        List<Target> targets = [];

        var risks = context.AnalysisJobs
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

        foreach (var risk in risks)
        {
            if (targets.Exists(target => target.Id == risk.RepositoryId))
            {
                continue;
            }

            // sievert:disable SV004 uc depo icin uc sorgu; olcum hazirligi, sicak yol degil
            Guid? scan = context.AnalysisJobs
                .AsNoTracking()
                .Where(job => job.RepositoryId == risk.RepositoryId
                    && job.Kind == AnalysisJobKind.StaticScan
                    && job.Status == AnalysisJobStatus.Succeeded
                    && job.IsResultComplete)
                .OrderByDescending(job => job.CompletedAtUtc)
                .Select(job => (Guid?)job.Id)
                .FirstOrDefault();

            string name = names[risk.RepositoryId];

            targets.Add(new Target(
                risk.RepositoryId, name, risk.Id, scan, runs.GetValueOrDefault(name, 3)));
        }

        return [.. targets.OrderBy(target => target.Name, StringComparer.Ordinal)];
    }

    // sievert:disable SV006 kendi zaman asimi var (60 sn); disaridan iptal edilecek bir cagiran yok
    private static async Task WaitAsync(HttpClient client)
    {
        for (int attempt = 0; attempt < 600; attempt++)
        {
            try
            {
                using HttpResponseMessage response = await client.GetAsync("/api/v1/health");

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

        throw new TimeoutException("API 60 saniyede cevap vermedi.");
    }

    private sealed record Target(int Id, string Name, Guid RiskJobId, Guid? StaticJobId, int Runs);

    private sealed record Measurement(
        string Repository,
        int Run,
        double AcceptedMs,
        double TotalMs,
        double DownloadMs,
        int PageCount,
        long ByteLength,
        int DatabaseCommands,
        double HealthMedianMs,
        double HealthMaxMs,
        double CommitsMedianMs,
        int SamplesDuringGeneration);
}
