using Sievert.Data;

namespace Sievert.Api;

/// <summary>
/// Baglanti dizesinin API icindeki tek tasiyicisi.
///
/// Dize koda yazilmiyor ve gunluge basilmiyor; yalnizca nereden okundugu
/// (<see cref="Source"/>) gorunur. Bulunamazsa API yine ayaga kalkiyor ama veritabani
/// isteyen her uc <c>DATABASE_NOT_READY</c> donuyor; boylece saglik ucu hatanin ne
/// oldugunu soyleyebiliyor.
/// </summary>
public sealed record DatabaseSettings(string? Value, string? Source, string? Error)
{
    public bool IsConfigured => !string.IsNullOrWhiteSpace(Value);

    public static DatabaseSettings Resolve(string workingDirectory)
    {
        ConnectionStringResult result = ConnectionString.Find(workingDirectory);

        return new DatabaseSettings(result.Value, result.Source, Sanitise(result.Error, workingDirectory));
    }

    /// <summary>
    /// Hata metnindeki mutlak yolu goreli hale getirir. Metin saglik ucunda ve <c>503</c>
    /// cevabinda goruluyor; sunucunun klasor duzeni oradan okunmamali.
    /// </summary>
    private static string? Sanitise(string? error, string workingDirectory) =>
        error?.Replace(workingDirectory, ".", StringComparison.Ordinal);
}
