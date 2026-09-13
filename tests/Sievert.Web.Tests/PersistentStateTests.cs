using Bunit;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

using Sievert.Contracts;
using Sievert.Web.Components.Pages;

namespace Sievert.Web.Tests;

/// <summary>
/// On-islemede cekilen verinin etkilesimli asamaya tasinmasi.
///
/// Adim 4'un olcumunde ayni veri iki kez cekiliyordu: bir kez sunucu sayfayi on-islerken,
/// bir kez devre baglaninca. Burada sinanan sey tam olarak bu istegin ikinci kez gidip
/// gitmedigi - ve daha onemlisi, **yanlis** veriyle gitmemesi: farkli bir filtre eski
/// pakete dusmemeli.
/// </summary>
public sealed class PersistentStateTests : BunitContext
{
    private FakePageState PageState { get; } = new();

    public PersistentStateTests()
    {
        Services.AddSingleton(new WebOptions { ApiBaseUrl = new Uri("http://127.0.0.1:5000") });
        Services.AddSingleton<IPageState>(PageState);
    }

    [Fact]
    public void ThePrerenderFetchesAndThenPersists()
    {
        StubApi stub = Ready();

        Render<Repositories>();

        Assert.Equal(1, stub.Requests.Count(path => path.StartsWith("/api/v1/repositories", StringComparison.Ordinal)));
        Assert.Equal(1, PageState.Subscriptions);

        PageState.Flush();

        Assert.True(PageState.Has("repositories|page=1|size=25"));
    }

    /// <summary>
    /// Ikinci olusturma saklanani aliyor ve **ikinci bir GET atmiyor**. Adim 4'te bu
    /// istek gidiyordu.
    /// </summary>
    [Fact]
    public void TheInteractiveRenderTakesTheStateAndDoesNotFetchAgain()
    {
        StubApi stub = Ready();

        Render<Repositories>();
        PageState.Flush();

        int afterPrerender = stub.Requests.Count;

        Render<Repositories>();

        Assert.Equal(afterPrerender, stub.Requests.Count);
    }

    [Fact]
    public void WithoutStoredStateThePageFetches()
    {
        StubApi stub = Ready();

        Render<Repositories>();
        Render<Repositories>();

        // Flush cagrilmadi, yani on-isleme hicbir sey saklamadi; iki olusturma da cekti.
        Assert.Equal(2, stub.Requests.Count(path => path.StartsWith("/api/v1/repositories", StringComparison.Ordinal)));
    }

    /// <summary>
    /// Anahtar sayfa numarasini iceriyor. Icermeseydi ikinci sayfa, on-islemeden kalan
    /// birinci sayfayi gosterir ve kullanici sayfa degistirdigini sanirdi.
    /// </summary>
    [Fact]
    public void ADifferentPageNumberDoesNotReuseTheStoredPage()
    {
        StubApi stub = Ready();

        Render<Repositories>();
        PageState.Flush();

        int afterPrerender = stub.Requests.Count;

        // Adres cubugunda sayfa 2.
        Services.GetRequiredService<NavigationManager>()
            .NavigateTo("http://localhost/repositories?page=2&pageSize=25");
        Render<Repositories>();

        Assert.True(stub.Requests.Count > afterPrerender, "ikinci sayfa eski paketi kullandi");
        Assert.Contains("repositories|page=2|size=25", PageState.Asked);
    }

    [Fact]
    public void ADifferentRepositoryGetsADifferentKey()
    {
        Assert.NotEqual(
            PageStateKey.For("repository", ("repo", 2)),
            PageStateKey.For("repository", ("repo", 3)));

        Assert.NotEqual(
            PageStateKey.For("repository", ("repo", 2), ("part", "commits"), ("fix", true)),
            PageStateKey.For("repository", ("repo", 2), ("part", "commits"), ("fix", false)));

        // "Hepsi" ile "false" ayni anahtari uretmemeli.
        Assert.NotEqual(
            PageStateKey.For("repository", ("fix", (bool?)null)),
            PageStateKey.For("repository", ("fix", false)));
    }

    /// <summary>
    /// Anahtar kulturden bagimsiz. Sunucunun kulturu virgullu ondalik kullansaydi ayni
    /// sayfa iki kulturde iki farkli anahtar uretir ve saklanan veri hic alinamazdi.
    /// </summary>
    [Fact]
    public void KeysAreCultureIndependent()
    {
        string expected = PageStateKey.For("x", ("window", 1000), ("ratio", 12.5));

        foreach (string culture in (string[])["tr-TR", "en-US", "de-DE"])
        {
            System.Globalization.CultureInfo before = System.Globalization.CultureInfo.CurrentCulture;

            try
            {
                System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo(culture);

                Assert.Equal(expected, PageStateKey.For("x", ("window", 1000), ("ratio", 12.5)));
            }
            finally
            {
                System.Globalization.CultureInfo.CurrentCulture = before;
            }
        }

        Assert.Contains("ratio=12.5", expected, StringComparison.Ordinal);
    }

    /// <summary>Saklanan pakette yol, parola ya da yazar e-postasi yok.</summary>
    [Fact]
    public void NothingSensitiveIsStored()
    {
        Ready();

        Render<Repositories>();
        PageState.Flush();

        string stored = PageState.All();

        Assert.DoesNotContain("localPath", stored, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Password", stored, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("/private/tmp", stored, StringComparison.Ordinal);
        Assert.DoesNotContain("Host=", stored, StringComparison.Ordinal);
        Assert.DoesNotContain("@", stored, StringComparison.Ordinal);
    }

    [Fact]
    public async Task DisposingThePageReleasesTheSubscription()
    {
        Ready();

        Render<Repositories>();

        Assert.Equal(1, PageState.Subscriptions);
        Assert.Equal(0, PageState.Released);

        await DisposeComponentsAsync();

        Assert.Equal(1, PageState.Released);
    }

    /// <summary>
    /// Terminal duruma gecmis bir is on-islemeden gelirse dongu hic baslamiyor: hem
    /// durum terminal hem de olusturma zaten bir kez veriyi getirmis oluyor.
    /// </summary>
    [Fact]
    public void ATerminalJobFromTheStateStartsNoPolling()
    {
        Guid jobId = Guid.Parse("01a09990-0000-7000-8000-000000000001");

        StubApi stub = new StubApi()
            .Returns($"/api/v1/analyses/{jobId}", Samples.Job("succeeded", complete: true))
            .Returns($"/api/v1/analyses/{jobId}/risks", Empty(jobId));

        PageState.Seed(
            PageStateKey.For("analysis", ("job", jobId)),
            Samples.Job("succeeded", complete: true));

        Services.AddSingleton(stub.Client());
        Renderer.SetRendererInfo(new RendererInfo("Server", isInteractive: true));

        Render<AnalysisDetail>(parameters => parameters.Add(component => component.JobId, jobId));

        Thread.Sleep(2200);

        // Is durumu icin tek bir istek bile gitmedi: paket on-islemeden geldi ve is
        // terminal oldugu icin dongu hic baslamadi.
        Assert.DoesNotContain(stub.Requests, path => path == $"/api/v1/analyses/{jobId}");
    }

    /// <summary>On-isleme (etkilesimsiz) olusturmada dongu baslamiyor.</summary>
    [Fact]
    public void TheServerPrerenderStartsNoPolling()
    {
        Guid jobId = Guid.Parse("01a09990-0000-7000-8000-000000000001");

        StubApi stub = new StubApi()
            .Returns($"/api/v1/analyses/{jobId}", Samples.Job("running"))
            .Returns($"/api/v1/analyses/{jobId}/risks", Empty(jobId));

        Services.AddSingleton(stub.Client());
        Renderer.SetRendererInfo(new RendererInfo("Static", isInteractive: false));

        Render<AnalysisDetail>(parameters => parameters.Add(component => component.JobId, jobId));

        int afterFirstRender = stub.Requests.Count(path => path == $"/api/v1/analyses/{jobId}");

        Thread.Sleep(2200);

        Assert.Equal(1, afterFirstRender);
        Assert.Equal(afterFirstRender, stub.Requests.Count(path => path == $"/api/v1/analyses/{jobId}"));
    }

    /// <summary>
    /// Saklanan durumun bir boyut butcesi var ve olmasi sart.
    ///
    /// Interactive Server'da saklanan durum devre acilirken istemciden sunucuya geri
    /// gonderiliyor ve o yolun varsayilan siniri 32 KB. Sinir asilinca devre kapaniyor:
    /// sayfa on-islemeden geldigi icin dolu gorunuyor ama hicbir tiklama calismiyor ve
    /// sunucu gunlugune tek satir dusmuyor.
    ///
    /// Bu yasandi: dosya haritasi sayfasinin durumu 142 KB cikti ve sayfa olu dogdu.
    /// </summary>
    [Fact]
    public void TheStateBudgetStaysWellUnderTheCircuitLimit()
    {
        Assert.True(PersistentPageState.TotalBudget < PersistentPageState.CircuitMessageLimit);

        // Kodlama saklanan baytlari buyutuyor; toplam butce cerceve sinirinin en fazla
        // yarisi olmali ki kodlama payi sigsin.
        Assert.True(PersistentPageState.TotalBudget <= PersistentPageState.CircuitMessageLimit / 2);

        // Anahtar basina sinir toplamdan kucuk; buyuk olsaydi hicbir sey sinirlamazdi.
        Assert.True(PersistentPageState.Budget < PersistentPageState.TotalBudget);

        Assert.True(PersistentPageState.Fits(1024, 0));
        Assert.True(PersistentPageState.Fits(PersistentPageState.Budget, 0));
        Assert.False(PersistentPageState.Fits(PersistentPageState.Budget + 1, 0));

        // Anahtar basina sinir tek basina yetmiyor; toplam da sayiliyor.
        Assert.False(PersistentPageState.Fits(8 * 1024, PersistentPageState.TotalBudget - 1024));
    }

    /// <summary>Butceyi asan paket saklanmiyor ve bir sonraki olusturma onu yeniden cekiyor.</summary>
    [Fact]
    public void AnOversizedPayloadIsNotStoredAndIsFetchedAgain()
    {
        StubApi stub = new StubApi().Returns(
            "/api/v1/repositories",
            new PagedResponse<RepositoryListItem>(1, 25, 400, [.. Enumerable.Range(0, 400).Select(Big)]));

        Services.AddSingleton(stub.Client());
        Renderer.SetRendererInfo(new RendererInfo("Server", isInteractive: true));

        Render<Repositories>();
        PageState.Flush();

        Assert.Contains("repositories|page=1|size=25", PageState.Skipped);
        Assert.False(PageState.Has("repositories|page=1|size=25"));

        int afterPrerender = stub.Requests.Count;

        Render<Repositories>();

        // Saklanmadigi icin ikinci olusturma cekiyor. Bu bir ek istek; alternatifi
        // calismayan bir sayfa.
        Assert.True(stub.Requests.Count > afterPrerender);
    }

    private static RepositoryListItem Big(int index) => new(
        index,
        "depo-" + index + new string('x', 60),
        "github.com/ornek/depo-" + index + new string('y', 60),
        "remote",
        "https://github.com/ornek/depo-" + index,
        1000 + index,
        DateTimeOffset.UnixEpoch,
        DateTimeOffset.UnixEpoch,
        DateTimeOffset.UnixEpoch,
        ModelProfileAvailable: true);

    private static AnalysisResultPage<CommitRiskSnapshotResponse> Empty(Guid jobId) => new(
        jobId, "succeeded", true, false, null, null, null, null, false, false, 1, 25, 0, []);

    private StubApi Ready()
    {
        StubApi stub = new StubApi().Returns(
            "/api/v1/repositories",
            new PagedResponse<RepositoryListItem>(1, 25, 1,
            [
                new RepositoryListItem(
                    2, "polly-full", "github.com/app-vnext/polly", "remote", null,
                    2759, DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch, true),
            ]));

        Services.AddSingleton(stub.Client());
        Renderer.SetRendererInfo(new RendererInfo("Server", isInteractive: true));

        return stub;
    }
}
