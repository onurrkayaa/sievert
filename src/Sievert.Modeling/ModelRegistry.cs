using System.Globalization;
using System.Text.Json;

namespace Sievert.Modeling;

/// <summary>Bir model profilinin, kanit dosyalarindan okunan degismez tanimi.</summary>
public sealed record ModelProfile(
    string ProfileCode,
    string RepositoryIdentity,
    string DisplayName,
    string ModelPath,
    string ModelChecksum,
    string ModelResultsChecksum,
    double TrainThreshold,
    string FeatureSchemaVersion,
    string ModelCodeCommit,
    string Trainer,
    string MlPackage,
    Coefficients Coefficients,
    IReadOnlyList<FeatureStatistics> TrainStatistics)
{
    /// <summary>Bu asamada her zaman false; uretim icin kalibrator secilmedi (ADR 0022).</summary>
    public bool IsCalibrated => false;

    public int FeatureCount => ModelFeatures.Candidates.Count;

    public string ShortChecksum => ModelChecksum[..12];
}

/// <summary>Bir profilin o anki durumu; saglik ucu bunu gosteriyor.</summary>
public enum ModelStatus
{
    /// <summary>Checksum dogrulandi ama model henuz diskten yuklenmedi.</summary>
    NotLoaded,

    /// <summary>Model bellekte, kullanilabilir.</summary>
    Ready,

    /// <summary>Dosyanin ozeti kayitli degerle ayni degil; model KULLANILMIYOR.</summary>
    ChecksumMismatch,

    /// <summary>Baska bir sebeple yuklenemedi.</summary>
    Failed,
}

/// <summary>
/// Uc model profilinin kayit defteri.
///
/// Metadata API koduna ELLE KOPYALANMIYOR: esik, katsayilar, egitim istatistikleri,
/// trainer ve surum bilgisi dondurulmus <c>model-results.json</c> dosyasindan, model
/// ozetleri <c>models/models.sha256</c> dosyasindan okunuyor.
///
/// Model dosyalari her istekte yeniden yuklenmiyor: profil basina tek bir
/// <see cref="Lazy{T}"/> var ve ayni anda gelen ilk iki istek modeli bir kez yukluyor.
/// Checksum dogrulanmadan hicbir model kullanilmiyor.
/// </summary>
public sealed class ModelRegistry
{
    /// <summary>Oznitelik semasinin surumu; 15 aday oznitelik ve donusum sozlesmesi.</summary>
    public const string FeatureSchemaVersion = "asama5-15oznitelik-log1p-standart";

    private static readonly (string Code, string Identity, string Display)[] Known =
    [
        ("polly", "github.com/app-vnext/polly", "Polly"),
        ("sharex", "github.com/sharex/sharex", "ShareX"),
        ("jellyfin", "github.com/jellyfin/jellyfin", "Jellyfin"),
    ];

    private readonly Dictionary<string, ModelProfile> profiles = new(StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<string, Lazy<LoadedModel>> loaders = new(StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<string, ModelStatus> statuses = new(StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<string, string> failures = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Profil basina modelin diskten KAC KEZ okundugu.</summary>
    private readonly Dictionary<string, int> loadCounts = new(StringComparer.OrdinalIgnoreCase);

    private ModelRegistry()
    {
    }

    /// <summary>Metadata'nin okundugu dondurulmus dosyanin ozeti.</summary>
    public string ModelResultsChecksum { get; private set; } = string.Empty;

    public IReadOnlyList<ModelProfile> Profiles =>
        [.. profiles.Values.OrderBy(profile => profile.ProfileCode, StringComparer.Ordinal)];

    /// <summary>
    /// Kanit dosyalarindan kayit defterini kurar ve her modelin ozetini dogrular.
    /// Ozet uymayan profil <see cref="ModelStatus.ChecksumMismatch"/> olarak isaretlenir
    /// ve <see cref="Load"/> onu kullanmayi reddeder.
    /// </summary>
    public static ModelRegistry Create(string modelResultsPath, string modelDirectory)
    {
        ModelRegistry registry = new();

        string modelResultsChecksum = FileChecksum.Sha256(modelResultsPath);
        registry.ModelResultsChecksum = modelResultsChecksum;
        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(modelResultsPath));
        JsonElement root = document.RootElement;

        string trainer = root.GetProperty("trainer").GetProperty("name").GetString()!;
        string package = root.GetProperty("mlPackage").GetString()!;
        string codeCommit = root.GetProperty("codeCommit").GetString()!;

        Dictionary<string, string> checksums = ReadChecksums(Path.Combine(modelDirectory, "models.sha256"));

        foreach (JsonElement entry in root.GetProperty("repositories").EnumerateArray())
        {
            string identity = entry.GetProperty("repository").GetString()!;
            (string code, string _, string display) = Array.Find(Known, item => item.Identity == identity);

            if (code is null)
            {
                continue;
            }

            string path = Path.Combine(modelDirectory, code + ".zip");

            ModelProfile profile = new(
                code,
                identity,
                display,
                path,
                checksums.GetValueOrDefault(code + ".zip", string.Empty),
                modelResultsChecksum,
                entry.GetProperty("trainThreshold").GetDouble(),
                FeatureSchemaVersion,
                codeCommit,
                trainer,
                package,
                ReadCoefficients(entry),
                ReadStatistics(entry));

            registry.profiles[code] = profile;
            registry.statuses[code] = Verify(profile, out string? failure);

            if (failure is not null)
            {
                registry.failures[code] = failure;
            }

            registry.loadCounts[code] = 0;
            registry.loaders[code] = new Lazy<LoadedModel>(
                () =>
                {
                    // Sayac Lazy'nin ICINDE: "bir kez yuklendi" iddiasi varsayim degil
                    // olculen bir sayi olsun diye.
                    lock (registry.loadCounts)
                    {
                        registry.loadCounts[code]++;
                    }

                    return LoadedModel.FromFile(profile.ModelPath);
                },
                LazyThreadSafetyMode.ExecutionAndPublication);
        }

        return registry;
    }

    public ModelProfile? Find(string profileCode) => profiles.GetValueOrDefault(profileCode);

    /// <summary>Depo kimligine gore profil. Eslesme yoksa null; sessiz varsayilan YOK.</summary>
    public ModelProfile? ForRepository(string repositoryIdentity)
    {
        foreach (ModelProfile profile in profiles.Values)
        {
            if (string.Equals(profile.RepositoryIdentity, repositoryIdentity, StringComparison.OrdinalIgnoreCase))
            {
                return profile;
            }
        }

        return null;
    }

    public ModelStatus StatusOf(string profileCode) =>
        statuses.GetValueOrDefault(profileCode, ModelStatus.Failed);

    public string? FailureOf(string profileCode) => failures.GetValueOrDefault(profileCode);

    /// <summary>Modelin diskten kac kez okundugu. Bir kez yuklendigi buradan gorulur.</summary>
    public int LoadCountOf(string profileCode)
    {
        lock (loadCounts)
        {
            return loadCounts.GetValueOrDefault(profileCode);
        }
    }

    /// <summary>
    /// Modeli getirir; ilk cagriada diskten yukler, sonrakilerde ayni ornegi verir.
    /// Ozet uymuyorsa yuklemez.
    /// </summary>
    public LoadedModel Load(string profileCode)
    {
        if (StatusOf(profileCode) == ModelStatus.ChecksumMismatch)
        {
            throw new InvalidDataException(
                $"{profileCode}: model dosyasinin ozeti kayitli degerle ayni degil; model kullanilmadi.");
        }

        if (!loaders.TryGetValue(profileCode, out Lazy<LoadedModel>? loader))
        {
            throw new InvalidOperationException($"Boyle bir model profili yok: {profileCode}");
        }

        try
        {
            LoadedModel model = loader.Value;
            statuses[profileCode] = ModelStatus.Ready;

            return model;
        }
        catch (Exception error)
        {
            statuses[profileCode] = ModelStatus.Failed;
            failures[profileCode] = error.Message;

            throw;
        }
    }

    /// <summary>Profilin egitim istatistiklerinden kurulan olcekleyici.</summary>
    public FeatureScaler ScalerFor(ModelProfile profile) =>
        FeatureScaler.FromStatistics(profile.RepositoryIdentity, profile.TrainStatistics);

    private static ModelStatus Verify(ModelProfile profile, out string? failure)
    {
        failure = null;

        if (!File.Exists(profile.ModelPath))
        {
            failure = "model dosyasi bulunamadi";

            return ModelStatus.Failed;
        }

        if (profile.ModelChecksum.Length == 0)
        {
            failure = "kayitli ozet yok";

            return ModelStatus.ChecksumMismatch;
        }

        if (!string.Equals(FileChecksum.Sha256(profile.ModelPath), profile.ModelChecksum, StringComparison.OrdinalIgnoreCase))
        {
            failure = "dosyanin ozeti kayitli degerle ayni degil";

            return ModelStatus.ChecksumMismatch;
        }

        return ModelStatus.NotLoaded;
    }

    private static Dictionary<string, string> ReadChecksums(string path)
    {
        Dictionary<string, string> checksums = new(StringComparer.OrdinalIgnoreCase);

        if (!File.Exists(path))
        {
            return checksums;
        }

        foreach (string line in File.ReadLines(path))
        {
            string[] parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length == 2)
            {
                checksums[parts[1]] = parts[0];
            }
        }

        return checksums;
    }

    private static Coefficients ReadCoefficients(JsonElement entry)
    {
        JsonElement coefficients = entry.GetProperty("coefficients");
        Dictionary<string, double> weights = new(StringComparer.Ordinal);

        foreach (JsonElement weight in coefficients.GetProperty("weights").EnumerateArray())
        {
            weights[weight.GetProperty("feature").GetString()!] = weight.GetProperty("weight").GetDouble();
        }

        // Katsayilar ModelFeatures.Candidates sirasinda tutuluyor ki katki hesabi
        // oznitelik vektoruyle ayni sirayi kullansin.
        List<double> ordered = [];

        foreach (string name in ModelFeatures.Candidates)
        {
            ordered.Add(weights.TryGetValue(name, out double value)
                ? value
                : throw new InvalidDataException($"Katsayi dosyasinda {name} yok."));
        }

        return new Coefficients(coefficients.GetProperty("intercept").GetDouble(), ordered);
    }

    private static IReadOnlyList<FeatureStatistics> ReadStatistics(JsonElement entry)
    {
        List<FeatureStatistics> statistics = [];

        foreach (JsonElement value in entry.GetProperty("trainStatistics").EnumerateArray())
        {
            statistics.Add(new FeatureStatistics(
                value.GetProperty("feature").GetString()!,
                value.GetProperty("min").GetDouble(),
                value.GetProperty("max").GetDouble(),
                value.GetProperty("mean").GetDouble(),
                value.GetProperty("standardDeviation").GetDouble()));
        }

        return statistics;
    }

    /// <summary>Ondalik degerleri kulturden bagimsiz yazmak icin.</summary>
    public static string Number(double value) => value.ToString("R", CultureInfo.InvariantCulture);
}
