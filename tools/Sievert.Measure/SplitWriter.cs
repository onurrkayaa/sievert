using System.Globalization;

using Sievert.Modeling;

namespace Sievert.Measure;

/// <summary>
/// Dondurulmus veri kumesini okuyup zaman sirali bolme manifestini yaziyor ve
/// <c>docs/olcumler/asama5-sizinti-kontrol.md</c> icindeki kontrolleri calistiriyor.
///
/// Urunun parcasi degil. Veriye dokunmuyor: anlik goruntuyu yalnizca okuyor, hicbir
/// satiri silmiyor, hicbir degeri donusturmuyor. Bir kontrol kalirsa manifest
/// YAZILMIYOR; yarim dogrulanmis bir bolmeyle model adimina gecmek, sonraki butun
/// sayilari supheli yapardi.
/// </summary>
public static class SplitWriter
{
    /// <summary>
    /// Asama 4'te olculen ve <c>asama5-veri-kumesi.md</c> icinde yazili sayilar. Burada
    /// sabit duruyorlar ki bolme onlari dogrulasin; veriden okunup veriye karsi
    /// karsilastirilsalardi kontrol olmazlardi.
    /// </summary>
    private static readonly Expected[] Repositories =
    [
        new("github.com/app-vnext/polly", 2759, 261, 1931, 828),
        new("github.com/jellyfin/jellyfin", 22917, 4688, 16041, 6876),
        new("github.com/sharex/sharex", 8490, 1013, 5943, 2547),
    ];

    private const int ExpectedRows = 34166;

    private const int ExpectedPositives = 5962;

    public static int Write(string snapshotPath, string checksumPath, string manifestPath)
    {
        Checks checks = new();

        Console.WriteLine("== 1) Anlik goruntunun ozeti ==");
        Console.WriteLine($"kayitli ozet dosyasi: {checksumPath}");

        IReadOnlyList<SnapshotRow> rows = SnapshotReader.ReadVerified(snapshotPath, checksumPath);

        Console.WriteLine($"SHA-256: {FileChecksum.Sha256(snapshotPath)}");
        checks.Record(1, "Snapshot checksum'u kayitli degerle ayni", true);
        Console.WriteLine();

        Summary[] repositories = Summarise(rows);

        Console.WriteLine("== 2) Satir ve etiket sayilari ==");
        bool counts = ReportCounts(rows, repositories);
        checks.Record(2, "Satir sayilari ve pozitif etiketler beklenenle ayni", counts);
        Console.WriteLine();

        Console.WriteLine("== 3) Repo+SHA tekrari ==");
        int duplicates = Duplicates(rows);
        Console.WriteLine($"tekrarlanan repo+SHA: {duplicates}");
        checks.Record(3, "Anlik goruntude repo+SHA tekrari yok", duplicates == 0);
        Console.WriteLine();

        IReadOnlyList<SplitEntry> entries = TimeOrderedSplit.Apply(rows);

        Console.WriteLine("== Bolme ==");
        Split[] splits = SplitSummaries(rows, entries);
        bool sizes = ReportSplit(splits);
        Console.WriteLine();

        Console.WriteLine("== 4) Train/test anahtar cakismasi ==");
        int shared = SharedKeys(entries);
        Console.WriteLine($"iki tarafta birden gorunen repo+SHA: {shared}");
        checks.Record(4, "Train ile test ayni repo+SHA'yi paylasmiyor", shared == 0);
        Console.WriteLine();

        Console.WriteLine("== 5) Zaman sirasi ==");
        int inversions = Inversions(entries);
        Console.WriteLine($"sirasi bozuk komsu cift: {inversions}");
        checks.Record(5, "Her repoda zaman sirasi korunuyor", inversions == 0);
        Console.WriteLine();

        Console.WriteLine("== 6) Train sonu, test baslangici ==");
        bool ordered = ReportBoundary(splits);
        checks.Record(6, "max(train tarihi) <= min(test tarihi)", ordered);
        Console.WriteLine();

        Console.WriteLine("== 7-10) Oznitelik listesine sizan alan var mi ==");
        bool contract = ReportContract();
        checks.Record(7, "IsBugIntroducing aday oznitelik listesinde degil", contract);
        checks.Record(8, "LabelSource aday oznitelik listesinde degil", contract);
        checks.Record(9, "Sha, Repository, RepositoryIdentity ve AuthorDateUtc listede degil", contract);
        checks.Record(10, "BotMu bu adimda listede degil", contract);
        Console.WriteLine();

        Console.WriteLine("== 11) NULL / NaN / sonsuz ==");
        int broken = NotFinite(rows);
        Console.WriteLine($"15 oznitelikte sonlu olmayan deger: {broken}");
        Console.WriteLine("(bos ya da okunamayan alan olsaydi okuma zaten durur ve satiri yazardi)");
        checks.Record(11, "Sayisal ozniteliklerde NULL/NaN/sonsuz yok", broken == 0);
        Console.WriteLine();

        Console.WriteLine("== 12) Normalizasyon ya da donusum ==");
        int changed = Transformed(snapshotPath, rows);
        Console.WriteLine($"dosyadaki ham degerden farkli oznitelik degeri: {changed}");
        checks.Record(12, "Normalizasyon ya da donusum uygulanmamis", changed == 0);
        Console.WriteLine();

        Console.WriteLine("== Sag sansur / etiket olgunlugu ==");
        ReportMaturity(rows, entries);
        Console.WriteLine();

        checks.Record(13, "Bolme boyutlari beklenenle ayni", sizes);

        Console.WriteLine("== Kontroller ==");
        bool all = checks.Report();
        Console.WriteLine();

        if (!all)
        {
            Console.Error.WriteLine("Bir kontrol KALDI; manifest yazilmadi.");

            return 2;
        }

        SplitManifest.Write(manifestPath, entries);

        string checksumOut = Path.ChangeExtension(manifestPath, ".sha256");
        File.WriteAllText(checksumOut, FileChecksum.Line(manifestPath));

        Console.WriteLine($"manifest: {manifestPath}");
        Console.WriteLine($"boyut: {new FileInfo(manifestPath).Length} bayt, satir: {entries.Count}");
        Console.WriteLine($"SHA-256: {FileChecksum.Sha256(manifestPath)}  ({checksumOut})");

        return 0;
    }

    private static Summary[] Summarise(IReadOnlyList<SnapshotRow> rows)
    {
        Dictionary<string, Summary> byIdentity = new(StringComparer.Ordinal);

        foreach (SnapshotRow row in rows)
        {
            if (!byIdentity.TryGetValue(row.RepositoryIdentity, out Summary? summary))
            {
                summary = new Summary(row.RepositoryIdentity);
                byIdentity[row.RepositoryIdentity] = summary;
            }

            summary.Add(row);
        }

        return [.. byIdentity.Values.OrderBy(summary => summary.Identity, StringComparer.Ordinal)];
    }

    private static bool ReportCounts(IReadOnlyList<SnapshotRow> rows, Summary[] repositories)
    {
        int positives = 0;

        foreach (SnapshotRow row in rows)
        {
            if (row.IsBugIntroducing)
            {
                positives++;
            }
        }

        bool same = rows.Count == ExpectedRows && positives == ExpectedPositives;

        Console.WriteLine($"toplam satir: {rows.Count} (beklenen {ExpectedRows})");
        Console.WriteLine($"toplam pozitif: {positives} (beklenen {ExpectedPositives})");
        Console.WriteLine($"farkli RepositoryIdentity: {repositories.Length} (beklenen {Repositories.Length})");

        same = same && repositories.Length == Repositories.Length;

        foreach (Summary summary in repositories)
        {
            Expected? expected = Array.Find(Repositories, item => item.Identity == summary.Identity);

            if (expected is null)
            {
                Console.WriteLine($"  {summary.Identity}: BEKLENMEYEN DEPO");
                same = false;
                continue;
            }

            bool match = summary.Rows == expected.Rows && summary.Positives == expected.Positives;
            same = same && match;

            Console.WriteLine(
                $"  {summary.Identity}: {summary.Positives} / {summary.Rows} "
                + $"(beklenen {expected.Positives} / {expected.Rows}) {(match ? "ayni" : "FARKLI")}");
            Console.WriteLine($"    tarih: {Date(summary.First)} - {Date(summary.Last)}");
        }

        return same;
    }

    private static int Duplicates(IReadOnlyList<SnapshotRow> rows)
    {
        HashSet<string> seen = new(StringComparer.Ordinal);
        int duplicates = 0;

        foreach (SnapshotRow row in rows)
        {
            if (!seen.Add(row.RepositoryIdentity + " " + row.Sha))
            {
                duplicates++;
            }
        }

        return duplicates;
    }

    private static Split[] SplitSummaries(IReadOnlyList<SnapshotRow> rows, IReadOnlyList<SplitEntry> entries)
    {
        Dictionary<string, bool> positive = new(StringComparer.Ordinal);

        foreach (SnapshotRow row in rows)
        {
            positive[row.RepositoryIdentity + " " + row.Sha] = row.IsBugIntroducing;
        }

        Dictionary<string, Split> byIdentity = new(StringComparer.Ordinal);

        foreach (SplitEntry entry in entries)
        {
            if (!byIdentity.TryGetValue(entry.RepositoryIdentity, out Split? split))
            {
                split = new Split(entry.RepositoryIdentity);
                byIdentity[entry.RepositoryIdentity] = split;
            }

            split.Add(entry, positive[entry.RepositoryIdentity + " " + entry.Sha]);
        }

        return [.. byIdentity.Values.OrderBy(split => split.Identity, StringComparer.Ordinal)];
    }

    private static bool ReportSplit(Split[] splits)
    {
        bool same = true;
        int train = 0;
        int test = 0;

        foreach (Split split in splits)
        {
            Expected? expected = Array.Find(Repositories, item => item.Identity == split.Identity);
            bool match = expected is not null && split.TrainCount == expected.Train && split.TestCount == expected.Test;
            same = same && match;
            train += split.TrainCount;
            test += split.TestCount;

            Console.WriteLine($"{split.Identity}");
            Console.WriteLine(
                $"  train: {split.TrainCount} satir (beklenen {expected?.Train}), "
                + $"pozitif {split.TrainPositives} / {split.TrainCount}");
            Console.WriteLine(
                $"  test : {split.TestCount} satir (beklenen {expected?.Test}), "
                + $"pozitif {split.TestPositives} / {split.TestCount}   {(match ? "ayni" : "FARKLI")}");
            Console.WriteLine($"  train tarih: {Date(split.TrainFirst)} - {Date(split.TrainLast)}");
            Console.WriteLine($"  test  tarih: {Date(split.TestFirst)} - {Date(split.TestLast)}");
            Console.WriteLine($"  son train sha : {split.LastTrainSha}");
            Console.WriteLine($"  ilk test  sha : {split.FirstTestSha}");
        }

        Console.WriteLine($"toplam train {train}, test {test} (beklenen 23915 / 10251)");

        return same && train == 23915 && test == 10251;
    }

    private static int SharedKeys(IReadOnlyList<SplitEntry> entries)
    {
        HashSet<string> train = new(StringComparer.Ordinal);
        HashSet<string> test = new(StringComparer.Ordinal);

        foreach (SplitEntry entry in entries)
        {
            HashSet<string> side = entry.Split == SplitEntry.Train ? train : test;
            side.Add(entry.RepositoryIdentity + " " + entry.Sha);
        }

        int shared = 0;

        foreach (string key in train)
        {
            if (test.Contains(key))
            {
                shared++;
            }
        }

        return shared;
    }

    /// <summary>Manifestte ayni deponun ardisik iki satiri tarihte geriye gidiyor mu.</summary>
    private static int Inversions(IReadOnlyList<SplitEntry> entries)
    {
        int inversions = 0;

        for (int index = 1; index < entries.Count; index++)
        {
            SplitEntry previous = entries[index - 1];
            SplitEntry current = entries[index];

            if (string.Equals(previous.RepositoryIdentity, current.RepositoryIdentity, StringComparison.Ordinal)
                && current.AuthorDateUtc < previous.AuthorDateUtc)
            {
                inversions++;
            }
        }

        return inversions;
    }

    private static bool ReportBoundary(Split[] splits)
    {
        bool ordered = true;

        foreach (Split split in splits)
        {
            bool fine = split.TrainLast <= split.TestFirst;
            ordered = ordered && fine;

            Console.WriteLine(
                $"{split.Identity}: {Date(split.TrainLast)} <= {Date(split.TestFirst)} "
                + $"{(fine ? "GECTI" : "KALDI")}");
        }

        return ordered;
    }

    private static bool ReportContract()
    {
        Console.WriteLine($"aday oznitelik: {ModelFeatures.Candidates.Count}");
        Console.WriteLine($"  {string.Join(", ", ModelFeatures.Candidates)}");

        try
        {
            ModelFeatures.EnsureNoExcluded(ModelFeatures.Candidates);
        }
        catch (InvalidOperationException error)
        {
            Console.WriteLine($"  KALDI: {error.Message}");

            return false;
        }

        foreach (KeyValuePair<string, string> excluded in ModelFeatures.Excluded)
        {
            Console.WriteLine($"  disarida: {excluded.Key} - {excluded.Value}");
        }

        return true;
    }

    private static int NotFinite(IReadOnlyList<SnapshotRow> rows)
    {
        int broken = 0;

        foreach (SnapshotRow row in rows)
        {
            IReadOnlyList<double> values = ModelFeatures.Values(row);

            for (int index = 0; index < values.Count; index++)
            {
                if (!double.IsFinite(values[index]))
                {
                    broken++;
                }
            }
        }

        return broken;
    }

    /// <summary>
    /// Oznitelik degerleri dosyadaki ham metinle birebir ayni mi. Normalizasyon, log
    /// donusumu ya da kirpma uygulanmis olsa burada fark cikardi; bu adimda hicbiri
    /// yapilmiyor ve kontrol bunu gosteriyor.
    /// </summary>
    private static int Transformed(string snapshotPath, IReadOnlyList<SnapshotRow> rows)
    {
        int[] columns = new int[ModelFeatures.Candidates.Count];

        for (int index = 0; index < columns.Length; index++)
        {
            columns[index] = Array.IndexOf(SnapshotReader.Columns, ModelFeatures.Candidates[index]);
        }

        using StreamReader reader = new(snapshotPath);
        reader.ReadLine();

        int changed = 0;
        int number = 0;

        while (reader.ReadLine() is string line)
        {
            if (line.Length == 0)
            {
                continue;
            }

            string[] fields = line.Split(',');
            IReadOnlyList<double> values = ModelFeatures.Values(rows[number]);

            for (int index = 0; index < columns.Length; index++)
            {
                double raw = double.Parse(fields[columns[index]], CultureInfo.InvariantCulture);

                if (raw != values[index])
                {
                    changed++;
                }
            }

            number++;
        }

        return changed;
    }

    /// <summary>
    /// Sag sansur: bir commit ancak kendisinden sonraki bir duzeltme onu suclarsa pozitif
    /// olabiliyor. Deponun sonuna yakin test commit'lerinin gelecegi daha az gozlendi.
    /// Burada yalnizca OLCULUYOR; ana bolme degistirilmiyor, satir silinmiyor.
    /// </summary>
    private static void ReportMaturity(IReadOnlyList<SnapshotRow> rows, IReadOnlyList<SplitEntry> entries)
    {
        Dictionary<string, bool> positive = new(StringComparer.Ordinal);
        Dictionary<string, DateTimeOffset> last = new(StringComparer.Ordinal);

        foreach (SnapshotRow row in rows)
        {
            positive[row.RepositoryIdentity + " " + row.Sha] = row.IsBugIntroducing;

            if (!last.TryGetValue(row.RepositoryIdentity, out DateTimeOffset current) || row.AuthorDateUtc > current)
            {
                last[row.RepositoryIdentity] = row.AuthorDateUtc;
            }
        }

        Dictionary<string, List<(int Days, bool Positive)>> maturity = new(StringComparer.Ordinal);

        foreach (SplitEntry entry in entries)
        {
            if (entry.Split != SplitEntry.Test)
            {
                continue;
            }

            if (!maturity.TryGetValue(entry.RepositoryIdentity, out List<(int, bool)>? list))
            {
                list = [];
                maturity[entry.RepositoryIdentity] = list;
            }

            int days = (int)(last[entry.RepositoryIdentity] - entry.AuthorDateUtc).TotalDays;
            list.Add((days, positive[entry.RepositoryIdentity + " " + entry.Sha]));
        }

        foreach (string identity in maturity.Keys.OrderBy(key => key, StringComparer.Ordinal))
        {
            List<(int Days, bool Positive)> list = maturity[identity];
            List<int> days = [.. list.Select(item => item.Days)];
            days.Sort();

            Console.WriteLine($"{identity} (test {list.Count} satir)");
            Console.WriteLine(
                $"  MaturityDays min {days[0]}, medyan {Quantile(days, 0.50)}, "
                + $"p95 {Quantile(days, 0.95)}, max {days[^1]}");

            foreach (int limit in (int[])[30, 90, 180])
            {
                int count = 0;
                int positives = 0;

                foreach ((int Days, bool Positive) item in list)
                {
                    if (item.Days >= limit)
                    {
                        continue;
                    }

                    count++;

                    if (item.Positive)
                    {
                        positives++;
                    }
                }

                Console.WriteLine($"  {limit} gunden az olgun: {count} satir, pozitif {positives} / {count}");
            }
        }
    }

    /// <summary>En yakin sira yontemi; <c>MetricDistribution</c> ile ayni kural.</summary>
    private static int Quantile(List<int> sorted, double fraction)
    {
        int index = (int)Math.Ceiling(fraction * sorted.Count) - 1;

        return sorted[Math.Clamp(index, 0, sorted.Count - 1)];
    }

    private static string Date(DateTimeOffset value) =>
        value.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);

    private sealed record Expected(string Identity, int Rows, int Positives, int Train, int Test);

    private sealed class Checks
    {
        private readonly List<(int Number, string Name, bool Passed)> results = [];

        public void Record(int number, string name, bool passed) => results.Add((number, name, passed));

        public bool Report()
        {
            bool all = true;

            foreach ((int number, string name, bool passed) in results)
            {
                all = all && passed;
                Console.WriteLine($"  {number,2}) {name}: {(passed ? "GECTI" : "KALDI")}");
            }

            return all;
        }
    }

    private sealed class Summary(string identity)
    {
        public string Identity { get; } = identity;

        public int Rows { get; private set; }

        public int Positives { get; private set; }

        public DateTimeOffset First { get; private set; } = DateTimeOffset.MaxValue;

        public DateTimeOffset Last { get; private set; } = DateTimeOffset.MinValue;

        public void Add(SnapshotRow row)
        {
            Rows++;

            if (row.IsBugIntroducing)
            {
                Positives++;
            }

            if (row.AuthorDateUtc < First)
            {
                First = row.AuthorDateUtc;
            }

            if (row.AuthorDateUtc > Last)
            {
                Last = row.AuthorDateUtc;
            }
        }
    }

    private sealed class Split(string identity)
    {
        public string Identity { get; } = identity;

        public int TrainCount { get; private set; }

        public int TestCount { get; private set; }

        public int TrainPositives { get; private set; }

        public int TestPositives { get; private set; }

        public DateTimeOffset TrainFirst { get; private set; } = DateTimeOffset.MaxValue;

        public DateTimeOffset TrainLast { get; private set; } = DateTimeOffset.MinValue;

        public DateTimeOffset TestFirst { get; private set; } = DateTimeOffset.MaxValue;

        public DateTimeOffset TestLast { get; private set; } = DateTimeOffset.MinValue;

        public string LastTrainSha { get; private set; } = string.Empty;

        public string FirstTestSha { get; private set; } = string.Empty;

        public void Add(SplitEntry entry, bool positive)
        {
            if (entry.Split == SplitEntry.Train)
            {
                TrainCount++;

                if (positive)
                {
                    TrainPositives++;
                }

                if (entry.AuthorDateUtc < TrainFirst)
                {
                    TrainFirst = entry.AuthorDateUtc;
                }

                if (entry.AuthorDateUtc >= TrainLast)
                {
                    TrainLast = entry.AuthorDateUtc;
                }

                // Manifest sirada geldigi icin son yazilan satir sinirin train tarafi.
                LastTrainSha = entry.Sha;

                return;
            }

            TestCount++;

            if (positive)
            {
                TestPositives++;
            }

            if (entry.AuthorDateUtc < TestFirst)
            {
                TestFirst = entry.AuthorDateUtc;
            }

            if (entry.AuthorDateUtc > TestLast)
            {
                TestLast = entry.AuthorDateUtc;
            }

            if (FirstTestSha.Length == 0)
            {
                FirstTestSha = entry.Sha;
            }
        }
    }
}
