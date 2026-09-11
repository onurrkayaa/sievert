using LibGit2Sharp;

namespace Sievert.Mining;

/// <summary>SZZ girdisi: bir duzeltme commit'i.</summary>
/// <param name="Sha">Commit hash'i.</param>
/// <param name="Date">Yazar tarihi, UTC. Zaman tutarliligi kontrolu bununla yapiliyor.</param>
public sealed record SzzFix(string Sha, DateTimeOffset Date);

/// <summary>SZZ ayarlari. Degerlerin gerekcesi ADR 0014'te.</summary>
/// <param name="MaxFilesInFix">
/// Bundan FAZLA dosyaya dokunan duzeltmeler atlaniyor. Buyuk yeniden duzenlemeler,
/// hatayla ilgisi olmayan yuzlerce satiri da degistirdigi icin gurultu uretiyor.
/// </param>
/// <param name="IgnoreWhitespace">
/// Sadece bosluk/girinti degisen satirlar suclanmasin mi. Acik oldugunda, silinen bir
/// satirin kirpilmis hâli ayni hunk'ta eklenen satirlardan birinde varsa o satir
/// atlaniyor.
/// </param>
public sealed record SzzOptions(int MaxFilesInFix = 50, bool IgnoreWhitespace = true)
{
    public static readonly SzzOptions Default = new();
}

/// <summary>SZZ ciktisi.</summary>
/// <param name="BlamedShas">Hata getirdigi dusunulen commit'ler.</param>
/// <param name="SkippedLargeFixes">Dosya sayisi sinirini asip atlanan duzeltme sayisi.</param>
/// <param name="DroppedByTime">Duzeltmeden sonra yazilmis gorundugu icin atilan etiket sayisi.</param>
/// <param name="DroppedMerges">Birlestirme commit'i oldugu icin suclanmayan satir sayisi.</param>
/// <param name="BlamedLines">Blame'e sorulan satir sayisi.</param>
/// <param name="ProcessedFixes">Gercekten islenen duzeltme sayisi.</param>
/// <param name="FixesWithCSharpChange">Icinde degisen .cs dosyasi olan duzeltme sayisi.</param>
/// <param name="FixesThatBlamed">En az bir commit'i suclayabilen duzeltme sayisi.</param>
public sealed record SzzOutcome(
    IReadOnlyList<string> BlamedShas,
    int SkippedLargeFixes,
    int DroppedByTime,
    int DroppedMerges,
    int BlamedLines,
    int ProcessedFixes,
    int FixesWithCSharpChange,
    int FixesThatBlamed);

/// <summary>
/// SZZ: bir duzeltme commit'inin degistirdigi satirlari en son kimin yazdigina bakip o
/// commit'i "hata getiren" diye etiketler. Yontemin tamami ve kusurlari ADR 0014'te.
/// </summary>
public sealed class BugIntroducerFinder
{
    private static readonly LibGit2Sharp.CompareOptions Compare = new()
    {
        Similarity = new SimilarityOptions
        {
            RenameDetectionMode = RenameDetectionMode.Renames,
            RenameThreshold = RepositoryMiner.RenameSimilarityThreshold,
        },
    };

    public SzzOutcome Find(string repositoryPath, IReadOnlyList<SzzFix> fixes, SzzOptions options)
    {
        using Repository repository = new(repositoryPath);

        HashSet<string> blamed = new(StringComparer.Ordinal);
        int skippedLarge = 0;
        int droppedByTime = 0;
        int droppedMerges = 0;
        int blamedLines = 0;
        int processed = 0;
        int withCSharp = 0;
        int thatBlamed = 0;

        foreach (SzzFix fix in fixes)
        {
            Commit? commit = repository.Lookup<Commit>(fix.Sha);

            // sievert:disable SV004 Parents bellekteki bir koleksiyon, veritabani sorgusu degil
            Commit? parent = commit?.Parents.FirstOrDefault();

            if (commit is null || parent is null)
            {
                // Ilk commit'in ebeveyni yok; suclanacak bir gecmis de yok.
                continue;
            }

            using Patch patch = repository.Diff.Compare<Patch>(parent.Tree, commit.Tree, Compare);
            List<PatchEntryChanges> changes = [.. patch];

            if (changes.Count > options.MaxFilesInFix)
            {
                skippedLarge++;
                continue;
            }

            processed++;
            bool touchedCSharp = false;
            int accusations = 0;

            foreach (PatchEntryChanges change in changes)
            {
                if (!IsCSharp(change.Path) || change.Status == ChangeKind.Added)
                {
                    continue;
                }

                touchedCSharp = true;
                string path = change.OldPath ?? change.Path;
                List<int> lines = DeletedLines(change.Patch, options.IgnoreWhitespace);

                if (lines.Count == 0)
                {
                    continue;
                }

                blamedLines += lines.Count;
                accusations += Accuse(repository, parent, path, lines, fix, blamed, ref droppedByTime, ref droppedMerges);
            }

            if (touchedCSharp)
            {
                withCSharp++;
            }

            // Sayac, kume buyudu mu diye degil gercekten birini sucladi mi diye bakiyor:
            // baska bir duzeltmenin daha once sucladigi bir commit'i suclamak da suclamaktir.
            if (accusations > 0)
            {
                thatBlamed++;
            }
        }

        return new SzzOutcome(
            [.. blamed],
            skippedLarge,
            droppedByTime,
            droppedMerges,
            blamedLines,
            processed,
            withCSharp,
            thatBlamed);
    }

    /// <summary>
    /// Verilen satirlari ebeveyn surumunde blame edip suclulari toplar. Blame dosya basina
    /// bir kez calistiriliyor, satir basina degil; blame pahali bir is.
    /// </summary>
    private static int Accuse(
        Repository repository,
        Commit parent,
        string path,
        List<int> lines,
        SzzFix fix,
        HashSet<string> blamed,
        ref int droppedByTime,
        ref int droppedMerges)
    {
        BlameHunkCollection hunks;

        try
        {
            hunks = repository.Blame(path, new BlameOptions { StartingAt = parent });
        }
        catch (LibGit2SharpException)
        {
            // Dosya o surumde yoksa ya da blame edilemiyorsa suclayacak kimse yok.
            return 0;
        }

        int accused = 0;

        foreach (int line in lines)
        {
            if (HunkFor(hunks, line) is not BlameHunk hunk)
            {
                continue;
            }

            Commit culprit = hunk.FinalCommit;

            // sievert:disable SV004 Parents bellekteki bir koleksiyon, veritabani sorgusu degil
            if (culprit.Parents.Count() > 1)
            {
                // Birlestirme commit'i suclanmiyor: kendi diff'i yok, getirdigi satirlar
                // baska bir dalda yazilmis.
                droppedMerges++;
                continue;
            }

            if (culprit.Author.When.ToUniversalTime() > fix.Date)
            {
                // Suclanan commit duzeltmeden sonra yazilmis gorunuyor. Yazar tarihi
                // serbestce verilebildigi icin bu mumkun ve boyle bir etiket anlamsiz.
                droppedByTime++;
                continue;
            }

            blamed.Add(culprit.Sha);
            accused++;
        }

        return accused;
    }

    /// <summary>
    /// Verilen 1 TABANLI satiri iceren blame hunk'i. LibGit2Sharp'in
    /// <c>FinalStartLineNumber</c> degeri 0 TABANLI; yamadan cikardigimiz satir numaralari
    /// ise 1 tabanli. Ilk yazdigim hâli ikisini dogrudan karsilastiriyordu ve bir satir
    /// kaymayla yanlis commit'i sucluyordu. Olcumu ADR 0014'te: duz <c>git blame</c> ile
    /// uyum %68,7'den %97,5'e cikti.
    /// </summary>
    private static BlameHunk? HunkFor(BlameHunkCollection hunks, int line)
    {
        int zeroBased = line - 1;

        foreach (BlameHunk hunk in hunks)
        {
            if (zeroBased >= hunk.FinalStartLineNumber && zeroBased < hunk.FinalStartLineNumber + hunk.LineCount)
            {
                return hunk;
            }
        }

        return null;
    }

    /// <summary>
    /// Yamadan, ESKI surumdeki silinen satir numaralarini cikarir. Hunk basligi
    /// <c>@@ -a,b +c,d @@</c> biciminde; eski taraftaki sayaci oradan baslatip
    /// <c>-</c> ve bosluk satirlarinda ilerletiyoruz.
    ///
    /// <paramref name="ignoreWhitespace"/> acikken iki tur satir atlaniyor: tamamen bos
    /// olanlar ve kirpilmis hâli ayni hunk'ta eklenen satirlardan birinde bulunanlar.
    /// Ikincisi girinti degisikligi demek, yani satirin icerigi ayni kalmis.
    /// </summary>
    public static List<int> DeletedLines(string patchText, bool ignoreWhitespace)
    {
        List<int> deleted = [];
        int oldLine = 0;
        List<string> hunkDeleted = [];
        List<string> hunkAdded = [];
        List<int> hunkNumbers = [];

        foreach (string line in patchText.Split('\n'))
        {
            if (line.StartsWith("@@", StringComparison.Ordinal))
            {
                Flush(deleted, hunkDeleted, hunkAdded, hunkNumbers, ignoreWhitespace);
                oldLine = StartOfOldSide(line);
                continue;
            }

            if (oldLine == 0)
            {
                continue;
            }

            if (line.StartsWith('-'))
            {
                hunkDeleted.Add(line[1..]);
                hunkNumbers.Add(oldLine);
                oldLine++;
                continue;
            }

            if (line.StartsWith('+'))
            {
                hunkAdded.Add(line[1..]);
                continue;
            }

            if (line.StartsWith('\\'))
            {
                // "\ No newline at end of file" satiri; sayaci ilerletmiyor.
                continue;
            }

            oldLine++;
        }

        Flush(deleted, hunkDeleted, hunkAdded, hunkNumbers, ignoreWhitespace);

        return deleted;
    }

    private static void Flush(
        List<int> deleted,
        List<string> hunkDeleted,
        List<string> hunkAdded,
        List<int> hunkNumbers,
        bool ignoreWhitespace)
    {
        HashSet<string> added = ignoreWhitespace
            ? new HashSet<string>(hunkAdded.Select(line => line.Trim()), StringComparer.Ordinal)
            : [];

        for (int i = 0; i < hunkDeleted.Count; i++)
        {
            string text = hunkDeleted[i].Trim();

            if (ignoreWhitespace && (text.Length == 0 || added.Contains(text)))
            {
                continue;
            }

            deleted.Add(hunkNumbers[i]);
        }

        hunkDeleted.Clear();
        hunkAdded.Clear();
        hunkNumbers.Clear();
    }

    /// <summary><c>@@ -a,b +c,d @@</c> basligindaki a degeri.</summary>
    private static int StartOfOldSide(string header)
    {
        int minus = header.IndexOf('-');

        if (minus < 0)
        {
            return 0;
        }

        int end = minus + 1;

        while (end < header.Length && char.IsAsciiDigit(header[end]))
        {
            end++;
        }

        return int.TryParse(header[(minus + 1)..end], System.Globalization.CultureInfo.InvariantCulture, out int start) ? start : 0;
    }

    private static bool IsCSharp(string path) => path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase);
}
