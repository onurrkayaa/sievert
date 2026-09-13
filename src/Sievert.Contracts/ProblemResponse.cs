using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sievert.Contracts;

/// <summary>
/// API'nin hata cevabinin istemci tarafindaki karsiligi (RFC 7807).
///
/// ASP.NET'in kendi <c>ProblemDetails</c> tipini kullanmiyorum: bu proje sozlesme
/// projesi ve icine web cercevesi girmemeli. Alanlar birebir ayni, istemci ayni JSON'u
/// okuyor.
///
/// <see cref="ErrorCode"/> makine tarafindan okunur ve degismez; <see cref="Detail"/>
/// serbest metin ve degisebilir. Istemci koda gore dallanmali, metne gore degil.
/// </summary>
public sealed record ProblemResponse
{
    public string? Type { get; init; }

    public string? Title { get; init; }

    public int? Status { get; init; }

    public string? Detail { get; init; }

    [JsonPropertyName("errorCode")]
    public string? ErrorCode { get; init; }

    [JsonPropertyName("traceId")]
    public string? TraceId { get; init; }

    /// <summary>
    /// Koda ozel ek alanlar; ornegin catisma cevabindaki <c>activeJobUrl</c>.
    /// Bilinmeyen alanlar burada toplaniyor, cevap ayristirmasi onlar yuzunden kirilmiyor.
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? Extensions { get; init; }

    /// <summary>Ek alanlardan bir metin okur; yoksa null.</summary>
    public string? Extension(string name) =>
        Extensions is not null
        && Extensions.TryGetValue(name, out JsonElement value)
        && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
}
