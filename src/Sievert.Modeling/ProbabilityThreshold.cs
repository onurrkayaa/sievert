namespace Sievert.Modeling;

/// <summary>
/// Ham olasilik uzerinden ikili tahmin. Iki esik birlikte raporlaniyor:
/// sabit 0,5 ve egitimde F1'e gore secilen esik.
///
/// Esik secimi <see cref="ThresholdSelection"/> kurallarini kullaniyor; taban cizgisiyle
/// ayni kural olsun diye ayri bir secim yolu yazilmadi (ADR 0017).
/// </summary>
public static class ProbabilityThreshold
{
    /// <summary>Sabit esik.</summary>
    public const double Fixed = 0.5;

    /// <summary>
    /// Aday esikler YALNIZCA egitim tahminlerinden uretiliyor; test etiketleri secime
    /// hic girmiyor. Kural: en yuksek egitim F1'i, esitlikte yuksek precision, hala
    /// esitlikte daha yuksek esik.
    /// </summary>
    public static ThresholdChoice Choose(IReadOnlyList<ScoredProbability> train)
    {
        List<(double Probability, bool Positive)> rows = new(train.Count);
        int positives = 0;

        foreach (ScoredProbability row in train)
        {
            rows.Add((row.Probability, row.Actual));

            if (row.Actual)
            {
                positives++;
            }
        }

        rows.Sort((left, right) => right.Probability.CompareTo(left.Probability));

        List<ThresholdCandidate> candidates = [];
        int truePositives = 0;
        int falsePositives = 0;
        int index = 0;

        while (index < rows.Count)
        {
            double probability = rows[index].Probability;

            while (index < rows.Count && rows[index].Probability.Equals(probability))
            {
                if (rows[index].Positive)
                {
                    truePositives++;
                }
                else
                {
                    falsePositives++;
                }

                index++;
            }

            candidates.Add(new ThresholdCandidate(
                probability,
                new Confusion(
                    truePositives,
                    falsePositives,
                    positives - truePositives,
                    rows.Count - positives - falsePositives)));
        }

        ThresholdCandidate best = ThresholdSelection.Pick(candidates);

        return new ThresholdChoice(best.Threshold, best.Counts, candidates.Count);
    }

    /// <summary>
    /// Esigi uygular. PR-AUC her zaman HAM olasilikla hesaplaniyor, 0/1 tahminle degil:
    /// esik sonrasi skor yalnizca iki grup uretir ve siralamayi olcmez.
    /// </summary>
    public static IReadOnlyList<Scored> Apply(IReadOnlyList<ScoredProbability> rows, double threshold)
    {
        List<Scored> scored = new(rows.Count);

        foreach (ScoredProbability row in rows)
        {
            scored.Add(new Scored(row.Probability >= threshold, row.Probability, row.Actual));
        }

        return scored;
    }
}
