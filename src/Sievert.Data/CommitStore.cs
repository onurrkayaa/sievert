using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

using Sievert.Core.Mining;
using Sievert.Data.Entities;

namespace Sievert.Data;

/// <summary>Yazma isinin ayarlari.</summary>
/// <param name="Identity">Deponun kimligi; ayni kimlik ikinci kez gelirse ayni satir guncelleniyor.</param>
/// <param name="IdentitySource">Kimlik nereden geldi: <c>remote</c> ya da <c>folder</c>.</param>
/// <param name="RepositoryName">Depo adi, gosterim icin.</param>
/// <param name="RemoteUrl">origin adresi, yoksa null.</param>
/// <param name="HeadSha">Tarama sirasinda HEAD'in gosterdigi commit.</param>
/// <param name="Rewrite">true ise deponun mevcut commit'leri silinip bastan yaziliyor.</param>
public sealed record StoreOptions(
    string Identity,
    string IdentitySource,
    string RepositoryName,
    string? RemoteUrl,
    string? HeadSha,
    bool Rewrite);

/// <summary>Yazma isinin sonucu.</summary>
/// <param name="RepositoryId">Yazilan deponun satir kimligi.</param>
/// <param name="Written">Bu kosuda eklenen commit sayisi.</param>
/// <param name="Skipped">Zaten kayitli oldugu icin atlanan commit sayisi.</param>
/// <param name="Deleted">--yeniden-yaz ile silinen eski commit sayisi.</param>
public sealed record StoreResult(int RepositoryId, int Written, int Skipped, int Deleted);

/// <summary>
/// Commit akisini veritabanina yazar. Akis korunuyor: gelen commit'ler bir listede
/// toplanmiyor, <see cref="BatchSize"/> kadari birikince yazilip degisiklik izleyici
/// temizleniyor. Boylece bellekte duran commit sayisi tarihin uzunlugundan bagimsiz.
/// </summary>
public sealed class CommitStore(SievertContext context)
{
    /// <summary>
    /// Kac commit birikince SaveChanges cagriliyor. Commit basina tek tek INSERT atmak
    /// her commit icin ayri bir gidis donus demek; hepsini biriktirmek ise Adim 1'de
    /// kacinilan belleğe toplama hatasinin aynisi. 500 ikisinin arasi.
    /// </summary>
    public const int BatchSize = 500;

    public StoreResult Write(IEnumerable<CommitRecord> commits, StoreOptions options)
    {
        // Tek islem: yarida kesilirse hicbir satir kalmiyor. Yarim yazilmis bir depo,
        // "bu commit zaten var" diyen idempotent yazmayi da kalici olarak yaniltirdi.
        using IDbContextTransaction transaction = context.Database.BeginTransaction();

        RepositoryRow repository = FindOrCreate(options);
        int deleted = options.Rewrite ? DeleteExistingCommits(repository.Id) : 0;

        HashSet<string> existing = options.Rewrite
            ? []
            : [.. context.Commits.Where(row => row.RepositoryId == repository.Id).Select(row => row.Sha)];

        int written = 0;
        int skipped = 0;
        int pending = 0;
        DateTimeOffset? first = null;
        DateTimeOffset? last = null;

        foreach (CommitRecord commit in commits)
        {
            Track(commit.AuthorDateUtc, ref first, ref last);

            if (!existing.Add(commit.Sha))
            {
                skipped++;
                continue;
            }

            context.Commits.Add(ToRow(commit, repository.Id));
            written++;
            pending++;

            if (pending < BatchSize)
            {
                continue;
            }

            context.SaveChanges();

            // Izleyiciyi temizlemezsek yazilan her commit bellekte kalir ve akis
            // ozelligi bir ise yaramaz.
            context.ChangeTracker.Clear();
            pending = 0;
        }

        if (pending > 0)
        {
            context.SaveChanges();
            context.ChangeTracker.Clear();
        }

        UpdateRepository(repository.Id, options, first, last);
        transaction.Commit();

        return new StoreResult(repository.Id, written, skipped, deleted);
    }

    private RepositoryRow FindOrCreate(StoreOptions options)
    {
        RepositoryRow? found = context.Repositories
            .FirstOrDefault(row => row.Identity == options.Identity);

        if (found is not null)
        {
            return found;
        }

        RepositoryRow created = new()
        {
            Identity = options.Identity,
            IdentitySource = options.IdentitySource,
            Name = options.RepositoryName,
            RemoteUrl = options.RemoteUrl,
            ScannedAt = DateTimeOffset.UtcNow,
            ScannedSha = options.HeadSha,
        };

        context.Repositories.Add(created);
        context.SaveChanges();

        return created;
    }

    /// <summary>Dosya satirlari basamakli silme ile gidiyor, ayrica silmeye gerek yok.</summary>
    private int DeleteExistingCommits(int repositoryId) =>
        context.Commits.Where(row => row.RepositoryId == repositoryId).ExecuteDelete();

    /// <summary>
    /// Ozet alanlari yazma bittikten sonra guncelleniyor. Sayimi veritabanina sorduruyorum,
    /// bu kosuda yazilani saymiyorum: depo daha once kismen yazilmis olabilir.
    /// </summary>
    private void UpdateRepository(
        int repositoryId,
        StoreOptions options,
        DateTimeOffset? first,
        DateTimeOffset? last)
    {
        RepositoryRow repository = context.Repositories.Single(row => row.Id == repositoryId);

        repository.TotalCommits = context.Commits.Count(row => row.RepositoryId == repositoryId);
        repository.Name = options.RepositoryName;
        repository.IdentitySource = options.IdentitySource;
        repository.RemoteUrl = options.RemoteUrl;
        repository.ScannedAt = DateTimeOffset.UtcNow;
        repository.ScannedSha = options.HeadSha;

        if (first is not null && (repository.FirstCommitDate is null || first < repository.FirstCommitDate))
        {
            repository.FirstCommitDate = first;
        }

        if (last is not null && (repository.LastCommitDate is null || last > repository.LastCommitDate))
        {
            repository.LastCommitDate = last;
        }

        context.SaveChanges();
    }

    private static void Track(DateTimeOffset date, ref DateTimeOffset? first, ref DateTimeOffset? last)
    {
        if (first is null || date < first)
        {
            first = date;
        }

        if (last is null || date > last)
        {
            last = date;
        }
    }

    private static CommitRow ToRow(CommitRecord commit, int repositoryId)
    {
        CommitRow row = new()
        {
            RepositoryId = repositoryId,
            Sha = commit.Sha,
            AuthorName = commit.AuthorName,
            AuthorEmail = commit.AuthorEmail,
            AuthorDateUtc = commit.AuthorDateUtc,
            MessageSubject = commit.MessageSubject,
            MessageFull = commit.Message,
            ParentCount = commit.ParentCount,
            IsBot = commit.AuthorLooksLikeBot,
            CoAuthorCount = commit.CoAuthors.Count,
            LinesAdded = commit.Summary.LinesAdded,
            LinesDeleted = commit.Summary.LinesDeleted,
            ChangedFiles = commit.Summary.ChangedFileCount,
            ChangedCSharpFiles = commit.Summary.ChangedCSharpFileCount,
        };

        foreach (FileChange file in commit.Files)
        {
            row.Files.Add(new CommitFileRow
            {
                Path = file.Path,
                OldPath = file.OldPath,
                LinesAdded = file.LinesAdded,
                LinesDeleted = file.LinesDeleted,
                ChangeKind = file.Kind.ToString().ToLowerInvariant(),
                IsCSharp = file.Path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase),
            });
        }

        return row;
    }
}
