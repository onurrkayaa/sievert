namespace Sievert.Modeling;

/// <summary>Tek bir satirin tahmini ve gercek etiketi.</summary>
public readonly record struct Prediction(bool Predicted, bool Actual);

/// <summary>
/// TP / FP / FN / TN sayimi ve ondan cikan oranlar. Tanimlar
/// <c>docs/olcumler/asama5-metrik-sozlesmesi.md</c> surum 1.0'da.
///
/// Oranlar <c>double?</c>: paydasi 0 olan bir oran sessizce 0 ya da 1 yapilmiyor, N/A
/// olarak (null) donuyor. "Precision 0" ile "hic pozitif tahmin yok" farkli seyler ve
/// ikisini ayni sayiyla yazmak tabanlari birbirine benzetirdi.
///
/// Accuracy bilerek YOK. Gerekcesi ADR 0017'de.
/// </summary>
public sealed record Confusion(int TruePositives, int FalsePositives, int FalseNegatives, int TrueNegatives)
{
    public int Total => TruePositives + FalsePositives + FalseNegatives + TrueNegatives;

    public int PredictedPositives => TruePositives + FalsePositives;

    public int ActualPositives => TruePositives + FalseNegatives;

    /// <summary>TP / (TP + FP). Hic pozitif tahmin yoksa N/A.</summary>
    public double? Precision => PredictedPositives == 0
        ? null
        : (double)TruePositives / PredictedPositives;

    /// <summary>TP / (TP + FN). Kumede hic gercek pozitif yoksa N/A.</summary>
    public double? Recall => ActualPositives == 0
        ? null
        : (double)TruePositives / ActualPositives;

    /// <summary>
    /// 2PR / (P + R). Recall N/A ise F1 de N/A: kumede olculecek pozitif yok.
    /// Precision N/A ise (hic pozitif tahmin yok) F1 = 0; gerekcesi sozlesmede.
    /// </summary>
    public double? F1
    {
        get
        {
            if (Recall is not double recall)
            {
                return null;
            }

            if (Precision is not double precision)
            {
                return 0.0;
            }

            return precision + recall == 0 ? 0.0 : 2 * precision * recall / (precision + recall);
        }
    }

    public static Confusion From(IEnumerable<Prediction> predictions)
    {
        int truePositives = 0;
        int falsePositives = 0;
        int falseNegatives = 0;
        int trueNegatives = 0;

        foreach (Prediction prediction in predictions)
        {
            if (prediction.Predicted)
            {
                if (prediction.Actual)
                {
                    truePositives++;
                }
                else
                {
                    falsePositives++;
                }
            }
            else if (prediction.Actual)
            {
                falseNegatives++;
            }
            else
            {
                trueNegatives++;
            }
        }

        return new Confusion(truePositives, falsePositives, falseNegatives, trueNegatives);
    }
}
