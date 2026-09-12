using Sievert.Data.Metrics;

namespace Sievert.Tests;

/// <summary>
/// Metrik hesabinin commit sirasi: tarih artan, esitlikte madencilik sirasi artan.
/// Ayni kural <c>MetricsRunner.Read</c> icinde EF tarafinda
/// <c>OrderBy(AuthorDateUtc).ThenBy(Id)</c> olarak duruyor.
/// </summary>
public class CommitOrderingTests
{
    private sealed record Row(string Sha, DateTimeOffset Date, int Sequence);

    [Fact]
    public void Ordering_PutsEarlierDatesFirst()
    {
        List<Row> rows =
        [
            new("c", Day(3), 0),
            new("a", Day(1), 1),
            new("b", Day(2), 2),
        ];

        Assert.Equal(["a", "b", "c"], Apply(rows).Select(row => row.Sha));
    }

    [Fact]
    public void Ordering_BreaksTiesByMiningSequenceNotBySha()
    {
        // Ayni saniye; madencilik sirasi 0, 1, 2 ama SHA sirasi tam tersi.
        List<Row> rows =
        [
            new("zzz", Day(1), 0),
            new("mmm", Day(1), 1),
            new("aaa", Day(1), 2),
        ];

        Assert.Equal(["zzz", "mmm", "aaa"], Apply(rows).Select(row => row.Sha));
        Assert.NotEqual(["aaa", "mmm", "zzz"], Apply(rows).Select(row => row.Sha));
    }

    [Fact]
    public void Ordering_IsStableForTheSameInput()
    {
        List<Row> rows = [new("b", Day(1), 5), new("a", Day(1), 2), new("c", Day(2), 1)];

        Assert.Equal(Apply(rows).Select(row => row.Sha), Apply(rows).Select(row => row.Sha));
    }

    [Fact]
    public void Ordering_DoesNotDependOnTheInputOrder()
    {
        List<Row> one = [new("a", Day(1), 2), new("b", Day(1), 5), new("c", Day(2), 1)];
        List<Row> other = [new("c", Day(2), 1), new("b", Day(1), 5), new("a", Day(1), 2)];

        Assert.Equal(Apply(one).Select(row => row.Sha), Apply(other).Select(row => row.Sha));
    }

    private static List<Row> Apply(List<Row> rows) =>
        CommitOrdering.Apply(rows, row => row.Date, row => row.Sequence);

    private static DateTimeOffset Day(int day) => DateTimeOffset.UnixEpoch.AddDays(day);
}
