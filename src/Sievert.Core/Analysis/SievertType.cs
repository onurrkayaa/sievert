namespace Sievert.Core.Analysis;

/// <summary>Bir tipin hangi anahtar kelimeyle tanımlandığı.</summary>
/// <remarks>Roslyn'in kendi TypeKind'iyla karışmasın diye Sievert önekiyle adlandırıldı.</remarks>
public enum SievertTypeKind
{
    Class,
    Record,
    Struct,
    Interface,
    Enum,
    Delegate,
}

/// <summary>Bir dosyada bulunan tek bir tipin özeti.</summary>
/// <param name="Name">Tipin adı. İç içe tipler "Dis.Ic" şeklinde yazılır.</param>
/// <param name="Kind">class / record / struct / interface.</param>
/// <param name="StartLine">Tipin başladığı satır (1'den başlar).</param>
/// <param name="Methods">Tipin doğrudan içinde tanımlı metotlar. Enum ve delegate için her zaman boş.</param>
public sealed record SievertType(
    string Name,
    SievertTypeKind Kind,
    int StartLine,
    IReadOnlyList<SievertMethod> Methods);

/// <summary>Tip turunun C# anahtar kelimesi. Ekran ve JSON ciktisi ayni yeri kullansin diye burada.</summary>
public static class TypeKindNames
{
    public static string Keyword(this SievertTypeKind kind) => kind switch
    {
        SievertTypeKind.Class => "class",
        SievertTypeKind.Record => "record",
        SievertTypeKind.Struct => "struct",
        SievertTypeKind.Interface => "interface",
        SievertTypeKind.Enum => "enum",
        SievertTypeKind.Delegate => "delegate",
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Bilinmeyen tip turu."),
    };

    /// <summary>Anahtar kelimeden tip turune geri donus. JSON okunurken lazim.</summary>
    public static SievertTypeKind Parse(string keyword) => keyword switch
    {
        "class" => SievertTypeKind.Class,
        "record" => SievertTypeKind.Record,
        "struct" => SievertTypeKind.Struct,
        "interface" => SievertTypeKind.Interface,
        "enum" => SievertTypeKind.Enum,
        "delegate" => SievertTypeKind.Delegate,
        _ => throw new ArgumentOutOfRangeException(nameof(keyword), keyword, "Bilinmeyen tip turu."),
    };
}
