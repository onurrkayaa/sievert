using System.Globalization;

namespace Sievert.Web;

/// <summary>
/// Gorsel kodlamanin **tek** yeri: endeks -> renk ve endeks -> koordinat.
///
/// Tek yerde durmasinin sebebi Adim 4'te yasanan bir kusur: katki cubugunun genisligi
/// bilesenin icinde bicimleniyordu ve sunucunun kulturu virgullu ondalik kullandiginda
/// tarayici degeri gecersiz sayip cubugu tamamen dolduruyordu. Bicimleme dagilinca her
/// yerde ayni hata bekleniyor; burada toplaninca tek bir testle kapatilabiliyor.
///
/// Butun sayilar **degismez kultur** ile yaziliyor.
/// </summary>
public static class VisualizationScale
{
    /// <summary>Cizim alaninin koordinat sistemi; SVG bunu viewBox olarak kullaniyor.</summary>
    public const double Width = 1000;

    public const double Height = 420;

    public const double PlotLeft = 54;

    public const double PlotRight = Width - 18;

    public const double PlotTop = 18;

    public const double PlotBottom = Height - 56;

    /// <summary>Koyu arka plan uzerindeki acik metin rengi.</summary>
    public const string LightText = "#e4eaf1";

    /// <summary>Acik hucre uzerindeki koyu metin rengi.</summary>
    public const string DarkText = "#0d1219";

    /// <summary>Kartlarin arka plani; hucre zemini bununla karistiriliyor.</summary>
    public const string Surface = "#141c26";

    /// <summary>
    /// Renk duraklari: koyu mavi -> camgobegi -> sari -> turuncu -> kirmizi.
    ///
    /// Kirmizi bileseni bastan sona artiyor (21 -> 229). Bu bilincli: "daha yuksek endeks
    /// renk olceginde geri gitmemeli" cumlesi ancak olculebilir bir sey uzerinden
    /// sinanabilir ve olculen sey bu.
    /// </summary>
    private static readonly (double At, int R, int G, int B)[] Stops =
    [
        (0, 58, 160, 216),
        (25, 63, 200, 192),
        (50, 232, 201, 72),
        (75, 239, 148, 56),
        (100, 240, 72, 60),
    ];

    /// <summary>Hucre zemininde olcek renginin payi; geri kalani kart rengi.</summary>
    private const double CellTint = 0.18;

    /// <summary>
    /// Endeksin olcek uzerindeki yeri, 0 ile 1 arasinda.
    ///
    /// Kirpma yalnizca **gorsel**: veri degismiyor, yalniz renk ve koordinat 0-100
    /// araligina sikistiriliyor. Endeksin kendisi zaten bu araliktaki bir yuzdelik ama
    /// disaridan gelen bir deger gorseli bozmamali.
    /// </summary>
    public static double Position(double riskIndex) => Math.Clamp(riskIndex, 0, 100) / 100.0;

    /// <summary>Hucre dolgusu. Ayni endeks her zaman ayni metni veriyor.</summary>
    public static string Fill(double riskIndex)
    {
        double value = Math.Clamp(riskIndex, 0, 100);

        for (int index = 1; index < Stops.Length; index++)
        {
            (double at, int r, int g, int b) = Stops[index];

            if (value > at)
            {
                continue;
            }

            (double previousAt, int previousR, int previousG, int previousB) = Stops[index - 1];

            double span = at - previousAt;
            double ratio = span <= 0 ? 0 : (value - previousAt) / span;

            return Hex(
                Mix(previousR, r, ratio),
                Mix(previousG, g, ratio),
                Mix(previousB, b, ratio));
        }

        (double _, int lastR, int lastG, int lastB) = Stops[^1];

        return Hex(lastR, lastG, lastB);
    }

    /// <summary>
    /// Hucrenin zemini: olcek rengi kart rengiyle karistirilmis hali.
    ///
    /// Metin dogrudan olcek renginin uzerine YAZILMIYOR. Sebep olculdu: koyudan aciga
    /// giden herhangi bir gecis, ortasinda ne acik ne koyu metnin 4,5 kontrast oranini
    /// tutturamadigi bir bolge biraktiriyor - ilk paletle 15,3 endeksinde oran 4,45
    /// cikti. Renk kodlamasi hucredeki ayri bir cubukta duruyor, metin ise her zaman
    /// koyu bir zeminin uzerinde.
    /// </summary>
    public static string CellBackground(double riskIndex)
    {
        string fill = Fill(riskIndex);

        return Hex(
            Mix(Component(Surface, 0), Component(fill, 0), CellTint),
            Mix(Component(Surface, 1), Component(fill, 1), CellTint),
            Mix(Component(Surface, 2), Component(fill, 2), CellTint));
    }

    /// <summary>
    /// Hucre zemininde okunabilir metin rengi.
    ///
    /// Secim tahminle degil olcumle: iki adaydan kontrast orani yuksek olani seciliyor.
    /// Testler her endeks icin oranin WCAG AA esigini (4,5) gectigini siniyor.
    /// </summary>
    public static string TextOn(double riskIndex)
    {
        string background = CellBackground(riskIndex);

        return Contrast(background, LightText) >= Contrast(background, DarkText) ? LightText : DarkText;
    }

    /// <summary>Iki rengin kontrast orani (WCAG 2.1 tanimi).</summary>
    public static double Contrast(string first, string second)
    {
        double a = Luminance(first);
        double b = Luminance(second);

        (double light, double dark) = a > b ? (a, b) : (b, a);

        return (light + 0.05) / (dark + 0.05);
    }

    /// <summary>
    /// Endeksin dikey konumu.
    ///
    /// Yuksek endeks yukarida: ekranda yukari cikmak "daha yuksek" demek. Y degeri bu
    /// yuzden endeks buyudukce KUCULUYOR ve testler tam bunu siniyor.
    /// </summary>
    public static double Y(double riskIndex) =>
        PlotBottom - (Position(riskIndex) * (PlotBottom - PlotTop));

    /// <summary>
    /// Bir noktanin yatay konumu.
    ///
    /// Zaman araligi sifirsa - butun commit'ler ayni damgada ya da tek commit varsa -
    /// sira numarasina duselecek. Sifira bolme yok ve uydurma bir kaydirma da yok.
    /// </summary>
    public static double X(int ordinal, int count, double elapsed, double span)
    {
        double usable = PlotRight - PlotLeft;

        if (span > 0)
        {
            return PlotLeft + (Math.Clamp(elapsed / span, 0, 1) * usable);
        }

        return count <= 1
            ? PlotLeft + (usable / 2)
            : PlotLeft + (ordinal / (double)(count - 1) * usable);
    }

    /// <summary>CSS ve SVG'ye yazilan her sayi buradan geciyor.</summary>
    public static string Number(double value) =>
        Math.Round(value, 2).ToString("0.##", CultureInfo.InvariantCulture);

    /// <summary>Yuzde isaretli genislik; yalniz CSS genisligi icin, skor icin DEGIL.</summary>
    public static string Percent(double ratio) =>
        Number(Math.Clamp(ratio, 0, 1) * 100) + "%";

    private static int Component(string hex, int index) =>
        Convert.ToInt32(hex.Substring(1 + (index * 2), 2), 16);

    private static int Mix(int from, int to, double ratio) =>
        (int)Math.Round(from + ((to - from) * Math.Clamp(ratio, 0, 1)), MidpointRounding.AwayFromZero);

    private static string Hex(int r, int g, int b) =>
        "#" + r.ToString("x2", CultureInfo.InvariantCulture)
        + g.ToString("x2", CultureInfo.InvariantCulture)
        + b.ToString("x2", CultureInfo.InvariantCulture);

    private static double Luminance(string hex)
    {
        double r = Channel(Convert.ToInt32(hex.Substring(1, 2), 16));
        double g = Channel(Convert.ToInt32(hex.Substring(3, 2), 16));
        double b = Channel(Convert.ToInt32(hex.Substring(5, 2), 16));

        return (0.2126 * r) + (0.7152 * g) + (0.0722 * b);
    }

    private static double Channel(int value)
    {
        double scaled = value / 255.0;

        return scaled <= 0.03928 ? scaled / 12.92 : Math.Pow((scaled + 0.055) / 1.055, 2.4);
    }
}
