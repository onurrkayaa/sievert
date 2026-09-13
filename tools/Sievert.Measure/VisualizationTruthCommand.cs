using System.Globalization;
using System.Text;
using System.Text.Json;

using Microsoft.EntityFrameworkCore;

using Sievert.Contracts;
using Sievert.Data;
using Sievert.Data.Entities;
using Sievert.Modeling;
using Sievert.Web;

namespace Sievert.Measure;

/// <summary>
/// Asama 6 Adim 5: gorsellestirme sayilarinin bagimsiz dogrulanmasi.
///
/// Kural su: burada hicbir sey uretici koddan **cagrilmiyor**. Dosya ozetleri ham
/// tablolardan yeniden hesaplaniyor (dosya basina ayri sorgu, ayri birlestirme), zaman
/// cizelgesi noktalari ayrica secilip siralaniyor. Ayni fonksiyonu cagirsaydim iki taraf
/// da ayni hatayi yapar ve olcum hicbir sey gostermezdi.
///
/// Tek istisna renk ve koordinat: orada sinanan sey **urunun kullandigi fonksiyonun
/// kendisi** - kultura duyarsiz mi, monoton mu. Ikinci bir kopya yazmak, olculen ile
/// calisani ayirirdi.
/// </summary>
public static class VisualizationTruthCommand
{
    private const string BaseUrl = "http://127.0.0.1:5199";

    /// <summary>Orneklem tohumu; ayni kosu ayni dosyalari seciyor.</summary>
    private const int Seed = 20260912;

    private const int FilesPerRepository = 10;

    private const int TimelinePointsPerRepository = 50;

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

        List<FileCheck> fileChecks = [];
        List<PointCheck> pointChecks = [];
        List<JobUsed> jobsUsed = [];

        using (ApiProcess api = ApiProcess.Start(repositoryRoot, logQueries: false))
        {
            using HttpClient client = new() { BaseAddress = new Uri(BaseUrl), Timeout = TimeSpan.FromMinutes(10) };

            await api.WaitUntilHealthyAsync(client);

            foreach (Target target in targets)
            {
                Guid job = await CompleteJobAsync(context, client, target);

                jobsUsed.Add(new JobUsed(target.Name, job, Snapshots(context, job)));

                fileChecks.AddRange(await CheckFilesAsync(context, client, target, job));
                pointChecks.AddRange(await CheckTimelineAsync(context, client, target, job));
            }

            api.Stop();
        }

        EncodingCheck encoding = CheckEncoding();

        string json = JsonSerializer.Serialize(
            new
            {
                kod = codeCommit,
                uretildi = DateTimeOffset.UtcNow,
                tohum = Seed,
                kullanilanIsler = jobsUsed,
                dosyalar = Summarise(fileChecks),
                dosyaOrnekleri = fileChecks,
                noktalar = SummarisePoints(pointChecks),
                gorselKodlama = encoding,
            },
            new JsonSerializerOptions { WriteIndented = true });

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        File.WriteAllText(outputPath, json, new UTF8Encoding(false));

        int differences = fileChecks.Sum(check => check.Differences) + pointChecks.Sum(check => check.Differences);

        Console.WriteLine();
        Console.WriteLine($"{fileChecks.Count} dosya, {pointChecks.Count} nokta, toplam fark {differences}");
        Console.WriteLine($"{outputPath} yazildi.");

        // Fark varsa komut basarisiz bitiyor: rapora gecmeden once duzeltilmeli.
        return differences == 0 && encoding.Ok ? 0 : 1;
    }

    /// <summary>
    /// Deponun tamamlanmis risk isi. Yoksa gercek isleyiciyle yeni bir tane aciliyor;
    /// ekran icin sahte veri uretmek yok.
    /// </summary>
    private static async Task<Guid> CompleteJobAsync(SievertContext context, HttpClient client, Target target)
    {
        Guid? existing = context.AnalysisJobs
            .AsNoTracking()
            .Where(job => job.RepositoryId == target.Id
                && job.Kind == AnalysisJobKind.RiskScoreAll
                && job.Status == AnalysisJobStatus.Succeeded
                && job.IsResultComplete)
            .OrderByDescending(job => job.CompletedAtUtc)
            .Select(job => (Guid?)job.Id)
            .FirstOrDefault();

        if (existing is Guid found)
        {
            return found;
        }

        Console.WriteLine($"  {target.Name}: tamamlanmis is yok, yenisi aciliyor");

        using HttpResponseMessage response = await client.PostAsync(
            $"/api/v1/repositories/{target.Id}/analyses",
            new StringContent("{\"kind\":\"risk-score-all\"}", Encoding.UTF8, "application/json"));

        Guid jobId = JsonDocument.Parse(await response.Content.ReadAsStringAsync())
            .RootElement.GetProperty("id").GetGuid();

        while (true)
        {
            JsonElement job = JsonDocument
                .Parse(await client.GetStringAsync($"/api/v1/analyses/{jobId}"))
                .RootElement;

            if (job.GetProperty("status").GetString() is "succeeded" or "failed" or "canceled")
            {
                return jobId;
            }

            await Task.Delay(500);
        }
    }

    private static int Snapshots(SievertContext context, Guid job) =>
        context.CommitRiskSnapshots.AsNoTracking().Count(row => row.AnalysisJobId == job);

    /// <summary>
    /// Haritadaki dosyalardan orneklem alip ham tablolardan yeniden hesaplar.
    ///
    /// Pencere de burada yeniden belirleniyor: isin en yeni N commit'i, tarih azalan ve
    /// esitlikte kimlik azalan. Uretici kodun sirasini kopyalamiyorum, tanimini
    /// uyguluyorum.
    /// </summary>
    private static async Task<List<FileCheck>> CheckFilesAsync(
        SievertContext context,
        HttpClient client,
        Target target,
        Guid job)
    {
        int window = VisualizationLimits.DefaultCommitWindow;

        FileActivityResponse map = JsonSerializer.Deserialize<FileActivityResponse>(
            await client.GetStringAsync(
                $"/api/v1/repositories/{target.Id}/visualizations/file-activity"
                + $"?analysisJobId={job}&commitWindow={window}&limit={VisualizationLimits.MaximumFileLimit}"),
            new JsonSerializerOptions(JsonSerializerDefaults.Web))!;

        List<int> windowIds = await context.Commits
            .AsNoTracking()
            .Where(commit => context.CommitRiskSnapshots.Any(
                snapshot => snapshot.AnalysisJobId == job && snapshot.CommitId == commit.Id))
            .OrderByDescending(commit => commit.AuthorDateUtc)
            .ThenByDescending(commit => commit.Id)
            .Take(window)
            .Select(commit => commit.Id)
            .ToListAsync();

        Random random = new(Seed + target.Id);

        List<FileActivityItem> sample = [.. map.Items
            .OrderBy(_ => random.Next())
            .Take(FilesPerRepository)];

        List<FileCheck> checks = [];

        foreach (FileActivityItem item in sample)
        {
            // sievert:disable SV004 dosya basina ayri sorgu bilerek: dogrulama her dosyayi tek tek, farkli bir sorguyla kontrol ediyor
            List<Row> rows = await context.CommitFiles
                .AsNoTracking()
                .Where(file => file.Path == item.RelativePath && windowIds.Contains(file.CommitId))
                .Join(
                    context.CommitRiskSnapshots.AsNoTracking().Where(snapshot => snapshot.AnalysisJobId == job),
                    file => file.CommitId,
                    snapshot => snapshot.CommitId,
                    (file, snapshot) => new { file, snapshot })
                .Join(
                    context.Commits.AsNoTracking(),
                    pair => pair.file.CommitId,
                    commit => commit.Id,
                    (pair, commit) => new Row(
                        pair.file.CommitId,
                        pair.file.LinesAdded,
                        pair.file.LinesDeleted,
                        pair.snapshot.RiskIndex,
                        commit.AuthorDateUtc,
                        commit.Sha))
                .ToListAsync();

            List<Row> deduped = Dedupe(rows);

            checks.Add(Compare(target.Name, item, deduped, Latest(deduped)));
        }

        return checks;
    }

    private static FileCheck Compare(string repository, FileActivityItem item, List<Row> rows, Row latest)
    {
        int touch = rows.Count;
        int added = rows.Sum(row => row.LinesAdded);
        int deleted = rows.Sum(row => row.LinesDeleted);
        double mean = Math.Round(rows.Average(row => row.RiskIndex), 1, MidpointRounding.AwayFromZero);
        double max = rows.Max(row => row.RiskIndex);

        List<string> differing = [];

        if (touch != item.TouchCount)
        {
            differing.Add("touchCount");
        }

        if (added != item.TotalLinesAdded)
        {
            differing.Add("totalLinesAdded");
        }

        if (deleted != item.TotalLinesDeleted)
        {
            differing.Add("totalLinesDeleted");
        }

        if (added + deleted != item.TotalChurn)
        {
            differing.Add("totalChurn");
        }

        if (Math.Abs(mean - item.MeanRiskIndex) > 0.0000001)
        {
            differing.Add("meanRiskIndex");
        }

        if (Math.Abs(max - item.MaxRiskIndex) > 0.0000001)
        {
            differing.Add("maxRiskIndex");
        }

        if (Math.Abs(latest.RiskIndex - item.LatestRiskIndex) > 0.0000001)
        {
            differing.Add("latestRiskIndex");
        }

        if (!string.Equals(latest.Sha, item.LatestCommitSha, StringComparison.Ordinal))
        {
            differing.Add("latestCommitSha");
        }

        return new FileCheck(
            repository,
            item.RelativePath,
            touch,
            item.TouchCount,
            Math.Round(mean, 1),
            item.MeanRiskIndex,
            max,
            item.MaxRiskIndex,
            added + deleted,
            item.TotalChurn,
            differing.Count,
            differing);
    }

    /// <summary>
    /// Zaman cizelgesi noktalarini ham tablodan yeniden secip karsilastirir.
    ///
    /// Secim tanimi: en yeni N, sonra kronolojik siraya cevir. Uretici kod da ayni tanimi
    /// uyguluyor ama bu satirlar onun ciktisini degil, veritabanini okuyor.
    /// </summary>
    private static async Task<List<PointCheck>> CheckTimelineAsync(
        SievertContext context,
        HttpClient client,
        Target target,
        Guid job)
    {
        RiskTimelineResponse timeline = JsonSerializer.Deserialize<RiskTimelineResponse>(
            await client.GetStringAsync(
                $"/api/v1/repositories/{target.Id}/visualizations/risk-timeline"
                + $"?analysisJobId={job}&count={TimelinePointsPerRepository}"),
            new JsonSerializerOptions(JsonSerializerDefaults.Web))!;

        var expected = await (
            from snapshot in context.CommitRiskSnapshots.AsNoTracking()
            join commit in context.Commits.AsNoTracking() on snapshot.CommitId equals commit.Id
            join metric in context.CommitMetrics.AsNoTracking() on commit.Id equals metric.CommitId into metrics
            from metric in metrics.DefaultIfEmpty()
            where snapshot.AnalysisJobId == job
            orderby commit.AuthorDateUtc descending, commit.Id descending
            select new
            {
                commit.Sha,
                commit.AuthorDateUtc,
                snapshot.RawModelScore,
                snapshot.RiskIndex,
                snapshot.DecisionAt05,
                snapshot.DecisionAtTrainThreshold,
                commit.IsBugIntroducing,
                commit.LinesAdded,
                commit.LinesDeleted,
                commit.ChangedFiles,
                commit.ChangedCSharpFiles,
                IsFix = metric != null && metric.IsFix,
            })
            .Take(TimelinePointsPerRepository)
            .ToListAsync();

        expected.Reverse();

        List<PointCheck> checks = [];

        for (int index = 0; index < Math.Min(expected.Count, timeline.Points.Count); index++)
        {
            var mine = expected[index];
            RiskTimelinePoint theirs = timeline.Points[index];

            List<string> differing = [];

            if (!string.Equals(mine.Sha, theirs.Sha, StringComparison.Ordinal))
            {
                differing.Add("sha");
            }

            if (theirs.Ordinal != index)
            {
                differing.Add("ordinal");
            }

            if (mine.AuthorDateUtc != theirs.AuthorDateUtc)
            {
                differing.Add("authorDateUtc");
            }

            if (mine.RawModelScore != theirs.RawModelScore)
            {
                differing.Add("rawModelScore");
            }

            if (mine.RiskIndex != theirs.RiskIndex)
            {
                differing.Add("riskIndex");
            }

            if (mine.DecisionAt05 != theirs.DecisionAt05)
            {
                differing.Add("decisionAt05");
            }

            if (mine.DecisionAtTrainThreshold != theirs.DecisionAtTrainThreshold)
            {
                differing.Add("decisionAtTrainThreshold");
            }

            if (mine.IsFix != theirs.IsFix)
            {
                differing.Add("isFix");
            }

            if (mine.IsBugIntroducing != theirs.IsBugIntroducing)
            {
                differing.Add("isBugIntroducing");
            }

            if (mine.LinesAdded + mine.LinesDeleted != theirs.LinesAdded + theirs.LinesDeleted)
            {
                differing.Add("churn");
            }

            if (mine.ChangedFiles != theirs.FilesChanged)
            {
                differing.Add("filesChanged");
            }

            if (mine.ChangedCSharpFiles != theirs.CsFilesChanged)
            {
                differing.Add("csFilesChanged");
            }

            checks.Add(new PointCheck(target.Name, index, theirs.ShortSha, differing.Count, differing));
        }

        // Esik endeksleri: ayni skor referansi, ayni mid-rank tanimi.
        ScoreReference reference = ScoreReference.Load(
            Path.Combine(ApiOptionsRoot(), "data", "asama6", "model-score-reference.json"));

        if (reference.For(timeline.ModelProfile) is ScoreDistribution distribution)
        {
            double at05 = distribution.RiskIndex(0.5);
            double train = distribution.RiskIndex(timeline.Points[0].TrainThreshold);

            List<string> differing = [];

            if (Math.Abs(at05 - timeline.DecisionAt05RiskIndex) > 0.0000001)
            {
                differing.Add("decisionAt05RiskIndex");
            }

            if (Math.Abs(train - timeline.DecisionAtTrainThresholdRiskIndex) > 0.0000001)
            {
                differing.Add("decisionAtTrainThresholdRiskIndex");
            }

            checks.Add(new PointCheck(target.Name, -1, "esikler", differing.Count, differing));
        }

        return checks;
    }

    private static string ApiOptionsRoot() =>
        Sievert.Api.ApiOptions.FindRepositoryRoot(Directory.GetCurrentDirectory())
        ?? Directory.GetCurrentDirectory();

    /// <summary>
    /// Renk ve koordinatin kultura duyarsizligi ve monotonlugu.
    ///
    /// Adim 4'te tam bu kirilmisti: cubuk genisligi virgullu yazildigi icin tarayici
    /// degeri yok sayiyordu. Artik sayilabilir.
    /// </summary>
    private static EncodingCheck CheckEncoding()
    {
        double[] anchors = [0, 25, 50, 75, 100];

        CultureInfo before = CultureInfo.CurrentCulture;
        Dictionary<string, string[]> byCulture = [];

        try
        {
            foreach (string culture in (string[])["tr-TR", "en-US"])
            {
                CultureInfo.CurrentCulture = new CultureInfo(culture);

                byCulture[culture] =
                [
                    .. anchors.Select(value =>
                        VisualizationScale.Fill(value)
                        + "|" + VisualizationScale.Percent(VisualizationScale.Position(value))
                        + "|" + VisualizationScale.Number(VisualizationScale.Y(value))),
                ];
            }
        }
        finally
        {
            CultureInfo.CurrentCulture = before;
        }

        bool sameAcrossCultures = byCulture["tr-TR"].SequenceEqual(byCulture["en-US"], StringComparer.Ordinal);

        int previousRed = -1;
        double previousY = double.MaxValue;
        bool colourMonotone = true;
        bool coordinateMonotone = true;

        for (int step = 0; step <= 1000; step++)
        {
            double value = step / 10.0;
            int red = Convert.ToInt32(VisualizationScale.Fill(value).Substring(1, 2), 16);
            double y = VisualizationScale.Y(value);

            colourMonotone &= red >= previousRed;
            coordinateMonotone &= y <= previousY;

            previousRed = red;
            previousY = y;
        }

        return new EncodingCheck(
            byCulture["tr-TR"],
            sameAcrossCultures,
            colourMonotone,
            coordinateMonotone,
            VisualizationScale.Fill(50) == VisualizationScale.Fill(50),
            sameAcrossCultures && colourMonotone && coordinateMonotone);
    }

    private static object Summarise(List<FileCheck> checks) => new
    {
        karsilastirilan = checks.Count,
        farkli = checks.Count(check => check.Differences > 0),
        enBuyukOrtalamaFarki = checks.Count == 0
            ? 0
            : checks.Max(check => Math.Abs(check.MeanExpected - check.MeanActual)),
        enBuyukDokunusFarki = checks.Count == 0
            ? 0
            : checks.Max(check => Math.Abs(check.TouchExpected - check.TouchActual)),
        enBuyukChurnFarki = checks.Count == 0
            ? 0
            : checks.Max(check => Math.Abs(check.ChurnExpected - check.ChurnActual)),
        farkliAlanlar = checks.SelectMany(check => check.DifferingFields).Distinct().ToList(),
    };

    private static object SummarisePoints(List<PointCheck> checks) => new
    {
        karsilastirilan = checks.Count(check => check.Ordinal >= 0),
        esikKontrolu = checks.Count(check => check.Ordinal < 0),
        farkli = checks.Count(check => check.Differences > 0),
        farkliAlanlar = checks.SelectMany(check => check.DifferingFields).Distinct().ToList(),
    };

    private sealed record Target(int Id, string Name);

    /// <summary>
    /// Commit basina tekillestirme: ayni commit ayni yol icin iki satir yazmissa endeks
    /// bir kez sayilir. Dongunun disinda duruyor ki dongu govdesi kisa kalsin.
    /// </summary>
    private static List<Row> Dedupe(List<Row> rows) =>
    [
        .. rows
            .GroupBy(row => row.CommitId)
            .Select(group => group.First() with
            {
                LinesAdded = group.Sum(row => row.LinesAdded),
                LinesDeleted = group.Sum(row => row.LinesDeleted),
            }),
    ];

    /// <summary>En yeni dokunus; esitlikte commit kimligi bozuyor.</summary>
    private static Row Latest(List<Row> rows) =>
        rows.OrderByDescending(row => row.AuthorDateUtc)
            .ThenByDescending(row => row.CommitId)
            .First();

    private sealed record Row(
        int CommitId,
        int LinesAdded,
        int LinesDeleted,
        double RiskIndex,
        DateTimeOffset AuthorDateUtc,
        string Sha);

    private sealed record JobUsed(string Repository, Guid JobId, int SnapshotCount);

    private sealed record FileCheck(
        string Repository,
        string Path,
        int TouchExpected,
        int TouchActual,
        double MeanExpected,
        double MeanActual,
        double MaxExpected,
        double MaxActual,
        int ChurnExpected,
        int ChurnActual,
        int Differences,
        IReadOnlyList<string> DifferingFields);

    private sealed record PointCheck(
        string Repository,
        int Ordinal,
        string ShortSha,
        int Differences,
        IReadOnlyList<string> DifferingFields);

    private sealed record EncodingCheck(
        IReadOnlyList<string> Anchors,
        bool SameAcrossCultures,
        bool ColourMonotone,
        bool CoordinateMonotone,
        bool Deterministic,
        bool Ok);
}
