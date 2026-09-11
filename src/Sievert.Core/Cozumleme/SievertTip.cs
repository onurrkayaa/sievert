namespace Sievert.Core.Cozumleme;

/// <summary>Bir tipin hangi anahtar kelimeyle tanımlandığı.</summary>
public enum TipTuru
{
    Sinif,
    Record,
    Struct,
    Interface,
}

/// <summary>Bir dosyada bulunan tek bir tipin özeti.</summary>
/// <param name="Ad">Tipin adı. İç içe tipler "Dis.Ic" şeklinde yazılır.</param>
/// <param name="Turu">class / record / struct / interface.</param>
/// <param name="BaslangicSatiri">Tipin başladığı satır (1'den başlar).</param>
/// <param name="Metotlar">Tipin doğrudan içinde tanımlı metotlar.</param>
public sealed record SievertTip(
    string Ad,
    TipTuru Turu,
    int BaslangicSatiri,
    IReadOnlyList<SievertMetot> Metotlar);
