using Sievert.Contracts;

namespace Sievert.Web.Api;

/// <summary>
/// Bir hatanin ekranda gosterilen yuzu.
///
/// <see cref="ApiResult{T}"/> generic oldugu icin hata kutusu onu dogrudan alamiyor;
/// bu arayuz tek bir hata bileseninin butun sayfalarda kullanilmasini sagliyor.
/// </summary>
public interface IProblemView
{
    string? Title { get; }

    string? Detail { get; }

    string? ErrorCode { get; }

    string? TraceId { get; }
}

/// <summary>
/// Sonuc tasimayan bir hata; is baslatma ve iptal cevaplarinda kullaniliyor.
///
/// Adi <c>ProblemView</c> DEGIL: o ad hata kutusu bileseninin adi ve ikisi ayni ad
/// alaninda gorununce Razor hangisini kastettigimizi bilemiyor.
/// </summary>
public sealed record ProblemSummary(string? Title, string? Detail, string? ErrorCode, string? TraceId) : IProblemView
{
    public static ProblemSummary From(ProblemResponse problem) =>
        new(problem.Title ?? "Istek karsilanamadi", problem.Detail, problem.ErrorCode, problem.TraceId);
}

/// <summary>
/// Bir API cagrisinin sonucu: ya deger, ya sorun.
///
/// Istisna firlatmak yerine sonuc dondurmek bilincli. Panelde her sayfa ayni uc durumu
/// gostermek zorunda - yukleniyor, veri, hata - ve hatayi istisna olarak tasimak her
/// bileseni try/catch ile sarmak demek olurdu. Ustelik <c>ProblemDetails</c> cevabi bir
/// kaza degil, API'nin normal cevaplarindan biri.
/// </summary>
/// <typeparam name="T">Basari durumunda donen tip.</typeparam>
public sealed record ApiResult<T> : IProblemView
{
    private ApiResult(T? value, ProblemResponse? problem, string? transportError)
    {
        Value = value;
        Problem = problem;
        TransportError = transportError;
    }

    public T? Value { get; }

    /// <summary>API cevap verdi ama istegi reddetti.</summary>
    public ProblemResponse? Problem { get; }

    /// <summary>API'ye hic ulasilamadi ya da cevabi okunamadi.</summary>
    public string? TransportError { get; }

    public bool IsSuccess => Problem is null && TransportError is null;

    /// <summary>Kullaniciya gosterilecek hata basligi; basarida null.</summary>
    public string? Title => TransportError is not null
        ? "API'ye ulasilamadi"
        : Problem?.Title;

    /// <summary>Kullaniciya gosterilecek aciklama; basarida null.</summary>
    public string? Detail => TransportError ?? Problem?.Detail;

    public string? ErrorCode => Problem?.ErrorCode;

    public string? TraceId => Problem?.TraceId;

    public static ApiResult<T> Ok(T value) => new(value, null, null);

    public static ApiResult<T> Refused(ProblemResponse problem) => new(default, problem, null);

    public static ApiResult<T> Unreachable(string message) => new(default, null, message);
}
