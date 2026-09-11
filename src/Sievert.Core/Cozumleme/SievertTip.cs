namespace Sievert.Core.Cozumleme;

/// <summary>Bir tipin hangi anahtar kelimeyle tanımlandığı.</summary>
public enum TipTuru
{
    Sinif,
    Record,
    Struct,
    Interface,
    Enum,
    Delegate,
}

/// <summary>Bir dosyada bulunan tek bir tipin özeti.</summary>
/// <param name="Ad">Tipin adı. İç içe tipler "Dis.Ic" şeklinde yazılır.</param>
/// <param name="Turu">class / record / struct / interface.</param>
/// <param name="BaslangicSatiri">Tipin başladığı satır (1'den başlar).</param>
/// <param name="Metotlar">Tipin doğrudan içinde tanımlı metotlar. Enum ve delegate için her zaman boş.</param>
public sealed record SievertTip(
    string Ad,
    TipTuru Turu,
    int BaslangicSatiri,
    IReadOnlyList<SievertMetot> Metotlar);

/// <summary>Tip turunun C# anahtar kelimesi. Ekran ve JSON ciktisi ayni yeri kullansin diye burada.</summary>
public static class TipTuruAdlari
{
    public static string AnahtarKelime(this TipTuru turu) => turu switch
    {
        TipTuru.Sinif => "class",
        TipTuru.Record => "record",
        TipTuru.Struct => "struct",
        TipTuru.Interface => "interface",
        TipTuru.Enum => "enum",
        TipTuru.Delegate => "delegate",
        _ => throw new ArgumentOutOfRangeException(nameof(turu), turu, "Bilinmeyen tip turu."),
    };

    /// <summary>Anahtar kelimeden tip turune geri donus. JSON okunurken lazim.</summary>
    public static TipTuru Cozumle(string anahtarKelime) => anahtarKelime switch
    {
        "class" => TipTuru.Sinif,
        "record" => TipTuru.Record,
        "struct" => TipTuru.Struct,
        "interface" => TipTuru.Interface,
        "enum" => TipTuru.Enum,
        "delegate" => TipTuru.Delegate,
        _ => throw new ArgumentOutOfRangeException(nameof(anahtarKelime), anahtarKelime, "Bilinmeyen tip turu."),
    };
}
