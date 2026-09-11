using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Sievert.Data;

/// <summary>Etiketleme sonucu.</summary>
/// <param name="Labelled">Hata getiren diye isaretlenen commit sayisi.</param>
/// <param name="Cleared">Once isaretli olup bu kosuda isareti kalkan commit sayisi.</param>
/// <param name="Total">Deponun toplam commit sayisi; etiket orani bundan hesaplaniyor.</param>
public sealed record LabelResult(int Labelled, int Cleared, int Total);

/// <summary>
/// SZZ etiketlerini veritabanina yazar. Idempotent: once deponun butun etiketleri
/// siliniyor, sonra verilen sha'lar isaretleniyor. Ayni girdiden ayni sonuc cikiyor ve
/// bir onceki kosudan kalan etiket birikmiyor.
/// </summary>
public sealed class LabelStore(SievertContext context)
{
    /// <summary>Etiketin kaynagi. Ileride baska bir yontem eklenirse burada ayrisacak.</summary>
    public const string Source = "szz";

    public LabelResult Apply(int repositoryId, IReadOnlyCollection<string> blamedShas)
    {
        using IDbContextTransaction transaction = context.Database.BeginTransaction();

        int cleared = context.Commits
            .Where(row => row.RepositoryId == repositoryId && row.IsBugIntroducing)
            .ExecuteUpdate(setters => setters
                .SetProperty(row => row.IsBugIntroducing, false)
                .SetProperty(row => row.LabelSource, (string?)null));

        string[] shas = [.. blamedShas];

        int labelled = context.Commits
            .Where(row => row.RepositoryId == repositoryId && shas.Contains(row.Sha))
            .ExecuteUpdate(setters => setters
                .SetProperty(row => row.IsBugIntroducing, true)
                .SetProperty(row => row.LabelSource, Source));

        int total = context.Commits.Count(row => row.RepositoryId == repositoryId);

        transaction.Commit();

        return new LabelResult(labelled, cleared, total);
    }

    /// <summary>Deponun duzeltme commit'leri: sha ve yazar tarihi.</summary>
    public IReadOnlyList<(string Sha, DateTimeOffset Date)> Fixes(int repositoryId) =>
        [.. context.CommitMetrics
            .AsNoTracking()
            .Where(metric => metric.IsFix && metric.Commit!.RepositoryId == repositoryId)
            .OrderBy(metric => metric.Commit!.AuthorDateUtc)
            .Select(metric => new ValueTuple<string, DateTimeOffset>(
                metric.Commit!.Sha,
                metric.Commit!.AuthorDateUtc))];
}
