namespace Sievert.Data.Metrics;

/// <summary>Hesaplama ayarlari.</summary>
/// <param name="FollowRenames">
/// Ad degisiminde dosyanin gecmisi yeni yola tasinsin mi. Kapali oldugunda ad degistiren
/// bir dosya sifirdan baslamis gibi gorunur; ikisinin farki olculdu, bkz.
/// <c>docs/olcumler/asama4-ad-degisimi.md</c>.
/// </param>
public sealed record MetricOptions(bool FollowRenames)
{
    public static readonly MetricOptions Default = new(FollowRenames: true);
}

/// <summary>
/// Commit'leri TARIH SIRASINA gore isleyip her biri icin olculeri hesaplar. Durum
/// ilerleyerek birikiyor: bir commit'in metrikleri hesaplanirken durumda yalnizca ondan
/// ONCEKI commit'ler var, sonra durum bu commit'le guncelleniyor. Gelecekten bilgi
/// sizmamasinin sebebi bu sira (ADR 0013).
/// </summary>
public sealed class MetricCalculator(MetricOptions options)
{
    private readonly Dictionary<string, FileHistory> files = new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> authorCommits = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, int> authorFileTouches = new(StringComparer.Ordinal);

    /// <summary>
    /// Commit'ler TARIH SIRASINDA (eskiden yeniye) gelmeli. Akis korunuyor: gelen commit
    /// islenip birakiliyor, listede toplanmiyor.
    /// </summary>
    public IEnumerable<CommitMetrics> Compute(IEnumerable<CommitForMetrics> commits)
    {
        foreach (CommitForMetrics commit in commits)
        {
            bool isFix = FixSubject.Looks(commit.Subject);

            yield return Measure(commit, isFix);

            Remember(commit, isFix);
        }
    }

    /// <summary>Olculer yalnizca su ana kadar biriken durumdan okunuyor.</summary>
    private CommitMetrics Measure(CommitForMetrics commit, bool isFix)
    {
        int added = commit.Files.Sum(file => file.LinesAdded);
        int deleted = commit.Files.Sum(file => file.LinesDeleted);

        int priorChanges = 0;
        int priorFixes = 0;
        int experience = 0;
        int maxAge = 0;
        int minAge = int.MaxValue;
        HashSet<string> authors = new(StringComparer.OrdinalIgnoreCase);

        foreach (FileForMetrics file in commit.Files)
        {
            experience += Touches(commit.AuthorEmail, PathFor(file));

            if (History(file) is not FileHistory history)
            {
                // Ilk kez gorulen dosya: gecmisi yok, yasi sifir.
                minAge = Math.Min(minAge, 0);
                continue;
            }

            priorChanges += history.Changes;
            priorFixes += history.Fixes;
            authors.UnionWith(history.Authors);

            int age = (int)(commit.Date - history.FirstSeen).TotalDays;
            maxAge = Math.Max(maxAge, age);
            minAge = Math.Min(minAge, age);
        }

        return new CommitMetrics(
            commit.Id,
            added,
            deleted,
            commit.Files.Count,
            commit.Files.Count(file => file.IsCSharp),
            Entropy(commit.Files, added + deleted),
            commit.Files.Select(Directory).Distinct(StringComparer.Ordinal).Count(),
            commit.Files.Select(Subsystem).Distinct(StringComparer.Ordinal).Count(),
            maxAge,
            minAge == int.MaxValue ? 0 : minAge,
            priorChanges,
            priorFixes,
            authors.Count,
            authorCommits.GetValueOrDefault(commit.AuthorEmail),
            experience,
            isFix);
    }

    /// <summary>Commit islendikten SONRA durum guncelleniyor; sira bu, tersi sizinti olur.</summary>
    private void Remember(CommitForMetrics commit, bool isFix)
    {
        authorCommits[commit.AuthorEmail] = authorCommits.GetValueOrDefault(commit.AuthorEmail) + 1;

        foreach (FileForMetrics file in commit.Files)
        {
            CarryHistoryOverRename(file);

            if (!files.TryGetValue(file.Path, out FileHistory? history))
            {
                history = new FileHistory(commit.Date);
                files[file.Path] = history;
            }

            history.Changes++;
            history.Authors.Add(commit.AuthorEmail);

            if (isFix)
            {
                history.Fixes++;
            }

            string key = TouchKey(commit.AuthorEmail, file.Path);
            authorFileTouches[key] = authorFileTouches.GetValueOrDefault(key) + 1;
        }
    }

    /// <summary>
    /// Ad degisiminde eski yolun gecmisini yeni yola tasir. Kapaliyken dosya yeni bir
    /// dosya gibi baslar ve gecmisi kopar.
    /// </summary>
    private void CarryHistoryOverRename(FileForMetrics file)
    {
        if (!options.FollowRenames || !file.IsRename || file.OldPath is not string old)
        {
            return;
        }

        if (files.Remove(old, out FileHistory? history))
        {
            files[file.Path] = history;
        }
    }

    private FileHistory? History(FileForMetrics file) => files.GetValueOrDefault(PathFor(file));

    /// <summary>
    /// Olcum sirasinda dosyanin gecmisi hangi yolda aranacak. Ad degisiminde gecmis
    /// henuz tasinmadigi icin eski yola bakiliyor.
    /// </summary>
    private string PathFor(FileForMetrics file) =>
        options.FollowRenames && file.IsRename && file.OldPath is string old ? old : file.Path;

    private int Touches(string email, string path) =>
        authorFileTouches.GetValueOrDefault(TouchKey(email, path));

    private static string TouchKey(string email, string path) => email + " " + path;

    /// <summary>
    /// Shannon entropisi: -sum(p_i * log2(p_i)). p_i, o dosyanin degisen satirinin
    /// commit'teki toplam degisen satira orani. Tek dosyalik commit'te 0 cikiyor,
    /// cunku p = 1 ve log2(1) = 0.
    /// </summary>
    private static double Entropy(IReadOnlyList<FileForMetrics> files, int totalLines)
    {
        if (totalLines == 0 || files.Count < 2)
        {
            return 0.0;
        }

        double sum = 0.0;

        foreach (FileForMetrics file in files)
        {
            double share = (double)(file.LinesAdded + file.LinesDeleted) / totalLines;

            if (share > 0)
            {
                sum += share * Math.Log2(share);
            }
        }

        return -sum;
    }

    /// <summary>Yolun tamami eksi dosya adi. Kok klasordeki dosyada bos string.</summary>
    private static string Directory(FileForMetrics file)
    {
        int last = file.Path.LastIndexOf('/');

        return last < 0 ? string.Empty : file.Path[..last];
    }

    /// <summary>Yolun ILK dizin bileseni. Kok klasordeki dosyada bos string.</summary>
    private static string Subsystem(FileForMetrics file)
    {
        int first = file.Path.IndexOf('/', StringComparison.Ordinal);

        return first < 0 ? string.Empty : file.Path[..first];
    }

    private sealed class FileHistory(DateTimeOffset firstSeen)
    {
        public DateTimeOffset FirstSeen { get; } = firstSeen;

        public int Changes { get; set; }

        public int Fixes { get; set; }

        public HashSet<string> Authors { get; } = new(StringComparer.OrdinalIgnoreCase);
    }
}
