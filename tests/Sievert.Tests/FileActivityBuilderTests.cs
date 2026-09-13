using Sievert.Api.Visualizations;
using Sievert.Contracts;

namespace Sievert.Tests;

/// <summary>
/// Dosya etkinlik toplamasi.
///
/// Veritabani yok: burada sinanan sey sorgu degil, sayinin nasil kuruldugu. Ayni commit
/// ayni dosya icin iki satir yazmissa kac dokunus sayilir, esit ortalamada hangi dosya
/// once gelir, "son dokunus" hangisidir - hepsi saf hesap.
/// </summary>
public sealed class FileActivityBuilderTests
{
    private static readonly DateTimeOffset Start = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void OneCommitTouchingOneFileIsOneTouch()
    {
        List<FileActivityItem> items = Build(
            [Commit(1, riskIndex: 40)],
            [new WindowFile(1, "src/A.cs", 10, 4)]);

        FileActivityItem item = Assert.Single(items);

        Assert.Equal("src/A.cs", item.RelativePath);
        Assert.Equal(1, item.TouchCount);
        Assert.Equal(10, item.TotalLinesAdded);
        Assert.Equal(4, item.TotalLinesDeleted);
        Assert.Equal(14, item.TotalChurn);
        Assert.Equal(40, item.MeanRiskIndex);
        Assert.Equal(40, item.MaxRiskIndex);
        Assert.Equal(40, item.LatestRiskIndex);
    }

    /// <summary>
    /// Ayni commit ayni yol icin iki satir yazmissa bu **tek** dokunus.
    ///
    /// Iki sayilsaydi o commit'in endeksi ortalamaya iki kez girer ve dosya hic olmadigi
    /// kadar riskli gorunurdu. Satirlarin degerleri yine toplaniyor; kaybolan bir sey yok.
    /// </summary>
    [Fact]
    public void TwoRowsForTheSameCommitAndPathCountAsOneTouch()
    {
        List<FileActivityItem> items = Build(
            [Commit(1, riskIndex: 90), Commit(2, riskIndex: 10)],
            [
                new WindowFile(1, "src/A.cs", 10, 1),
                new WindowFile(1, "src/A.cs", 5, 2),
                new WindowFile(2, "src/A.cs", 1, 1),
            ]);

        FileActivityItem item = Assert.Single(items);

        Assert.Equal(2, item.TouchCount);
        Assert.Equal(16, item.TotalLinesAdded);
        Assert.Equal(4, item.TotalLinesDeleted);

        // 90 ve 10; 90 iki kez sayilsaydi ortalama 63,3 cikardi.
        Assert.Equal(50, item.MeanRiskIndex);
    }

    [Fact]
    public void MeanMaxAndLatestAreThreeDifferentNumbers()
    {
        List<FileActivityItem> items = Build(
            [Commit(1, riskIndex: 10), Commit(2, riskIndex: 100), Commit(3, riskIndex: 40)],
            [
                new WindowFile(1, "src/A.cs", 1, 0),
                new WindowFile(2, "src/A.cs", 1, 0),
                new WindowFile(3, "src/A.cs", 1, 0),
            ]);

        FileActivityItem item = Assert.Single(items);

        Assert.Equal(50, item.MeanRiskIndex);
        Assert.Equal(100, item.MaxRiskIndex);

        // En yeni commit 3; onun endeksi 40.
        Assert.Equal(40, item.LatestRiskIndex);
        Assert.Equal(Sha(3), item.LatestCommitSha);
    }

    [Fact]
    public void OnlyTheFilesOfTheGivenWindowAreCounted()
    {
        // Pencerede yalniz commit 1 var; commit 9 disarida.
        List<FileActivityItem> items = Build(
            [Commit(1, riskIndex: 20)],
            [new WindowFile(1, "src/A.cs", 1, 0), new WindowFile(9, "src/A.cs", 100, 100)]);

        FileActivityItem item = Assert.Single(items);

        Assert.Equal(1, item.TouchCount);
        Assert.Equal(1, item.TotalChurn);
    }

    [Fact]
    public void OnlyTheFirstFilesSurviveTheLimit()
    {
        List<WindowCommit> commits = [Commit(1, riskIndex: 50)];
        List<WindowFile> files = [];

        for (int index = 0; index < 30; index++)
        {
            files.Add(new WindowFile(1, $"src/F{index:00}.cs", index, 0));
        }

        List<FileActivityItem> items = FileActivityBuilder.Build(
            commits, files, limit: 10, FileActivitySort.ChurnDescending, null, null, out int before);

        Assert.Equal(30, before);
        Assert.Equal(10, items.Count);
        Assert.Equal(29, items[0].TotalChurn);
    }

    public static TheoryData<string, string> Sorted()
    {
        TheoryData<string, string> data = [];

        data.Add(FileActivitySort.MeanRiskDescending, "src/Mean.cs");
        data.Add(FileActivitySort.MaxRiskDescending, "src/Max.cs");
        data.Add(FileActivitySort.LatestRiskDescending, "src/Latest.cs");
        data.Add(FileActivitySort.ChurnDescending, "src/Churn.cs");
        data.Add(FileActivitySort.TouchCountDescending, "src/Touch.cs");
        data.Add(FileActivitySort.PathAscending, "src/Churn.cs");

        return data;
    }

    [Theory]
    [MemberData(nameof(Sorted))]
    public void EverySortPutsItsOwnWinnerFirst(string sort, string expected)
    {
        // Her dosya bir olcude kazansin diye kume elle kuruldu: en yuksek ortalama,
        // en yuksek maksimum, en yuksek son dokunus, en cok churn ve en cok dokunus
        // farkli dosyalarda.
        List<WindowCommit> commits =
        [
            Commit(2, riskIndex: 5),
            Commit(3, riskIndex: 99),
            Commit(4, riskIndex: 60),
            Commit(5, riskIndex: 85),
            Commit(6, riskIndex: 95),
            Commit(7, riskIndex: 2),
            Commit(8, riskIndex: 3),
        ];

        List<WindowFile> files =
        [
            // Ortalamasi en yuksek: tek dokunus, 85.
            new WindowFile(5, "src/Mean.cs", 1, 0),

            // Maksimumu en yuksek (99) ama son dokunusu dusuk (2).
            new WindowFile(3, "src/Max.cs", 1, 0),
            new WindowFile(7, "src/Max.cs", 1, 0),

            // Son dokunusu en yuksek (95).
            new WindowFile(6, "src/Latest.cs", 1, 0),
            new WindowFile(2, "src/Latest.cs", 1, 0),

            // Churn'u en yuksek, endeksi dusuk.
            new WindowFile(2, "src/Churn.cs", 500, 500),

            // Dokunusu en cok, butun olculeri dusuk.
            new WindowFile(7, "src/Touch.cs", 1, 0),
            new WindowFile(8, "src/Touch.cs", 1, 0),
            new WindowFile(2, "src/Touch.cs", 1, 0),
            new WindowFile(4, "src/Touch.cs", 1, 0),
        ];

        List<FileActivityItem> items = FileActivityBuilder.Build(
            commits, files, limit: 100, sort, null, null, out int _);

        Assert.Equal(expected, items[0].RelativePath);
    }

    /// <summary>
    /// Esitlikte sira sabit: maksimum endeks, sonra dokunus, sonra yol. Ayni veri her
    /// kosuda ayni sirayi vermeli, yoksa iki ekran goruntusu karsilastirilamaz.
    /// </summary>
    [Fact]
    public void TiesAreBrokenDeterministically()
    {
        List<WindowCommit> commits = [Commit(1, riskIndex: 50)];

        List<WindowFile> files =
        [
            new WindowFile(1, "src/Zeta.cs", 1, 0),
            new WindowFile(1, "src/Alpha.cs", 1, 0),
            new WindowFile(1, "src/Mid.cs", 1, 0),
        ];

        for (int run = 0; run < 5; run++)
        {
            List<FileActivityItem> items = FileActivityBuilder.Build(
                commits, files, limit: 100, FileActivitySort.MeanRiskDescending, null, null, out int _);

            Assert.Equal(
                ["src/Alpha.cs", "src/Mid.cs", "src/Zeta.cs"],
                items.Select(item => item.RelativePath));
        }
    }

    [Fact]
    public void OnlyTheLastFiveTouchesAreCarried()
    {
        List<WindowCommit> commits = [];
        List<WindowFile> files = [];

        for (int index = 1; index <= 9; index++)
        {
            commits.Add(Commit(index, riskIndex: index * 10));
            files.Add(new WindowFile(index, "src/A.cs", 1, 0));
        }

        FileActivityItem item = Assert.Single(Build(commits, files));

        Assert.Equal(9, item.TouchCount);
        Assert.Equal(5, item.Commits.Count);

        // En yeniden eskiye.
        Assert.Equal(Sha(9), item.Commits[0].Sha);
        Assert.Equal(Sha(5), item.Commits[^1].Sha);
    }

    /// <summary>
    /// Statik bulgu sayisi renge girmiyor ve varsayilan sirayi degistirmiyor.
    ///
    /// Bu bir suslemenin degil, risk sozlesmesinin geregi: statik bulgular model skoruna
    /// dahil degil, dolayisiyla skordan turetilen renge de dahil degil.
    /// </summary>
    [Fact]
    public void StaticFindingsChangeNeitherTheColourValueNorTheOrder()
    {
        List<WindowCommit> commits = [Commit(1, riskIndex: 30), Commit(2, riskIndex: 80)];

        List<WindowFile> files =
        [
            new WindowFile(1, "src/Low.cs", 1, 0),
            new WindowFile(2, "src/High.cs", 1, 0),
        ];

        List<FileActivityItem> without = FileActivityBuilder.Build(
            commits, files, 100, FileActivitySort.MeanRiskDescending, null, null, out int _);

        Dictionary<string, int> counts = new(StringComparer.Ordinal)
        {
            ["src/Low.cs"] = 99,
            ["src/High.cs"] = 0,
        };

        Guid staticJob = Guid.Parse("01a09990-0000-7000-8000-0000000000ff");

        List<FileActivityItem> with = FileActivityBuilder.Build(
            commits, files, 100, FileActivitySort.MeanRiskDescending, counts, staticJob, out int _);

        Assert.Equal(
            without.Select(item => item.RelativePath),
            with.Select(item => item.RelativePath));

        Assert.Equal(
            without.Select(item => item.MeanRiskIndex),
            with.Select(item => item.MeanRiskIndex));

        Assert.All(with, item => Assert.False(item.StaticFindingsIncludedInColor));
        Assert.Equal(99, with.First(item => item.RelativePath == "src/Low.cs").StaticFindingCount);
        Assert.Equal(staticJob, with[0].StaticFindingSourceJobId);

        // Ustuste bindirme yoksa sayi null - "0 bulgu" ile "bakilmadi" ayni sey degil.
        Assert.All(without, item => Assert.Null(item.StaticFindingCount));
    }

    [Fact]
    public void BackslashesAreNormalisedButCaseIsNot()
    {
        Assert.Equal("src/A.cs", FileActivityBuilder.Normalise("src\\A.cs"));

        List<FileActivityItem> items = Build(
            [Commit(1, riskIndex: 10)],
            [new WindowFile(1, "src/A.cs", 1, 0), new WindowFile(1, "src/a.cs", 1, 0)]);

        // Iki yol ayri kaliyor: sessizce birlestirmek, veritabaninda ayri duran iki
        // dosyayi tek satir gostermek olurdu.
        Assert.Equal(2, items.Count);
    }

    private static List<FileActivityItem> Build(
        IReadOnlyList<WindowCommit> commits,
        IReadOnlyList<WindowFile> files) =>
        FileActivityBuilder.Build(
            commits, files, limit: 100, FileActivitySort.MeanRiskDescending, null, null, out int _);

    private static string Sha(int index) => index.ToString("x8") + new string('c', 32);

    private static WindowCommit Commit(int id, double riskIndex) => new(
        id,
        Sha(id),
        Start.AddHours(id),
        "commit " + id,
        riskIndex / 100.0,
        riskIndex,
        riskIndex >= 50,
        riskIndex >= 30,
        0.2381,
        "[]",
        IsBugIntroducing: false,
        IsBot: false,
        1,
        1,
        1,
        1,
        IsFix: false);
}
