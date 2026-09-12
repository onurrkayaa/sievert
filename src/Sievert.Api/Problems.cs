using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Sievert.Api;

/// <summary>
/// Butun hatalar ayni bicimde doner: RFC 7807 ProblemDetails, uzerine makine okunabilir
/// bir <c>errorCode</c> ve istek izleme kimligi.
///
/// Dahili istisna ayrintisi, dosya yolu ve baglanti dizesi cevaba KOYULMAZ; bunlar
/// yalnizca sunucu gunlugunde kalir (ADR 0023).
/// </summary>
public static class Problems
{
    public static IResult Create(HttpContext context, int status, string title, string detail, string errorCode)
    {
        ProblemDetails problem = new()
        {
            Type = $"https://sievert.invalid/errors/{errorCode.ToLowerInvariant().Replace('_', '-')}",
            Title = title,
            Status = status,
            Detail = detail,
        };

        problem.Extensions["errorCode"] = errorCode;
        problem.Extensions["traceId"] = context.TraceIdentifier;

        return Results.Problem(
            title: problem.Title,
            detail: problem.Detail,
            statusCode: problem.Status,
            type: problem.Type,
            extensions: problem.Extensions);
    }

    public static IResult NotFound(HttpContext context, string detail, string errorCode) =>
        Create(context, StatusCodes.Status404NotFound, "Bulunamadi", detail, errorCode);

    public static IResult BadRequest(HttpContext context, string detail, string errorCode) =>
        Create(context, StatusCodes.Status400BadRequest, "Gecersiz istek", detail, errorCode);

    public static IResult Conflict(HttpContext context, string detail, string errorCode) =>
        Create(context, StatusCodes.Status409Conflict, "Istek bu veri durumuyla karsilanamiyor", detail, errorCode);

    public static IResult Unprocessable(HttpContext context, string detail, string errorCode) =>
        Create(context, StatusCodes.Status422UnprocessableEntity, "Istek islenemiyor", detail, errorCode);

    public static IResult Unavailable(HttpContext context, string detail, string errorCode) =>
        Create(context, StatusCodes.Status503ServiceUnavailable, "Servis hazir degil", detail, errorCode);
}
