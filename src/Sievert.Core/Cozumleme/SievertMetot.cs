namespace Sievert.Core.Cozumleme;

/// <summary>Bir dosyada bulunan tek bir metodun özeti.</summary>
/// <param name="Ad">Metodun adı.</param>
/// <param name="BaslangicSatiri">Metodun başladığı satır (1'den başlar).</param>
/// <param name="SatirSayisi">İmzadan gövdenin sonuna kadar kaç satır tuttuğu.</param>
/// <param name="AsyncMi">Metot async olarak işaretlenmiş mi.</param>
/// <param name="ParametreSayisi">Metodun aldığı parametre sayısı.</param>
/// <param name="DonusTipi">Dönüş tipinin kaynak koddaki yazılışı, örneğin "Task&lt;int&gt;".</param>
public sealed record SievertMetot(
    string Ad,
    int BaslangicSatiri,
    int SatirSayisi,
    bool AsyncMi,
    int ParametreSayisi,
    string DonusTipi);
