namespace Sievert.Modeling;

/// <summary>Tek bir kalibrasyon kutusu.</summary>
public sealed record CalibrationBin(
    double Lower,
    double Upper,
    int Count,
    double MeanPrediction,
    int Positives)
{
    /// <summary>Kutudaki gercek pozitif orani. Bos kutuda tanimsiz.</summary>
    public double? ObservedRate => Count == 0 ? null : (double)Positives / Count;

    /// <summary>|ortalama tahmin - gozlenen oran|. Bos kutuda tanimsiz.</summary>
    public double? Gap => ObservedRate is double observed ? Math.Abs(MeanPrediction - observed) : null;
}

/// <summary>Ham olasilik kalitesi: Brier, ECE ve kutu tablosu.</summary>
public sealed record CalibrationResult(double Brier, double Ece, IReadOnlyList<CalibrationBin> Bins, int Count);

/// <summary>
/// Ham olasiligin kalitesi. Bu adimda sonradan kalibrasyon (Platt, isotonic) UYGULANMIYOR;
/// yalnizca modelin kendi urettigi olasilik olculuyor.
///
/// Iki olcu birlikte yaziliyor. Brier butun hatayi tek sayida topluyor ama ayrismasini
/// gostermiyor; ECE kutu bazinda sapmayi gosteriyor ama kutu secimine duyarli ve tek
/// basina yaniltici olabiliyor.
/// </summary>
public static class Calibration
{
    /// <summary>10 esit genislikli kutu: [0,0 - 0,1), [0,1 - 0,2), ... [0,9 - 1,0].</summary>
    public const int BinCount = 10;

    /// <summary>sum((p - y)^2) / N.</summary>
    public static double Brier(IReadOnlyList<ScoredProbability> rows)
    {
        double total = 0.0;

        foreach (ScoredProbability row in rows)
        {
            double label = row.Actual ? 1.0 : 0.0;
            total += (row.Probability - label) * (row.Probability - label);
        }

        return rows.Count == 0 ? double.NaN : total / rows.Count;
    }

    /// <summary>
    /// ECE = sum((kutu sayisi / N) * |ortalama tahmin - gozlenen oran|).
    ///
    /// Bos kutular toplama 0 agirlikla giriyor: icinde satir olmayan bir kutu icin
    /// ortalama uydurulmuyor, kutu tabloda satir sayisi 0 ile duruyor.
    /// </summary>
    public static CalibrationResult Measure(IReadOnlyList<ScoredProbability> rows)
    {
        double[] sums = new double[BinCount];
        int[] counts = new int[BinCount];
        int[] positives = new int[BinCount];

        foreach (ScoredProbability row in rows)
        {
            int index = BinOf(row.Probability);
            sums[index] += row.Probability;
            counts[index]++;

            if (row.Actual)
            {
                positives[index]++;
            }
        }

        List<CalibrationBin> bins = [];
        double ece = 0.0;

        for (int index = 0; index < BinCount; index++)
        {
            double mean = counts[index] == 0 ? 0.0 : sums[index] / counts[index];
            CalibrationBin bin = new(
                index / (double)BinCount,
                (index + 1) / (double)BinCount,
                counts[index],
                mean,
                positives[index]);

            bins.Add(bin);

            if (bin.Gap is double gap && rows.Count > 0)
            {
                ece += counts[index] / (double)rows.Count * gap;
            }
        }

        return new CalibrationResult(Brier(rows), ece, bins, rows.Count);
    }

    /// <summary>
    /// Kutu secimi: alt sinir dahil, ust sinir haric; son kutu 1,0'i ICERIYOR.
    /// Boylece olasiligi tam 1,0 olan bir satir kaybolmuyor.
    /// </summary>
    public static int BinOf(double probability)
    {
        int index = (int)(probability * BinCount);

        return Math.Clamp(index, 0, BinCount - 1);
    }
}

/// <summary>Bir satirin ham olasiligi ve gercek etiketi.</summary>
public readonly record struct ScoredProbability(double Probability, bool Actual);
