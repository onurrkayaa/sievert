namespace Sievert.Modeling;

/// <summary>Yuksek korelasyonlu bir oznitelik cifti.</summary>
public sealed record CorrelatedPair(string First, string Second, double Rho);

/// <summary>
/// Spearman sira korelasyonu. YALNIZCA egitim bolumunde, yalnizca 15 oznitelik arasinda
/// hesaplaniyor; hedef degisken matrise GIRMIYOR.
///
/// Amaci model secmek ya da oznitelik elemek degil: katsayi yorumunun sinirini gostermek.
/// Birbirine cok benzeyen iki oznitelik arasinda katsayinin nasil bolunecegi veriye
/// bagli ve kararsiz olabiliyor.
/// </summary>
public static class SpearmanCorrelation
{
    /// <summary>Raporlanacak esik. Eleme yapilmiyor, yalnizca yaziliyor.</summary>
    public const double ReportAbove = 0.80;

    public static IReadOnlyList<CorrelatedPair> HighPairs(IReadOnlyList<SnapshotRow> train)
    {
        int count = ModelFeatures.Candidates.Count;
        double[][] ranks = new double[count][];

        for (int index = 0; index < count; index++)
        {
            double[] values = new double[train.Count];

            for (int row = 0; row < train.Count; row++)
            {
                values[row] = ModelFeatures.Value(train[row], ModelFeatures.Candidates[index]);
            }

            ranks[index] = Ranks(values);
        }

        List<CorrelatedPair> pairs = [];

        for (int first = 0; first < count; first++)
        {
            for (int second = first + 1; second < count; second++)
            {
                double rho = Pearson(ranks[first], ranks[second]);

                if (double.IsFinite(rho) && Math.Abs(rho) >= ReportAbove)
                {
                    pairs.Add(new CorrelatedPair(
                        ModelFeatures.Candidates[first],
                        ModelFeatures.Candidates[second],
                        rho));
                }
            }
        }

        pairs.Sort((left, right) => Math.Abs(right.Rho).CompareTo(Math.Abs(left.Rho)));

        return pairs;
    }

    /// <summary>Esit degerler ortalama sira aliyor; Spearman'in standart bagli-sira islemi.</summary>
    private static double[] Ranks(double[] values)
    {
        int[] order = [.. Enumerable.Range(0, values.Length)];
        Array.Sort(order, (left, right) => values[left].CompareTo(values[right]));

        double[] ranks = new double[values.Length];
        int index = 0;

        while (index < order.Length)
        {
            int end = index;

            while (end + 1 < order.Length && values[order[end + 1]].Equals(values[order[index]]))
            {
                end++;
            }

            double average = (index + end) / 2.0 + 1;

            for (int position = index; position <= end; position++)
            {
                ranks[order[position]] = average;
            }

            index = end + 1;
        }

        return ranks;
    }

    private static double Pearson(double[] left, double[] right)
    {
        double leftMean = left.Average();
        double rightMean = right.Average();
        double covariance = 0.0;
        double leftSquares = 0.0;
        double rightSquares = 0.0;

        for (int index = 0; index < left.Length; index++)
        {
            double one = left[index] - leftMean;
            double other = right[index] - rightMean;

            covariance += one * other;
            leftSquares += one * one;
            rightSquares += other * other;
        }

        double denominator = Math.Sqrt(leftSquares * rightSquares);

        return denominator == 0 ? double.NaN : covariance / denominator;
    }
}
