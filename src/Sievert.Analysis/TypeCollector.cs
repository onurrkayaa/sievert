using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Sievert.Core.Analysis;

namespace Sievert.Analysis;

/// <summary>
/// Sözdizimi ağacını gezip tipleri ve metotlarını toplar.
/// Metot gövdesinin içine inmediği için yerel fonksiyonlar metot sayılmaz;
/// constructor ve property için de ayrı bir ziyaret metodu yazılmadığından onlar da sayılmaz.
/// </summary>
internal sealed class TypeCollector : CSharpSyntaxWalker
{
    private readonly Stack<TypeEntry> _openTypes = new();
    private readonly List<TypeEntry> _entries = [];

    /// <summary>Partial parçaları birleştirilmiş haliyle bulunan tipler.</summary>
    public IReadOnlyList<SievertType> Types =>
        _entries
            .GroupBy(entry => entry.Name, StringComparer.Ordinal)
            .Select(parts => new SievertType(
                parts.Key,
                parts.First().Kind,
                parts.Min(part => part.StartLine),
                parts.SelectMany(part => part.Methods).ToList()))
            .ToList();

    public override void VisitClassDeclaration(ClassDeclarationSyntax node) => WalkType(node, SievertTypeKind.Class);

    public override void VisitRecordDeclaration(RecordDeclarationSyntax node) => WalkType(node, SievertTypeKind.Record);

    public override void VisitStructDeclaration(StructDeclarationSyntax node) => WalkType(node, SievertTypeKind.Struct);

    public override void VisitInterfaceDeclaration(InterfaceDeclarationSyntax node) => WalkType(node, SievertTypeKind.Interface);

    // Enum ve delegate metot icermez, o yuzden sadece kaydedilip icine inilmiyor.
    public override void VisitEnumDeclaration(EnumDeclarationSyntax node) =>
        RecordType(node.Identifier.ValueText, SievertTypeKind.Enum, node);

    public override void VisitDelegateDeclaration(DelegateDeclarationSyntax node) =>
        RecordType(node.Identifier.ValueText, SievertTypeKind.Delegate, node);

    public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
    {
        if (_openTypes.Count == 0)
        {
            return;
        }

        FileLinePositionSpan span = node.GetLocation().GetLineSpan();
        int start = span.StartLinePosition.Line + 1;
        int end = span.EndLinePosition.Line + 1;

        _openTypes.Peek().Methods.Add(new SievertMethod(
            node.Identifier.ValueText,
            start,
            end - start + 1,
            node.Modifiers.Any(SyntaxKind.AsyncKeyword),
            node.ParameterList.Parameters.Count,
            node.ReturnType.ToString()));

        // Gövdenin içine inmiyoruz, çünkü yerel fonksiyonlar metot sayılmayacak.
    }

    private void WalkType(TypeDeclarationSyntax node, SievertTypeKind kind)
    {
        TypeEntry entry = RecordType(node.Identifier.ValueText, kind, node);
        _openTypes.Push(entry);

        // İç içe tipleri ve metotları bulmak için üyeleri tek tek geziyoruz.
        foreach (MemberDeclarationSyntax member in node.Members)
        {
            Visit(member);
        }

        _openTypes.Pop();
    }

    /// <summary>Tipi listeye ekler. Ic ice tipler ust tipin adiyla nitelenir.</summary>
    private TypeEntry RecordType(string name, SievertTypeKind kind, SyntaxNode node)
    {
        TypeEntry entry = new(
            _openTypes.Count == 0 ? name : $"{_openTypes.Peek().Name}.{name}",
            kind,
            node.GetLocation().GetLineSpan().StartLinePosition.Line + 1);

        _entries.Add(entry);
        return entry;
    }

    /// <summary>Gezinti sırasında metotları biriktirebilmek için kullanılan ara kayıt.</summary>
    private sealed record TypeEntry(string Name, SievertTypeKind Kind, int StartLine)
    {
        public List<SievertMethod> Methods { get; } = [];
    }
}
