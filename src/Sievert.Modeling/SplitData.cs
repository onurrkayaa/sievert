using System.Globalization;

namespace Sievert.Modeling;

/// <summary>
/// Dondurulmus bolme manifestini okur. Bolme burada yeniden hesaplanmiyor: manifest
/// Adim 1'de uretildi, ozeti kayitli ve sonraki butun olcumler ayni dosyayi okumali.
/// Yeniden hesaplamak, bir gun kod degistiginde sessizce baska bir bolmeye gecmek olurdu.
/// </summary>
public static class SplitManifestReader
{
    public static readonly string[] Columns = ["RepositoryIdentity", "Sha", "Split", "AuthorDateUtc"];

    public static IReadOnlyList<SplitEntry> ReadVerified(string path, string checksumPath)
    {
        FileChecksum.Verify(path, checksumPath);

        return Read(path);
    }

    public static IReadOnlyList<SplitEntry> Read(string path)
    {
        using StreamReader reader = new(path);

        string? header = reader.ReadLine()
            ?? throw new InvalidDataException($"Manifest bos: {path}");

        if (!header.Split(',').SequenceEqual(Columns, StringComparer.Ordinal))
        {
            throw new InvalidDataException(
                $"Manifest basligi beklenenden farkli: {path}{Environment.NewLine}"
                + $"  beklenen: {string.Join(',', Columns)}{Environment.NewLine}"
                + $"  okunan  : {header}");
        }

        List<SplitEntry> entries = [];
        int number = 1;

        while (reader.ReadLine() is string line)
        {
            number++;

            if (line.Length == 0)
            {
                continue;
            }

            string[] fields = line.Split(',');

            if (fields.Length != Columns.Length)
            {
                throw new InvalidDataException(
                    $"{path}:{number} satirinda {Columns.Length} alan olmali, {fields.Length} alan var.");
            }

            if (fields[2] is not (SplitEntry.Train or SplitEntry.Test))
            {
                throw new InvalidDataException(
                    $"{path}:{number} satirinda Split alani '{fields[2]}'; "
                    + $"yalnizca '{SplitEntry.Train}' ya da '{SplitEntry.Test}' olabilir.");
            }

            entries.Add(new SplitEntry(
                fields[0],
                fields[1],
                fields[2],
                DateTimeOffset.Parse(
                    fields[3],
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal)));
        }

        return entries;
    }
}

/// <summary>Anlik goruntu satirlarini manifeste gore depo depo ikiye ayirir.</summary>
public static class SplitData
{
    /// <summary>
    /// Depolar ordinal siralaniyor; satirlar manifest sirasini koruyor. Anlik goruntude
    /// olup manifestte olmayan bir satir hata: iki dosya ayni kumeyi anlatmali.
    /// </summary>
    public static IReadOnlyList<RepositorySplit> Build(
        IReadOnlyList<SnapshotRow> rows,
        IReadOnlyList<SplitEntry> manifest)
    {
        Dictionary<string, SnapshotRow> byKey = new(StringComparer.Ordinal);

        foreach (SnapshotRow row in rows)
        {
            byKey[Key(row.RepositoryIdentity, row.Sha)] = row;
        }

        Dictionary<string, (List<SnapshotRow> Train, List<SnapshotRow> Test)> sides =
            new(StringComparer.Ordinal);
        int matched = 0;

        foreach (SplitEntry entry in manifest)
        {
            if (!byKey.TryGetValue(Key(entry.RepositoryIdentity, entry.Sha), out SnapshotRow? row))
            {
                throw new InvalidDataException(
                    $"Manifestte olup anlik goruntude olmayan satir: {entry.RepositoryIdentity} {entry.Sha}");
            }

            if (!sides.TryGetValue(entry.RepositoryIdentity, out (List<SnapshotRow>, List<SnapshotRow>) side))
            {
                side = ([], []);
                sides[entry.RepositoryIdentity] = side;
            }

            (entry.Split == SplitEntry.Train ? side.Item1 : side.Item2).Add(row);
            matched++;
        }

        if (matched != rows.Count)
        {
            throw new InvalidDataException(
                $"Anlik goruntude {rows.Count} satir var, manifest {matched} tanesini gosteriyor.");
        }

        return
        [
            .. sides
                .OrderBy(entry => entry.Key, StringComparer.Ordinal)
                .Select(entry => new RepositorySplit(entry.Key, entry.Value.Train, entry.Value.Test))
        ];
    }

    private static string Key(string identity, string sha) => identity + " " + sha;
}

/// <summary>Mikro ve makro toplama. Ikisi ayni sey degil (metrik sozlesmesi, surum 1.0).</summary>
public static class Totals
{
    /// <summary>Butun depolarin sayimi tek havuzda toplaniyor; buyuk repo agir basiyor.</summary>
    public static Confusion Micro(IEnumerable<Confusion> parts)
    {
        int truePositives = 0;
        int falsePositives = 0;
        int falseNegatives = 0;
        int trueNegatives = 0;

        foreach (Confusion part in parts)
        {
            truePositives += part.TruePositives;
            falsePositives += part.FalsePositives;
            falseNegatives += part.FalseNegatives;
            trueNegatives += part.TrueNegatives;
        }

        return new Confusion(truePositives, falsePositives, falseNegatives, trueNegatives);
    }

    /// <summary>Depo F1'lerinin basit ortalamasi; her repo esit agirlikli.</summary>
    public static double? MacroF1(IEnumerable<double?> scores)
    {
        double total = 0.0;
        int count = 0;

        foreach (double? score in scores)
        {
            if (score is not double value)
            {
                return null;
            }

            total += value;
            count++;
        }

        return count == 0 ? null : total / count;
    }
}
