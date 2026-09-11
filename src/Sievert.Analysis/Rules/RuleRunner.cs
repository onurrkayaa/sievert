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
    /// Her dosyayi bir kez ayristirip butun kurallara verir. Bulgulardaki dosya yolu
    /// <paramref name="scanRoot"/> kokune gore goreli yazilir.
    /// </summary>
    public IReadOnlyList<Finding> Run(IReadOnlyList<string> filePaths, string scanRoot)
    {
        List<Finding> findings = [];

        foreach (string filePath in filePaths)
        {
            SyntaxTree tree = CSharpSyntaxTree.ParseText(File.ReadAllText(filePath), path: filePath);
            string relativePath = ScanRoot.RelativePath(filePath, scanRoot);

            foreach (IRule rule in rules)
            {
                findings.AddRange(rule.InspectFile(tree, relativePath));
            }
        }

        return findings;
    }
}
