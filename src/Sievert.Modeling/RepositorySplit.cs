namespace Sievert.Modeling;

/// <summary>
/// Bir deponun egitim ve test bolumleri. Bolme dondurulmus manifestten geliyor
/// (<c>data/asama5/split-manifest.csv</c>), burada yeniden hesaplanmiyor.
///
/// Depo kimligi modele oznitelik olarak girmiyor; yalnizca ayri esik secmek, sonuclari
/// gruplamak ve mikro/makro toplama yapmak icin kullaniliyor (ADR 0016).
/// </summary>
public sealed record RepositorySplit(
    string Identity,
    IReadOnlyList<SnapshotRow> Train,
    IReadOnlyList<SnapshotRow> Test)
{
    /// <summary>
    /// Rastgele tabanin tahmin olasiligi: YALNIZCA egitim bolumunden. Test etiketlerine
    /// bakarak bir oran uretmek, tabanin cevabi bilmesi olurdu.
    /// </summary>
    public double TrainPositiveRate => Train.Count == 0 ? 0.0 : (double)TrainPositives / Train.Count;

    public int TrainPositives => Count(Train);

    public int TestPositives => Count(Test);

    private static int Count(IReadOnlyList<SnapshotRow> rows)
    {
        int positives = 0;

        foreach (SnapshotRow row in rows)
        {
            if (row.IsBugIntroducing)
            {
                positives++;
            }
        }

        return positives;
    }
}
