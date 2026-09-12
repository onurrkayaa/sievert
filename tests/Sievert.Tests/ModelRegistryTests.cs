using Sievert.Modeling;

namespace Sievert.Tests;

/// <summary>
/// Kayit defteri testleri. Amac: model metadata'sinin API koduna elle kopyalanmadigini
/// ve ozeti uymayan bir model dosyasinin kullanilmadigini sinamak.
/// </summary>
public sealed class ModelRegistryTests
{
    private static readonly string ResultsPath = ProjectRoot.Combine("data", "asama5", "model-results.json");

    private static readonly string ModelDirectory = ProjectRoot.Combine("data", "asama5", "models");

    private static ModelRegistry Registry() => ModelRegistry.Create(ResultsPath, ModelDirectory);

    [Fact]
    public void Create_FindsTheThreeTrainedProfiles()
    {
        IReadOnlyList<ModelProfile> profiles = Registry().Profiles;

        Assert.Equal(["jellyfin", "polly", "sharex"], profiles.Select(profile => profile.ProfileCode));
    }

    [Fact]
    public void EveryProfile_PassesItsChecksum()
    {
        ModelRegistry registry = Registry();

        foreach (ModelProfile profile in registry.Profiles)
        {
            Assert.Equal(ModelStatus.NotLoaded, registry.StatusOf(profile.ProfileCode));
            Assert.Null(registry.FailureOf(profile.ProfileCode));
        }
    }

    [Fact]
    public void ThresholdsComeFromTheFrozenFile_NotFromCode()
    {
        // Degerler burada elle yazili degil: dosyadan okunup dosyayla karsilastiriliyor.
        using System.Text.Json.JsonDocument document =
            System.Text.Json.JsonDocument.Parse(File.ReadAllText(ResultsPath));

        ModelRegistry registry = Registry();

        foreach (System.Text.Json.JsonElement entry in document.RootElement.GetProperty("repositories").EnumerateArray())
        {
            string identity = entry.GetProperty("repository").GetString()!;
            ModelProfile profile = registry.ForRepository(identity)!;

            Assert.Equal(entry.GetProperty("trainThreshold").GetDouble(), profile.TrainThreshold);
        }
    }

    [Fact]
    public void EveryProfile_ReportsItselfAsUncalibrated()
    {
        foreach (ModelProfile profile in Registry().Profiles)
        {
            Assert.False(profile.IsCalibrated);
        }
    }

    [Fact]
    public void EveryProfile_CarriesFifteenCoefficients()
    {
        foreach (ModelProfile profile in Registry().Profiles)
        {
            Assert.Equal(15, profile.Coefficients.Weights.Count);
            Assert.Equal(15, profile.FeatureCount);
        }
    }

    [Fact]
    public void ScalerFor_RebuildsTheTrainStatistics()
    {
        ModelRegistry registry = Registry();
        ModelProfile profile = registry.Find("polly")!;
        FeatureScaler scaler = registry.ScalerFor(profile);

        // 15 aday ozniteligin 14'u surekli; IsFix standartlastirilmiyor.
        Assert.Equal(14, scaler.Statistics.Count);
        Assert.Equal(profile.RepositoryIdentity, scaler.Identity);
    }

    [Fact]
    public void Load_ReturnsTheSameInstanceEveryTime()
    {
        ModelRegistry registry = Registry();

        LoadedModel first = registry.Load("polly");
        LoadedModel second = registry.Load("polly");

        Assert.Same(first, second);
        Assert.Equal(ModelStatus.Ready, registry.StatusOf("polly"));
    }

    [Fact]
    public void ForRepository_ReturnsNothingForAnUnknownRepository()
    {
        Assert.Null(Registry().ForRepository("github.com/bilinmeyen/depo"));
    }

    [Fact]
    public void ABrokenModelFile_IsRefusedInsteadOfUsed()
    {
        string directory = CopyModelsTo(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()));

        try
        {
            // Tek bir bayt degistirmek yetiyor.
            byte[] bytes = File.ReadAllBytes(Path.Combine(directory, "polly.zip"));
            bytes[^1] ^= 0xFF;
            File.WriteAllBytes(Path.Combine(directory, "polly.zip"), bytes);

            ModelRegistry registry = ModelRegistry.Create(ResultsPath, directory);

            Assert.Equal(ModelStatus.ChecksumMismatch, registry.StatusOf("polly"));
            Assert.NotNull(registry.FailureOf("polly"));
            Assert.Throws<InvalidDataException>(() => registry.Load("polly"));

            // Digerleri etkilenmiyor.
            Assert.Equal(ModelStatus.NotLoaded, registry.StatusOf("sharex"));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void AMissingChecksumLine_IsTreatedAsAMismatch()
    {
        string directory = CopyModelsTo(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()));

        try
        {
            File.Delete(Path.Combine(directory, "models.sha256"));

            ModelRegistry registry = ModelRegistry.Create(ResultsPath, directory);

            Assert.Equal(ModelStatus.ChecksumMismatch, registry.StatusOf("polly"));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static string CopyModelsTo(string directory)
    {
        Directory.CreateDirectory(directory);

        foreach (string file in Directory.GetFiles(ModelDirectory))
        {
            File.Copy(file, Path.Combine(directory, Path.GetFileName(file)));
        }

        return directory;
    }
}
