using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

using Sievert.Core.Analysis;

namespace Sievert.Cli;

/// <summary>Tarama sonucunu JSON'a cevirir.</summary>
public static class JsonFormatter
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new TypeKindConverter() },
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
