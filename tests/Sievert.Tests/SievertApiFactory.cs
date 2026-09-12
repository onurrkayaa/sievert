using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Sievert.Api;

namespace Sievert.Tests;

/// <summary>
/// API'yi bellek ici bir sunucuda ayaga kaldirir.
///
/// Iki sey degistiriliyor: icerik koku repo koku yapiliyor (model dosyalari oraya gore
/// duruyor) ve baglanti dizesi test konteynerininki oluyor. Geri kalan her sey uretimde
/// kosan yapilandirmanin aynisi; endpoint'ler test icin ayri bir yoldan gecmiyor.
/// </summary>
public sealed class SievertApiFactory(string? connectionString) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseContentRoot(ProjectRoot.Path);
        builder.UseSetting("Sievert:ModelDirectory", Path.Combine("data", "asama5", "models"));

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DatabaseSettings>();
            services.AddSingleton(connectionString is null
                ? new DatabaseSettings(null, null, "SIEVERT_DB verilmedi.")
                : new DatabaseSettings(connectionString, "test", null));
        });
    }
}
