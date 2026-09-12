namespace Sievert.Data.Metrics;

/// <summary>
/// Metrik hesabinin commit sirasi. Tek yerde yaziyor ki ayni kural iki ayri kodda metin
/// olarak kopyalanmasin.
///
/// **Kural:** once <c>AuthorDateUtc</c> artan, esitlikte **madencilik sirasi** artan.
///
/// Madencilik sirasi, commit'lerin git'ten okunma sirasi
/// (<c>CommitSortStrategies.Time</c>) ve veritabanina o sirayla yazildiklari icin
/// <c>Commits.Id</c> ile ayni. <see cref="MetricsRunner.Read"/> bu yuzden
/// <c>OrderBy(AuthorDateUtc).ThenBy(Id)</c> diyor; ayni sirayi veritabanina gitmeden
/// kurmak isteyen bir kod, commit'leri ayni filtreyle okuyup okuma sirasini sayac olarak
/// kullanmali.
///
/// SHA siralamaya **girmiyor**. Ayni saniyede birden fazla commit olan gruplarda Id
/// sirasi ile SHA sirasi cogu zaman farkli (olculdu: Polly 69 grubun 40'inda,
/// Jellyfin 321 grubun 173'unde, ShareX 9 grubun 4'unde), yani SHA ile siralamak farkli
/// bir gecmis birikimi uretir.
/// </summary>
public static class CommitOrdering
{
    /// <summary>Kurali uygular: tarih artan, esitlikte madencilik sirasi artan.</summary>
    public static List<T> Apply<T>(
        IEnumerable<T> items,
        Func<T, DateTimeOffset> date,
        Func<T, int> miningSequence)
    {
        List<T> ordered = [.. items];

        ordered.Sort((left, right) =>
        {
            int compared = date(left).UtcDateTime.CompareTo(date(right).UtcDateTime);

            return compared != 0 ? compared : miningSequence(left).CompareTo(miningSequence(right));
        });

        return ordered;
    }
}
