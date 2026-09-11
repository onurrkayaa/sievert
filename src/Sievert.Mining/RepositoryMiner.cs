using LibGit2Sharp;

using Sievert.Core.Mining;

namespace Sievert.Mining;

/// <summary>
/// Git tarihini yuruyup commit basina veri cikarir. Commit'ler tek tek veriliyor
/// (<c>yield return</c>); cagiran bir commit'i isleyip biraktiginda o commit bellekte
/// kalmiyor. Asama 3'te tarama tarafinda butun dosyalari ayni anda bellekte tutmak
/// tepe bellegi %72 buyutmustu, burada o kalibi bastan kurmuyorum.
/// </summary>
public sealed class RepositoryMiner
{
    /// <summary>
    /// Ad degisimi icin benzerlik esigi, yuzde. LibGit2Sharp'in varsayilani da bu.
    /// Acikca yaziyorum ki raporda hangi esigin kullanildigi belli olsun.
    /// </summary>
    public const int RenameSimilarityThreshold = 50;

    private static readonly CompareOptions Compare = new()
    {
        Similarity = new SimilarityOptions
        {
            RenameDetectionMode = RenameDetectionMode.Renames,
            RenameThreshold = RenameSimilarityThreshold,
        },
    };

    /// <summary>
    /// Atlanan birlestirme commit'i sayisi. Ancak numaralandirma bittikten sonra dogru;
    /// akis tembel oldugu icin sayac yol boyunca artiyor.
    /// </summary>
    public int SkippedMergeCount { get; private set; }

    /// <summary>
    /// Verilen yol bir git deposu mu (ya da bir deponun icinde mi). Kullaniciya duzgun
    /// bir mesaj verebilmek icin, LibGit2Sharp'in istisnasini beklemek yerine once soruyoruz.
    /// </summary>
    public static bool IsRepository(string path) => Repository.Discover(path) is not null;

    /// <summary>
    /// Depo shallow mi, yani tarihi kesilmis mi (<c>git clone --depth N</c>). Boyleyse
    /// okunan tarih deponun tamami degil ve ozetteki "ilk commit" gercek ilk commit degil.
    /// Cagiranin bunu kullaniciya soylemesi gerekiyor; burada karar verilmiyor, sadece
    /// soruluyor.
    /// </summary>
    public static bool IsShallow(string path)
    {
        using Repository repository = new(path);

        return repository.Info.IsShallow;
    }

    /// <summary>
    /// Tarihi en yeniden en eskiye dogru yurur. Birlestirme commit'leri (ebeveyn sayisi
    /// birden buyuk) ciktiya girmez ama <see cref="SkippedMergeCount"/> icinde sayilir;
    /// gerekcesi ADR 0011'de.
    /// </summary>
    public IEnumerable<CommitRecord> Read(string repositoryPath, MiningOptions options)
    {
        SkippedMergeCount = 0;

        using Repository repository = new(repositoryPath);

        CommitFilter filter = new() { SortBy = CommitSortStrategies.Time };
        int written = 0;

        foreach (Commit commit in repository.Commits.QueryBy(filter))
        {
            DateTimeOffset authored = commit.Author.When.ToUniversalTime();

            if (options.Since is DateTimeOffset since && authored < since)
            {
                continue;
            }

            if (commit.Parents.Count() > 1)
            {
                SkippedMergeCount++;
                continue;
            }

            yield return Describe(repository, commit, authored);

            written++;

            if (options.MaxCommits is int max && written >= max)
            {
                yield break;
            }
        }
    }

    private static CommitRecord Describe(Repository repository, Commit commit, DateTimeOffset authored)
    {
        Tree? parent = commit.Parents.FirstOrDefault()?.Tree;
        IReadOnlyList<FileChange> files = ReadChanges(repository, parent, commit.Tree);
        string message = commit.Message ?? string.Empty;

        return new CommitRecord(
            commit.Sha,
            commit.Sha[..7],
            commit.Author.Name,
            commit.Author.Email.ToLowerInvariant(),
            authored,
            Subject(message),
            message,
            commit.Parents.Count(),
            BotAuthor.Looks(commit.Author.Name, commit.Author.Email),
            CoAuthorParser.Parse(message),
            files,
            Summarize(files));
    }

    /// <summary>
    /// Ilk commit'in ebeveyni yok; o durumda bos agacla karsilastiriliyor ve butun
    /// dosyalar "eklendi" cikiyor.
    /// </summary>
    private static IReadOnlyList<FileChange> ReadChanges(Repository repository, Tree? parent, Tree current)
    {
        using Patch patch = repository.Diff.Compare<Patch>(parent, current, Compare);

        return patch
            .Select(change => new FileChange(
                change.Path,
                change.Status == ChangeKind.Renamed ? change.OldPath : null,
                change.LinesAdded,
                change.LinesDeleted,
                KindOf(change.Status)))
            .ToList();
    }

    private static FileChangeKind KindOf(ChangeKind status) => status switch
    {
        ChangeKind.Added => FileChangeKind.Added,
        ChangeKind.Deleted => FileChangeKind.Deleted,
        ChangeKind.Renamed => FileChangeKind.Renamed,
        _ => FileChangeKind.Modified,
    };

    private static CommitChangeSummary Summarize(IReadOnlyList<FileChange> files) =>
        new(
            files.Sum(file => file.LinesAdded),
            files.Sum(file => file.LinesDeleted),
            files.Count,
            files.Count(file => file.Path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)));

    /// <summary>Mesajin ilk satiri. Bos mesajda bos string doner.</summary>
    private static string Subject(string message)
    {
        int end = message.IndexOf('\n');

        return (end < 0 ? message : message[..end]).TrimEnd('\r').Trim();
    }
}
