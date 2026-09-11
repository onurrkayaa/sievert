using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Sievert.Core.Rules;

namespace Sievert.Analysis.Rules;

/// <summary>SV003 - donen gorevi hicbir sey yapmadan birakan cagrilari bulur.</summary>
public sealed class MissingAwaitRule : IRule
{
    public string Code => "SV003";

    public string Name => "kayip gorev";

    public string Description =>
        "Donen gorev beklenmezse icindeki hata kimseye ulasmaz ve is, cagiran bittikten "
        + "sonra yarim kalabilir.";

    /// <summary>
    /// Semantic model olmadigi icin metodun gercekten Task donup donmedigini bilemiyoruz;
    /// adi Async ile bitiyorsa Task donuyor sayiyoruz. Iki muafiyet var: sonucun <c>_ =</c>
    /// ile bilincli atilmasi ve cagrinin <c>Task.Run(...)</c> icinde olmasi.
    /// </summary>
    public RuleResult Inspect(RuleContext context)
    {
        string filePath = context.File.RelativePath;

        List<Finding> findings = [];
        List<Exemption> exemptions = [];

        foreach (ExpressionStatementSyntax statement in context.File.Tree.GetRoot()
            .DescendantNodes()
            .OfType<ExpressionStatementSyntax>())
        {
            if (Discarded(statement) is InvocationExpressionSyntax discarded)
            {
                exemptions.Add(ToExemption(discarded, filePath, ExemptionReason.DiscardedResult));
                continue;
            }

            if (statement.Expression is not InvocationExpressionSyntax invocation
                || !ReturnsTask(invocation))
            {
                continue;
            }

            if (IsInsideTaskRun(statement))
            {
                exemptions.Add(ToExemption(invocation, filePath, ExemptionReason.FireAndForget));
                continue;
            }

            findings.Add(new Finding(
                Code,
                Name,
                $"{CalledName(invocation)} cagrisinin donen gorevi kullanilmiyor. await et ya da bilerek atiyorsan _ = yaz.",
                Description,
                filePath,
                BlockingCallRule.LineOf(invocation),
                BlockingCallRule.EnclosingMethodName(invocation),
                Severity.Warning));
        }

        return findings.Count == 0 && exemptions.Count == 0
            ? RuleResult.Empty
            : new RuleResult(findings, exemptions);
    }

    /// <summary>
    /// <c>_ = FooAsync();</c> kalibi. Sonucu bilerek atan bir yazimi tartismiyoruz, ama
    /// muafiyet olarak kaydediyoruz ki "bu cagri neden listede yok" sorusu cevaplanabilsin.
    /// </summary>
    private static InvocationExpressionSyntax? Discarded(ExpressionStatementSyntax statement) =>
        statement.Expression is AssignmentExpressionSyntax
        {
            Left: IdentifierNameSyntax { Identifier.ValueText: "_" },
            Right: InvocationExpressionSyntax invocation,
        } && ReturnsTask(invocation)
            ? invocation
            : null;

    /// <summary>
    /// Cagri Task donuyor gibi duruyor mu. <c>.ConfigureAwait(...)</c> zincirinin ustunu
    /// soyup altindaki cagriya bakiyoruz: tek basina duran bir ConfigureAwait zinciri de
    /// beklenmemis bir gorevdir, muafiyet degil.
    /// </summary>
    private static bool ReturnsTask(InvocationExpressionSyntax invocation) =>
        CalledName(Unwrap(invocation)).EndsWith("Async", StringComparison.Ordinal);

    private static InvocationExpressionSyntax Unwrap(InvocationExpressionSyntax invocation) =>
        invocation is
        {
            Expression: MemberAccessExpressionSyntax
            {
                Name.Identifier.ValueText: "ConfigureAwait",
                Expression: InvocationExpressionSyntax inner,
            },
        }
            ? Unwrap(inner)
            : invocation;

    /// <summary>
    /// Cagri <c>Task.Run(...)</c> argumanlarinin icinde mi. Oradaki bir cagri zaten ayri
    /// bir goreve verilmis oluyor; cagiranin onu beklemesi beklenmiyor.
    /// </summary>
    private static bool IsInsideTaskRun(SyntaxNode node) =>
        node.Ancestors()
            .OfType<InvocationExpressionSyntax>()
            .Any(invocation => invocation.Expression is MemberAccessExpressionSyntax
            {
                Name.Identifier.ValueText: "Run",
                Expression: IdentifierNameSyntax { Identifier.ValueText: "Task" },
            });

    private static string CalledName(InvocationExpressionSyntax invocation) => invocation.Expression switch
    {
        MemberAccessExpressionSyntax member => member.Name.Identifier.ValueText,
        IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
        _ => invocation.Expression.ToString(),
    };

    private Exemption ToExemption(SyntaxNode node, string filePath, ExemptionReason reason) =>
        new(
            Code,
            filePath,
            BlockingCallRule.LineOf(node),
            BlockingCallRule.EnclosingMethodName(node),
            reason);
}
