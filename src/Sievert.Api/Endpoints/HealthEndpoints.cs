using Microsoft.EntityFrameworkCore;

using Sievert.Data;
using Sievert.Modeling;

namespace Sievert.Api.Endpoints;

public static class HealthEndpoints
{
    public static void MapHealth(this RouteGroupBuilder api) =>
        api.MapGet("/health", async (
            HttpContext context,
            DatabaseSettings settings,
            ModelRegistry registry,
            CancellationToken cancellation) =>
        {
            DatabaseHealth health = await CheckDatabase(context, settings, cancellation);

            List<ModelHealth> models = [];

            foreach (ModelProfile profile in registry.Profiles)
            {
                models.Add(new ModelHealth(
                    profile.ProfileCode,
                    registry.StatusOf(profile.ProfileCode).ToString(),
                    registry.FailureOf(profile.ProfileCode)));
            }

            return Results.Ok(new HealthResponse(
                health.Reachable ? "ok" : "degraded",
                typeof(Program).Assembly.GetName().Version?.ToString() ?? "bilinmiyor",
                health,
                registry.ModelResultsChecksum[..12],
                models));
        })
        .WithName("Health")
        .WithSummary("Veritabani ve model profillerinin durumu")
        .Produces<HealthResponse>();

    private static async Task<DatabaseHealth> CheckDatabase(
        HttpContext context,
        DatabaseSettings settings,
        CancellationToken cancellation)
    {
        // Hata metni ayar rehberligi iceriyor, baglanti dizesini degil.
        // Baglam bu kontrolden SONRA cozuluyor: dize yokken kurulmasi istisna atiyor.
        if (!settings.IsConfigured)
        {
            return new DatabaseHealth(false, false, settings.Error);
        }

        try
        {
            SievertContext database = Database.Open(context);

            if (!await database.Database.CanConnectAsync(cancellation))
            {
                return new DatabaseHealth(false, false, "Veritabanina baglanilamadi.");
            }

            bool pending = (await database.Database.GetPendingMigrationsAsync(cancellation)).Any();

            return new DatabaseHealth(true, !pending, pending ? "Uygulanmamis gecis var." : null);
        }
        catch (Exception error) when (error is not OperationCanceledException)
        {
            // Istisnanin kendi metni sunucu adi ve kullanici adi tasiyabilir; disari cikmiyor.
            return new DatabaseHealth(false, false, "Veritabanina baglanilamadi.");
        }
    }
}
