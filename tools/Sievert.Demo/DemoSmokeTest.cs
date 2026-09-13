using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Sievert.Demo;

/// <summary>
/// Demo ortaminin ucdan uca kontrolu.
///
/// "Acildi" yetmiyor: panel sayfalari, gorsellestirme uclari ve **gercek bir PDF**
/// kontrol ediliyor. CI'da bu kip kosuyor; sessizce atlanmiyor, sonucu gorunuyor.
/// </summary>
public static class DemoSmokeTest
{
    public static async Task<int> RunAsync(
        HttpClient client,
        string apiUrl,
        string webUrl,
        ImportResult imported,
        CancellationToken cancellation)
    {
        Console.WriteLine();
        Console.WriteLine("Duman testi:");

        List<string> failures = [];

        await CheckAsync(client, failures, "saglik", $"{apiUrl}/api/v1/health", cancellation);
        await CheckAsync(client, failures, "panel ana sayfa", webUrl, cancellation);
        await CheckAsync(client, failures, "depo listesi", $"{webUrl}/repositories", cancellation);
        await CheckAsync(
            client, failures, "depo sayfasi", $"{webUrl}/repositories/{imported.RepositoryId}", cancellation);

        await CheckAsync(
            client,
            failures,
            "dosya etkinligi ucu",
            $"{apiUrl}/api/v1/repositories/{imported.RepositoryId}/visualizations/file-activity"
            + $"?analysisJobId={imported.RiskJobId}&commitWindow=200&limit=50",
            cancellation);

        await CheckAsync(
            client,
            failures,
            "zaman cizelgesi ucu",
            $"{apiUrl}/api/v1/repositories/{imported.RepositoryId}/visualizations/risk-timeline"
            + $"?analysisJobId={imported.RiskJobId}&count=100",
            cancellation);

        Guid? reportId = await ReportAsync(client, apiUrl, imported, failures, cancellation);

        if (reportId is Guid id)
        {
            await DownloadAsync(client, apiUrl, id, failures, cancellation);
        }

        Console.WriteLine();

        if (failures.Count == 0)
        {
            Console.WriteLine("Duman testi gecti.");

            return 0;
        }

        foreach (string failure in failures)
        {
            Console.Error.WriteLine($"  BASARISIZ: {failure}");
        }

        return 1;
    }

    private static async Task CheckAsync(
        HttpClient client,
        List<string> failures,
        string name,
        string url,
        CancellationToken cancellation)
    {
        try
        {
            using HttpResponseMessage response = await client.GetAsync(url, cancellation);

            Console.WriteLine($"  {name}: {(int)response.StatusCode}");

            if (!response.IsSuccessStatusCode)
            {
                failures.Add($"{name} {(int)response.StatusCode} dondu");
            }
        }
        catch (HttpRequestException error)
        {
            Console.WriteLine($"  {name}: baglanti hatasi");
            failures.Add($"{name}: {error.Message}");
        }
    }

    private static async Task<Guid?> ReportAsync(
        HttpClient client,
        string apiUrl,
        ImportResult imported,
        List<string> failures,
        CancellationToken cancellation)
    {
        using HttpRequestMessage request = new(
            HttpMethod.Post, $"{apiUrl}/api/v1/repositories/{imported.RepositoryId}/reports")
        {
            Content = JsonContent.Create(new
            {
                riskAnalysisJobId = imported.RiskJobId,
                staticAnalysisJobId = imported.StaticJobId,
                culture = "tr-TR",
            }),
        };

        request.Headers.TryAddWithoutValidation("Idempotency-Key", "demo-duman-testi");

        using HttpResponseMessage response = await client.SendAsync(request, cancellation);

        Console.WriteLine($"  rapor istegi: {(int)response.StatusCode}");

        if (!response.IsSuccessStatusCode)
        {
            failures.Add($"rapor istegi {(int)response.StatusCode} dondu");

            return null;
        }

        using JsonDocument accepted = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync(cancellation));

        Guid reportId = accepted.RootElement.GetProperty("reportId").GetGuid();

        DateTimeOffset deadline = DateTimeOffset.UtcNow.AddMinutes(2);

        while (DateTimeOffset.UtcNow < deadline)
        {
            using JsonDocument report = JsonDocument.Parse(
                await client.GetStringAsync($"{apiUrl}/api/v1/reports/{reportId}", cancellation));

            string status = report.RootElement.GetProperty("status").GetString()!;

            if (status != "pending")
            {
                Console.WriteLine($"  rapor durumu: {status}");

                if (status != "ready")
                {
                    failures.Add($"rapor {status} durumunda bitti");

                    return null;
                }

                return reportId;
            }

            await Task.Delay(250, cancellation);
        }

        failures.Add("rapor iki dakikada hazir olmadi");

        return null;
    }

    private static async Task DownloadAsync(
        HttpClient client,
        string apiUrl,
        Guid reportId,
        List<string> failures,
        CancellationToken cancellation)
    {
        using JsonDocument report = JsonDocument.Parse(
            await client.GetStringAsync($"{apiUrl}/api/v1/reports/{reportId}", cancellation));

        string expected = report.RootElement.GetProperty("sha256").GetString()!;

        byte[] content = await client.GetByteArrayAsync(
            $"{apiUrl}/api/v1/reports/{reportId}/download", cancellation);

        string actual = Convert.ToHexStringLower(SHA256.HashData(content));
        string header = Encoding.ASCII.GetString(content, 0, Math.Min(5, content.Length));

        Console.WriteLine($"  PDF: {content.Length / 1024.0:F1} KB, basligi {header}");

        if (header != "%PDF-")
        {
            failures.Add("indirilen dosya PDF degil");
        }

        if (!string.Equals(expected, actual, StringComparison.Ordinal))
        {
            failures.Add("indirilen PDF'in ozeti metadata ile tutmuyor");
        }
    }
}
