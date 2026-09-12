using System.Globalization;
using System.Text;

using LibGit2Sharp;

using Sievert.Modeling;

namespace Sievert.Measure;

/// <summary>Kor listedeki tek bir ornek.</summary>
public sealed record ValidationRow(
    string SampleId,
    string Identity,
    string Sha,
    bool ModelPrediction,
    double Probability,
    double Threshold,
    bool SzzLabel);

/// <summary>
/// Adim 5b Bolum D: 30 tahminlik kor degerlendirme listesi hazirlar.
///
/// SINIFLANDIRMA YAPMIYOR. Malzeme dosyasinda karar alanlari bos; model tahmini,
/// olasilik, SZZ etiketi ve karisiklik matrisi hucresi malzemeye **girmiyor** - onlar
/// yalnizca ayri anahtar dosyasinda.
/// </summary>
public static class ValidationSample
{
    public const int Seed = 20260912;

    /// <summary>Secimden sonra 30 satiri karistiran ikinci sabit tohum.</summary>
    public const int ShuffleSeed = 20260913;

    public const int PerGroup = 5;

    /// <summary>Bir commit icin gosterilecek en fazla diff satiri.</summary>
    public const int MaximumDiffLines = 160;

    /// <summary>Bir dosya icin gosterilecek en fazla sonraki commit.</summary>
    public const int MaximumFollowUps = 3;

    public static int Run(string[] args)
    {
        string dataDirectory = args[1];
        string codeCommit = args[2];
        string materialPath = args[3];
        Dictionary<string, string> clones = Clones(args[4]);

        string snapshot = Path.Combine(dataDirectory, "commit-metrics.csv");
        string manifest = Path.Combine(dataDirectory, "split-manifest.csv");
        string predictionsPath = Path.Combine(dataDirectory, "model-predictions.csv");

        foreach (string file in (string[])[snapshot, manifest, predictionsPath])
        {
            FileChecksum.Verify(file, Path.ChangeExtension(file, ".sha256"));
        }

        IReadOnlyList<SnapshotRow> rows = SnapshotReader.Read(snapshot);
        IReadOnlyList<RepositorySplit> repositories = SplitData.Build(rows, SplitManifestReader.Read(manifest));

        Dictionary<string, (double Probability, bool Predicted, double Threshold)> predictions =
            new(StringComparer.Ordinal);

        foreach (string line in File.ReadLines(predictionsPath).Skip(1))
        {
            string[] fields = line.Split(',');
            predictions[fields[0] + " " + fields[1]] = (
                double.Parse(fields[4], CultureInfo.InvariantCulture),
                fields[6] == "1",
                double.Parse(fields[7], CultureInfo.InvariantCulture));
        }

        List<ValidationRow> picked = [];

        Console.WriteLine("== Orneklem uygunlugu ==");

        foreach (RepositorySplit repository in repositories)
        {
            foreach (bool wanted in (bool[])[true, false])
            {
                List<SnapshotRow> group = [];
                int eligible = 0;
                int excluded = 0;
                int total = 0;

                foreach (SnapshotRow row in repository.Test)
                {
                    (double probability, bool predicted, double threshold) =
                        predictions[row.RepositoryIdentity + " " + row.Sha];
                    _ = probability;
                    _ = threshold;

                    if (predicted != wanted)
                    {
                        continue;
                    }

                    total++;

                    // Tek uygunluk kurali: insan kaynak kod degisikligine bakacak.
                    if (row.CsFilesChanged > 0)
                    {
                        eligible++;
                        group.Add(row);
                    }
                    else
                    {
                        excluded++;
                    }
                }

                // Secim deterministik: sabit tohumlu karistirma, sonra ilk bes.
                Random random = new(Seed);

                for (int index = group.Count - 1; index > 0; index--)
                {
                    int swap = random.Next(index + 1);
                    (group[index], group[swap]) = (group[swap], group[index]);
                }

                int take = Math.Min(PerGroup, group.Count);

                Console.WriteLine(
                    $"  {repository.Identity} model-{(wanted ? "pozitif" : "negatif")}: "
                    + $"toplam {total}, uygun {eligible}, secilen {take}, "
                    + $"dislanan CsFilesChanged=0 {excluded}"
                    + (take < PerGroup ? "  EKSIK" : string.Empty));

                for (int index = 0; index < take; index++)
                {
                    (double probability, bool predicted, double threshold) =
                        predictions[group[index].RepositoryIdentity + " " + group[index].Sha];

                    picked.Add(new ValidationRow(
                        string.Empty,
                        group[index].RepositoryIdentity,
                        group[index].Sha,
                        predicted,
                        probability,
                        threshold,
                        group[index].IsBugIntroducing));
                }
            }
        }

        // Ikinci sabit tohumla karistir; SampleId sirasi model sinifini ele vermesin.
        Random shuffle = new(ShuffleSeed);

        for (int index = picked.Count - 1; index > 0; index--)
        {
            int swap = shuffle.Next(index + 1);
            (picked[index], picked[swap]) = (picked[swap], picked[index]);
        }

        List<ValidationRow> numbered = [];

        for (int index = 0; index < picked.Count; index++)
        {
            numbered.Add(picked[index] with
            {
                SampleId = "SAMPLE-" + (index + 1).ToString("D2", CultureInfo.InvariantCulture),
            });
        }

        WriteKey(dataDirectory, numbered);
        WriteMaterial(materialPath, dataDirectory, codeCommit, numbered, rows, clones);

        return 0;
    }

    private static void WriteKey(string dataDirectory, IReadOnlyList<ValidationRow> rows)
    {
        StringBuilder csv = new();
        csv.Append("SampleId,RepositoryIdentity,Sha,ModelPrediction,Probability,TrainThreshold,SzzLabel,ConfusionAgainstSzz\n");

        foreach (ValidationRow row in rows)
        {
            string confusion = (row.ModelPrediction, row.SzzLabel) switch
            {
                (true, true) => "TP",
                (true, false) => "FP",
                (false, true) => "FN",
                (false, false) => "TN",
            };

            csv.Append(row.SampleId).Append(',');
            csv.Append(row.Identity).Append(',');
            csv.Append(row.Sha).Append(',');
            csv.Append(row.ModelPrediction ? '1' : '0').Append(',');
            csv.Append(row.Probability.ToString("R", CultureInfo.InvariantCulture)).Append(',');
            csv.Append(row.Threshold.ToString("R", CultureInfo.InvariantCulture)).Append(',');
            csv.Append(row.SzzLabel ? '1' : '0').Append(',');
            csv.Append(confusion).Append('\n');
        }

        string path = Path.Combine(dataDirectory, "prediction-validation-key.csv");
        File.WriteAllText(path, csv.ToString(), new UTF8Encoding(false));
        File.WriteAllText(Path.ChangeExtension(path, ".sha256"), FileChecksum.Line(path));

        Console.WriteLine();
        Console.WriteLine($"prediction-validation-key.csv: {FileChecksum.Sha256(path)}");
    }

    private static void WriteMaterial(
        string materialPath,
        string dataDirectory,
        string codeCommit,
        IReadOnlyList<ValidationRow> rows,
        IReadOnlyList<SnapshotRow> snapshot,
        Dictionary<string, string> clones)
    {
        Dictionary<string, SnapshotRow> byKey = new(StringComparer.Ordinal);

        foreach (SnapshotRow row in snapshot)
        {
            byKey[row.RepositoryIdentity + " " + row.Sha] = row;
        }

        StringBuilder text = new();
        text.Append("# Model tahminlerinin elle dogrulanmasi: malzeme\n\n");
        text.Append("**Olcut dosyasi:** `docs/olcumler/asama5-tahmin-dogrulama-olcut.md`\n\n");
        text.Append(CultureInfo.InvariantCulture, $"**Orneklem tohumu:** {Seed} (secim), {ShuffleSeed} (karistirma)\n\n");
        text.Append(CultureInfo.InvariantCulture, $"**Ureten kod commit'i:** `{codeCommit}`\n\n");

        Dictionary<string, int> perRepository = new(StringComparer.Ordinal);

        foreach (ValidationRow row in rows)
        {
            perRepository[row.Identity] = perRepository.GetValueOrDefault(row.Identity) + 1;
        }

        text.Append("**Repo dagilimi:** ");
        text.Append(string.Join(
            ", ",
            perRepository.OrderBy(entry => entry.Key, StringComparer.Ordinal)
                .Select(entry => $"{entry.Key} {entry.Value}")));
        text.Append("\n\n");

        text.Append("**Uygunluk kurali:** yalnizca `CsFilesChanged > 0` olan test commit'leri. ");
        text.Append("Sebep: degerlendirici kaynak kod degisikligine bakacak. ");
        text.Append("Baska boyut, churn ya da dosya sayisi filtresi uygulanmadi; botlar dislanmadi.\n\n");

        text.Append("**Korlenen alanlar (bu dosyada YOK):** model tahmini, model olasiligi, ");
        text.Append("train esigi, SZZ etiketi (`IsBugIntroducing`), `LabelSource`, ");
        text.Append("karisiklik matrisi hucresi (TP/FP/FN/TN).\n\n");

        text.Append("> **Uyari:** kararlar tamamlanmadan `data/asama5/prediction-validation-key.csv` acilmaz.\n\n");

        text.Append("Her ornek icin yalnizca kronolojik olgular veriliyor. ");
        text.Append("Sonraki commit'lerin hicbiri \"ilgili duzeltme\" diye isaretlenmedi.\n\n");
        text.Append("---\n\n");

        foreach (ValidationRow row in rows)
        {
            SnapshotRow metrics = byKey[row.Identity + " " + row.Sha];
            text.Append(Section(row, metrics, clones[row.Identity]));
        }

        File.WriteAllText(materialPath, text.ToString(), new UTF8Encoding(false));

        Console.WriteLine($"malzeme: {materialPath}");
    }

    private static string Section(ValidationRow row, SnapshotRow metrics, string clonePath)
    {
        using Repository repository = new(clonePath);
        Commit commit = repository.Lookup<Commit>(row.Sha);

        StringBuilder text = new();
        string link = "https://" + row.Identity + "/commit/" + row.Sha;

        text.Append(CultureInfo.InvariantCulture, $"### Ornek {row.SampleId} — {row.Identity}\n\n");
        text.Append("Commit:\n\n");
        text.Append(CultureInfo.InvariantCulture, $"- Kisa SHA: `{row.Sha[..12]}`\n");
        text.Append(CultureInfo.InvariantCulture, $"- Yazar tarihi: {Date(metrics.AuthorDateUtc)}\n");
        text.Append(CultureInfo.InvariantCulture, $"- Mesaj basligi: {Escape(Subject(commit))}\n");
        text.Append(CultureInfo.InvariantCulture, $"- Baglanti: {link}\n\n");

        // sievert:disable SV004 Parents bellekteki koleksiyon, veritabani sorgusu degil
        Tree? parent = commit.Parents.FirstOrDefault()?.Tree;
        using Patch patch = repository.Diff.Compare<Patch>(parent, commit.Tree);

        List<PatchEntryChanges> csharp =
            [.. patch.Where(change => change.Path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))];

        text.Append("Degisiklik ozeti:\n\n");
        text.Append(CultureInfo.InvariantCulture, $"- Degisen toplam dosya: {metrics.FilesChanged}\n");
        text.Append(CultureInfo.InvariantCulture, $"- Degisen `.cs` dosyasi: {metrics.CsFilesChanged}\n");
        text.Append(CultureInfo.InvariantCulture, $"- Eklenen satir: {metrics.LinesAdded}, silinen satir: {metrics.LinesDeleted}\n");
        text.Append("- Degisen `.cs` dosyalari:\n");

        foreach (PatchEntryChanges change in csharp)
        {
            text.Append(CultureInfo.InvariantCulture, $"  - `{change.Path}`\n");
        }

        text.Append("\nCommit'in ilgili C# diff'i:\n\n```diff\n");

        int written = 0;
        bool trimmed = false;

        foreach (PatchEntryChanges change in csharp)
        {
            if (written >= MaximumDiffLines)
            {
                trimmed = true;
                break;
            }

            text.Append(CultureInfo.InvariantCulture, $"--- {change.Path}\n");
            written++;

            foreach (string line in change.Patch.Split('\n'))
            {
                if (written >= MaximumDiffLines)
                {
                    trimmed = true;
                    break;
                }

                if (line.StartsWith("@@", StringComparison.Ordinal)
                    || line.StartsWith('+')
                    || line.StartsWith('-')
                    || line.StartsWith(' '))
                {
                    text.Append(line.TrimEnd('\r')).Append('\n');
                    written++;
                }
            }
        }

        text.Append("```\n\n");

        if (trimmed)
        {
            text.Append(CultureInfo.InvariantCulture, $"Kirpildi; tam diff: {link}\n\n");
        }

        text.Append("Sonraki tarihsel olgular:\n\n");

        foreach (PatchEntryChanges change in csharp)
        {
            text.Append(CultureInfo.InvariantCulture, $"- `{change.Path}`\n");

            List<LogEntry> later = [];

            foreach (LogEntry entry in repository.Commits.QueryBy(
                change.Path,
                new CommitFilter { SortBy = CommitSortStrategies.Time }))
            {
                if (entry.Commit.Author.When.ToUniversalTime() > metrics.AuthorDateUtc)
                {
                    later.Add(entry);
                }
            }

            later.Reverse();

            if (later.Count == 0)
            {
                text.Append("  - Gozlem araliginda sonraki degisiklik yok\n");
                continue;
            }

            foreach (LogEntry entry in later.Take(MaximumFollowUps))
            {
                text.Append(CultureInfo.InvariantCulture, $"  - `{entry.Commit.Sha[..12]}` ");
                text.Append(CultureInfo.InvariantCulture, $"{Date(entry.Commit.Author.When.ToUniversalTime())} ");
                text.Append(CultureInfo.InvariantCulture, $"{Escape(Subject(entry.Commit))}\n");
                text.Append(CultureInfo.InvariantCulture, $"    https://{row.Identity}/commit/{entry.Commit.Sha}\n");

                if (!string.Equals(entry.Path, change.Path, StringComparison.Ordinal))
                {
                    text.Append(CultureInfo.InvariantCulture, $"    yol: `{change.Path}` -> `{entry.Path}`\n");
                }
            }
        }

        text.Append("\nKarar:\n\n");
        text.Append("[ ]\n\n");
        text.Append("Not:\n\n");
        text.Append("[ ]\n\n");
        text.Append("---\n\n");

        return text.ToString();
    }

    private static string Subject(Commit commit)
    {
        string message = commit.Message ?? string.Empty;
        int newline = message.IndexOf('\n', StringComparison.Ordinal);

        return (newline < 0 ? message : message[..newline]).Trim();
    }

    private static string Escape(string text) =>
        text.Replace("|", "\\|", StringComparison.Ordinal)
            .Replace("`", "'", StringComparison.Ordinal);

    private static string Date(DateTimeOffset value) =>
        value.UtcDateTime.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static Dictionary<string, string> Clones(string list)
    {
        Dictionary<string, string> clones = new(StringComparer.Ordinal);

        foreach (string entry in list.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            string[] parts = entry.Split('=', 2);
            clones[parts[0]] = parts[1];
        }

        return clones;
    }
}
