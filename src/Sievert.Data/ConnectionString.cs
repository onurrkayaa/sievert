namespace Sievert.Data;

/// <summary>Baglanti dizesinin nereden okundugu ve bulunamadiysa ne yazilacagi.</summary>
/// <param name="Value">Bulunduysa baglanti dizesi.</param>
/// <param name="Source">Nereden geldigi; kullaniciya "hangi ayar okundu" diye gosteriliyor.</param>
/// <param name="Error">Bulunamadiysa kullaniciya gosterilecek metin.</param>
public sealed record ConnectionStringResult(string? Value, string? Source, string? Error);

/// <summary>
/// Baglanti dizesini bulur. Kodda yazili bir dize yok ve olmayacak: sifre iceren bir
/// dizeyi kaynak koda yazmak, repo public oldugu icin dogrudan sizdirmak demek.
/// </summary>
public static class ConnectionString
{
    /// <summary>Once bakilan ortam degiskeni.</summary>
    public const string EnvironmentVariable = "SIEVERT_DB";

    /// <summary>Ortam degiskeni yoksa bakilan dosya ve icindeki alan.</summary>
    public const string SettingsFileName = "appsettings.json";

    private const string SettingsSection = "ConnectionStrings";
    private const string SettingsKey = "Sievert";

    /// <summary>
    /// Once <c>SIEVERT_DB</c>, sonra calisilan klasordeki <c>appsettings.json</c>.
    /// Ikisi de yoksa <see cref="ConnectionStringResult.Error"/> dolu doner ve cagiran
    /// cikis kodu 2 ile durur.
    /// </summary>
    public static ConnectionStringResult Find(string workingDirectory)
    {
        if (Environment.GetEnvironmentVariable(EnvironmentVariable) is string fromEnvironment
            && !string.IsNullOrWhiteSpace(fromEnvironment))
        {
            return new ConnectionStringResult(fromEnvironment, EnvironmentVariable, null);
        }

        string settingsPath = Path.Combine(workingDirectory, SettingsFileName);

        if (File.Exists(settingsPath))
        {
            if (ReadFromSettings(settingsPath) is string fromFile && !string.IsNullOrWhiteSpace(fromFile))
            {
                return new ConnectionStringResult(fromFile, settingsPath, null);
            }

            return new ConnectionStringResult(
                null,
                null,
                $"{settingsPath} okundu ama icinde {SettingsSection}:{SettingsKey} alani yok.");
        }

        return new ConnectionStringResult(
            null,
            null,
            $"Veritabani baglantisi bulunamadi. {EnvironmentVariable} ortam degiskenini ver ya da "
            + $"calistigin klasore {SettingsFileName} koyup icine {SettingsSection}:{SettingsKey} yaz. "
            + "Ornek: SIEVERT_DB=\"Host=localhost;Port=5433;Database=sievert;Username=postgres;Password=...\"");
    }

    private static string? ReadFromSettings(string path)
    {
        using FileStream stream = File.OpenRead(path);
        using System.Text.Json.JsonDocument document = System.Text.Json.JsonDocument.Parse(stream);

        return document.RootElement.TryGetProperty(SettingsSection, out System.Text.Json.JsonElement section)
            && section.TryGetProperty(SettingsKey, out System.Text.Json.JsonElement value)
                ? value.GetString()
                : null;
    }
}
