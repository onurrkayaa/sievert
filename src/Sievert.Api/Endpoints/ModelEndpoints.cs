using Sievert.Modeling;

namespace Sievert.Api.Endpoints;

public static class ModelEndpoints
{
    /// <summary>
    /// Her profille birlikte donen sinirliliklar. Model kartinin kisa hali: bir istemci
    /// yalnizca bu ucu okusa bile skorun ne olmadigini gormeli.
    /// </summary>
    public static readonly IReadOnlyList<string> Limitations =
    [
        "Skor kalibre edilmedi; olasilik olarak okunamaz.",
        "Hedef SZZ ile uretildi, gercek hata kayitlariyla degil.",
        "Egitim tek bir repodan; baska repolara genellendigi olculmedi.",
        "C# dosyasi degistirmeyen commit'lerde pozitif etiket hic gorulmedi.",
        "Insan dogrulamasi 14 ornekle sinirli kaldi.",
    ];

    public static void MapModels(this RouteGroupBuilder api)
    {
        api.MapGet("/models", (ModelRegistry registry) =>
            Results.Ok(registry.Profiles.Select(Describe).ToList()))
            .WithName("Models")
            .WithSummary("Egitilmis model profilleri ve sinirliliklari");

        api.MapGet("/models/{code}", (string code, ModelRegistry registry, HttpContext context) =>
            registry.Find(code) is ModelProfile profile
                ? Results.Ok(Describe(profile))
                : Problems.NotFound(context, $"Boyle bir model profili yok: {code}", ApiError.ModelProfileNotFound))
            .WithName("Model")
            .WithSummary("Tek bir model profili");
    }

    private static ModelResponse Describe(ModelProfile profile) => new(
        profile.ProfileCode,
        profile.DisplayName,
        profile.RepositoryIdentity,
        profile.Trainer,
        profile.MlPackage,
        profile.FeatureCount,
        profile.FeatureSchemaVersion,
        profile.TrainThreshold,
        profile.IsCalibrated,
        profile.ModelCodeCommit,
        profile.ShortChecksum,
        Limitations);
}
