using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Sievert.Core.Rules;

namespace Sievert.Analysis.Rules;

/// <summary>SV004 - dongu govdesinde sorguya benzeyen cagrilari bulur.</summary>
public sealed class NPlusOneRule : IRule
{
    public string Code => "SV004";

    public string Name => "dongu icinde sorgu";

    public string Description =>
        "Dongunun her adiminda ayri bir sorgu calistirmak, tek sorguda alinabilecek veriyi "
        + "N kere gidip getirmek demek.";

    /// <summary>Sorguyu bitiren, yani gercekten veriyi ceken cagri adlari.</summary>
    private static readonly string[] QueryTerminators =
    [
        "ToList", "ToListAsync", "ToArray", "ToArrayAsync", "ToDictionary", "ToDictionaryAsync",
        "First", "FirstAsync", "FirstOrDefault", "FirstOrDefaultAsync",
        "Single", "SingleAsync", "SingleOrDefault", "SingleOrDefaultAsync",
        "Any", "AnyAsync", "Count", "CountAsync", "LongCount", "LongCountAsync",
        "Sum", "SumAsync", "Max", "MaxAsync", "Min", "MinAsync",
    ];

    /// <summary>
    /// Uzerlerindeki <c>Max</c>/<c>Min</c>/<c>Sum</c> LINQ degil, statik yardimci cagrisidir.
    /// Olcumde SV004'un dort yanlis pozitifinin ikisi <c>Math.Max</c> idi.
    /// </summary>
    private static readonly string[] NonQueryReceivers = ["Math"];

    /// <summary>Uzerinde cagri yapilan ifadede gecerse veritabani olma ihtimalini artiran parcalar.</summary>
    private static readonly string[] DatabaseHints = ["Context", "Db", "Repo", "Set"];

    public RuleResult Inspect(RuleContext context)
    {
        string filePath = context.File.RelativePath;
        List<Finding> findings = [];

        foreach (InvocationExpressionSyntax invocation in context.File.Tree.GetRoot()
            .DescendantNodes()
            .OfType<InvocationExpressionSyntax>())
        {
            if (invocation.Expression is not MemberAccessExpressionSyntax member
                || !QueryTerminators.Contains(member.Name.Identifier.ValueText, StringComparer.Ordinal)
                || NonQueryReceivers.Contains(member.Expression.ToString(), StringComparer.Ordinal)
                || !IsInsideALoopBody(invocation))
            {
                continue;
            }

            string receiver = member.Expression.ToString();

            findings.Add(new Finding(
                Code,
                Name,
                Message(member.Name.Identifier.ValueText, receiver),
                Description,
                filePath,
                BlockingCallRule.LineOf(invocation),
                BlockingCallRule.EnclosingMethodName(invocation),
                Severity.Warning));
        }

        return findings.Count == 0 ? RuleResult.Empty : new RuleResult(findings, []);
    }

    /// <summary>
    /// Ipucu mesaja giriyor ama filtre olarak kullanilmiyor. Filtre yapsaydim, adinda
    /// Context/Db/Repo/Set gecmeyen gercek bir N+1'i kacirirdim; hangi bedelin daha buyuk
    /// oldugunu olcmeden karar vermek istemedim. Gerekcesi ADR 0010'da.
    /// </summary>
    private static string Message(string call, string receiver) =>
        DatabaseHints.Any(hint => receiver.Contains(hint, StringComparison.OrdinalIgnoreCase))
            ? $"Dongu icinde {receiver}.{call}() cagriliyor. {receiver} adina bakilirsa bu bir veritabani sorgusu olabilir; sorguyu dongunun disina alabilirsin."
            : $"Dongu icinde {receiver}.{call}() cagriliyor. Sorgu ise dongunun disina alinabilir; bellekteki bir koleksiyonsa bu bulgu gereksizdir.";

    /// <summary>
    /// Cagri bir dongunun GOVDESINDE mi. Dongunun kaynagindaki cagri (ornegin
    /// <c>foreach (var x in db.Items.ToList())</c>) bulgu degil: orasi zaten tek sorgu,
    /// yani N+1'in cozumu.
    /// </summary>
    private static bool IsInsideALoopBody(SyntaxNode node)
    {
        foreach (SyntaxNode ancestor in node.Ancestors())
        {
            StatementSyntax? body = ancestor switch
            {
                ForEachStatementSyntax loop => loop.Statement,
                ForEachVariableStatementSyntax loop => loop.Statement,
                ForStatementSyntax loop => loop.Statement,
                WhileStatementSyntax loop => loop.Statement,
                DoStatementSyntax loop => loop.Statement,
                _ => null,
            };

            if (body is not null && node.Ancestors().Contains(body))
            {
                return true;
            }
        }

        return false;
    }
}
