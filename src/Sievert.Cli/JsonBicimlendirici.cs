using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

using Sievert.Core.Cozumleme;

namespace Sievert.Cli;

/// <summary>Tarama sonucunu JSON'a cevirir.</summary>
public static class JsonBicimlendirici
{
    private static readonly JsonSerializerOptions Ayarlar = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
        // Cikti terminale gidiyor, HTML'e degil. Boyle olmazsa Task<int> "Task\u003Cint\u003E" diye yaziliyor.
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static string Bicimlendir(
        IReadOnlyList<DosyaAnalizi> analizler,
        TaramaOzeti ozet,
        IReadOnlyList<MetotYeri> enUzunMetotlar) =>
        JsonSerializer.Serialize(
            new TaramaCiktisi(analizler, ozet, enUzunMetotlar.Count == 0 ? null : enUzunMetotlar),
            Ayarlar);

    /// <summary>JSON'un en dis katmani. enUzunMetotlar --top verilmediyse hic yazilmaz.</summary>
    private sealed record TaramaCiktisi(
        IReadOnlyList<DosyaAnalizi> Dosyalar,
        TaramaOzeti Ozet,
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        IReadOnlyList<MetotYeri>? EnUzunMetotlar);
}
