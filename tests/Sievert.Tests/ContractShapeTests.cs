using System.Reflection;
using System.Text.Json;

using Sievert.Contracts;

namespace Sievert.Tests;

/// <summary>
/// Sozlesme projesinin sekli.
///
/// Panel bu tipleri okuyor ve API'ye proje referansi vermiyor. O yuzden burada sinanan
/// sey "derleniyor mu" degil: alan adlarinin kablo uzerinde nasil gorundugu, hangi
/// alanlarin hic bulunmadigi ve sozlesme projesinin neye baglandigi.
/// </summary>
public sealed class ContractShapeTests
{
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Sozlesme projesi ASP.NET, EF Core ya da ML.NET'e baglanmamali. Baglanirsa panel
    /// dolayli olarak veritabanina ve modele baglanir ve "panel kendi hesabini yapmiyor"
    /// cumlesini yalnizca iyi niyet korur.
    /// </summary>
    [Fact]
    public void TheContractProjectDependsOnNothingButTheBaseLibrary()
    {
        string[] forbidden =
        [
            "Microsoft.AspNetCore",
            "Microsoft.EntityFrameworkCore",
            "Microsoft.ML",
            "Npgsql",
            "LibGit2Sharp",
            "Sievert.Data",
            "Sievert.Api",
            "Sievert.Modeling",
            "Sievert.Analysis",
            "Sievert.Mining",
        ];

        foreach (AssemblyName reference in typeof(ApiError).Assembly.GetReferencedAssemblies())
        {
            Assert.DoesNotContain(reference.Name, forbidden);
        }
    }

    [Fact]
    public void FieldNamesGoOnTheWireInCamelCase()
    {
        AnalysisJobResponse job = new(
            Guid.Empty,
            7,
            "static-scan",
            "succeeded",
            DateTimeOffset.UnixEpoch,
            null,
            null,
            "succeeded",
            10,
            10,
            100,
            false,
            3,
            true,
            null,
            null,
            null,
            "abc",
            "abc",
            "clean",
            true,
            false,
            []);

        string json = JsonSerializer.Serialize(job, Web);

        Assert.Contains("\"repositoryId\":", json, StringComparison.Ordinal);
        Assert.Contains("\"isResultComplete\":", json, StringComparison.Ordinal);
        Assert.Contains("\"sourceHeadSha\":", json, StringComparison.Ordinal);
        Assert.DoesNotContain("\"RepositoryId\":", json, StringComparison.Ordinal);
    }

    /// <summary>Durum ve tur kablo uzerinde metin; sayi olsaydi istemci 3'un ne oldugunu bilemezdi.</summary>
    [Fact]
    public void StatusAndKindAreTextNotNumbers()
    {
        foreach (PropertyInfo property in typeof(AnalysisJobResponse).GetProperties())
        {
            if (property.Name is "Status" or "Kind" or "CurrentPhase" or "ErrorCode" or "SourceTreeState")
            {
                Assert.Equal(typeof(string), Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType);
            }
        }
    }

    /// <summary>Tarihler ISO-8601 ve UTC. Yerel saat yazilsa iki makine iki farkli sey gosterirdi.</summary>
    [Fact]
    public void DatesAreWrittenAsIsoUtc()
    {
        AnalysisLink link = new("self", "/api/v1/analyses/x");
        RecoveryHealth recovery = new(new DateTimeOffset(2026, 9, 13, 7, 30, 0, TimeSpan.Zero), 1, 2, 3);

        Assert.Contains("2026-09-13T07:30:00+00:00", JsonSerializer.Serialize(recovery, Web), StringComparison.Ordinal);
        Assert.Contains("\"rel\":\"self\"", JsonSerializer.Serialize(link, Web), StringComparison.Ordinal);
    }

    /// <summary>
    /// Sozlesmede hicbir yerde dosya yolu, baglanti dizesi ya da commit yazarinin
    /// e-postasi yok. Tip duzeyinde sinaniyor: bir gun biri ekleyecek olursa test duser.
    /// </summary>
    [Theory]
    [InlineData(typeof(AnalysisJobResponse))]
    [InlineData(typeof(AnalysisResultPage<StaticFindingResponse>))]
    [InlineData(typeof(StaticFindingResponse))]
    [InlineData(typeof(CommitRiskSnapshotResponse))]
    [InlineData(typeof(RepositoryListItem))]
    [InlineData(typeof(RepositoryDetail))]
    [InlineData(typeof(CommitListItem))]
    [InlineData(typeof(CommitRiskAssessment))]
    [InlineData(typeof(HealthResponse))]
    [InlineData(typeof(ProcessHealth))]
    [InlineData(typeof(ModelResponse))]
    public void NoContractTypeCarriesAPathAPasswordOrAnAuthor(Type type)
    {
        string[] forbidden =
        [
            "localpath",
            "fullpath",
            "absolutepath",
            "connectionstring",
            "password",
            "authoremail",
            "authorname",
            "probability",
            "combinedrisk",
        ];

        foreach (PropertyInfo property in type.GetProperties())
        {
            string name = property.Name.ToLowerInvariant();

            Assert.DoesNotContain(name, forbidden);
        }
    }

    /// <summary>
    /// Bilinmeyen alanlar cevabi kirmiyor. API'ye yeni bir alan eklendiginde panelin
    /// eski surumu calismaya devam etmeli.
    /// </summary>
    [Fact]
    public void AnUnknownFieldDoesNotBreakDeserialisation()
    {
        const string json = """
            {
              "code": "polly",
              "displayName": "Polly",
              "repositoryIdentity": "github.com/app-vnext/polly",
              "trainer": "LbfgsLogisticRegression",
              "mlPackage": "Microsoft.ML 5.0.0",
              "featureCount": 15,
              "featureSchemaVersion": "1",
              "trainThreshold": 0.2381,
              "isCalibrated": false,
              "modelCodeCommit": "abc",
              "modelChecksum": "def",
              "limitations": [],
              "gelecektekiAlan": 42
            }
            """;

        ModelResponse? model = JsonSerializer.Deserialize<ModelResponse>(json, Web);

        Assert.NotNull(model);
        Assert.Equal("polly", model.Code);
        Assert.False(model.IsCalibrated);
    }

    /// <summary>Hata cevabinda kod ve iz kimligi her zaman okunabiliyor; ek alanlar kayboluyor degil.</summary>
    [Fact]
    public void AProblemResponseKeepsItsCodeTraceAndExtensions()
    {
        const string json = """
            {
              "type": "https://sievert.invalid/errors/analysis-already-active",
              "title": "Zaten aktif bir is var",
              "status": 409,
              "detail": "Bu depo ve is turu icin running durumda bir is var.",
              "errorCode": "ANALYSIS_ALREADY_ACTIVE",
              "traceId": "0HN7",
              "activeJobUrl": "/api/v1/analyses/7"
            }
            """;

        ProblemResponse? problem = JsonSerializer.Deserialize<ProblemResponse>(json, Web);

        Assert.NotNull(problem);
        Assert.Equal("ANALYSIS_ALREADY_ACTIVE", problem.ErrorCode);
        Assert.Equal("0HN7", problem.TraceId);
        Assert.Equal(409, problem.Status);
        Assert.Equal("/api/v1/analyses/7", problem.Extension("activeJobUrl"));
        Assert.Null(problem.Extension("boyleBirAlanYok"));
    }
}
