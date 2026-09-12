namespace Sievert.Modeling;

/// <summary>
/// Platt scaling: ham olasiligin logit'i uzerinde tek degiskenli lojistik donusum.
/// Tanim <c>docs/olcumler/asama5-kalibrasyon-sozlesmesi.md</c> surum 1.0'da.
///
/// Parametreler YALNIZCA calibration bolumunun etiketlerinden ogreniliyor; test
/// etiketleri fit'e hic girmiyor.
/// </summary>
public sealed record PlattCalibration(double Slope, double Offset, int Iterations)
{
    /// <summary>Newton adimi olabilirligi kotulestirirse kac kez yariya bolunecegi.</summary>
    public const int MaximumHalvings = 50;

    /// <summary>Logit alinabilmesi icin olasiligin kirpildigi aralik.</summary>
    public const double Clamp = 1e-15;

    /// <summary>Parametre degisiminin en buyuk mutlak degeri bunun altina inince duruyor.</summary>
    public const double Tolerance = 1e-10;

    public const int MaximumIterations = 200;

    /// <summary>Hessian'in determinanti bunun altindaysa matris tekil sayiliyor.</summary>
    public const double SingularLimit = 1e-12;

    public double Apply(double probability) => Sigmoid(Slope * Logit(probability) + Offset);

    public static double Logit(double probability)
    {
        double clamped = Math.Clamp(probability, Clamp, 1 - Clamp);

        return Math.Log(clamped / (1 - clamped));
    }

    public static double Sigmoid(double value) => 1.0 / (1.0 + Math.Exp(-value));

    /// <summary>
    /// Newton-Raphson (IRLS). Iki parametreli bir problem oldugu icin gradyan ve Hessian
    /// kapali bicimde yaziliyor; tohum gerekmiyor ve sonuc deterministik.
    ///
    /// Baslangic a = 1, b = 0, yani ham olasiligin kendisi. Yakinsamazsa ya da donusum
    /// azalan cikarsa sonuc SESSIZCE kullanilmiyor, hata veriliyor.
    /// </summary>
    public static PlattCalibration Fit(IReadOnlyList<ScoredProbability> calibration)
    {
        double[] logits = new double[calibration.Count];
        double[] labels = new double[calibration.Count];

        for (int index = 0; index < calibration.Count; index++)
        {
            logits[index] = Logit(calibration[index].Probability);
            labels[index] = calibration[index].Actual ? 1.0 : 0.0;
        }

        double slope = 1.0;
        double offset = 0.0;

        for (int iteration = 1; iteration <= MaximumIterations; iteration++)
        {
            double gradientSlope = 0.0;
            double gradientOffset = 0.0;
            double hessianAa = 0.0;
            double hessianAb = 0.0;
            double hessianBb = 0.0;

            for (int index = 0; index < logits.Length; index++)
            {
                double predicted = Sigmoid(slope * logits[index] + offset);
                double residual = predicted - labels[index];
                double weight = predicted * (1 - predicted);

                gradientSlope += residual * logits[index];
                gradientOffset += residual;
                hessianAa += weight * logits[index] * logits[index];
                hessianAb += weight * logits[index];
                hessianBb += weight;
            }

            double determinant = hessianAa * hessianBb - hessianAb * hessianAb;

            if (Math.Abs(determinant) < SingularLimit)
            {
                throw new InvalidOperationException(
                    "Platt fit: Hessian tekil, cozum bulunamadi. Kalibrasyon kumesi yetersiz olabilir.");
            }

            double stepSlope = (hessianBb * gradientSlope - hessianAb * gradientOffset) / determinant;
            double stepOffset = (hessianAa * gradientOffset - hessianAb * gradientSlope) / determinant;

            // Sonumlu Newton: tam adim olabilirligi kotulestirirse adim yariya boluniyor.
            // Sonumleme olmadan ilk adim asiri buyuk olabiliyor ve butun tahminler
            // doyuma gidince Hessian tekillesiyor; bu bir testte gorulmustu.
            double current = NegativeLogLikelihood(logits, labels, slope, offset);
            double scale = 1.0;
            int halvings = 0;

            while (halvings < MaximumHalvings
                && NegativeLogLikelihood(logits, labels, slope - (scale * stepSlope), offset - (scale * stepOffset)) > current)
            {
                scale /= 2;
                halvings++;
            }

            slope -= scale * stepSlope;
            offset -= scale * stepOffset;

            if (Math.Max(Math.Abs(scale * stepSlope), Math.Abs(scale * stepOffset)) < Tolerance)
            {
                return slope > 0
                    ? new PlattCalibration(slope, offset, iteration)
                    : throw new InvalidOperationException(
                        $"Platt fit: egim pozitif degil ({slope.ToString(System.Globalization.CultureInfo.InvariantCulture)}). "
                        + "Donusum monoton artan olmali; sonuc kullanilmadi.");
            }
        }

        throw new InvalidOperationException(
            $"Platt fit: {MaximumIterations} yinelemede yakinsamadi. Sonuc kullanilmadi.");
    }

    private static double NegativeLogLikelihood(double[] logits, double[] labels, double slope, double offset)
    {
        double total = 0.0;

        for (int index = 0; index < logits.Length; index++)
        {
            double value = (slope * logits[index]) + offset;

            // log(1 + e^v) sayisal olarak guvenli bicimde.
            double softplus = value > 0 ? value + Math.Log(1 + Math.Exp(-value)) : Math.Log(1 + Math.Exp(value));
            total += softplus - (labels[index] * value);
        }

        return total;
    }
}
