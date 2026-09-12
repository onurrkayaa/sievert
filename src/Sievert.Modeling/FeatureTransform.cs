namespace Sievert.Modeling;

/// <summary>
/// Donusum sozlesmesi. Adim 3'te, hicbir model sonucu gorulmeden sabitlendi; sonradan
/// sonuca bakarak degistirilmez.
///
/// Sayim ve buyukluk olculerine log1p uygulaniyor cunku hepsi uzun kuyruklu: ShareX'te
/// bir commit 165 538 satir ekleyebiliyor, medyan ise 21. Log almadan tek bir uc deger
/// standartlastirmayi tek basina belirler.
///
/// Entropy zaten sinirli bir araliktaki bir olcu (0 ile log2(dosya sayisi) arasi), o
/// yuzden log uygulanmiyor ama standartlastiriliyor. IsFix mantiksal; ne log ne
/// standartlastirma goruyor, 0/1 kaliyor.
/// </summary>
public static class FeatureTransform
{
    /// <summary>log1p uygulanan 13 sayim/buyukluk olcusu.</summary>
    public static readonly IReadOnlyList<string> LogFeatures =
    [
        "LinesAdded",
        "LinesDeleted",
        "FilesChanged",
        "CsFilesChanged",
        "DirectoryCount",
        "SubsystemCount",
        "MaxFileAgeDays",
        "MinFileAgeDays",
        "PriorChanges",
        "PriorFixes",
        "DistinctAuthorsOnFiles",
        "AuthorCommitCount",
        "AuthorFileExperience",
    ];

    /// <summary>Log uygulanmayan ama standartlastirilan tek surekli olcu.</summary>
    public const string RawContinuousFeature = "Entropy";

    /// <summary>Ne log ne standartlastirma goren mantiksal alan.</summary>
    public const string FlagFeature = "IsFix";

    /// <summary>
    /// log(1 + x). Negatif deger bu veri kumesinde olmamali; olursa sessizce duzeltmek
    /// yerine duruluyor, cunku negatif bir sayim alani veri hatasidir.
    /// </summary>
    public static double Log1P(double value) => value >= 0
        ? Math.Log(1 + value)
        : throw new InvalidDataException(
            $"log1p negatif degere uygulanamaz: {value.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
}

/// <summary>Bir ozniteligin egitim bolumundeki dagilimi; log uygulanan alanlarda log olcegindedir.</summary>
public sealed record FeatureStatistics(string Name, double Min, double Max, double Mean, double StandardDeviation);

/// <summary>
/// Donusum ve standartlastirma parametreleri. YALNIZCA egitim bolumunden ogreniliyor;
/// test satirlari <see cref="Fit"/> asamasina hic girmiyor.
///
/// Testte egitim araligi disinda kalan degerler KIRPILMIYOR: ayni donusum uygulanip
/// gecilyor ve kac satirin arali disinda kaldigi ayrica sayiliyor. Kirpmak, gercek
/// kullanimda gorulecek buyuk commit'leri egitimde gorulen en buyugu gibi gostermek olurdu.
/// </summary>
public sealed class FeatureScaler
{
    private readonly Dictionary<string, FeatureStatistics> statistics;

    private FeatureScaler(string identity, Dictionary<string, FeatureStatistics> statistics)
    {
        Identity = identity;
        this.statistics = statistics;
    }

    public string Identity { get; }

    /// <summary>Standartlastirilan 14 surekli oznitelik. IsFix burada yok.</summary>
    public IReadOnlyList<FeatureStatistics> Statistics => [.. statistics.Values];

    /// <summary>
    /// Kullanilan oznitelik adlari, <see cref="ModelFeatures.Candidates"/> sirasinda.
    /// Ana modelde 15'in hepsi; ablasyon deneyinde bir tanesi cikarilmis hali.
    /// </summary>
    public IReadOnlyList<string> Features { get; private init; } = ModelFeatures.Candidates;

    public static FeatureScaler Fit(string identity, IReadOnlyList<SnapshotRow> train) =>
        Fit(identity, train, ModelFeatures.Candidates);

    /// <summary>
    /// Dondurulmus kanit dosyasindaki egitim istatistiklerinden olcekleyiciyi kurar.
    /// Egitim verisini yeniden okumadan ayni donusumu uygulamak icin: istatistikler
    /// <c>model-results.json</c> icinde kayitli ve o dosya dondurulmus.
    /// </summary>
    public static FeatureScaler FromStatistics(string identity, IReadOnlyList<FeatureStatistics> statistics)
    {
        Dictionary<string, FeatureStatistics> learned = new(StringComparer.Ordinal);

        foreach (FeatureStatistics entry in statistics)
        {
            learned[entry.Name] = entry.StandardDeviation > 0
                ? entry
                : throw new InvalidDataException(
                    $"{identity}: {entry.Name} ozniteligi sifir varyansli kaydedilmis.");
        }

        foreach (string name in Continuous(ModelFeatures.Candidates))
        {
            if (!learned.ContainsKey(name))
            {
                throw new InvalidDataException($"{identity}: {name} ozniteligi icin egitim istatistigi yok.");
            }
        }

        return new FeatureScaler(identity, learned);
    }

    public static FeatureScaler Fit(
        string identity,
        IReadOnlyList<SnapshotRow> train,
        IReadOnlyList<string> features)
    {
        Dictionary<string, FeatureStatistics> learned = new(StringComparer.Ordinal);

        foreach (string name in Continuous(features))
        {
            double[] values = new double[train.Count];

            for (int index = 0; index < train.Count; index++)
            {
                values[index] = Value(train[index], name);
            }

            learned[name] = Describe(identity, name, values);
        }

        return new FeatureScaler(identity, learned) { Features = features };
    }

    /// <summary>Kullanilan oznitelikler, <see cref="Features"/> ile ayni sirada.</summary>
    public float[] Apply(SnapshotRow row)
    {
        float[] features = new float[Features.Count];

        for (int index = 0; index < features.Length; index++)
        {
            string name = Features[index];

            if (string.Equals(name, FeatureTransform.FlagFeature, StringComparison.Ordinal))
            {
                features[index] = (float)ModelFeatures.Value(row, name);
                continue;
            }

            FeatureStatistics entry = statistics[name];
            features[index] = (float)((Value(row, name) - entry.Mean) / entry.StandardDeviation);
        }

        return features;
    }

    /// <summary>
    /// Bir ozniteligin donusum uygulanmis (log alinmis) ama standartlastirilmamis degeri.
    /// Aciklama uretirken egitim araligiyla karsilastirmak icin gerekiyor: aralik da ayni
    /// olcekte kayitli.
    /// </summary>
    public double ScaleValue(SnapshotRow row, string name) => Value(row, name);

    /// <summary>Ozniteligin egitim istatistigi. IsFix standartlastirilmadigi icin onda null.</summary>
    public FeatureStatistics? StatisticsFor(string name) => statistics.GetValueOrDefault(name);

    /// <summary>Egitimde gorulen [min, max] araliginin disinda kalan DEGER sayisi.</summary>
    public int OutsideTrainRange(IReadOnlyList<SnapshotRow> rows)
    {
        int outside = 0;

        foreach (SnapshotRow row in rows)
        {
            outside += OutsideCount(row);
        }

        return outside;
    }

    /// <summary>En az bir ozniteligi aralik disinda kalan SATIR sayisi.</summary>
    public int OutsideTrainRangeRows(IReadOnlyList<SnapshotRow> rows)
    {
        int outside = 0;

        foreach (SnapshotRow row in rows)
        {
            if (OutsideCount(row) > 0)
            {
                outside++;
            }
        }

        return outside;
    }

    private int OutsideCount(SnapshotRow row)
    {
        int outside = 0;

        foreach (string name in Continuous())
        {
            double value = Value(row, name);
            FeatureStatistics entry = statistics[name];

            if (value < entry.Min || value > entry.Max)
            {
                outside++;
            }
        }

        return outside;
    }

    private IEnumerable<string> Continuous() => Continuous(Features);

    private static IEnumerable<string> Continuous(IReadOnlyList<string> features) =>
        [.. features.Where(name => !string.Equals(name, FeatureTransform.FlagFeature, StringComparison.Ordinal))];

    private static double Value(SnapshotRow row, string name)
    {
        double raw = ModelFeatures.Value(row, name);

        return FeatureTransform.LogFeatures.Contains(name, StringComparer.Ordinal)
            ? FeatureTransform.Log1P(raw)
            : raw;
    }

    /// <summary>
    /// Standart sapma 0 ise model egitilmiyor. Sifir varyansli bir ozniteligi sessizce
    /// atmak, sonradan "model 15 oznitelikle egitildi" cumlesini yanlis yapardi.
    /// </summary>
    private static FeatureStatistics Describe(string identity, string name, double[] values)
    {
        double minimum = double.MaxValue;
        double maximum = double.MinValue;
        double total = 0.0;

        foreach (double value in values)
        {
            minimum = Math.Min(minimum, value);
            maximum = Math.Max(maximum, value);
            total += value;
        }

        double mean = total / values.Length;
        double squares = 0.0;

        foreach (double value in values)
        {
            squares += (value - mean) * (value - mean);
        }

        double deviation = Math.Sqrt(squares / values.Length);

        return deviation > 0
            ? new FeatureStatistics(name, minimum, maximum, mean, deviation)
            : throw new InvalidDataException(
                $"{identity} deposunun egitim bolumunde {name} ozniteligi sifir varyansli "
                + $"(butun satirlarda {minimum.ToString(System.Globalization.CultureInfo.InvariantCulture)}). "
                + "Model egitilmedi.");
    }
}
