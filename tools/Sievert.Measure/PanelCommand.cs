using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Sievert.Measure;

/// <summary>
/// Asama 6 Adim 4: panelin sayfa sureleri ve durum sorma dongusu.
///
/// Iki ayri komut var cunku iki ayri sey olculuyor. Sayfa sureleri HTTP duzeyinde
/// olculebiliyor: Blazor Web App on-isleme yaptigi icin ilk cevabin govdesi zaten
/// sayfanin icerigi. Durum sorma dongusu ise ancak gercek bir tarayici devresi acikken
/// kosuyor, o yuzden o olcum tarayiciyla yapilip panelin kendi gunlugunden sayiliyor.
///
/// Panel sayilari API'nin Debug olcumleriyle **ayni tabloya konmuyor**: kosullar farkli.
/// </summary>
public static partial class PanelCommand
{
    private const string ApiUrl = "http://127.0.0.1:5198";

    private const string WebUrl = "http://127.0.0.1:5197";

    /// <summary>Her sayfa icin kosu sayisi. Tek kosu bir performans sonucu degildir.</summary>
    private const int Runs = 5;

    // sievert:disable SV006 komut satirindan tek sefer kosuyor; iptali Ctrl+C yapiyor
    public static async Task<int> PagesAsync(string[] args)
    {
        string repositoryRoot = Path.GetFullPath(args[1]);
        string codeCommit = args[2];
        string outputPath = args[3];

        string publishRoot = Path.Combine(Path.GetTempPath(), "sievert-panel-olcum");

        Console.WriteLine("Release yayimi hazirlaniyor...");

        string apiDll = HostProcess.Publish(repositoryRoot, "Sievert.Api", publishRoot);
        string webDll = HostProcess.Publish(repositoryRoot, "Sievert.Web", publishRoot);

        using HostProcess api = HostProcess.Start(apiDll, ApiUrl, repositoryRoot, null);
        using HostProcess web = HostProcess.Start(webDll, WebUrl, contentRoot: null, ApiUrl);

        using HttpClient client = new() { Timeout = TimeSpan.FromMinutes(2) };

        await WaitAsync(client, $"{ApiUrl}/api/v1/health");
        await WaitAsync(client, $"{WebUrl}/");

        // Olculen depo ve commit, sayfa adreslerini kurmak icin API'den okunuyor.
        JsonElement repositories = JsonDocument
            .Parse(await client.GetStringAsync($"{ApiUrl}/api/v1/repositories?pageSize=100"))
            .RootElement;

        JsonElement first = repositories.GetProperty("items")[0];
        int repositoryId = first.GetProperty("id").GetInt32();

        JsonElement commits = JsonDocument
            .Parse(await client.GetStringAsync($"{ApiUrl}/api/v1/repositories/{repositoryId}/commits?pageSize=1"))
            .RootElement;

        string sha = commits.GetProperty("items")[0].GetProperty("sha").GetString()!;

        JsonElement jobs = JsonDocument
            .Parse(await client.GetStringAsync($"{ApiUrl}/api/v1/repositories/{repositoryId}/analyses?pageSize=1"))
            .RootElement;

        string? jobId = jobs.GetProperty("items").GetArrayLength() > 0
            ? jobs.GetProperty("items")[0].GetProperty("id").GetString()
            : null;

        List<(string Name, string Path)> pages =
        [
            ("Genel bakis", "/"),
            ("Repolar", "/repositories"),
            ("Depo ayrintisi", $"/repositories/{repositoryId}"),
            ("Analiz isleri", "/analyses"),
            ("Commit riski", $"/repositories/{repositoryId}/commits/{sha}/risk"),
            ("Modeller", "/models"),
            ("Sistem durumu", "/system"),
        ];

        if (jobId is not null)
        {
            pages.Insert(4, ("Is ayrintisi", $"/analyses/{jobId}"));
        }

        // Ilk kosu soguk: model onbellegi bos, JIT isinmamis. Ayri raporlaniyor.
        List<PageResult> results = [];

        foreach ((string name, string path) in pages)
        {
            results.Add(await MeasurePageAsync(client, web, name, path));
        }

        string json = JsonSerializer.Serialize(
            new
            {
                kod = codeCommit,
                uretildi = DateTimeOffset.UtcNow,
                kosullar = new
                {
                    derleme = "Release",
                    istemci = "tek HttpClient, tarayici yok",
                    onbellek = "her kosu yeni istek; HTTP onbellegi kullanilmiyor",
                    kosuSayisi = Runs,
                    not = "Blazor on-isleme acik; ilk cevabin govdesi sayfanin icerigi.",
                },
                sayfalar = results,
            },
            new JsonSerializerOptions { WriteIndented = true });

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        File.WriteAllText(outputPath, json, new UTF8Encoding(false));

        Console.WriteLine();
        Console.WriteLine($"{outputPath} yazildi.");

        return 0;
    }

    /// <summary>
    /// Durum sorma dongusunun sayilari.
    ///
    /// Girdi, panelin kendi gunlugu: <c>SievertApiClient</c> her istegin basini ve sonunu
    /// yaziyor. Zaman damgasi icin gunluk <c>Logging__Console__FormatterOptions__TimestampFormat</c>
    /// ile baslatilmali.
    /// </summary>
    public static int Polling(string[] args)
    {
        string logPath = args[1];
        string jobId = args[2];
        string outputPath = args[3];

        List<(TimeSpan At, bool Start)> events = [];
        int total = 0;

        foreach (string line in File.ReadLines(logPath))
        {
            Match stamp = Timestamp().Match(line);

            if (!stamp.Success)
            {
                continue;
            }

            TimeSpan at = TimeSpan.Parse(stamp.Groups[1].Value, CultureInfo.InvariantCulture);

            if (line.Contains("Start processing HTTP request GET", StringComparison.Ordinal)
                && line.Contains($"/api/v1/analyses/{jobId}", StringComparison.Ordinal)
                && !line.Contains("/risks", StringComparison.Ordinal)
                && !line.Contains("/findings", StringComparison.Ordinal))
            {
                events.Add((at, true));
                total++;
            }
            else if (line.Contains("End processing HTTP request after", StringComparison.Ordinal) && total > 0)
            {
                events.Add((at, false));
            }
        }

        events.Sort((left, right) => left.At.CompareTo(right.At));

        int open = 0;
        int peak = 0;

        foreach ((TimeSpan _, bool start) in events)
        {
            open += start ? 1 : -1;
            open = Math.Max(0, open);
            peak = Math.Max(peak, open);
        }

        List<double> gaps = [];
        TimeSpan? previous = null;

        foreach ((TimeSpan at, bool start) in events.Where(item => item.Start))
        {
            if (previous is TimeSpan last)
            {
                gaps.Add(Math.Round((at - last).TotalMilliseconds, 1));
            }

            previous = at;
        }

        var report = new
        {
            isKimligi = jobId,
            toplamIstek = total,
            enYuksekEsZamanliIstek = peak,
            araliklarMs = gaps,
            ortancaAralikMs = gaps.Count == 0 ? 0 : Median(gaps),
            enKisaAralikMs = gaps.Count == 0 ? 0 : gaps.Min(),
            enUzunAralikMs = gaps.Count == 0 ? 0 : gaps.Max(),
        };

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        File.WriteAllText(
            outputPath,
            JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }),
            new UTF8Encoding(false));

        Console.WriteLine($"{total} istek, en yuksek es zamanli {peak}, ortanca aralik "
            + $"{(gaps.Count == 0 ? 0 : Median(gaps))} ms");
        Console.WriteLine($"{outputPath} yazildi.");

        return 0;
    }

    private static async Task<PageResult> MeasurePageAsync(
        HttpClient client,
        HostProcess web,
        string name,
        string path)
    {
        List<double> durations = [];
        List<int> apiCalls = [];
        long bytes = 0;

        web.ResetSlowest();

        for (int run = 0; run < Runs; run++)
        {
            int before = web.ApiRequestCount;

            Stopwatch clock = Stopwatch.StartNew();
            string body = await client.GetStringAsync(WebUrl + path);
            clock.Stop();

            durations.Add(Math.Round(clock.Elapsed.TotalMilliseconds, 1));
            apiCalls.Add(web.ApiRequestCount - before);
            bytes = Encoding.UTF8.GetByteCount(body);
        }

        Console.WriteLine(
            $"{name,-16} ortanca {Median(durations),8:F1} ms  en yuksek {durations.Max(),8:F1} ms  "
            + $"API {apiCalls.Max()} istek  {bytes / 1024.0:F1} KB");

        return new PageResult(
            name,
            path,
            durations[0],
            Median(durations),
            durations.Max(),
            durations,
            apiCalls.Max(),
            bytes,
            Math.Round(web.SlowestApiMilliseconds, 1));
    }

    private static double Median(List<double> values)
    {
        List<double> sorted = [.. values.Order()];

        return sorted.Count % 2 == 1
            ? sorted[sorted.Count / 2]
            : Math.Round((sorted[(sorted.Count / 2) - 1] + sorted[sorted.Count / 2]) / 2, 1);
    }

    // sievert:disable SV006 acilis bekleme dongusu; kendi zaman asimi var
    private static async Task WaitAsync(HttpClient client, string url)
    {
        for (int attempt = 0; attempt < 300; attempt++)
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
                // Henuz dinlemiyor.
            }

            await Task.Delay(100);
        }

        throw new TimeoutException($"{url} 30 saniyede cevap vermedi.");
    }

    [GeneratedRegex(@"^(\d{2}:\d{2}:\d{2}\.\d{3})")]
    private static partial Regex Timestamp();

    private sealed record PageResult(
        string Name,
        string Path,
        double FirstRunMs,
        double MedianMs,
        double MaxMs,
        IReadOnlyList<double> RunsMs,
        int ApiRequests,
        long HtmlBytes,
        double SlowestApiMs);
}
