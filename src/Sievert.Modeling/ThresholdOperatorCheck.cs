namespace Sievert.Modeling;

/// <summary>Bir depoda iki operator yazimi arasindaki fark.</summary>
/// <param name="GreaterOrEqual">Uygulanan esik: <c>LinesAdded &gt;= esik</c>.</param>
/// <param name="Greater">
/// Ayni aileyi <c>&gt;</c> ile ifade eden esik: egitimde gorulen, esikten kucuk en buyuk
/// deger. Esik egitimin en kucuk degeriyse boyle bir deger yok ve null doner.
/// </param>
public sealed record OperatorCheck(
    double GreaterOrEqual,
    double? Greater,
    int TrainRows,
    int TrainDifferences,
    int TestRows,
    int TestDifferences);

/// <summary>
/// Beklenti dosyasi kurali <c>LinesAdded &gt; esik</c> diye yazmisti, Adim 2'de ilan
/// edilip uygulanan protokol ise <c>LinesAdded &gt;= esik</c>. Ana sonuc degismiyor; bu
/// sinif yalnizca iki yazimin ayni tahminleri uretip uretmedigini gercek veride sayiyor.
///
/// Hipotez: aday esikler egitimde gorulen benzersiz degerlerden geldigi icin iki yazim
/// ayni monoton siniflandirma ailesini temsil eder. Bu bir sonuc degil, kontrol edilecek
/// bir iddia; test bolumunde egitimde gorulmemis bir deger iki esigin arasina duserse
/// fark cikar.
/// </summary>
public static class ThresholdOperatorCheck
{
    /// <summary>Egitimde gorulen, esikten kucuk en buyuk deger.</summary>
    public static double? EquivalentGreaterThreshold(IReadOnlyList<SnapshotRow> train, double threshold)
    {
        double? below = null;

        foreach (SnapshotRow row in train)
        {
            double value = ModelFeatures.Value(row, LinesAddedBaseline.Feature);

            if (value < threshold && (below is not double current || value > current))
            {
                below = value;
            }
        }

        return below;
    }

    /// <summary>Iki yazimin farkli tahmin ettigi satir sayisi.</summary>
    public static int Differences(IReadOnlyList<SnapshotRow> rows, double greaterOrEqual, double greater)
    {
        int differences = 0;

        foreach (SnapshotRow row in rows)
        {
            double value = ModelFeatures.Value(row, LinesAddedBaseline.Feature);

            if (value >= greaterOrEqual != value > greater)
            {
                differences++;
            }
        }

        return differences;
    }

    public static OperatorCheck Compare(RepositorySplit repository, double threshold)
    {
        double? greater = EquivalentGreaterThreshold(repository.Train, threshold);

        if (greater is not double value)
        {
            return new OperatorCheck(
                threshold,
                null,
                repository.Train.Count,
                repository.Train.Count,
                repository.Test.Count,
                repository.Test.Count);
        }

        return new OperatorCheck(
            threshold,
            value,
            repository.Train.Count,
            Differences(repository.Train, threshold, value),
            repository.Test.Count,
            Differences(repository.Test, threshold, value));
    }
}
