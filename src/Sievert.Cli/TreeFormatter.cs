using System.Globalization;

using Sievert.Core.Analysis;

namespace Sievert.Cli;

/// <summary>Tarama sonucunu ASCII agac olarak satirlara cevirir.</summary>
public static class TreeFormatter
{
    /// <summary>Bu satirdan uzun metotlara uyari isareti konur.</summary>
    public const int LongMethodThreshold = 40;

    /// <summary>Ekranda bir dosya icin en fazla kac ayristirma hatasi gosterilecegi. JSON ciktisinda hepsi yer alir.</summary>
    public const int MaxErrorsOnScreen = 3;

    private const string AsyncTag = "[async]";

    /// <summary>
    /// Dosyalari, tipleri ve metotlari agac halinde yazar; istenirse en uzun metotlari
    /// ve en sonda ozeti ekler. Renk burada secilmiyor, her parcaya sadece rolu yaziliyor.
    /// </summary>
    public static IReadOnlyList<OutputLine> Format(
        IReadOnlyList<FileAnalysis> analyses,
        ScanSummary summary,
        IReadOnlyList<MethodLocation> longestMethods)
    {
        int nameColumn = MethodNameColumnWidth(analyses);
        int lineColumn = LineCountColumnWidth(analyses);

        List<OutputLine> lines = [];

        foreach (FileAnalysis analysis in analyses)
        {
            lines.Add(FileHeading(analysis));
            lines.AddRange(FormatFile(analysis, nameColumn, lineColumn));
        }

        if (longestMethods.Count > 0)
        {
            lines.Add(EmptyLine());
            lines.Add(Line(new OutputSpan($"En uzun {longestMethods.Count} metot", OutputColor.Heading)));
            lines.AddRange(FormatLongest(longestMethods, lineColumn));
        }

        lines.Add(EmptyLine());
        lines.Add(Line(new OutputSpan("Ozet", OutputColor.Heading)));
        lines.AddRange(FormatSummary(summary));

        return lines;
    }

    private static IEnumerable<OutputLine> FormatFile(FileAnalysis analysis, int nameColumn, int lineColumn)
    {
        for (int i = 0; i < analysis.Types.Count; i++)
        {
            SievertType type = analysis.Types[i];
            bool lastType = i == analysis.Types.Count - 1 && analysis.ParseErrors.Count == 0;

            yield return Line(
                new OutputSpan(lastType ? "`- " : "+- ", OutputColor.Dim),
                new OutputSpan(type.Name, OutputColor.Normal),
                new OutputSpan($" ({type.Kind.Keyword()})", OutputColor.Dim));

            for (int j = 0; j < type.Methods.Count; j++)
            {
                yield return MethodLine(
                    type.Methods[j],
                    (lastType ? "   " : "|  ") + (j == type.Methods.Count - 1 ? "`- " : "+- "),
                    nameColumn,
                    lineColumn);
            }
        }

        if (analysis.NoTypesFound && !analysis.HasParseErrors)
        {
            yield return Line(
                new OutputSpan("`- ", OutputColor.Dim),
                new OutputSpan("(hic tip bulunamadi)", OutputColor.Dim));
        }

        foreach (OutputLine line in FormatErrors(analysis))
        {
            yield return line;
        }
    }

    private static IEnumerable<OutputLine> FormatErrors(FileAnalysis analysis)
    {
        for (int i = 0; i < Math.Min(MaxErrorsOnScreen, analysis.ParseErrors.Count); i++)
        {
            bool last = i == analysis.ParseErrors.Count - 1;
            yield return Line(
                new OutputSpan(last ? "`- " : "+- ", OutputColor.Dim),
                new OutputSpan("! " + analysis.ParseErrors[i], OutputColor.Warning));
        }

        int remaining = analysis.ParseErrors.Count - MaxErrorsOnScreen;
        if (remaining > 0)
        {
            yield return Line(
                new OutputSpan("`- ", OutputColor.Dim),
                new OutputSpan($"! ve {remaining} hata daha", OutputColor.Warning));
        }
    }

    private static OutputLine MethodLine(SievertMethod method, string prefix, int nameColumn, int lineColumn)
    {
        List<OutputSpan> spans =
        [
            new OutputSpan(prefix, OutputColor.Dim),
            new OutputSpan(method.Name.PadRight(nameColumn - prefix.Length + 2), OutputColor.Normal),
            new OutputSpan(
                method.IsAsync ? AsyncTag : new string(' ', AsyncTag.Length),
                method.IsAsync ? OutputColor.Tag : OutputColor.Normal),
            new OutputSpan(
                "  " + method.LineCount.ToString(CultureInfo.InvariantCulture).PadLeft(lineColumn) + " satir",
                OutputColor.Dim),
        ];

        if (method.LineCount > LongMethodThreshold)
        {
            spans.Add(new OutputSpan("  (!)", OutputColor.Warning));
        }

        return new OutputLine(spans);
    }

    private static IEnumerable<OutputLine> FormatLongest(
        IReadOnlyList<MethodLocation> longestMethods,
        int lineColumn)
    {
        int rankColumn = longestMethods.Count.ToString(CultureInfo.InvariantCulture).Length;
        int nameColumn = longestMethods.Max(location => $"{location.TypeName}.{location.Method.Name}".Length);

        for (int i = 0; i < longestMethods.Count; i++)
        {
            MethodLocation location = longestMethods[i];
            string rank = (i + 1).ToString(CultureInfo.InvariantCulture).PadLeft(rankColumn);
            string length = location.Method.LineCount.ToString(CultureInfo.InvariantCulture).PadLeft(lineColumn);

            yield return Line(
                new OutputSpan($"  {rank}. ", OutputColor.Dim),
                new OutputSpan($"{length} satir  ", location.Method.LineCount > LongMethodThreshold ? OutputColor.Warning : OutputColor.Normal),
                new OutputSpan($"{location.TypeName}.{location.Method.Name}".PadRight(nameColumn + 2), OutputColor.Normal),
                new OutputSpan(
                    $"{location.FilePath}:{location.Method.StartLine}",
                    OutputColor.Dim));
        }
    }

    private static IEnumerable<OutputLine> FormatSummary(ScanSummary summary)
    {
        MethodLength length = summary.MethodLength;
        CodeSplit split = summary.CodeSplit;

        (string Label, string Value)[] rows =
        [
            ("Dosya", $"{Number(summary.FileCount)}  (uretim {split.ProductionFileCount} / test {split.TestFileCount})"),
            ("Dislanan dosya", Number(summary.ExcludedFileCount)),
            ("Atlanan klasor", Number(summary.SkippedDirectories.Count)),
            ("Tip", Number(summary.TypeCount)),
            ("Metot", $"{Number(summary.MethodCount)}  (uretim {split.ProductionMethodCount} / test {split.TestMethodCount})"),
            ("Async orani", $"%{OneDecimal(summary.AsyncRatio * 100)} ({summary.AsyncMethodCount}/{summary.MethodCount})"),
            ("Metot uzunlugu", $"ortalama {OneDecimal(length.Average)}  medyan {OneDecimal(length.Median)}  p90 {length.P90}  p95 {length.P95}  en uzun {length.Longest}"),
            ("Kor nokta", BlindSpotSummary(summary.BlindSpot)),
            ("Ayristirilamayan dosya", Number(summary.FilesWithParseErrors)),
            ("Tip bulunamayan dosya", Number(summary.FilesWithoutTypes)),
        ];

        int labelColumn = rows.Max(row => row.Label.Length);

        foreach ((string label, string value) in rows)
        {
            yield return Line(
                new OutputSpan("  " + label.PadRight(labelColumn) + " : ", OutputColor.Dim),
                new OutputSpan(value, OutputColor.Normal));
        }
    }

    /// <summary>Metot adlari hangi dosyada olursa olsun ayni sutunda dursun diye toplu olcum.</summary>
    private static int MethodNameColumnWidth(IReadOnlyList<FileAnalysis> analyses)
    {
        // 3 karakter agac on eki + 2 karakter bosluk payi.
        int longestName = analyses
            .SelectMany(analysis => analysis.Types)
            .SelectMany(type => type.Methods)
            .Select(method => method.Name.Length)
            .DefaultIfEmpty(0)
            .Max();

        return longestName + 6;
    }

    private static int LineCountColumnWidth(IReadOnlyList<FileAnalysis> analyses) =>
        analyses
            .SelectMany(analysis => analysis.Types)
            .SelectMany(type => type.Methods)
            .Select(method => method.LineCount.ToString(CultureInfo.InvariantCulture).Length)
            .DefaultIfEmpty(1)
            .Max();

    /// <summary>Dosya adi, kosullu derleme varsa yaninda kucuk bir isaret.</summary>
    private static OutputLine FileHeading(FileAnalysis analysis)
    {
        if (!analysis.HasConditionalCompilation)
        {
            return Line(new OutputSpan(analysis.FilePath, OutputColor.Heading));
        }

        string mark = analysis.BlindSpotLines > 0
            ? $"  [#if - {analysis.BlindSpotLines} satir gorulmedi]"
            : "  [#if]";

        return Line(
            new OutputSpan(analysis.FilePath, OutputColor.Heading),
            new OutputSpan(mark, OutputColor.Warning));
    }

    private static string BlindSpotSummary(BlindSpot blindSpot) =>
        blindSpot.FileCount == 0
            ? "yok"
            : $"{blindSpot.FileCount} dosyada #if, {blindSpot.LineCount} satir gorulmedi (%{OneDecimal(blindSpot.Ratio * 100)})";

    private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);

    private static string OneDecimal(double value) => value.ToString("0.0", CultureInfo.InvariantCulture);

    private static OutputLine Line(params OutputSpan[] spans) => new(spans);

    private static OutputLine EmptyLine() => new([]);
}
