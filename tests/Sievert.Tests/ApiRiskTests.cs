using System.Net;
using System.Text.Json;

using Sievert.Data;

namespace Sievert.Tests;

/// <summary>
/// Commit risk ucunun testleri. Sozlesme: docs/urun/risk-sozlesmesi.md surum 1.0.
///
/// Testlerin cogu sayinin dogrulugunu degil, sayinin NE OLDUGUNUN dogru anlatildigini
/// siniyor: zorunlu uyarilar, kalibre olmadigi bilgisi, statik analizin skora girmedigi.
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class ApiRiskTests(PostgresFixture postgres)
{
    [DockerFact]
    public async Task AKnownRepositoryCommit_GetsAScoreTwoDecisionsAndAnIndex()
    {
        (SievertApiFactory factory, int known, int _) = Setup();
        using SievertApiFactory owner = factory;
        using HttpClient client = factory.CreateClient();

        JsonElement body = await Read(client, $"/api/v1/repositories/{known}/commits/{ApiSeed.ShaFor(0)}/risk");

        Assert.Equal("polly", body.GetProperty("modelProfile").GetString());
        Assert.False(body.GetProperty("isCalibrated").GetBoolean());
        Assert.InRange(body.GetProperty("rawModelScore").GetDouble(), 0.0, 1.0);
        Assert.InRange(body.GetProperty("riskIndex").GetDouble(), 0.0, 100.0);

        // Iki karar ayri alan ve esik ayrica duruyor.
        Assert.True(body.TryGetProperty("decisionAt05", out _));
        Assert.True(body.TryGetProperty("decisionAtTrainThreshold", out _));
        Assert.Equal(0.23811133205890656, body.GetProperty("trainThreshold").GetDouble());

        Assert.Equal(15, body.GetProperty("featureValues").GetArrayLength());
    }

    [DockerFact]
    public async Task EveryAssessmentCarriesTheMandatoryWarnings()
    {
        (SievertApiFactory factory, int known, int _) = Setup();
        using SievertApiFactory owner = factory;
        using HttpClient client = factory.CreateClient();

        foreach (int index in (int[])[0, 1, 2, 3, 4])
        {
            JsonElement body = await Read(
                client,
                $"/api/v1/repositories/{known}/commits/{ApiSeed.ShaFor(index)}/risk");

            List<string> warnings = [.. body.GetProperty("warnings").EnumerateArray()
                .Select(warning => warning.GetString()!)];

            Assert.Contains("UNCALIBRATED_SCORE", warnings);
            Assert.Contains("SZZ_TARGET", warnings);
            Assert.Contains("STATIC_ANALYSIS_NOT_INCLUDED", warnings);
            Assert.Contains("HUMAN_VALIDATION_LIMITED", warnings);

            // Her uyarinin bir aciklamasi var; kod tek basina anlamsiz kalmiyor.
            Assert.Equal(warnings.Count, body.GetProperty("limitations").GetArrayLength());
        }
    }

    [DockerFact]
    public async Task ACommitWithoutCSharpFiles_GetsTheCoverageWarning()
    {
        (SievertApiFactory factory, int known, int _) = Setup();
        using SievertApiFactory owner = factory;
        using HttpClient client = factory.CreateClient();

        // Tohumda 3 numarali commit'in CsFilesChanged degeri 0.
        JsonElement body = await Read(client, $"/api/v1/repositories/{known}/commits/{ApiSeed.ShaFor(3)}/risk");

        Assert.Contains(
            "CS_LABEL_COVERAGE_LIMIT",
            body.GetProperty("warnings").EnumerateArray().Select(warning => warning.GetString()));

        JsonElement other = await Read(client, $"/api/v1/repositories/{known}/commits/{ApiSeed.ShaFor(2)}/risk");

        Assert.DoesNotContain(
            "CS_LABEL_COVERAGE_LIMIT",
            other.GetProperty("warnings").EnumerateArray().Select(warning => warning.GetString()));
    }

    [DockerFact]
    public async Task StaticFindingsAreSeparateAndDoNotEnterTheScore()
    {
        (SievertApiFactory factory, int known, int _) = Setup();
        using SievertApiFactory owner = factory;
        using HttpClient client = factory.CreateClient();

        JsonElement body = await Read(client, $"/api/v1/repositories/{known}/commits/{ApiSeed.ShaFor(0)}/risk");
        JsonElement section = body.GetProperty("staticAnalysis");

        Assert.Equal("not-run", section.GetProperty("status").GetString());
        Assert.False(section.GetProperty("includedInModelScore").GetBoolean());
        Assert.Empty(section.GetProperty("findings").EnumerateArray());
    }

    [DockerFact]
    public async Task ThereIsNoCombinedScoreField()
    {
        (SievertApiFactory factory, int known, int _) = Setup();
        using SievertApiFactory owner = factory;
        using HttpClient client = factory.CreateClient();

        string body = await client.GetStringAsync(
            $"/api/v1/repositories/{known}/commits/{ApiSeed.ShaFor(0)}/risk");

        Assert.DoesNotContain("combinedRisk", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("probability", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("hata olasilig", body, StringComparison.OrdinalIgnoreCase);
    }

    [DockerFact]
    public async Task ContributionsAreCappedAtFivePerDirectionAndNonCausal()
    {
        (SievertApiFactory factory, int known, int _) = Setup();
        using SievertApiFactory owner = factory;
        using HttpClient client = factory.CreateClient();

        JsonElement body = await Read(client, $"/api/v1/repositories/{known}/commits/{ApiSeed.ShaFor(1)}/risk");

        foreach (string side in (string[])["positiveContributions", "negativeContributions"])
        {
            JsonElement list = body.GetProperty(side);

            Assert.True(list.GetArrayLength() <= 5, $"{side} 5'ten fazla oge tasiyor.");

            int rank = 1;

            foreach (JsonElement item in list.EnumerateArray())
            {
                Assert.Equal(rank++, item.GetProperty("rankByAbsoluteContribution").GetInt32());
                Assert.NotEqual(0.0, item.GetProperty("contribution").GetDouble());
                Assert.DoesNotContain("neden", item.GetProperty("explanation").GetString()!, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("yol act", item.GetProperty("explanation").GetString()!, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    [DockerFact]
    public async Task AnUnknownRepository_IsNotScoredWithSomeOtherProfile()
    {
        (SievertApiFactory factory, int _, int unknown) = Setup();
        using SievertApiFactory owner = factory;
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client.GetAsync(
            $"/api/v1/repositories/{unknown}/commits/{ApiSeed.ShaFor(0)}/risk");

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);

        JsonElement body = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;

        Assert.Equal("UNKNOWN_REPOSITORY_MODEL", body.GetProperty("errorCode").GetString());
    }

    /// <summary>
    /// Adim 2'de bu parametre vardi ve kaldirildi (risk sozlesmesi surum 1.1). Sessizce
    /// yok sayilmiyor: istegin sahibi sectigi profille skorlandigini sanardi.
    /// </summary>
    [DockerFact]
    public async Task TheRemovedProfileParameterIsRefusedInsteadOfIgnored()
    {
        (SievertApiFactory factory, int known, int _) = Setup();
        using SievertApiFactory owner = factory;
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client.GetAsync(
            $"/api/v1/repositories/{known}/commits/{ApiSeed.ShaFor(0)}/risk?profile=sharex");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        JsonElement body = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;

        Assert.Equal("PROFILE_SELECTION_NOT_SUPPORTED", body.GetProperty("errorCode").GetString());
    }

    [DockerFact]
    public async Task AKnownRepositoryAlwaysGetsItsOwnProfile()
    {
        (SievertApiFactory factory, int known, int _) = Setup();
        using SievertApiFactory owner = factory;
        using HttpClient client = factory.CreateClient();

        JsonElement body = await Read(client, $"/api/v1/repositories/{known}/commits/{ApiSeed.ShaFor(2)}/risk");

        Assert.Equal("polly", body.GetProperty("modelProfile").GetString());
        Assert.DoesNotContain(
            "EXTERNAL_MODEL_PROFILE",
            body.GetProperty("warnings").EnumerateArray().Select(warning => warning.GetString()));
    }

    [DockerFact]
    public async Task ACommitWithoutMetrics_SaysSoInsteadOfGuessing()
    {
        string connection = postgres.NewDatabaseConnectionString();
        string sha;

        using (SievertContext database = SievertContextBuilder.Create(connection))
        {
            (int known, int _) = ApiSeed.Write(database);
            sha = ApiSeed.AddCommitWithoutMetrics(database, known);

            using SievertApiFactory factory = new(connection);
            using HttpClient client = factory.CreateClient();

            using HttpResponseMessage response = await client.GetAsync(
                $"/api/v1/repositories/{known}/commits/{sha}/risk");

            Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);

            JsonElement body = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;

            Assert.Equal("COMMIT_METRICS_MISSING", body.GetProperty("errorCode").GetString());
        }
    }

    [DockerFact]
    public async Task AMissingCommit_Returns404()
    {
        (SievertApiFactory factory, int known, int _) = Setup();
        using SievertApiFactory owner = factory;
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client.GetAsync(
            $"/api/v1/repositories/{known}/commits/{new string('f', 40)}/risk");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        JsonElement body = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;

        Assert.Equal("COMMIT_NOT_FOUND", body.GetProperty("errorCode").GetString());
    }

    [DockerFact]
    public async Task AMalformedSha_IsRefusedBeforeTouchingTheDatabase()
    {
        (SievertApiFactory factory, int known, int _) = Setup();
        using SievertApiFactory owner = factory;
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client.GetAsync(
            $"/api/v1/repositories/{known}/commits/zzz/risk");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        JsonElement body = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;

        Assert.Equal("INVALID_SHA", body.GetProperty("errorCode").GetString());
    }

    [DockerFact]
    public async Task TheIndexNeverFallsWhileTheScoreRises()
    {
        (SievertApiFactory factory, int known, int _) = Setup();
        using SievertApiFactory owner = factory;
        using HttpClient client = factory.CreateClient();

        List<(double Score, double Index)> points = [];

        foreach (int index in (int[])[0, 1, 2, 3, 4])
        {
            JsonElement body = await Read(
                client,
                $"/api/v1/repositories/{known}/commits/{ApiSeed.ShaFor(index)}/risk");

            points.Add((body.GetProperty("rawModelScore").GetDouble(), body.GetProperty("riskIndex").GetDouble()));
        }

        points.Sort((left, right) => left.Score.CompareTo(right.Score));

        for (int index = 1; index < points.Count; index++)
        {
            Assert.True(
                points[index].Index >= points[index - 1].Index,
                $"skor {points[index].Score} icin endeks dustu.");
        }
    }

    private (SievertApiFactory Factory, int Known, int Unknown) Setup()
    {
        string connection = postgres.NewDatabaseConnectionString();

        using SievertContext database = SievertContextBuilder.Create(connection);
        (int known, int unknown) = ApiSeed.Write(database);

        return (new SievertApiFactory(connection), known, unknown);
    }

    private static async Task<JsonElement> Read(HttpClient client, string path)
    {
        using HttpResponseMessage response = await client.GetAsync(path);
        string body = await response.Content.ReadAsStringAsync();

        Assert.True(response.IsSuccessStatusCode, $"{path} -> {(int)response.StatusCode}: {body}");

        return JsonDocument.Parse(body).RootElement.Clone();
    }
}
