using System.Globalization;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using Microsoft.EntityFrameworkCore;

using Sievert.Data;
using Sievert.Data.Entities;

namespace Sievert.Measure;

/// <summary>
/// Asama 6 Adim 6: PDF raporunun bagimsiz dogrulanmasi.
///
/// Rapordaki sayilar, raporu ureten koda hic bakilmadan **ham tablolardan** yeniden
/// hesaplaniyor ve PDF'in kendi metniyle karsilastiriliyor. PDF metni de uretici
/// kutuphaneyle degil, ayri bir cikariciyla okunuyor (<see cref="PdfText"/>).
///
/// Yuvarlanmis degerlerde ham double esitligi beklenmiyor: karsilastirma rapor
/// sozlesmesindeki bicimle (endeks 1 ondalik, ham skor 4 ondalik) yapiliyor ve yuvarlama
/// kurali burada bagimsiz olarak uygulaniyor.
/// </summary>
public static class ReportTruthCommand
{
    private const string ApiUrl = "http://127.0.0.1:5193";

    private const int CommitWindow = 200;

    private const int FileLimit = 25;

    private const int TimelineCount = 100;

    private const int TopCommitCount = 10;

    // sievert:disable SV006 komut satirindan tek sefer kosuyor; iptali Ctrl+C yapiyor
    public static async Task<int> RunAsync(SievertContext context, string[] args)
    {
        string repositoryRoot = Path.GetFullPath(args[1]);
        string codeCommit = args[2];
        string outputPath = args[3];

        string publishRoot = Path.Combine(Path.GetTempPath(), "sievert-rapor-dogrulama");

        Console.WriteLine("Release yayimi hazirlaniyor...");

        string apiDll = HostProcess.Publish(repositoryRoot, "Sievert.Api", publishRoot);

        List<Target> targets = Targets(context);
        List<ReportCheck> checks = [];

        using (HostProcess api = HostProcess.Start(apiDll, ApiUrl, repositoryRoot, null))
        {
            using HttpClient client = new() { BaseAddress = new Uri(ApiUrl), Timeout = TimeSpan.FromMinutes(5) };

            await WaitAsync(client);

            foreach (Target target in targets)
            {
                Console.WriteLine();
                Console.WriteLine($"{target.Name}");

                checks.Add(await CheckAsync(context, client, target));
            }
        }

        int differences = checks.Sum(check => check.Differences.Count);

        string json = JsonSerializer.Serialize(
            new
            {
                kod = codeCommit,
                uretildi = DateTimeOffset.UtcNow,
                aciklama = "PDF raporundaki degerler ham tablolardan yeniden hesaplanip "
                    + "PDF metniyle karsilastirildi. PDF metni ayri bir cikariciyla okundu.",
                parametreler = new
                {
                    commitWindow = CommitWindow,
                    fileLimit = FileLimit,
                    timelineCount = TimelineCount,
                    topCommitCount = TopCommitCount,
                },
                toplamKontrol = checks.Sum(check => check.Checked),
                toplamFark = differences,
                raporlar = checks,
            },
            new JsonSerializerOptions { WriteIndented = true });

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outputPath))!);
        File.WriteAllText(outputPath, json, new UTF8Encoding(false));

        Console.WriteLine();
        Console.WriteLine($"{checks.Sum(check => check.Checked)} kontrol, {differences} fark");
        Console.WriteLine($"{outputPath} yazildi.");

        return differences == 0 ? 0 : 1;
    }

    private static async Task<ReportCheck> CheckAsync(
        SievertContext context, HttpClient client, Target target)
    {
        using HttpRequestMessage request = new(
            HttpMethod.Post, $"/api/v1/repositories/{target.Id}/reports")
        {
            Content = JsonContent.Create(new
            {
                riskAnalysisJobId = target.RiskJobId,
                staticAnalysisJobId = target.StaticJobId,
                culture = "tr-TR",
                commitWindow = CommitWindow,
                fileLimit = FileLimit,
                timelineCount = TimelineCount,
                topCommitCount = TopCommitCount,
            }),
        };

        request.Headers.TryAddWithoutValidation("Idempotency-Key", $"truth-{target.Id}");

        using HttpResponseMessage accepted = await client.SendAsync(request);

        accepted.EnsureSuccessStatusCode();

        JsonElement created = JsonDocument
            .Parse(await accepted.Content.ReadAsStringAsync()).RootElement;

        Guid reportId = created.GetProperty("reportId").GetGuid();

        JsonElement report = await WaitForReadyAsync(client, reportId);

        byte[] pdf = await client.GetByteArrayAsync($"/api/v1/reports/{reportId}/download");

        string extracted = PdfText.Extract(pdf);
        string text = Normalise(extracted);

        // Uzun dosya yollari satir sonunda bolunuyor ve araya bosluk giriyor. Yol ve
        // ozet gibi bosluksuz degerler icin butun boslugu atilmis bir kopya kullaniliyor.
        string dense = new(extracted.Where(character => !char.IsWhiteSpace(character)).ToArray());

        string manifest = await client.GetStringAsync($"/api/v1/reports/{reportId}/manifest");

        List<string> differences = [];
        int count = 0;

        // 1 - Depo kimligi ve model.
        Expect(dense, target.Identity, "depo kimligi", differences, ref count);
        Expect(text, target.Profile, "model profili", differences, ref count);

        // 2 - Manifest ozeti: metadata ile PDF ayni ozeti tasimali.
        string manifestChecksum = Convert.ToHexStringLower(
            SHA256.HashData(new UTF8Encoding(false).GetBytes(manifest)));

        count++;

        if (manifestChecksum != report.GetProperty("manifestSha256").GetString())
        {
            differences.Add("manifest ozeti metadata ile tutmuyor");
        }

        Expect(dense, manifestChecksum, "manifest ozeti PDF'te", differences, ref count);

        // 3 - Ham tablolardan yeniden hesap.
        Recomputed expected = await RecomputeAsync(context, target);

        Expect(text, expected.CoveredCommits.ToString(CultureInfo.InvariantCulture),
            "kapsanan commit sayisi", differences, ref count);

        Expect(text, Index(expected.MeanIndex), "ortalama endeks", differences, ref count);
        Expect(text, Index(expected.MedianIndex), "medyan endeks", differences, ref count);
        Expect(text, Index(expected.MaxIndex), "en yuksek endeks", differences, ref count);

        Expect(text, expected.DecisionAt05.ToString(CultureInfo.InvariantCulture),
            "0,5 karar sayisi", differences, ref count);

        Expect(text, expected.DecisionAtTrain.ToString(CultureInfo.InvariantCulture),
            "egitim esigi karar sayisi", differences, ref count);

        // 4 - En yuksek endeksli commit'ler: kisa sha ve siralama.
        int position = -1;

        foreach (string sha in expected.TopShas)
        {
            count++;

            int found = dense.IndexOf(sha, StringComparison.Ordinal);

            if (found < 0)
            {
                differences.Add($"commit {sha} raporda yok");
            }
            else if (found < position)
            {
                differences.Add($"commit {sha} beklenen siradan once geciyor");
            }
            else
            {
                position = found;
            }
        }

        // 5 - Dosya etkinligi: ilk yollar ve degerleri.
        foreach ((string path, double mean) in expected.TopFiles)
        {
            Expect(dense, path, $"dosya {path}", differences, ref count);
            Expect(text, Index(mean), $"dosya {path} ortalamasi", differences, ref count);
        }

        // 6 - Zaman cizelgesinin ilk ve son tarihi.
        Expect(text, expected.FirstDate, "ilk tarih", differences, ref count);
        Expect(text, expected.LastDate, "son tarih", differences, ref count);

        // 7 - Statik bulgu sayisi ve kural dagilimi.
        if (target.StaticJobId is not null)
        {
            Expect(text, expected.FindingCount.ToString(CultureInfo.InvariantCulture),
                "statik bulgu sayisi", differences, ref count);

            foreach ((string rule, int rows) in expected.RuleCounts)
            {
                Expect(text, $"{rule} {rows.ToString("N0", Turkish)}",
                    $"kural {rule} sayisi", differences, ref count);
            }
        }

        // 8 - Kapsam ve zorunlu cumleler.
        // Beklenen cumleler raporun kendi yazimiyla, Turkce karakterleriyle araniyor:
        // "degildir" yazip gecmek, Turkce karakterlerin bozulmadigini sinamamak olurdu.
        Expect(text, "Kalibre edilmedi", "kalibrasyon ifadesi", differences, ref count);
        Expect(text, "Bu rapor kesin kusur kararı değildir.", "urun siniri", differences, ref count);
        Expect(text, "Statik bulgular ham model skoruna dahil değildir", "ayrim", differences, ref count);
        Expect(text, "Göreli risk endeksi", "endeks tanimi", differences, ref count);

        count++;

        if (text.Contains("hata olasılığı", StringComparison.OrdinalIgnoreCase)
            || text.Contains("hata olasiligi", StringComparison.OrdinalIgnoreCase)
            || text.Contains("birleşik skor", StringComparison.OrdinalIgnoreCase))
        {
            differences.Add("yasakli ifade bulundu");
        }

        Console.WriteLine($"  {count} kontrol, {differences.Count} fark");

        foreach (string difference in differences)
        {
            Console.WriteLine($"    FARK: {difference}");
        }

        return new ReportCheck(
            target.Name,
            reportId,
            report.GetProperty("pageCount").GetInt32(),
            report.GetProperty("byteLength").GetInt64(),
            count,
            differences);
    }

    /// <summary>
    /// Rapordan bagimsiz hesap: sorgular rapor kodunun sorgularindan farkli yazildi
    /// (join yerine ayri cekip bellekte birlestirme).
    /// </summary>
    private static async Task<Recomputed> RecomputeAsync(SievertContext context, Target target)
    {
        List<CommitRiskSnapshotRow> snapshots = await context.CommitRiskSnapshots
            .AsNoTracking()
            .Where(row => row.AnalysisJobId == target.RiskJobId)
            .ToListAsync();

        Dictionary<int, CommitRow> commits = await context.Commits
            .AsNoTracking()
            .Where(row => row.RepositoryId == target.Id)
            .ToDictionaryAsync(row => row.Id);

        List<(CommitRow Commit, CommitRiskSnapshotRow Snapshot)> pairs =
        [
            .. snapshots
                .Where(snapshot => commits.ContainsKey(snapshot.CommitId))
                .Select(snapshot => (commits[snapshot.CommitId], snapshot))
                .OrderByDescending(pair => pair.Item1.AuthorDateUtc)
                .ThenByDescending(pair => pair.Item1.Id)
                .Take(CommitWindow),
        ];

        double[] indices = [.. pairs.Select(pair => pair.Snapshot.RiskIndex).Order()];

        double median = indices.Length == 0
            ? 0
            : indices.Length % 2 == 1
                ? indices[indices.Length / 2]
                : (indices[(indices.Length / 2) - 1] + indices[indices.Length / 2]) / 2;

        List<(CommitRow Commit, CommitRiskSnapshotRow Snapshot)> ranked =
        [
            .. pairs
                .OrderByDescending(pair => pair.Snapshot.RiskIndex)
                .ThenByDescending(pair => pair.Commit.AuthorDateUtc)
                .ThenBy(pair => pair.Commit.Sha, StringComparer.Ordinal),
        ];

        HashSet<int> windowIds = [.. pairs.Select(pair => pair.Commit.Id)];

        List<CommitFileRow> files = await context.CommitFiles
            .AsNoTracking()
            .Where(row => windowIds.Contains(row.CommitId) && row.IsCSharp)
            .ToListAsync();

        Dictionary<string, List<double>> byPath = new(StringComparer.Ordinal);

        foreach (IGrouping<(int Commit, string Path), CommitFileRow> group in files
            .GroupBy(row => (row.CommitId, row.Path)))
        {
            double index = pairs.First(pair => pair.Commit.Id == group.Key.Commit).Snapshot.RiskIndex;

            if (!byPath.TryGetValue(group.Key.Path, out List<double>? values))
            {
                byPath[group.Key.Path] = values = [];
            }

            values.Add(index);
        }

        List<(string Path, double Mean)> topFiles =
        [
            .. byPath
                .Select(entry => (entry.Key, Mean: Math.Round(entry.Value.Average(), 1, MidpointRounding.AwayFromZero)))
                .OrderByDescending(entry => entry.Mean)
                .ThenBy(entry => entry.Key, StringComparer.Ordinal)
                .Take(5),
        ];

        List<(string Rule, int Count)> rules = target.StaticJobId is null
            ? []
            : [.. (await context.StaticAnalysisFindings
                    .AsNoTracking()
                    .Where(row => row.AnalysisJobId == target.StaticJobId)
                    .GroupBy(row => row.RuleCode)
                    .Select(group => new { Rule = group.Key, Count = group.Count() })
                    .ToListAsync())
                .Select(row => (row.Rule, row.Count))
                .OrderBy(row => row.Rule, StringComparer.Ordinal)];

        int findingCount = target.StaticJobId is null
            ? 0
            : await context.StaticAnalysisFindings
                .AsNoTracking()
                .CountAsync(row => row.AnalysisJobId == target.StaticJobId);

        List<(CommitRow Commit, CommitRiskSnapshotRow Snapshot)> chronological =
            [.. pairs.OrderBy(pair => pair.Commit.AuthorDateUtc).ThenBy(pair => pair.Commit.Id)];

        return new Recomputed(
            pairs.Count,
            indices.Length == 0 ? 0 : Math.Round(indices.Average(), 1, MidpointRounding.AwayFromZero),
            Math.Round(median, 1, MidpointRounding.AwayFromZero),
            indices.Length == 0 ? 0 : indices[^1],
            pairs.Count(pair => pair.Snapshot.DecisionAt05),
            pairs.Count(pair => pair.Snapshot.DecisionAtTrainThreshold),
            [.. ranked.Take(TopCommitCount).Select(pair => Short(pair.Commit.Sha))],
            topFiles,
            Day(chronological[0].Commit.AuthorDateUtc),
            Day(chronological[^1].Commit.AuthorDateUtc),
            findingCount,
            rules);
    }

    private static readonly CultureInfo Turkish = CultureInfo.GetCultureInfo("tr-TR");

    private static string Short(string sha) => sha.Length >= 12 ? sha[..12] : sha;

    private static string Index(double value) => value.ToString("F1", Turkish);

    private static string Day(DateTimeOffset value) => value.UtcDateTime.ToString("d MMMM yyyy", Turkish);

    /// <summary>Bosluk ve satir sonu farklarini eritir; PDF'te kelimeler arasi bosluk degisebiliyor.</summary>
    private static string Normalise(string text)
    {
        StringBuilder cleaned = new(text.Length);

        foreach (char character in text)
        {
            cleaned.Append(char.IsWhiteSpace(character) ? ' ' : character);
        }

        string result = cleaned.ToString();

        while (result.Contains("  ", StringComparison.Ordinal))
        {
            result = result.Replace("  ", " ", StringComparison.Ordinal);
        }

        return result;
    }

    private static void Expect(
        string text, string value, string what, List<string> differences, ref int count)
    {
        count++;

        if (!text.Contains(value, StringComparison.Ordinal))
        {
            differences.Add($"{what}: raporda '{value}' yok");
        }
    }

    private static List<Target> Targets(SievertContext context)
    {
        List<Target> targets = [];

        var risks = context.AnalysisJobs
            .AsNoTracking()
            .Where(job => job.Kind == AnalysisJobKind.RiskScoreAll
                && job.Status == AnalysisJobStatus.Succeeded
                && job.IsResultComplete)
            .OrderByDescending(job => job.CompletedAtUtc)
            .Select(job => new { job.Id, job.RepositoryId })
            .ToList();

        Dictionary<int, RepositoryRow> repositories = context.Repositories
            .AsNoTracking()
            .ToDictionary(row => row.Id);

        foreach (var risk in risks)
        {
            if (targets.Exists(target => target.Id == risk.RepositoryId))
            {
                continue;
            }

            Guid? scan = context.AnalysisJobs
                .AsNoTracking()
                .Where(job => job.RepositoryId == risk.RepositoryId
                    && job.Kind == AnalysisJobKind.StaticScan
                    && job.Status == AnalysisJobStatus.Succeeded
                    && job.IsResultComplete)
                .OrderByDescending(job => job.CompletedAtUtc)
                .Select(job => (Guid?)job.Id)
                .FirstOrDefault();

            string profile = context.CommitRiskSnapshots
                .AsNoTracking()
                .Where(row => row.AnalysisJobId == risk.Id)
                .Select(row => row.ModelProfile)
                .FirstOrDefault() ?? string.Empty;

            targets.Add(new Target(
                risk.RepositoryId,
                repositories[risk.RepositoryId].Name,
                repositories[risk.RepositoryId].Identity,
                profile,
                risk.Id,
                scan));
        }

        return [.. targets.OrderBy(target => target.Name, StringComparer.Ordinal)];
    }

    private static async Task<JsonElement> WaitForReadyAsync(HttpClient client, Guid reportId)
    {
        DateTimeOffset deadline = DateTimeOffset.UtcNow.AddMinutes(3);

        while (DateTimeOffset.UtcNow < deadline)
        {
            JsonElement report = JsonDocument
                .Parse(await client.GetStringAsync($"/api/v1/reports/{reportId}")).RootElement;

            if (report.GetProperty("status").GetString() is not "pending")
            {
                return report;
            }

            await Task.Delay(200);
        }

        throw new TimeoutException($"Rapor hazir olmadi: {reportId}");
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

    private sealed record Target(
        int Id, string Name, string Identity, string Profile, Guid RiskJobId, Guid? StaticJobId);

    private sealed record Recomputed(
        int CoveredCommits,
        double MeanIndex,
        double MedianIndex,
        double MaxIndex,
        int DecisionAt05,
        int DecisionAtTrain,
        IReadOnlyList<string> TopShas,
        IReadOnlyList<(string Path, double Mean)> TopFiles,
        string FirstDate,
        string LastDate,
        int FindingCount,
        IReadOnlyList<(string Rule, int Count)> RuleCounts);

    private sealed record ReportCheck(
        string Repository,
        Guid ReportId,
        int PageCount,
        long ByteLength,
        int Checked,
        IReadOnlyList<string> Differences);
}
