using Bunit;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

using Sievert.Contracts;
using Sievert.Web.Components.Pages;
using Sievert.Web.Components.Shared;

namespace Sievert.Web.Tests;

/// <summary>
/// Rapor formunun ve rapor sayfasinin ciktisi.
///
/// Sinanan sey ekrandaki metin: kismi bir is secildiginde onay kutusu cikiyor mu, onay
/// verilmeden dugme kapali mi, hazir olmayan bir raporun indirme baglantisi gorunuyor mu.
/// Bunlarin hepsi "sessizce yanlis" olabilecek seyler.
/// </summary>
public sealed class ReportPageTests : BunitContext
{
    private static readonly Guid ReportId = Guid.Parse("11111111-2222-3333-4444-555555555555");

    private static readonly Guid RiskJobId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

    private static readonly DateTimeOffset Moment = new(2026, 9, 13, 10, 0, 0, TimeSpan.Zero);

    public ReportPageTests()
    {
        Services.AddSingleton(new WebOptions { ApiBaseUrl = new Uri("http://127.0.0.1:5000") });
        Services.AddSingleton<IPageState>(new FakePageState());
    }

    [Fact]
    public void TheFormOffersTheContractDefaults()
    {
        IRenderedComponent<ReportForm> form = RenderForm(Job(complete: true));

        string markup = form.Markup;

        Assert.Contains($"value=\"{ReportLimits.DefaultCommitWindow}\" selected", markup, StringComparison.Ordinal);
        Assert.Contains($"value=\"{ReportLimits.DefaultFileLimit}\" selected", markup, StringComparison.Ordinal);
        Assert.Contains($"value=\"{ReportLimits.DefaultTimelineCount}\" selected", markup, StringComparison.Ordinal);
        Assert.Contains($"value=\"{ReportLimits.DefaultTopCommitCount}\" selected", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void TheFormOnlyOffersChoicesTheApiAccepts()
    {
        IRenderedComponent<ReportForm> form = RenderForm(Job(complete: true));

        foreach (int choice in ReportLimits.WindowChoices)
        {
            Assert.True(ReportLimits.IsCommitWindow(choice));
            Assert.Contains($"value=\"{choice}\"", form.Markup, StringComparison.Ordinal);
        }

        foreach (int choice in ReportLimits.FileLimitChoices)
        {
            Assert.True(ReportLimits.IsFileLimit(choice));
        }

        foreach (int choice in ReportLimits.TimelineChoices)
        {
            Assert.True(ReportLimits.IsTimelineCount(choice));
        }
    }

    [Fact]
    public void ACompleteJobNeedsNoPartialConsent()
    {
        IRenderedComponent<ReportForm> form = RenderForm(Job(complete: true));

        Assert.DoesNotContain("kabul ediyorum", form.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("disabled", form.Find("button.btn").OuterHtml, StringComparison.Ordinal);
    }

    [Fact]
    public void APartialJobAsksForConsentAndBlocksTheButtonUntilItIsGiven()
    {
        IRenderedComponent<ReportForm> form = RenderForm(Job(complete: false));

        Assert.Contains("kabul ediyorum", form.Markup, StringComparison.Ordinal);
        Assert.Contains("disabled", form.Find("button.btn").OuterHtml, StringComparison.Ordinal);

        form.Find("input[type=checkbox]").Change(true);

        Assert.DoesNotContain("disabled", form.Find("button.btn").OuterHtml, StringComparison.Ordinal);
    }

    [Fact]
    public void WithoutAnyJobTheFormSaysWhatIsMissing()
    {
        Services.AddSingleton(new StubApi().Client());
        Renderer.SetRendererInfo(new RendererInfo("Server", isInteractive: false));

        IRenderedComponent<ReportForm> form = Render<ReportForm>(parameters => parameters
            .Add(component => component.RepositoryId, 2)
            .Add(component => component.Jobs, []));

        Assert.Contains("Once bir risk analizi calistir", form.Markup, StringComparison.Ordinal);
        Assert.Empty(form.FindAll("button.btn"));
    }

    [Fact]
    public void ClickingTwiceSendsOnlyOneRequest()
    {
        StubApi stub = new StubApi()
            .Returns("/api/v1/repositories/2/reports", new ReportAcceptedResponse(
                ReportId, Guid.NewGuid(), ReportStatus.Pending, new string('a', 64), false, []));

        Services.AddSingleton(stub.Client());
        Renderer.SetRendererInfo(new RendererInfo("Server", isInteractive: true));

        IRenderedComponent<ReportForm> form = Render<ReportForm>(parameters => parameters
            .Add(component => component.RepositoryId, 2)
            .Add(component => component.Jobs, [Job(complete: true)]));

        form.Find("button.btn").Click();
        form.Find("button.btn").Click();

        Assert.Equal(1, stub.Requests.Count(path => path.EndsWith("/reports", StringComparison.Ordinal)));
    }

    [Fact]
    public void ARefusedRequestShowsTheErrorCode()
    {
        StubApi stub = new StubApi()
            .ReturnsProblem(
                "/api/v1/repositories/2/reports",
                System.Net.HttpStatusCode.UnprocessableEntity,
                ApiError.ReportPartialResultNotAllowed,
                "Is tamamlanmadi.");

        Services.AddSingleton(stub.Client());
        Renderer.SetRendererInfo(new RendererInfo("Server", isInteractive: true));

        IRenderedComponent<ReportForm> form = Render<ReportForm>(parameters => parameters
            .Add(component => component.RepositoryId, 2)
            .Add(component => component.Jobs, [Job(complete: true)]));

        form.Find("button.btn").Click();

        Assert.Contains(ApiError.ReportPartialResultNotAllowed, form.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void APendingReportShowsNoDownloadLink()
    {
        IRenderedComponent<ReportDetail> page = RenderDetail(Report(ReportStatus.Pending));

        Assert.Contains("Uretiliyor", page.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("PDF indir", page.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void AReadyReportShowsTheDownloadLinkAndTheChecksum()
    {
        IRenderedComponent<ReportDetail> page = RenderDetail(Report(ReportStatus.Ready));

        Assert.Contains("PDF indir", page.Markup, StringComparison.Ordinal);
        Assert.Contains(new string('c', 64), page.Markup, StringComparison.Ordinal);
        Assert.Contains("8", page.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void AFailedReportShowsTheErrorCodeAndARetryButton()
    {
        IRenderedComponent<ReportDetail> page = RenderDetail(
            Report(ReportStatus.Failed) with
            {
                ErrorCode = ApiError.ReportGenerationFailed,
                ErrorMessage = "PDF uretilemedi.",
            });

        Assert.Contains(ApiError.ReportGenerationFailed, page.Markup, StringComparison.Ordinal);
        Assert.Contains("Yeniden dene", page.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("PDF indir", page.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void ACorruptedReportIsNotOfferedForDownload()
    {
        IRenderedComponent<ReportDetail> page = RenderDetail(Report(ReportStatus.Corrupted));

        Assert.Contains("dogrulanamadi", page.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("PDF indir", page.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void APartialReportIsLabelledOnTheDetailPage()
    {
        IRenderedComponent<ReportDetail> page = RenderDetail(
            Report(ReportStatus.Ready) with { IsPartial = true });

        Assert.Contains("kismi", page.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TheDownloadLinkHasNoDoubleSlash()
    {
        // Taban adres "/" ile bitiyor; elle birlestirince adres "//api/v1/..." oluyordu
        // ve API 404 donuyordu. Gercek tarayicida yakalandi.
        string markup = RenderDetail(Report(ReportStatus.Ready)).Markup;

        Assert.Contains($"http://127.0.0.1:5000/api/v1/reports/{ReportId}/download", markup, StringComparison.Ordinal);
        Assert.DoesNotContain("5000//api", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void TheDetailPageShowsNoFileSystemPath()
    {
        string markup = RenderDetail(Report(ReportStatus.Ready)).Markup;

        Assert.DoesNotContain("data/runtime", markup, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("storageKey", markup, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("/Users/", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void APendingReportIsAskedAgainOnlyWhileItIsPending()
    {
        StubApi stub = new StubApi().Returns($"/api/v1/reports/{ReportId}", Report(ReportStatus.Ready));

        Services.AddSingleton(stub.Client());
        Renderer.SetRendererInfo(new RendererInfo("Server", isInteractive: true));

        IRenderedComponent<ReportDetail> page = Render<ReportDetail>(parameters =>
            parameters.Add(component => component.ReportId, ReportId));

        Assert.Contains("Hazir", page.Markup, StringComparison.Ordinal);

        // Hazir bir rapor icin dongu hic baslamiyor: tek bir istek yeterli.
        Assert.Equal(1, stub.Requests.Count(path => path.Contains("/reports/", StringComparison.Ordinal)));
    }

    private IRenderedComponent<ReportForm> RenderForm(AnalysisJobResponse job)
    {
        Services.AddSingleton(new StubApi().Client());
        Renderer.SetRendererInfo(new RendererInfo("Server", isInteractive: false));

        return Render<ReportForm>(parameters => parameters
            .Add(component => component.RepositoryId, 2)
            .Add(component => component.Jobs, [job]));
    }

    private IRenderedComponent<ReportDetail> RenderDetail(ReportResponse report)
    {
        StubApi stub = new StubApi().Returns($"/api/v1/reports/{ReportId}", report);

        Services.AddSingleton(stub.Client());

        // Sayfa RendererInfo okuyor (dongu yalniz etkilesimli olusturmada basliyor),
        // o yuzden bUnit'e hangi olusturmada oldugumuzu soylemek gerekiyor.
        Renderer.SetRendererInfo(new RendererInfo("Server", isInteractive: false));

        return Render<ReportDetail>(parameters =>
            parameters.Add(component => component.ReportId, ReportId));
    }

    private static AnalysisJobResponse Job(bool complete) => new(
        RiskJobId,
        2,
        "risk-score-all",
        complete ? "succeeded" : "canceled",
        Moment,
        Moment,
        Moment,
        complete ? "succeeded" : "canceled",
        100,
        100,
        100,
        false,
        100,
        complete,
        null,
        null,
        null,
        null,
        null,
        null,
        false,
        false,
        []);

    private static ReportResponse Report(string status) => new(
        ReportId,
        2,
        Guid.NewGuid(),
        RiskJobId,
        null,
        status,
        "pdf",
        ReportCulture.Turkish,
        "sievert-polly-20260913.pdf",
        false,
        new string('b', 64),
        status == ReportStatus.Ready ? new string('c', 64) : null,
        status == ReportStatus.Ready ? 158914 : null,
        status == ReportStatus.Ready ? 8 : null,
        Moment,
        status == ReportStatus.Ready ? Moment : null,
        status == ReportStatus.Ready ? Moment : null,
        null,
        null,
        "1.0",
        "sievert-report/1.0",
        new ReportParameters(false, 200, 50, 100, 20, 50, null, null),
        []);
}
