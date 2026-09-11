namespace Sievert.Data.Metrics;

/// <summary>Metrik hesabi icin bir dosya degisikligi. Veritabanindan okunan hâli.</summary>
/// <param name="Path">Repo kokune gore goreli yol.</param>
/// <param name="OldPath">Ad degisiminde eski yol.</param>
/// <param name="LinesAdded">Eklenen satir.</param>
/// <param name="LinesDeleted">Silinen satir.</param>
/// <param name="IsRename">Degisim turu ad degisimi mi.</param>
/// <param name="IsCSharp">Uzantisi .cs mi.</param>
public sealed record FileForMetrics(
    string Path,
    string? OldPath,
    int LinesAdded,
    int LinesDeleted,
    bool IsRename,
    bool IsCSharp);

/// <summary>
/// Metrik hesabi icin bir commit. Git'ten degil veritabanindan geliyor; hesaplama
/// asamasinda git'e hic gidilmiyor (ADR 0013).
/// </summary>
/// <param name="Id">Commits tablosundaki satir kimligi.</param>
/// <param name="AuthorEmail">Yazar kimligi (ADR 0011).</param>
/// <param name="Date">Yazar tarihi, UTC.</param>
/// <param name="Subject">Mesajin ilk satiri; IsFix buradan bakiliyor.</param>
/// <param name="Files">Degisen dosyalar.</param>
public sealed record CommitForMetrics(
    int Id,
    string AuthorEmail,
    DateTimeOffset Date,
    string Subject,
    IReadOnlyList<FileForMetrics> Files);

/// <summary>
/// Tek bir commit icin hesaplanan olculer. Hepsi SADECE o commit'ten onceki veriyle
/// hesaplaniyor; gerekcesi ADR 0013'teki zaman sizintisi bolumunde.
/// </summary>
public sealed record CommitMetrics(
    int CommitId,
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
    bool IsFix);
