using System.Net;
using System.Text.Json;

using Sievert.Contracts;
using Sievert.Data;
using Sievert.Data.Entities;

namespace Sievert.Tests;

/// <summary>
/// Gorsellestirme uclarinin uctan uca testleri: gercek PostgreSQL, gercek API.
///
/// Toplama mantigi ayrica <see cref="FileActivityBuilderTests"/> icinde sinaniyor;
/// burada sinanan sey sorgunun dogru satirlari getirdigi, dogrulamalarin calistigi ve
/// cevabin sozlesmeye uydugu.
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class VisualizationApiTests(PostgresFixture postgres)
{
    [DockerFact]
    public async Task TheFileMapSummarisesTheCommitsThatTouchedEachFile()
    {
        (HttpClient client, int repository, Guid job, SievertApiFactory factory) = Setup(
            [10, 90, 50],
            [["src/A.cs"], ["src/A.cs", "src/B.cs"], ["src/B.cs"]]);

        using SievertApiFactory owner = factory;

        FileActivityResponse map = await MapAsync(client, repository, job);

        Assert.Equal(RankingScope.CompleteAnalysis, map.RankingScope);
        Assert.True(map.IsResultComplete);
        Assert.False(map.IsPartial);
        Assert.Equal(3, map.ConsideredCommitCount);
        Assert.Equal("polly", map.ModelProfile);
        Assert.False(map.IsCalibrated);

        FileActivityItem a = map.Items.First(item => item.RelativePath == "src/A.cs");
        FileActivityItem b = map.Items.First(item => item.RelativePath == "src/B.cs");

        Assert.Equal(2, a.TouchCount);
        Assert.Equal(50, a.MeanRiskIndex);
        Assert.Equal(90, a.MaxRiskIndex);
        Assert.Equal(90, a.LatestRiskIndex);

        Assert.Equal(2, b.TouchCount);
        Assert.Equal(70, b.MeanRiskIndex);

        // Varsayilan siralama ortalama endekse gore; B (70) once, A (50) sonra.
        Assert.Equal("src/B.cs", map.Items[0].RelativePath);
    }

    [DockerFact]
    public async Task TheWindowKeepsOnlyTheNewestCommits()
    {
        (HttpClient client, int repository, Guid job, SievertApiFactory factory) = Setup(
            [100, 0, 0],
            [["src/Old.cs"], ["src/New.cs"], ["src/New.cs"]]);

        using SievertApiFactory owner = factory;

        // Pencere 10 en kucuk gecerli deger ve uc commit'ten daha buyuk; hepsi giriyor.
        FileActivityResponse all = await MapAsync(client, repository, job, "&commitWindow=10");

        Assert.Equal(3, all.ConsideredCommitCount);
        Assert.Contains(VisualizationWarning.WindowLargerThanResults, all.Warnings);
        Assert.Equal(2, all.Items.Count);
    }

    [DockerFact]
    public async Task OnlyCsharpFilesAreOnTheMap()
    {
        (HttpClient client, int repository, Guid job, SievertApiFactory factory) = Setup(
            [50],
            [["src/A.cs", "README.md", "src/style.css"]]);

        using SievertApiFactory owner = factory;

        FileActivityResponse map = await MapAsync(client, repository, job);

        Assert.Equal("src/A.cs", Assert.Single(map.Items).RelativePath);
    }

    [DockerFact]
    public async Task ThePartialJobSaysItsRankingIsOnlyAboutWrittenRows()
    {
        (HttpClient client, int repository, Guid job, SievertApiFactory factory) = Setup(
            [80, 20],
            [["src/A.cs"], ["src/B.cs"]],
            complete: false);

        using SievertApiFactory owner = factory;

        FileActivityResponse map = await MapAsync(client, repository, job);

        Assert.False(map.IsResultComplete);
        Assert.True(map.IsPartial);
        Assert.Equal(RankingScope.WrittenResultsOnly, map.RankingScope);
        Assert.Contains(VisualizationWarning.PartialAnalysisResult, map.Warnings);

        RiskTimelineResponse timeline = await TimelineAsync(client, repository, job);

        Assert.True(timeline.IsPartial);
        Assert.Equal(RankingScope.WrittenResultsOnly, timeline.RankingScope);
        Assert.Contains(VisualizationWarning.PartialAnalysisResult, timeline.Warnings);
    }

    [DockerFact]
    public async Task TheTimelineSelectsTheNewestButReturnsThemChronologically()
    {
        (HttpClient client, int repository, Guid job, SievertApiFactory factory) = Setup(
            [10, 20, 30, 40, 50],
            [["a.cs"], ["a.cs"], ["a.cs"], ["a.cs"], ["a.cs"]]);

        using SievertApiFactory owner = factory;

        RiskTimelineResponse timeline = await TimelineAsync(client, repository, job, "&count=10");

        Assert.Equal(5, timeline.ReturnedCount);
        Assert.Equal(10, timeline.RequestedCount);

        // Kronolojik: eski once.
        Assert.Equal([10, 20, 30, 40, 50], timeline.Points.Select(point => point.RiskIndex));
        Assert.Equal([0, 1, 2, 3, 4], timeline.Points.Select(point => point.Ordinal));
        Assert.True(timeline.StartDateUtc < timeline.EndDateUtc);

        // Secim en yeniden: uc nokta istenirse son ucu gelir ama yine kronolojik.
        RiskTimelineResponse last = await TimelineAsync(client, repository, job, "&count=10");
        Assert.Equal(5, last.ReturnedCount);
    }

    [DockerFact]
    public async Task TheTimelineCarriesBothThresholdsAsIndexValues()
    {
        (HttpClient client, int repository, Guid job, SievertApiFactory factory) = Setup(
            [10, 90],
            [["a.cs"], ["a.cs"]]);

        using SievertApiFactory owner = factory;

        RiskTimelineResponse timeline = await TimelineAsync(client, repository, job);

        // Iki esik de ayni skor referansindan; 0,5 esigi egitim esiginden yukarida olmali.
        Assert.InRange(timeline.DecisionAt05RiskIndex, 0, 100);
        Assert.InRange(timeline.DecisionAtTrainThresholdRiskIndex, 0, 100);
        Assert.True(timeline.DecisionAt05RiskIndex >= timeline.DecisionAtTrainThresholdRiskIndex);
    }

    [DockerFact]
    public async Task ASingleCommitTimelineDoesNotBreak()
    {
        (HttpClient client, int repository, Guid job, SievertApiFactory factory) = Setup(
            [42], [["a.cs"]]);

        using SievertApiFactory owner = factory;

        RiskTimelineResponse timeline = await TimelineAsync(client, repository, job);

        Assert.Equal(1, timeline.ReturnedCount);
        Assert.Equal(timeline.StartDateUtc, timeline.EndDateUtc);
        Assert.Equal(42, timeline.Points[0].RiskIndex);
    }

    public static TheoryData<string, string, HttpStatusCode> BadRequests()
    {
        TheoryData<string, string, HttpStatusCode> data = [];

        data.Add("", ApiError.VisualizationJobRequired, HttpStatusCode.BadRequest);
        data.Add("&commitWindow=9", ApiError.VisualizationWindowInvalid, HttpStatusCode.BadRequest);
        data.Add("&commitWindow=1001", ApiError.VisualizationWindowInvalid, HttpStatusCode.BadRequest);
        data.Add("&commitWindow=abc", ApiError.VisualizationWindowInvalid, HttpStatusCode.BadRequest);
        data.Add("&limit=9", ApiError.VisualizationLimitInvalid, HttpStatusCode.BadRequest);
        data.Add("&limit=201", ApiError.VisualizationLimitInvalid, HttpStatusCode.BadRequest);
        data.Add("&sort=boyle-bir-sira-yok", ApiError.VisualizationSortInvalid, HttpStatusCode.BadRequest);

        return data;
    }

    [DockerTheory]
    [MemberData(nameof(BadRequests))]
    public async Task InvalidParametersAreRefusedWithTheirOwnCode(
        string extra,
        string code,
        HttpStatusCode status)
    {
        (HttpClient client, int repository, Guid job, SievertApiFactory factory) = Setup(
            [50], [["a.cs"]]);

        using SievertApiFactory owner = factory;

        string query = extra.Length == 0
            ? $"/api/v1/repositories/{repository}/visualizations/file-activity"
            : $"/api/v1/repositories/{repository}/visualizations/file-activity?analysisJobId={job}{extra}";

        using HttpResponseMessage response = await client.GetAsync(query);

        Assert.Equal(status, response.StatusCode);
        Assert.Equal(code, await ErrorCodeAsync(response));
    }

    [DockerFact]
    public async Task AJobFromAnotherRepositoryOrKindIsRefused()
    {
        string connection = postgres.NewDatabaseConnectionString();
        int known;
        int other;
        Guid riskJob;
        Guid scanJob;

        using (SievertContext database = SievertContextBuilder.Create(connection))
        {
            (known, other) = ApiSeed.Write(database);
            riskJob = ApiSeed.AddRiskJobWithFiles(database, known, [50], [["a.cs"]]);

            AnalysisJobStore store = new(database);
            scanJob = (await store.CreateAsync(known, AnalysisJobKind.StaticScan, null, DateTimeOffset.UtcNow))
                .Job!.Id;
        }

        using SievertApiFactory factory = new(connection);
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage wrongRepository = await client.GetAsync(
            $"/api/v1/repositories/{other}/visualizations/file-activity?analysisJobId={riskJob}");

        Assert.Equal(HttpStatusCode.UnprocessableEntity, wrongRepository.StatusCode);
        Assert.Equal(ApiError.VisualizationJobRepositoryMismatch, await ErrorCodeAsync(wrongRepository));

        using HttpResponseMessage wrongKind = await client.GetAsync(
            $"/api/v1/repositories/{known}/visualizations/file-activity?analysisJobId={scanJob}");

        Assert.Equal(HttpStatusCode.UnprocessableEntity, wrongKind.StatusCode);
        Assert.Equal(ApiError.VisualizationJobKindMismatch, await ErrorCodeAsync(wrongKind));

        using HttpResponseMessage missing = await client.GetAsync(
            $"/api/v1/repositories/{known}/visualizations/file-activity?analysisJobId={Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        Assert.Equal(ApiError.VisualizationJobNotFound, await ErrorCodeAsync(missing));
    }

    [DockerFact]
    public async Task AJobWithoutAnySnapshotIsRefused()
    {
        string connection = postgres.NewDatabaseConnectionString();
        int known;
        Guid empty;

        using (SievertContext database = SievertContextBuilder.Create(connection))
        {
            (known, _) = ApiSeed.Write(database);

            AnalysisJobStore store = new(database);
            empty = (await store.CreateAsync(known, AnalysisJobKind.RiskScoreAll, null, DateTimeOffset.UtcNow))
                .Job!.Id;
        }

        using SievertApiFactory factory = new(connection);
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client.GetAsync(
            $"/api/v1/repositories/{known}/visualizations/file-activity?analysisJobId={empty}");

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal(ApiError.VisualizationNoResults, await ErrorCodeAsync(response));
    }

    /// <summary>
    /// Ustuste bindirme yalniz ayni depoya ait, tamamlanmis bir taramadan geliyor.
    ///
    /// Iki static-scan isi kuruluyor: biri kuyrukta kalmis (uygun degil), biri bitmis.
    /// Ikisi de API ayaga kalkmadan once son hallerine getiriliyor - API acilirken
    /// kurtarma calisiyor ve o an `running` olan her isi basarisiz isaretliyor.
    /// </summary>
    [DockerFact]
    public async Task AnIncompatibleStaticOverlayIsRefusedAndACompatibleOneIsCountedSeparately()
    {
        string connection = postgres.NewDatabaseConnectionString();
        int known;
        Guid riskJob;
        Guid queued;
        Guid finished;

        using (SievertContext database = SievertContextBuilder.Create(connection))
        {
            (known, _) = ApiSeed.Write(database);
            riskJob = ApiSeed.AddRiskJobWithFiles(database, known, [50, 90], [["src/A.cs"], ["src/B.cs"]]);

            AnalysisJobStore store = new(database);

            finished = (await store.CreateAsync(known, AnalysisJobKind.StaticScan, "bitmis", DateTimeOffset.UtcNow))
                .Job!.Id;

            await store.TryStartAsync(finished, "test", DateTimeOffset.UtcNow);

            Assert.True(await store.CompleteAsync(
                finished, AnalysisJobStatus.Succeeded, 1, 1, null, null, DateTimeOffset.UtcNow));

            // Ayni repo ve tur icin ikinci is ancak birincisi terminal olduktan sonra acilabiliyor.
            queued = (await store.CreateAsync(known, AnalysisJobKind.StaticScan, "kuyrukta", DateTimeOffset.UtcNow))
                .Job!.Id;

            database.StaticAnalysisFindings.Add(new StaticAnalysisFindingRow
            {
                AnalysisJobId = finished,
                RuleCode = "SV001",
                Severity = "warning",
                RelativePath = "src/A.cs",
                Line = 3,
                Message = "async void",
                Rationale = "gerekce",
                IsTestCode = false,
                IsSuppressed = false,
                CreatedAtUtc = DateTimeOffset.UtcNow,
            });

            database.SaveChanges();
        }

        using SievertApiFactory factory = new(connection);
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage incomplete = await client.GetAsync(
            $"/api/v1/repositories/{known}/visualizations/file-activity"
            + $"?analysisJobId={riskJob}&staticAnalysisJobId={queued}");

        Assert.Equal(HttpStatusCode.UnprocessableEntity, incomplete.StatusCode);
        Assert.Equal(ApiError.StaticAnalysisJobIncompatible, await ErrorCodeAsync(incomplete));

        FileActivityResponse map = await MapAsync(
            client, known, riskJob, $"&staticAnalysisJobId={finished}");

        FileActivityItem a = map.Items.First(item => item.RelativePath == "src/A.cs");
        FileActivityItem b = map.Items.First(item => item.RelativePath == "src/B.cs");

        Assert.Equal(1, a.StaticFindingCount);
        Assert.Equal(0, b.StaticFindingCount);
        Assert.Equal(finished, a.StaticFindingSourceJobId);
        Assert.All(map.Items, item => Assert.False(item.StaticFindingsIncludedInColor));

        // Bulgusu olan dosya bulgu yuzunden one gecmiyor: siralama endekse gore.
        Assert.Equal("src/B.cs", map.Items[0].RelativePath);
    }

    [DockerFact]
    public async Task NoVisualizationResponseLeaksTheServerLayout()
    {
        (HttpClient client, int repository, Guid job, SievertApiFactory factory) = Setup(
            [50], [["src/A.cs"]]);

        using SievertApiFactory owner = factory;

        foreach (string path in (string[])
        [
            $"/api/v1/repositories/{repository}/visualizations/file-activity?analysisJobId={job}",
            $"/api/v1/repositories/{repository}/visualizations/risk-timeline?analysisJobId={job}",
        ])
        {
            string body = await client.GetStringAsync(path);

            Assert.DoesNotContain("localPath", body, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Password", body, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("probability", body, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("combinedRisk", body, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(Path.GetTempPath(), body, StringComparison.Ordinal);
        }
    }

    private static async Task<string?> ErrorCodeAsync(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync())
            .RootElement.GetProperty("errorCode").GetString();

    private static async Task<FileActivityResponse> MapAsync(
        HttpClient client,
        int repository,
        Guid job,
        string extra = "") =>
        JsonSerializer.Deserialize<FileActivityResponse>(
            await client.GetStringAsync(
                $"/api/v1/repositories/{repository}/visualizations/file-activity?analysisJobId={job}{extra}"),
            new JsonSerializerOptions(JsonSerializerDefaults.Web))!;

    private static async Task<RiskTimelineResponse> TimelineAsync(
        HttpClient client,
        int repository,
        Guid job,
        string extra = "") =>
        JsonSerializer.Deserialize<RiskTimelineResponse>(
            await client.GetStringAsync(
                $"/api/v1/repositories/{repository}/visualizations/risk-timeline?analysisJobId={job}{extra}"),
            new JsonSerializerOptions(JsonSerializerDefaults.Web))!;

    private (HttpClient Client, int Repository, Guid Job, SievertApiFactory Factory) Setup(
        double[] scores,
        string[][] files,
        bool complete = true)
    {
        string connection = postgres.NewDatabaseConnectionString();
        int known;
        Guid job;

        using (SievertContext database = SievertContextBuilder.Create(connection))
        {
            (known, _) = ApiSeed.Write(database);
            job = ApiSeed.AddRiskJobWithFiles(database, known, scores, files, complete);
        }

        SievertApiFactory factory = new(connection);

        return (factory.CreateClient(), known, job, factory);
    }
}
