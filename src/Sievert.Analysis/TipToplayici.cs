using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Sievert.Core.Cozumleme;

namespace Sievert.Analysis;

/// <summary>
/// Sözdizimi ağacını gezip tipleri ve metotlarını toplar.
/// Metot gövdesinin içine inmediği için yerel fonksiyonlar metot sayılmaz;
/// constructor ve property için de ayrı bir ziyaret metodu yazılmadığından onlar da sayılmaz.
/// </summary>
internal sealed class TipToplayici : CSharpSyntaxWalker
{
    private readonly Stack<TipKaydi> _acikTipler = new();
    private readonly List<TipKaydi> _kayitlar = [];

    /// <summary>Partial parçaları birleştirilmiş haliyle bulunan tipler.</summary>
    public IReadOnlyList<SievertTip> Tipler =>
        _kayitlar
            .GroupBy(kayit => kayit.Ad, StringComparer.Ordinal)
            .Select(parcalar => new SievertTip(
                parcalar.Key,
                parcalar.First().Turu,
                parcalar.Min(parca => parca.BaslangicSatiri),
                parcalar.SelectMany(parca => parca.Metotlar).ToList()))
            .ToList();

    public override void VisitClassDeclaration(ClassDeclarationSyntax node) => TipiGez(node, TipTuru.Sinif);

    public override void VisitRecordDeclaration(RecordDeclarationSyntax node) => TipiGez(node, TipTuru.Record);

    public override void VisitStructDeclaration(StructDeclarationSyntax node) => TipiGez(node, TipTuru.Struct);

    public override void VisitInterfaceDeclaration(InterfaceDeclarationSyntax node) => TipiGez(node, TipTuru.Interface);

    // Enum ve delegate metot icermez, o yuzden sadece kaydedilip icine inilmiyor.
    public override void VisitEnumDeclaration(EnumDeclarationSyntax node) =>
        TipiKaydet(node.Identifier.ValueText, TipTuru.Enum, node);

    public override void VisitDelegateDeclaration(DelegateDeclarationSyntax node) =>
        TipiKaydet(node.Identifier.ValueText, TipTuru.Delegate, node);

    public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
    {
        if (_acikTipler.Count == 0)
        {
            return;
        }

        FileLinePositionSpan aralik = node.GetLocation().GetLineSpan();
        int baslangic = aralik.StartLinePosition.Line + 1;
        int bitis = aralik.EndLinePosition.Line + 1;

        _acikTipler.Peek().Metotlar.Add(new SievertMetot(
            node.Identifier.ValueText,
            baslangic,
            bitis - baslangic + 1,
            node.Modifiers.Any(SyntaxKind.AsyncKeyword),
            node.ParameterList.Parameters.Count,
            node.ReturnType.ToString()));

        // Gövdenin içine inmiyoruz, çünkü yerel fonksiyonlar metot sayılmayacak.
    }

    private void TipiGez(TypeDeclarationSyntax node, TipTuru turu)
    {
        TipKaydi kayit = TipiKaydet(node.Identifier.ValueText, turu, node);
        _acikTipler.Push(kayit);

        // İç içe tipleri ve metotları bulmak için üyeleri tek tek geziyoruz.
        foreach (MemberDeclarationSyntax uye in node.Members)
        {
            Visit(uye);
        }

        _acikTipler.Pop();
    }

    /// <summary>Tipi listeye ekler. Ic ice tipler ust tipin adiyla nitelenir.</summary>
    private TipKaydi TipiKaydet(string ad, TipTuru turu, SyntaxNode node)
    {
        TipKaydi kayit = new(
            _acikTipler.Count == 0 ? ad : $"{_acikTipler.Peek().Ad}.{ad}",
            turu,
            node.GetLocation().GetLineSpan().StartLinePosition.Line + 1);

        _kayitlar.Add(kayit);
        return kayit;
    }

    /// <summary>Gezinti sırasında metotları biriktirebilmek için kullanılan ara kayıt.</summary>
    private sealed record TipKaydi(string Ad, TipTuru Turu, int BaslangicSatiri)
    {
        public List<SievertMetot> Metotlar { get; } = [];
    }
}
