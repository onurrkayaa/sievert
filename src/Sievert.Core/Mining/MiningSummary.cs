namespace Sievert.Core.Mining;

/// <summary>
/// Bir madencilik kosusunun ozeti. Commit'ler tek tek akip gittigi icin bu ozet
/// yol boyunca biriktiriliyor, sonda listeden hesaplanmiyor (B031'in dersi).
/// </summary>
/// <param name="CommitCount">Ciktiya yazilan commit sayisi.</param>
/// <param name="SkippedMergeCount">Birlestirme oldugu icin atlanan commit sayisi.</param>
/// <param name="FirstAuthorDateUtc">En eski yazar tarihi. Hic commit yoksa null.</param>
/// <param name="LastAuthorDateUtc">En yeni yazar tarihi. Hic commit yoksa null.</param>
/// <param name="DistinctAuthorCount">Farkli eposta sayisi. Ad degil, eposta (ADR 0011).</param>
/// <param name="BotAuthorCommitCount">Yazari bot gorunen commit sayisi.</param>
/// <param name="CoAuthorLineCount">Ayristirilan <c>Co-Authored-By</c> satiri toplami.</param>
/// <param name="RenameSimilarityThreshold">Ad degisimi icin kullanilan benzerlik esigi (yuzde).</param>
public sealed record MiningSummary(
    int CommitCount,
    int SkippedMergeCount,
    DateTimeOffset? FirstAuthorDateUtc,
    DateTimeOffset? LastAuthorDateUtc,
    int DistinctAuthorCount,
    int BotAuthorCommitCount,
    int CoAuthorLineCount,
    int RenameSimilarityThreshold);
