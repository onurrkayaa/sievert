namespace Sievert.Core.Mining;

/// <summary>Bir dosyanin commit icindeki degisim turu.</summary>
public enum FileChangeKind
{
    /// <summary>Dosya bu commit'te eklendi.</summary>
    Added,

    /// <summary>Dosya bu commit'te silindi.</summary>
    Deleted,

    /// <summary>Dosyanin icerigi degisti.</summary>
    Modified,

    /// <summary>Dosyanin adi ya da yolu degisti; eski yol <c>OldPath</c> alaninda.</summary>
    Renamed,
}

/// <summary>Commit'in bir dosyaya yaptigi degisiklik.</summary>
/// <param name="Path">Repo kokune gore goreli yol. Mutlak yol hicbir zaman yazilmaz.</param>
/// <param name="OldPath">Ad degisiminde eski yol, diger durumlarda null.</param>
/// <param name="LinesAdded">Eklenen satir sayisi.</param>
/// <param name="LinesDeleted">Silinen satir sayisi.</param>
/// <param name="Kind">Degisim turu.</param>
public sealed record FileChange(
    string Path,
    string? OldPath,
    int LinesAdded,
    int LinesDeleted,
    FileChangeKind Kind);

/// <summary>
/// Commit'in toplami. <c>.cs</c> dosyalari ayri sayiliyor cunku risk tahmini sadece C#
/// kodunu ilgilendiriyor; bir commit yuzlerce satir JSON degistirip tek satir kod
/// degistirmis olabilir.
/// </summary>
/// <param name="LinesAdded">Butun dosyalarda eklenen satir toplami.</param>
/// <param name="LinesDeleted">Butun dosyalarda silinen satir toplami.</param>
/// <param name="ChangedFileCount">Degisen dosya sayisi.</param>
/// <param name="ChangedCSharpFileCount">Bunlarin kacinin uzantisi <c>.cs</c>.</param>
public sealed record CommitChangeSummary(
    int LinesAdded,
    int LinesDeleted,
    int ChangedFileCount,
    int ChangedCSharpFileCount);

/// <summary>Mesaj govdesindeki <c>Co-Authored-By</c> satirindan cikarilan kisi.</summary>
/// <param name="Name">Satirdaki ad.</param>
/// <param name="Email">Koseli parantez icindeki eposta, kucuk harfe cevrilmis.</param>
public sealed record CoAuthor(string Name, string Email);

/// <summary>
/// Tek bir commit hakkinda git tarihinden cikarilan her sey. Icinde tek bir LibGit2Sharp
/// tipi yok; ADR 0004'teki "kutuphane tipleri katman sinirini gecmez" karariyla ayni cizgide.
/// </summary>
/// <param name="Sha">Tam commit hash'i.</param>
/// <param name="ShortSha">Ilk yedi karakter.</param>
/// <param name="AuthorName">Yazarin adi. Kimlik icin kullanilmiyor, bilgi olarak duruyor.</param>
/// <param name="AuthorEmail">Yazarin epostasi, kucuk harfe cevrilmis. Kimlik bu (ADR 0011).</param>
/// <param name="AuthorDateUtc">Yazar tarihi, UTC'ye cevrilmis. Committer tarihi kullanilmiyor.</param>
/// <param name="MessageSubject">Mesajin ilk satiri.</param>
/// <param name="Message">Mesajin tamami, hic dokunulmadan.</param>
/// <param name="ParentCount">Ebeveyn sayisi. Birlestirme commit'leri zaten ciktiya girmiyor.</param>
/// <param name="AuthorLooksLikeBot">Yazar bot gorunuyor mu. Bayrak; commit yine de ciktida.</param>
/// <param name="CoAuthors">Mesajdan ayristirilmis ortak yazarlar.</param>
/// <param name="Files">Degisen dosyalar.</param>
/// <param name="Summary">Commit'in toplami.</param>
public sealed record CommitRecord(
    string Sha,
    string ShortSha,
    string AuthorName,
    string AuthorEmail,
    DateTimeOffset AuthorDateUtc,
    string MessageSubject,
    string Message,
    int ParentCount,
    bool AuthorLooksLikeBot,
    IReadOnlyList<CoAuthor> CoAuthors,
    IReadOnlyList<FileChange> Files,
    CommitChangeSummary Summary);
