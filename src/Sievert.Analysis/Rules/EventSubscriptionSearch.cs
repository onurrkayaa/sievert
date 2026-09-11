using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Sievert.Analysis.Rules;

/// <summary>
/// <c>+= MetotAdi</c> biciminde abone edilen metot adlarini toplar. Arama metot adiyla
/// yapiliyor ama kapsam bilerek dar tutuldu: sadece metodun kendi dosyasi ve ayni
/// klasordeki, ayni adi tasiyan partial sinif parcalari.
///
/// Repo genelinde ad aramasi yapmiyoruz cunku metot adi benzersiz degil. ShareX klonunda
/// <c>OnOpened</c> 42, <c>OnDrop</c> 17 ayri yerde abone ediliyor ve bunlar farkli
/// siniflarin ayni adli metotlari. Genis bir ad aramasi, bir sinifta yapilan aboneligi
/// baska bir sinifin metoduna mal eder ve neredeyse her async void metodu muaf tutar.
/// Gerekcesi ADR 0008'de.
/// </summary>
internal static class EventSubscriptionSearch
{
    /// <summary>Bu agacta <c>+= MetotAdi</c> ile abone edilen adlar.</summary>
    public static HashSet<string> NamesIn(SyntaxNode root) =>
        root.DescendantNodes()
            .OfType<AssignmentExpressionSyntax>()
            .Where(assignment => assignment.IsKind(SyntaxKind.AddAssignmentExpression))
            .Select(assignment => assignment.Right)
            .OfType<IdentifierNameSyntax>()
            .Select(name => name.Identifier.ValueText)
            .ToHashSet(StringComparer.Ordinal);

    /// <summary>
    /// Komsu dosyalar arasinda <paramref name="typeName"/> adli partial tipin baska bir
    /// parcasi varsa, oradaki abonelikleri toplar. Parcalari klasore ve tip adina bakarak
    /// buluyoruz; bu bir heuristik, ayrintisi docs/sinirliliklar.md'de.
    /// </summary>
    /// <param name="filesInSameFolder">
    /// Ayni klasordeki diger <em>taranan</em> dosyalar. Diski kendimiz okumuyoruz: tarama
    /// disi birakilan bir dosya buraya girmedigi icin kanit da olamiyor.
    /// </param>
    public static HashSet<string> NamesInPartialParts(
        IReadOnlyList<ScannedFile> filesInSameFolder,
        string typeName)
    {
        HashSet<string> names = new(StringComparer.Ordinal);

        foreach (ScannedFile sibling in filesInSameFolder)
        {
            SyntaxNode root = sibling.Tree.GetRoot();

            if (DeclaresPartialType(root, typeName))
            {
                names.UnionWith(NamesIn(root));
            }
        }

        return names;
    }

    private static bool DeclaresPartialType(SyntaxNode root, string typeName) =>
        root.DescendantNodes()
            .OfType<TypeDeclarationSyntax>()
            .Any(type => type.Identifier.ValueText == typeName
                && type.Modifiers.Any(SyntaxKind.PartialKeyword));
}
