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
    public async Task Health_SaysWhatIsWrongWhenTheConnectionStringIsMissing()
    {
        using SievertApiFactory factory = new(connectionString: null);
        using HttpClient client = factory.CreateClient();

        JsonElement body = await Read(client, "/api/v1/health");

        Assert.Equal("degraded", body.GetProperty("status").GetString());
        Assert.False(body.GetProperty("database").GetProperty("reachable").GetBoolean());
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
