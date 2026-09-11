using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
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
    /// <paramref name="scanRoot"/> kokune gore goreli yazilir. Agaci ayristirirken
    /// dosyanin diskteki tam yolu veriliyor; dosya sinirini asmasi gereken kurallar
    /// (ornegin SV001'in partial sinif aramasi) o yolu kullaniyor.
    /// </summary>
    public RuleResult Run(IReadOnlyList<string> filePaths, string scanRoot)
    {
        List<Finding> findings = [];
        List<Exemption> exemptions = [];

        foreach (string filePath in filePaths)
        {
            SyntaxTree tree = CSharpSyntaxTree.ParseText(File.ReadAllText(filePath), path: filePath);
            string relativePath = ScanRoot.RelativePath(filePath, scanRoot);

            foreach (IRule rule in rules)
            {
                RuleResult result = rule.InspectFile(tree, relativePath);

                findings.AddRange(result.Findings);
                exemptions.AddRange(result.Exemptions);
            }
        }

        return new RuleResult(findings, exemptions);
    }
}
