namespace Sievert.Core.Cozumleme;

/// <summary>Bir tarama sonundaki toplu sayilar.</summary>
/// <param name="DosyaSayisi">Taranan dosya sayisi.</param>
/// <param name="TipSayisi">Bulunan tip sayisi.</param>
/// <param name="MetotSayisi">Bulunan metot sayisi.</param>
/// <param name="AsyncMetotSayisi">Bunlarin kaci async.</param>
/// <param name="AsyncOrani">Async metotlarin tum metotlara orani, 0 ile 1 arasi.</param>
/// <param name="OrtalamaMetotUzunlugu">Metot basina ortalama satir sayisi.</param>
/// <param name="AyristirilamayanDosyaSayisi">En az bir sozdizimi hatasi olan dosya sayisi.</param>
/// <param name="TipBulunamayanDosyaSayisi">Okundu ama icinde hic tip cikmayan dosya sayisi.</param>
public sealed record TaramaOzeti(
    int DosyaSayisi,
    int TipSayisi,
    int MetotSayisi,
    int AsyncMetotSayisi,
    double AsyncOrani,
    double OrtalamaMetotUzunlugu,
    int AyristirilamayanDosyaSayisi,
    int TipBulunamayanDosyaSayisi);

/// <summary>Bir metodun hangi dosyada ve hangi tipin icinde oldugu.</summary>
public sealed record MetotYeri(string DosyaYolu, string TipAdi, SievertMetot Metot);
