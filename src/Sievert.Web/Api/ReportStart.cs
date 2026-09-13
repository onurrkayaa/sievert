using Sievert.Contracts;

namespace Sievert.Web.Api;

/// <summary>
/// Rapor istegi nasil sonuclandi.
///
/// Is baslatmadan farkli olarak 202 ile 200 ayrilmiyor: ikisinde de kullanici ayni rapor
/// sayfasina gidiyor ve orada raporun durumu zaten yaziyor. "Zaten vardi" bilgisini ayri
/// bir mesajla vermek, ayni sonuca iki farkli yoldan bakmak olurdu.
/// </summary>
/// <param name="Report">Kabul edilen rapor.</param>
/// <param name="Problem">Reddedildiyse sebep.</param>
public sealed record ReportStart(ReportAcceptedResponse? Report, ProblemResponse? Problem);
