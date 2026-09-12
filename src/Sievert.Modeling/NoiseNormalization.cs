namespace Sievert.Modeling;

/// <summary>
/// Sentetik gurultu sonuclarini taban oranindan ayiran olculer. Tanimlar
/// <c>docs/olcumler/asama5-gurultu-normalizasyon-sozlesmesi.md</c> surum 1.0'da.
///
/// Hepsi SENTETIK etiketlere gore hesaplaniyor; gercek performans iddiasi degil.
/// </summary>
public static class NoiseNormalization
{
    /// <summary>
    /// PR-AUC lift = PR-AUC − test pozitif orani. Sabit skorlu bir tabanin PR-AUC'si
    /// taban oranina esit oldugu icin bu, siralamanin taban orandan ne kadar fazlasi.
    /// </summary>
    public static double? Lift(double? prAuc, double positiveRate) =>
        prAuc is double area ? area - positiveRate : null;

    /// <summary>
    /// Normalize PR-AUC = lift / (1 − taban orani). Taban orani 1 ise payda 0 ve deger
    /// hesaplanmiyor.
    /// </summary>
    public static double? Normalised(double? prAuc, double positiveRate) =>
        prAuc is double area && positiveRate < 1.0
            ? (area - positiveRate) / (1.0 - positiveRate)
            : null;

    /// <summary>
    /// Climatology Brier: her test satirina sentetik TRAIN pozitif orani olasilik olarak
    /// veriliyor. Veriye hic bakmayan ama taban orani bilen bir tahmincinin Brier'i.
    /// </summary>
    public static double Climatology(IReadOnlyList<ScoredProbability> test, double trainPositiveRate)
    {
        List<ScoredProbability> flat = new(test.Count);

        foreach (ScoredProbability row in test)
        {
            flat.Add(new ScoredProbability(trainPositiveRate, row.Actual));
        }

        return Calibration.Brier(flat);
    }

    /// <summary>
    /// Brier skill score = 1 − (Brier_model / Brier_climatology). Climatology 0 ise
    /// payda 0 ve deger hesaplanmiyor.
    /// </summary>
    public static double? SkillScore(double brier, double climatology) =>
        climatology > 0 ? 1.0 - (brier / climatology) : null;

    /// <summary>Bir kumedeki pozitif orani.</summary>
    public static double PositiveRate(IReadOnlyList<SnapshotRow> rows)
    {
        if (rows.Count == 0)
        {
            return double.NaN;
        }

        int positives = 0;

        foreach (SnapshotRow row in rows)
        {
            if (row.IsBugIntroducing)
            {
                positives++;
            }
        }

        return (double)positives / rows.Count;
    }

    /// <summary>
    /// Eslenmis rastgele taban: her test satirina p olasilikla pozitif; p sentetik
    /// train'in pozitif orani. Skor surekli rastgele sayi, yani siralama bilgisi tasiyor
    /// ama etiketten bagimsiz.
    /// </summary>
    public static IReadOnlyList<Scored> RandomBaselineFor(
        IReadOnlyList<SnapshotRow> test,
        double probability,
        Random random)
    {
        List<Scored> scored = new(test.Count);

        foreach (SnapshotRow row in test)
        {
            double score = random.NextDouble();
            scored.Add(new Scored(score < probability, score, row.IsBugIntroducing));
        }

        return scored;
    }

    /// <summary>
    /// Eslenmis <c>LinesAdded</c> tabani: esik YALNIZCA sentetik train etiketlerinden
    /// seciliyor, test etiketleri secime girmiyor. Brier gerekiyorsa 0/1 tahmin skoruyla;
    /// ham <c>LinesAdded</c> olasilik gibi kullanilmiyor.
    /// </summary>
    public static (double Threshold, Confusion Counts, double? PrAuc, double Brier) LinesAddedFor(
        IReadOnlyList<SnapshotRow> train,
        IReadOnlyList<SnapshotRow> test)
    {
        double threshold = LinesAddedBaseline.Choose(train).Threshold;
        ThresholdOutcome outcome = LinesAddedBaseline.Apply(test, threshold);

        List<ScoredProbability> binary = new(test.Count);

        foreach (SnapshotRow row in test)
        {
            bool predicted = ModelFeatures.Value(row, LinesAddedBaseline.Feature) >= threshold;
            binary.Add(new ScoredProbability(predicted ? 1.0 : 0.0, row.IsBugIntroducing));
        }

        return (threshold, outcome.Counts, outcome.RawPrAuc, Calibration.Brier(binary));
    }
}
