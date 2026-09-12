namespace Sievert.Modeling;

/// <summary>Bir satirin skoru ve gercek etiketi.</summary>
public readonly record struct ScoredRow(double Score, bool Actual);

/// <summary>
/// PR egrisinin altindaki alan. Hesap <c>docs/olcumler/asama5-metrik-sozlesmesi.md</c>
/// surum 1.0'da tanimli ve elle hesaplanmis bir ornekle sinaniyor.
///
/// Hicbir kutuphanenin varsayilanina baglanmiyor: "average precision" ve yamuk toplami
/// ayni egri icin farkli sayilar uretiyor, hangisinin kullanildigi yazilmazsa sonuclar
/// karsilastirilamaz.
/// </summary>
public static class PrecisionRecall
{
    /// <summary>
    /// Esit skorlar TEK esik grubu olarak isleniyor; bu yuzden sonuc esit skorlu
    /// satirlarin kendi aralarindaki sirasindan bagimsiz. Kumede hic gercek pozitif
    /// yoksa recall tanimsiz, alan da N/A.
    /// </summary>
    public static double? Area(IEnumerable<ScoredRow> rows)
    {
        List<ScoredRow> ordered = [.. rows];
        ordered.Sort((left, right) => right.Score.CompareTo(left.Score));

        int positives = 0;

        foreach (ScoredRow row in ordered)
        {
            if (row.Actual)
            {
                positives++;
            }
        }

        if (positives == 0)
        {
            return null;
        }

        double area = 0.0;
        double previousRecall = 0.0;
        double previousPrecision = double.NaN;
        int truePositives = 0;
        int falsePositives = 0;
        int index = 0;

        while (index < ordered.Count)
        {
            double score = ordered[index].Score;

            // Ayni skorlu butun satirlar birlikte isleniyor; grup bolunmuyor.
            while (index < ordered.Count && ordered[index].Score.Equals(score))
            {
                if (ordered[index].Actual)
                {
                    truePositives++;
                }
                else
                {
                    falsePositives++;
                }

                index++;
            }

            double recall = (double)truePositives / positives;
            double precision = (double)truePositives / (truePositives + falsePositives);

            // Egrinin basina (0, ilk grubun precision'i) noktasi ekleniyor: ilk
            // precision sola dogru sabit uzatiliyor.
            if (double.IsNaN(previousPrecision))
            {
                previousPrecision = precision;
            }

            area += (recall - previousRecall) * (precision + previousPrecision) / 2;

            previousRecall = recall;
            previousPrecision = precision;
        }

        return area;
    }
}
