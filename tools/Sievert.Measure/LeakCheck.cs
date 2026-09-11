using Microsoft.EntityFrameworkCore;

using Sievert.Data;
using Sievert.Data.Entities;
using Sievert.Data.Metrics;

namespace Sievert.Measure;

/// <summary>
/// Saglik kontrolu: kayitli metrikler, ayni ham veriden yeniden hesaplananlarla ayni mi.
/// Rastgele secilen commit'lerin butun olculeri karsilastiriliyor. Fark cikarsa ya hesap
/// deterministik degildir ya da kayitli veri baska bir surumden kalmistir; ikisi de
/// Asama 5'in modelini sessizce bozardi.
/// </summary>
public static class LeakCheck
{
    private const int Seed = 42;

    public static void Report(SievertContext context, MetricsRunner runner, int repositoryId, int sampleSize)
    {
        Dictionary<int, CommitMetrics> recomputed = new MetricCalculator(MetricOptions.Default)
            .Compute(runner.Read(repositoryId))
            .ToDictionary(metrics => metrics.CommitId);

        List<int> ids = [.. recomputed.Keys];
        Random random = new(Seed);
        List<int> sample = [.. ids.OrderBy(_ => random.Next()).Take(sampleSize)];

        Dictionary<int, CommitMetricRow> stored = context.CommitMetrics
            .AsNoTracking()
            .Where(row => sample.Contains(row.CommitId))
            .ToDictionary(row => row.CommitId);

        int same = 0;
        int different = 0;
        int missing = 0;

        foreach (int id in sample)
        {
            if (!stored.TryGetValue(id, out CommitMetricRow? row))
            {
                missing++;
                continue;
            }

            if (Matches(row, recomputed[id]))
            {
                same++;
            }
            else
            {
                different++;
                Console.WriteLine($"  fark: CommitId {id}");
            }
        }

        Console.WriteLine($"orneklem: {sample.Count}, ayni: {same}, farkli: {different}, kayitli degil: {missing}");
    }

    private static bool Matches(CommitMetricRow stored, CommitMetrics fresh) =>
        stored.LinesAdded == fresh.LinesAdded
        && stored.LinesDeleted == fresh.LinesDeleted
        && stored.FilesChanged == fresh.FilesChanged
        && stored.CsFilesChanged == fresh.CsFilesChanged
        && Math.Abs(stored.Entropy - fresh.Entropy) < 1e-9
        && stored.DirectoryCount == fresh.DirectoryCount
        && stored.SubsystemCount == fresh.SubsystemCount
        && stored.MaxFileAgeDays == fresh.MaxFileAgeDays
        && stored.MinFileAgeDays == fresh.MinFileAgeDays
        && stored.PriorChanges == fresh.PriorChanges
        && stored.PriorFixes == fresh.PriorFixes
        && stored.DistinctAuthorsOnFiles == fresh.DistinctAuthorsOnFiles
        && stored.AuthorCommitCount == fresh.AuthorCommitCount
        && stored.AuthorFileExperience == fresh.AuthorFileExperience
        && stored.IsFix == fresh.IsFix;
}
