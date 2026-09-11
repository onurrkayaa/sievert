using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

using Sievert.Core.Mining;

namespace Sievert.Cli;

/// <summary>
/// Commit'i tek satirlik JSON'a cevirir. Dosyanin tamami tek bir dizi degil, satir satir
/// JSONL: boylece yazan taraf butun commit'leri bellekte toplamak zorunda kalmiyor, okuyan
/// taraf da satir satir okuyabiliyor. Ayni sebeple ozet bu dosyaya yazilmiyor; dosyada tek
/// bir kayit turu var (ADR 0011).
/// </summary>
public static class MineJsonFormatter
{
    private static readonly JsonSerializerOptions Options = new()
    {
        // Satir satir okunacagi icin girinti yok: bir commit bir satir.
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static string Line(CommitRecord commit) => JsonSerializer.Serialize(commit, Options);
}
