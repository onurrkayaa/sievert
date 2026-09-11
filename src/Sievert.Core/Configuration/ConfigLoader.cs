using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sievert.Core.Configuration;

/// <summary>sievert.json dosyasini okur.</summary>
public static class ConfigLoader
{
    /// <summary>Taranan kokte aranan dosyanin adi.</summary>
    public const string FileName = "sievert.json";

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = false,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = false,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
        // Tanimadigimiz bir alan sessizce yutulmasin. "excludes" diye yazan biri hicbir
        // seyin dislanmadigini fark etmezdi; yazim hatasi hata olarak donsun.
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    };

    /// <summary>
    /// Kokteki sievert.json'i okur. Dosya yoksa bu bir hata degil: varsayilanlar doner.
    /// Yapilandirma opsiyonel olsun istedim, arac dosyasiz da calissin.
    /// </summary>
    public static ConfigLoadResult LoadFromRoot(string root)
    {
        string path = Path.Combine(root, FileName);

        return File.Exists(path) ? Read(path) : new ConfigLoadResult(SievertConfig.Default, null);
    }

    /// <summary>
    /// --config ile acikca verilen dosyayi okur. Burada dosyanin olmamasi hata: kullanici
    /// bir yol yazdiysa o yolun tutmasini bekliyordur.
    /// </summary>
    public static ConfigLoadResult LoadFile(string path) =>
        File.Exists(path)
            ? Read(path)
            : new ConfigLoadResult(null, $"Yapilandirma dosyasi bulunamadi: {path}");

    /// <summary>
    /// Dosyanin birebir karsiligi. Ayri bir tip, cunku JSON'da yazilmayan alanlar null
    /// geliyor; SievertConfig'i null kabul eden bir tip yapmak yerine donusumu burada
    /// yapiyorum. Alan adlari ve isimlendirme SievertConfig ile ayni kalmali.
    /// </summary>
    private sealed record ConfigFile(IReadOnlyList<RuleSetting>? Rules, IReadOnlyList<string>? Exclude);

    private static ConfigLoadResult Read(string path)
    {
        try
        {
            ConfigFile? file = JsonSerializer.Deserialize<ConfigFile>(File.ReadAllText(path), Options);

            if (file is null)
            {
                return new ConfigLoadResult(null, $"{path} bos.");
            }

            SievertConfig config = new(file.Rules ?? [], file.Exclude ?? []);

            if (config.Rules.Any(rule => string.IsNullOrWhiteSpace(rule.Code)))
            {
                return new ConfigLoadResult(null, $"{path}: her kural kaydinda \"code\" olmali.");
            }

            return new ConfigLoadResult(config, null);
        }
        catch (JsonException error)
        {
            // Satir numarasi olmadan bozuk bir JSON'da hatayi aramak zor oluyor.
            string where = error.LineNumber is long line
                ? $" (satir {line + 1})"
                : string.Empty;

            return new ConfigLoadResult(null, $"{path} okunamadi{where}: {error.Message}");
        }
    }
}
