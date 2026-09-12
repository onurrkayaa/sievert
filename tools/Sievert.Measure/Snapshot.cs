using System.Globalization;
using System.Text;

using Microsoft.EntityFrameworkCore;

using Sievert.Data;

namespace Sievert.Measure;

/// <summary>
/// Model girdisini veritabanindan deterministik bir CSV anlik goruntusune yaziyor.
/// Urunun parcasi degil: hicbir sey hesaplamiyor, yalnizca kayitli veriyi disari
/// aktariyor ve butunlugunu sayiyor. Asama 5'in butun deneyleri bu tek dosyadan
/// beslenecek; sebebi veritabani degisse bile olculerin ayni veriyle uretilmis olmasi.
///
/// Satir sirasi RepositoryIdentity, AuthorDateUtc, Sha. Ucunun birlikte benzersiz
/// olmasi siralamayi tek bir sonuca kilitliyor, yani ayni veriden ayni checksum cikiyor.
/// </summary>
public static class Snapshot
{
    private sealed record Row(
        string Repository,
        string RepositoryIdentity,
        string Sha,
        DateTimeOffset AuthorDateUtc,
        int LinesAdded,
        int LinesDeleted,
        int FilesChanged,
        int CsFilesChanged,
        double Entropy,
        int DirectoryCount,
        int SubsystemCount,
        int MaxFileAgeDays,
        int MinFileAgeDays,
        int PriorChanges,
        int PriorFixes,
        int DistinctAuthorsOnFiles,
        int AuthorCommitCount,
        int AuthorFileExperience,
        bool IsFix,
        bool IsBugIntroducing,
        string? LabelSource,
        bool IsBot);

    private sealed record RepositorySummary(
        string Identity,
        int Count,
        int Positives,
        DateTimeOffset First,
        DateTimeOffset Last);

    private const string Header =
        "Repository,RepositoryIdentity,Sha,AuthorDateUtc,"
        + "LinesAdded,LinesDeleted,FilesChanged,CsFilesChanged,Entropy,"
        + "DirectoryCount,SubsystemCount,MaxFileAgeDays,MinFileAgeDays,"
        + "PriorChanges,PriorFixes,DistinctAuthorsOnFiles,"
        + "AuthorCommitCount,AuthorFileExperience,IsFix,"
        + "IsBugIntroducing,LabelSource,BotMu";

    public static int Write(SievertContext context, string outputPath)
    {
        List<Row> rows =
        [
            .. context.CommitMetrics
                .AsNoTracking()
                .Join(
                    context.Commits.AsNoTracking(),
                    metric => metric.CommitId,
                    commit => commit.Id,
                    (metric, commit) => new { metric, commit })
                .Join(
                    context.Repositories.AsNoTracking(),
                    pair => pair.commit.RepositoryId,
                    repository => repository.Id,
                    (pair, repository) => new Row(
                        repository.Name,
                        repository.Identity,
                        pair.commit.Sha,
                        pair.commit.AuthorDateUtc,
                        pair.metric.LinesAdded,
                        pair.metric.LinesDeleted,
                        pair.metric.FilesChanged,
                        pair.metric.CsFilesChanged,
                        pair.metric.Entropy,
                        pair.metric.DirectoryCount,
                        pair.metric.SubsystemCount,
                        pair.metric.MaxFileAgeDays,
                        pair.metric.MinFileAgeDays,
                        pair.metric.PriorChanges,
                        pair.metric.PriorFixes,
                        pair.metric.DistinctAuthorsOnFiles,
                        pair.metric.AuthorCommitCount,
                        pair.metric.AuthorFileExperience,
                        pair.metric.IsFix,
                        pair.commit.IsBugIntroducing,
                        pair.commit.LabelSource,
                        pair.commit.IsBot))
        ];

        // Siralama bellekte ve ordinal yapiliyor: veritabaninin harmanlama ayari
        // makineden makineye degisebilir, ordinal karsilastirma degismez.
        rows.Sort(Compare);

        StringBuilder text = new();
        text.Append(Header).Append('\n');

        foreach (Row row in rows)
        {
            text.Append(Field(row.Repository)).Append(',');
            text.Append(Field(row.RepositoryIdentity)).Append(',');
            text.Append(Field(row.Sha)).Append(',');
            text.Append(Field(Date(row.AuthorDateUtc))).Append(',');
            text.Append(Number(row.LinesAdded)).Append(',');
            text.Append(Number(row.LinesDeleted)).Append(',');
            text.Append(Number(row.FilesChanged)).Append(',');
            text.Append(Number(row.CsFilesChanged)).Append(',');
            text.Append(Real(row.Entropy)).Append(',');
            text.Append(Number(row.DirectoryCount)).Append(',');
            text.Append(Number(row.SubsystemCount)).Append(',');
            text.Append(Number(row.MaxFileAgeDays)).Append(',');
            text.Append(Number(row.MinFileAgeDays)).Append(',');
            text.Append(Number(row.PriorChanges)).Append(',');
            text.Append(Number(row.PriorFixes)).Append(',');
            text.Append(Number(row.DistinctAuthorsOnFiles)).Append(',');
            text.Append(Number(row.AuthorCommitCount)).Append(',');
            text.Append(Number(row.AuthorFileExperience)).Append(',');
            text.Append(Flag(row.IsFix)).Append(',');
            text.Append(Flag(row.IsBugIntroducing)).Append(',');
            text.Append(Field(row.LabelSource)).Append(',');
            text.Append(Flag(row.IsBot)).Append('\n');
        }

        // BOM yok: dosyayi okuyan her sey UTF-8 varsayacak ve BOM checksum'a giriyor.
        File.WriteAllText(outputPath, text.ToString(), new UTF8Encoding(false));

        Report(rows, outputPath);
        return 0;
    }

    private static int Compare(Row left, Row right)
    {
        int identity = string.CompareOrdinal(left.RepositoryIdentity, right.RepositoryIdentity);

        if (identity != 0)
        {
            return identity;
        }

        int date = left.AuthorDateUtc.UtcDateTime.CompareTo(right.AuthorDateUtc.UtcDateTime);

        return date != 0 ? date : string.CompareOrdinal(left.Sha, right.Sha);
    }

    private static void Report(List<Row> rows, string outputPath)
    {
        Console.WriteLine($"dosya: {outputPath}");
        Console.WriteLine($"boyut: {new FileInfo(outputPath).Length} bayt");
        Console.WriteLine($"satir: {rows.Count}, pozitif: {rows.Count(row => row.IsBugIntroducing)}");
        Console.WriteLine();

        Console.WriteLine("repo bazinda:");

        // Ozetler donguye girmeden once tek gecisle hesaplaniyor; dongu icinde her repo
        // icin listeyi bastan taramak gereksiz ve aracin kendi SV004 kurali da bunu
        // isaretliyor.
        List<RepositorySummary> summaries =
        [
            .. rows
                .GroupBy(row => row.RepositoryIdentity)
                .Select(group => new RepositorySummary(
                    group.Key,
                    group.Count(),
                    group.Count(row => row.IsBugIntroducing),
                    group.Min(row => row.AuthorDateUtc),
                    group.Max(row => row.AuthorDateUtc)))
                .OrderBy(summary => summary.Identity, StringComparer.Ordinal)
        ];

        foreach (RepositorySummary summary in summaries)
        {
            Console.WriteLine(
                $"  {summary.Identity}: {summary.Positives} / {summary.Count}  "
                + $"{Date(summary.First)} - {Date(summary.Last)}");
        }

        Console.WriteLine();
        Console.WriteLine($"farkli RepositoryIdentity: {rows.Select(row => row.RepositoryIdentity).Distinct().Count()}");

        int duplicates = rows
            .GroupBy(row => (row.RepositoryIdentity, row.Sha))
            .Count(group => group.Count() > 1);

        Console.WriteLine($"tekrarlanan repo+SHA: {duplicates}");
        Console.WriteLine($"UTC olmayan tarih: {rows.Count(row => row.AuthorDateUtc.Offset != TimeSpan.Zero)}");
        Console.WriteLine($"LabelSource dolu: {rows.Count(row => !string.IsNullOrEmpty(row.LabelSource))}");
        Console.WriteLine();

        // Hedef ve butun sayisal oznitelikler veritabaninda NOT NULL; yine de sayiliyor,
        // cunku "olamaz" demek kontrol etmenin yerine gecmiyor.
        Console.WriteLine("oznitelik bazinda NULL / NaN / sonsuz:");

        (string Name, Func<Row, double> Value)[] numeric =
        [
            ("LinesAdded", row => row.LinesAdded),
            ("LinesDeleted", row => row.LinesDeleted),
            ("FilesChanged", row => row.FilesChanged),
            ("CsFilesChanged", row => row.CsFilesChanged),
            ("Entropy", row => row.Entropy),
            ("DirectoryCount", row => row.DirectoryCount),
            ("SubsystemCount", row => row.SubsystemCount),
            ("MaxFileAgeDays", row => row.MaxFileAgeDays),
            ("MinFileAgeDays", row => row.MinFileAgeDays),
            ("PriorChanges", row => row.PriorChanges),
            ("PriorFixes", row => row.PriorFixes),
            ("DistinctAuthorsOnFiles", row => row.DistinctAuthorsOnFiles),
            ("AuthorCommitCount", row => row.AuthorCommitCount),
            ("AuthorFileExperience", row => row.AuthorFileExperience),
            ("IsFix", row => row.IsFix ? 1 : 0),
        ];

        int[] nan = new int[numeric.Length];
        int[] infinite = new int[numeric.Length];

        // Tek gecis: satirlar bir kez dolasilip 15 oznitelik ayni anda sayiliyor.
        foreach (Row row in rows)
        {
            for (int index = 0; index < numeric.Length; index++)
            {
                double value = numeric[index].Value(row);

                if (double.IsNaN(value))
                {
                    nan[index]++;
                }

                if (double.IsInfinity(value))
                {
                    infinite[index]++;
                }
            }
        }

        for (int index = 0; index < numeric.Length; index++)
        {
            Console.WriteLine(
                $"  {numeric[index].Name}: NULL 0, NaN {nan[index]}, sonsuz {infinite[index]}");
        }

        Console.WriteLine($"  IsBugIntroducing (hedef): NULL 0");
    }

    private static string Date(DateTimeOffset value) =>
        value.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);

    private static string Number(int value) =>
        value.ToString(CultureInfo.InvariantCulture);

    private static string Real(double value) =>
        value.ToString("R", CultureInfo.InvariantCulture);

    private static string Flag(bool value) => value ? "1" : "0";

    private static string Field(string? value)
    {
        string text = value ?? string.Empty;

        return text.Contains(',') || text.Contains('"') || text.Contains('\n') || text.Contains('\r')
            ? '"' + text.Replace("\"", "\"\"") + '"'
            : text;
    }
}
