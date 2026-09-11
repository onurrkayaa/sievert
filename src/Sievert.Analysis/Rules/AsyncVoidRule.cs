using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Sievert.Core.Rules;

namespace Sievert.Analysis.Rules;

/// <summary>SV001 - async isaretli ama void donen metotlari bulur.</summary>
public sealed class AsyncVoidRule : IRule
{
    public string Code => "SV001";

    public string Name => "async void metot";

    public string Description =>
        "async void bir metotta olusan hata cagirana ulasmaz, yakalanamadigi icin uygulamayi dusurur.";

    public IReadOnlyList<Finding> InspectFile(SyntaxTree tree, string filePath) =>
        tree.GetRoot()
            .DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .Where(method => IsAsyncVoid(method) && !LooksLikeEventHandler(method))
            .Select(method => ToFinding(method, filePath))
            .ToList();

    private Finding ToFinding(MethodDeclarationSyntax method, string filePath)
    {
        string name = method.Identifier.ValueText;

        return new Finding(
            Code,
            Name,
            $"{name} metodu async void. Donus tipini Task yaparsan hatalar cagirana ulasir.",
            filePath,
            method.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
            name,
            Severity.Error);
    }

    /// <summary>
    /// Iki parametre alan ve ikincisinin tip adi EventArgs ile biten metotlari event handler
    /// sayar. Heuristik: semantic model olmadigi icin gercek tip hiyerarsisine bakamiyoruz,
    /// sadece kaynakta yazan ada bakiyoruz.
    /// </summary>
    private static bool LooksLikeEventHandler(MethodDeclarationSyntax method)
    {
        SeparatedSyntaxList<ParameterSyntax> parameters = method.ParameterList.Parameters;

        return parameters.Count == 2
            && parameters[1].Type?.ToString().EndsWith("EventArgs", StringComparison.Ordinal) == true;
    }

    private static bool IsAsyncVoid(MethodDeclarationSyntax method) =>
        method.Modifiers.Any(SyntaxKind.AsyncKeyword)
        && method.ReturnType is PredefinedTypeSyntax returnType
        && returnType.Keyword.IsKind(SyntaxKind.VoidKeyword);
}
