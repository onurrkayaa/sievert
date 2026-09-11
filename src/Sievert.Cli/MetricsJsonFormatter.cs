using System.Text.Encodings.Web;
using System.Text.Json;

using Sievert.Data.Metrics;

namespace Sievert.Cli;

/// <summary>
/// Dagilim ozetini JSON'a cevirir. Burada JSONL yok: tek bir ozet nesnesi, satir satir
/// okunacak bir sey degil.
/// </summary>
public static class MetricsJsonFormatter
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static string Distribution(string repositoryName, MetricDistribution distribution) =>
        JsonSerializer.Serialize(
            new DistributionOutput(repositoryName, distribution.Count, distribution.Ranges()),
            Options);

    private sealed record DistributionOutput(
        string Repository,
        int CommitCount,
        IReadOnlyList<MetricRange> Metrics);
}
