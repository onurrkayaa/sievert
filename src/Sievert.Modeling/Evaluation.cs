namespace Sievert.Modeling;

/// <summary>Bir satirin tahmini, skoru ve gercek etiketi.</summary>
/// <param name="Predicted">Ikili tahmin.</param>
/// <param name="Score">Siralama icin surekli skor. Sabit skor siralama bilgisi tasimaz.</param>
/// <param name="Actual">Gercek etiket (<c>IsBugIntroducing</c>).</param>
public readonly record struct Scored(bool Predicted, double Score, bool Actual);

/// <summary>Bir kumede olculen sayilar. Accuracy bilerek yok (ADR 0017).</summary>
public sealed record Outcome(Confusion Counts, double? PrAuc);

/// <summary>
/// Tahmin listesinden sayimi ve PR-AUC'yi birlikte cikarir. Butun tabanlarin ve ileride
/// modelin ayni yerden olculmesi icin var; iki yontem farkli formullerle olculurse
/// karsilastirilamazlar.
/// </summary>
public static class Evaluation
{
    public static Outcome Of(IReadOnlyList<Scored> rows)
    {
        List<Prediction> predictions = new(rows.Count);
        List<ScoredRow> scored = new(rows.Count);

        foreach (Scored row in rows)
        {
            predictions.Add(new Prediction(row.Predicted, row.Actual));
            scored.Add(new ScoredRow(row.Score, row.Actual));
        }

        return new Outcome(Confusion.From(predictions), PrecisionRecall.Area(scored));
    }
}
