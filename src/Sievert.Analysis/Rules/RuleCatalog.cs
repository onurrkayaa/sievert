using Sievert.Core.Configuration;
using Sievert.Core.Rules;

namespace Sievert.Analysis.Rules;

/// <summary>
/// Secilmis kurallar ve onlara uygulanacak seviye ezmeleri.
/// </summary>
/// <param name="Enabled">Calistirilacak kurallar, kod sirasina gore.</param>
/// <param name="SeverityOverrides">Kural kodundan ezilen seviyeye. Ezme yoksa bos.</param>
/// <param name="DisabledCodes">Yapilandirmayla kapatilmis kural kodlari, ozette gorunsun diye.</param>
public sealed record RuleSelection(
    IReadOnlyList<IRule> Enabled,
    IReadOnlyDictionary<string, Severity> SeverityOverrides,
    IReadOnlyList<string> DisabledCodes)
{
    /// <summary>
    /// Ezilen seviyeleri bulgulara uygular. Kural kendi seviyesini uretiyor, ezme sonradan
    /// yaziliyor; boylece kural kodunda "yapilandirma" diye bir kavram yok. Bulgu nesnesi
    /// degistigi icin --fail-on karsilastirmasi da ezilmis seviyeye gore yapiliyor.
    /// </summary>
    public IReadOnlyList<Finding> ApplySeverity(IReadOnlyList<Finding> findings)
    {
        if (SeverityOverrides.Count == 0)
        {
            return findings;
        }

        return findings
            .Select(finding => SeverityOverrides.TryGetValue(finding.RuleCode, out Severity severity)
                ? finding with { Severity = severity }
                : finding)
            .ToList();
    }
}

/// <summary>Secim sonucu: ya secim ya da kullaniciya gosterilecek bir hata.</summary>
public sealed record RuleSelectionResult(RuleSelection? Selection, string? Error);

/// <summary>
/// Kural kodundan kurala tek eslesme yeri. Yeni bir kural yazilinca sadece buraya
/// eklenecek; CLI'da ya da baska bir yerde ikinci bir liste tutulmuyor.
/// </summary>
public static class RuleCatalog
{
    private static readonly IRule[] All =
    [
        new AsyncVoidRule(),
        new BlockingCallRule(),
        new MissingAwaitRule(),
        new NPlusOneRule(),
        new DisposableLeakRule(),
        new CancellationRule(),
    ];

    /// <summary>Tanidigimiz butun kural kodlari, sirali.</summary>
    public static IReadOnlyList<string> KnownCodes { get; } =
        All.Select(rule => rule.Code).Order(StringComparer.Ordinal).ToArray();

    /// <summary>
    /// Varsayilan olarak KAPALI gelen kurallar. Yapilandirmayla acilabiliyorlar.
    ///
    /// Varsayilan kume "tanidigim her kural" degil, olculmus precision'a gore secilmis bir
    /// kume. Jellyfin uzerinde her kuraldan bes bulguya elle bakildi (bkz.
    /// docs/olcumler/asama3-precision.md): SV003 %0, SV005 %0 cikti. Ikisinin de yanlis
    /// pozitifleri tek cinsten ve duzeltilebilir, ama duzeltme yazilana kadar acik durmalari
    /// kullanicinin gordugu bulgularin yarisini gurultuye cevirir.
    ///
    /// SV004 de esigin altinda (%20) ama acik birakildi: yanlis pozitiflerinin tek bir kok
    /// nedeni var (ad, tipin yerine kullaniliyor) ve o neden giderilebilir durumda.
    /// </summary>
    public static IReadOnlyList<string> DefaultOffCodes { get; } = ["SV003", "SV005"];

    /// <summary>
    /// Yapilandirmayi katalogla birlestirir. Listede adi gecmeyen kurallar varsayilan
    /// haliyle kaliyor; yapilandirma bir izin listesi degil, sadece istisna listesi.
    /// Varsayilan hal cogu kural icin acik, <see cref="DefaultOffCodes"/> icin kapali.
    /// </summary>
    public static RuleSelectionResult Select(SievertConfig config)
    {
        Dictionary<string, Severity> overrides = new(StringComparer.Ordinal);

        // Varsayilan kapali kurallarla basliyoruz; yapilandirma bunlari acabilir.
        HashSet<string> disabled = new(DefaultOffCodes, StringComparer.Ordinal);

        foreach (RuleSetting setting in config.Rules)
        {
            if (!KnownCodes.Contains(setting.Code, StringComparer.Ordinal))
            {
                // Sessizce atlasaydik yazim hatasi kurali kapatmis gibi gorunurdu.
                return new RuleSelectionResult(
                    null,
                    $"Bilinmeyen kural kodu: {setting.Code}. Tanidigim kurallar: {string.Join(", ", KnownCodes)}.");
            }

            if (!setting.Enabled)
            {
                disabled.Add(setting.Code);
                continue;
            }

            // Adi gecen ve kapatilmamis bir kural aciliyor: varsayilan kapali olsa bile.
            disabled.Remove(setting.Code);

            if (setting.Severity is Severity severity)
            {
                overrides[setting.Code] = severity;
            }
        }

        IReadOnlyList<IRule> enabled = All
            .Where(rule => !disabled.Contains(rule.Code))
            .OrderBy(rule => rule.Code, StringComparer.Ordinal)
            .ToList();

        return new RuleSelectionResult(
            new RuleSelection(enabled, overrides, disabled.Order(StringComparer.Ordinal).ToArray()),
            null);
    }
}
