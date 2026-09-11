using LibGit2Sharp;

using Sievert.Mining;

namespace Sievert.Measure;

/// <summary>
/// SZZ'yi iki bicimde calistirip farki olcer: silinen satirlar secilirken bosluk
/// degisiklikleri atlaniyor (varsayilan) ya da atlanmiyor. Blame ikisinde de ayni
/// oldugu icin dosya basina BIR kez calistiriliyor; iki varyant ayni blame ciktisindan
/// okunuyor, yoksa olcum iki kat surerdi.
///
/// Ayrica SZZ'nin nerede eleme yaptigini sayiyor: kac duzeltme .cs degistiriyor, kacinin
/// silinen satiri var, kaci birini suclayabiliyor.
/// </summary>
public static class SzzVariants
{
    public static void Report(string repositoryPath, IReadOnlyList<SzzFix> fixes)
    {
        using Repository repository = new(repositoryPath);

        LibGit2Sharp.CompareOptions compare = new()
        {
            Similarity = new SimilarityOptions
            {
                RenameDetectionMode = RenameDetectionMode.Renames,
                RenameThreshold = RepositoryMiner.RenameSimilarityThreshold,
            },
        };

        HashSet<string> withWhitespaceIgnored = new(StringComparer.Ordinal);
        HashSet<string> withWhitespaceKept = new(StringComparer.Ordinal);

        int touchedCSharp = 0;
        int hadDeletedLines = 0;
        int blamedSomeone = 0;
        int linesIgnored = 0;
        int linesKept = 0;
        int blameFailures = 0;
        int hunkMisses = 0;
        int droppedTime = 0;
        int droppedMerge = 0;
        int onlyWhitespaceDeletions = 0;

        foreach (SzzFix fix in fixes)
        {
            Commit? commit = repository.Lookup<Commit>(fix.Sha);

            // sievert:disable SV004 Parents bellekteki bir koleksiyon, veritabani sorgusu degil
            Commit? parent = commit?.Parents.FirstOrDefault();

            if (commit is null || parent is null)
            {
                continue;
            }

            using Patch patch = repository.Diff.Compare<Patch>(parent.Tree, commit.Tree, compare);
            List<PatchEntryChanges> changes = [.. patch];

            if (changes.Count > SzzOptions.Default.MaxFilesInFix)
            {
                continue;
            }

            bool anyCSharp = false;
            bool anyDeleted = false;
            int accusations = 0;

            foreach (PatchEntryChanges change in changes)
            {
                if (!change.Path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)
                    || change.Status == ChangeKind.Added)
                {
                    continue;
                }

                anyCSharp = true;

                List<int> ignored = BugIntroducerFinder.DeletedLines(change.Patch, ignoreWhitespace: true);
                List<int> kept = BugIntroducerFinder.DeletedLines(change.Patch, ignoreWhitespace: false);

                linesIgnored += ignored.Count;
                linesKept += kept.Count;

                if (kept.Count > 0)
                {
                    anyDeleted = true;
                }

                if (kept.Count == 0)
                {
                    continue;
                }

                BlameHunkCollection hunks;

                try
                {
                    hunks = repository.Blame(
                        change.OldPath ?? change.Path,
                        new BlameOptions { StartingAt = parent });
                }
                catch (LibGit2SharpException)
                {
                    blameFailures++;
                    continue;
                }

                if (ignored.Count == 0)
                {
                    onlyWhitespaceDeletions++;
                }

                accusations += Collect(hunks, ignored, fix, withWhitespaceIgnored, ref hunkMisses, ref droppedTime, ref droppedMerge);
                Collect(hunks, kept, fix, withWhitespaceKept, ref hunkMisses, ref droppedTime, ref droppedMerge);
            }

            if (anyCSharp)
            {
                touchedCSharp++;
            }

            if (anyDeleted)
            {
                hadDeletedLines++;
            }

            if (accusations > 0)
            {
                blamedSomeone++;
            }
        }

        Console.WriteLine($"duzeltme commit'i                 : {fixes.Count}");
        Console.WriteLine($"  .cs degistiren                  : {touchedCSharp}");
        Console.WriteLine($"  silinen .cs satiri olan         : {hadDeletedLines}");
        Console.WriteLine($"  birini suclayabilen             : {blamedSomeone}");
        Console.WriteLine();
        Console.WriteLine($"blame'e sorulan satir (bosluk yok sayilarak) : {linesIgnored}");
        Console.WriteLine($"blame'e sorulan satir (bosluk sayilarak)     : {linesKept}");
        Console.WriteLine();
        Console.WriteLine($"etiket (bosluk yok sayilarak, varsayilan) : {withWhitespaceIgnored.Count}");
        Console.WriteLine($"etiket (bosluk sayilarak)                 : {withWhitespaceKept.Count}");
        Console.WriteLine($"sadece bosluk sayilinca eklenen etiket    : {withWhitespaceKept.Except(withWhitespaceIgnored, StringComparer.Ordinal).Count()}");
        Console.WriteLine($"sadece bosluk yok sayilinca eklenen etiket: {withWhitespaceIgnored.Except(withWhitespaceKept, StringComparer.Ordinal).Count()}");
        Console.WriteLine();
        Console.WriteLine($"blame calistirilamadi (dosya bulunamadi) : {blameFailures}");
        Console.WriteLine($"satir hicbir hunk'a dusmedi              : {hunkMisses}");
        Console.WriteLine($"atilan: birlestirme                      : {droppedMerge}");
        Console.WriteLine($"atilan: zaman tutarsizligi               : {droppedTime}");
        Console.WriteLine($"silinen satirlari sadece bosluk olan dosya: {onlyWhitespaceDeletions}");
    }

    private static int Collect(
        BlameHunkCollection hunks,
        List<int> lines,
        SzzFix fix,
        HashSet<string> blamed,
        ref int hunkMisses,
        ref int droppedTime,
        ref int droppedMerge)
    {
        int accused = 0;

        foreach (int line in lines)
        {
            BlameHunk? hunk = null;

            foreach (BlameHunk candidate in hunks)
            {
                if (line >= candidate.FinalStartLineNumber
                    && line < candidate.FinalStartLineNumber + candidate.LineCount)
                {
                    hunk = candidate;
                    break;
                }
            }

            if (hunk is null)
            {
                hunkMisses++;
                continue;
            }

            Commit culprit = hunk.FinalCommit;

            // sievert:disable SV004 Parents bellekteki bir koleksiyon, veritabani sorgusu degil
            if (culprit.Parents.Count() > 1)
            {
                droppedMerge++;
                continue;
            }

            if (culprit.Author.When.ToUniversalTime() > fix.Date)
            {
                droppedTime++;
                continue;
            }

            blamed.Add(culprit.Sha);
            accused++;
        }

        return accused;
    }
}
