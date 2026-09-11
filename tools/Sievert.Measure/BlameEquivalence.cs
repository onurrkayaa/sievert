using System.Diagnostics;

using LibGit2Sharp;

using Sievert.Mining;

namespace Sievert.Measure;

/// <summary>
/// Bizim bosluk filtremiz ile <c>git blame -w</c> ayni commit'i mi sucluyor.
///
/// Ikisi ayni sey degil: biz blame'e HANGI satirlarin sorulacagini suzuyoruz, <c>-w</c>
/// ise blame'in kendi eslestirmesini bosluga duyarsiz yapiyor. Bu olcum, farkin pratikte
/// ne kadar oldugunu satir satir sayiyor. Davranis degistirmiyor.
/// </summary>
public static class BlameEquivalence
{
    /// <summary>Orneklem her kosuda ayni ciksin diye sabit tohum.</summary>
    private const int Seed = 42;

    public static void Report(string repositoryPath, IReadOnlyList<SzzFix> fixes, int sampleSize)
    {
        using Repository repository = new(repositoryPath);

        Random random = new(Seed);
        List<SzzFix> sample = [.. fixes.OrderBy(_ => random.Next()).Take(sampleSize)];

        int comparedLines = 0;
        int sameAsIgnoringWhitespace = 0;
        int differentFromIgnoringWhitespace = 0;
        int sameAsPlainGit = 0;
        int differentFromPlainGit = 0;
        int gitWhitespaceMatters = 0;
        int shiftedPlusOne = 0;
        int shiftedMinusOne = 0;
        int missingOnOurSide = 0;
        int missingOnGitSide = 0;
        int filesCompared = 0;

        foreach (SzzFix fix in sample)
        {
            Commit? commit = repository.Lookup<Commit>(fix.Sha);

            // sievert:disable SV004 Parents bellekteki bir koleksiyon, veritabani sorgusu degil
            Commit? parent = commit?.Parents.FirstOrDefault();

            if (commit is null || parent is null)
            {
                continue;
            }

            using Patch patch = repository.Diff.Compare<Patch>(parent.Tree, commit.Tree);

            foreach (PatchEntryChanges change in patch)
            {
                if (!change.Path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)
                    || change.Status == ChangeKind.Added)
                {
                    continue;
                }

                List<int> lines = BugIntroducerFinder.DeletedLines(change.Patch, ignoreWhitespace: true);

                if (lines.Count == 0)
                {
                    continue;
                }

                string path = change.OldPath ?? change.Path;
                Dictionary<int, string> ours = OurBlame(repository, parent, path);
                Dictionary<int, string> theirs = GitBlame(repositoryPath, parent.Sha, path, ignoreWhitespace: true);
                Dictionary<int, string> plain = GitBlame(repositoryPath, parent.Sha, path, ignoreWhitespace: false);

                if (ours.Count == 0 || theirs.Count == 0 || plain.Count == 0)
                {
                    continue;
                }

                filesCompared++;

                foreach (int line in lines)
                {
                    bool haveOurs = ours.TryGetValue(line, out string? oursSha);
                    bool haveTheirs = theirs.TryGetValue(line, out string? theirsSha);
                    bool havePlain = plain.TryGetValue(line, out string? plainSha);

                    if (!haveOurs && !haveTheirs)
                    {
                        continue;
                    }

                    comparedLines++;

                    if (!haveOurs)
                    {
                        missingOnOurSide++;
                        continue;
                    }

                    if (!haveTheirs)
                    {
                        missingOnGitSide++;
                        continue;
                    }

                    if (string.Equals(oursSha, theirsSha, StringComparison.Ordinal))
                    {
                        sameAsIgnoringWhitespace++;
                    }
                    else
                    {
                        differentFromIgnoringWhitespace++;
                    }

                    // Kaydirma denemesi: satir numarasi taban farki var mi.
                    if (havePlain)
                    {
                        if (ours.TryGetValue(line + 1, out string? plusOne)
                            && string.Equals(plusOne, plainSha, StringComparison.Ordinal))
                        {
                            shiftedPlusOne++;
                        }

                        if (ours.TryGetValue(line - 1, out string? minusOne)
                            && string.Equals(minusOne, plainSha, StringComparison.Ordinal))
                        {
                            shiftedMinusOne++;
                        }
                    }

                    // Kontrol kosusu: bizimki ile duz git blame (bosluk secenegi yok).
                    // Ikisi de ayrilirsa fark -w'den degil, iki blame uygulamasinin
                    // kendisinden geliyor demektir.
                    if (havePlain)
                    {
                        if (string.Equals(oursSha, plainSha, StringComparison.Ordinal))
                        {
                            sameAsPlainGit++;
                        }
                        else
                        {
                            differentFromPlainGit++;
                        }

                        if (!string.Equals(plainSha, theirsSha, StringComparison.Ordinal))
                        {
                            gitWhitespaceMatters++;
                        }
                    }
                }
            }
        }

        Console.WriteLine($"orneklem duzeltme commit'i : {sample.Count}");
        Console.WriteLine($"karsilastirilan dosya      : {filesCompared}");
        Console.WriteLine($"karsilastirilan satir      : {comparedLines}");
        Console.WriteLine();
        Console.WriteLine($"bizimki vs git blame -w   : ayni {sameAsIgnoringWhitespace}, farkli {differentFromIgnoringWhitespace}");
        Console.WriteLine($"bizimki vs duz git blame  : ayni {sameAsPlainGit}, farkli {differentFromPlainGit}");
        Console.WriteLine($"git blame -w vs duz git blame farkli: {gitWhitespaceMatters}");
        Console.WriteLine($"bizimki(+1) vs duz git blame ayni : {shiftedPlusOne}");
        Console.WriteLine($"bizimki(-1) vs duz git blame ayni : {shiftedMinusOne}");
        Console.WriteLine($"  bizde karsilik yok      : {missingOnOurSide}");
        Console.WriteLine($"  git blame -w'de yok     : {missingOnGitSide}");
    }

    private static Dictionary<int, string> OurBlame(Repository repository, Commit parent, string path)
    {
        Dictionary<int, string> lines = new();

        try
        {
            foreach (BlameHunk hunk in repository.Blame(path, new BlameOptions { StartingAt = parent }))
            {
                for (int i = 0; i < hunk.LineCount; i++)
                {
                    // FinalStartLineNumber 0 tabanli; disariya 1 tabanli veriyoruz.
                    lines[hunk.FinalStartLineNumber + i + 1] = hunk.FinalCommit.Sha;
                }
            }
        }
        catch (LibGit2SharpException)
        {
            return [];
        }

        return lines;
    }

    /// <summary>
    /// <c>git blame -w --porcelain</c> ciktisini okur. Porcelain bicimde her satir grubu
    /// "&lt;sha&gt; &lt;eski satir&gt; &lt;yeni satir&gt; [&lt;adet&gt;]" basligiyla basliyor.
    /// Git ikilisini disaridan cagiriyoruz; bu yalnizca olcum programinda, urunde degil.
    /// </summary>
    private static Dictionary<int, string> GitBlame(
        string repositoryPath,
        string sha,
        string path,
        bool ignoreWhitespace)
    {
        ProcessStartInfo start = new("git")
        {
            WorkingDirectory = repositoryPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        start.ArgumentList.Add("blame");

        if (ignoreWhitespace)
        {
            start.ArgumentList.Add("-w");
        }

        start.ArgumentList.Add("--porcelain");
        start.ArgumentList.Add(sha);
        start.ArgumentList.Add("--");
        start.ArgumentList.Add(path);

        using Process? process = Process.Start(start);

        if (process is null)
        {
            return [];
        }

        Dictionary<int, string> lines = new();
        string? line;

        while ((line = process.StandardOutput.ReadLine()) is not null)
        {
            if (Header(line) is not (string sha40, int finalLine))
            {
                continue;
            }

            lines[finalLine] = sha40;
        }

        process.WaitForExit();

        return process.ExitCode == 0 ? lines : [];
    }

    private static (string Sha, int FinalLine)? Header(string line)
    {
        if (line.Length < 42 || line[40] != ' ')
        {
            return null;
        }

        string sha = line[..40];

        if (!sha.All(char.IsAsciiHexDigitLower))
        {
            return null;
        }

        string[] parts = line.Split(' ');

        return parts.Length >= 3 && int.TryParse(parts[2], out int finalLine)
            ? (sha, finalLine)
            : null;
    }
}
