using Sievert.Contracts;
using Sievert.Web;
using Sievert.Web.Api;
using Sievert.Web.Components;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Kimlik dogrulama yok; panel de API gibi varsayilan olarak yalnizca loopback dinliyor.
// Ayni kural iki surecte de ayni koddan geciyor (Sievert.Contracts).
if (RemoteAccessGuard.Check(
    builder.Configuration["urls"],
    Environment.GetEnvironmentVariable(RemoteAccessGuard.EnvironmentVariable),
    host: "Panel") is string refusal)
{
    Console.Error.WriteLine(refusal);

    return 2;
}

WebOptions options;

try
{
    options = WebOptions.Read(builder.Configuration);
}
catch (InvalidOperationException error)
{
    // Adres okunamiyorsa ilk sayfada "beklenmeyen hata" gostermek yerine burada duruyoruz.
    Console.Error.WriteLine(error.Message);

    return 2;
}

builder.Services.AddSingleton(options);

// Tek tiplenmis istemci. HttpClient bilesenlere dagitilmiyor; hata cevabinin nasil
// okunacagi ve sayfalama sinirinin ne oldugu tek yerde duruyor.
builder.Services.AddHttpClient<SievertApiClient>(client =>
{
    client.BaseAddress = options.ApiBaseUrl;

    // Uzun suren isler arka planda kosuyor; panel yalnizca durum soruyor, o yuzden
    // istekler kisa olmali. Uzun bir zaman asimi, API duzgun cevap vermedigi halde
    // sayfayi dakikalarca "yukleniyor" tutardi.
    client.Timeout = TimeSpan.FromSeconds(20);
});

builder.Services.AddRazorComponents().AddInteractiveServerComponents();

WebApplication app = builder.Build();

if (RemoteAccessGuard.IsAllowed(Environment.GetEnvironmentVariable(RemoteAccessGuard.EnvironmentVariable))
    && !string.IsNullOrWhiteSpace(builder.Configuration["urls"]))
{
    app.Logger.LogWarning("{Warning}", RemoteAccessGuard.RemoteWarning);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/hata", createScopeForErrors: true);
}

app.UseStatusCodePagesWithReExecute("/bulunamadi", createScopeForStatusCodePages: true);

// CORS yok ve gerekmiyor: sayfalar sunucuda isleniyor, API cagrilari tarayicidan degil
// panelin kendi surecinden gidiyor. Tarayiciya acilan bir kaynak paylasimi da yok.
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.Run();

return 0;

/// <summary>Test sunucusunun tutunacagi giris noktasi.</summary>
public partial class Program;
