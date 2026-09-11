using System.Globalization;
using System.Text.RegularExpressions;

using LibGit2Sharp;

using Sievert.Mining;

namespace Sievert.Measure;

/// <summary>
/// Dogrulama listesindeki satirlar icin siniflandirma malzemesi uretir: duzeltmenin ilgili
/// hunk'i ve suclanan satirin ebeveyn surumundeki baglami.
///
/// Karar YAZMIYOR. Ciktidaki "Karar" ve "Not" satirlari bos; bu program yalnizca bakilacak
/// seyi onune koyuyor. Ornek karar da koymuyor, cunku konulan ornek siniflandirmayi
/// yonlendirir.
/// </summary>
public static partial class Material
{
    /// <summary>Hunk'tan yazilacak en fazla satir. Asilirsa kirpildigi yaziliyor.</summary>
    private const int MaxHunkLines = 40;

    /// <summary>Suclanan satirin ustunde ve altinda kac satir gosterilecek.</summary>
    private const int ContextLines = 8;

    public static void Report(string repositoryPath, string listPath, string repositoryHeading, int startNumber)
    {
        using Repository repository = new(repositoryPath);

        bool inThisRepo = false;
        bool labelledSection = true;
        int number = startNumber;

        foreach (string line in File.ReadLines(listPath))
        {
            if (line.StartsWith("## ", StringComparison.Ordinal) && !line.StartsWith("### ", StringComparison.Ordinal))
            {
                inThisRepo = line.Contains(repositoryHeading, StringComparison.OrdinalIgnoreCase);
            }

            if (line.StartsWith("### ", StringComparison.Ordinal))
            {
                labelledSection = !line.Contains("Etiketlenmemis", StringComparison.Ordinal);
            }

            if (!inThisRepo || RowPattern().Match(line.Trim()) is not { Success: true } match)
            {
                continue;
            }

            Write(repository, repositoryHeading, number++, labelledSection, match);
        }
    }

    private static void Write(
        Repository repository,
        string repositoryHeading,
        int number,
        bool labelled,
        Match row)
    {
        string fixShort = row.Groups[1].Value;
        string culpritShort = row.Groups[2].Value;
        string path = row.Groups[3].Value;
        (int first, int last) = Range(row.Groups[4].Value);

        Commit? fix = repository.Lookup<Commit>(fixShort);
        Commit? culprit = repository.Lookup<Commit>(culpritShort);

        // sievert:disable SV004 Parents bellekteki bir koleksiyon, veritabani sorgusu degil
        Commit? parent = fix?.Parents.FirstOrDefault();

        Console.WriteLine($"### Satir {number} / 30  [{repositoryHeading}]  [etiketli: {(labelled ? "EVET" : "HAYIR")}]");
        Console.WriteLine();

        if (fix is null || parent is null || culprit is null)
        {
            Console.WriteLine("Commit bulunamadi; satir uretilemedi.");
            Console.WriteLine();
            return;
        }

        Console.WriteLine($"Duzeltme commit'i: `{fix.Sha}` - {Subject(fix)}");

        if (labelled)
        {
            Console.WriteLine($"Suclanan commit:   `{culprit.Sha}` - {Subject(culprit)} ({Date(culprit)})");
        }
        else
        {
            Console.WriteLine($"Aday commit:       `{culprit.Sha}` - {Subject(culprit)} ({Date(culprit)})");
        }

        Console.WriteLine($"Dosya: `{path}`:{Span(first, last)}");
        Console.WriteLine();

        PatchEntryChanges? change = ChangeFor(repository, parent, fix, path);

        if (!labelled)
        {
            Console.WriteLine($"SZZ neden suclamadi: {WhyNotBlamed(change, first, last)}");
            Console.WriteLine();
        }

        Console.WriteLine("Duzeltmenin ilgili hunk'i:");
        Console.WriteLine();
        Console.WriteLine("```diff");
        WriteHunk(change, first, last);
        Console.WriteLine("```");
        Console.WriteLine();

        Console.WriteLine("Suclanan satirin baglami:");
        Console.WriteLine();
        Console.WriteLine("```csharp");
        WriteContext(parent, change?.OldPath ?? path, first, last);
        Console.WriteLine("```");
        Console.WriteLine();

        Console.WriteLine("Karar:");
        Console.WriteLine("Not:");
        Console.WriteLine();
    }

    /// <summary>
    /// Satir araligina denk gelen hunk'i yazar. Ortusen hunk yoksa dosyanin ilk hunk'i
    /// yaziliyor ve bu durum belirtiliyor.
    /// </summary>
    private static void WriteHunk(PatchEntryChanges? change, int first, int last)
    {
        if (change is null)
        {
            Console.WriteLine("(bu dosya duzeltmenin diff'inde bulunamadi)");
            return;
        }

        List<List<string>> hunks = Hunks(change.Patch);

        if (hunks.Count == 0)
        {
            Console.WriteLine("(diff bos)");
            return;
        }

        List<string>? chosen = hunks.FirstOrDefault(hunk => Overlaps(hunk[0], first, last));
        bool overlapping = chosen is not null;
        chosen ??= hunks[0];

        if (!overlapping)
        {
            Console.WriteLine($"(satir {Span(first, last)} hicbir hunk'a denk gelmiyor; dosyanin ilk hunk'i)");
        }

        foreach (string line in chosen.Take(MaxHunkLines))
        {
            Console.WriteLine(line);
        }

        if (chosen.Count > MaxHunkLines)
        {
            Console.WriteLine($"(kirpildi: hunk {chosen.Count} satir, ilk {MaxHunkLines} satiri yazildi)");
        }
    }

    /// <summary>Ebeveyn surumundeki dosyadan satir araliginin cevresini yazar.</summary>
    private static void WriteContext(Commit parent, string path, int first, int last)
    {
        if (parent[path]?.Target is not Blob blob)
        {
            Console.WriteLine("(dosya ebeveyn surumunde bulunamadi)");
            return;
        }

        string[] lines = blob.GetContentText().Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        int from = Math.Max(1, first - ContextLines);
        int to = Math.Min(lines.Length, last + ContextLines);

        for (int line = from; line <= to; line++)
        {
            string marker = line >= first && line <= last ? ">>>" : "   ";

            Console.WriteLine($"{marker} {line,6}  {lines[line - 1]}");
        }
    }

    /// <summary>
    /// Bu commit'in neden suclanmadigini tek cumleyle anlatir. Karar degil, olculen bir
    /// mesafe: aday satirlarin duzeltmenin sildigi en yakin satira uzakligi.
    /// </summary>
    private static string WhyNotBlamed(PatchEntryChanges? change, int first, int last)
    {
        if (change is null)
        {
            return "Bu dosya duzeltmenin diff'inde bulunamadi.";
        }

        List<int> deleted = BugIntroducerFinder.DeletedLines(change.Patch, ignoreWhitespace: true);

        if (deleted.Count == 0)
        {
            return "Duzeltme bu dosyada hic satir silmemis, yalnizca ekleme yapmis; "
                + "SZZ silinen satirlara baktigi icin bu dosyadan kimse suclanmiyor.";
        }

        int nearest = deleted.MinBy(line => Math.Min(Math.Abs(line - first), Math.Abs(line - last)));
        int distance = Math.Min(Math.Abs(nearest - first), Math.Abs(nearest - last));

        return $"Bu commit'in satirlari ({Span(first, last)}) duzeltmenin sildigi satirlar "
            + $"arasinda degil; en yakin silinen satir {nearest}, arada {distance} satir var.";
    }

    private static PatchEntryChanges? ChangeFor(Repository repository, Commit parent, Commit fix, string path)
    {
        using Patch patch = repository.Diff.Compare<Patch>(parent.Tree, fix.Tree);

        return patch.FirstOrDefault(change =>
            string.Equals(change.Path, path, StringComparison.Ordinal)
            || string.Equals(change.OldPath, path, StringComparison.Ordinal));
    }

    /// <summary>Yamayi hunk'lara boler; her hunk basligiyla birlikte donuyor.</summary>
    private static List<List<string>> Hunks(string patchText)
    {
        List<List<string>> hunks = [];
        List<string>? current = null;

        foreach (string line in patchText.Split('\n'))
        {
            if (line.StartsWith("@@", StringComparison.Ordinal))
            {
                current = [line];
                hunks.Add(current);
                continue;
            }

            current?.Add(line.TrimEnd('\r'));
        }

        return hunks;
    }

    /// <summary>Hunk basligindaki eski taraf araligi, verilen satir araligiyla kesisiyor mu.</summary>
    private static bool Overlaps(string header, int first, int last)
    {
        Match match = HeaderPattern().Match(header);

        if (!match.Success)
        {
            return false;
        }

        int start = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
        int count = match.Groups[2].Success
            ? int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture)
            : 1;

        return start <= last && start + Math.Max(count, 1) - 1 >= first;
    }

    private static (int First, int Last) Range(string span)
    {
        string[] parts = span.Split('-');
        int first = int.Parse(parts[0], CultureInfo.InvariantCulture);

        return (first, int.Parse(parts[^1], CultureInfo.InvariantCulture));
    }

    private static string Span(int first, int last) =>
        first == last
            ? first.ToString(CultureInfo.InvariantCulture)
            : $"{first}-{last}";

    private static string Subject(Commit commit)
    {
        string text = commit.MessageShort.Trim();

        return text.Length > 90 ? text[..90] + "..." : text;
    }

    private static string Date(Commit commit) =>
        commit.Author.When.ToUniversalTime().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    [GeneratedRegex(@"^\|\s*\d+\s*\|\s*`([0-9a-f]+)`.*?\|\s*`([0-9a-f]+)`.*?\|\s*`(.+?)`\s*\|\s*([0-9-]+)\s*\|")]
    private static partial Regex RowPattern();

    [GeneratedRegex(@"^@@ -(\d+)(?:,(\d+))?")]
    private static partial Regex HeaderPattern();
}
