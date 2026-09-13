using System.Net;

using Bunit;

using Microsoft.Extensions.DependencyInjection;

using Sievert.Contracts;
using Sievert.Web.Api;
using Sievert.Web.Components.Pages;
using Sievert.Web.Components.Shared;

namespace Sievert.Web.Tests;

/// <summary>
/// Panel bilesenlerinin ciktisi.
///
/// Sinanan sey hesaplanan deger degil **ekranda gorunen sey**: yuzde isareti var mi,
/// zorunlu uyarilar duruyor mu, kismi sonuc tam sonuc gibi mi gorunuyor. Bu iddialarin
/// hicbiri sinif duzeyinde sinanamaz.
/// </summary>
public sealed class PanelTests : BunitContext
{
    private FakePageState PageState { get; } = new();

    public PanelTests()
    {
        Services.AddSingleton(new WebOptions { ApiBaseUrl = new Uri("http://127.0.0.1:5000") });
        Services.AddSingleton<IPageState>(PageState);
    }

    [Fact]
    public void TheRiskPageNeverPutsAPercentSignNextToTheRawScore()
    {
        IRenderedComponent<CommitRisk> page = RenderRisk(Samples.Risk(0.1725));
        string markup = page.Markup;

        Assert.Contains("0.1725", markup, StringComparison.Ordinal);

        // Ham skorun yanina yuzde isareti koymak, kalibre edilmemis bir sayiyi olasilik
        // gibi gostermenin en kisa yolu (risk sozlesmesi 1.0).
        Assert.DoesNotContain("0.1725%", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("%17", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void TheRelativeIndexIsExplainedAsAPercentileNotAProbability()
    {
        IRenderedComponent<CommitRisk> page = RenderRisk(Samples.Risk());

        Assert.Contains("Goreli risk endeksi", page.Markup, StringComparison.Ordinal);
        Assert.Contains("gercek bir olasilik degildir", page.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void TheUncalibratedBadgeIsAlwaysVisible()
    {
        IRenderedComponent<CommitRisk> page = RenderRisk(Samples.Risk());

        Assert.Contains("kalibre edilmedi", page.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void EveryMandatoryWarningIsRendered()
    {
        IRenderedComponent<CommitRisk> page = RenderRisk(Samples.Risk());

        foreach (string code in RiskWarning.Always)
        {
            Assert.Contains(code, page.Markup, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void TheStaticSectionSaysItIsNotPartOfTheModelScore()
    {
        IRenderedComponent<CommitRisk> page = RenderRisk(Samples.Risk());

        Assert.Contains("model skoruna dahil degil", page.Markup, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gorsel kontrolde cikan gercek kusurun testi.
    ///
    /// Cubuk genisligi kultura bagli bicimlenirse "width:67,8%" yaziliyor, tarayici bunu
    /// gecersiz sayiyor ve cubuk tamamen doluyor. O zaman 0,43 ile 0,01 katkinin cubugu
    /// ayni uzunlukta gorunuyor.
    /// </summary>
    [Fact]
    public void ContributionBarWidthsAreWrittenWithADot()
    {
        IRenderedComponent<ContributionBars> bars = Render<ContributionBars>(parameters => parameters
            .Add(component => component.Title, "Skoru yukari tasiyanlar")
            .Add(component => component.Up, true)
            .Add(component => component.Items, Samples.Risk().PositiveContributions));

        string markup = bars.Markup;

        List<string> widths = [.. markup
            .Split("width:", StringSplitOptions.None)
            .Skip(1)
            .Select(part => part.Split('%')[0])];

        Assert.Equal(2, widths.Count);
        Assert.Equal("100.0", widths[0]);
        Assert.All(widths, width => Assert.DoesNotContain(",", width, StringComparison.Ordinal));

        // Ikinci katki birincinin yuzde ikisi kadar; cubuk da oyle olmali.
        Assert.NotEqual("100.0", widths[1]);

        // Sayi her satirda isaretiyle birlikte yazili; cubuk tek bilgi tasiyicisi degil.
        Assert.Contains("+0.5807", markup, StringComparison.Ordinal);
        Assert.Contains("+0.0138", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void AnUnreachableApiShowsARetryButtonAndNoStackTrace()
    {
        StubApi stub = new StubApi().GoesOffline();
        Services.AddSingleton(stub.Client());

        IRenderedComponent<Repositories> page = Render<Repositories>();

        Assert.Contains("API'ye ulasilamadi", page.Markup, StringComparison.Ordinal);
        Assert.Contains("Yeniden dene", page.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("HttpRequestException", page.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("   at ", page.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void AProblemResponseShowsItsCodeAndTraceId()
    {
        StubApi stub = new StubApi()
            .ReturnsProblem(
                "/api/v1/repositories/2",
                HttpStatusCode.UnprocessableEntity,
                ApiError.UnknownRepositoryModel,
                "Bu depo icin egitilmis bir model profili yok.");

        Services.AddSingleton(stub.Client());

        IRenderedComponent<RepositoryPage> page = Render<RepositoryPage>(parameters =>
            parameters.Add(component => component.RepositoryId, 2));

        Assert.Contains(ApiError.UnknownRepositoryModel, page.Markup, StringComparison.Ordinal);
        Assert.Contains("0HTEST", page.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void AnEmptyRepositoryListSaysSoInsteadOfShowingAnEmptyTable()
    {
        StubApi stub = new StubApi()
            .Returns("/api/v1/repositories", new PagedResponse<RepositoryListItem>(1, 25, 0, []));

        Services.AddSingleton(stub.Client());

        IRenderedComponent<Repositories> page = Render<Repositories>();

        Assert.Contains("Henuz taranmis depo yok", page.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("<table", page.Markup, StringComparison.Ordinal);
    }

    /// <summary>
    /// Sayfa boyutu secenekleri API'nin ust sinirini asamaz. Asan bir secenek koymak,
    /// kullaniciya her seferinde hata gostermek olurdu.
    /// </summary>
    [Fact]
    public void ThePageSizeChoicesNeverExceedTheApiLimit()
    {
        Assert.All(Pager.Sizes, size => Assert.True(size <= SievertApiClient.MaximumPageSize));
        Assert.Equal(100, SievertApiClient.MaximumPageSize);
    }

    [Fact]
    public void APartialResultIsNotPresentedLikeACompleteOne()
    {
        IRenderedComponent<PartialBanner> banner = Render<PartialBanner>(parameters => parameters
            .Add(component => component.Partial, true)
            .Add(component => component.Warning, "Is basariyla bitmedi."));

        Assert.Contains("Bu sonuc tamamlanmamistir", banner.Markup, StringComparison.Ordinal);
        Assert.Contains("notice warning", banner.Markup, StringComparison.Ordinal);

        IRenderedComponent<PartialBanner> complete = Render<PartialBanner>(parameters =>
            parameters.Add(component => component.Partial, false));

        Assert.Equal(string.Empty, complete.Markup.Trim());
    }

    /// <summary>
    /// C# dosyasi degistirmeyen commit'lerde kapsam uyarisi ekranda duruyor. Bu grupta
    /// egitim verisinde hic pozitif etiket yok; skoru uyarisiz gostermek yaniltici olurdu.
    /// </summary>
    [Fact]
    public void TheCoverageWarningIsVisibleWhenTheCommitTouchesNoCsharpFile()
    {
        CommitRiskAssessment risk = Samples.Risk() with
        {
            Warnings = [.. RiskWarning.Always, RiskWarning.CsLabelCoverageLimit],
            Limitations = RiskWarning.Describe([.. RiskWarning.Always, RiskWarning.CsLabelCoverageLimit]),
        };

        IRenderedComponent<CommitRisk> page = RenderRisk(risk);

        Assert.Contains(RiskWarning.CsLabelCoverageLimit, page.Markup, StringComparison.Ordinal);
        Assert.Contains("hic C# dosyasi degistirmiyor", page.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void TheRepositoryListShowsWhatTheApiReturnedAndNothingMore()
    {
        StubApi stub = new StubApi().Returns(
            "/api/v1/repositories",
            new PagedResponse<RepositoryListItem>(1, 25, 1,
            [
                new RepositoryListItem(
                    2,
                    "polly-full",
                    "github.com/app-vnext/polly",
                    "remote",
                    "https://github.com/app-vnext/polly",
                    2759,
                    DateTimeOffset.UnixEpoch,
                    DateTimeOffset.UnixEpoch.AddDays(1000),
                    DateTimeOffset.UnixEpoch.AddDays(2000),
                    ModelProfileAvailable: true),
            ]));

        Services.AddSingleton(stub.Client());

        IRenderedComponent<Repositories> page = Render<Repositories>();

        Assert.Contains("polly-full", page.Markup, StringComparison.Ordinal);
        Assert.Contains("2.759", page.Markup, StringComparison.Ordinal);

        // Yerel klasor yolu hicbir sutunda yok; API zaten gondermiyor, panel de uydurmuyor.
        Assert.DoesNotContain("localPath", page.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("/private/tmp", page.Markup, StringComparison.Ordinal);
    }

    /// <summary>
    /// Bu depo icin zaten calisan bir is varsa dugme kapali. API ikinci isi zaten
    /// reddediyor; reddedilecegi belli olan bir dugme gostermenin anlami yok.
    /// </summary>
    [Fact]
    public void TheStartButtonIsDisabledWhileAJobOfTheSameKindIsActive()
    {
        StubApi stub = new StubApi()
            .Returns(
                "/api/v1/repositories/2",
                new RepositoryDetail(
                    new RepositoryListItem(
                        2, "polly-full", "github.com/app-vnext/polly", "remote", null,
                        2759, DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch, true),
                    2759, 261, 0.09, 854, 2759, "polly", []))
            .Returns(
                "/api/v1/repositories/2/analyses",
                new PagedResponse<AnalysisJobResponse>(1, 5, 1, [Samples.Job("running", "risk-score-all")]))
            .Returns("/api/v1/repositories/2/commits", new PagedResponse<CommitListItem>(1, 25, 0, []));

        Services.AddSingleton(stub.Client());

        IRenderedComponent<RepositoryPage> page = Render<RepositoryPage>(parameters =>
            parameters.Add(component => component.RepositoryId, 2));

        string markup = page.Markup;

        // Calisan is risk skorlamasi; o dugme kapali, tarama dugmesi acik.
        Assert.Contains("su an Calisiyor", markup, StringComparison.Ordinal);

        IReadOnlyList<AngleSharp.Dom.IElement> buttons = page.FindAll("button");

        AngleSharp.Dom.IElement score = buttons.First(button =>
            button.TextContent.Contains("skorla", StringComparison.Ordinal));

        AngleSharp.Dom.IElement scan = buttons.First(button =>
            button.TextContent.Contains("Statik tarama", StringComparison.Ordinal));

        Assert.True(score.HasAttribute("disabled"), "calisan is varken skorlama dugmesi acik kalmis");
        Assert.False(scan.HasAttribute("disabled"), "ilgisiz bir is yuzunden tarama dugmesi kapanmis");
    }

    private IRenderedComponent<CommitRisk> RenderRisk(CommitRiskAssessment risk)
    {
        StubApi stub = new StubApi()
            .Returns($"/api/v1/repositories/2/commits/{risk.CommitSha}/risk", risk);

        Services.AddSingleton(stub.Client());

        return Render<CommitRisk>(parameters => parameters
            .Add(component => component.RepositoryId, 2)
            .Add(component => component.Sha, risk.CommitSha));
    }
}
