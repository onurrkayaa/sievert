namespace Sievert.Data.Metrics;

/// <summary>Tek bir olcunun dagilimi.</summary>
/// <param name="Metric">Olcunun adi.</param>
/// <param name="Min">En kucuk deger.</param>
/// <param name="Median">Medyan.</param>
/// <param name="P95">95. yuzdelik.</param>
/// <param name="Max">En buyuk deger.</param>
public sealed record MetricRange(string Metric, double Min, double Median, double P95, double Max);

/// <summary>
/// Olculerin dagilimini biriktirir. Ortalama yazmiyorum: bu olculerin cogu uzun kuyruklu
/// (bir commit 500 dosya degistirebiliyor) ve ortalama o kuyrukta kayboluyor. Asama 5'te
/// esik secilecekse medyan ve p95 daha kullanisli (B033: esikler olcumden once ilan
/// edilir, o yuzden dagilim once burada durmali).
/// </summary>
public sealed class MetricDistribution
{
    private readonly Dictionary<string, List<double>> values = new(StringComparer.Ordinal);

    public int Count { get; private set; }

    public void Add(CommitMetrics metrics)
    {
        Count++;

        Record("LinesAdded", metrics.LinesAdded);
        Record("LinesDeleted", metrics.LinesDeleted);
        Record("FilesChanged", metrics.FilesChanged);
        Record("CsFilesChanged", metrics.CsFilesChanged);
        Record("Entropy", metrics.Entropy);
        Record("DirectoryCount", metrics.DirectoryCount);
        Record("SubsystemCount", metrics.SubsystemCount);
        Record("MaxFileAgeDays", metrics.MaxFileAgeDays);
        Record("MinFileAgeDays", metrics.MinFileAgeDays);
        Record("PriorChanges", metrics.PriorChanges);
        Record("PriorFixes", metrics.PriorFixes);
        Record("DistinctAuthorsOnFiles", metrics.DistinctAuthorsOnFiles);
        Record("AuthorCommitCount", metrics.AuthorCommitCount);
        Record("AuthorFileExperience", metrics.AuthorFileExperience);
        Record("IsFix", metrics.IsFix ? 1 : 0);
    }

    public IReadOnlyList<MetricRange> Ranges() =>
        [.. values.Select(entry => Range(entry.Key, entry.Value))];

    private void Record(string metric, double value)
    {
        if (!values.TryGetValue(metric, out List<double>? list))
        {
            list = [];
            values[metric] = list;
        }

        list.Add(value);
    }

    private static MetricRange Range(string metric, List<double> list)
    {
        list.Sort();

        return new MetricRange(metric, list[0], Quantile(list, 0.50), Quantile(list, 0.95), list[^1]);
    }

    /// <summary>
    /// Sirali listeden yuzdelik. En yakin sira yontemi: ara deger uretmiyorum, cunku bu
    /// olculerin cogu tam sayi ve "2,5 dosya" diye bir sey yok.
    /// </summary>
    private static double Quantile(List<double> sorted, double fraction)
    {
        int index = (int)Math.Ceiling(fraction * sorted.Count) - 1;

        return sorted[Math.Clamp(index, 0, sorted.Count - 1)];
    }
}
