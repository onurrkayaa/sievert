using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;

using Microsoft.EntityFrameworkCore;

using Sievert.Api;

using Sievert.Data;
using Sievert.Data.Entities;
using Sievert.Modeling;

namespace Sievert.Measure;

/// <summary>
/// Asama 6 Adim 2: API'nin temel olcumu.
///
/// Iki ayri yoldan olcuyor ve ikisini karistirmamak onemli:
///
/// - **HTTP tarafı:** API gercek bir surec olarak baslatiliyor ve istekler aga
///   benzeyen bir yoldan gidiyor. Sure, cevap boyutu, uyarilar ve bellek buradan.
/// - **Surec ici taraf:** aciklama-model farki ve endeks monotonlugu butun veritabani
///   uzerinde, HTTP'ye hic girmeden olculuyor. HTTP uzerinden 34 bin istek atmak ayni
///   sayiyi gunlerce uretirdi ve olculen sey yine ayni hesap olurdu.
/// </summary>
public static class ApiBaselineCommand
{
    /// <summary>Ornekleme tohumu. Sabit; ayni kosu ayni commit'leri seciyor.</summary>
    private const int Seed = 20260912;

    private const int SampleSize = 300;

    /// <summary>Ornekte C# dosyasi degistirmeyen commit sayisi.</summary>
    private const int ZeroCsharpTarget = 50;

    private const string BaseUrl = "http://127.0.0.1:5199";

    public static async Task<int> RunAsync(SievertContext context, string[] args)
    {
        string repositoryRoot = Path.GetFullPath(args[1]);
        string codeCommit = args[2];
        string outputPath = args[3];

        string dataDirectory = Path.Combine(repositoryRoot, "data", "asama5");
        ModelRegistry registry = ModelRegistry.Create(
            Path.Combine(dataDirectory, "model-results.json"),
            Path.Combine(dataDirectory, "models"));

        ScoreReference reference = ScoreReference.Load(
            Path.Combine(repositoryRoot, "data", "asama6", "model-score-reference.json"));

        List<Target> sample = Sample(context, registry);

        Console.WriteLine($"Ornek: {sample.Count} commit, {sample.Count(item => item.CsFilesChanged == 0)} tanesi CsFilesChanged=0");
        Console.WriteLine();

        HttpMeasurement http = await MeasureHttpAsync(repositoryRoot, sample);
        QueryCounts queries = await MeasureQueriesAsync(repositoryRoot, sample);
        WholeDatabase whole = MeasureWholeDatabase(context, registry, reference);

        string json = Render(codeCommit, sample, http, queries, whole);
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        File.WriteAllText(outputPath, json, new UTF8Encoding(false));

        Print(http, queries, whole);
        Console.WriteLine();
        Console.WriteLine($"{outputPath} yazildi.");

        return http.MissingMandatoryWarnings == 0 && whole.LogitViolations == 0 && whole.MonotonicityViolations == 0
            ? 0
            : 1;
    }

    /// <summary>Sabit tohumla ornek: once sifir C# grubundan, sonra geri kalanindan.</summary>
    private static List<Target> Sample(SievertContext context, ModelRegistry registry)
    {
        List<Target> all =
        [
            .. context.Commits
                .AsNoTracking()
                .Join(
                    context.CommitMetrics.AsNoTracking(),
                    commit => commit.Id,
                    metric => metric.CommitId,
                    (commit, metric) => new { commit.RepositoryId, commit.Sha, metric.CsFilesChanged })
                .OrderBy(row => row.RepositoryId)
                .ThenBy(row => row.Sha)
                .Select(row => new Target(row.RepositoryId, row.Sha, row.CsFilesChanged))
        ];

        HashSet<int> scorable =
        [
            .. context.Repositories
                .AsNoTracking()
                .Where(row => true)
                .ToList()
                // sievert:disable SV004 bellekte suzme; kimlik esleme veritabaninda degil
                .Where(row => registry.ForRepository(row.Identity) is not null)
                .Select(row => row.Id)
        ];

        all.RemoveAll(item => !scorable.Contains(item.RepositoryId));

        Random random = new(Seed);

        List<Target> zero = Pick(all.Where(item => item.CsFilesChanged == 0), ZeroCsharpTarget, random);
        List<Target> rest = Pick(all.Where(item => item.CsFilesChanged > 0), SampleSize - zero.Count, random);

        return [.. zero, .. rest];
    }

    private static List<Target> Pick(IEnumerable<Target> source, int count, Random random)
    {
        List<Target> pool = [.. source];

        // Fisher-Yates; sabit tohumla ayni siralama.
        for (int index = pool.Count - 1; index > 0; index--)
        {
            int other = random.Next(index + 1);
            (pool[index], pool[other]) = (pool[other], pool[index]);
        }

        return [.. pool.Take(Math.Min(count, pool.Count))];
    }

    private static async Task<HttpMeasurement> MeasureHttpAsync(string repositoryRoot, List<Target> sample)
    {
        using ApiProcess api = ApiProcess.Start(repositoryRoot, logQueries: false);
        using HttpClient client = new() { BaseAddress = new Uri(BaseUrl), Timeout = TimeSpan.FromMinutes(2) };

        Stopwatch startup = Stopwatch.StartNew();
        await api.WaitUntilHealthyAsync(client);
        startup.Stop();

        // Ilk risk istegi modeli ve skor referansini de yukluyor; ayri olculuyor.
        Stopwatch cold = Stopwatch.StartNew();
        using (HttpResponseMessage first = await client.GetAsync(RiskPath(sample[0])))
        {
            first.EnsureSuccessStatusCode();
        }

        cold.Stop();

        List<double> durations = [];
        List<int> sizes = [];
        // Endeks profil basina olceklendigi icin monotonluk da profil basina bakiliyor.
        // Uc deponun noktalarini tek listede siralamak, karsilastirilamaz iki endeksi
        // karsilastirmak olurdu - sozlesme bunu acikca soyluyor.
        Dictionary<int, List<(double Score, double Index)>> points = [];
        int missing = 0;
        int coverage = 0;
        int outside = 0;

        foreach (Target target in sample)
        {
            Stopwatch watch = Stopwatch.StartNew();
            using HttpResponseMessage response = await client.GetAsync(RiskPath(target));
            string body = await response.Content.ReadAsStringAsync();
            watch.Stop();

            response.EnsureSuccessStatusCode();

            durations.Add(watch.Elapsed.TotalMilliseconds);
            sizes.Add(Encoding.UTF8.GetByteCount(body));

            JsonElement root = JsonDocument.Parse(body).RootElement;
            List<string> warnings = [.. root.GetProperty("warnings").EnumerateArray().Select(item => item.GetString()!)];

            foreach (string mandatory in (string[])["UNCALIBRATED_SCORE", "SZZ_TARGET", "STATIC_ANALYSIS_NOT_INCLUDED"])
            {
                if (!warnings.Contains(mandatory))
                {
                    missing++;
                }
            }

            coverage += warnings.Contains("CS_LABEL_COVERAGE_LIMIT") ? 1 : 0;
            outside += warnings.Contains("OUTSIDE_TRAIN_RANGE") ? 1 : 0;

            if (!points.TryGetValue(target.RepositoryId, out List<(double Score, double Index)>? repository))
            {
                repository = [];
                points[target.RepositoryId] = repository;
            }

            repository.Add((root.GetProperty("rawModelScore").GetDouble(), root.GetProperty("riskIndex").GetDouble()));
        }

        long peak = api.PeakWorkingSetBytes;
        api.Stop();

        durations.Sort();

        return new HttpMeasurement(
            startup.Elapsed.TotalMilliseconds,
            cold.Elapsed.TotalMilliseconds,
            durations[0],
            Percentile(durations, 0.50),
            Percentile(durations, 0.95),
            durations[^1],
            sizes.Min(),
            (int)sizes.Average(),
            sizes.Max(),
            missing,
            coverage,
            outside,
            peak,
            points.Values.Sum(MonotonicityViolations));
    }

    /// <summary>
    /// Istek basina kac veritabani komutu calistigi. EF Core'un komut gunlugu aciliyor;
    /// bu gunluk olcumun kendisini yavaslattigi icin sureler AYRI kosudan geliyor.
    /// </summary>
    private static async Task<QueryCounts> MeasureQueriesAsync(string repositoryRoot, List<Target> sample)
    {
        using ApiProcess api = ApiProcess.Start(repositoryRoot, logQueries: true);
        using HttpClient client = new() { BaseAddress = new Uri(BaseUrl), Timeout = TimeSpan.FromMinutes(2) };

        await api.WaitUntilHealthyAsync(client);

        // Isindirma: ilk istegin acilis sorgulari sayima girmesin.
        using (HttpResponseMessage warm = await client.GetAsync(RiskPath(sample[0])))
        {
            warm.EnsureSuccessStatusCode();
        }

        int risk = await CountAsync(api, client, RiskPath(sample[1]), 5);
        int detail = await CountAsync(api, client, $"/api/v1/repositories/{sample[1].RepositoryId}", 5);
        int commits = await CountAsync(api, client, $"/api/v1/repositories/{sample[1].RepositoryId}/commits", 5);
        int list = await CountAsync(api, client, "/api/v1/repositories", 5);

        api.Stop();

        return new QueryCounts(risk, detail, commits, list);
    }

    private static async Task<int> CountAsync(ApiProcess api, HttpClient client, string path, int repeats)
    {
        int before = api.DatabaseCommandCount;

        for (int index = 0; index < repeats; index++)
        {
            using HttpResponseMessage response = await client.GetAsync(path);
            response.EnsureSuccessStatusCode();
        }

        // Gunluk satirlari ayri bir is parcaciginda toplaniyor; son satirlar icin bekle.
        await Task.Delay(500);

        return (api.DatabaseCommandCount - before) / repeats;
    }

    /// <summary>
    /// Butun veritabani uzerinde iki kontrol: aciklamanin modelden sapmasi ve endeksin
    /// skorla birlikte hic dusmemesi.
    /// </summary>
    private static WholeDatabase MeasureWholeDatabase(
        SievertContext context,
        ModelRegistry registry,
        ScoreReference reference)
    {
        double worst = 0.0;
        double worstMagnitude = 0.0;
        double worstAbsolute = 0.0;
        List<double> violatingMagnitudes = [];
        int rows = 0;
        int monotonicity = 0;
        double smallestViolatingMagnitude = double.MaxValue;

        foreach (RepositoryRow repository in context.Repositories.AsNoTracking().OrderBy(row => row.Identity).ToList())
        {
            if (registry.ForRepository(repository.Identity) is not ModelProfile profile)
            {
                continue;
            }

            FeatureScaler scaler = registry.ScalerFor(profile);
            LoadedModel model = registry.Load(profile.ProfileCode);
            ScoreDistribution distribution = reference.For(profile.ProfileCode)!;

            List<SnapshotRow> snapshot =
            [
                .. context.Commits
                    .AsNoTracking()
                    .Where(commit => commit.RepositoryId == repository.Id)
                    .Join(
                        context.CommitMetrics.AsNoTracking(),
                        commit => commit.Id,
                        metric => metric.CommitId,
                        (commit, metric) => new { commit, metric })
                    .OrderBy(pair => pair.commit.Sha)
                    .ToList()
                    // sievert:disable SV004 bellekte donusum; sorgu yukarida tek seferde kosuyor
                    .Select(pair => CommitFeatures.ToSnapshotRow(repository, pair.commit, pair.metric))
            ];

            List<float[]> features = [.. snapshot.Select(scaler.Apply)];
            IReadOnlyList<ModelScore> scores = model.Evaluate(features);

            List<(double Score, double Index)> points = [];

            for (int index = 0; index < snapshot.Count; index++)
            {
                double explained = profile.Coefficients.Intercept;
                double absolute = Math.Abs(profile.Coefficients.Intercept);

                for (int feature = 0; feature < features[index].Length; feature++)
                {
                    double contribution = profile.Coefficients.Weights[feature] * features[index][feature];
                    explained += contribution;
                    absolute += Math.Abs(contribution);
                }

                double difference = Math.Abs(scores[index].Logit - explained);
                double magnitude = Math.Abs(scores[index].Logit);

                if (difference > worst)
                {
                    worst = difference;
                    worstMagnitude = magnitude;
                    worstAbsolute = absolute;
                }

                if (difference > ModelExplainer.Tolerance)
                {
                    violatingMagnitudes.Add(magnitude);
                    smallestViolatingMagnitude = Math.Min(smallestViolatingMagnitude, magnitude);
                }

                points.Add((scores[index].Probability, distribution.RiskIndex(scores[index].Probability)));
                rows++;
            }

            monotonicity += MonotonicityViolations(points);
        }

        return new WholeDatabase(
            rows,
            worst,
            worstMagnitude,
            worstAbsolute,
            violatingMagnitudes.Count,
            violatingMagnitudes.Count == 0 ? 0.0 : smallestViolatingMagnitude,
            violatingMagnitudes.Count == 0 ? 0.0 : violatingMagnitudes.Max(),
            monotonicity);
    }

    private static int MonotonicityViolations(List<(double Score, double Index)> points)
    {
        List<(double Score, double Index)> ordered = [.. points.OrderBy(point => point.Score)];
        int violations = 0;

        for (int index = 1; index < ordered.Count; index++)
        {
            if (ordered[index].Index < ordered[index - 1].Index)
            {
                violations++;
            }
        }

        return violations;
    }

    private static string RiskPath(Target target) =>
        $"/api/v1/repositories/{target.RepositoryId}/commits/{target.Sha}/risk";

    /// <summary>En yakin sira yontemi; ara deger uretilmiyor.</summary>
    private static double Percentile(List<double> sorted, double share) =>
        sorted[Math.Min(sorted.Count - 1, (int)Math.Ceiling(share * sorted.Count) - 1)];

    private static void Print(HttpMeasurement http, QueryCounts queries, WholeDatabase whole)
    {
        Console.WriteLine($"acilis (saglik ucu cevap verene kadar): {http.StartupMilliseconds:F0} ms");
        Console.WriteLine($"ilk risk istegi (model + referans yukleniyor): {http.FirstRequestMilliseconds:F0} ms");
        Console.WriteLine(
            $"isinmis risk istegi: en kucuk {http.MinimumMilliseconds:F1} ms, medyan {http.MedianMilliseconds:F1} ms, "
            + $"p95 {http.P95Milliseconds:F1} ms, en buyuk {http.MaximumMilliseconds:F1} ms");
        Console.WriteLine($"cevap boyutu: {http.MinimumBytes} / {http.MeanBytes} / {http.MaximumBytes} bayt");
        Console.WriteLine($"eksik zorunlu uyari: {http.MissingMandatoryWarnings}");
        Console.WriteLine($"kapsam uyarisi cikan istek: {http.CoverageWarnings}");
        Console.WriteLine($"aralik disi uyarisi cikan istek: {http.OutsideRangeWarnings}");
        Console.WriteLine($"API surecinin en yuksek olculen bellegi: {http.PeakWorkingSetBytes / (1024 * 1024)} MB");
        Console.WriteLine();
        Console.WriteLine($"istek basina veritabani komutu: risk {queries.Risk}, depo {queries.RepositoryDetail}, "
            + $"commit listesi {queries.CommitList}, depo listesi {queries.RepositoryList}");
        Console.WriteLine();
        Console.WriteLine($"butun veritabani: {whole.Rows} satir");
        Console.WriteLine($"  en buyuk logit farki: {whole.WorstLogitDifference:E3} (tolerans {ModelExplainer.Tolerance:E0})");
        Console.WriteLine($"  o satirin |logit| degeri: {whole.WorstLogitMagnitude:F2}, "
            + $"mutlak katki toplami: {whole.WorstRowAbsoluteContributionSum:F2}");
        Console.WriteLine($"  toleransi asan satir : {whole.LogitViolations}");
        Console.WriteLine($"  asan satirlarin |logit| araligi: {whole.SmallestViolatingLogitMagnitude:F2} - "
            + $"{whole.LargestViolatingLogitMagnitude:F2}");
        Console.WriteLine($"  monotonluk ihlali    : {whole.MonotonicityViolations}");
    }

    private static string Render(
        string codeCommit,
        List<Target> sample,
        HttpMeasurement http,
        QueryCounts queries,
        WholeDatabase whole)
    {
        StringBuilder text = new();
        text.Append("{\n");
        text.Append("  \"codeCommit\": \"").Append(codeCommit).Append("\",\n");
        text.Append("  \"seed\": ").Append(Seed).Append(",\n");
        text.Append("  \"sampleSize\": ").Append(sample.Count).Append(",\n");
        text.Append("  \"sampleWithoutCSharpFiles\": ").Append(sample.Count(item => item.CsFilesChanged == 0)).Append(",\n");
        text.Append("  \"http\": {\n");
        text.Append("    \"startupMs\": ").Append(Number(http.StartupMilliseconds)).Append(",\n");
        text.Append("    \"firstRequestMs\": ").Append(Number(http.FirstRequestMilliseconds)).Append(",\n");
        text.Append("    \"warmMinimumMs\": ").Append(Number(http.MinimumMilliseconds)).Append(",\n");
        text.Append("    \"warmMedianMs\": ").Append(Number(http.MedianMilliseconds)).Append(",\n");
        text.Append("    \"warmP95Ms\": ").Append(Number(http.P95Milliseconds)).Append(",\n");
        text.Append("    \"warmMaximumMs\": ").Append(Number(http.MaximumMilliseconds)).Append(",\n");
        text.Append("    \"minimumBytes\": ").Append(http.MinimumBytes).Append(",\n");
        text.Append("    \"meanBytes\": ").Append(http.MeanBytes).Append(",\n");
        text.Append("    \"maximumBytes\": ").Append(http.MaximumBytes).Append(",\n");
        text.Append("    \"missingMandatoryWarnings\": ").Append(http.MissingMandatoryWarnings).Append(",\n");
        text.Append("    \"coverageWarnings\": ").Append(http.CoverageWarnings).Append(",\n");
        text.Append("    \"outsideRangeWarnings\": ").Append(http.OutsideRangeWarnings).Append(",\n");
        text.Append("    \"peakWorkingSetBytes\": ").Append(http.PeakWorkingSetBytes).Append(",\n");
        text.Append("    \"monotonicityViolations\": ").Append(http.MonotonicityViolations).Append('\n');
        text.Append("  },\n");
        text.Append("  \"databaseCommandsPerRequest\": {\n");
        text.Append("    \"risk\": ").Append(queries.Risk).Append(",\n");
        text.Append("    \"repositoryDetail\": ").Append(queries.RepositoryDetail).Append(",\n");
        text.Append("    \"commitList\": ").Append(queries.CommitList).Append(",\n");
        text.Append("    \"repositoryList\": ").Append(queries.RepositoryList).Append('\n');
        text.Append("  },\n");
        text.Append("  \"wholeDatabase\": {\n");
        text.Append("    \"rows\": ").Append(whole.Rows).Append(",\n");
        text.Append("    \"tolerance\": ").Append(Number(ModelExplainer.Tolerance)).Append(",\n");
        text.Append("    \"worstLogitDifference\": ").Append(Number(whole.WorstLogitDifference)).Append(",\n");
        text.Append("    \"worstLogitMagnitude\": ").Append(Number(whole.WorstLogitMagnitude)).Append(",\n");
        text.Append("    \"worstRowAbsoluteContributionSum\": ")
            .Append(Number(whole.WorstRowAbsoluteContributionSum)).Append(",\n");
        text.Append("    \"logitViolations\": ").Append(whole.LogitViolations).Append(",\n");
        text.Append("    \"smallestViolatingLogitMagnitude\": ")
            .Append(Number(whole.SmallestViolatingLogitMagnitude)).Append(",\n");
        text.Append("    \"largestViolatingLogitMagnitude\": ")
            .Append(Number(whole.LargestViolatingLogitMagnitude)).Append(",\n");
        text.Append("    \"monotonicityViolations\": ").Append(whole.MonotonicityViolations).Append('\n');
        text.Append("  }\n");
        text.Append("}\n");

        return text.ToString();
    }

    private static string Number(double value) => value.ToString("R", CultureInfo.InvariantCulture);

    private sealed record Target(int RepositoryId, string Sha, int CsFilesChanged);

    private sealed record HttpMeasurement(
        double StartupMilliseconds,
        double FirstRequestMilliseconds,
        double MinimumMilliseconds,
        double MedianMilliseconds,
        double P95Milliseconds,
        double MaximumMilliseconds,
        int MinimumBytes,
        int MeanBytes,
        int MaximumBytes,
        int MissingMandatoryWarnings,
        int CoverageWarnings,
        int OutsideRangeWarnings,
        long PeakWorkingSetBytes,
        int MonotonicityViolations);

    private sealed record QueryCounts(int Risk, int RepositoryDetail, int CommitList, int RepositoryList);

    private sealed record WholeDatabase(
        int Rows,
        double WorstLogitDifference,
        double WorstLogitMagnitude,
        double WorstRowAbsoluteContributionSum,
        int LogitViolations,
        double SmallestViolatingLogitMagnitude,
        double LargestViolatingLogitMagnitude,
        int MonotonicityViolations);
}
