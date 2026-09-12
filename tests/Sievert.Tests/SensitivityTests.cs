using Sievert.Modeling;

namespace Sievert.Tests;

/// <summary>Adim 4 duyarlilik deneylerinin testleri.</summary>
public class SensitivityTests
{
    [Theory]
    [InlineData(89, false)]
    [InlineData(90, true)]
    [InlineData(91, true)]
    public void Maturity_TheNinetyDayBoundaryIsInclusive(int days, bool mature)
    {
        DateTimeOffset last = DateTimeOffset.UnixEpoch.AddDays(1000);
        int measured = Sensitivity.Maturity(last, last.AddDays(-days));

        Assert.Equal(days, measured);
        Assert.Equal(mature, measured >= Sensitivity.MaturityDays);
    }

    [Fact]
    public void Flip_OnlyTouchesNegativeRowsThatChangedCsharpFiles()
    {
        IReadOnlyList<SnapshotRow> rows =
        [
            Row(sha: "a", positive: false, csFiles: 3),
            Row(sha: "b", positive: false, csFiles: 0),
            Row(sha: "c", positive: true, csFiles: 3),
        ];

        // Oran 1,0: butun adaylar cevriliyor.
        IReadOnlyList<SnapshotRow> flipped = Sensitivity.Flip(rows, 1.0, new Random(20260912));

        Assert.True(flipped[0].IsBugIntroducing);
        Assert.False(flipped[1].IsBugIntroducing);
        Assert.True(flipped[2].IsBugIntroducing);
        Assert.Equal("sentetik", flipped[0].LabelSource);
        Assert.Equal("szz", flipped[2].LabelSource);
    }

    [Fact]
    public void Flip_LeavesTheOriginalRowsUntouched()
    {
        IReadOnlyList<SnapshotRow> rows = [Row(sha: "a", positive: false, csFiles: 3)];

        _ = Sensitivity.Flip(rows, 1.0, new Random(20260912));

        Assert.False(rows[0].IsBugIntroducing);
        Assert.Null(rows[0].LabelSource);
    }

    [Fact]
    public void Flip_IsDeterministicForTheSameSeed()
    {
        IReadOnlyList<SnapshotRow> rows = Many(200);

        IReadOnlyList<SnapshotRow> first = Sensitivity.Flip(rows, 0.1, new Random(20260912));
        IReadOnlyList<SnapshotRow> second = Sensitivity.Flip(rows, 0.1, new Random(20260912));

        Assert.Equal(
            first.Select(row => row.IsBugIntroducing),
            second.Select(row => row.IsBugIntroducing));
    }

    [Fact]
    public void Flip_ChangesWithADifferentSeed()
    {
        IReadOnlyList<SnapshotRow> rows = Many(200);

        Assert.NotEqual(
            Sensitivity.Flip(rows, 0.1, new Random(20260912)).Select(row => row.IsBugIntroducing),
            Sensitivity.Flip(rows, 0.1, new Random(20260913)).Select(row => row.IsBugIntroducing));
    }

    [Theory]
    [InlineData(0.05)]
    [InlineData(0.10)]
    [InlineData(0.20)]
    public void Flip_TurnsRoughlyTheDeclaredShareOfCandidates(double rate)
    {
        IReadOnlyList<SnapshotRow> rows = Many(4000);
        int candidates = Sensitivity.Candidates(rows);

        IReadOnlyList<SnapshotRow> flipped = Sensitivity.Flip(rows, rate, new Random(20260912));
        int added = flipped.Count(row => row.IsBugIntroducing) - rows.Count(row => row.IsBugIntroducing);

        Assert.InRange(added, candidates * rate * 0.8, candidates * rate * 1.2);
    }

    [Fact]
    public void Ranges_CountBelowAndAboveSeparately()
    {
        // Egitim satirlarinin hepsinde deger degisiyor; sifir varyans kontrolu gecsin diye
        // kucuk bir kume degil, uretilmis bir kume kullaniliyor. LinesAdded 1 ile 60 arasi.
        FeatureScaler scaler = FeatureScaler.Fit("a/b", Many(60));

        IReadOnlyList<SnapshotRow> test =
        [
            Row(sha: "s1", linesAdded: 0, day: 10),
            Row(sha: "s2", linesAdded: 500, day: 10),
            Row(sha: "s3", linesAdded: 30, day: 10),
        ];

        RangeCount count = Sensitivity.Ranges(scaler, test).Single(entry => entry.Feature == "LinesAdded");

        Assert.Equal(1, count.BelowMinimum);
        Assert.Equal(1, count.AboveMaximum);
    }

    [Fact]
    public void Ablation_UsesExactlyFourteenFeatures()
    {
        IReadOnlyList<string> ablated =
            [.. ModelFeatures.Candidates.Where(name => name != "CsFilesChanged")];

        Assert.Equal(14, ablated.Count);
        Assert.DoesNotContain("CsFilesChanged", ablated);

        FeatureScaler scaler = FeatureScaler.Fit("a/b", Many(60), ablated);

        Assert.Equal(14, scaler.Apply(Many(1)[0]).Length);
        Assert.DoesNotContain(scaler.Statistics, entry => entry.Name == "CsFilesChanged");
    }

    [Fact]
    public void CsharpSubset_ContainsNoRowWithoutCsharpFiles()
    {
        IReadOnlyList<SnapshotRow> rows = Many(200);
        IReadOnlyList<SnapshotRow> subset = [.. rows.Where(row => row.CsFilesChanged > 0)];

        Assert.NotEmpty(subset);
        Assert.All(subset, row => Assert.True(row.CsFilesChanged > 0));
    }

    [Fact]
    public void Evaluate_KeepsTheGivenThresholdAndCountsPositives()
    {
        List<(SnapshotRow, double)> rows =
        [
            (Row(sha: "a", positive: true), 0.9),
            (Row(sha: "b", positive: false), 0.4),
            (Row(sha: "c", positive: true), 0.2),
        ];

        SubsetOutcome outcome = Sensitivity.Evaluate("deney", rows, threshold: 0.5);

        Assert.Equal(3, outcome.Rows);
        Assert.Equal(2, outcome.Positives);
        Assert.Equal(0.5, outcome.Threshold);
        Assert.Equal(1, outcome.Counts.TruePositives);
        Assert.Equal(1, outcome.Counts.FalseNegatives);
    }

    private static IReadOnlyList<SnapshotRow> Many(int count)
    {
        List<SnapshotRow> rows = [];

        for (int index = 0; index < count; index++)
        {
            rows.Add(Row(
                sha: "r" + index,
                positive: index % 7 == 0,
                csFiles: index % 3,
                linesAdded: index + 1,
                day: index));
        }

        return rows;
    }

    private static SnapshotRow Row(
        string sha,
        bool positive = false,
        int csFiles = 1,
        int linesAdded = 10,
        int day = 0) =>
        new("klasor", "a/b", sha, DateTimeOffset.UnixEpoch.AddDays(day), linesAdded, day + 1,
            (day % 5) + 1, csFiles, (day % 4) * 0.5, (day % 6) + 1, (day % 2) + 1, day * 3, day,
            (day % 7) + 1, day % 4, (day % 5) + 1, day * 2, (day % 6) + 1,
            positive, positive, positive ? "szz" : null, false);
}
