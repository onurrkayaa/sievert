namespace Sievert.Api;

/// <summary>
/// API ayarlari. Baglanti dizesi burada YOK: o yalnizca <c>SIEVERT_DB</c> ortam
/// degiskeninden okunuyor (ADR 0012) ve hicbir ayar dosyasina yazilmiyor.
/// </summary>
public sealed class ApiOptions
{
    public const string Section = "Sievert";

    /// <summary>
    /// Model artefaktlarinin bulundugu klasor. Icerik koku uzerinden cozuluyor;
    /// calisma dizinine guvenilmiyor, cunku API baska bir klasorden baslatilabilir.
    /// </summary>
    public string ModelDirectory { get; set; } = Path.Combine("data", "asama5", "models");

    /// <summary>Model metadata'sinin okundugu dondurulmus kanit dosyasi.</summary>
    public string ModelResultsPath { get; set; } = Path.Combine("data", "asama5", "model-results.json");

    /// <summary>RiskIndex icin egitim skor referansi.</summary>
    public string ScoreReferencePath { get; set; } = Path.Combine("data", "asama6", "model-score-reference.json");

    /// <summary>Sayfalama varsayilani ve ust siniri.</summary>
    public int DefaultPageSize { get; set; } = 25;

    public int MaximumPageSize { get; set; } = 100;

    /// <summary>Goreli yollari icerik kokune gore cozer.</summary>
    public ApiOptions Resolve(string contentRoot) => new()
    {
        ModelDirectory = Absolute(contentRoot, ModelDirectory),
        ModelResultsPath = Absolute(contentRoot, ModelResultsPath),
        ScoreReferencePath = Absolute(contentRoot, ScoreReferencePath),
        DefaultPageSize = DefaultPageSize,
        MaximumPageSize = MaximumPageSize,
    };

    private static string Absolute(string root, string path) =>
        Path.IsPathRooted(path) ? path : Path.GetFullPath(Path.Combine(root, path));
}
