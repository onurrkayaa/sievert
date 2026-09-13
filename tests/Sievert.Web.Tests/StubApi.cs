using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

using Microsoft.Extensions.Logging.Abstractions;

using Sievert.Contracts;
using Sievert.Web.Api;

namespace Sievert.Web.Tests;

/// <summary>
/// API'nin yerine gecen sahte istemci.
///
/// Gercek bir sunucu acilmiyor: sinanan sey panelin API'ye ne sordugu degil, gelen
/// cevabi ekranda nasil gosterdigi. Cevaplar burada elle kuruluyor ki "API erisilemiyor"
/// ve "422 bilinmeyen depo" gibi durumlar bir kaza beklemeden uretilebilsin.
/// </summary>
internal sealed class StubApi
{
    private readonly Dictionary<string, (HttpStatusCode Status, string Body)> answers = new(StringComparer.Ordinal);

    private bool offline;

    public List<string> Requests { get; } = [];

    /// <summary>Su ana kadar kac kez is durumu soruldu; polling testleri bunu sayiyor.</summary>
    public int JobRequests => Requests.Count(path => path.Contains("/analyses/", StringComparison.Ordinal));

    public StubApi Returns<T>(string path, T value)
    {
        answers[path] = (HttpStatusCode.OK, JsonSerializer.Serialize(value, new JsonSerializerOptions(JsonSerializerDefaults.Web)));

        return this;
    }

    public StubApi ReturnsProblem(string path, HttpStatusCode status, string errorCode, string detail)
    {
        answers[path] = (status, JsonSerializer.Serialize(new
        {
            title = "Istek karsilanamadi",
            status = (int)status,
            detail,
            errorCode,
            traceId = "0HTEST",
        }));

        return this;
    }

    /// <summary>Bundan sonraki her istek baglanti hatasi verir.</summary>
    public StubApi GoesOffline()
    {
        offline = true;

        return this;
    }

    public SievertApiClient Client() => new(
        new HttpClient(new Handler(this)) { BaseAddress = new Uri("http://api.test") },
        NullLogger<SievertApiClient>.Instance);

    private sealed class Handler(StubApi stub) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            string path = request.RequestUri!.PathAndQuery;

            lock (stub.Requests)
            {
                stub.Requests.Add(path);
            }

            if (stub.offline)
            {
                throw new HttpRequestException("baglanti yok");
            }

            // Sorgu dizesi olmadan da eslesiyor; testler sayfa numarasi yazmak zorunda kalmasin.
            string withoutQuery = path.Split('?')[0];

            if (!stub.answers.TryGetValue(path, out (HttpStatusCode Status, string Body) answer)
                && !stub.answers.TryGetValue(withoutQuery, out answer))
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)
                {
                    Content = new StringContent(
                        """{"title":"Bulunamadi","status":404,"errorCode":"NOT_FOUND","detail":"sahte istemcide tanimsiz"}""",
                        Encoding.UTF8,
                        "application/problem+json"),
                });
            }

            return Task.FromResult(new HttpResponseMessage(answer.Status)
            {
                Content = new StringContent(answer.Body, Encoding.UTF8, "application/json"),
            });
        }
    }
}

/// <summary>Testlerde tekrar tekrar kurulan ornek cevaplar.</summary>
internal static class Samples
{
    public static HealthResponse Health(string status = "ok") => new(
        status,
        "0.1.0",
        new DatabaseHealth(true, true, null),
        "cd3b3e3d7575",
        [new ModelHealth("polly", "NotLoaded", null)],
        new AnalysisHealth(100, 0, 1, 0, 0, DateTimeOffset.UnixEpoch, null),
        new ProcessHealth(1024 * 1024 * 120, 1024 * 1024 * 30, 1, 0, 0, 12.5));

    public static AnalysisJobResponse Job(
        string status = "running",
        string kind = "risk-score-all",
        int processed = 100,
        int resultCount = 100,
        bool complete = false,
        string? errorCode = null) => new(
        Guid.Parse("01a09990-0000-7000-8000-000000000001"),
        2,
        kind,
        status,
        DateTimeOffset.UnixEpoch,
        DateTimeOffset.UnixEpoch,
        status is "running" or "queued" ? null : DateTimeOffset.UnixEpoch.AddSeconds(3),
        status == "running" ? "scoring" : status,
        processed,
        1000,
        processed / 10.0,
        false,
        resultCount,
        complete,
        errorCode,
        errorCode is null ? null : "Is calisirken hata olustu.",
        null,
        kind == "static-scan" ? "1d7b6d97844c1111111111111111111111111111" : null,
        kind == "static-scan" ? "1d7b6d97844c" : null,
        kind == "static-scan" ? "clean" : null,
        kind == "static-scan",
        false,
        [new AnalysisLink("self", "/api/v1/analyses/x")]);

    public static CommitRiskAssessment Risk(double score = 0.1725, double index = 74.3) => new(
        2,
        "github.com/app-vnext/polly",
        "482bdf824fba19c1188655156e512570c3d7f7af",
        "polly",
        "3a4c2ce",
        "0761308193ca",
        score,
        IsCalibrated: false,
        index,
        "polly profilinin egitim bolumundeki 1000 skorun ampirik yuzdeligi",
        DecisionAt05: score >= 0.5,
        DecisionAtTrainThreshold: score >= 0.2381,
        0.2381,
        "SZZ ile isaretlenmis hata getiren commit",
        [new FeatureValue("FilesChanged", 2, -0.386)],
        [
            new FeatureContribution("FilesChanged", 2, -0.386, -1.5044, 0.5807, "up", 1,
                "contribution.up", "Model bu satirda skoru yukari tasidi.", false, null),
            new FeatureContribution("PriorFixes", 0, -0.9424, -0.418, 0.0138, "up", 2,
                "contribution.up", "Model bu satirda skoru yukari tasidi.", false, null),
        ],
        [
            new FeatureContribution("PriorChanges", 7, -1.0558, 0.4094, -0.4322, "down", 1,
                "contribution.down", "Model bu satirda skoru asagi tasidi.", false, null),
        ],
        [.. RiskWarning.Always],
        RiskWarning.Describe(RiskWarning.Always),
        new StaticAnalysisSection("not-run", [], IncludedInModelScore: false),
        new AuditSection(false, null));
}
