namespace Sievert.Data.Entities;

/// <summary>Bir commit'in tek bir dosyaya yaptigi degisiklik.</summary>
public sealed class CommitFileRow
{
    public int Id { get; set; }

    public int CommitId { get; set; }

    public CommitRow? Commit { get; set; }

    /// <summary>Repo kokune gore goreli yol. Mutlak yol veritabanina girmiyor.</summary>
    public required string Path { get; set; }

    /// <summary>Ad degisiminde eski yol, digerlerinde null.</summary>
    public string? OldPath { get; set; }

    public int LinesAdded { get; set; }

    public int LinesDeleted { get; set; }

    /// <summary>added / deleted / modified / renamed.</summary>
    public required string ChangeKind { get; set; }

    /// <summary>Uzantisi <c>.cs</c> mi. Sorguda her seferinde LIKE yazmamak icin sutun.</summary>
    public bool IsCSharp { get; set; }
}
