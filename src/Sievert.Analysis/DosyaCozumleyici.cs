using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Sievert.Core.Cozumleme;

namespace Sievert.Analysis;

/// <summary>Tek bir C# dosyasını Roslyn ile ayrıştırıp özetini çıkarır.</summary>
public static class DosyaCozumleyici
{
    /// <summary>Diskteki bir dosyayı okuyup çözümler.</summary>
    public static DosyaAnalizi DosyayiCozumle(string dosyaYolu) =>
        MetniCozumle(File.ReadAllText(dosyaYolu), dosyaYolu);

    /// <summary>
    /// Verilen kaynak metni çözümler. Sözdizimi hatası olsa bile exception fırlatmaz;
    /// hatalar listeye konur ve ağacın çözülebilen kısmı döndürülür.
    /// </summary>
    public static DosyaAnalizi MetniCozumle(string kaynak, string dosyaYolu)
    {
        SyntaxTree agac = CSharpSyntaxTree.ParseText(kaynak, path: dosyaYolu);
        SyntaxNode kok = agac.GetRoot();

        TipToplayici toplayici = new();
        toplayici.Visit(kok);

        List<string> hatalar = agac.GetDiagnostics()
            .Where(tani => tani.Severity == DiagnosticSeverity.Error)
            .Select(tani => $"Satir {tani.Location.GetLineSpan().StartLinePosition.Line + 1}: {tani.GetMessage()}")
            .ToList();

        (int korNokta, bool kosullu) = KorNoktayiOlc(kok);

        return new DosyaAnalizi(
            dosyaYolu,
            toplayici.Tipler,
            SatirSayisi(agac.GetText()),
            hatalar,
            korNokta,
            kosullu);
    }

    /// <summary>
    /// Kapalı <c>#if</c> dallarında kalan satırları sayar. Hiçbir preprocessor sembolü
    /// tanımlamadığımız için Roslyn o dalları DisabledTextTrivia olarak işaretliyor; biz de
    /// sadece ne kadarını görmediğimizi ölçüyoruz. Sembol tahmin edip yeniden ayrıştırmıyoruz.
    /// </summary>
    private static (int KorNoktaSatirlari, bool KosulluDerlemeVarMi) KorNoktayiOlc(SyntaxNode kok)
    {
        int satirlar = 0;
        bool kosullu = false;

        foreach (SyntaxTrivia trivia in kok.DescendantTrivia(descendIntoTrivia: true))
        {
            if (trivia.IsKind(SyntaxKind.DisabledTextTrivia))
            {
                satirlar += TriviaSatirSayisi(trivia);
            }
            else if (trivia.IsKind(SyntaxKind.IfDirectiveTrivia))
            {
                kosullu = true;
            }
        }

        return (satirlar, kosullu);
    }

    /// <summary>Bir trivia parçasının kaç satır tuttuğu. Sondaki satır sonu fazladan saydırmasın diye elenir.</summary>
    private static int TriviaSatirSayisi(SyntaxTrivia trivia)
    {
        FileLinePositionSpan aralik = trivia.GetLocation().GetLineSpan();
        int fark = aralik.EndLinePosition.Line - aralik.StartLinePosition.Line;

        return aralik.EndLinePosition.Character == 0 ? fark : fark + 1;
    }

    /// <summary>Dosyanın satır sayısı. Sondaki satır sonu fazladan bir satır saydırmasın diye elenir.</summary>
    private static int SatirSayisi(SourceText metin)
    {
        int sayi = metin.Lines.Count;
        if (sayi > 1 && metin.Lines[sayi - 1].Span.IsEmpty)
        {
            sayi--;
        }

        return sayi;
    }
}
