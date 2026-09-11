using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Sievert.Core.Analysis;

namespace Sievert.Analysis;

/// <summary>Tek bir C# dosyasını Roslyn ile ayrıştırıp özetini çıkarır.</summary>
public static class FileAnalyzer
{
    /// <summary>Diskteki bir dosyayı okuyup çözümler.</summary>
    public static FileAnalysis AnalyzeFile(string filePath) =>
        AnalyzeText(File.ReadAllText(filePath), filePath);

    /// <summary>
    /// Verilen kaynak metni çözümler. Sözdizimi hatası olsa bile exception fırlatmaz;
    /// hatalar listeye konur ve ağacın çözülebilen kısmı döndürülür.
    /// </summary>
    public static FileAnalysis AnalyzeText(string source, string filePath)
    {
        SyntaxTree tree = CSharpSyntaxTree.ParseText(source, path: filePath);
        SyntaxNode root = tree.GetRoot();

        TypeCollector collector = new();
        collector.Visit(root);

        List<string> errors = tree.GetDiagnostics()
            .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .Select(diagnostic => $"Satir {diagnostic.Location.GetLineSpan().StartLinePosition.Line + 1}: {diagnostic.GetMessage()}")
            .ToList();

        (int blindSpot, bool conditional) = MeasureBlindSpot(root);

        return new FileAnalysis(
            filePath,
            collector.Types,
            LineCount(tree.GetText()),
            errors,
            blindSpot,
            conditional);
    }

    /// <summary>
    /// Kapalı <c>#if</c> dallarında kalan satırları sayar. Hiçbir preprocessor sembolü
    /// tanımlamadığımız için Roslyn o dalları DisabledTextTrivia olarak işaretliyor; biz de
    /// sadece ne kadarını görmediğimizi ölçüyoruz. Sembol tahmin edip yeniden ayrıştırmıyoruz.
    /// </summary>
    private static (int BlindSpotLines, bool HasConditionalCompilation) MeasureBlindSpot(SyntaxNode root)
    {
        int lines = 0;
        bool conditional = false;

        foreach (SyntaxTrivia trivia in root.DescendantTrivia(descendIntoTrivia: true))
        {
            if (trivia.IsKind(SyntaxKind.DisabledTextTrivia))
            {
                lines += TriviaLineCount(trivia);
            }
            else if (trivia.IsKind(SyntaxKind.IfDirectiveTrivia))
            {
                conditional = true;
            }
        }

        return (lines, conditional);
    }

    /// <summary>Bir trivia parçasının kaç satır tuttuğu. Sondaki satır sonu fazladan saydırmasın diye elenir.</summary>
    private static int TriviaLineCount(SyntaxTrivia trivia)
    {
        FileLinePositionSpan span = trivia.GetLocation().GetLineSpan();
        int difference = span.EndLinePosition.Line - span.StartLinePosition.Line;

        return span.EndLinePosition.Character == 0 ? difference : difference + 1;
    }

    /// <summary>Dosyanın satır sayısı. Sondaki satır sonu fazladan bir satır saydırmasın diye elenir.</summary>
    private static int LineCount(SourceText text)
    {
        int count = text.Lines.Count;
        if (count > 1 && text.Lines[count - 1].Span.IsEmpty)
        {
            count--;
        }

        return count;
    }
}
