using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Sievert.Analysis.Rules;

/// <summary>
/// Kaynaktaki tek bir <c>// sievert:disable</c> yorumu.
/// </summary>
/// <param name="RuleCode">Susturulmak istenen kural kodu.</param>
/// <param name="Reason">Yazilan gerekce; yazilmadiysa bos.</param>
/// <param name="CommentLine">Yorumun kendi satiri (1'den baslar).</param>
/// <param name="SuppressedLine">Susturmanin etkiledigi satir, yani yorumun bir altindaki.</param>
/// <param name="MethodName">Yorumun icinde durdugu metot; bulgu raporunda gorunsun diye.</param>
public sealed record SuppressionComment(
    string RuleCode,
    string Reason,
    int CommentLine,
    int SuppressedLine,
    string MethodName)
{
    /// <summary>Gerekce yazilmis mi. Yazilmadiysa susturma gecersiz ve kendisi bulgu uretir.</summary>
    public bool HasReason => !string.IsNullOrWhiteSpace(Reason);
}

/// <summary>
/// Susturma yorumlarini kaynaktan okur. Bicim: <c>// sievert:disable SV004 gerekce</c>.
/// Tek satirlik, sadece bir sonraki satiri etkiliyor; dosya geneli susturma yok (ADR 0015).
/// </summary>
public static class SuppressionReader
{
    private const string Marker = "sievert:disable";

    public static IReadOnlyList<SuppressionComment> Read(SyntaxTree tree)
    {
        List<SuppressionComment> comments = [];

        foreach (SyntaxTrivia trivia in tree.GetRoot().DescendantTrivia())
        {
            if (!trivia.IsKind(SyntaxKind.SingleLineCommentTrivia))
            {
                continue;
            }

            if (Parse(trivia) is SuppressionComment comment)
            {
                comments.Add(comment);
            }
        }

        return comments;
    }

    private static SuppressionComment? Parse(SyntaxTrivia trivia)
    {
        string text = trivia.ToString().TrimStart('/').Trim();

        if (!text.StartsWith(Marker, StringComparison.Ordinal))
        {
            return null;
        }

        string rest = text[Marker.Length..].Trim();
        int space = rest.IndexOf(' ', StringComparison.Ordinal);
        string code = space < 0 ? rest : rest[..space];

        if (!LooksLikeRuleCode(code))
        {
            // Kural kodu yazilmamis bir satiri susturma sayamam; hangi kurali
            // susturdugunu bilmeden bulgu elenirse her sey elenir.
            return null;
        }

        string reason = space < 0 ? string.Empty : rest[(space + 1)..].Trim();
        int line = trivia.GetLocation().GetLineSpan().StartLinePosition.Line + 1;

        string method = trivia.Token.Parent is null
            ? string.Empty
            : BlockingCallRule.EnclosingMethodName(trivia.Token.Parent);

        return new SuppressionComment(code, reason, line, line + 1, method);
    }

    /// <summary>SV ile baslayip uc rakamla devam eden kodlar (ADR: kural kodlari SV onekli).</summary>
    private static bool LooksLikeRuleCode(string value) =>
        value.Length == 5
        && value.StartsWith("SV", StringComparison.Ordinal)
        && value.Skip(2).All(char.IsAsciiDigit);
}
