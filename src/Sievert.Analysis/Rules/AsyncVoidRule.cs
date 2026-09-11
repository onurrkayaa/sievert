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

    /// <summary>
    /// Muafiyet iki ayri kanit kaynagindan gelebiliyor, biri yetiyor: metodun imzasi event
    /// handler kalibina uyuyorsa, ya da metoda bir yerde <c>+= MetotAdi</c> ile abone
    /// olunuyorsa. Ikisinin de neden gerektigi ADR 0008'de.
    /// </summary>
    public RuleResult Inspect(RuleContext context)
    {
        string filePath = context.File.RelativePath;
        SyntaxNode root = context.File.Tree.GetRoot();

        List<MethodDeclarationSyntax> candidates = root
            .DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .Where(IsAsyncVoid)
            .ToList();

        if (candidates.Count == 0)
        {
            return RuleResult.Empty;
        }

        List<Finding> findings = [];
        List<Exemption> exemptions = [];

        HashSet<string> subscribedInThisFile = EventSubscriptionSearch.NamesIn(root);

        // Komsu dosyalar taraniyor, ayni tip icin bir kez.
        Dictionary<string, HashSet<string>> subscribedInPartialParts = new(StringComparer.Ordinal);

        foreach (MethodDeclarationSyntax method in candidates)
        {
            if (LooksLikeEventHandler(method))
            {
                exemptions.Add(ToExemption(method, filePath, ExemptionReason.Signature));
                continue;
            }

            if (IsSubscribed(method, context, subscribedInThisFile, subscribedInPartialParts))
            {
                exemptions.Add(ToExemption(method, filePath, ExemptionReason.Subscription));
                continue;
            }

            findings.Add(ToFinding(method, filePath));
        }

        return new RuleResult(findings, exemptions);
    }

    /// <summary>
    /// Metoda kendi dosyasinda ya da ayni partial sinifin baska bir parcasinda abone
    /// olunuyor mu. Parcalar baglamdaki komsu dosyalardan bulunuyor; taranmayan bir dosya
    /// baglamda olmadigi icin kanit da uretmiyor.
    /// </summary>
    private static bool IsSubscribed(
        MethodDeclarationSyntax method,
        RuleContext context,
        HashSet<string> subscribedInThisFile,
        Dictionary<string, HashSet<string>> subscribedInPartialParts)
    {
        string name = method.Identifier.ValueText;

        if (subscribedInThisFile.Contains(name))
        {
            return true;
        }

        // Partial olmayan bir sinifin baska dosyada parcasi olamaz; komsulara hic bakmiyoruz.
        if (method.Parent is not TypeDeclarationSyntax type
            || !type.Modifiers.Any(SyntaxKind.PartialKeyword))
        {
            return false;
        }

        string typeName = type.Identifier.ValueText;

        if (!subscribedInPartialParts.TryGetValue(typeName, out HashSet<string>? names))
        {
            names = EventSubscriptionSearch.NamesInPartialParts(context.FilesInSameFolder, typeName);
            subscribedInPartialParts[typeName] = names;
        }

        return names.Contains(name);
    }

    private Finding ToFinding(MethodDeclarationSyntax method, string filePath)
    {
        string name = method.Identifier.ValueText;

        return new Finding(
            Code,
            Name,
            $"{name} metodu async void. Donus tipini Task yaparsan hatalar cagirana ulasir.",
            Description,
            filePath,
            LineOf(method),
            name,
            Severity.Error);
    }

    private Exemption ToExemption(MethodDeclarationSyntax method, string filePath, ExemptionReason reason) =>
        new(Code, filePath, LineOf(method), method.Identifier.ValueText, reason);

    /// <summary>
    /// Metodun IMZA satiri. Dugumun konumu oznitelik listesinden basladigi icin
    /// dogrudan kullanilamiyor; ayrintisi <see cref="BlockingCallRule.LineOf(MethodDeclarationSyntax)"/>.
    /// </summary>
    private static int LineOf(MethodDeclarationSyntax method) =>
        BlockingCallRule.LineOf(method);

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
