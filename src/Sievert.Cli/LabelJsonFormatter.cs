using System.Text.Encodings.Web;
using System.Text.Json;

using Sievert.Data;
using Sievert.Mining;

namespace Sievert.Cli;

/// <summary>Etiketleme ozetini JSON'a cevirir.</summary>
public static class LabelJsonFormatter
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static string Summary(string repositoryName, LabelResult label, SzzOutcome outcome) =>
        JsonSerializer.Serialize(
            new LabelOutput(
                repositoryName,
                label.Total,
                label.Labelled,
                label.Total == 0 ? 0 : Math.Round(100.0 * label.Labelled / label.Total, 2),
                outcome.ProcessedFixes,
                outcome.SkippedLargeFixes,
                outcome.DroppedByTime,
                outcome.DroppedMerges,
                outcome.BlamedLines,
                outcome.FixesWithCSharpChange,
                outcome.FixesThatBlamed),
            Options);

    private sealed record LabelOutput(
        string Repository,
        int CommitCount,
        int LabelledCommits,
        double LabelRatioPercent,
        int ProcessedFixes,
        int SkippedLargeFixes,
        int DroppedByTime,
        int DroppedMerges,
        int BlamedLines,
        int FixesWithCSharpChange,
        int FixesThatBlamed);
}
