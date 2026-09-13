using System.Net;

using Sievert.Contracts;

namespace Sievert.Web.Api;

/// <summary>
/// Is baslatma istegi nasil sonuclandi.
///
/// <c>202</c> ile <c>200</c> ayri tutuluyor: ikincisi "ayni tekrar anahtariyla acilmis
/// bir is zaten vardi" demek ve kullaniciya yeni bir is actigini soylemek yanlis olur.
/// </summary>
public enum JobStartOutcome
{
    /// <summary>Yeni is acildi (<c>202</c>).</summary>
    Accepted,

    /// <summary>Ayni anahtarla acilmis is zaten vardi (<c>200</c>).</summary>
    AlreadyOpen,

    /// <summary>Ayni depo ve tur icin aktif bir is var (<c>409</c>).</summary>
    Conflict,

    /// <summary>Istek reddedildi.</summary>
    Refused,
}

/// <param name="Outcome">Nasil sonuclandi.</param>
/// <param name="Job">Acilan ya da zaten duran is.</param>
/// <param name="ActiveJobUrl">Catisma durumunda duran isin adresi.</param>
/// <param name="Problem">Reddedildiyse sebep.</param>
public sealed record JobStart(
    JobStartOutcome Outcome,
    AnalysisJobResponse? Job,
    string? ActiveJobUrl,
    ProblemResponse? Problem)
{
    public static JobStart From(HttpStatusCode status, AnalysisJobResponse job) => new(
        status == HttpStatusCode.Accepted ? JobStartOutcome.Accepted : JobStartOutcome.AlreadyOpen,
        job,
        null,
        null);
}

/// <summary>Iptal istegi nasil sonuclandi.</summary>
/// <param name="Immediate">
/// <c>200</c>: is kuyruktaydi ya da zaten bitmisti, durum su an kesin.
/// <c>202</c>: is kosuyor, gercek durus bir sonraki obek sinirinda.
/// </param>
public sealed record JobCancel(bool Immediate, AnalysisJobResponse? Job, ProblemResponse? Problem);
