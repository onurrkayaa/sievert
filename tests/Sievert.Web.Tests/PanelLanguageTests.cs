using Bunit;

using Microsoft.Extensions.DependencyInjection;

using Sievert.Contracts;
using Sievert.Web.Components.Pages;

namespace Sievert.Web.Tests;

/// <summary>
/// Panel metinlerinin dili.
///
/// Risk sozlesmesindeki yasak ifadeler API cevaplarinda zaten sinaniyor
/// (<c>RiskContractTests</c>). Burada ayni liste **ekranda gorunen metne** uygulaniyor:
/// panel kendi cumlelerini yaziyor ve API'nin dilini devralmiyor.
///
/// Yasak ifade olumsuzlanarak bile yazilmiyor. Sebep pratik: bu test cumlenin anlamina
/// degil metnin icinde gecip gecmedigine bakiyor, ve "hata olasiligi degildir" cumlesini
/// ekranda yarim okuyan biri de ayni seyi goruyor.
/// </summary>
public sealed class PanelLanguageTests : BunitContext
{
    private static readonly string[] Forbidden =
    [
        "hata olasilig",
        "hata ihtimali",
        "ihtimalle hata",
        "kalibre edilmis olasilik",
        "birlesik skor",
        "combinedRisk",
        "probability",
        "kesin risk",
        "bu commit hatali",
        "model dogru bildi",
    ];

    public PanelLanguageTests()
    {
        Services.AddSingleton(new WebOptions { ApiBaseUrl = new Uri("http://127.0.0.1:5000") });
    }

    [Fact]
    public void TheRiskPageUsesNoneOfTheForbiddenPhrases()
    {
        CommitRiskAssessment risk = Samples.Risk();

        StubApi stub = new StubApi()
            .Returns($"/api/v1/repositories/2/commits/{risk.CommitSha}/risk", risk);

        Services.AddSingleton(stub.Client());

        IRenderedComponent<CommitRisk> page = Render<CommitRisk>(parameters => parameters
            .Add(component => component.RepositoryId, 2)
            .Add(component => component.Sha, risk.CommitSha));

        AssertClean(page.Markup);
    }

    [Fact]
    public void TheOverviewPageUsesNoneOfTheForbiddenPhrases()
    {
        StubApi stub = new StubApi()
            .Returns("/api/v1/health", Samples.Health())
            .Returns("/api/v1/repositories", new PagedResponse<RepositoryListItem>(1, 100, 0, []))
            .Returns("/api/v1/models", new List<ModelResponse>());

        Services.AddSingleton(stub.Client());

        AssertClean(Render<Home>().Markup);
    }

    /// <summary>
    /// Ham skorun yaninda yuzde isareti yok.
    ///
    /// Aranan sey bir sayinin hemen ardindan gelen <c>%</c>. Metnin icindeki "%100"
    /// gibi ilerleme yuzdeleri baska bir sey; onlar skor degil.
    /// </summary>
    [Fact]
    public void NoScoreIsFollowedByAPercentSign()
    {
        CommitRiskAssessment risk = Samples.Risk(0.1725, 74.3);

        StubApi stub = new StubApi()
            .Returns($"/api/v1/repositories/2/commits/{risk.CommitSha}/risk", risk);

        Services.AddSingleton(stub.Client());

        IRenderedComponent<CommitRisk> page = Render<CommitRisk>(parameters => parameters
            .Add(component => component.RepositoryId, 2)
            .Add(component => component.Sha, risk.CommitSha));

        Assert.DoesNotContain("0.1725%", page.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("74.3%", page.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("17.25%", page.Markup, StringComparison.Ordinal);
    }

    private static void AssertClean(string markup)
    {
        foreach (string phrase in Forbidden)
        {
            Assert.DoesNotContain(phrase, markup, StringComparison.OrdinalIgnoreCase);
        }
    }
}
