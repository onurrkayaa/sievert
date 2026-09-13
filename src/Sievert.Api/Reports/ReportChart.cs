using System.Globalization;
using System.Text;

using Sievert.Contracts;

namespace Sievert.Api.Reports;

/// <summary>
/// Zaman cizelgesinin SVG cizimi.
///
/// PDF'e **vektor** olarak giriyor: ekran goruntusu alip gomulmuyor, o yuzden
/// yakinlastirinca bozulmuyor ve siyah-beyaz baskida da cizgiler kayboluyor degil.
///
/// Panelin <c>VisualizationScale</c> sinifiyla ayni isi yapiyor ama ayri duruyor, cunku
/// API panele referans veremez (ADR 0025: panel API'ye bagli, tersi degil). Ikisinin
/// ayni veriyi ayni sekilde gostermesi, ikisinin de ayni uctan beslenmesiyle saglaniyor;
/// bagimsiz dogrulama araci bunu ayrica kontrol ediyor.
///
/// Butun sayilar degismez kulturle yaziliyor: virgullu bir ondalik SVG'yi sessizce
/// bozardi. Adim 4'te tam bu kusur panelde yasandi.
/// </summary>
public static class ReportChart
{
    private const double Width = 1000;

    private const double Height = 420;

    private const double PlotLeft = 60;

    private const double PlotRight = 980;

    private const double PlotTop = 20;

    private const double PlotBottom = 360;

    /// <summary>Cizelgenin SVG metni. Bos veri icin de gecerli bir belge doner.</summary>
    public static string Timeline(
        IReadOnlyList<RiskTimelinePoint> points,
        double thresholdAt05,
        double thresholdAtTrain,
        ReportText text)
    {
        StringBuilder svg = new();

        svg.Append(CultureInfo.InvariantCulture, $"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 {N(Width)} {N(Height)}\" width=\"{N(Width)}\" height=\"{N(Height)}\">");
        svg.Append("<rect x=\"0\" y=\"0\" width=\"1000\" height=\"420\" fill=\"#ffffff\" />");

        // Yatay izgara ve eksen etiketleri.
        for (int index = 0; index <= 100; index += 25)
        {
            double y = Y(index);

            svg.Append(CultureInfo.InvariantCulture,
                $"<line x1=\"{N(PlotLeft)}\" y1=\"{N(y)}\" x2=\"{N(PlotRight)}\" y2=\"{N(y)}\" stroke=\"#d8d8d8\" stroke-width=\"1\" />");

            svg.Append(CultureInfo.InvariantCulture,
                $"<text x=\"{N(PlotLeft - 8)}\" y=\"{N(y + 4)}\" text-anchor=\"end\" font-family=\"Lato, sans-serif\" font-size=\"13\" fill=\"#555555\">{index}</text>");
        }

        // Esik cizgileri. Ikisi ayri: ust uste dusseler bile iki ayri karar var.
        Threshold(svg, thresholdAt05, "#b03030", text.ThresholdAt05, text);
        Threshold(svg, thresholdAtTrain, "#2f6f9f", text.ThresholdAtTrain, text);

        if (points.Count > 0)
        {
            StringBuilder path = new();

            for (int index = 0; index < points.Count; index++)
            {
                double x = X(index, points.Count);
                double y = Y(points[index].RiskIndex);

                path.Append(CultureInfo.InvariantCulture, $"{N(x)},{N(y)} ");
            }

            svg.Append(CultureInfo.InvariantCulture,
                $"<polyline points=\"{path.ToString().Trim()}\" fill=\"none\" stroke=\"#1f3f5f\" stroke-width=\"1.6\" />");

            // Nokta sayisi cok yuksekken her noktayi cizmek PDF'i buyutuyor ve gorsel
            // olarak da bir sey katmiyor; **veri azaltilmiyor**, yalniz isaretleyici
            // cizimi seyreltiliyor ve cizgi butun noktalardan geciyor.
            int step = Math.Max(1, points.Count / 120);

            for (int index = 0; index < points.Count; index += step)
            {
                svg.Append(CultureInfo.InvariantCulture,
                    $"<circle cx=\"{N(X(index, points.Count))}\" cy=\"{N(Y(points[index].RiskIndex))}\" r=\"2.2\" fill=\"#1f3f5f\" />");
            }

            // Yatay eksen: ilk, orta ve son noktanin tarihi.
            Label(svg, points[0], X(0, points.Count), "start", text);

            if (points.Count > 2)
            {
                int middle = points.Count / 2;
                Label(svg, points[middle], X(middle, points.Count), "middle", text);
            }

            if (points.Count > 1)
            {
                Label(svg, points[^1], X(points.Count - 1, points.Count), "end", text);
            }
        }

        svg.Append(CultureInfo.InvariantCulture,
            $"<line x1=\"{N(PlotLeft)}\" y1=\"{N(PlotBottom)}\" x2=\"{N(PlotRight)}\" y2=\"{N(PlotBottom)}\" stroke=\"#555555\" stroke-width=\"1.2\" />");

        svg.Append("</svg>");

        return svg.ToString();
    }

    private static void Threshold(
        StringBuilder svg, double index, string colour, string caption, ReportText text)
    {
        double y = Y(index);

        svg.Append(CultureInfo.InvariantCulture,
            $"<line x1=\"{N(PlotLeft)}\" y1=\"{N(y)}\" x2=\"{N(PlotRight)}\" y2=\"{N(y)}\" stroke=\"{colour}\" stroke-width=\"1.2\" stroke-dasharray=\"6 4\" />");

        // Etiketteki sayi **belgenin kulturuyle** yaziliyor: ayni sayfada bir yerde
        // "93,3" digerinde "93.3" gormek gorsel kontrolde yakalanan bir tutarsizlikti.
        // Koordinatlar degismez kulturde kaliyor; oradaki virgul SVG'yi bozar.
        svg.Append(CultureInfo.InvariantCulture,
            $"<text x=\"{N(PlotRight - 4)}\" y=\"{N(y - 6)}\" text-anchor=\"end\" font-family=\"Lato, sans-serif\" font-size=\"13\" fill=\"{colour}\">"
            + $"{Escape(caption)} {Escape(text.Number(index, 1))}</text>");
    }

    private static void Label(
        StringBuilder svg, RiskTimelinePoint point, double x, string anchor, ReportText text)
    {
        svg.Append(CultureInfo.InvariantCulture,
            $"<text x=\"{N(x)}\" y=\"{N(PlotBottom + 22)}\" text-anchor=\"{anchor}\" font-family=\"Lato, sans-serif\" font-size=\"13\" fill=\"#555555\">"
            + $"{Escape(point.AuthorDateUtc.UtcDateTime.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))}</text>");
    }

    /// <summary>Endeks buyudukce yukari: Y ekseni ters, bu yuzden cikarma.</summary>
    private static double Y(double index) =>
        PlotBottom - (Math.Clamp(index, 0, 100) / 100 * (PlotBottom - PlotTop));

    private static double X(int ordinal, int count) =>
        count <= 1
            ? PlotLeft + ((PlotRight - PlotLeft) / 2)
            : PlotLeft + (ordinal / (double)(count - 1) * (PlotRight - PlotLeft));

    private static string N(double value) =>
        Math.Round(value, 2).ToString("0.##", CultureInfo.InvariantCulture);

    /// <summary>SVG metni icinde isaret karakterleri kacisliyor.</summary>
    private static string Escape(string value) => value
        .Replace("&", "&amp;", StringComparison.Ordinal)
        .Replace("<", "&lt;", StringComparison.Ordinal)
        .Replace(">", "&gt;", StringComparison.Ordinal);
}
