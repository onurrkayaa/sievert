namespace Sievert.Modeling;

/// <summary>
/// Dondurulmus veri kumesindeki tek bir satir. Alanlar
/// <c>data/asama5/commit-metrics.csv</c> basligiyla ayni sirada.
///
/// Buradaki her alan modele girmiyor: hangisinin ne oldugu
/// <see cref="ModelFeatures"/> icinde ve ADR 0016'da yaziyor.
/// </summary>
/// <param name="Repository">Deponun klasor adi. Makineye ozel olabilir, yalnizca gosterim.</param>
/// <param name="RepositoryIdentity">Uzak adresten normalize edilmis kimlik. Gruplama bundan.</param>
/// <param name="Sha">Commit'in tam sha'si. Kimlik alani.</param>
/// <param name="AuthorDateUtc">Yazar tarihi, UTC. Bolme icin; oznitelik degil.</param>
/// <param name="IsFix">Mesaj basligi duzeltme imasi tasiyor mu.</param>
/// <param name="IsBugIntroducing">Hedef degisken.</param>
/// <param name="LabelSource">Etiketin kaynagi. Denetim alani; etiketsizde null.</param>
/// <param name="IsBot">CSV'de <c>BotMu</c> basligiyla duruyor. Bu adimda modele girmiyor.</param>
public sealed record SnapshotRow(
    string Repository,
    string RepositoryIdentity,
    string Sha,
    DateTimeOffset AuthorDateUtc,
    int LinesAdded,
    int LinesDeleted,
    int FilesChanged,
    int CsFilesChanged,
    double Entropy,
    int DirectoryCount,
    int SubsystemCount,
    int MaxFileAgeDays,
    int MinFileAgeDays,
    int PriorChanges,
    int PriorFixes,
    int DistinctAuthorsOnFiles,
    int AuthorCommitCount,
    int AuthorFileExperience,
    bool IsFix,
    bool IsBugIntroducing,
    string? LabelSource,
    bool IsBot);
