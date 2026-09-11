using LibGit2Sharp;

using Sievert.Mining;

namespace Sievert.Measure;

/// <summary>
/// Etiketleme dogrulama listesini uretir: bir duzeltme commit'i, sucladigi (ya da
/// suclamadigi) commit, hangi dosya, hangi satir araligi ve iki GitHub baglantisi.
/// E/H sutunlari BOS birakiliyor; onlari elle isaretlemek ayri bir is.
///
/// Iki tur satir var. "Etiketlenmis": duzeltmenin silinen satirlarindan blame ile
/// cikan commit. "Etiketlenmemis": ayni dosyanin blame'inde gorunen ama o duzeltmenin
/// sildigi satirlara denk gelmeyen commit - yani SZZ'nin firsati vardi, suclamadi.
/// </summary>
public static class VerificationList
{
    private const int Seed = 42;

    public static void Report(
        string repositoryPath,
        string remoteUrl,
        IReadOnlyList<SzzFix> fixes,
        int rowsPerKind)
    {
        using Repository repository = new(repositoryPath);

        Random random = new(Seed);
        List<Row> labelled = [];
        List<Row> notLabelled = [];

        // Her duzeltmeden EN FAZLA birer satir aliniyor. Ilk yazdigim hâli boyle degildi
        // ve tek bir buyuk duzeltme commit'i (Polly'de "Fix CA2000/redundant suppressions")
        // orneklemin tamamini doldurdu; ornekleme repoya degil o commit'e bakiyordu.
        foreach (SzzFix fix in fixes.OrderBy(_ => random.Next()))
        {
            if (labelled.Count >= rowsPerKind * 5 && notLabelled.Count >= rowsPerKind * 5)
            {
                break;
            }

            List<Row> fromThisFix = [];
            List<Row> missedByThisFix = [];

            Collect(repository, fix, fromThisFix, missedByThisFix);

            if (fromThisFix.Count > 0)
            {
                labelled.Add(fromThisFix[random.Next(fromThisFix.Count)]);
            }

            if (missedByThisFix.Count > 0)
            {
                notLabelled.Add(missedByThisFix[random.Next(missedByThisFix.Count)]);
            }
        }

        Write("Etiketlenmis", Pick(labelled, rowsPerKind), remoteUrl);
        Write("Etiketlenmemis (ayni dosyada, suclanmamis)", Pick(notLabelled, rowsPerKind), remoteUrl);
    }

    /// <summary>
    /// Bir duzeltme icin iki liste uretir. Onemli ayrinti: "etiketlenmemis" adaylar
    /// duzeltmenin TAMAMI islendikten sonra suzuluyor. Ilk yazdigim hâli her hunk'ta
    /// aninda karar veriyordu ve ayni commit'i hem etiketsiz hem etiketli listeye
    /// koyabiliyordu: commit bir hunk'ta silinen satira denk gelmezken baska bir hunk'ta
    /// ya da baska bir dosyada denk gelebiliyor. Bagimsiz satir kontrolu bunu Polly'de
    /// bes etiketsiz satirin ikisinde yakaladi.
    /// </summary>
    private static void Collect(Repository repository, SzzFix fix, List<Row> labelled, List<Row> notLabelled)
    {
        List<Row> candidates = [];
        HashSet<string> accusedAnywhere = new(StringComparer.Ordinal);

        Commit? commit = repository.Lookup<Commit>(fix.Sha);

        // sievert:disable SV004 Parents bellekteki bir koleksiyon, veritabani sorgusu degil
        Commit? parent = commit?.Parents.FirstOrDefault();

        if (commit is null || parent is null)
        {
            return;
        }

        using Patch patch = repository.Diff.Compare<Patch>(parent.Tree, commit.Tree);
        List<PatchEntryChanges> changes = [.. patch];

        if (changes.Count > SzzOptions.Default.MaxFilesInFix)
        {
            return;
        }

        foreach (PatchEntryChanges change in changes)
        {
            if (!change.Path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)
                || change.Status == ChangeKind.Added)
            {
                continue;
            }

            List<int> deleted = BugIntroducerFinder.DeletedLines(change.Patch, ignoreWhitespace: true);

            if (deleted.Count == 0)
            {
                continue;
            }

            string path = change.OldPath ?? change.Path;
            BlameHunkCollection hunks;

            try
            {
                hunks = repository.Blame(path, new BlameOptions { StartingAt = parent });
            }
            catch (LibGit2SharpException)
            {
                continue;
            }

            Dictionary<string, (int First, int Last, Commit Commit)> accused = new(StringComparer.Ordinal);
            HashSet<int> deletedSet = [.. deleted];

            foreach (BlameHunk hunk in hunks)
            {
                Commit culprit = hunk.FinalCommit;

                // sievert:disable SV004 Parents bellekteki bir koleksiyon, veritabani sorgusu degil
                bool isMerge = culprit.Parents.Count() > 1;

                if (isMerge || culprit.Author.When.ToUniversalTime() > fix.Date)
                {
                    continue;
                }

                bool touched = false;
                int first = int.MaxValue;
                int last = 0;

                for (int i = 0; i < hunk.LineCount; i++)
                {
                    // FinalStartLineNumber 0 tabanli, silinen satirlar 1 tabanli.
                    int line = hunk.FinalStartLineNumber + i + 1;

                    if (!deletedSet.Contains(line))
                    {
                        continue;
                    }

                    touched = true;
                    first = Math.Min(first, line);
                    last = Math.Max(last, line);
                }

                if (touched)
                {
                    accused[culprit.Sha] = (first, last, culprit);
                }
                else
                {
                    candidates.Add(new Row(
                        fix.Sha,
                        commit.MessageShort,
                        culprit.Sha,
                        culprit.MessageShort,
                        path,
                        hunk.FinalStartLineNumber + 1,
                        hunk.FinalStartLineNumber + hunk.LineCount));
                }
            }

            foreach ((int first, int last, Commit culprit) in accused.Values)
            {
                accusedAnywhere.Add(culprit.Sha);

                labelled.Add(new Row(
                    fix.Sha,
                    commit.MessageShort,
                    culprit.Sha,
                    culprit.MessageShort,
                    path,
                    first,
                    last));
            }
        }

        // Bu duzeltmenin herhangi bir yerde sucladigi commit'ler aday olamaz.
        notLabelled.AddRange(candidates.Where(row => !accusedAnywhere.Contains(row.CulpritSha)));
    }

    private static IReadOnlyList<Row> Pick(List<Row> rows, int count)
    {
        Random random = new(Seed + 1);

        return [.. rows.OrderBy(_ => random.Next()).Take(count)];
    }

    private static void Write(string title, IReadOnlyList<Row> rows, string remoteUrl)
    {
        Console.WriteLine($"### {title}");
        Console.WriteLine();
        Console.WriteLine(
            "| # | Duzeltme | Duzeltme basligi | Suclanan | Suclanan basligi | Dosya | Satir | Baglantilar "
            + "| Gercek mi (E/H) | Neden | Yontem nasil duzelmeli |");
        Console.WriteLine("|---|---|---|---|---|---|---|---|---|---|---|");

        for (int i = 0; i < rows.Count; i++)
        {
            Row row = rows[i];
            string range = row.First == row.Last
                ? row.First.ToString(System.Globalization.CultureInfo.InvariantCulture)
                : $"{row.First}-{row.Last}";

            Console.WriteLine(
                $"| {i + 1} | `{row.FixSha[..8]}` | {Clean(row.FixSubject)} | `{row.CulpritSha[..8]}` "
                + $"| {Clean(row.CulpritSubject)} | `{row.Path}` | {range} "
                + $"| [duzeltme]({CommitUrl(remoteUrl, row.FixSha)}) / [suclanan]({CommitUrl(remoteUrl, row.CulpritSha)}) "
                + "|  |  |  |");
        }

        Console.WriteLine();
    }

    private static string Clean(string subject)
    {
        string text = subject.Trim().Replace("|", "\\|", StringComparison.Ordinal);

        return text.Length > 70 ? text[..70] + "..." : text;
    }

    /// <summary>
    /// Baglanti deponun kendi uzak adresinden uretiliyor, normalize edilmis kimlikten
    /// degil: normalize etmek kucuk harfe cevirdigi icin baglantilar App-vNext yerine
    /// app-vnext gosteriyordu. GitHub ikisini de acar ama yazilan sey deponun gercek adi olmali.
    /// </summary>
    private static string CommitUrl(string remoteUrl, string sha)
    {
        string url = remoteUrl.Trim();

        if (url.EndsWith(".git", StringComparison.OrdinalIgnoreCase))
        {
            url = url[..^4];
        }

        return $"{url.TrimEnd('/')}/commit/{sha}";
    }

    private sealed record Row(
        string FixSha,
        string FixSubject,
        string CulpritSha,
        string CulpritSubject,
        string Path,
        int First,
        int Last);
}
