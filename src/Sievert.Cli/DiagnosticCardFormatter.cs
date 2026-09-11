using System.Globalization;

using Sievert.Core.Rules;

namespace Sievert.Cli;

/// <summary>Bulgulari teshis karti halinde satirlara cevirir.</summary>
public static class DiagnosticCardFormatter
{
    /// <summary>
    /// Her bulgu icin bir kart, en sonda ozet yazar. Renk burada secilmiyor,
    /// her parcaya sadece rolu yaziliyor; agac ciktisiyla ayni yol.
    /// </summary>
    public static IReadOnlyList<OutputLine> Format(IReadOnlyList<Finding> findings, CheckSummary summary)
    {
        List<OutputLine> lines = [];

        for (int i = 0; i < findings.Count; i++)
        {
            if (i > 0)
            {
                lines.Add(EmptyLine());
            }

            lines.AddRange(Card(findings[i]));
        }

        if (findings.Count == 0)
        {
            lines.Add(Line(new OutputSpan("Bulgu yok.", OutputColor.Normal)));
        }

        lines.Add(EmptyLine());
        lines.Add(Line(new OutputSpan("Ozet", OutputColor.Heading)));
        lines.AddRange(FormatSummary(summary));

        return lines;
    }

    /// <summary>Tek bir bulgunun karti: baslik, yeri, ne oldugu, neden onemli oldugu.</summary>
    private static IEnumerable<OutputLine> Card(Finding finding)
    {
        yield return Line(
            new OutputSpan(finding.RuleCode, OutputColor.Heading),
            new OutputSpan("  " + SeverityLabel(finding.Severity), SeverityColor(finding.Severity)),
            new OutputSpan("  " + finding.Title, OutputColor.Normal));

        yield return Line(
            new OutputSpan($"  {finding.FilePath}:{finding.Line}", OutputColor.Dim),
            new OutputSpan("  " + finding.MethodName, OutputColor.Normal));

        yield return Line(new OutputSpan("  " + finding.Description, OutputColor.Normal));

        yield return Line(new OutputSpan("  Neden onemli: " + finding.Rationale, OutputColor.Dim));
    }

    private static IEnumerable<OutputLine> FormatSummary(CheckSummary summary)
    {
        List<(string Label, string Value)> rows =
        [
            ("Taranan dosya", summary.FileCount.ToString(CultureInfo.InvariantCulture)),
            ("Dislanan dosya", summary.ExcludedFileCount.ToString(CultureInfo.InvariantCulture)),
            ("Atlanan klasor", summary.SkippedDirectories.Count.ToString(CultureInfo.InvariantCulture)),
            ("Etkin kural", CodeList(summary.Rules.ActiveCodes)),
            ("Kapali kural", CodeList(summary.Rules.DisabledCodes)),
            ("Bulgu", summary.FindingCount.ToString(CultureInfo.InvariantCulture)),
        ];

        rows.AddRange(summary.ByRuleCode.Select(entry =>
            (entry.RuleCode, entry.Count.ToString(CultureInfo.InvariantCulture))));

        int labelColumn = rows.Max(row => row.Label.Length);

        foreach ((string label, string value) in rows)
        {
            yield return Line(
                new OutputSpan("  " + label.PadRight(labelColumn) + " : ", OutputColor.Dim),
                new OutputSpan(value, OutputColor.Normal));
        }
    }

    /// <summary>Kural kodlarini tek satirda yazar. Bos listeyi "yok" diye gosteriyoruz ki
    /// bos bir satir "acaba yazilmadi mi" diye dusundurmesin.</summary>
    private static string CodeList(IReadOnlyList<string> codes) =>
        codes.Count == 0 ? "yok" : string.Join(", ", codes);

    /// <summary>Seviyenin ekranda gorunen hali. JSON tarafi enum adini kullaniyor, burasi Turkce.</summary>
    private static string SeverityLabel(Severity severity) => severity switch
    {
        Severity.Info => "[bilgi]",
        Severity.Warning => "[uyari]",
        Severity.Error => "[hata]",
        _ => throw new ArgumentOutOfRangeException(nameof(severity), severity, "Bilinmeyen seviye."),
    };

    private static OutputColor SeverityColor(Severity severity) =>
        severity == Severity.Info ? OutputColor.Tag : OutputColor.Warning;

    private static OutputLine Line(params OutputSpan[] spans) => new(spans);

    private static OutputLine EmptyLine() => new([]);
}
