using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Sievert.Core.Rules;

namespace Sievert.Analysis.Rules;

/// <summary>SV002 - bir gorevin sonucunu bloklayarak bekleyen cagrilari bulur.</summary>
public sealed class BlockingCallRule : IRule
{
    public string Code => "SV002";

    public string Name => "bloklayan gorev beklemesi";

    public string Description =>
        "Bir gorevi .Result, .Wait() ya da .GetAwaiter().GetResult() ile beklemek, gorev ayni "
        + "baglama donmeye calistiginda kilitlenmeye yol acabilir.";

    /// <summary>
    /// Semantic model olmadigi icin ifadenin gercekten Task olup olmadigini bilemiyoruz;
    /// karar ada bakiyor (bkz. <see cref="LooksLikeTask"/>). Tek muafiyet Main.
    /// </summary>
    public RuleResult Inspect(RuleContext context)
    {
        string filePath = context.File.RelativePath;

        List<Finding> findings = [];
        List<Exemption> exemptions = [];

        foreach (ExpressionSyntax blocked in context.File.Tree.GetRoot()
            .DescendantNodes()
            .Select(BlockedTaskExpression)
            .OfType<ExpressionSyntax>())
        {
            if (!LooksLikeTask(blocked))
            {
                continue;
            }

            string methodName = EnclosingMethodName(blocked);
            int line = LineOf(blocked);

            // Main'de bloklamak mesru olabiliyor: konsol girisinde geri donulecek bir
            // senkronizasyon baglami yok, yani klasik kilitlenme orada olusmuyor.
            if (methodName == "Main")
            {
                exemptions.Add(new Exemption(Code, filePath, line, methodName, ExemptionReason.EntryPoint));
                continue;
            }

            findings.Add(new Finding(
                Code,
                Name,
                $"{Describe(blocked)} gorevi bloklayarak bekliyor. await kullanirsan is parcacigi serbest kalir.",
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
    /// Dugum bloklayan bir bekleme ise, uzerinde beklenen ifadeyi doner; degilse null.
    /// Uc kalip: <c>x.Result</c>, <c>x.Wait()</c> ve <c>x.GetAwaiter().GetResult()</c>.
    /// </summary>
    private static ExpressionSyntax? BlockedTaskExpression(SyntaxNode node) => node switch
    {
        // x.GetAwaiter().GetResult() - once bunu deniyoruz, cunku ayni zamanda bir Wait
        // gibi gorunmesin diye zincirin tamamini cozmek gerekiyor.
        InvocationExpressionSyntax
        {
            Expression: MemberAccessExpressionSyntax
            {
                Name.Identifier.ValueText: "GetResult",
                Expression: InvocationExpressionSyntax
                {
                    Expression: MemberAccessExpressionSyntax
                    {
                        Name.Identifier.ValueText: "GetAwaiter",
                        Expression: ExpressionSyntax awaited,
                    },
                },
            },
        } => awaited,

        // x.Wait()
        InvocationExpressionSyntax
        {
            ArgumentList.Arguments.Count: 0,
            Expression: MemberAccessExpressionSyntax
            {
                Name.Identifier.ValueText: "Wait",
                Expression: ExpressionSyntax waited,
            },
        } => waited,

        // x.Result - cagri degil, property erisimi. Zincirin parcasi olanlari elemek
        // icin ustundeki dugume bakiyoruz: GetAwaiter().GetResult() zaten yukarida yakalandi.
        MemberAccessExpressionSyntax
        {
            Name.Identifier.ValueText: "Result",
            Expression: ExpressionSyntax source,
        } => source,

        _ => null,
    };

    /// <summary>
    /// Ifade Task gibi duruyor mu. Elimizde sadece kaynakta yazan ad var: adi Async ile
    /// biten bir cagri, adi Task ile biten ya da Task olan bir tanimlayici, ve Task tipi
    /// uzerinden yapilan cagrilar (Task.Run gibi) Task sayiliyor. Bu heuristik iki yone de
    /// yanilir, ayrintisi docs/sinirliliklar.md'de.
    /// </summary>
    private static bool LooksLikeTask(ExpressionSyntax expression) => expression switch
    {
        InvocationExpressionSyntax invocation => LooksLikeTask(invocation.Expression),
        MemberAccessExpressionSyntax member =>
            IsTaskName(member.Name.Identifier.ValueText) || IsTaskName(member.Expression.ToString()),
        IdentifierNameSyntax identifier => IsTaskName(identifier.Identifier.ValueText),
        ParenthesizedExpressionSyntax parenthesized => LooksLikeTask(parenthesized.Expression),
        _ => false,
    };

    private static bool IsTaskName(string name) =>
        name.EndsWith("Async", StringComparison.Ordinal)
        || name.EndsWith("Task", StringComparison.OrdinalIgnoreCase)
        || name.EndsWith("ValueTask", StringComparison.OrdinalIgnoreCase);

    /// <summary>Bulgu mesajinda gecen kisa ifade. Cok uzun zincirler kirpiliyor.</summary>
    private static string Describe(ExpressionSyntax expression)
    {
        string text = expression.ToString();

        return text.Length <= 40 ? text : text[..40] + "...";
    }

    /// <summary>
    /// Ifadenin icinde bulundugu metodun adi. Metot disinda (ornegin alan baslangic
    /// degerinde) duran bir ifade icin bos doner.
    /// </summary>
    internal static string EnclosingMethodName(SyntaxNode node) =>
        node.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault()?.Identifier.ValueText
        ?? node.Ancestors().OfType<LocalFunctionStatementSyntax>().FirstOrDefault()?.Identifier.ValueText
        ?? string.Empty;

    internal static int LineOf(SyntaxNode node) =>
        node.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
}
