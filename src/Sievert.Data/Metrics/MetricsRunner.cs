using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

using Sievert.Data.Entities;

namespace Sievert.Data.Metrics;

/// <summary>Hesaplama sonucu.</summary>
/// <param name="RepositoryId">Hesaplanan deponun satir kimligi.</param>
/// <param name="CommitCount">Kac commit icin olcu yazildi.</param>
public sealed record MetricsResult(int RepositoryId, int CommitCount);

/// <summary>
/// Veritabanindaki ham veriden olculeri hesaplayip <c>CommitMetrics</c> tablosuna yazar.
/// Git'e hic gitmiyor: Adim 1 tarihi bir kez okudu, burasi onun uzerinde calisiyor.
/// </summary>
public sealed class MetricsRunner(SievertContext context)
{
    /// <summary>Kac olcu birikince yazildigi. Yazma tarafiyla ayni mantik (ADR 0012).</summary>
    public const int BatchSize = 500;

    /// <summary>Depoyu kimliginden ya da adindan bulur; ikisi de denenir.</summary>
    public RepositoryRow? FindRepository(string nameOrIdentity) =>
        context.Repositories.FirstOrDefault(row => row.Identity == nameOrIdentity)
        ?? context.Repositories.FirstOrDefault(row => row.Name == nameOrIdentity);

    /// <summary>
    /// Deponun butun commit'leri icin olculeri hesaplar ve yazar. Ayni depo ikinci kez
    /// hesaplanirsa eski olculer silinip yenileri yaziliyor: olculer turetilmis veri,
    /// yeniden hesaplanmalari normal (ADR 0012). Hesap deterministik oldugu icin ayni
    /// veriden ayni sonuc cikiyor.
    /// </summary>
    public MetricsResult Run(int repositoryId, MetricOptions options, Action<CommitMetrics>? observe = null)
    {
        using IDbContextTransaction transaction = context.Database.BeginTransaction();

        context.CommitMetrics
            .Where(row => row.Commit!.RepositoryId == repositoryId)
            .ExecuteDelete();

        int written = 0;
        int pending = 0;

        foreach (CommitMetrics metrics in new MetricCalculator(options).Compute(Read(repositoryId)))
        {
            observe?.Invoke(metrics);
            context.CommitMetrics.Add(ToRow(metrics));
            written++;
            pending++;

            if (pending < BatchSize)
            {
                continue;
            }

            context.SaveChanges();
            context.ChangeTracker.Clear();
            pending = 0;
        }

        if (pending > 0)
        {
            context.SaveChanges();
            context.ChangeTracker.Clear();
        }

        transaction.Commit();

        return new MetricsResult(repositoryId, written);
    }

    /// <summary>
    /// Commit'leri TARIH SIRASINDA, dosyalariyla birlikte okur.
    ///
    /// Once commit basliklari okunuyor (kucuk taraf: kimlik, eposta, tarih, baslik), sonra
    /// dosya satirlari <see cref="BatchSize"/> commit'lik obekler hâlinde. Obek obek
    /// okumanin iki sebebi var. Birincisi bellek: butun dosya tablosu (Polly'de 17 428
    /// satir) ayni anda bellege girmiyor. Ikincisi teknik: Npgsql tek baglanti uzerinde
    /// acik bir okuyucu varken yazmaya izin vermiyor, oysa olculer okuma suruyorken
    /// yaziliyor. Obegin satirlari once tamamen okunuyor, okuyucu kapaniyor, sonra
    /// yaziliyor.
    /// </summary>
    public IEnumerable<CommitForMetrics> Read(int repositoryId)
    {
        List<CommitHeader> commits = context.Commits
            .AsNoTracking()
            .Where(row => row.RepositoryId == repositoryId)
            .OrderBy(row => row.AuthorDateUtc)
            .ThenBy(row => row.Id)
            .Select(row => new CommitHeader(row.Id, row.AuthorEmail, row.AuthorDateUtc, row.MessageSubject))
            .ToList();

        foreach (CommitHeader[] chunk in commits.Chunk(BatchSize))
        {
            int[] ids = [.. chunk.Select(commit => commit.Id)];

            ILookup<int, FileForMetrics> files = context.CommitFiles
                .AsNoTracking()
                .Where(row => ids.Contains(row.CommitId))
                .Select(row => new FileRow(
                    row.CommitId,
                    row.Path,
                    row.OldPath,
                    row.LinesAdded,
                    row.LinesDeleted,
                    row.ChangeKind,
                    row.IsCSharp))
                .ToLookup(
                    row => row.CommitId,
                    row => new FileForMetrics(
                        row.Path,
                        row.OldPath,
                        row.LinesAdded,
                        row.LinesDeleted,
                        string.Equals(row.ChangeKind, "renamed", StringComparison.Ordinal),
                        row.IsCSharp));

            foreach (CommitHeader commit in chunk)
            {
                yield return new CommitForMetrics(
                    commit.Id,
                    commit.AuthorEmail,
                    commit.Date,
                    commit.Subject,
                    [.. files[commit.Id]]);
            }
        }
    }

    private static CommitMetricRow ToRow(CommitMetrics metrics) => new()
    {
        CommitId = metrics.CommitId,
        LinesAdded = metrics.LinesAdded,
        LinesDeleted = metrics.LinesDeleted,
        FilesChanged = metrics.FilesChanged,
        CsFilesChanged = metrics.CsFilesChanged,
        Entropy = metrics.Entropy,
        DirectoryCount = metrics.DirectoryCount,
        SubsystemCount = metrics.SubsystemCount,
        MaxFileAgeDays = metrics.MaxFileAgeDays,
        MinFileAgeDays = metrics.MinFileAgeDays,
        PriorChanges = metrics.PriorChanges,
        PriorFixes = metrics.PriorFixes,
        DistinctAuthorsOnFiles = metrics.DistinctAuthorsOnFiles,
        AuthorCommitCount = metrics.AuthorCommitCount,
        AuthorFileExperience = metrics.AuthorFileExperience,
        IsFix = metrics.IsFix,
    };

    private sealed record CommitHeader(int Id, string AuthorEmail, DateTimeOffset Date, string Subject);

    private sealed record FileRow(
        int CommitId,
        string Path,
        string? OldPath,
        int LinesAdded,
        int LinesDeleted,
        string ChangeKind,
        bool IsCSharp);
}
