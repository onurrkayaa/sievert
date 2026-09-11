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
        List<Suppression> suppressions = [];

        foreach (RuleContext context in contexts)
        {
            List<Finding> fromThisFile = [];

            foreach (IRule rule in rules)
            {
                RuleResult result = rule.Inspect(context);

                fromThisFile.AddRange(result.Findings);
                exemptions.AddRange(result.Exemptions);
            }

            // Susturma kurallardan sonra uygulaniyor: kural bulgusunu her zaman uretiyor,
            // elenip elenmedigine burasi karar veriyor ve elenen her bulgu kaydediliyor.
            findings.AddRange(Apply(SuppressionReader.Read(context.File.Tree), fromThisFile, suppressions));
        }

        return new RuleResult(findings, exemptions) { Suppressions = suppressions };
    }

    /// <summary>
    /// Gecerli susturmalarla eslesen bulgulari ayirir. Gerekcesiz susturmalar burada hicbir
    /// sey elemiyor; onlari SV007 ayrica bulgu olarak raporluyor.
    /// </summary>
    private static List<Finding> Apply(
        IReadOnlyList<SuppressionComment> comments,
        IReadOnlyList<Finding> findings,
        List<Suppression> suppressed)
    {
        List<SuppressionComment> valid = [.. comments.Where(comment => comment.HasReason)];

        if (valid.Count == 0)
        {
            return [.. findings];
        }

        List<Finding> kept = [];

        foreach (Finding finding in findings)
        {
            // sievert:disable SV004 valid bellekteki kisa bir liste, veritabani sorgusu degil
            SuppressionComment? match = valid.FirstOrDefault(comment =>
                comment.SuppressedLine == finding.Line
                && string.Equals(comment.RuleCode, finding.RuleCode, StringComparison.Ordinal));

            if (match is null)
            {
                kept.Add(finding);
                continue;
            }

            suppressed.Add(new Suppression(finding.RuleCode, finding.FilePath, finding.Line, match.Reason));
        }

        return kept;
    }
}
