namespace Sievert.Modeling;

/// <summary>
/// Her seye negatif diyen taban. Hicbir sey ogrenmiyor, hicbir alarm uretmiyor.
///
/// Olculmesinin sebebi ADR 0017'de: bu taban accuracy'de yuksek bir sayi aliyor ama
/// bulunan hic hata yok. Sonraki her yontem once bunun uzerine cikmak zorunda.
/// </summary>
public static class NegativeBaseline
{
    /// <summary>Skor sabit: siralama bilgisi tasimiyor, PR-AUC'si taban orani cikar.</summary>
    public const double Score = 0.0;

    public static IReadOnlyList<Scored> Apply(IReadOnlyList<SnapshotRow> test)
    {
        List<Scored> scored = new(test.Count);

        foreach (SnapshotRow row in test)
        {
            scored.Add(new Scored(false, Score, row.IsBugIntroducing));
        }

        return scored;
    }
}
