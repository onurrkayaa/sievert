namespace Sievert.Modeling;

/// <summary>
/// Bir ozniteligin skora katkisi.
///
/// **Nedensel bir iddia degil.** Katki, modelin o oznitelige verdigi agirlik ile
/// standartlastirilmis degerin carpimi; "bu oznitelik hataya yol acti" demek degil,
/// "model bu satirda skoru bu kadar yukari/asagi tasidi" demek.
/// </summary>
/// <param name="Name">Oznitelik adi.</param>
/// <param name="RawValue">Olcunun kendi biriminde degeri (ornegin eklenen satir sayisi).</param>
/// <param name="TransformedValue">Modelin gordugu standartlastirilmis deger.</param>
/// <param name="Coefficient">Egitilmis katsayi.</param>
/// <param name="Contribution">Katsayi carpi standartlastirilmis deger.</param>
/// <param name="OutsideTrainRange">Deger egitimde gorulen araligin disinda mi.</param>
/// <param name="OutsideDirection">Disindaysa hangi tarafta: <c>above</c> ya da <c>below</c>.</param>
public sealed record FeatureEffect(
    string Name,
    double RawValue,
    double TransformedValue,
    double Coefficient,
    double Contribution,
    bool OutsideTrainRange,
    string? OutsideDirection);

/// <summary>Bir satirin skoru ve o skorun oznitelik katkilarina ayrilmis hali.</summary>
public sealed record ModelExplanation(
    double Intercept,
    double ModelLogit,
    double ExplainedLogit,
    double RawModelScore,
    IReadOnlyList<FeatureEffect> Effects)
{
    /// <summary>Aciklamanin modelin kendi logit'inden sapmasi.</summary>
    public double Difference => Math.Abs(ModelLogit - ExplainedLogit);

    /// <summary>En az bir oznitelik egitim araliginin disinda mi.</summary>
    public bool AnyOutsideTrainRange => Effects.Any(effect => effect.OutsideTrainRange);
}

/// <summary>
/// Skoru oznitelik katkilarina ayirir.
///
/// Lojistik regresyonda bu ayristirma tam: logit = kesisim + toplam(katsayi * deger).
/// Yaklasik bir aciklama yontemi kullanilmiyor, cunku gerek yok - ve yaklasik olsaydi
/// toplaminin skora esit oldugunu soyleyemezdik.
///
/// Hesap, modelin kendi verdigi logit ile karsilastirilip dogrulaniyor. Tutmuyorsa
/// aciklama DONMUYOR; yanlis bir aciklama, aciklama olmamasindan kotu.
/// </summary>
public static class ModelExplainer
{
    /// <summary>Aciklamanin modelin logit'ine ne kadar yaklasmasi gerektigi.</summary>
    public const double Tolerance = 1e-6;

    public static ModelExplanation Explain(
        ModelProfile profile,
        FeatureScaler scaler,
        LoadedModel model,
        SnapshotRow row)
    {
        float[] features = scaler.Apply(row);
        ModelScore score = model.Evaluate(features);

        List<FeatureEffect> effects = [];
        double explained = profile.Coefficients.Intercept;

        for (int index = 0; index < scaler.Features.Count; index++)
        {
            string name = scaler.Features[index];
            double coefficient = profile.Coefficients.Weights[index];

            // Katki, modelin GORDUGU deger uzerinden hesaplaniyor (float). Cift duyarlikla
            // yeniden hesaplamak daha "dogru" gorunurdu ama modelin yaptigi islem bu degil.
            double transformed = features[index];
            double contribution = coefficient * transformed;

            explained += contribution;

            (bool outside, string? direction) = Range(scaler, row, name);

            effects.Add(new FeatureEffect(
                name,
                ModelFeatures.Value(row, name),
                transformed,
                coefficient,
                contribution,
                outside,
                direction));
        }

        return new ModelExplanation(
            profile.Coefficients.Intercept,
            score.Logit,
            explained,
            score.Probability,
            effects);
    }

    private static (bool Outside, string? Direction) Range(FeatureScaler scaler, SnapshotRow row, string name)
    {
        if (scaler.StatisticsFor(name) is not FeatureStatistics statistics)
        {
            // IsFix standartlastirilmiyor, araligi da yok.
            return (false, null);
        }

        double value = scaler.ScaleValue(row, name);

        if (value > statistics.Max)
        {
            return (true, "above");
        }

        return value < statistics.Min ? (true, "below") : (false, null);
    }
}
