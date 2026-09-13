namespace Sievert.Contracts;

/// <summary>Saglik cevabi. Baglanti dizesi ve dosya yolu ICERMEZ.</summary>
public sealed record HealthResponse(
    string Status,
    string Version,
    DatabaseHealth Database,
    string ModelMetadataVersion,
    IReadOnlyList<ModelHealth> Models,
    AnalysisHealth Analysis,
    ProcessHealth Process);

/// <summary>
/// Surecin bellek durumu.
///
/// Yol, kullanici adi ya da makine adi yok; yalnizca sayilar. Iki yerde ise yariyor:
/// panelin sistem sayfasi ve bellek olcumu. Olcum disaridan yalnizca isletim sisteminin
/// verdigi calisma kumesini gorebiliyor, yonetilen yigin ile toplama sayaclarini goremiyor.
/// </summary>
/// <param name="WorkingSetBytes">Isletim sisteminin gordugu calisma kumesi.</param>
/// <param name="ManagedHeapBytes"><c>GC.GetTotalMemory(false)</c>; toplama zorlanmadan.</param>
public sealed record ProcessHealth(
    long WorkingSetBytes,
    long ManagedHeapBytes,
    int Gen0Collections,
    int Gen1Collections,
    int Gen2Collections,
    double UptimeSeconds);

public sealed record DatabaseHealth(bool Reachable, bool MigrationsApplied, string? Detail);

public sealed record ModelHealth(string Code, string Status, string? Detail);
