using Sievert.Core.Rules;

namespace Sievert.Core.Configuration;

/// <summary>
/// Tek bir kural icin yapilandirma. Burada sadece kuralin acik mi kapali mi oldugu ve
/// bulgularinin ne kadar ciddi sayilacagi var; kuralin ne aradigi kodda duruyor (ADR 0009).
/// </summary>
/// <param name="Code">Kural kodu, ornegin "SV001".</param>
/// <param name="Enabled">Kural calissin mi. Yazilmazsa true.</param>
/// <param name="Severity">Bulgularin seviyesini ezer. Yazilmazsa kuralin kendi seviyesi kalir.</param>
public sealed record RuleSetting(string Code, bool Enabled = true, Severity? Severity = null);

/// <summary>sievert.json dosyasinin karsiligi.</summary>
/// <param name="Rules">Kural ayarlari. Listede olmayan kurallar varsayilan haliyle calisir.</param>
/// <param name="Exclude">Tarama disi birakilacak kaliplar. --exclude ile birlesir, onu ezmez.</param>
public sealed record SievertConfig(IReadOnlyList<RuleSetting> Rules, IReadOnlyList<string> Exclude)
{
    /// <summary>Dosya hic yoksa gecerli olan ayarlar: butun kurallar acik, hicbir sey dislanmiyor.</summary>
    public static readonly SievertConfig Default = new([], []);
}

/// <summary>Yapilandirma okuma sonucu: ya ayarlar ya da kullaniciya gosterilecek bir hata.</summary>
/// <param name="Config">Basarili okumada dolu olur.</param>
/// <param name="Error">Basarisiz okumada dolu olur.</param>
public sealed record ConfigLoadResult(SievertConfig? Config, string? Error);
