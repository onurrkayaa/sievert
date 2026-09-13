using System.Globalization;

using AngleSharp.Dom;

using Bunit;

using Microsoft.Extensions.DependencyInjection;

using Sievert.Contracts;
using Sievert.Web.Components.Shared;

namespace Sievert.Web.Tests;

/// <summary>
/// Harita ve cizelgenin ciktisi.
///
/// Sinanan sey hesaplanan sayi degil, **ekrana yazilan sey**: hucre rengi endeksle
/// tutarli mi, SVG'deki Y koordinati endeksle ters mi, grafigin altindaki tablo ayni
/// veriyi tasiyor mu, sayilar kultura bagli mi bicimleniyor.
/// </summary>
public sealed class VisualizationComponentTests : BunitContext
{
    private static readonly DateTimeOffset Start = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public VisualizationComponentTests()
    {
        Services.AddSingleton(new WebOptions { ApiBaseUrl = new Uri("http://127.0.0.1:5000") });
        Services.AddSingleton<IPageState>(new FakePageState());
    }

    [Fact]
    public void EveryCellShowsItsIndexAsANumberNotOnlyAsAColour()
    {
        IRenderedComponent<FileActivityMap> map = Render<FileActivityMap>(parameters =>
            parameters.Add(component => component.Map, Map()));

        string markup = map.Markup;

        Assert.Contains("Endeks 80", markup, StringComparison.Ordinal);
        Assert.Contains("Endeks 20", markup, StringComparison.Ordinal);

        // Erisilebilir etiket de sayiyi tasiyor; renk kelimeye cevrilmiyor.
        foreach (IElement cell in map.FindAll(".map-cell"))
        {
            string label = cell.GetAttribute("aria-label")!;

            Assert.Contains("ortalama endeks", label, StringComparison.Ordinal);
            Assert.Contains("dokunus", label, StringComparison.Ordinal);
            Assert.DoesNotContain("kirmizi", label, StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// Daha yuksek endeksin hucresi renk olceginde geri gitmiyor.
    ///
    /// Olculebilir tanim: cubugun kirmizi bileseni. Palet bu sinanabilsin diye boyle
    /// secildi ve ayni kural <see cref="VisualizationScaleTests"/> icinde sayisal olarak
    /// da duruyor; burada sinanan sey bilesenin o olcegi gercekten kullandigi.
    /// </summary>
    [Fact]
    public void TheCellColourFollowsTheIndex()
    {
        IRenderedComponent<FileActivityMap> map = Render<FileActivityMap>(parameters =>
            parameters.Add(component => component.Map, Map()));

        List<IElement> bars = [.. map.FindAll(".map-bar > span")];

        Assert.Equal(3, bars.Count);

        // Siralama ortalama endekse gore azalan; ilk hucre en yuksek endeks.
        int previous = 256;

        foreach (IElement bar in bars)
        {
            string style = bar.GetAttribute("style")!;
            int red = Convert.ToInt32(style.Split("background:#")[1][..2], 16);

            Assert.True(red <= previous, "daha dusuk endeksin rengi daha sicak cikti");
            previous = red;
        }
    }

    /// <summary>Kultur degisince stil metinleri degismiyor; virgul hicbir yerde yok.</summary>
    [Theory]
    [InlineData("tr-TR")]
    [InlineData("en-US")]
    public void TheRenderedStylesAreCultureIndependent(string culture)
    {
        CultureInfo before = CultureInfo.CurrentCulture;

        try
        {
            CultureInfo.CurrentCulture = new CultureInfo(culture);

            IRenderedComponent<FileActivityMap> map = Render<FileActivityMap>(parameters =>
                parameters.Add(component => component.Map, Map()));

            foreach (IElement bar in map.FindAll(".map-bar > span"))
            {
                string style = bar.GetAttribute("style")!;
                string width = style.Split("width:")[1].Split(';')[0];

                Assert.DoesNotContain(",", width, StringComparison.Ordinal);
            }

            IRenderedComponent<RiskTimeline> timeline = Render<RiskTimeline>(parameters =>
                parameters.Add(component => component.Timeline, Timeline()));

            foreach (IElement mark in timeline.FindAll("circle, rect"))
            {
                foreach (string name in (string[])["cx", "cy", "x", "y"])
                {
                    if (mark.GetAttribute(name) is string value)
                    {
                        Assert.DoesNotContain(",", value, StringComparison.Ordinal);
                    }
                }
            }
        }
        finally
        {
            CultureInfo.CurrentCulture = before;
        }
    }

    /// <summary>
    /// Yuksek endeksli nokta grafikte yukarida.
    ///
    /// SVG'de Y asagi dogru buyudugu icin, endeks buyudukce cy kuculmeli. Tek bir ters
    /// nokta bile grafigi yalan soyler hale getirir.
    /// </summary>
    [Fact]
    public void AHigherIndexIsDrawnHigher()
    {
        IRenderedComponent<RiskTimeline> timeline = Render<RiskTimeline>(parameters =>
            parameters.Add(component => component.Timeline, Timeline()));

        List<(double Index, double Y)> points = [];
        List<RiskTimelinePoint> data = [.. Timeline().Points];

        List<IElement> marks = [.. timeline.FindAll(".chart-mark")];

        Assert.Equal(data.Count, marks.Count);

        for (int index = 0; index < marks.Count; index++)
        {
            string raw = marks[index].GetAttribute("cy") ?? marks[index].GetAttribute("y")!;
            double y = double.Parse(raw, CultureInfo.InvariantCulture);

            // Kare isaretin y'si ust kenar; merkeze cevirip karsilastiriyoruz.
            points.Add((data[index].RiskIndex, marks[index].TagName == "rect" ? y + 4 : y));
        }

        foreach ((double index, double y) in points)
        {
            foreach ((double otherIndex, double otherY) in points)
            {
                if (index < otherIndex)
                {
                    Assert.True(y > otherY, $"{index} endeksi {otherIndex} endeksinden yukarida cizildi");
                }

                if (Math.Abs(index - otherIndex) < 0.001)
                {
                    Assert.Equal(y, otherY, 3);
                }
            }
        }
    }

    [Fact]
    public void BothThresholdsAreDrawnAsSeparateReferenceLines()
    {
        IRenderedComponent<RiskTimeline> timeline = Render<RiskTimeline>(parameters =>
            parameters.Add(component => component.Timeline, Timeline()));

        Assert.Single(timeline.FindAll(".chart-threshold-train"));
        Assert.Single(timeline.FindAll(".chart-threshold-half"));
        Assert.Contains("Egitim esigi", timeline.Markup, StringComparison.Ordinal);
        Assert.Contains("0,5 esigi", timeline.Markup, StringComparison.Ordinal);
    }

    /// <summary>Iki esik ayni endekse dusuyorsa tek cizgi; ust uste iki etiket okunmaz.</summary>
    [Fact]
    public void CoincidingThresholdsBecomeOneLineWithOneLabel()
    {
        RiskTimelineResponse data = Timeline() with
        {
            DecisionAt05RiskIndex = 60,
            DecisionAtTrainThresholdRiskIndex = 60,
            Warnings = [VisualizationWarning.ThresholdsCoincide],
        };

        IRenderedComponent<RiskTimeline> timeline = Render<RiskTimeline>(parameters =>
            parameters.Add(component => component.Timeline, data));

        Assert.Empty(timeline.FindAll(".chart-threshold-train"));
        Assert.Empty(timeline.FindAll(".chart-threshold-half"));
        Assert.Single(timeline.FindAll(".chart-threshold-both"));
        Assert.Contains("Iki esik de", timeline.Markup, StringComparison.Ordinal);
    }

    /// <summary>Her nokta klavyeyle gezilebiliyor ve kendi cumlesini tasiyor.</summary>
    [Fact]
    public void EveryPointIsFocusableAndDescribesItself()
    {
        IRenderedComponent<RiskTimeline> timeline = Render<RiskTimeline>(parameters =>
            parameters.Add(component => component.Timeline, Timeline()));

        List<IElement> points = [.. timeline.FindAll(".chart-point")];

        Assert.Equal(4, points.Count);

        foreach (IElement point in points)
        {
            Assert.Equal("0", point.GetAttribute("tabindex"));

            string label = point.GetAttribute("aria-label")!;

            Assert.Contains("endeks", label, StringComparison.Ordinal);
            Assert.Contains("ham skor", label, StringComparison.Ordinal);
            Assert.Contains("esigi", label, StringComparison.Ordinal);
        }

        // SZZ etiketi ve duzeltme sekille de ayriliyor; renk tek tasiyici degil.
        Assert.NotEmpty(timeline.FindAll("rect.chart-mark"));
        Assert.NotEmpty(timeline.FindAll(".chart-halo"));
    }

    /// <summary>Grafigin altindaki tablo ayni commit'leri ayni sirada ve ayni sayilarla tasiyor.</summary>
    [Fact]
    public void TheAccessibleTableCarriesExactlyTheSameData()
    {
        RiskTimelineResponse data = Timeline();

        IRenderedComponent<RiskTimeline> timeline = Render<RiskTimeline>(parameters =>
            parameters.Add(component => component.Timeline, data));

        List<IElement> rows = [.. timeline.FindAll("details table tbody tr")];

        Assert.Equal(data.Points.Count, rows.Count);

        for (int index = 0; index < rows.Count; index++)
        {
            RiskTimelinePoint point = data.Points[index];
            string text = rows[index].TextContent;

            Assert.Contains(point.ShortSha, text, StringComparison.Ordinal);
            Assert.Contains(Display.Index(point.RiskIndex), text, StringComparison.Ordinal);
            Assert.Contains(Display.Score(point.RawModelScore), text, StringComparison.Ordinal);
            Assert.Contains(point.Ordinal.ToString(CultureInfo.InvariantCulture), text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void TheRawScoreNeverCarriesAPercentSign()
    {
        IRenderedComponent<RiskTimeline> timeline = Render<RiskTimeline>(parameters =>
            parameters.Add(component => component.Timeline, Timeline()));

        Assert.Contains("0.9000", timeline.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("0.9000%", timeline.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("90.0%", timeline.Markup, StringComparison.Ordinal);
    }

    /// <summary>Statik bulgu sayisi ayri bir rozet ve renge girmedigi yazili.</summary>
    [Fact]
    public void StaticFindingsAreShownSeparatelyFromTheColour()
    {
        FileActivityResponse data = Map(withStaticFindings: true);

        IRenderedComponent<FileActivityMap> map = Render<FileActivityMap>(parameters =>
            parameters.Add(component => component.Map, data));

        Assert.Contains("statik bulgu", map.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("hucre rengine ya da", map.Markup, StringComparison.Ordinal);
        Assert.Contains("Statik bulgular bu renge dahil degildir", map.Markup, StringComparison.Ordinal);
    }

    /// <summary>Yasak ifadeler haritada ve cizelgede gecmiyor.</summary>
    [Fact]
    public void NeitherVisualisationUsesAForbiddenPhrase()
    {
        string[] forbidden =
        [
            "hata olasilig",
            "dosyanin hata ihtimali",
            "bu dosya hatali",
            "kesin risk",
            "model dogru bildi",
            "birlesik skor",
            "combinedRisk",
            "kalibre olasilik",
        ];

        string markup = Render<FileActivityMap>(parameters =>
            parameters.Add(component => component.Map, Map(withStaticFindings: true))).Markup
            + Render<RiskTimeline>(parameters =>
                parameters.Add(component => component.Timeline, Timeline())).Markup;

        foreach (string phrase in forbidden)
        {
            Assert.DoesNotContain(phrase, markup, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void APartialVisualisationSaysItsRankingIsLimited()
    {
        IRenderedComponent<PartialResultBanner> banner = Render<PartialResultBanner>(parameters => parameters
            .Add(component => component.Partial, true)
            .Add(component => component.RankingScope, RankingScope.WrittenResultsOnly));

        Assert.Contains("Bu analiz tamamlanmadi", banner.Markup, StringComparison.Ordinal);
        Assert.Contains("yalnizca su ana kadar kaydedilmis sonuclara", banner.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void ASingleCommitTimelineStillDraws()
    {
        RiskTimelineResponse data = Timeline() with
        {
            Points = [Timeline().Points[0]],
            ReturnedCount = 1,
            StartDateUtc = Start,
            EndDateUtc = Start,
        };

        IRenderedComponent<RiskTimeline> timeline = Render<RiskTimeline>(parameters =>
            parameters.Add(component => component.Timeline, data));

        Assert.Single(timeline.FindAll(".chart-point"));

        // Tek nokta cizgi cizmiyor ve bolme sifir hatasi da vermiyor.
        Assert.Empty(timeline.FindAll(".chart-line"));
    }

    [Fact]
    public void AnEmptyMapSaysSoInsteadOfDrawingNothing()
    {
        FileActivityResponse data = Map() with { Items = [], ReturnedFileCount = 0, FileCountBeforeLimit = 0 };

        IRenderedComponent<FileActivityMap> map = Render<FileActivityMap>(parameters =>
            parameters.Add(component => component.Map, data));

        Assert.Contains("degistirilmis C# dosyasi yok", map.Markup, StringComparison.Ordinal);
        Assert.Empty(map.FindAll(".map-cell"));
    }

    private static FileActivityResponse Map(bool withStaticFindings = false) => new(
        2,
        Guid.Parse("01a09990-0000-7000-8000-000000000001"),
        "1d7b6d97844c1111111111111111111111111111",
        200,
        120,
        3,
        3,
        100,
        FileActivitySort.MeanRiskDescending,
        IsResultComplete: true,
        IsPartial: false,
        RankingScope.CompleteAnalysis,
        "polly",
        IsCalibrated: false,
        [
            Item("src/High.cs", 80, withStaticFindings ? 4 : null),
            Item("src/Mid.cs", 50, withStaticFindings ? 0 : null),
            Item("src/Low.cs", 20, withStaticFindings ? 0 : null),
        ],
        []);

    private static FileActivityItem Item(string path, double mean, int? findings) => new(
        path,
        6,
        120,
        30,
        150,
        mean,
        Math.Min(100, mean + 10),
        mean,
        "aaaaaaaaaaaabbbbbbbbbbbbccccccccccccdddd",
        Start,
        findings,
        findings is null ? null : Guid.Parse("01a09990-0000-7000-8000-0000000000ff"),
        StaticFindingsIncludedInColor: false,
        [
            new FileActivityCommit(
                "aaaaaaaaaaaabbbbbbbbbbbbccccccccccccdddd",
                "aaaaaaaaaaaa",
                Start,
                "ornek commit",
                mean,
                mean / 100.0,
                10,
                2),
        ]);

    private static RiskTimelineResponse Timeline() => new(
        2,
        Guid.Parse("01a09990-0000-7000-8000-000000000001"),
        100,
        4,
        IsResultComplete: true,
        IsPartial: false,
        RankingScope.CompleteAnalysis,
        "polly",
        IsCalibrated: false,
        72.5,
        41.2,
        Start,
        Start.AddDays(3),
        [
            Point(0, 10, Start, isFix: false, szz: false),
            Point(1, 90, Start.AddDays(1), isFix: true, szz: false),
            Point(2, 45, Start.AddDays(2), isFix: false, szz: true),
            Point(3, 45, Start.AddDays(3), isFix: false, szz: false),
        ],
        []);

    private static RiskTimelinePoint Point(int ordinal, double index, DateTimeOffset at, bool isFix, bool szz) => new(
        ordinal,
        ordinal.ToString("x8", CultureInfo.InvariantCulture) + new string('e', 32),
        ordinal.ToString("x8", CultureInfo.InvariantCulture) + "eeee",
        at,
        "commit " + ordinal,
        index / 100.0,
        index,
        index >= 50,
        index >= 30,
        0.2381,
        isFix,
        szz,
        10,
        3,
        2,
        1,
        IsBot: false,
        ["UNCALIBRATED_SCORE"]);
}
