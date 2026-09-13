using System.Text;
using System.Text.RegularExpressions;

using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Sievert.Api.Reports;

/// <summary>Uretilen PDF: baytlar ve sayfa sayisi.</summary>
public sealed record RenderedReport(byte[] Content, int PageCount);

/// <summary>
/// Rapor modelini PDF baytlarina cevirir.
///
/// Lisans secimi burada, acikca yapiliyor: QuestPDF cift lisansli ve varsayilana
/// guvenmek, lisans kararini koddan okunamaz hale getirirdi. Gerekce ve uygunluk kanidi
/// <c>docs/urun/pdf-kutuphanesi-ve-lisans.md</c> icinde.
/// </summary>
public static partial class ReportGenerator
{
    private static int licenseSet;

    public static RenderedReport Render(ReportModel model, ReportText text)
    {
        ApplyLicense();

        byte[] content = new ReportDocument(model, text).GeneratePdf();

        return new RenderedReport(content, CountPages(content));
    }

    /// <summary>
    /// Lisans secimi bir kez uygulaniyor. Idempotent: worker her rapor icin cagirsa da
    /// ayni sonucu veriyor.
    /// </summary>
    public static void ApplyLicense()
    {
        if (Interlocked.Exchange(ref licenseSet, 1) == 0)
        {
            QuestPDF.Settings.License = LicenseType.Community;
        }
    }

    /// <summary>
    /// PDF'in sayfa sayisi.
    ///
    /// Kutuphane sayfa sayisi vermiyor, o yuzden uretilen belgedeki sayfa nesneleri
    /// sayiliyor. Sayi <c>pdfinfo</c> ile ayrica karsilastirildi; gorsel QA bolumunde
    /// yaziyor.
    /// </summary>
    public static int CountPages(byte[] content)
    {
        string text = Encoding.Latin1.GetString(content);

        return PageObject().Matches(text).Count;
    }

    // "/Type /Page" ama "/Pages" degil: ikincisi sayfa agacinin kokundeki nesne.
    [GeneratedRegex(@"/Type\s*/Page(?![s])")]
    private static partial Regex PageObject();
}
