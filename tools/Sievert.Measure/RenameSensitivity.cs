using System.Globalization;
using System.Text;
using System.Text.Json;

using LibGit2Sharp;

using Sievert.Data.Metrics;
using Sievert.Modeling;

namespace Sievert.Measure;

/// <summary>
/// Adim 4.5: ad degisimi duyarliligi. Iki ayri soru var ve karistirilmiyor.
///
/// 1) ZINCIR: bir dosyanin adi degisince gecmisi yeni yola tasinsin mi
///    (<c>MetricOptions.FollowRenames</c>). Bu, git'e gitmeden ayni diff'lerden
///    hesaplanabiliyor.
/// 2) ESIK: git'in ad degisimi benzerlik esigi (40 / 50 / 60). Bu, git tarihinin farkli
///    bir esikle yeniden okunmasini gerektiriyor.
///
/// Ana snapshot ve veritabani DEGISMIYOR: bu arac git'i yalnizca okuyor ve ciktisini
/// ayri bir klasore yaziyor. Etiketler dondurulmus anlik goruntuden SHA ile baglaniyor.
/// </summary>
public static class RenameSensitivity
{
    /// <summary>Sonuc gormeden sabitlenen esikler. 50 ana kosudaki deger.</summary>
    private static readonly int[] Thresholds = [40, 50, 60];

    private static readonly string[] Historical =
    [
        "PriorFixes",
        "PriorChanges",
        "AuthorCommitCount",
        "AuthorFileExperience",
        "DistinctAuthorsOnFiles",
        "MaxFileAgeDays",
        "MinFileAgeDays",
    ];

    public static int Run(string[] args)
    {
        string dataDirectory = args[1];
        string codeCommit = args[2];
        string outputDirectory = args[3];

        string snapshot = Path.Combine(dataDirectory, "commit-metrics.csv");
        string manifest = Path.Combine(dataDirectory, "split-manifest.csv");

        foreach (string file in (string[])[snapshot, manifest])
        {
            FileChecksum.Verify(file, Path.ChangeExtension(file, ".sha256"));
        }

        IReadOnlyList<SnapshotRow> rows = SnapshotReader.Read(snapshot);
        IReadOnlyList<SplitEntry> entries = SplitManifestReader.Read(manifest);

        Dictionary<string, List<(string Identity, string Path)>> clones = Clones(args[4]);
        Directory.CreateDirectory(outputDirectory);

        using MemoryStream stream = new();
        Utf8JsonWriter writer = new(stream, new JsonWriterOptions { Indented = true });

        writer.WriteStartObject();
        writer.WriteString("snapshotSha256", FileChecksum.Sha256(snapshot));
        writer.WriteString("manifestSha256", FileChecksum.Sha256(manifest));
        writer.WriteString("codeCommit", codeCommit);
        writer.WriteString(
            "note",
            "git yeniden okundu; ana snapshot ve veritabani degismedi. Etiketler SHA ile "
            + "dondurulmus hedeften baglandi. Commit sirasi bu araçta tarih + SHA ordinal.");
        writer.WriteStartArray("repositories");

        foreach ((string identity, string path) in clones.Values.SelectMany(entry => entry))
        {
            Console.WriteLine();
            Console.WriteLine($"== {identity} ==");

            Dictionary<int, Dictionary<string, CommitMetrics>> byThreshold = [];
            ChainEffect? chain = null;

            foreach (int threshold in Thresholds)
            {
                DateTimeOffset started = DateTimeOffset.UtcNow;
                (List<CommitForMetrics> commits, int renames, Dictionary<int, string> shas) = ReadGit(path, threshold);

                Dictionary<string, CommitMetrics> metrics = new(StringComparer.Ordinal);

                foreach (CommitMetrics computed in new MetricCalculator(MetricOptions.Default).Compute(commits))
                {
                    metrics[shas[computed.CommitId]] = computed;
                }

                byThreshold[threshold] = metrics;

                Console.WriteLine(
                    $"  esik {threshold}: {commits.Count} commit, {renames} ad degisimi, "
                    + $"{(DateTimeOffset.UtcNow - started).TotalSeconds.ToString("F1", CultureInfo.InvariantCulture)} sn");

                if (threshold == 50)
                {
                    chain = Chain(identity, commits, shas, metrics);
                }
            }

            writer.WriteStartObject();
            writer.WriteString("repository", identity);

            if (chain is not null)
            {
                writer.WriteStartObject("chainEffect");
                writer.WriteNumber("commits", chain.Commits);
                writer.WriteNumber("affectedCommits", chain.AffectedCommits);
                writer.WriteNumber("priorChangesDifferent", chain.PriorChangesDifferent);
                writer.WriteNumber("priorChangesLargestDifference", chain.PriorChangesLargest);
                writer.WriteNumber("priorChangesTotalFollowed", chain.PriorChangesFollowed);
                writer.WriteNumber("priorChangesTotalPlain", chain.PriorChangesPlain);
                writer.WriteNumber("maxFileAgeDifferent", chain.MaxFileAgeDifferent);
                writer.WriteNumber("maxFileAgeLargestDifference", chain.MaxFileAgeLargest);
                writer.WriteNumber("maxFileAgeTotalFollowed", chain.MaxFileAgeFollowed);
                writer.WriteNumber("maxFileAgeTotalPlain", chain.MaxFileAgePlain);
                writer.WriteEndObject();
            }

            writer.WriteStartArray("thresholds");

            foreach (int threshold in Thresholds)
            {
                Compare(writer, identity, threshold, byThreshold[threshold], byThreshold[50], rows, entries);
            }

            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
        writer.WriteEndObject();
        writer.Flush();

        string output = Path.Combine(outputDirectory, "rename-results.json");
        File.WriteAllText(output, Encoding.UTF8.GetString(stream.ToArray()) + "\n", new UTF8Encoding(false));
        File.WriteAllText(Path.ChangeExtension(output, ".sha256"), FileChecksum.Line(output));

        Console.WriteLine();
        Console.WriteLine($"rename-results.json: {FileChecksum.Sha256(output)}");

        return 0;
    }

    /// <summary>Zincir takibi acik/kapali karsilastirmasinin sayilari.</summary>
    private sealed record ChainEffect(
        int Commits,
        int AffectedCommits,
        int PriorChangesDifferent,
        int PriorChangesLargest,
        long PriorChangesFollowed,
        long PriorChangesPlain,
        int MaxFileAgeDifferent,
        int MaxFileAgeLargest,
        long MaxFileAgeFollowed,
        long MaxFileAgePlain);

    /// <summary>Zincir takibi acik/kapali karsilastirmasi; ayni diff'lerden hesaplaniyor.</summary>
    private static ChainEffect Chain(
        string identity,
        List<CommitForMetrics> commits,
        Dictionary<int, string> shas,
        Dictionary<string, CommitMetrics> followed)
    {
        Dictionary<string, CommitMetrics> plain = new(StringComparer.Ordinal);

        foreach (CommitMetrics computed in new MetricCalculator(new MetricOptions(FollowRenames: false)).Compute(commits))
        {
            plain[shas[computed.CommitId]] = computed;
        }

        int changed = 0;
        long priorChangesFollowed = 0;
        long priorChangesPlain = 0;
        long ageFollowed = 0;
        long agePlain = 0;
        int priorChangesDifferent = 0;
        int ageDifferent = 0;
        int largestPriorChanges = 0;
        int largestAge = 0;

        foreach (KeyValuePair<string, CommitMetrics> entry in followed)
        {
            CommitMetrics other = plain[entry.Key];

            priorChangesFollowed += entry.Value.PriorChanges;
            priorChangesPlain += other.PriorChanges;
            ageFollowed += entry.Value.MaxFileAgeDays;
            agePlain += other.MaxFileAgeDays;

            if (entry.Value.PriorChanges != other.PriorChanges)
            {
                priorChangesDifferent++;
                largestPriorChanges = Math.Max(largestPriorChanges, Math.Abs(entry.Value.PriorChanges - other.PriorChanges));
            }

            if (entry.Value.MaxFileAgeDays != other.MaxFileAgeDays)
            {
                ageDifferent++;
                largestAge = Math.Max(largestAge, Math.Abs(entry.Value.MaxFileAgeDays - other.MaxFileAgeDays));
            }

            if (Different(entry.Value, other))
            {
                changed++;
            }
        }

        Console.WriteLine(
            $"    zincir: etkilenen commit {changed} / {commits.Count}, "
            + $"PriorChanges farkli {priorChangesDifferent} (en buyuk {largestPriorChanges}), "
            + $"MaxFileAgeDays farkli {ageDifferent} (en buyuk {largestAge})");

        return new ChainEffect(
            commits.Count,
            changed,
            priorChangesDifferent,
            largestPriorChanges,
            priorChangesFollowed,
            priorChangesPlain,
            ageDifferent,
            largestAge,
            ageFollowed,
            agePlain);
    }

    /// <summary>Bir esigin metriklerini ana esikle (50) karsilastirir ve modeli kosar.</summary>
    private static void Compare(
        Utf8JsonWriter writer,
        string identity,
        int threshold,
        Dictionary<string, CommitMetrics> metrics,
        Dictionary<string, CommitMetrics> main,
        IReadOnlyList<SnapshotRow> snapshot,
        IReadOnlyList<SplitEntry> entries)
    {
        int changed = 0;

        foreach (KeyValuePair<string, CommitMetrics> entry in metrics)
        {
            if (main.TryGetValue(entry.Key, out CommitMetrics? other) && Different(entry.Value, other))
            {
                changed++;
            }
        }

        // Etiketler dondurulmus hedeften SHA ile baglaniyor; metrikler bu esikten geliyor.
        List<SnapshotRow> rebuilt = [];

        foreach (SnapshotRow row in snapshot)
        {
            if (!string.Equals(row.RepositoryIdentity, identity, StringComparison.Ordinal))
            {
                continue;
            }

            rebuilt.Add(metrics.TryGetValue(row.Sha, out CommitMetrics? computed)
                ? row with
                {
                    LinesAdded = computed.LinesAdded,
                    LinesDeleted = computed.LinesDeleted,
                    FilesChanged = computed.FilesChanged,
                    CsFilesChanged = computed.CsFilesChanged,
                    Entropy = computed.Entropy,
                    DirectoryCount = computed.DirectoryCount,
                    SubsystemCount = computed.SubsystemCount,
                    MaxFileAgeDays = computed.MaxFileAgeDays,
                    MinFileAgeDays = computed.MinFileAgeDays,
                    PriorChanges = computed.PriorChanges,
                    PriorFixes = computed.PriorFixes,
                    DistinctAuthorsOnFiles = computed.DistinctAuthorsOnFiles,
                    AuthorCommitCount = computed.AuthorCommitCount,
                    AuthorFileExperience = computed.AuthorFileExperience,
                    IsFix = computed.IsFix,
                }
                : row);
        }

        IReadOnlyList<RepositorySplit> splits = SplitData.Build(
            rebuilt,
            [.. entries.Where(entry => string.Equals(entry.RepositoryIdentity, identity, StringComparison.Ordinal))]);

        SubsetOutcome outcome = Sensitivity.Retrain(
            "esik-" + threshold.ToString(CultureInfo.InvariantCulture),
            identity,
            splits[0].Train,
            splits[0].Test,
            ModelFeatures.Candidates);

        Console.WriteLine(
            $"    esik {threshold}: 50'ye gore metrigi degisen commit {changed}, "
            + $"F1 {outcome.Counts.F1?.ToString("F4", CultureInfo.InvariantCulture) ?? "N/A"}, "
            + $"PR-AUC {outcome.PrAuc?.ToString("F4", CultureInfo.InvariantCulture) ?? "N/A"}");

        writer.WriteStartObject();
        writer.WriteNumber("threshold", threshold);
        writer.WriteNumber("commitsDifferentFromMain", changed);
        writer.WriteNumber("rows", outcome.Rows);
        writer.WriteNumber("positives", outcome.Positives);
        writer.WriteNumber("chosenThreshold", outcome.Threshold);
        writer.WriteNumber("tp", outcome.Counts.TruePositives);
        writer.WriteNumber("fp", outcome.Counts.FalsePositives);
        writer.WriteNumber("fn", outcome.Counts.FalseNegatives);
        writer.WriteNumber("tn", outcome.Counts.TrueNegatives);

        if (outcome.Counts.F1 is double f1)
        {
            writer.WriteNumber("f1", f1);
        }
        else
        {
            writer.WriteNull("f1");
        }

        if (outcome.PrAuc is double area)
        {
            writer.WriteNumber("prAuc", area);
        }
        else
        {
            writer.WriteNull("prAuc");
        }

        writer.WriteNumber("brier", outcome.Brier);
        writer.WriteNumber("ece", outcome.Ece);
        writer.WriteEndObject();
    }

    private static bool Different(CommitMetrics one, CommitMetrics other) =>
        one.PriorFixes != other.PriorFixes
        || one.PriorChanges != other.PriorChanges
        || one.AuthorCommitCount != other.AuthorCommitCount
        || one.AuthorFileExperience != other.AuthorFileExperience
        || one.DistinctAuthorsOnFiles != other.DistinctAuthorsOnFiles
        || one.MaxFileAgeDays != other.MaxFileAgeDays
        || one.MinFileAgeDays != other.MinFileAgeDays;

    /// <summary>
    /// Git'i verilen benzerlik esigiyle okur. Birlestirme commit'leri atlaniyor ve yazar
    /// tarihi kullaniliyor; ana madencilikle ayni politika (ADR 0011).
    /// </summary>
    private static (List<CommitForMetrics> Commits, int Renames, Dictionary<int, string> Shas) ReadGit(
        string path,
        int threshold)
    {
        LibGit2Sharp.CompareOptions compare = new()
        {
            Similarity = new SimilarityOptions
            {
                RenameDetectionMode = RenameDetectionMode.Renames,
                RenameThreshold = threshold,
            },
        };

        using Repository repository = new(path);

        List<(DateTimeOffset Date, string Sha, string Email, string Subject, List<FileForMetrics> Files)> read = [];
        int renames = 0;

        foreach (Commit commit in repository.Commits.QueryBy(new CommitFilter { SortBy = CommitSortStrategies.Time }))
        {
            // sievert:disable SV004 Parents bellekteki koleksiyon, veritabani sorgusu degil
            if (commit.Parents.Count() > 1)
            {
                continue;
            }

            // sievert:disable SV004 her commit'in kendi ebeveynine bakiliyor; dongunun disina alinamaz
            Tree? parent = commit.Parents.FirstOrDefault()?.Tree;

            using Patch patch = repository.Diff.Compare<Patch>(parent, commit.Tree, compare);

            List<FileForMetrics> files = [];

            foreach (PatchEntryChanges change in patch)
            {
                bool rename = change.Status == ChangeKind.Renamed;

                if (rename)
                {
                    renames++;
                }

                files.Add(new FileForMetrics(
                    change.Path,
                    rename ? change.OldPath : null,
                    change.LinesAdded,
                    change.LinesDeleted,
                    rename,
                    change.Path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)));
            }

            string message = commit.Message ?? string.Empty;
            int newline = message.IndexOf('\n', StringComparison.Ordinal);

            read.Add((
                commit.Author.When.ToUniversalTime(),
                commit.Sha,
                commit.Author.Email.ToLowerInvariant(),
                (newline < 0 ? message : message[..newline]).Trim(),
                files));
        }

        // Sira tarih artan, esitlikte SHA ordinal. Ana boru hattinda sira tarih + satir
        // kimligi; bu arac icinde uc esik de AYNI kurali kullandigi icin karsilastirma
        // gecerli, ama ana snapshot'la birebir ayni sira degil.
        read.Sort((left, right) =>
        {
            int date = left.Date.UtcDateTime.CompareTo(right.Date.UtcDateTime);

            return date != 0 ? date : string.CompareOrdinal(left.Sha, right.Sha);
        });

        List<CommitForMetrics> commits = [];
        Dictionary<int, string> shas = [];

        for (int index = 0; index < read.Count; index++)
        {
            commits.Add(new CommitForMetrics(index, read[index].Email, read[index].Date, read[index].Subject, read[index].Files));
            shas[index] = read[index].Sha;
        }

        return (commits, renames, shas);
    }

    private static Dictionary<string, List<(string Identity, string Path)>> Clones(string list)
    {
        Dictionary<string, List<(string, string)>> clones = new(StringComparer.Ordinal) { ["all"] = [] };

        foreach (string entry in list.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            string[] parts = entry.Split('=', 2);
            clones["all"].Add((parts[0], parts[1]));
        }

        return clones;
    }
}
