using System.Globalization;

using Sievert.Data;
using Sievert.Mining;

namespace Sievert.Cli;

/// <summary>label komutunun ekran ciktisi.</summary>
public static class LabelFormatter
{
    public static IReadOnlyList<OutputLine> Format(
        string repositoryName,
        LabelResult label,
        SzzOutcome outcome,
        TimeSpan elapsed,
        string? outputPath)
    {
        double ratio = label.Total == 0 ? 0 : 100.0 * label.Labelled / label.Total;

        List<OutputLine> lines =
        [
            new([new OutputSpan("SZZ etiketleme", OutputColor.Heading)]),
            Row("depo", repositoryName),
            Row("islenen duzeltme", outcome.ProcessedFixes.ToString(CultureInfo.InvariantCulture)),
            Row("atlanan buyuk duzeltme", outcome.SkippedLargeFixes.ToString(CultureInfo.InvariantCulture)),
            Row(".cs degistiren duzeltme", outcome.FixesWithCSharpChange.ToString(CultureInfo.InvariantCulture)),
            Row("birini suclayan duzeltme", outcome.FixesThatBlamed.ToString(CultureInfo.InvariantCulture)),
            Row("etiketlenen commit", label.Labelled.ToString(CultureInfo.InvariantCulture)),
            Row("etiket orani", "%" + ratio.ToString("0.0", CultureInfo.InvariantCulture)),
            Row("atilan (zaman tutarsiz)", outcome.DroppedByTime.ToString(CultureInfo.InvariantCulture)),
            Row("atilan (birlestirme)", outcome.DroppedMerges.ToString(CultureInfo.InvariantCulture)),
            Row("blame'e sorulan satir", outcome.BlamedLines.ToString(CultureInfo.InvariantCulture)),
            Row("sure", elapsed.TotalSeconds.ToString("0.00", CultureInfo.InvariantCulture) + " sn"),
        ];

        lines.Add(new OutputLine([
            new OutputSpan(
                outputPath is null
                    ? "Ozet yazilmadi. Istiyorsan --out <dosya> ver."
                    : $"Ozet yazildi: {outputPath}",
                OutputColor.Dim),
        ]));

        return lines;
    }

    private static OutputLine Row(string label, string value) =>
        new([new OutputSpan(label.PadRight(24), OutputColor.Dim), new OutputSpan(value, OutputColor.Normal)]);
}
