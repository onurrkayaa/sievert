using Sievert.Data;
using Sievert.Data.Entities;

namespace Sievert.Tests;

/// <summary>
/// Durum makinesinin testleri. Gecerli gecisler kadar gecersizler de sayiliyor: bir
/// durum makinesinin degeri, izin verdiklerinden cok reddettiklerinde.
/// </summary>
public sealed class AnalysisJobTransitionTests
{
    public static TheoryData<AnalysisJobStatus, AnalysisJobStatus> Allowed()
    {
        TheoryData<AnalysisJobStatus, AnalysisJobStatus> data = [];

        data.Add(AnalysisJobStatus.Queued, AnalysisJobStatus.Running);
        data.Add(AnalysisJobStatus.Queued, AnalysisJobStatus.Canceled);
        data.Add(AnalysisJobStatus.Running, AnalysisJobStatus.Succeeded);
        data.Add(AnalysisJobStatus.Running, AnalysisJobStatus.Failed);
        data.Add(AnalysisJobStatus.Running, AnalysisJobStatus.Canceled);

        return data;
    }

    public static TheoryData<AnalysisJobStatus, AnalysisJobStatus> Refused()
    {
        TheoryData<AnalysisJobStatus, AnalysisJobStatus> data = [];

        // Terminal durumdan hicbir yere gidilemez, ayni duruma bile.
        foreach (AnalysisJobStatus terminal in (AnalysisJobStatus[])
        [
            AnalysisJobStatus.Succeeded,
            AnalysisJobStatus.Failed,
            AnalysisJobStatus.Canceled,
        ])
        {
            foreach (AnalysisJobStatus target in Enum.GetValues<AnalysisJobStatus>())
            {
                data.Add(terminal, target);
            }
        }

        // Kuyruktan dogrudan basariya gidilemez: once running olmali.
        data.Add(AnalysisJobStatus.Queued, AnalysisJobStatus.Succeeded);

        // Kuyruktaki is basarisiz da yapilamaz. Adim 3'te bu gecis listedeydi ama hicbir
        // yerden cagrilmiyordu; cagiran yeri olmayan bir izin, hic denenmemis bir isi
        // denenmis gostermenin acik kapisi.
        data.Add(AnalysisJobStatus.Queued, AnalysisJobStatus.Failed);
        data.Add(AnalysisJobStatus.Queued, AnalysisJobStatus.Queued);
        data.Add(AnalysisJobStatus.Running, AnalysisJobStatus.Queued);
        data.Add(AnalysisJobStatus.Running, AnalysisJobStatus.Running);

        return data;
    }

    [Theory]
    [MemberData(nameof(Allowed))]
    public void TheValidTransitionsAreAccepted(AnalysisJobStatus from, AnalysisJobStatus to)
    {
        Assert.True(AnalysisJobTransitions.IsAllowed(from, to), $"{from} -> {to} reddedildi.");
    }

    [Theory]
    [MemberData(nameof(Refused))]
    public void TheInvalidTransitionsAreRefused(AnalysisJobStatus from, AnalysisJobStatus to)
    {
        Assert.False(AnalysisJobTransitions.IsAllowed(from, to), $"{from} -> {to} kabul edildi.");
    }

    [Fact]
    public void EveryPairIsEitherAllowedOrRefusedButNeverBoth()
    {
        // Iki listenin cakismadigini sinar; biri degisince digeri sessizce eskimesin.
        HashSet<(AnalysisJobStatus, AnalysisJobStatus)> allowed = [];

        foreach (object?[] row in Allowed())
        {
            allowed.Add(((AnalysisJobStatus)row[0]!, (AnalysisJobStatus)row[1]!));
        }

        foreach (object?[] row in Refused())
        {
            Assert.DoesNotContain(((AnalysisJobStatus)row[0]!, (AnalysisJobStatus)row[1]!), allowed);
        }
    }

    [Theory]
    [InlineData(AnalysisJobStatus.Succeeded)]
    [InlineData(AnalysisJobStatus.Failed)]
    [InlineData(AnalysisJobStatus.Canceled)]
    public void TerminalStatusesAreRecognised(AnalysisJobStatus status)
    {
        Assert.True(AnalysisJobTransitions.IsTerminal(status));
        Assert.False(AnalysisJobRow.IsActive(status));
    }

    [Theory]
    [InlineData(AnalysisJobStatus.Queued)]
    [InlineData(AnalysisJobStatus.Running)]
    public void ActiveStatusesAreRecognised(AnalysisJobStatus status)
    {
        Assert.False(AnalysisJobTransitions.IsTerminal(status));
        Assert.True(AnalysisJobRow.IsActive(status));
        Assert.True(AnalysisJobTransitions.IsCancelable(status));
    }

    [Fact]
    public void OnlySucceededCountsAsACompleteResult()
    {
        Assert.True(AnalysisJobTransitions.IsResultComplete(AnalysisJobStatus.Succeeded));
        Assert.False(AnalysisJobTransitions.IsResultComplete(AnalysisJobStatus.Failed));
        Assert.False(AnalysisJobTransitions.IsResultComplete(AnalysisJobStatus.Canceled));
    }

    [Fact]
    public void TheKindNamesRoundTrip()
    {
        foreach (AnalysisJobKind kind in Enum.GetValues<AnalysisJobKind>())
        {
            Assert.Equal(kind, AnalysisJobRow.Parse(AnalysisJobRow.Name(kind)));
        }

        Assert.Null(AnalysisJobRow.Parse("boyle-bir-tur-yok"));
        Assert.Null(AnalysisJobRow.Parse(null));
    }

    [Fact]
    public void TheDeduplicationKeyCarriesBothRepositoryAndKind()
    {
        Assert.Equal("7:static-scan", AnalysisJobRow.DeduplicationKeyFor(7, AnalysisJobKind.StaticScan));
        Assert.Equal("7:risk-score-all", AnalysisJobRow.DeduplicationKeyFor(7, AnalysisJobKind.RiskScoreAll));
        Assert.NotEqual(
            AnalysisJobRow.DeduplicationKeyFor(7, AnalysisJobKind.StaticScan),
            AnalysisJobRow.DeduplicationKeyFor(8, AnalysisJobKind.StaticScan));
    }
}
