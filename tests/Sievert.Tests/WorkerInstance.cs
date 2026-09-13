using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Sievert.Api.Analysis;
using Sievert.Data;
using Sievert.Modeling;

namespace Sievert.Tests;

/// <summary>
/// Tek basina duran bir worker ornegi: kendi kuyrugu, kendi iptal jetonu defteri, kendi
/// servis kabi. Ayni veritabanina baglanan iki ornek acilabiliyor.
///
/// Ikinci bir HTTP sureci acmiyorum. Olcmek istedigim sey HTTP degil: iki worker'in
/// **veritabani uzerinden** nasil anlastigi. Surec ici kuyruk ve jeton defteri zaten
/// paylasilmiyor; ikisini ayirmak, iki ayri surecte olacak durumun aynisini kuruyor.
/// </summary>
internal sealed class WorkerInstance : IAsyncDisposable
{
    private readonly ServiceProvider provider;

    private readonly AnalysisJobWorker worker;

    /// <param name="configure">
    /// Servisleri degistirme noktasi. Es zamanlilik testi gercek isleyicilerin yerine
    /// sayan bir isleyici koyuyor; olculen sey oradaki isin ne yaptigi degil, ayni anda
    /// kac isleyicinin kostugu.
    /// </param>
    public WorkerInstance(
        string connectionString,
        AnalysisOptions? options = null,
        Action<IServiceCollection>? configure = null)
    {
        ServiceCollection services = new();

        services.AddLogging(logging => logging.SetMinimumLevel(LogLevel.None));
        services.AddSingleton(options ?? new AnalysisOptions());
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IAnalysisJobQueue>(scope =>
            new AnalysisJobQueue(scope.GetRequiredService<AnalysisOptions>()));
        services.AddSingleton<JobCancellationRegistry>();

        services.AddSingleton(ModelRegistry.Create(
            ProjectRoot.Combine("data", "asama5", "model-results.json"),
            ProjectRoot.Combine("data", "asama5", "models")));

        services.AddSingleton(ScoreReference.Load(
            ProjectRoot.Combine("data", "asama6", "model-score-reference.json")));

        services.AddDbContext<SievertContext>(
            context => context.UseNpgsql(connectionString),
            ServiceLifetime.Scoped);

        services.AddScoped<AnalysisJobStore>();
        services.AddScoped<IAnalysisJobHandler, StaticScanHandler>();
        services.AddScoped<IAnalysisJobHandler, RiskScoreAllHandler>();
        services.AddSingleton<AnalysisJobWorker>();

        configure?.Invoke(services);

        provider = services.BuildServiceProvider();
        worker = provider.GetRequiredService<AnalysisJobWorker>();
        Queue = provider.GetRequiredService<IAnalysisJobQueue>();
        Cancellations = provider.GetRequiredService<JobCancellationRegistry>();
    }

    public IAnalysisJobQueue Queue { get; }

    /// <summary>Bu ornegin yerel jeton defteri. Otekinin isini iptal EDEMEZ.</summary>
    public JobCancellationRegistry Cancellations { get; }

    public string InstanceId => worker.InstanceId;

    public Task StartAsync(CancellationToken cancellation = default) => worker.StartAsync(cancellation);

    public Task StopAsync(CancellationToken cancellation = default) => worker.StopAsync(cancellation);

    /// <summary>Bu ornegin kendi kapsaminda bir is deposu acar.</summary>
    public AnalysisJobStore NewStore(out IServiceScope scope)
    {
        scope = provider.CreateScope();

        return scope.ServiceProvider.GetRequiredService<AnalysisJobStore>();
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            await worker.StopAsync(CancellationToken.None);
        }
        catch (OperationCanceledException)
        {
            // Kapanirken iptal bekleniyor.
        }

        await provider.DisposeAsync();
    }
}
