namespace Sievert.Data.Entities;

/// <summary>
/// Tek bir commit. Burada duran her sey git'ten OKUNAN ham veri; hesaplanmis hicbir sey
/// yok. Turetilen olculer ayri tabloda (<see cref="CommitMetricRow"/>), gerekcesi ADR 0012'de.
/// </summary>
public sealed class CommitRow
{
    public int Id { get; set; }

    public int RepositoryId { get; set; }

    public RepositoryRow? Repository { get; set; }

    public required string Sha { get; set; }

    /// <summary>Yazarin adi. Kimlik degil, bilgi; kimlik eposta (ADR 0011).</summary>
    public required string AuthorName { get; set; }

    /// <summary>Yazarin epostasi, kucuk harfe cevrilmis. Kisi kimligi bu.</summary>
    public required string AuthorEmail { get; set; }

    /// <summary>Yazar tarihi, UTC. Committer tarihi kullanilmiyor (ADR 0011).</summary>
    public DateTimeOffset AuthorDateUtc { get; set; }

    public required string MessageSubject { get; set; }

    public required string MessageFull { get; set; }

    public int ParentCount { get; set; }

    /// <summary>Yazar bot gorunuyor mu. Commit yine de kayitli, sadece isaretli.</summary>
    public bool IsBot { get; set; }

    /// <summary>
    /// Mesajdan ayristirilan <c>Co-Authored-By</c> satiri sayisi. Adlarin ve epostalarin
    /// kendisi burada tutulmuyor; gerekcesi ADR 0012'de.
    /// </summary>
    public int CoAuthorCount { get; set; }

    public int LinesAdded { get; set; }

    public int LinesDeleted { get; set; }

    public int ChangedFiles { get; set; }

    public int ChangedCSharpFiles { get; set; }

    public List<CommitFileRow> Files { get; } = [];
}
