using System.Globalization;

using Sievert.Data.Metrics;

namespace Sievert.Cli;

/// <summary>metrics komutunun ekran ciktisi. Dagilim ekrana degil --out dosyasina gidiyor.</summary>
public static class MetricsFormatter
{
    public static IReadOnlyList<OutputLine> Format(
        string repositoryName,
        MetricsResult result,
        TimeSpan elapsed,
        string? outputPath)
    {
        List<OutputLine> lines =
        [
            new([new OutputSpan("commit olculeri", OutputColor.Heading)]),
            Row("depo", repositoryName),
            Row("hesaplanan commit", result.CommitCount.ToString(CultureInfo.InvariantCulture)),
            Row("sure", elapsed.TotalSeconds.ToString("0.00", CultureInfo.InvariantCulture) + " sn"),
        ];

        lines.Add(new OutputLine([
            new OutputSpan(
                outputPath is null
                    ? "Dagilim ozeti yazilmadi. Istiyorsan --out <dosya> ver."
                    : $"Dagilim ozeti yazildi: {outputPath}",
                OutputColor.Dim),
        ]));

        return lines;
    }

    private static OutputLine Row(string label, string value) =>
        new([new OutputSpan(label.PadRight(20), OutputColor.Dim), new OutputSpan(value, OutputColor.Normal)]);
}
