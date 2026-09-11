using Sievert.Core.Analysis;

namespace Sievert.Analysis;

/// <summary>Dosya analizlerinden toplu sayilari cikarir.</summary>
public static class Summarizer
{
    /// <param name="excludedFileCount">--exclude ile elenen dosya sayisi.</param>
    /// <param name="skippedDirectories">Icine hic girilmeyen klasorler, koke gore goreli.</param>
    public static ScanSummary Summarize(
        IReadOnlyList<FileAnalysis> analyses,
        int excludedFileCount = 0,
        IReadOnlyList<string>? skippedDirectories = null)
    {
        List<SievertMethod> methods = analyses.SelectMany(MethodsOf).ToList();
        int asyncCount = methods.Count(method => method.IsAsync);
        int blindSpotLines = analyses.Sum(analysis => analysis.BlindSpotLines);
        int totalLines = analyses.Sum(analysis => analysis.TotalLineCount);

        return new ScanSummary(
            FileCount: analyses.Count,
            TypeCount: analyses.Sum(analysis => analysis.Types.Count),
            MethodCount: methods.Count,
            AsyncMethodCount: asyncCount,
            AsyncRatio: Ratio(asyncCount, methods.Count),
            MethodLength: LengthStats(methods),
            FilesWithParseErrors: analyses.Count(analysis => analysis.HasParseErrors),
            FilesWithoutTypes: analyses.Count(analysis => analysis.NoTypesFound),
            BlindSpot: new BlindSpot(
                analyses.Count(analysis => analysis.HasConditionalCompilation),
                blindSpotLines,
                Ratio(blindSpotLines, totalLines)),
            CodeSplit: new CodeSplit(
                ProductionFileCount: analyses.Count(analysis => !analysis.IsTestCode),
                TestFileCount: analyses.Count(analysis => analysis.IsTestCode),
                ProductionMethodCount: analyses.Where(analysis => !analysis.IsTestCode).Sum(analysis => MethodsOf(analysis).Count()),
                TestMethodCount: analyses.Where(analysis => analysis.IsTestCode).Sum(analysis => MethodsOf(analysis).Count())),
            ExcludedFileCount: excludedFileCount,
            SkippedDirectories: skippedDirectories ?? []);
    }

    /// <summary>En uzun metotlari uzundan kisaya dogru dondurur.</summary>
    public static IReadOnlyList<MethodLocation> LongestMethods(IReadOnlyList<FileAnalysis> analyses, int count) =>
        analyses
            .SelectMany(analysis => analysis.Types
                .SelectMany(type => type.Methods.Select(method => new MethodLocation(analysis.FilePath, type.Name, method))))
            .OrderByDescending(location => location.Method.LineCount)
            .ThenBy(location => location.FilePath, StringComparer.Ordinal)
            .ThenBy(location => location.Method.StartLine)
            .Take(count)
            .ToList();

    private static IEnumerable<SievertMethod> MethodsOf(FileAnalysis analysis) =>
        analysis.Types.SelectMany(type => type.Methods);

    private static MethodLength LengthStats(List<SievertMethod> methods)
    {
        if (methods.Count == 0)
        {
            return new MethodLength(0, 0, 0, 0, 0);
        }

        int[] lengths = methods.Select(method => method.LineCount).Order().ToArray();

        return new MethodLength(
            Average: lengths.Average(),
            Median: Median(lengths),
            P90: Percentile(lengths, 0.90),
            P95: Percentile(lengths, 0.95),
            Longest: lengths[^1]);
    }

    /// <summary>Cift sayida deger varsa ortadaki ikisinin ortalamasi.</summary>
    private static double Median(int[] sorted) =>
        sorted.Length % 2 == 1
            ? sorted[sorted.Length / 2]
            : (sorted[sorted.Length / 2 - 1] + sorted[sorted.Length / 2]) / 2.0;

    /// <summary>En yakin siraya gore yuzdelik: siradaki degeri dondurur, ara deger uretmez.</summary>
    private static int Percentile(int[] sorted, double percentile)
    {
        int index = (int)Math.Ceiling(percentile * sorted.Length) - 1;
        return sorted[Math.Clamp(index, 0, sorted.Length - 1)];
    }

    private static double Ratio(int numerator, int denominator) => denominator == 0 ? 0 : (double)numerator / denominator;
}
