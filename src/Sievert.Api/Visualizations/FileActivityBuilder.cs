using Sievert.Contracts;

namespace Sievert.Api.Visualizations;

/// <summary>
/// Pencere satirlarindan dosya etkinlik haritasini kurar.
///
/// Veritabanindan ayri duruyor cunku burada karar verilen seyler veritabani sorusu degil:
/// ayni commit ayni dosya icin iki satir yazmissa bu kac dokunus sayilir, esit ortalamada
/// hangi dosya once gelir, "son dokunus" hangisidir. Bunlarin hepsi saf hesap ve
/// veritabani olmadan sinanabilmeli.
/// </summary>
public static class FileActivityBuilder
{
    /// <param name="commits">Pencere, kronolojik (eski -> yeni).</param>
    /// <param name="files">Ayni penceredeki C# dosya satirlari.</param>
    /// <param name="staticCounts">Yola gore statik bulgu sayisi; ustuste bindirme yoksa null.</param>
    public static List<FileActivityItem> Build(
        IReadOnlyList<WindowCommit> commits,
        IReadOnlyList<WindowFile> files,
        int limit,
        string sort,
        IReadOnlyDictionary<string, int>? staticCounts,
        Guid? staticJobId,
        out int fileCountBeforeLimit)
    {
        Dictionary<int, WindowCommit> byId = commits.ToDictionary(commit => commit.CommitId);

        // Once (commit, dosya) tekillestirmesi.
        //
        // Ayni commit ayni yol icin birden fazla satir yazmissa bu TEK dokunus. Aksi
        // halde o commit'in endeksi ortalamaya iki kez girer ve dosya, hic olmadigi kadar
        // riskli gorunur.
        Dictionary<(int Commit, string Path), Touch> touches = [];

        foreach (WindowFile file in files)
        {
            if (!byId.TryGetValue(file.CommitId, out WindowCommit? commit))
            {
                continue;
            }

            (int, string) key = (file.CommitId, file.Path);

            touches[key] = touches.TryGetValue(key, out Touch? existing)
                ? existing with
                {
                    LinesAdded = existing.LinesAdded + file.LinesAdded,
                    LinesDeleted = existing.LinesDeleted + file.LinesDeleted,
                }
                : new Touch(commit, Normalise(file.Path), file.LinesAdded, file.LinesDeleted);
        }

        Dictionary<string, List<Touch>> byPath = [];

        foreach (Touch touch in touches.Values)
        {
            if (!byPath.TryGetValue(touch.Path, out List<Touch>? list))
            {
                byPath[touch.Path] = list = [];
            }

            list.Add(touch);
        }

        fileCountBeforeLimit = byPath.Count;

        List<FileActivityItem> items = [];

        foreach ((string path, List<Touch> list) in byPath)
        {
            // En yeni once; "son dokunus" ve "son bes commit" ayni siradan cikiyor.
            list.Sort(Newest);

            Touch latest = list[0];

            // Dort ayri LINQ cagrisi yerine tek gecis: listeyi dort kez dolasmanin
            // anlami yok ve kural kontrolu de bunu dongu icinde sorgu saniyordu.
            int added = 0;
            int deleted = 0;
            double indexSum = 0;
            double indexMax = double.MinValue;

            foreach (Touch touch in list)
            {
                added += touch.LinesAdded;
                deleted += touch.LinesDeleted;
                indexSum += touch.Commit.RiskIndex;
                indexMax = Math.Max(indexMax, touch.Commit.RiskIndex);
            }

            items.Add(new FileActivityItem(
                path,
                list.Count,
                added,
                deleted,
                added + deleted,
                Round(indexSum / list.Count),
                indexMax,
                latest.Commit.RiskIndex,
                latest.Commit.Sha,
                latest.Commit.AuthorDateUtc,
                staticCounts is null ? null : staticCounts.GetValueOrDefault(path, 0),
                staticCounts is null ? null : staticJobId,

                // Her zaman false: statik bulgular model skoruna girmiyor, dolayisiyla
                // renge de girmiyor (risk sozlesmesi 1.0).
                StaticFindingsIncludedInColor: false,
                [.. list.Take(VisualizationLimits.RecentCommitsPerFile).Select(Describe)]));
        }

        items.Sort(Comparer(sort));

        return [.. items.Take(limit)];
    }

    /// <summary>
    /// Yol normalizasyonu: yalniz ayirici. Buyuk/kucuk harf DOKUNULMUYOR - iki yolu
    /// sessizce birlestirmek, veritabaninda ayri duran iki dosyayi tek satir gostermek
    /// olurdu.
    /// </summary>
    public static string Normalise(string path) => path.Replace('\\', '/');

    /// <summary>
    /// Ortalama tek ondalikli.
    ///
    /// <c>RiskIndex</c>'in kendisi tek ondalikli (mid-rank yuzdelik); ortalamayi daha
    /// hassas gostermek, olculenden fazlasini biliyormus gibi yapmak olurdu.
    /// </summary>
    private static double Round(double value) => Math.Round(value, 1, MidpointRounding.AwayFromZero);

    private static int Newest(Touch left, Touch right)
    {
        int date = right.Commit.AuthorDateUtc.CompareTo(left.Commit.AuthorDateUtc);

        return date != 0 ? date : right.Commit.CommitId.CompareTo(left.Commit.CommitId);
    }

    private static FileActivityCommit Describe(Touch touch) => new(
        touch.Commit.Sha,
        touch.Commit.ShortSha,
        touch.Commit.AuthorDateUtc,
        touch.Commit.MessageSubject,
        touch.Commit.RiskIndex,
        touch.Commit.RawModelScore,
        touch.LinesAdded,
        touch.LinesDeleted);

    /// <summary>
    /// Siralama.
    ///
    /// Esitlikte kural sabit: maksimum endeks, sonra dokunus sayisi, sonra yol. Yol en
    /// sonda ve ordinal karsilastirmayla; boylece ayni veri her kosuda ayni sirayi
    /// veriyor ve iki ekran goruntusu karsilastirilabiliyor.
    /// </summary>
    private static Comparison<FileActivityItem> Comparer(string sort) => sort switch
    {
        FileActivitySort.MaxRiskDescending => (left, right) =>
            Then(right.MaxRiskIndex.CompareTo(left.MaxRiskIndex), left, right),
        FileActivitySort.LatestRiskDescending => (left, right) =>
            Then(right.LatestRiskIndex.CompareTo(left.LatestRiskIndex), left, right),
        FileActivitySort.ChurnDescending => (left, right) =>
            Then(right.TotalChurn.CompareTo(left.TotalChurn), left, right),
        FileActivitySort.TouchCountDescending => (left, right) =>
            Then(right.TouchCount.CompareTo(left.TouchCount), left, right),
        FileActivitySort.PathAscending => (left, right) =>
            string.CompareOrdinal(left.RelativePath, right.RelativePath),
        _ => (left, right) => Then(right.MeanRiskIndex.CompareTo(left.MeanRiskIndex), left, right),
    };

    private static int Then(int primary, FileActivityItem left, FileActivityItem right)
    {
        if (primary != 0)
        {
            return primary;
        }

        int max = right.MaxRiskIndex.CompareTo(left.MaxRiskIndex);

        if (max != 0)
        {
            return max;
        }

        int touches = right.TouchCount.CompareTo(left.TouchCount);

        return touches != 0 ? touches : string.CompareOrdinal(left.RelativePath, right.RelativePath);
    }

    private sealed record Touch(WindowCommit Commit, string Path, int LinesAdded, int LinesDeleted);
}
