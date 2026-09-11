using Sievert.Core.Rules;

namespace Sievert.Cli;

/// <summary>check komutunun cikis kodu karari. Gerekcesi ADR 0006'da.</summary>
public static class CheckCommand
{
    /// <summary>
    /// <paramref name="failOn"/> seviyesinde ya da ustunde en az bir bulgu varsa 1, yoksa 0.
    /// Esigin altinda kalan bulgular yine ekrana basiliyor, sadece build'i kirmiyorlar.
    /// </summary>
    public static int ExitCode(IReadOnlyList<Finding> findings, Severity failOn) =>
        findings.Any(finding => finding.Severity >= failOn) ? 1 : 0;
}
