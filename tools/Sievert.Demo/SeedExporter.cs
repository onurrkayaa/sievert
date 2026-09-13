using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using Microsoft.EntityFrameworkCore;

using Sievert.Data;
using Sievert.Data.Entities;

namespace Sievert.Demo;

/// <summary>
/// Gercek veritabanindan demo alt kumesini cikarir.
///
/// Cikan dosya iki kez uretildiginde **bayt olarak ayni** olmali: o yuzden hicbir yerde
/// uretim zamani, rastgele kimlik ya da kultura bagli bicim yok. Kaynak kimlikleri de
/// tasinmiyor; importer kendi kimliklerini uretiyor.
/// </summary>
public static class SeedExporter
{
    public static async Task<int> RunAsync(string outputDirectory, string codeCommit, CancellationToken cancellation)
    {
        ConnectionStringResult found = ConnectionString.Find(Directory.GetCurrentDirectory());

        if (found.Value is not string connection)
        {
            Console.Error.WriteLine(found.Error ?? $"{ConnectionString.EnvironmentVariable} tanimli degil.");

            return 2;
        }

        await using SievertContext context = SievertContextBuilder.Create(connection);

        RepositoryRow? repository = await context.Repositories
            .AsNoTracking()
            .FirstOrDefaultAsync(row => row.Identity == DemoSelection.RepositoryIdentity, cancellation);

        if (repository is null)
        {
            Console.Error.WriteLine($"{DemoSelection.RepositoryIdentity} veritabaninda yok.");

            return 2;
        }

        // Secim kurali: en yeni N commit, tarih sirasinda. Esitlikte madencilik sirasi,
        // yani ayni sorgu her zaman ayni kumeyi veriyor.
        List<CommitRow> newest = await context.Commits
            .AsNoTracking()
            .Where(row => row.RepositoryId == repository.Id)
            .OrderByDescending(row => row.AuthorDateUtc)
            .ThenByDescending(row => row.Id)
            .Take(DemoSelection.CommitCount)
            .ToListAsync(cancellation);

        List<CommitRow> commits = [.. newest.OrderBy(row => row.AuthorDateUtc).ThenBy(row => row.Id)];

        if (commits.Count < DemoSelection.CommitCount)
        {
            Console.Error.WriteLine(
                $"Depoda {DemoSelection.CommitCount} commit yok; bulunan {commits.Count}.");

            return 2;
        }

        HashSet<int> ids = [.. commits.Select(row => row.Id)];
        Dictionary<int, string> shaOf = commits.ToDictionary(row => row.Id, row => row.Sha);

        List<CommitFileRow> files = await context.CommitFiles
            .AsNoTracking()
            .Where(row => ids.Contains(row.CommitId))
            .OrderBy(row => row.CommitId)
            .ThenBy(row => row.Path)
            .ToListAsync(cancellation);

        List<CommitMetricRow> metrics = await context.CommitMetrics
            .AsNoTracking()
            .Where(row => ids.Contains(row.CommitId))
            .OrderBy(row => row.CommitId)
            .ToListAsync(cancellation);

        AnalysisJobRow? risk = await context.AnalysisJobs
            .AsNoTracking()
            .Where(job => job.RepositoryId == repository.Id
                && job.Kind == AnalysisJobKind.RiskScoreAll
                && job.Status == AnalysisJobStatus.Succeeded
                && job.IsResultComplete)
            .OrderByDescending(job => job.CompletedAtUtc)
            .FirstOrDefaultAsync(cancellation);

        if (risk is null)
        {
            Console.Error.WriteLine("Tamamlanmis bir risk skorlama isi yok.");

            return 2;
        }

        List<CommitRiskSnapshotRow> snapshots = await context.CommitRiskSnapshots
            .AsNoTracking()
            .Where(row => row.AnalysisJobId == risk.Id && ids.Contains(row.CommitId))
            .OrderBy(row => row.CommitId)
            .ToListAsync(cancellation);

        AnalysisJobRow? scan = await context.AnalysisJobs
            .AsNoTracking()
            .Where(job => job.RepositoryId == repository.Id
                && job.Kind == AnalysisJobKind.StaticScan
                && job.Status == AnalysisJobStatus.Succeeded
                && job.IsResultComplete)
            .OrderByDescending(job => job.CompletedAtUtc)
            .FirstOrDefaultAsync(cancellation);

        List<StaticAnalysisFindingRow> findings = scan is null
            ? []
            : await context.StaticAnalysisFindings
                .AsNoTracking()
                .Where(row => row.AnalysisJobId == scan.Id)
                .OrderBy(row => row.RuleCode)
                .ThenBy(row => row.RelativePath)
                .ThenBy(row => row.Line)
                .ThenBy(row => row.Id)
                .ToListAsync(cancellation);

        DemoSeedFile seed = new(
            DemoSeedFile.CurrentSchemaVersion,
            new DemoRepository(
                repository.Identity,
                repository.IdentitySource,
                repository.Name,
                repository.RemoteUrl,
                commits.Count,
                repository.ScannedSha),
            [.. commits.Select(Commit)],
            [.. files.Select(row => File(row, shaOf))],
            [.. metrics.Select(row => Metric(row, shaOf))],
            new DemoRiskJob(
                AnalysisJobRow.Name(risk.Status),
                risk.RequestedAtUtc,
                risk.StartedAtUtc ?? risk.RequestedAtUtc,
                risk.CompletedAtUtc ?? risk.RequestedAtUtc,
                snapshots.Count,
                true,
                snapshots.Count > 0 ? snapshots[0].ModelProfile : "polly",
                snapshots.Count > 0 ? snapshots[0].ModelChecksum : string.Empty),
            [.. snapshots.Select(row => Snapshot(row, shaOf))],
            scan is null
                ? null
                : new DemoStaticJob(
                    AnalysisJobRow.Name(scan.Status),
                    scan.RequestedAtUtc,
                    scan.StartedAtUtc ?? scan.RequestedAtUtc,
                    scan.CompletedAtUtc ?? scan.RequestedAtUtc,
                    findings.Count,
                    true,
                    scan.SourceHeadSha,
                    scan.SourceTreeState,
                    scan.ResultSummary),
            [.. findings.Select(Finding)]);

        Directory.CreateDirectory(outputDirectory);

        string seedPath = Path.Combine(outputDirectory, "demo-seed.json");
        string manifestPath = Path.Combine(outputDirectory, "source-manifest.json");

        string json = JsonSerializer.Serialize(seed, DemoSeedFile.Json) + "\n";

        // BOM yok, LF: iki uretimin bayt olarak ayni olmasi bunlara bagli.
        await System.IO.File.WriteAllTextAsync(seedPath, json, new UTF8Encoding(false), cancellation);

        string seedChecksum = Checksum(seedPath);

        object manifest = new
        {
            sourceRepository = repository.Identity,
            sourceSnapshot = repository.ScannedSha,
            selectionRule = DemoSelection.Rule,
            commitCount = commits.Count,
            firstCommitSha = commits[0].Sha,
            firstCommitDateUtc = Moment(commits[0].AuthorDateUtc),
            lastCommitSha = commits[^1].Sha,
            lastCommitDateUtc = Moment(commits[^1].AuthorDateUtc),
            commitFileCount = files.Count,
            metricCount = metrics.Count,
            bugIntroducingCount = commits.Count(row => row.IsBugIntroducing),
            botCommitCount = commits.Count(row => row.IsBot),
            riskSnapshotCount = snapshots.Count,
            modelProfile = snapshots.Count > 0 ? snapshots[0].ModelProfile : null,
            modelChecksum = snapshots.Count > 0 ? snapshots[0].ModelChecksum : null,
            staticFindingCount = findings.Count,
            producedByCommit = codeCommit,
            producedByCommand = "dotnet run --project tools/Sievert.Demo -- export-seed",
            seedChecksum,
            scopeLimit = "Bu alt kume yalniz demo icindir; olcum ve model sonuclari icin "
                + "kullanilmaz. Tam veri data/asama5 altindaki dondurulmus dosyalarda.",
        };

        await System.IO.File.WriteAllTextAsync(
            manifestPath,
            JsonSerializer.Serialize(manifest, DemoSeedFile.Json) + "\n",
            new UTF8Encoding(false),
            cancellation);

        await System.IO.File.WriteAllTextAsync(
            Path.Combine(outputDirectory, "demo-seed.sha256"),
            $"{seedChecksum}  demo-seed.json\n",
            new UTF8Encoding(false),
            cancellation);

        await System.IO.File.WriteAllTextAsync(
            Path.Combine(outputDirectory, "source-manifest.sha256"),
            $"{Checksum(manifestPath)}  source-manifest.json\n",
            new UTF8Encoding(false),
            cancellation);

        Console.WriteLine($"{commits.Count} commit, {files.Count} dosya satiri, {metrics.Count} olcu");
        Console.WriteLine($"{snapshots.Count} risk satiri, {findings.Count} statik bulgu");
        Console.WriteLine($"seed ozeti: {seedChecksum}");

        return 0;
    }

    /// <summary>Dosyanin SHA-256'si, kucuk harf onaltilik.</summary>
    public static string Checksum(string path) =>
        Convert.ToHexStringLower(SHA256.HashData(System.IO.File.ReadAllBytes(path)));

    private static string Moment(DateTimeOffset value) =>
        value.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);

    private static DemoCommit Commit(CommitRow row) => new(
        row.Sha,
        row.AuthorDateUtc,
        Clean(row.MessageSubject),
        row.ParentCount,
        row.LinesAdded,
        row.LinesDeleted,
        row.ChangedFiles,
        row.ChangedCSharpFiles,
        row.IsBugIntroducing,
        row.IsBot,
        row.LabelSource);

    private static DemoFile File(CommitFileRow row, Dictionary<int, string> shaOf) => new(
        shaOf[row.CommitId],
        row.Path,
        row.OldPath,
        row.ChangeKind,
        row.LinesAdded,
        row.LinesDeleted,
        row.IsCSharp);

    private static DemoMetric Metric(CommitMetricRow row, Dictionary<int, string> shaOf) => new(
        shaOf[row.CommitId],
        row.LinesAdded,
        row.LinesDeleted,
        row.FilesChanged,
        row.CsFilesChanged,
        row.Entropy,
        row.DirectoryCount,
        row.SubsystemCount,
        row.MaxFileAgeDays,
        row.MinFileAgeDays,
        row.PriorChanges,
        row.PriorFixes,
        row.DistinctAuthorsOnFiles,
        row.AuthorCommitCount,
        row.AuthorFileExperience,
        row.IsFix);

    private static DemoSnapshot Snapshot(CommitRiskSnapshotRow row, Dictionary<int, string> shaOf) => new(
        shaOf[row.CommitId],
        row.RawModelScore,
        row.RiskIndex,
        row.DecisionAt05,
        row.DecisionAtTrainThreshold,
        row.TrainThreshold,
        row.ModelProfile,
        row.ModelChecksum,
        row.WarningCodes);

    private static DemoFinding Finding(StaticAnalysisFindingRow row) => new(
        row.RuleCode,
        row.Severity,
        row.RelativePath,
        row.Line,
        row.Column,
        row.MemberName,
        Clean(row.Message),
        Clean(row.Rationale),
        row.IsTestCode,
        row.IsSuppressed);

    /// <summary>Kontrol karakterlerini temizler; commit basligi tek satir kaliyor.</summary>
    private static string Clean(string value)
    {
        StringBuilder cleaned = new(value.Length);

        foreach (char character in value)
        {
            cleaned.Append(char.IsControl(character) ? ' ' : character);
        }

        return cleaned.ToString().Trim();
    }
}
