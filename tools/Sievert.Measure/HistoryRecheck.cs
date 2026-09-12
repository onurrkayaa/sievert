using Microsoft.EntityFrameworkCore;

using Sievert.Data;
using Sievert.Modeling;

namespace Sievert.Measure;

/// <summary>
/// Tarihsel ozniteliklerin bagimsiz kontrolu: bir commit'in yedi gecmis olcusu, yalnizca
/// o commit'ten ONCEKI satirlardan yeniden hesaplandiginda anlik goruntudeki degerle ayni
/// mi. Fark cikarsa oznitelik gelecekten bilgi tasiyor demektir.
///
/// Hesabi <c>MetricCalculator</c>'a yaptirmiyor, kendi durumunu kendisi biriktiriyor.
/// Sebebi Asama 4'un dersi: ureticinin kendi ciktisini dogrulamasi yalnizca
/// deterministik oldugunu gosterir. Ondan aldigi tek sey ham veri ve commit sirasi.
///
/// Onceki commit'lerin `IsFix` degeri bile anlik goruntuden okunuyor, urun kodundan
/// degil; boylece `PriorFixes` kontrolu heuristigin kendisine bagli kalmiyor.
/// </summary>
public static class HistoryRecheck
{
    /// <summary>Orneklem tohumu. Sabit ve yazili; secim tekrarlanabilir olsun diye.</summary>
    public const int Seed = 42;

    /// <summary>Her repodan kac train ve kac test commit'i secilecek.</summary>
    public const int PerSide = 10;

    private static readonly string[] Metrics =
    [
        "PriorFixes",
        "PriorChanges",
        "AuthorCommitCount",
        "AuthorFileExperience",
        "DistinctAuthorsOnFiles",
        "MaxFileAgeDays",
        "MinFileAgeDays",
    ];

    public static int Report(SievertContext context, string snapshotPath, string checksumPath)
    {
        IReadOnlyList<SnapshotRow> rows = SnapshotReader.ReadVerified(snapshotPath, checksumPath);
        IReadOnlyList<SplitEntry> entries = TimeOrderedSplit.Apply(rows);

        Console.WriteLine($"anlik goruntu: {snapshotPath}");
        Console.WriteLine($"SHA-256: {FileChecksum.Sha256(snapshotPath)}");
        Console.WriteLine($"tohum: {Seed}, repo basina {PerSide} train + {PerSide} test");
        Console.WriteLine();

        Dictionary<string, SnapshotRow> stored = new(StringComparer.Ordinal);

        foreach (SnapshotRow row in rows)
        {
            stored[Key(row.RepositoryIdentity, row.Sha)] = row;
        }

        Tally[] tallies = [.. Metrics.Select(metric => new Tally(metric))];
        int missing = 0;

        // Depo satirlari donguden once tek sorguyla aliniyor; ucu de ayni tabloda.
        Dictionary<string, int> repositories = context.Repositories
            .AsNoTracking()
            .ToDictionary(row => row.Identity, row => row.Id, StringComparer.Ordinal);

        foreach (string identity in Identities(entries))
        {
            Console.WriteLine(identity);

            if (!repositories.TryGetValue(identity, out int repositoryId))
            {
                Console.Error.WriteLine($"  veritabaninda boyle bir depo yok: {identity}");

                return 2;
            }

            HashSet<string> sample = Sample(entries, identity);
            Console.WriteLine($"  secilen commit: {sample.Count}");

            missing += Compare(context, repositoryId, identity, sample, stored, tallies);
        }

        Console.WriteLine();
        Console.WriteLine("| Olcu | Karsilastirilan | Eslesen | Farkli | En buyuk mutlak fark |");
        Console.WriteLine("|---|---|---|---|---|");

        bool same = missing == 0;

        foreach (Tally tally in tallies)
        {
            same = same && tally.Different == 0;
            Console.WriteLine($"| {tally.Metric} | {tally.Compared} | {tally.Same} | {tally.Different} | {tally.Largest} |");
        }

        Console.WriteLine();

        if (missing > 0)
        {
            Console.Error.WriteLine($"Anlik goruntude bulunamayan commit: {missing}");
        }

        Console.WriteLine(same
            ? "Tarihsel oznitelik kontrolu: GECTI"
            : "Tarihsel oznitelik kontrolu: KALDI");

        return same ? 0 : 2;
    }

    private static string[] Identities(IReadOnlyList<SplitEntry> entries)
    {
        HashSet<string> identities = new(StringComparer.Ordinal);

        foreach (SplitEntry entry in entries)
        {
            identities.Add(entry.RepositoryIdentity);
        }

        return [.. identities.OrderBy(identity => identity, StringComparer.Ordinal)];
    }

    /// <summary>
    /// Secim: once o deponun train (sonra test) satirlari manifest sirasinda diziliyor,
    /// sonra sabit tohumlu bir karistirmayla ilk <see cref="PerSide"/> tanesi aliniyor.
    /// Girdi sirasi deterministik oldugu icin secim de deterministik.
    /// </summary>
    private static HashSet<string> Sample(IReadOnlyList<SplitEntry> entries, string identity)
    {
        HashSet<string> sample = new(StringComparer.Ordinal);

        foreach (string side in (string[])[SplitEntry.Train, SplitEntry.Test])
        {
            List<string> shas = [];

            foreach (SplitEntry entry in entries)
            {
                if (string.Equals(entry.RepositoryIdentity, identity, StringComparison.Ordinal)
                    && string.Equals(entry.Split, side, StringComparison.Ordinal))
                {
                    shas.Add(entry.Sha);
                }
            }

            Random random = new(Seed);

            for (int index = shas.Count - 1; index > 0; index--)
            {
                int swap = random.Next(index + 1);
                (shas[index], shas[swap]) = (shas[swap], shas[index]);
            }

            for (int index = 0; index < PerSide && index < shas.Count; index++)
            {
                sample.Add(shas[index]);
            }
        }

        return sample;
    }

    /// <summary>
    /// Deponun butun commit'lerini tarih sirasinda tek gecisle yuruyor. Bir commit
    /// orneklemdeyse olculeri o ANDAKI durumdan okunuyor; durumda yalnizca daha onceki
    /// commit'ler var, cunku guncelleme okumadan SONRA yapiliyor.
    /// </summary>
    private static int Compare(
        SievertContext context,
        int repositoryId,
        string identity,
        HashSet<string> sample,
        Dictionary<string, SnapshotRow> stored,
        Tally[] tallies)
    {
        List<Header> commits =
        [
            .. context.Commits
                .AsNoTracking()
                .Where(row => row.RepositoryId == repositoryId)
                .OrderBy(row => row.AuthorDateUtc)
                .ThenBy(row => row.Id)
                .Select(row => new Header(row.Id, row.Sha, row.AuthorEmail, row.AuthorDateUtc))
        ];

        ILookup<int, Change> changes = context.CommitFiles
            .AsNoTracking()
            .Where(row => row.Commit!.RepositoryId == repositoryId)
            .Select(row => new Change(row.CommitId, row.Path, row.OldPath, row.ChangeKind))
            .ToLookup(change => change.CommitId);

        Dictionary<string, History> files = new(StringComparer.Ordinal);
        Dictionary<string, int> authorCommits = new(StringComparer.OrdinalIgnoreCase);
        Dictionary<string, int> touches = new(StringComparer.Ordinal);
        int missing = 0;

        foreach (Header commit in commits)
        {
            Change[] paths = [.. changes[commit.Id]];

            if (sample.Contains(commit.Sha))
            {
                if (stored.TryGetValue(Key(identity, commit.Sha), out SnapshotRow? row))
                {
                    Check(commit, paths, files, authorCommits, touches, row, tallies);
                }
                else
                {
                    missing++;
                }
            }

            Remember(commit, paths, files, authorCommits, touches, stored, identity);
        }

        return missing;
    }

    private static void Check(
        Header commit,
        Change[] paths,
        Dictionary<string, History> files,
        Dictionary<string, int> authorCommits,
        Dictionary<string, int> touches,
        SnapshotRow row,
        Tally[] tallies)
    {
        int priorChanges = 0;
        int priorFixes = 0;
        int experience = 0;
        int maxAge = 0;
        int minAge = int.MaxValue;
        HashSet<string> authors = new(StringComparer.OrdinalIgnoreCase);

        foreach (Change change in paths)
        {
            string lookup = Lookup(change);
            experience += touches.GetValueOrDefault(commit.Email + " " + lookup);

            if (!files.TryGetValue(lookup, out History? history))
            {
                minAge = 0;
                continue;
            }

            priorChanges += history.Changes;
            priorFixes += history.Fixes;
            authors.UnionWith(history.Authors);

            int age = (int)(commit.Date - history.FirstSeen).TotalDays;
            maxAge = Math.Max(maxAge, age);
            minAge = Math.Min(minAge, age);
        }

        int[] fresh =
        [
            priorFixes,
            priorChanges,
            authorCommits.GetValueOrDefault(commit.Email),
            experience,
            authors.Count,
            maxAge,
            minAge == int.MaxValue ? 0 : minAge,
        ];

        int[] recorded =
        [
            row.PriorFixes,
            row.PriorChanges,
            row.AuthorCommitCount,
            row.AuthorFileExperience,
            row.DistinctAuthorsOnFiles,
            row.MaxFileAgeDays,
            row.MinFileAgeDays,
        ];

        for (int index = 0; index < tallies.Length; index++)
        {
            tallies[index].Add(commit.Sha, recorded[index], fresh[index]);
        }
    }

    private static void Remember(
        Header commit,
        Change[] paths,
        Dictionary<string, History> files,
        Dictionary<string, int> authorCommits,
        Dictionary<string, int> touches,
        Dictionary<string, SnapshotRow> stored,
        string identity)
    {
        authorCommits[commit.Email] = authorCommits.GetValueOrDefault(commit.Email) + 1;

        // IsFix anlik goruntuden geliyor; heuristik burada yeniden uygulanmiyor.
        bool isFix = stored.TryGetValue(Key(identity, commit.Sha), out SnapshotRow? row) && row.IsFix;

        foreach (Change change in paths)
        {
            if (change.IsRename && change.OldPath is string old && files.Remove(old, out History? carried))
            {
                files[change.Path] = carried;
            }

            if (!files.TryGetValue(change.Path, out History? history))
            {
                history = new History(commit.Date);
                files[change.Path] = history;
            }

            history.Changes++;
            history.Authors.Add(commit.Email);

            if (isFix)
            {
                history.Fixes++;
            }

            string key = commit.Email + " " + change.Path;
            touches[key] = touches.GetValueOrDefault(key) + 1;
        }
    }

    /// <summary>Ad degisiminde gecmis henuz tasinmadigi icin olcum eski yola bakiyor.</summary>
    private static string Lookup(Change change) =>
        change.IsRename && change.OldPath is string old ? old : change.Path;

    private static string Key(string identity, string sha) => identity + " " + sha;

    private sealed record Header(int Id, string Sha, string Email, DateTimeOffset Date);

    private sealed record Change(int CommitId, string Path, string? OldPath, string ChangeKind)
    {
        public bool IsRename => string.Equals(ChangeKind, "renamed", StringComparison.Ordinal);
    }

    private sealed class History(DateTimeOffset firstSeen)
    {
        public DateTimeOffset FirstSeen { get; } = firstSeen;

        public int Changes { get; set; }

        public int Fixes { get; set; }

        public HashSet<string> Authors { get; } = new(StringComparer.OrdinalIgnoreCase);
    }

    private sealed class Tally(string metric)
    {
        public string Metric { get; } = metric;

        public int Compared { get; private set; }

        public int Same { get; private set; }

        public int Different { get; private set; }

        public int Largest { get; private set; }

        public void Add(string sha, int recorded, int fresh)
        {
            Compared++;

            if (recorded == fresh)
            {
                Same++;

                return;
            }

            Different++;
            Largest = Math.Max(Largest, Math.Abs(recorded - fresh));
            Console.WriteLine($"  fark: {Metric} {sha} kayitli {recorded}, yeniden hesaplanan {fresh}");
        }
    }
}
