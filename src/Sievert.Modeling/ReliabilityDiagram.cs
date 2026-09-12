using System.Globalization;
using System.Text;

namespace Sievert.Modeling;

/// <summary>
/// Guvenilirlik diyagrami, elle yazilmis SVG olarak. Harici grafik paketi eklenmedi:
/// cizim bir eksen, bir kosegen ve uc egriden ibaret, bunun icin paket almak gereksiz.
///
/// Cikti deterministik: renkler sabit, sayilar invariant bicimde, nokta sirasi kutu
/// sirasinda. BOS kutular nokta olarak cizilmiyor ama satir sayilari
/// calibration-results.json icinde duruyor.
/// </summary>
public static class ReliabilityDiagram
{
    private const int Width = 520;

    private const int Height = 520;

    private const int Margin = 70;

    private const string RawColour = "#b5651d";

    private const string PlattColour = "#1f6fb4";

    private const string IsotonicColour = "#2e8b57";

    public static string Render(
        string title,
        int testCount,
        CalibrationResult raw,
        CalibrationResult platt,
        CalibrationResult isotonic)
    {
        StringBuilder svg = new();

        svg.Append(CultureInfo.InvariantCulture, $"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{Width}\" height=\"{Height}\" viewBox=\"0 0 {Width} {Height}\">\n");
        svg.Append("<rect width=\"100%\" height=\"100%\" fill=\"#ffffff\"/>\n");
        svg.Append(CultureInfo.InvariantCulture, $"<text x=\"{Width / 2}\" y=\"26\" text-anchor=\"middle\" font-family=\"sans-serif\" font-size=\"15\" fill=\"#111111\">{Escape(title)}</text>\n");
        svg.Append(CultureInfo.InvariantCulture, $"<text x=\"{Width / 2}\" y=\"46\" text-anchor=\"middle\" font-family=\"sans-serif\" font-size=\"12\" fill=\"#444444\">test N = {testCount}</text>\n");

        // Eksen cercevesi.
        svg.Append(CultureInfo.InvariantCulture, $"<rect x=\"{Margin}\" y=\"{Margin}\" width=\"{Width - (2 * Margin)}\" height=\"{Height - (2 * Margin)}\" fill=\"none\" stroke=\"#999999\" stroke-width=\"1\"/>\n");

        for (int step = 0; step <= 10; step += 2)
        {
            double value = step / 10.0;

            svg.Append(CultureInfo.InvariantCulture, $"<line x1=\"{X(value)}\" y1=\"{Margin}\" x2=\"{X(value)}\" y2=\"{Height - Margin}\" stroke=\"#eeeeee\" stroke-width=\"1\"/>\n");
            svg.Append(CultureInfo.InvariantCulture, $"<line x1=\"{Margin}\" y1=\"{Y(value)}\" x2=\"{Width - Margin}\" y2=\"{Y(value)}\" stroke=\"#eeeeee\" stroke-width=\"1\"/>\n");
            svg.Append(CultureInfo.InvariantCulture, $"<text x=\"{X(value)}\" y=\"{Height - Margin + 18}\" text-anchor=\"middle\" font-family=\"sans-serif\" font-size=\"11\" fill=\"#444444\">{Number(value)}</text>\n");
            svg.Append(CultureInfo.InvariantCulture, $"<text x=\"{Margin - 10}\" y=\"{YOffset(value)}\" text-anchor=\"end\" font-family=\"sans-serif\" font-size=\"11\" fill=\"#444444\">{Number(value)}</text>\n");
        }

        // Ideal y = x.
        svg.Append(CultureInfo.InvariantCulture, $"<line x1=\"{X(0)}\" y1=\"{Y(0)}\" x2=\"{X(1)}\" y2=\"{Y(1)}\" stroke=\"#000000\" stroke-width=\"1\" stroke-dasharray=\"5 4\"/>\n");

        Curve(svg, raw, RawColour);
        Curve(svg, platt, PlattColour);
        Curve(svg, isotonic, IsotonicColour);

        svg.Append(CultureInfo.InvariantCulture, $"<text x=\"{Width / 2}\" y=\"{Height - 18}\" text-anchor=\"middle\" font-family=\"sans-serif\" font-size=\"12\" fill=\"#111111\">ortalama tahmin olasiligi</text>\n");
        svg.Append(CultureInfo.InvariantCulture, $"<text x=\"18\" y=\"{Height / 2}\" text-anchor=\"middle\" font-family=\"sans-serif\" font-size=\"12\" fill=\"#111111\" transform=\"rotate(-90 18 {Height / 2})\">gozlenen pozitif oran</text>\n");

        Legend(svg);

        svg.Append("</svg>\n");

        return svg.ToString();
    }

    private static void Legend(StringBuilder svg)
    {
        (string Label, string Colour)[] entries =
        [
            ("ham", RawColour),
            ("Platt", PlattColour),
            ("isotonic", IsotonicColour),
        ];

        int top = Margin + 12;

        for (int index = 0; index < entries.Length; index++)
        {
            int y = top + (index * 18);

            svg.Append(CultureInfo.InvariantCulture, $"<line x1=\"{Margin + 12}\" y1=\"{y}\" x2=\"{Margin + 40}\" y2=\"{y}\" stroke=\"{entries[index].Colour}\" stroke-width=\"2\"/>\n");
            svg.Append(CultureInfo.InvariantCulture, $"<circle cx=\"{Margin + 26}\" cy=\"{y}\" r=\"3\" fill=\"{entries[index].Colour}\"/>\n");
            svg.Append(CultureInfo.InvariantCulture, $"<text x=\"{Margin + 48}\" y=\"{y + 4}\" font-family=\"sans-serif\" font-size=\"11\" fill=\"#111111\">{entries[index].Label}</text>\n");
        }

        svg.Append(CultureInfo.InvariantCulture, $"<line x1=\"{Margin + 12}\" y1=\"{top + 54}\" x2=\"{Margin + 40}\" y2=\"{top + 54}\" stroke=\"#000000\" stroke-width=\"1\" stroke-dasharray=\"5 4\"/>\n");
        svg.Append(CultureInfo.InvariantCulture, $"<text x=\"{Margin + 48}\" y=\"{top + 58}\" font-family=\"sans-serif\" font-size=\"11\" fill=\"#111111\">ideal (y = x)</text>\n");
    }

    /// <summary>Bos kutular atlaniyor: nokta da cizgi de yok.</summary>
    private static void Curve(StringBuilder svg, CalibrationResult result, string colour)
    {
        List<(double X, double Y)> points = [];

        foreach (CalibrationBin bin in result.Bins)
        {
            if (bin.ObservedRate is double observed)
            {
                points.Add((bin.MeanPrediction, observed));
            }
        }

        if (points.Count > 1)
        {
            StringBuilder path = new();

            foreach ((double x, double y) in points)
            {
                path.Append(CultureInfo.InvariantCulture, $"{X(x)},{Y(y)} ");
            }

            svg.Append(CultureInfo.InvariantCulture, $"<polyline points=\"{path.ToString().Trim()}\" fill=\"none\" stroke=\"{colour}\" stroke-width=\"2\"/>\n");
        }

        foreach ((double x, double y) in points)
        {
            svg.Append(CultureInfo.InvariantCulture, $"<circle cx=\"{X(x)}\" cy=\"{Y(y)}\" r=\"3.5\" fill=\"{colour}\"/>\n");
        }
    }

    private static string YOffset(double value) =>
        (Height - Margin - (value * (Height - (2 * Margin))) + 4).ToString("F2", CultureInfo.InvariantCulture);

    private static string X(double value) =>
        (Margin + (value * (Width - (2 * Margin)))).ToString("F2", CultureInfo.InvariantCulture);

    private static string Y(double value) =>
        (Height - Margin - (value * (Height - (2 * Margin)))).ToString("F2", CultureInfo.InvariantCulture);

    private static string Number(double value) => value.ToString("F1", CultureInfo.InvariantCulture);

    private static string Escape(string text) =>
        text.Replace("&", "&amp;", StringComparison.Ordinal)
            .Replace("<", "&lt;", StringComparison.Ordinal)
            .Replace(">", "&gt;", StringComparison.Ordinal);
}
