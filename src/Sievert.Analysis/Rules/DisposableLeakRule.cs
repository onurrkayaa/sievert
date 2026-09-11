using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Sievert.Core.Rules;

namespace Sievert.Analysis.Rules;

/// <summary>SV005 - atilmasi gereken ama atilmayan nesneleri bulur.</summary>
public sealed class DisposableLeakRule : IRule
{
    public string Code => "SV005";

    public string Name => "atilmayan nesne";

    public string Description =>
        "IDisposable bir nesne atilmazsa tuttugu dosya, baglanti ya da soket isi biteceginde "
        + "degil, cop toplayici geldiginde birakilir.";

    /// <summary>
    /// Atilmasi gereken tipler. Tek liste, buyutulebilir. Semantic model olmadigi icin
    /// gercekten IDisposable olup olmadiklarina bakamiyoruz, ada bakiyoruz.
    /// </summary>
    private static readonly string[] DisposableTypeNames =
    [
        "HttpClient", "SqlConnection", "SqliteConnection", "NpgsqlConnection",
        "StreamReader", "StreamWriter", "BinaryReader", "BinaryWriter",
        "Bitmap", "Image", "Graphics", "Timer", "CancellationTokenSource", "Process",
        "FileStream", "MemoryStream", "BufferedStream", "GZipStream", "DeflateStream",
        "CryptoStream", "NetworkStream",
    ];

    /// <summary>DI kaydi yapan cagrilar. Nesnenin sahibi kap oluyor.</summary>
    private static readonly string[] ContainerRegistrations =
    [
        "AddSingleton", "AddScoped", "AddTransient", "RegisterInstance", "RegisterSingleton",
    ];

    public RuleResult Inspect(RuleContext context)
    {
        string filePath = context.File.RelativePath;

        List<Finding> findings = [];
        List<Exemption> exemptions = [];

        foreach (BaseObjectCreationExpressionSyntax creation in context.File.Tree.GetRoot()
            .DescendantNodes()
            .OfType<BaseObjectCreationExpressionSyntax>())
        {
            if (TypeNameOf(creation) is not string typeName || !IsDisposable(typeName))
            {
                continue;
            }

            string methodName = BlockingCallRule.EnclosingMethodName(creation);
            int line = BlockingCallRule.LineOf(creation);

            if (ExemptionFor(creation) is ExemptionReason reason)
            {
                exemptions.Add(new Exemption(Code, filePath, line, methodName, reason));
                continue;
            }

            findings.Add(new Finding(
                Code,
                Name,
                Message(typeName),
                Description,
                filePath,
                line,
                methodName,
                Severity.Warning));
        }

        return findings.Count == 0 && exemptions.Count == 0
            ? RuleResult.Empty
            : new RuleResult(findings, exemptions);
    }

    /// <summary>
    /// HttpClient'in dogru kullanimi zaten uzun omurlu olmak, yani her bulgu "burada
    /// using yaz" demek degil. Mesaji ayirdim ki kural yanlis tavsiye vermesin.
    /// </summary>
    private static string Message(string typeName) =>
        typeName == "HttpClient"
            ? "new HttpClient() atilmiyor. HttpClient'in dogru kullanimi zaten uzun omurlu olmak: "
              + "her cagrida yenisini acmak yerine tek bir ornegi paylas ya da IHttpClientFactory kullan."
            : $"new {typeName}() atilmiyor. using ile kapsam sonunda atmasini saglayabilirsin.";

    private static ExemptionReason? ExemptionFor(SyntaxNode creation)
    {
        foreach (SyntaxNode ancestor in creation.Ancestors())
        {
            switch (ancestor)
            {
                // using (var x = new ...) ya da using var x = new ...
                case UsingStatementSyntax:
                    return ExemptionReason.UsingScope;

                case LocalDeclarationStatementSyntax local when local.UsingKeyword != default:
                    return ExemptionReason.UsingScope;

                // var x = new ...; await using (x.ConfigureAwait(false)) - iki adimli kalip.
                case LocalDeclarationStatementSyntax local when IsUsedByALaterUsing(local):
                    return ExemptionReason.UsingScope;

                // return new ...  - sahiplik cagirana geciyor.
                case ReturnStatementSyntax:
                case ArrowExpressionClauseSyntax:
                    return ExemptionReason.CallerOwns;

                // Alan baslangic degeri: sinif sahibi.
                case FieldDeclarationSyntax:
                    return ExemptionReason.OwnedByType;

                // services.AddSingleton(new ...) - sahibi DI kabi.
                case InvocationExpressionSyntax invocation
                    when invocation.Expression is MemberAccessExpressionSyntax member
                        && ContainerRegistrations.Contains(member.Name.Identifier.ValueText, StringComparer.Ordinal):
                    return ExemptionReason.OwnedByType;

                // _shared = new ... - alana atama. Alan mi yerel mi oldugunu semantic model
                // olmadan kesin bilemiyoruz; ayni tipte ayni adda bir alan var mi diye bakiyoruz.
                case AssignmentExpressionSyntax assignment when IsFieldTarget(assignment):
                    return ExemptionReason.OwnedByType;

                case MethodDeclarationSyntax:
                case ConstructorDeclarationSyntax:
                    return null;
            }
        }

        return null;
    }

    /// <summary>
    /// Bildirilen degisken daha sonra bir <c>using</c> deyimine veriliyor mu. Nesnenin
    /// olusturulmasi ile atilmasi iki ayri satirda olabiliyor:
    /// <c>var x = new MemoryStream(); await using (x.ConfigureAwait(false)) { ... }</c>
    /// </summary>
    private static bool IsUsedByALaterUsing(LocalDeclarationStatementSyntax local)
    {
        HashSet<string> declared = local.Declaration.Variables
            .Select(variable => variable.Identifier.ValueText)
            .ToHashSet(StringComparer.Ordinal);

        return local.Parent is not null
            && local.Parent.DescendantNodes()
                .OfType<UsingStatementSyntax>()
                .Any(statement => statement.Expression is not null
                    && statement.Expression.DescendantNodesAndSelf()
                        .OfType<IdentifierNameSyntax>()
                        .Any(name => declared.Contains(name.Identifier.ValueText)));
    }

    /// <summary>
    /// Atamanin sol tarafi bu tipin bir alani mi. <c>this.X</c> her zaman alan sayiliyor;
    /// duz bir ad ise ayni tipte ayni adda bir alan bildirimi araniyor.
    /// </summary>
    private static bool IsFieldTarget(AssignmentExpressionSyntax assignment)
    {
        if (assignment.Left is MemberAccessExpressionSyntax { Expression: ThisExpressionSyntax })
        {
            return true;
        }

        if (assignment.Left is not IdentifierNameSyntax identifier)
        {
            return false;
        }

        TypeDeclarationSyntax? type = assignment.Ancestors().OfType<TypeDeclarationSyntax>().FirstOrDefault();

        return type is not null
            && type.Members
                .OfType<FieldDeclarationSyntax>()
                .SelectMany(field => field.Declaration.Variables)
                .Any(variable => variable.Identifier.ValueText == identifier.Identifier.ValueText);
    }

    /// <summary>
    /// Onceden <c>Stream</c> ekiyle biten her tip disposable sayiliyordu. Olcumde SV005'in
    /// bes yanlis pozitifinin ucu bu yuzden cikti: <c>MediaStream</c> bir veri sinifi, ekine
    /// bakilip IDisposable sanildi. Artik yalnizca listedeki adlar sayiliyor.
    /// </summary>
    private static bool IsDisposable(string typeName) =>
        DisposableTypeNames.Contains(typeName, StringComparer.Ordinal);

    /// <summary>Tipin nitelendirilmemis adi. <c>new System.Text.StringBuilder()</c> icin StringBuilder.</summary>
    private static string? TypeNameOf(BaseObjectCreationExpressionSyntax creation) =>
        creation is ObjectCreationExpressionSyntax { Type: var type }
            ? type switch
            {
                IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
                GenericNameSyntax generic => generic.Identifier.ValueText,
                QualifiedNameSyntax qualified => qualified.Right.Identifier.ValueText,
                _ => null,
            }
            : null;
}
