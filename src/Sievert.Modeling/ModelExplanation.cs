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
    public double AbsoluteError => Math.Abs(ModelLogit - ExplainedLogit);

    /// <summary>Eski ad; olcum ve rapor kodu bunu kullaniyordu.</summary>
    public double Difference => AbsoluteError;

    /// <summary>
    /// Toplamanin sayisal olcegi: kesisim ve katkilarin MUTLAK toplami, en az 1.
    ///
    /// Logit'in kendisi olcek degil. Buyuk pozitif ve buyuk negatif katkilar birbirini
    /// goturdugunde sonuc kucuk kaliyor ama temsil hatasi buyuk terimlerin olceginde
    /// olusuyor; Adim 2'de olculen en kotu satir tam olarak boyleydi.
    /// </summary>
    public double Scale => Math.Max(
        1.0,
        Math.Abs(Intercept) + Effects.Sum(effect => Math.Abs(effect.Contribution)));

    /// <summary>Bu satirda kabul edilen en buyuk fark. Sozlesme surum 2.0.</summary>
    public double AllowedError =>
        ModelExplainer.AbsoluteToleranceV1 + (ModelExplainer.FloatUnitRoundoff * Scale);

    /// <summary>
    /// Farkin izin verilen paya orani. Butun satirlarda ayni anlama geldigi icin
    /// raporlanan olcu bu; ham mutlak fark olcege gore yaniltici olurdu.
    /// </summary>
    public double NormalizedError => AbsoluteError / AllowedError;

    /// <summary>Aciklama modelle tutuyor mu.</summary>
    public bool IsWithinTolerance => NormalizedError <= 1.0;

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
    /// <summary>
    /// Surum 1.0'in mutlak toleransi. Surum 2.0'da taban olarak duruyor.
    ///
    /// Tek basina kullanildiginda 34 166 satirin 11'inde tutmamisti; olculen sonuc
    /// docs/olcumler/asama6-api-temel.md bolum 7'de duruyor ve silinmedi.
    /// </summary>
    public const double AbsoluteToleranceV1 = 1e-6;

    /// <summary>
    /// <c>float</c> makine epsilonu, yani 2^-23.
    ///
    /// <c>float.Epsilon</c> DEGIL: o sabit temsil edilebilir en kucuk pozitif subnormal
    /// sayi (yaklasik 1,4e-45) ve buraya konursa tolerans pratikte sifirlanir. Deger
    /// burada acikca 2 uzeri -23 olarak yaziliyor.
    /// </summary>
    public const double FloatUnitRoundoff = 1.0 / (1 << 23);

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
