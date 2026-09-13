using Bunit;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

using Sievert.Contracts;
using Sievert.Web.Components.Pages;

namespace Sievert.Web.Tests;

/// <summary>
/// Is sayfasinin davranisi: durum sorma dongusu, iptal dugmesi ve kismi sonuc.
///
/// Polling kurallari sinif duzeyinde de sinaniyor (<see cref="JobPolling"/>) ama
/// dongunun gercekten durdugu ancak sayfa islenerek gorulebiliyor.
/// </summary>
public sealed class JobPageTests : BunitContext
{
    private FakePageState PageState { get; } = new();

    private static readonly Guid JobId = Guid.Parse("01a09990-0000-7000-8000-000000000001");

    public JobPageTests()
    {
        Services.AddSingleton(new WebOptions { ApiBaseUrl = new Uri("http://127.0.0.1:5000") });
        Services.AddSingleton<IPageState>(PageState);
    }

    [Fact]
    public void ATerminalJobIsNeverAskedAboutAgain()
    {
        StubApi stub = new StubApi()
            .Returns($"/api/v1/analyses/{JobId}", Samples.Job("succeeded", complete: true))
            .Returns($"/api/v1/analyses/{JobId}/risks", Page(complete: true));

        Interactive(stub);

        IRenderedComponent<AnalysisDetail> page = Render<AnalysisDetail>(parameters =>
            parameters.Add(component => component.JobId, JobId));

        int afterFirstRender = stub.JobRequests;

        // Iki saniye bekleniyor; dongu kosuyor olsaydi en az iki istek daha gorurduk.
        Thread.Sleep(2200);

        Assert.Equal(afterFirstRender, stub.JobRequests);
        Assert.Contains("Tamamlandi", page.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public async Task DisposingThePageStopsThePolling()
    {
        StubApi stub = new StubApi().Returns($"/api/v1/analyses/{JobId}", Samples.Job("running"));

        Interactive(stub);

        IRenderedComponent<AnalysisDetail> page = Render<AnalysisDetail>(parameters =>
            parameters.Add(component => component.JobId, JobId));

        Thread.Sleep(1300);

        int beforeDispose = stub.JobRequests;

        Assert.True(beforeDispose >= 2, $"dongu hic kosmadi: {beforeDispose} istek");

        await DisposeComponentsAsync();
        Thread.Sleep(1500);

        // Birakildiktan sonra tek bir istek bile gitmemeli; ucusta olan biri sayilabilir.
        Assert.True(
            stub.JobRequests - beforeDispose <= 1,
            $"birakildiktan sonra {stub.JobRequests - beforeDispose} istek daha gitti");
    }

    [Fact]
    public void OnlyOneJobRequestIsInFlightAtATime()
    {
        StubApi stub = new StubApi()
            .Returns($"/api/v1/analyses/{JobId}", Samples.Job("running"));

        Interactive(stub);

        Render<AnalysisDetail>(parameters => parameters.Add(component => component.JobId, JobId));

        Thread.Sleep(2200);

        // Bir saniyelik aralikla iki saniyede en fazla uc istek olur; daha fazlasi
        // ust uste binen istek demektir.
        Assert.InRange(stub.JobRequests, 2, 4);
    }

    [Fact]
    public async Task ARunningJobOffersCancelAndATerminalOneDoesNot()
    {
        StubApi running = new StubApi().Returns($"/api/v1/analyses/{JobId}", Samples.Job("running"));
        Interactive(running);

        IRenderedComponent<AnalysisDetail> page = Render<AnalysisDetail>(parameters =>
            parameters.Add(component => component.JobId, JobId));

        Assert.Contains("Isi iptal et", page.Markup, StringComparison.Ordinal);

        await DisposeComponentsAsync();
    }

    [Fact]
    public void ACanceledJobShowsItsPartialResultWithAWarning()
    {
        StubApi stub = new StubApi()
            .Returns($"/api/v1/analyses/{JobId}", Samples.Job("canceled", resultCount: 2500, errorCode: "ANALYSIS_CANCELED"))
            .Returns($"/api/v1/analyses/{JobId}/risks", Page(complete: false));

        Interactive(stub);

        IRenderedComponent<AnalysisDetail> page = Render<AnalysisDetail>(parameters =>
            parameters.Add(component => component.JobId, JobId));

        Assert.Contains("Iptal edildi", page.Markup, StringComparison.Ordinal);
        Assert.Contains("ANALYSIS_CANCELED", page.Markup, StringComparison.Ordinal);
        Assert.Contains("Bu sonuc tamamlanmamistir", page.Markup, StringComparison.Ordinal);
        Assert.Contains("tam degil", page.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void AStaticScanShowsTheCommitItScannedAndKeepsFindingsOutOfTheModelScore()
    {
        StubApi stub = new StubApi()
            .Returns($"/api/v1/analyses/{JobId}", Samples.Job("succeeded", "static-scan", complete: true))
            .Returns(
                $"/api/v1/analyses/{JobId}/findings",
                new AnalysisResultPage<StaticFindingResponse>(
                    JobId,
                    "succeeded",
                    true,
                    false,
                    null,
                    "1d7b6d97844c1111111111111111111111111111",
                    "1d7b6d97844c",
                    "clean",
                    true,
                    false,
                    1,
                    25,
                    1,
                    [
                        new StaticFindingResponse(
                            "SV001", "warning", "src/Ornek.cs", 12, null, "Bos", "async void", "gerekce", false),
                    ]));

        Interactive(stub);

        IRenderedComponent<AnalysisDetail> page = Render<AnalysisDetail>(parameters =>
            parameters.Add(component => component.JobId, JobId));

        Assert.Contains("1d7b6d97844c", page.Markup, StringComparison.Ordinal);
        Assert.Contains("Temiz calisma agaci", page.Markup, StringComparison.Ordinal);
        Assert.Contains("model skoruna dahil degildir", page.Markup, StringComparison.Ordinal);
        Assert.Contains("src/Ornek.cs", page.Markup, StringComparison.Ordinal);
    }

    /// <summary>
    /// API'ye yeni bir durum eklenirse panel cokmemeli. Tanimadigi degeri "Bilinmiyor"
    /// diye gosteriyor ve sozlesme degerini degistirmiyor.
    /// </summary>
    [Fact]
    public async Task AnUnknownStatusIsShownAsUnknownInsteadOfCrashing()
    {
        StubApi stub = new StubApi()
            .Returns($"/api/v1/analyses/{JobId}", Samples.Job("paused-for-maintenance"));

        Interactive(stub);

        IRenderedComponent<AnalysisDetail> page = Render<AnalysisDetail>(parameters =>
            parameters.Add(component => component.JobId, JobId));

        Assert.Contains("Bilinmiyor", page.Markup, StringComparison.Ordinal);

        await DisposeComponentsAsync();
    }

    [Fact]
    public void TheStatusLabelsAreTurkishButTheContractValuesAreNot()
    {
        Assert.Equal("Kuyrukta", Display.Status("queued"));
        Assert.Equal("Calisiyor", Display.Status("running"));
        Assert.Equal("Tamamlandi", Display.Status("succeeded"));
        Assert.Equal("Basarisiz", Display.Status("failed"));
        Assert.Equal("Iptal edildi", Display.Status("canceled"));
        Assert.Equal("Bilinmiyor", Display.Status("bilmedigim-durum"));
        Assert.Equal("Bilinmiyor", Display.Kind("bilmedigim-tur"));
    }

    [Fact]
    public void PollingWaitsOneSecondAndBacksOffOnFailures()
    {
        Assert.Equal(TimeSpan.FromSeconds(1), JobPolling.Interval);
        Assert.Equal(TimeSpan.FromSeconds(1), JobPolling.Wait(0));
        Assert.Equal(TimeSpan.FromSeconds(1), JobPolling.Wait(1));
        Assert.Equal(TimeSpan.FromSeconds(2), JobPolling.Wait(2));
        Assert.Equal(TimeSpan.FromSeconds(5), JobPolling.Wait(3));

        // Merdiven sinirli: bes saniyede duruyor, sonsuz buyumuyor.
        Assert.Equal(TimeSpan.FromSeconds(5), JobPolling.Wait(50));

        Assert.True(JobPolling.IsTerminal("succeeded"));
        Assert.True(JobPolling.IsTerminal("failed"));
        Assert.True(JobPolling.IsTerminal("canceled"));
        Assert.False(JobPolling.IsTerminal("running"));
        Assert.False(JobPolling.IsTerminal("queued"));
    }

    /// <summary>
    /// Istemciyi kaydeder ve olusturmayi **etkilesimli** yapar.
    ///
    /// Servisler tek seferde kaydedilmek zorunda: bUnit ilk servis alindiktan sonra yeni
    /// kayit kabul etmiyor. O yuzden RendererInfo burada, yapicida degil.
    /// </summary>
    private void Interactive(StubApi stub)
    {
        Services.AddSingleton(stub.Client());
        Renderer.SetRendererInfo(new RendererInfo("Server", isInteractive: true));
    }

    private static AnalysisResultPage<CommitRiskSnapshotResponse> Page(bool complete) => new(
        JobId,
        complete ? "succeeded" : "canceled",
        complete,
        !complete,
        complete ? null : "Is basariyla bitmedi; bu satirlar kismi olabilir.",
        null,
        null,
        null,
        false,
        false,
        1,
        25,
        1,
        [
            new CommitRiskSnapshotResponse(
                "482bdf824fba19c1188655156e512570c3d7f7af",
                "482bdf824fba",
                DateTimeOffset.UnixEpoch,
                "ornek commit",
                0.1725,
                74.3,
                false,
                false,
                0.2381,
                "polly",
                "0761308193ca",
                false,
                ["UNCALIBRATED_SCORE"]),
        ]);
}
