using System.Globalization;

namespace Sievert.Web.Tests;

/// <summary>
/// Gorsel kodlamanin sayiyla tutarliligi.
///
/// Bu testlerin varlik sebebi Adim 4'te yasanan somut bir kusur: cubuk genisligi kultura
/// bagli bicimleniyordu, tarayici "width:67,8%" degerini gecersiz sayiyordu ve 0,43 ile
/// 0,02 katkinin cubugu ayni uzunlukta gorunuyordu. Ne derleme ne testler yakalamisti;
/// ekran goruntusune bakmak yakalamisti. Artik sayilabilir hale getirildi.
/// </summary>
public sealed class VisualizationScaleTests
{
    private static readonly double[] Sweep = [.. Enumerable.Range(0, 1001).Select(step => step / 10.0)];

    [Fact]
    public void TheSameIndexAlwaysProducesTheSameColour()
    {
        foreach (double index in Sweep)
        {
            Assert.Equal(VisualizationScale.Fill(index), VisualizationScale.Fill(index));
        }
    }

    /// <summary>
    /// Kultur degisince hicbir sey degismiyor: ne renk, ne koordinat, ne genislik.
    /// </summary>
    [Theory]
    [InlineData("tr-TR")]
    [InlineData("en-US")]
    [InlineData("de-DE")]
    public void ColoursAndCoordinatesAreCultureIndependent(string culture)
    {
        (string Fill, string Y, string Width)[] expected =
        [
            .. Sweep.Select(index => (
                VisualizationScale.Fill(index),
                VisualizationScale.Number(VisualizationScale.Y(index)),
                VisualizationScale.Percent(VisualizationScale.Position(index)))),
        ];

        CultureInfo before = CultureInfo.CurrentCulture;

        try
        {
            CultureInfo.CurrentCulture = new CultureInfo(culture);

            for (int index = 0; index < Sweep.Length; index++)
            {
                double value = Sweep[index];

                Assert.Equal(expected[index].Fill, VisualizationScale.Fill(value));
                Assert.Equal(expected[index].Y, VisualizationScale.Number(VisualizationScale.Y(value)));
                Assert.Equal(
                    expected[index].Width,
                    VisualizationScale.Percent(VisualizationScale.Position(value)));
            }
        }
        finally
        {
            CultureInfo.CurrentCulture = before;
        }
    }

    /// <summary>Hicbir sayida virgul yok; CSS ve SVG yalniz noktali ondaligi anliyor.</summary>
    [Fact]
    public void NoNumberEverCarriesAComma()
    {
        CultureInfo before = CultureInfo.CurrentCulture;

        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("tr-TR");

            foreach (double index in Sweep)
            {
                Assert.DoesNotContain(",", VisualizationScale.Number(VisualizationScale.Y(index)), StringComparison.Ordinal);
                Assert.DoesNotContain(",", VisualizationScale.Percent(VisualizationScale.Position(index)), StringComparison.Ordinal);
                Assert.DoesNotContain(",", VisualizationScale.Fill(index), StringComparison.Ordinal);
            }

            Assert.Equal("12.34", VisualizationScale.Number(12.337));
        }
        finally
        {
            CultureInfo.CurrentCulture = before;
        }
    }

    /// <summary>Olcek uzerindeki yer endeksle birlikte hic geri gitmiyor.</summary>
    [Fact]
    public void ThePositionOnTheScaleNeverGoesBackwards()
    {
        double previous = -1;

        foreach (double index in Sweep)
        {
            double position = VisualizationScale.Position(index);

            Assert.True(position >= previous, $"{index} icin olcek geri gitti");
            previous = position;
        }
    }

    /// <summary>
    /// Rengin "isisi" endeksle birlikte artiyor.
    ///
    /// Olculebilir tanim: kirmizi bileseni hic azalmiyor. Palet bu sinanabilsin diye
    /// boyle secildi; "bana sicak gorundu" bir test degil.
    /// </summary>
    [Fact]
    public void TheRedChannelNeverDecreasesAsTheIndexGrows()
    {
        int previous = -1;

        foreach (double index in Sweep)
        {
            int red = Convert.ToInt32(VisualizationScale.Fill(index).Substring(1, 2), 16);

            Assert.True(red >= previous, $"{index} endeksinde kirmizi bileseni geri gitti");
            previous = red;
        }
    }

    [Fact]
    public void TheAnchorColoursAreAllDifferent()
    {
        string[] colours = [.. new double[] { 0, 25, 50, 75, 100 }.Select(VisualizationScale.Fill)];

        Assert.Equal(colours.Length, colours.Distinct(StringComparer.Ordinal).Count());
    }

    /// <summary>
    /// Kirpma yalniz gorseli etkiliyor: arada bir sekilde 0'in altinda ya da 100'un
    /// ustunde bir deger gelirse renk uctaki renge esitleniyor, veri degistirilmiyor.
    /// </summary>
    [Fact]
    public void ValuesOutsideTheRangeClampVisuallyOnly()
    {
        Assert.Equal(VisualizationScale.Fill(0), VisualizationScale.Fill(-40));
        Assert.Equal(VisualizationScale.Fill(100), VisualizationScale.Fill(140));
        Assert.Equal(VisualizationScale.Y(0), VisualizationScale.Y(-40));
        Assert.Equal(VisualizationScale.Y(100), VisualizationScale.Y(140));
    }

    /// <summary>Her hucrede metin okunur: kontrast orani WCAG AA esigini geciyor.</summary>
    [Fact]
    public void EveryCellHasReadableText()
    {
        foreach (double index in Sweep)
        {
            string background = VisualizationScale.CellBackground(index);
            double ratio = VisualizationScale.Contrast(background, VisualizationScale.TextOn(index));

            Assert.True(ratio >= 4.5, $"{index} endeksinde kontrast orani {ratio:F2}");
        }
    }

    /// <summary>
    /// Renk cubugu kart zemininden ayirt edilebiliyor.
    ///
    /// WCAG'in metin disi ogeler icin verdigi esik 3,0. Cubuk tek bilgi tasiyicisi degil -
    /// sayi her hucrede yazili - ama gorunmeyen bir cubuk da yer kaplamaktan baska bir
    /// ise yaramaz.
    /// </summary>
    [Fact]
    public void TheColourBarStandsOutFromTheCard()
    {
        foreach (double index in Sweep)
        {
            double ratio = VisualizationScale.Contrast(
                VisualizationScale.Fill(index),
                VisualizationScale.Surface);

            Assert.True(ratio >= 3.0, $"{index} endeksinde cubuk/kart orani {ratio:F2}");
        }
    }

    /// <summary>
    /// Yuksek endeks yukarida. Y ekranda asagi dogru buyudugu icin, endeks buyudukce Y
    /// kuculuyor; ters davranan tek bir deger bile grafigi yalan soyler hale getirir.
    /// </summary>
    [Fact]
    public void AHigherIndexIsAlwaysHigherOnTheChart()
    {
        double previous = double.MaxValue;

        foreach (double index in Sweep)
        {
            double y = VisualizationScale.Y(index);

            Assert.True(y <= previous, $"{index} endeksinde Y koordinati ters dondu");
            previous = y;
        }

        Assert.True(VisualizationScale.Y(0) > VisualizationScale.Y(100));
        Assert.Equal(VisualizationScale.Y(50), VisualizationScale.Y(50));
    }

    [Fact]
    public void TheHorizontalAxisFallsBackToOrdinalWhenTimeDoesNotMove()
    {
        // Tek nokta: ortada, sifira bolme yok.
        Assert.Equal(
            VisualizationScale.PlotLeft + ((VisualizationScale.PlotRight - VisualizationScale.PlotLeft) / 2),
            VisualizationScale.X(0, 1, 0, 0));

        // Butun damgalar ayni: sira numarasina dusuyor ve noktalar ust uste binmiyor.
        Assert.Equal(VisualizationScale.PlotLeft, VisualizationScale.X(0, 5, 0, 0));
        Assert.Equal(VisualizationScale.PlotRight, VisualizationScale.X(4, 5, 0, 0));
        Assert.True(VisualizationScale.X(1, 5, 0, 0) < VisualizationScale.X(2, 5, 0, 0));

        // Zaman ilerliyorsa konum zamandan geliyor.
        Assert.Equal(VisualizationScale.PlotLeft, VisualizationScale.X(0, 3, 0, 100));
        Assert.Equal(VisualizationScale.PlotRight, VisualizationScale.X(2, 3, 100, 100));
    }
}
