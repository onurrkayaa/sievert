namespace Sievert.Data.Entities;

/// <summary>
/// Bir commit icin TURETILMIS olculer. Bu adimda tablo bilerek bos: sadece hangi commit'e
/// ait oldugu duruyor, olculeri Adim 3 dolduracak. Ham veriden ayri durmasinin sebebi
/// ADR 0012'de: turetilmis olculerin tanimi degisince bu tablo silinip yeniden
/// hesaplanabilmeli, ham veri ise git'ten bir kez okunup oldugu gibi kalmali.
/// </summary>
public sealed class CommitMetricRow
{
    public int Id { get; set; }

    public int CommitId { get; set; }

    public CommitRow? Commit { get; set; }
}
