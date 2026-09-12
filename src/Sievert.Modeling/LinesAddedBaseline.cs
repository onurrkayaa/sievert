namespace Sievert.Modeling;

/// <summary>Denenen bir esik ve o esigin egitim bolumunde urettigi sayim.</summary>
public sealed record ThresholdCandidate(double Threshold, Confusion Counts);

/// <summary>Secilen esik, sayimi ve kac aday arasindan secildigi.</summary>
public sealed record ThresholdChoice(double Threshold, Confusion Counts, int CandidateCount);

/// <summary>
/// Esikli bir tabanin bir kumedeki sonucu. PR-AUC iki ayri sayi olarak duruyor:
/// <see cref="RawPrAuc"/> ozniteligin kendisini skor kabul ediyor ve siralama yetenegini
/// olcuyor; <see cref="BinaryPrAuc"/> esik sonrasi 0/1 skorunu kullaniyor ve yalnizca iki
/// grup uretiyor. Ikisi ayni sey degil (metrik sozlesmesi, surum 1.0).
/// </summary>
public sealed record ThresholdOutcome(Confusion Counts, double? RawPrAuc, double? BinaryPrAuc);

/// <summary>
/// Aday esikler arasindan secim kurali. Sirasiyla: en yuksek egitim F1'i, esitlikte en
/// yuksek egitim precision'i, hala esitlikte DAHA YUKSEK esik.
///
/// Ucuncu kuralin gerekcesi: daha yuksek esik daha az alarm uretiyor, yani iki taban ayni
/// sayida dogru buluyorsa muhafazakar olani seciliyor.
///
/// Kural ayri bir sinifta duruyor cunku tek basina sinanabilsin: tek oznitelikli monoton
/// bir esikte ikinci ve ucuncu kuralin gercek veride sonucu degistiremedigi gosterildi
/// (ADR 0017), ama kural yine de yazili ve test edilmis olmali.
/// </summary>
public static class ThresholdSelection
{
    public static ThresholdCandidate Pick(IReadOnlyList<ThresholdCandidate> candidates)
    {
        if (candidates.Count == 0)
        {
            throw new InvalidOperationException("Aday esik yok.");
        }

        ThresholdCandidate best = candidates[0];

        for (int index = 1; index < candidates.Count; index++)
        {
            if (Better(candidates[index], best))
            {
                best = candidates[index];
            }
        }

        return best;
    }

    private static bool Better(ThresholdCandidate candidate, ThresholdCandidate best)
    {
        double one = candidate.Counts.F1 ?? 0.0;
        double other = best.Counts.F1 ?? 0.0;

        if (one != other)
        {
            return one > other;
        }

        double onePrecision = candidate.Counts.Precision ?? 0.0;
        double otherPrecision = best.Counts.Precision ?? 0.0;

        return onePrecision != otherPrecision
            ? onePrecision > otherPrecision
            : candidate.Threshold > best.Threshold;
    }
}

/// <summary>
/// Tek oznitelikli taban: <c>LinesAdded &gt;= esik</c> ise pozitif.
///
/// Esik YALNIZCA egitim bolumunde seciliyor ve aday esikler yalnizca egitimde gorulen
/// degerlerden uretiliyor. Test bolumunden aday uretmek, esigi test verisine bakarak
/// ayarlamak olurdu ve olculen sayi gercek kullanimda alinamayacak bir sayiya donerdi.
/// </summary>
public static class LinesAddedBaseline
{
    public const string Feature = "LinesAdded";

    public static ThresholdChoice Choose(IReadOnlyList<SnapshotRow> train)
    {
        List<ThresholdCandidate> candidates = [.. Candidates(train)];
        ThresholdCandidate best = ThresholdSelection.Pick(candidates);

        return new ThresholdChoice(best.Threshold, best.Counts, candidates.Count);
    }

    public static ThresholdOutcome Apply(IReadOnlyList<SnapshotRow> rows, double threshold)
    {
        List<Scored> raw = new(rows.Count);
        List<Scored> binary = new(rows.Count);

        foreach (SnapshotRow row in rows)
        {
            double value = ModelFeatures.Value(row, Feature);
            bool predicted = value >= threshold;

            raw.Add(new Scored(predicted, value, row.IsBugIntroducing));
            binary.Add(new Scored(predicted, predicted ? 1.0 : 0.0, row.IsBugIntroducing));
        }

        Outcome rawOutcome = Evaluation.Of(raw);

        return new ThresholdOutcome(rawOutcome.Counts, rawOutcome.PrAuc, Evaluation.Of(binary).PrAuc);
    }

    /// <summary>
    /// Adaylar: egitimde gorulen benzersiz degerler. Satirlar degere gore azalan
    /// siralanip tek gecisle taraniyor; her benzersiz degerin sonunda o deger icin
    /// "buyuk ya da esit" sayimi hazir oluyor.
    /// </summary>
    private static IEnumerable<ThresholdCandidate> Candidates(IReadOnlyList<SnapshotRow> train)
    {
        List<(double Value, bool Positive)> rows = new(train.Count);
        int positives = 0;

        foreach (SnapshotRow row in train)
        {
            bool positive = row.IsBugIntroducing;
            rows.Add((ModelFeatures.Value(row, Feature), positive));

            if (positive)
            {
                positives++;
            }
        }

        rows.Sort((left, right) => right.Value.CompareTo(left.Value));

        int truePositives = 0;
        int falsePositives = 0;
        int index = 0;

        while (index < rows.Count)
        {
            double value = rows[index].Value;

            while (index < rows.Count && rows[index].Value.Equals(value))
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

            yield return new ThresholdCandidate(
                value,
                new Confusion(
                    truePositives,
                    falsePositives,
                    positives - truePositives,
                    rows.Count - positives - falsePositives));
        }
    }
}
