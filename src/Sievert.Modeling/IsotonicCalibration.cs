namespace Sievert.Modeling;

/// <summary>Isotonic haritasindaki tek bir blok.</summary>
/// <param name="Lower">Blogun kapsadigi en kucuk ham olasilik.</param>
/// <param name="Upper">Blogun kapsadigi en buyuk ham olasilik.</param>
/// <param name="Value">Blogun kalibre degeri.</param>
/// <param name="Weight">Blokta toplanan calibration satiri sayisi.</param>
public sealed record IsotonicBlock(double Lower, double Upper, double Value, int Weight);

/// <summary>
/// Isotonic regression, Pool Adjacent Violators ile. Tanim
/// <c>docs/olcumler/asama5-kalibrasyon-sozlesmesi.md</c> surum 1.0'da.
///
/// Platt'in aksine bir bicim varsaymiyor; yalnizca ciktinin azalmamasini sarta bagliyor.
/// Bunun bedeli calibration kumesi kucukken az sayida genis blok uretmesi, yani basamakli
/// ve veriye duyarli bir harita.
/// </summary>
public sealed record IsotonicCalibration(IReadOnlyList<IsotonicBlock> Blocks)
{
    /// <summary>
    /// Calibration araliginin altindaki skora ilk blogun, ustundekine son blogun degeri
    /// veriliyor. Arada kalan skor, sinirlari kendisini iceren bloga dusuyor.
    /// </summary>
    public double Apply(double probability)
    {
        if (probability <= Blocks[0].Upper)
        {
            return Blocks[0].Value;
        }

        for (int index = 1; index < Blocks.Count; index++)
        {
            if (probability <= Blocks[index].Upper)
            {
                return Blocks[index].Value;
            }
        }

        return Blocks[^1].Value;
    }

    public static IsotonicCalibration Fit(IReadOnlyList<ScoredProbability> calibration)
    {
        if (calibration.Count == 0)
        {
            throw new InvalidOperationException("Isotonic fit: kalibrasyon kumesi bos.");
        }

        List<ScoredProbability> ordered = [.. calibration];
        ordered.Sort((left, right) => left.Probability.CompareTo(right.Probability));

        // Esit skorlar tek grup: boylece sonuc esit skorlu satirlarin kendi aralarindaki
        // sirasindan bagimsiz.
        List<IsotonicBlock> blocks = [];
        int index2 = 0;

        while (index2 < ordered.Count)
        {
            double probability = ordered[index2].Probability;
            int positives = 0;
            int count = 0;

            while (index2 < ordered.Count && ordered[index2].Probability.Equals(probability))
            {
                if (ordered[index2].Actual)
                {
                    positives++;
                }

                count++;
                index2++;
            }

            blocks.Add(new IsotonicBlock(probability, probability, (double)positives / count, count));
        }

        // Pool Adjacent Violators: soldaki blok sagdakinden buyukse ikisi birlestirilip
        // agirlikli ortalama aliniyor, ihlal kalmayana kadar.
        List<IsotonicBlock> pooled = [];

        foreach (IsotonicBlock block in blocks)
        {
            IsotonicBlock current = block;

            while (pooled.Count > 0 && pooled[^1].Value > current.Value)
            {
                IsotonicBlock previous = pooled[^1];
                pooled.RemoveAt(pooled.Count - 1);

                int weight = previous.Weight + current.Weight;
                double value = ((previous.Value * previous.Weight) + (current.Value * current.Weight)) / weight;

                current = new IsotonicBlock(previous.Lower, current.Upper, value, weight);
            }

            pooled.Add(current);
        }

        return new IsotonicCalibration(pooled);
    }
}
