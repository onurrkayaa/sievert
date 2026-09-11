using Sievert.Core.Rules;

namespace Sievert.Analysis.Rules;

/// <summary>
/// Elindeki kurallari verilen dosyalar uzerinde calistirir ve butun bulgulari toplar.
/// Kural listesini disaridan aliyor, icinde sabit bir liste tutmuyor.
/// </summary>
public sealed class RuleRunner(IReadOnlyList<IRule> rules)
{
    /// <summary>
    /// Her dosyayi bir kez ayristirip butun kurallara verir. Yollar
    /// <paramref name="scanRoot"/> kokune gore goreli yazilir. Dosya sinirini asmasi
    /// gereken kurallar (ornegin SV001'in partial sinif aramasi) komsu dosyalari
    /// baglamdan aliyor, diskten degil.
    /// </summary>
    public RuleResult Run(IReadOnlyList<string> filePaths, string scanRoot) =>
        Run(ScannedFileSet.Contexts(ScannedFileSet.Parse(filePaths, scanRoot)));

    /// <summary>Baglamlari hazir olan bir kume uzerinde calistirir.</summary>
    public RuleResult Run(IReadOnlyList<RuleContext> contexts)
    {
        List<Finding> findings = [];
        List<Exemption> exemptions = [];

        foreach (RuleContext context in contexts)
        {
            foreach (IRule rule in rules)
            {
                RuleResult result = rule.Inspect(context);

                findings.AddRange(result.Findings);
                exemptions.AddRange(result.Exemptions);
            }
        }

        return new RuleResult(findings, exemptions);
    }
}
