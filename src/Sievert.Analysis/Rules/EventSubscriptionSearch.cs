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
    /// Ayni klasordeki diger dosyalarda <paramref name="typeName"/> adli partial tipin baska
    /// bir parcasi varsa, oradaki abonelikleri toplar. Parcalari klasore ve tip adina bakarak
    /// buluyoruz; bu bir heuristik, ayrintisi docs/sinirliliklar.md'de.
    /// </summary>
    /// <param name="absoluteFilePath">
    /// Incelenen dosyanin diskteki yolu. Bos gelirse (yoldan ayristirilmamis bir agac)
    /// klasore hic bakilmaz, sadece dosyanin kendisi kanit sayilir.
    /// </param>
    public static HashSet<string> NamesInPartialParts(string absoluteFilePath, string typeName)
    {
        HashSet<string> names = new(StringComparer.Ordinal);

        if (string.IsNullOrEmpty(absoluteFilePath))
        {
            return names;
        }

        string? folder = Path.GetDirectoryName(absoluteFilePath);

        if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder))
        {
            return names;
        }

        foreach (string sibling in Directory.EnumerateFiles(folder, "*.cs"))
        {
            if (string.Equals(sibling, absoluteFilePath, StringComparison.Ordinal))
            {
                continue;
            }

            string text = File.ReadAllText(sibling);

            // Parca olabilmesi icin hem "partial" hem tip adi metinde gecmek zorunda.
            // Gecmiyorsa dosyayi ayristirmaya gerek yok.
            if (!text.Contains("partial", StringComparison.Ordinal)
                || !text.Contains(typeName, StringComparison.Ordinal))
            {
                continue;
            }

            SyntaxNode root = CSharpSyntaxTree.ParseText(text).GetRoot();

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
