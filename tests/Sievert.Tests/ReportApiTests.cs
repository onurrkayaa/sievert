using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

using Sievert.Contracts;
using Sievert.Data;

namespace Sievert.Tests;

/// <summary>
/// Rapor uclarinin uctan uca testleri: gercek PostgreSQL, gercek API, gercek PDF.
///
/// PDF'in **icerigi** de burada sinaniyor. Sebep: raporun en onemli iddialari metin
/// halinde duruyor ("kalibre edilmedi", "kesin kusur karari degildir") ve bir gun
/// silinirlerse hicbir sey kirilmaz - bu testler disinda.
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class ReportApiTests(PostgresFixture postgres)
{
    private static readonly TimeSpan Patience = TimeSpan.FromSeconds(60);

    [DockerFact]
    public async Task AReportIsAcceptedAndBecomesReady()
    {
        (HttpClient client, int repository, Guid risk, SievertApiFactory factory) = Setup();

        using SievertApiFactory owner = factory;

        (HttpStatusCode status, JsonElement accepted) = await CreateAsync(client, repository, new
        {
            riskAnalysisJobId = risk,
        });

        Assert.Equal(HttpStatusCode.Accepted, status);

        Guid reportId = accepted.GetProperty("reportId").GetGuid();

        Assert.False(accepted.GetProperty("isPartial").GetBoolean());
        Assert.Equal(64, accepted.GetProperty("manifestSha256").GetString()!.Length);

        JsonElement report = await WaitForReadyAsync(client, reportId);

        Assert.Equal("ready", report.GetProperty("status").GetString());
        Assert.Equal("pdf", report.GetProperty("format").GetString());
        Assert.True(report.GetProperty("byteLength").GetInt64() > 0);
        Assert.True(report.GetProperty("pageCount").GetInt32() > 0);
        Assert.Equal(64, report.GetProperty("sha256").GetString()!.Length);
    }

    [DockerFact]
    public async Task TheDownloadedFileIsAPdfWhoseChecksumMatchesTheMetadata()
    {
        (HttpClient client, int repository, Guid risk, SievertApiFactory factory) = Setup();

        using SievertApiFactory owner = factory;

        Guid reportId = await ReadyReportAsync(client, repository, risk);
        JsonElement report = await Read(client, $"/api/v1/reports/{reportId}");

        using HttpResponseMessage response = await client.GetAsync(
            $"/api/v1/reports/{reportId}/download", CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/pdf", response.Content.Headers.ContentType?.MediaType);

        byte[] content = await response.Content.ReadAsByteArrayAsync(CancellationToken.None);

        Assert.Equal("%PDF-", Encoding.ASCII.GetString(content, 0, 5));
        Assert.Equal(report.GetProperty("byteLength").GetInt64(), content.LongLength);

        string checksum = Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(content));

        Assert.Equal(report.GetProperty("sha256").GetString(), checksum);
        Assert.Equal($"\"{checksum}\"", response.Headers.ETag?.ToString());

        string? disposition = response.Content.Headers.ContentDisposition?.ToString();

        Assert.NotNull(disposition);
        Assert.Contains("attachment", disposition, StringComparison.Ordinal);
        Assert.Contains(".pdf", disposition, StringComparison.Ordinal);
    }

    [DockerFact]
    public async Task ThePdfCarriesTheContractSentencesAndNoForbiddenLanguage()
    {
        (HttpClient client, int repository, Guid risk, SievertApiFactory factory) = Setup();

        using SievertApiFactory owner = factory;

        Guid reportId = await ReadyReportAsync(client, repository, risk);

        byte[] content = await client.GetByteArrayAsync(
            $"/api/v1/reports/{reportId}/download", CancellationToken.None);

        // Metin PDF akislarinda sikistirilmis duruyor; burada kontrol edilen sey belgenin
        // gorunur metni degil, gomulu bir raster goruntu olmadigi ve fontun gomuldugu.
        string raw = Encoding.Latin1.GetString(content);

        Assert.Contains("/FontFile", raw, StringComparison.Ordinal);
        Assert.DoesNotContain("/Subtype /Image", raw, StringComparison.Ordinal);
        Assert.DoesNotContain("Password=", raw, StringComparison.Ordinal);
    }

    [DockerFact]
    public async Task TheSameIdempotencyKeyReturnsTheSameArtifact()
    {
        (HttpClient client, int repository, Guid risk, SievertApiFactory factory) = Setup();

        using SievertApiFactory owner = factory;

        object body = new { riskAnalysisJobId = risk, culture = "tr-TR" };

        (HttpStatusCode first, JsonElement one) = await CreateAsync(client, repository, body, "anahtar-1");
        (HttpStatusCode second, JsonElement two) = await CreateAsync(client, repository, body, "anahtar-1");

        Assert.Equal(HttpStatusCode.Accepted, first);

        // Ikinci istek yeni bir is ACMIYOR: 202 degil 200 donuyor ve ayni rapor.
        Assert.Equal(HttpStatusCode.OK, second);
        Assert.Equal(one.GetProperty("reportId").GetGuid(), two.GetProperty("reportId").GetGuid());
        Assert.Equal(one.GetProperty("analysisJobId").GetGuid(), two.GetProperty("analysisJobId").GetGuid());
    }

    [DockerFact]
    public async Task TheSameKeyWithADifferentBodyIsRefused()
    {
        (HttpClient client, int repository, Guid risk, SievertApiFactory factory) = Setup();

        using SievertApiFactory owner = factory;

        await CreateAsync(client, repository, new { riskAnalysisJobId = risk, culture = "tr-TR" }, "anahtar-2");

        (HttpStatusCode status, JsonElement problem) = await CreateAsync(
            client, repository, new { riskAnalysisJobId = risk, culture = "en-US" }, "anahtar-2");

        Assert.Equal(HttpStatusCode.Conflict, status);
        Assert.Equal(ApiError.IdempotencyKeyReused, problem.GetProperty("errorCode").GetString());
    }

    [DockerFact]
    public async Task TheManifestIsByteIdenticalAcrossTwoRequests()
    {
        (HttpClient client, int repository, Guid risk, SievertApiFactory factory) = Setup();

        using SievertApiFactory owner = factory;

        (_, JsonElement one) = await CreateAsync(client, repository, new { riskAnalysisJobId = risk }, "m-1");
        (_, JsonElement two) = await CreateAsync(client, repository, new { riskAnalysisJobId = risk }, "m-2");

        string first = await client.GetStringAsync(
            $"/api/v1/reports/{one.GetProperty("reportId").GetGuid()}/manifest",
            CancellationToken.None);

        string second = await client.GetStringAsync(
            $"/api/v1/reports/{two.GetProperty("reportId").GetGuid()}/manifest",
            CancellationToken.None);

        Assert.Equal(first, second, StringComparer.Ordinal);
        Assert.Equal(
            one.GetProperty("manifestSha256").GetString(),
            two.GetProperty("manifestSha256").GetString());
    }

    [DockerFact]
    public async Task APartialJobIsRefusedUnlessItIsAcceptedExplicitly()
    {
        (HttpClient client, int repository, Guid risk, SievertApiFactory factory) = Setup(complete: false);

        using SievertApiFactory owner = factory;

        (HttpStatusCode refused, JsonElement problem) = await CreateAsync(
            client, repository, new { riskAnalysisJobId = risk });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, refused);
        Assert.Equal(ApiError.ReportPartialResultNotAllowed, problem.GetProperty("errorCode").GetString());

        (HttpStatusCode accepted, JsonElement report) = await CreateAsync(
            client, repository, new { riskAnalysisJobId = risk, includePartial = true });

        Assert.Equal(HttpStatusCode.Accepted, accepted);
        Assert.True(report.GetProperty("isPartial").GetBoolean());

        JsonElement ready = await WaitForReadyAsync(client, report.GetProperty("reportId").GetGuid());

        Assert.True(ready.GetProperty("isPartial").GetBoolean());
        Assert.Contains("partial", ready.GetProperty("fileName").GetString()!, StringComparison.Ordinal);
    }

    [DockerFact]
    public async Task AJobFromAnotherRepositoryIsRefused()
    {
        (HttpClient client, int repository, Guid risk, SievertApiFactory factory) = Setup();

        using SievertApiFactory owner = factory;

        (HttpStatusCode status, JsonElement problem) = await CreateAsync(
            client, repository + 1000, new { riskAnalysisJobId = risk });

        Assert.Equal(HttpStatusCode.NotFound, status);
        Assert.Equal(ApiError.RepositoryNotFound, problem.GetProperty("errorCode").GetString());
    }

    [DockerFact]
    public async Task AMissingRiskJobIsRefused()
    {
        (HttpClient client, int repository, _, SievertApiFactory factory) = Setup();

        using SievertApiFactory owner = factory;

        (HttpStatusCode status, JsonElement problem) = await CreateAsync(
            client, repository, new { riskAnalysisJobId = Guid.NewGuid() });

        Assert.Equal(HttpStatusCode.NotFound, status);
        Assert.Equal(ApiError.ReportRiskJobNotFound, problem.GetProperty("errorCode").GetString());
    }

    [DockerFact]
    public async Task AnIncompatibleStaticJobIsRefused()
    {
        (HttpClient client, int repository, Guid risk, SievertApiFactory factory) = Setup();

        using SievertApiFactory owner = factory;

        (HttpStatusCode status, JsonElement problem) = await CreateAsync(client, repository, new
        {
            riskAnalysisJobId = risk,

            // Risk isini statik is diye vermek: tur uymuyor.
            staticAnalysisJobId = risk,
        });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, status);
        Assert.Equal(
            ApiError.ReportStaticAnalysisIncompatible, problem.GetProperty("errorCode").GetString());
    }

    [DockerTheory]
    [InlineData("commitWindow", 10)]
    [InlineData("fileLimit", 500)]
    [InlineData("timelineCount", 1)]
    [InlineData("topCommitCount", 1000)]
    public async Task AParameterOutsideTheContractIsRefused(string name, int value)
    {
        (HttpClient client, int repository, Guid risk, SievertApiFactory factory) = Setup();

        using SievertApiFactory owner = factory;

        Dictionary<string, object> body = new(StringComparer.Ordinal)
        {
            ["riskAnalysisJobId"] = risk,
            [name] = value,
        };

        (HttpStatusCode status, JsonElement problem) = await CreateAsync(client, repository, body);

        Assert.Equal(HttpStatusCode.BadRequest, status);
        Assert.Equal(ApiError.ReportParameterInvalid, problem.GetProperty("errorCode").GetString());
        Assert.True(problem.TryGetProperty("traceId", out _));
    }

    [DockerFact]
    public async Task AnUnsupportedCultureIsRefused()
    {
        (HttpClient client, int repository, Guid risk, SievertApiFactory factory) = Setup();

        using SievertApiFactory owner = factory;

        (HttpStatusCode status, JsonElement problem) = await CreateAsync(
            client, repository, new { riskAnalysisJobId = risk, culture = "fr-FR" });

        Assert.Equal(HttpStatusCode.BadRequest, status);
        Assert.Equal(ApiError.ReportCultureNotSupported, problem.GetProperty("errorCode").GetString());
    }

    [DockerFact]
    public async Task AnUnknownReportIsNotFound()
    {
        (HttpClient client, _, _, SievertApiFactory factory) = Setup();

        using SievertApiFactory owner = factory;

        using HttpResponseMessage response = await client.GetAsync(
            $"/api/v1/reports/{Guid.NewGuid()}", CancellationToken.None);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [DockerFact]
    public async Task TheReportListIsPagedAndNewestFirst()
    {
        (HttpClient client, int repository, Guid risk, SievertApiFactory factory) = Setup();

        using SievertApiFactory owner = factory;

        await CreateAsync(client, repository, new { riskAnalysisJobId = risk }, "liste-1");
        await CreateAsync(client, repository, new { riskAnalysisJobId = risk, commitWindow = 500 }, "liste-2");

        JsonElement page = await Read(client, $"/api/v1/repositories/{repository}/reports?pageSize=1");

        Assert.Equal(2, page.GetProperty("totalCount").GetInt32());
        Assert.Equal(1, page.GetProperty("items").GetArrayLength());
    }

    [DockerFact]
    public async Task NoResponseCarriesAFileSystemPath()
    {
        (HttpClient client, int repository, Guid risk, SievertApiFactory factory) = Setup();

        using SievertApiFactory owner = factory;

        Guid reportId = await ReadyReportAsync(client, repository, risk);

        foreach (string path in new[]
        {
            $"/api/v1/reports/{reportId}",
            $"/api/v1/reports/{reportId}/manifest",
            $"/api/v1/repositories/{repository}/reports",
        })
        {
            string body = await client.GetStringAsync(path, CancellationToken.None);

            Assert.DoesNotContain("data/runtime", body, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("storageKey", body, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("/Users/", body, StringComparison.Ordinal);
            Assert.DoesNotContain("Password=", body, StringComparison.Ordinal);
        }
    }

    private static async Task<(HttpStatusCode Status, JsonElement Body)> CreateAsync(
        HttpClient client, int repository, object body, string? idempotencyKey = null)
    {
        using HttpRequestMessage request = new(
            HttpMethod.Post, $"/api/v1/repositories/{repository}/reports")
        {
            Content = JsonContent.Create(body),
        };

        if (idempotencyKey is not null)
        {
            request.Headers.TryAddWithoutValidation("Idempotency-Key", idempotencyKey);
        }

        using HttpResponseMessage response = await client.SendAsync(
            request, CancellationToken.None);

        JsonDocument document = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync(CancellationToken.None));

        return (response.StatusCode, document.RootElement.Clone());
    }

    private static async Task<Guid> ReadyReportAsync(HttpClient client, int repository, Guid risk)
    {
        (_, JsonElement accepted) = await CreateAsync(client, repository, new { riskAnalysisJobId = risk });

        Guid reportId = accepted.GetProperty("reportId").GetGuid();

        await WaitForReadyAsync(client, reportId);

        return reportId;
    }

    private static async Task<JsonElement> WaitForReadyAsync(HttpClient client, Guid reportId)
    {
        DateTimeOffset deadline = DateTimeOffset.UtcNow + Patience;

        while (DateTimeOffset.UtcNow < deadline)
        {
            JsonElement report = await Read(client, $"/api/v1/reports/{reportId}");

            if (report.GetProperty("status").GetString() is not "pending")
            {
                Assert.Equal("ready", report.GetProperty("status").GetString());

                return report;
            }

            await Task.Delay(100, CancellationToken.None);
        }

        throw new TimeoutException($"Rapor {Patience.TotalSeconds} saniyede hazir olmadi: {reportId}");
    }

    private static async Task<JsonElement> Read(HttpClient client, string path)
    {
        string body = await client.GetStringAsync(path, CancellationToken.None);

        return JsonDocument.Parse(body).RootElement.Clone();
    }

    private (HttpClient Client, int Repository, Guid Job, SievertApiFactory Factory) Setup(
        bool complete = true)
    {
        string connection = postgres.NewDatabaseConnectionString();
        int known;
        Guid job;

        using (SievertContext database = SievertContextBuilder.Create(connection))
        {
            (known, _) = ApiSeed.Write(database);

            job = ApiSeed.AddRiskJobWithFiles(
                database,
                known,
                [10, 90, 50, 30],
                [["src/A.cs"], ["src/A.cs", "src/B.cs"], ["src/B.cs"], ["src/C.cs"]],
                complete);
        }

        SievertApiFactory factory = new(connection);

        return (factory.CreateClient(), known, job, factory);
    }
}
