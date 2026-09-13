using System.Text.Json;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

using Sievert.Data;
using Sievert.Data.Entities;

namespace Sievert.Demo;

/// <summary>Importun sonucu; testler ve orkestratorun ciktisi bunu okuyor.</summary>
public sealed record ImportResult(
    int RepositoryId,
    Guid RiskJobId,
    Guid? StaticJobId,
    int Commits,
    int Files,
    int Metrics,
    int Snapshots,
    int Findings,
    bool Inserted);

/// <summary>
/// Demo seed'ini veritabanina yazar.
///
/// Iki kere calistirilirsa satir cogaltmiyor: kimlikler **sabit** ve arama once mevcut
/// satiri ariyor. Sabitlik icin commit sha'si, dosya icin (commit, yol) ve is icin
/// deponun kimligi kullaniliyor; hicbiri rastgele degil.
///
/// Hata halinde transaction geri aliniyor: yarim bir demo veritabani, bos bir
/// veritabanindan daha kotudur - kullanici eksigi fark etmez.
/// </summary>
public static class SeedImporter
{
    public static async Task<ImportResult> ImportAsync(
        SievertContext context,
        string seedDirectory,
        CancellationToken cancellation)
    {
        string seedPath = Path.Combine(seedDirectory, "demo-seed.json");
        string checksumPath = Path.Combine(seedDirectory, "demo-seed.sha256");

        if (!File.Exists(seedPath) || !File.Exists(checksumPath))
        {
            throw new InvalidOperationException(
                $"Demo seed bulunamadi: {Path.GetFileName(seedPath)} ve ozet dosyasi gerekli.");
        }

        // Ozet once dogrulaniyor: bozuk bir seed OKUNMUYOR bile. Once okuyup sonra
        // dogrulamak, bozuk veriyi ayristirmis olmak demekti.
        string expected = (await File.ReadAllTextAsync(checksumPath, cancellation)).Split(' ')[0].Trim();
        string actual = SeedExporter.Checksum(seedPath);

        if (!string.Equals(expected, actual, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Demo seed ozeti tutmuyor; dosya degismis olabilir. Import yapilmadi.");
        }

        DemoSeedFile seed = JsonSerializer.Deserialize<DemoSeedFile>(
            await File.ReadAllTextAsync(seedPath, cancellation), DemoSeedFile.Json)
            ?? throw new InvalidOperationException("Demo seed okunamadi.");

        if (seed.SchemaVersion != DemoSeedFile.CurrentSchemaVersion)
        {
            throw new InvalidOperationException(
                $"Demo seed semasi {seed.SchemaVersion}; bu surum {DemoSeedFile.CurrentSchemaVersion} bekliyor.");
        }

        await using IDbContextTransaction transaction = await context.Database
            .BeginTransactionAsync(cancellation);

        try
        {
            ImportResult result = await WriteAsync(context, seed, cancellation);

            await transaction.CommitAsync(cancellation);

            return result;
        }
        catch
        {
            await transaction.RollbackAsync(cancellation);

            throw;
        }
    }

    private static async Task<ImportResult> WriteAsync(
        SievertContext context, DemoSeedFile seed, CancellationToken cancellation)
    {
        RepositoryRow? repository = await context.Repositories
            .FirstOrDefaultAsync(row => row.Identity == seed.Repository.Identity, cancellation);

        bool inserted = repository is null;

        if (repository is null)
        {
            repository = new RepositoryRow
            {
                Identity = seed.Repository.Identity,
                IdentitySource = seed.Repository.IdentitySource,
                Name = seed.Repository.Name,
                RemoteUrl = seed.Repository.RemoteUrl,
                ScannedAt = seed.RiskJob.CompletedAtUtc,
                ScannedSha = seed.Repository.ScannedSha,
            };

            context.Repositories.Add(repository);
        }

        repository.TotalCommits = seed.Repository.TotalCommits;
        repository.FirstCommitDate = seed.Commits.Count > 0 ? seed.Commits[0].AuthorDateUtc : null;
        repository.LastCommitDate = seed.Commits.Count > 0 ? seed.Commits[^1].AuthorDateUtc : null;

        // Demo isareti: yalniz kaynak sunumu. Skorlari degistirmiyor.
        repository.IsDemoData = true;

        // LocalPath bilerek yazilmiyor: demo veritabaninda bir dosya yolu bulunmasin.
        repository.LocalPath = null;

        await context.SaveChangesAsync(cancellation);

        Dictionary<string, CommitRow> existing = await context.Commits
            .Where(row => row.RepositoryId == repository.Id)
            .ToDictionaryAsync(row => row.Sha, cancellation);

        foreach (DemoCommit commit in seed.Commits)
        {
            if (!existing.TryGetValue(commit.Sha, out CommitRow? row))
            {
                row = new CommitRow
                {
                    RepositoryId = repository.Id,
                    Sha = commit.Sha,

                    // Yazar kimligi seed'de yok; demo veritabaninda da olusturulmuyor.
                    AuthorName = string.Empty,
                    AuthorEmail = string.Empty,
                    MessageSubject = commit.MessageSubject,
                    MessageFull = commit.MessageSubject,
                };

                context.Commits.Add(row);
                existing[commit.Sha] = row;
            }

            row.AuthorDateUtc = commit.AuthorDateUtc;
            row.MessageSubject = commit.MessageSubject;
            row.ParentCount = commit.ParentCount;
            row.LinesAdded = commit.LinesAdded;
            row.LinesDeleted = commit.LinesDeleted;
            row.ChangedFiles = commit.ChangedFiles;
            row.ChangedCSharpFiles = commit.ChangedCSharpFiles;
            row.IsBugIntroducing = commit.IsBugIntroducing;
            row.IsBot = commit.IsBot;
            row.LabelSource = commit.LabelSource;
        }

        await context.SaveChangesAsync(cancellation);

        Dictionary<string, int> idOf = existing.ToDictionary(pair => pair.Key, pair => pair.Value.Id);
        HashSet<int> commitIds = [.. idOf.Values];

        Dictionary<(int Commit, string Path), CommitFileRow> currentFiles = await context.CommitFiles
            .Where(row => commitIds.Contains(row.CommitId))
            .ToDictionaryAsync(row => (row.CommitId, row.Path), cancellation);

        foreach (DemoFile file in seed.Files)
        {
            int commitId = idOf[file.Sha];

            if (!currentFiles.TryGetValue((commitId, file.Path), out CommitFileRow? row))
            {
                row = new CommitFileRow
                {
                    CommitId = commitId,
                    Path = file.Path,
                    ChangeKind = file.ChangeKind,
                };

                context.CommitFiles.Add(row);
                currentFiles[(commitId, file.Path)] = row;
            }

            row.OldPath = file.OldPath;
            row.ChangeKind = file.ChangeKind;
            row.LinesAdded = file.LinesAdded;
            row.LinesDeleted = file.LinesDeleted;
            row.IsCSharp = file.IsCSharp;
        }

        Dictionary<int, CommitMetricRow> currentMetrics = await context.CommitMetrics
            .Where(row => commitIds.Contains(row.CommitId))
            .ToDictionaryAsync(row => row.CommitId, cancellation);

        foreach (DemoMetric metric in seed.Metrics)
        {
            int commitId = idOf[metric.Sha];

            if (!currentMetrics.TryGetValue(commitId, out CommitMetricRow? row))
            {
                row = new CommitMetricRow { CommitId = commitId };

                context.CommitMetrics.Add(row);
                currentMetrics[commitId] = row;
            }

            row.LinesAdded = metric.LinesAdded;
            row.LinesDeleted = metric.LinesDeleted;
            row.FilesChanged = metric.FilesChanged;
            row.CsFilesChanged = metric.CsFilesChanged;
            row.Entropy = metric.Entropy;
            row.DirectoryCount = metric.DirectoryCount;
            row.SubsystemCount = metric.SubsystemCount;
            row.MaxFileAgeDays = metric.MaxFileAgeDays;
            row.MinFileAgeDays = metric.MinFileAgeDays;
            row.PriorChanges = metric.PriorChanges;
            row.PriorFixes = metric.PriorFixes;
            row.DistinctAuthorsOnFiles = metric.DistinctAuthorsOnFiles;
            row.AuthorCommitCount = metric.AuthorCommitCount;
            row.AuthorFileExperience = metric.AuthorFileExperience;
            row.IsFix = metric.IsFix;
        }

        await context.SaveChangesAsync(cancellation);

        Guid riskJobId = await UpsertJobAsync(
            context, repository.Id, AnalysisJobKind.RiskScoreAll, seed.RiskJob, cancellation);

        Dictionary<int, CommitRiskSnapshotRow> currentSnapshots = await context.CommitRiskSnapshots
            .Where(row => row.AnalysisJobId == riskJobId)
            .ToDictionaryAsync(row => row.CommitId, cancellation);

        foreach (DemoSnapshot snapshot in seed.Snapshots)
        {
            int commitId = idOf[snapshot.Sha];

            if (!currentSnapshots.TryGetValue(commitId, out CommitRiskSnapshotRow? row))
            {
                row = new CommitRiskSnapshotRow
                {
                    AnalysisJobId = riskJobId,
                    CommitId = commitId,
                    ModelProfile = snapshot.ModelProfile,
                    ModelChecksum = snapshot.ModelChecksum,
                    WarningCodes = snapshot.WarningCodes,
                };

                context.CommitRiskSnapshots.Add(row);
                currentSnapshots[commitId] = row;
            }

            row.RawModelScore = snapshot.RawModelScore;
            row.RiskIndex = snapshot.RiskIndex;
            row.DecisionAt05 = snapshot.DecisionAt05;
            row.DecisionAtTrainThreshold = snapshot.DecisionAtTrainThreshold;
            row.TrainThreshold = snapshot.TrainThreshold;
            row.ModelProfile = snapshot.ModelProfile;
            row.ModelChecksum = snapshot.ModelChecksum;
            row.WarningCodes = snapshot.WarningCodes;
            row.IsCalibrated = false;
            row.CreatedAtUtc = seed.RiskJob.CompletedAtUtc;
        }

        await context.SaveChangesAsync(cancellation);

        Guid? staticJobId = null;

        if (seed.StaticJob is DemoStaticJob scan)
        {
            staticJobId = await UpsertJobAsync(
                context, repository.Id, AnalysisJobKind.StaticScan, scan, cancellation);

            int currentFindings = await context.StaticAnalysisFindings
                .CountAsync(row => row.AnalysisJobId == staticJobId, cancellation);

            // Bulgularin dogal bir anahtari yok (ayni kural ayni satirda iki kez
            // cikabiliyor), o yuzden sayidan anlasiliyor: zaten yazilmissa dokunulmuyor.
            if (currentFindings == 0)
            {
                context.StaticAnalysisFindings.AddRange(seed.Findings.Select(finding =>
                    new StaticAnalysisFindingRow
                    {
                        AnalysisJobId = staticJobId.Value,
                        RuleCode = finding.RuleCode,
                        Severity = finding.Severity,
                        RelativePath = finding.RelativePath,
                        Line = finding.Line,
                        Column = finding.Column,
                        MemberName = finding.MemberName,
                        Message = finding.Message,
                        Rationale = finding.Rationale,
                        IsTestCode = finding.IsTestCode,
                        IsSuppressed = finding.IsSuppressed,
                        CreatedAtUtc = scan.CompletedAtUtc,
                    }));

                await context.SaveChangesAsync(cancellation);
            }
        }

        return new ImportResult(
            repository.Id,
            riskJobId,
            staticJobId,
            seed.Commits.Count,
            seed.Files.Count,
            seed.Metrics.Count,
            seed.Snapshots.Count,
            seed.Findings.Count,
            inserted);
    }

    /// <summary>
    /// Isi sabit bir anahtarla yazar: ayni depo ve ayni tur icin demo isi bir tane.
    ///
    /// Kimlik rastgele uretiliyor ama **yalniz ilk seferde**; ikinci import ayni isi
    /// buluyor ve yeni bir kimlik uretmiyor, yoksa iki import iki farkli rapor kaynagi
    /// birakirdi.
    /// </summary>
    private static async Task<Guid> UpsertJobAsync(
        SievertContext context,
        int repositoryId,
        AnalysisJobKind kind,
        object job,
        CancellationToken cancellation)
    {
        AnalysisJobRow? existing = await context.AnalysisJobs
            .Where(row => row.RepositoryId == repositoryId && row.Kind == kind)
            .OrderBy(row => row.RequestedAtUtc)
            .FirstOrDefaultAsync(cancellation);

        AnalysisJobRow row = existing ?? new AnalysisJobRow
        {
            Id = Guid.CreateVersion7(),
            RepositoryId = repositoryId,
            Kind = kind,
        };

        if (existing is null)
        {
            context.AnalysisJobs.Add(row);
        }

        switch (job)
        {
            case DemoRiskJob risk:
                row.Status = AnalysisJobStatus.Succeeded;
                row.RequestedAtUtc = risk.RequestedAtUtc;
                row.StartedAtUtc = risk.StartedAtUtc;
                row.CompletedAtUtc = risk.CompletedAtUtc;
                row.ResultCount = risk.ResultCount;
                row.IsResultComplete = risk.IsResultComplete;
                row.CurrentPhase = "succeeded";
                row.ResultSummary = $"{{\"modelProfile\":\"{risk.ModelProfile}\"}}";
                break;

            case DemoStaticJob scan:
                row.Status = AnalysisJobStatus.Succeeded;
                row.RequestedAtUtc = scan.RequestedAtUtc;
                row.StartedAtUtc = scan.StartedAtUtc;
                row.CompletedAtUtc = scan.CompletedAtUtc;
                row.ResultCount = scan.ResultCount;
                row.IsResultComplete = scan.IsResultComplete;
                row.CurrentPhase = "succeeded";
                row.SourceHeadSha = scan.SourceHeadSha;
                row.SourceHeadShortSha = scan.SourceHeadSha?[..Math.Min(12, scan.SourceHeadSha.Length)];
                row.SourceTreeState = scan.SourceTreeState;
                row.SourceStateVerifiedAtUtc = scan.CompletedAtUtc;
                row.ResultSummary = scan.ResultSummary;
                break;
        }

        row.ActiveDeduplicationKey = null;

        await context.SaveChangesAsync(cancellation);

        return row.Id;
    }
}
