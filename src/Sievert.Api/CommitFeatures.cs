using Sievert.Data.Entities;
using Sievert.Modeling;

namespace Sievert.Api;

/// <summary>
/// Veritabani satirlarini modelin bekledigi satira cevirir.
///
/// Ara bir bicim uydurmuyorum: <see cref="SnapshotRow"/> Asama 5'te modelin egitildigi
/// bicim ve <see cref="FeatureScaler"/> tam olarak onu donusturuyor. Ikinci bir yol
/// acmak, egitimdeki donusum ile istekteki donusumun zamanla ayrismasi demek olurdu.
/// </summary>
public static class CommitFeatures
{
    public static SnapshotRow ToSnapshotRow(RepositoryRow repository, CommitRow commit, CommitMetricRow metric) =>
        new(
            repository.Name,
            repository.Identity,
            commit.Sha,
            commit.AuthorDateUtc,
            metric.LinesAdded,
            metric.LinesDeleted,
            metric.FilesChanged,
            metric.CsFilesChanged,
            metric.Entropy,
            metric.DirectoryCount,
            metric.SubsystemCount,
            metric.MaxFileAgeDays,
            metric.MinFileAgeDays,
            metric.PriorChanges,
            metric.PriorFixes,
            metric.DistinctAuthorsOnFiles,
            metric.AuthorCommitCount,
            metric.AuthorFileExperience,
            metric.IsFix,
            commit.IsBugIntroducing,
            commit.LabelSource,
            commit.IsBot);
}
