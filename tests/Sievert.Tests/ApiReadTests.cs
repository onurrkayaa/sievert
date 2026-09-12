using System.Net;
using System.Text.Json;

using Sievert.Data;

namespace Sievert.Tests;

/// <summary>
/// Salt-okunur uclarin testleri. Gercek bir PostgreSQL ve gercek model dosyalari
/// kullaniliyor; sahte servis yok, cunku sinanmak istenen sey tam olarak bu ikisiyle
/// birlikte dogru davranip davranmadigi.
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class ApiReadTests(PostgresFixture postgres)
{
    [DockerFact]
    public async Task Health_ReportsTheDatabaseAndEveryModelProfile()
    {
        using SievertApiFactory factory = new(postgres.NewDatabaseConnectionString());
        using HttpClient client = factory.CreateClient();

        JsonElement body = await Read(client, "/api/v1/health");

        Assert.Equal("ok", body.GetProperty("status").GetString());
        Assert.True(body.GetProperty("database").GetProperty("reachable").GetBoolean());
        Assert.True(body.GetProperty("database").GetProperty("migrationsApplied").GetBoolean());

        List<string> codes = [.. body.GetProperty("models").EnumerateArray()
            .Select(model => model.GetProperty("code").GetString()!)];

        Assert.Equal(["jellyfin", "polly", "sharex"], codes);

        foreach (JsonElement model in body.GetProperty("models").EnumerateArray())
        {
            Assert.NotEqual("ChecksumMismatch", model.GetProperty("status").GetString());
        }
    }

    [DockerFact]
    public async Task NoErrorResponseLeaksTheServerLayout()
    {
        using SievertApiFactory factory = new(connectionString: null);
        using HttpClient client = factory.CreateClient();

        // Baglanti dizesi yokken veritabani isteyen her uc 503 doniyor; metinde
        // mutlak yol, sunucu adi ya da parola olmamali.
        foreach (string path in (string[])["/api/v1/health", "/api/v1/repositories"])
        {
            string body = await (await client.GetAsync(path)).Content.ReadAsStringAsync();

            Assert.DoesNotContain(ProjectRoot.Path, body, StringComparison.Ordinal);
            Assert.DoesNotContain("Password=sievert", body, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("/Users/", body, StringComparison.Ordinal);
        }
    }

    [DockerFact]
    public async Task Health_SaysWhatIsWrongWhenTheConnectionStringIsMissing()
    {
        using SievertApiFactory factory = new(connectionString: null);
        using HttpClient client = factory.CreateClient();

        JsonElement body = await Read(client, "/api/v1/health");

        Assert.Equal("degraded", body.GetProperty("status").GetString());
        Assert.False(body.GetProperty("database").GetProperty("reachable").GetBoolean());
    }

    [DockerFact]
    public async Task Repositories_AreListedWithTheirModelAvailability()
    {
        string connection = postgres.NewDatabaseConnectionString();
        using SievertContext database = SievertContextBuilder.Create(connection);
        ApiSeed.Write(database);

        using SievertApiFactory factory = new(connection);
        using HttpClient client = factory.CreateClient();

        JsonElement body = await Read(client, "/api/v1/repositories");

        Assert.Equal(2, body.GetProperty("totalCount").GetInt32());

        Dictionary<string, bool> available = body.GetProperty("items").EnumerateArray()
            .ToDictionary(
                item => item.GetProperty("repositoryIdentity").GetString()!,
                item => item.GetProperty("modelProfileAvailable").GetBoolean());

        Assert.True(available[ApiSeed.KnownIdentity]);
        Assert.False(available[ApiSeed.UnknownIdentity]);
    }

    [DockerFact]
    public async Task RepositoryDetail_CountsLabelsAndNamesTheProfile()
    {
        string connection = postgres.NewDatabaseConnectionString();
        using SievertContext database = SievertContextBuilder.Create(connection);
        (int known, int unknown) = ApiSeed.Write(database);

        using SievertApiFactory factory = new(connection);
        using HttpClient client = factory.CreateClient();

        JsonElement body = await Read(client, $"/api/v1/repositories/{known}");

        Assert.Equal(5, body.GetProperty("commitCount").GetInt32());
        Assert.Equal(1, body.GetProperty("bugIntroducingCount").GetInt32());
        Assert.Equal(1, body.GetProperty("botCount").GetInt32());
        Assert.Equal(5, body.GetProperty("commitsWithMetrics").GetInt32());
        Assert.Equal("polly", body.GetProperty("modelProfile").GetString());

        JsonElement other = await Read(client, $"/api/v1/repositories/{unknown}");

        Assert.Equal(JsonValueKind.Null, other.GetProperty("modelProfile").ValueKind);
        Assert.NotEmpty(other.GetProperty("coverageNotes").EnumerateArray());
    }

    [DockerFact]
    public async Task AMissingRepository_Returns404WithAnErrorCode()
    {
        using SievertApiFactory factory = new(postgres.NewDatabaseConnectionString());
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client.GetAsync("/api/v1/repositories/9999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        JsonElement body = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;

        Assert.Equal("REPOSITORY_NOT_FOUND", body.GetProperty("errorCode").GetString());
        Assert.False(string.IsNullOrWhiteSpace(body.GetProperty("traceId").GetString()));
    }

    [DockerFact]
    public async Task Commits_ComeBackNewestFirstAndPaged()
    {
        string connection = postgres.NewDatabaseConnectionString();
        using SievertContext database = SievertContextBuilder.Create(connection);
        (int known, int _) = ApiSeed.Write(database);

        using SievertApiFactory factory = new(connection);
        using HttpClient client = factory.CreateClient();

        JsonElement body = await Read(client, $"/api/v1/repositories/{known}/commits?pageSize=2");

        Assert.Equal(5, body.GetProperty("totalCount").GetInt32());
        Assert.Equal(2, body.GetProperty("pageSize").GetInt32());

        List<string> shas = [.. body.GetProperty("items").EnumerateArray()
            .Select(item => item.GetProperty("sha").GetString()!)];

        Assert.Equal([ApiSeed.ShaFor(4), ApiSeed.ShaFor(3)], shas);
    }

    [DockerFact]
    public async Task Paging_RefusesOutOfRangeValuesInsteadOfClampingThem()
    {
        using SievertApiFactory factory = new(postgres.NewDatabaseConnectionString());
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client.GetAsync("/api/v1/repositories?pageSize=5000");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        JsonElement body = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;

        Assert.Equal("INVALID_PAGINATION", body.GetProperty("errorCode").GetString());
    }

    [DockerFact]
    public async Task Models_DescribeThemselvesAsUncalibrated()
    {
        using SievertApiFactory factory = new(postgres.NewDatabaseConnectionString());
        using HttpClient client = factory.CreateClient();

        JsonElement body = await Read(client, "/api/v1/models");

        Assert.Equal(3, body.GetArrayLength());

        foreach (JsonElement model in body.EnumerateArray())
        {
            Assert.False(model.GetProperty("isCalibrated").GetBoolean());
            Assert.NotEmpty(model.GetProperty("limitations").EnumerateArray());
            Assert.Equal(15, model.GetProperty("featureCount").GetInt32());
        }
    }

    [DockerFact]
    public async Task NoResponseCarriesAProbabilityField()
    {
        string connection = postgres.NewDatabaseConnectionString();
        using SievertContext database = SievertContextBuilder.Create(connection);
        (int known, int _) = ApiSeed.Write(database);

        using SievertApiFactory factory = new(connection);
        using HttpClient client = factory.CreateClient();

        string[] paths =
        [
            "/api/v1/health",
            "/api/v1/models",
            "/api/v1/repositories",
            $"/api/v1/repositories/{known}",
            $"/api/v1/repositories/{known}/commits",
        ];

        foreach (string path in paths)
        {
            string body = await client.GetStringAsync(path);

            Assert.DoesNotContain("probability", body, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("hata olasilig", body, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("kalibre edilmis olasilik", body, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static async Task<JsonElement> Read(HttpClient client, string path)
    {
        using HttpResponseMessage response = await client.GetAsync(path);
        string body = await response.Content.ReadAsStringAsync();

        Assert.True(
            response.IsSuccessStatusCode,
            $"{path} -> {(int)response.StatusCode}: {body}");

        return JsonDocument.Parse(body).RootElement.Clone();
    }
}
