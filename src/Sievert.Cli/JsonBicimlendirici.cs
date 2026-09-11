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
        Converters = { new TipTuruDonusturucu() },
        // Cikti terminale gidiyor, HTML'e degil. Boyle olmazsa Task<int> "Task<int>" diye yaziliyor.
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    /// <summary>
    /// Dosya yollari taramaKoku'ne gore goreli yazilir, mutlak yol sadece taramaKoku alaninda durur.
    /// Ayristirma hatalarinin hepsi burada yer alir; ekran ciktisi ilk birkacini gosteriyor.
    /// </summary>
    public static string Bicimlendir(
        string taramaKoku,
        IReadOnlyList<DosyaAnalizi> analizler,
        TaramaOzeti ozet,
        IReadOnlyList<MetotYeri> enUzunMetotlar) =>
        JsonSerializer.Serialize(
            new TaramaCiktisi(taramaKoku, analizler, ozet, enUzunMetotlar.Count == 0 ? null : enUzunMetotlar),
            Ayarlar);

    /// <summary>JSON'un en dis katmani. enUzunMetotlar --top verilmediyse hic yazilmaz.</summary>
    private sealed record TaramaCiktisi(
        string TaramaKoku,
        IReadOnlyList<DosyaAnalizi> Dosyalar,
        TaramaOzeti Ozet,
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        IReadOnlyList<MetotYeri>? EnUzunMetotlar);

    /// <summary>Tip turunu enum adiyla degil, agac ciktisindaki gibi C# anahtar kelimesiyle yazar.</summary>
    private sealed class TipTuruDonusturucu : JsonConverter<TipTuru>
    {
        public override TipTuru Read(ref Utf8JsonReader okuyucu, Type tip, JsonSerializerOptions ayarlar) =>
            TipTuruAdlari.Cozumle(okuyucu.GetString() ?? string.Empty);

        public override void Write(Utf8JsonWriter yazici, TipTuru deger, JsonSerializerOptions ayarlar) =>
            yazici.WriteStringValue(deger.AnahtarKelime());
    }
}
