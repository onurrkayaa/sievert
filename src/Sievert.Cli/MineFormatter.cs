using System.Globalization;

using Sievert.Core.Mining;
using Sievert.Data;

namespace Sievert.Cli;

/// <summary>mine komutunun ekran ciktisi. Tam veri ekrana degil dosyaya gidiyor.</summary>
public static class MineFormatter
{
    public static IReadOnlyList<OutputLine> Format(
        MiningSummary summary,
        TimeSpan elapsed,
        string? outputPath,
        bool shallow,
        StoreResult? store)
    {
        List<OutputLine> lines =
        [
            Line("git tarihi", OutputColor.Heading),
            Row("commit", summary.CommitCount.ToString(CultureInfo.InvariantCulture)),
            Row("atlanan birlestirme", summary.SkippedMergeCount.ToString(CultureInfo.InvariantCulture)),
            Row("tarih araligi", Range(summary)),
            Row("Shallow", shallow ? "evet" : "hayir"),
            Row("farkli yazar", summary.DistinctAuthorCount.ToString(CultureInfo.InvariantCulture) + " (epostaya gore)"),
            Row("bot gorunumlu commit", summary.BotAuthorCommitCount.ToString(CultureInfo.InvariantCulture)),
            Row("Co-Authored-By satiri", summary.CoAuthorLineCount.ToString(CultureInfo.InvariantCulture)),
            Row("ad degisimi esigi", "%" + summary.RenameSimilarityThreshold.ToString(CultureInfo.InvariantCulture)),
            Row("sure", elapsed.TotalSeconds.ToString("0.00", CultureInfo.InvariantCulture) + " sn"),
        ];

        if (store is not null)
        {
            lines.Add(Row("veritabanina yazilan", store.Written.ToString(CultureInfo.InvariantCulture)));
            lines.Add(Row("zaten kayitli, atlanan", store.Skipped.ToString(CultureInfo.InvariantCulture)));

            if (store.Deleted > 0)
            {
                lines.Add(Row("silinip yeniden yazilan", store.Deleted.ToString(CultureInfo.InvariantCulture)));
            }
        }

        lines.Add(outputPath is null
            ? Line("Tam veri yazilmadi. Istiyorsan --out <dosya> ver.", OutputColor.Dim)
            : Line($"Tam veri yazildi: {outputPath} (her satir bir commit)", OutputColor.Dim));

        return lines;
    }

    private static string Range(MiningSummary summary) =>
        summary.FirstAuthorDateUtc is DateTimeOffset first && summary.LastAuthorDateUtc is DateTimeOffset last
            ? $"{first:yyyy-MM-dd} - {last:yyyy-MM-dd} (UTC)"
            : "commit yok";

    private static OutputLine Row(string label, string value) =>
        new([new OutputSpan(label.PadRight(24), OutputColor.Dim), new OutputSpan(value, OutputColor.Normal)]);

    private static OutputLine Line(string text, OutputColor color) => new([new OutputSpan(text, color)]);
}
