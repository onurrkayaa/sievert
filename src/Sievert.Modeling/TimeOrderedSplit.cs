using System.Globalization;

namespace Sievert.Modeling;

/// <summary>Bolme manifestindeki tek satir.</summary>
public sealed record SplitEntry(
    string RepositoryIdentity,
    string Sha,
    string Split,
    DateTimeOffset AuthorDateUtc)
{
    public const string Train = "train";

    public const string Test = "test";
}

/// <summary>
/// Zaman sirali bolme. Her depo KENDI ICINDE siralaniyor ve ilk %70'i egitim oluyor.
/// Rastgele bolme yok, karistirma yok; gerekcesi ADR 0016'da.
/// </summary>
public static class TimeOrderedSplit
{
    public const double TrainShare = 0.70;

    /// <summary>
    /// Siralama: depo kimligi ordinal, sonra tarih, sonra sha ordinal. Ucu birlikte
    /// benzersiz oldugu icin sonuc tek; ayni girdi her zaman ayni manifesti veriyor.
    /// </summary>
    public static IReadOnlyList<SplitEntry> Apply(IEnumerable<SnapshotRow> rows)
    {
        List<SplitEntry> entries = [];

        foreach (IGrouping<string, SnapshotRow> repository in rows
            .GroupBy(row => row.RepositoryIdentity, StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.Ordinal))
        {
            List<SnapshotRow> ordered =
            [
                .. repository
                    .OrderBy(row => row.AuthorDateUtc.UtcDateTime)
                    .ThenBy(row => row.Sha, StringComparer.Ordinal)
            ];

            // floor: kalan satir her zaman TEST tarafina gidiyor. Yuvarlama tercihi degil,
            // sinirin veri buyudukce kaymamasi icin sabit bir kural (ADR 0016).
            int train = (int)Math.Floor(ordered.Count * TrainShare);

            for (int index = 0; index < ordered.Count; index++)
            {
                entries.Add(new SplitEntry(
                    ordered[index].RepositoryIdentity,
                    ordered[index].Sha,
                    index < train ? SplitEntry.Train : SplitEntry.Test,
                    ordered[index].AuthorDateUtc));
            }
        }

        return entries;
    }
}

/// <summary>Bolme manifestinin metni. Dort alan; oznitelik ya da etiket tasimiyor.</summary>
public static class SplitManifest
{
    public const string Header = "RepositoryIdentity,Sha,Split,AuthorDateUtc";

    public static string Render(IReadOnlyList<SplitEntry> entries)
    {
        System.Text.StringBuilder text = new();
        text.Append(Header).Append('\n');

        foreach (SplitEntry entry in entries)
        {
            text.Append(entry.RepositoryIdentity).Append(',');
            text.Append(entry.Sha).Append(',');
            text.Append(entry.Split).Append(',');
            text.Append(entry.AuthorDateUtc.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture));
            text.Append('\n');
        }

        return text.ToString();
    }

    /// <summary>BOM yok: ozet degeri BOM'dan etkilenmesin.</summary>
    public static void Write(string path, IReadOnlyList<SplitEntry> entries) =>
        File.WriteAllText(path, Render(entries), new System.Text.UTF8Encoding(false));
}
