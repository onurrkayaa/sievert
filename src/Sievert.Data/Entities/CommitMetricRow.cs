namespace Sievert.Data.Entities;

/// <summary>
/// Bir commit icin TURETILMIS olculer. Ham veriden (Commits, CommitFiles) hesaplaniyor,
/// git'e tekrar gidilmiyor. Ayri tabloda durmasinin sebebi ADR 0012'de: bir olcunun
/// tanimi degisince bu tablo silinip yeniden hesaplanabilmeli, ham veri ise git'ten bir
/// kez okunup oldugu gibi kalmali.
///
/// Butun alanlar SADECE o commit'ten onceki veriyle hesaplaniyor; gerekcesi ADR 0013'teki
/// zaman sizintisi bolumunde.
/// </summary>
public sealed class CommitMetricRow
{
    public int Id { get; set; }

    public int CommitId { get; set; }

    public CommitRow? Commit { get; set; }

    // Boyut. Bunlar Commits tablosundakilerin kopyasi; olcu tablosu tek basina
    // sorgulanabilsin diye burada da duruyorlar.

    public int LinesAdded { get; set; }

    public int LinesDeleted { get; set; }

    public int FilesChanged { get; set; }

    public int CsFilesChanged { get; set; }

    // Daginiklik: degisiklik tek yerde mi toplanmis, yoksa repoya yayilmis mi.

    /// <summary>Shannon entropisi. Tek dosyalik commit'te 0.</summary>
    public double Entropy { get; set; }

    /// <summary>Dokunulan farkli dizin sayisi (yol eksi dosya adi).</summary>
    public int DirectoryCount { get; set; }

    /// <summary>Dokunulan farkli alt sistem sayisi (yolun ilk bileseni).</summary>
    public int SubsystemCount { get; set; }

    // Gecmis: dokunulan dosyalarin bu commit'ten ONCEKI hâli.

    public int MaxFileAgeDays { get; set; }

    public int MinFileAgeDays { get; set; }

    /// <summary>Dokunulan dosyalarin bu commit'ten once kac kez degistigi, toplam.</summary>
    public int PriorChanges { get; set; }

    /// <summary>Ayni sayim, sadece duzeltme commit'leri.</summary>
    public int PriorFixes { get; set; }

    /// <summary>Bu dosyalara daha once dokunmus farkli yazar sayisi (epostaya gore).</summary>
    public int DistinctAuthorsOnFiles { get; set; }

    // Gelistirici.

    /// <summary>Yazarin bu commit'ten onceki commit sayisi.</summary>
    public int AuthorCommitCount { get; set; }

    /// <summary>Yazarin bu dosyalara daha once kac kez dokundugu, toplam.</summary>
    public int AuthorFileExperience { get; set; }

    // Amac.

    /// <summary>Mesaj basligi duzeltme imasi tasiyor mu. Heuristik, kusurlari ADR 0013'te.</summary>
    public bool IsFix { get; set; }
}
