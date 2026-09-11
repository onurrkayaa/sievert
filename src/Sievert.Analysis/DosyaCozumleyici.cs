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

        TipToplayici toplayici = new();
        toplayici.Visit(agac.GetRoot());

        List<string> hatalar = agac.GetDiagnostics()
            .Where(tani => tani.Severity == DiagnosticSeverity.Error)
            .Select(tani => $"Satir {tani.Location.GetLineSpan().StartLinePosition.Line + 1}: {tani.GetMessage()}")
            .ToList();

        return new DosyaAnalizi(dosyaYolu, toplayici.Tipler, SatirSayisi(agac.GetText()), hatalar);
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
