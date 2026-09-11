using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

using Sievert.Core.Analysis;
using Sievert.Core.Rules;

namespace Sievert.Cli;

/// <summary>Tarama sonucunu JSON'a cevirir.</summary>
public static class JsonFormatter
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new TypeKindConverter(), new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
        // Cikti terminale gidiyor, HTML'e degil. Boyle olmazsa Task<int> "Task<int>" diye yaziliyor.
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    /// <summary>
    /// Dosya yollari scanRoot'a gore goreli yazilir, mutlak yol sadece scanRoot alaninda durur.
    /// Ayristirma hatalarinin hepsi burada yer alir; ekran ciktisi ilk birkacini gosteriyor.
    /// </summary>
    public static string Format(
        string scanRoot,
        IReadOnlyList<FileAnalysis> analyses,
        ScanSummary summary,
        IReadOnlyList<MethodLocation> longestMethods) =>
        JsonSerializer.Serialize(
            new ScanOutput(scanRoot, analyses, summary, longestMethods.Count == 0 ? null : longestMethods),
            Options);

    /// <summary>
    /// check komutunun ciktisi. Alan adlari scan ciktisiyla ayni kalipta: en disda tarama koku,
    /// sonra sonuc dizisi, sonra ozet.
    /// </summary>
    /// <param name="exemptions">
    /// Kuralin bulgu uretmeden gectigi metotlar. Hic yoksa alan JSON'a hic yazilmiyor;
    /// ekran ciktisinda ise hicbir zaman gorunmuyor.
    /// </param>
    public static string FormatCheck(
        string scanRoot,
        IReadOnlyList<Finding> findings,
        IReadOnlyList<Exemption> exemptions,
        CheckSummary summary) =>
        JsonSerializer.Serialize(
            new CheckOutput(scanRoot, findings, exemptions.Count == 0 ? null : exemptions, summary),
            Options);

    /// <summary>check ciktisinin en dis katmani. exemptions bos oldugunda hic yazilmaz.</summary>
    private sealed record CheckOutput(
        string ScanRoot,
        IReadOnlyList<Finding> Findings,
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        IReadOnlyList<Exemption>? Exemptions,
        CheckSummary Summary);

    /// <summary>JSON'un en dis katmani. longestMethods --top verilmediyse hic yazilmaz.</summary>
    private sealed record ScanOutput(
        string ScanRoot,
        IReadOnlyList<FileAnalysis> Files,
        ScanSummary Summary,
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        IReadOnlyList<MethodLocation>? LongestMethods);

    /// <summary>Tip turunu enum adiyla degil, agac ciktisindaki gibi C# anahtar kelimesiyle yazar.</summary>
    private sealed class TypeKindConverter : JsonConverter<SievertTypeKind>
    {
        public override SievertTypeKind Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            TypeKindNames.Parse(reader.GetString() ?? string.Empty);

        public override void Write(Utf8JsonWriter writer, SievertTypeKind value, JsonSerializerOptions options) =>
            writer.WriteStringValue(value.Keyword());
    }
}
