using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Sievert.Core.Analysis;
using Sievert.Core.Rules;

namespace Sievert.Analysis.Rules;

/// <summary>SV006 - Task donen public metotlarda iptal destegi olmamasini bulur.</summary>
public sealed class CancellationRule : IRule
{
    public string Code => "SV006";

    public string Name => "iptal edilemeyen islem";

    public string Description =>
        "Uzun surebilecek bir islemi cagiran taraf iptal edemezse, is bitene kadar beklemekten "
        + "baska secenegi kalmiyor.";

    /// <summary>
    /// Seviye <c>info</c>: bu bir hata degil, eksik bir yetenek. Kural cok bulgu uretiyor ve
    /// error ya da warning olsaydi --fail-on'un varsayilani (warning) yuzunden herkesin
    /// build'ini kirardi. Info olarak ekranda gorunuyor ama kimseyi durdurmuyor.
    /// </summary>
    public RuleResult Inspect(RuleContext context)
    {
        string filePath = context.File.RelativePath;
        bool isTestCode = FileAnalysis.IsTestCodePath(filePath);

        List<Finding> findings = [];
        List<Exemption> exemptions = [];

        foreach (MethodDeclarationSyntax method in context.File.Tree.GetRoot()
            .DescendantNodes()
            .OfType<MethodDeclarationSyntax>())
        {
            if (!ReturnsTask(method) || HasCancellationToken(method))
            {
                continue;
            }

            if (ExemptionFor(method, isTestCode) is ExemptionReason reason)
            {
                exemptions.Add(new Exemption(
                    Code,
                    filePath,
                    BlockingCallRule.LineOf(method),
                    method.Identifier.ValueText,
                    reason));
                continue;
            }

            findings.Add(new Finding(
                Code,
                Name,
                $"{method.Identifier.ValueText} metodu Task donuyor ama CancellationToken almiyor. "
                + "Parametre eklersen cagiran islemi iptal edebilir.",
                Description,
                filePath,
                BlockingCallRule.LineOf(method),
                method.Identifier.ValueText,
                Severity.Info));
        }

        return findings.Count == 0 && exemptions.Count == 0
            ? RuleResult.Empty
            : new RuleResult(findings, exemptions);
    }

    private static ExemptionReason? ExemptionFor(MethodDeclarationSyntax method, bool isTestCode)
    {
        // Imza baska bir yerden geliyorsa parametre eklenemez. Explicit arayuz uygulamasi ve
        // override sozdiziminden kesin anlasiliyor; siradan (ortuk) arayuz uygulamasini
        // semantic model olmadan goremiyoruz, bu bir sinirlilik olarak yazildi.
        if (method.ExplicitInterfaceSpecifier is not null
            || method.Modifiers.Any(SyntaxKind.OverrideKeyword))
        {
            return ExemptionReason.InheritedSignature;
        }

        if (!method.Modifiers.Any(SyntaxKind.PublicKeyword))
        {
            return ExemptionReason.NotPublic;
        }

        return isTestCode ? ExemptionReason.TestCode : null;
    }

    /// <summary>
    /// Donus tipi Task ya da ValueTask mi. Kaynakta yazan ada bakiyoruz; takma ad verilmis
    /// ya da baska bir awaitable donen metotlar gorunmuyor.
    /// </summary>
    private static bool ReturnsTask(MethodDeclarationSyntax method)
    {
        string name = method.ReturnType switch
        {
            GenericNameSyntax generic => generic.Identifier.ValueText,
            IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
            QualifiedNameSyntax qualified => qualified.Right.Identifier.ValueText,
            _ => string.Empty,
        };

        return name is "Task" or "ValueTask";
    }

    private static bool HasCancellationToken(MethodDeclarationSyntax method) =>
        method.ParameterList.Parameters.Any(parameter =>
            parameter.Type?.ToString().EndsWith("CancellationToken", StringComparison.Ordinal) == true);
}
