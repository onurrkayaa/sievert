using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;

using Sievert.Api;
using Sievert.Api.Endpoints;
using Sievert.Data;
using Sievert.Modeling;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Yollar icerik kokune gore cozuluyor. Calisma dizinine guvenmek, API'yi baska bir
// klasorden baslatinca modelleri sessizce bulamamak demek olurdu.
ApiOptions options = new();
builder.Configuration.GetSection(ApiOptions.Section).Bind(options);
options = options.Resolve(builder.Environment.ContentRootPath);

// Kanit dosyalari olmadan sunulacak bir sey yok. Ilk istekte "beklenmeyen hata"
// donmektense burada acik bir mesajla durmak daha dogru: kullanici neyin eksik
// oldugunu ve hangi ayari verecegini goruyor.
if (!File.Exists(options.ModelResultsPath) || !File.Exists(options.ScoreReferencePath))
{
    Console.Error.WriteLine(
        $"Kanit dosyalari bulunamadi. Arandigi kok: {options.ArtifactRoot}\n"
        + $"  {options.ModelResultsPath}\n"
        + $"  {options.ScoreReferencePath}\n"
        + "Repo kokunden calistir ya da Sievert:ArtifactRoot ayarini ver.");

    return 2;
}

builder.Services.AddSingleton(options);

// Baglanti dizesi SIEVERT_DB'den okunuyor; koda yazilmiyor ve gunluge basilmiyor.
builder.Services.AddSingleton(DatabaseSettings.Resolve(builder.Environment.ContentRootPath));

builder.Services.AddDbContext<SievertContext>((provider, context) =>
{
    DatabaseSettings settings = provider.GetRequiredService<DatabaseSettings>();

    context.UseNpgsql(settings.Value ?? throw new InvalidOperationException(
        settings.Error ?? $"{ConnectionString.EnvironmentVariable} yok."));

    // Salt-okunur API; degisiklik takibi yapilacak bir sey yok.
    context.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
});

// Kayit defteri tek ornek: modeller istek basina degil, profil basina bir kez yukleniyor.
builder.Services.AddSingleton(provider =>
{
    ApiOptions resolved = provider.GetRequiredService<ApiOptions>();

    return ModelRegistry.Create(resolved.ModelResultsPath, resolved.ModelDirectory);
});

// Egitim skor dagilimi bir kez okunuyor; her istekte 24 bin satirlik dosyayi
// yeniden ayristirmanin anlami yok.
builder.Services.AddSingleton(provider =>
    ScoreReference.Load(provider.GetRequiredService<ApiOptions>().ScoreReferencePath));

builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

WebApplication app = builder.Build();

// Beklenmeyen hatada istisnanin kendisi cevaba girmiyor; sunucu gunlugunde kaliyor.
app.UseExceptionHandler(handler => handler.Run(async context =>
{
    IExceptionHandlerFeature? feature = context.Features.Get<IExceptionHandlerFeature>();

    app.Logger.LogError(feature?.Error, "Istek islenirken beklenmeyen hata: {Path}", context.Request.Path);

    await Problems
        .Create(
            context,
            StatusCodes.Status500InternalServerError,
            "Beklenmeyen hata",
            "Istek islenemedi. Ayrinti sunucu gunlugunde.",
            ApiError.Unexpected)
        .ExecuteAsync(context);
}));

app.MapOpenApi();

RouteGroupBuilder api = app.MapGroup("/api/v1");

api.MapHealth();
api.MapModels();
api.MapRepositories();
api.MapRisk();

app.Run();

return 0;

/// <summary>Test sunucusunun (WebApplicationFactory) tutunacagi giris noktasi.</summary>
public partial class Program;
