namespace Sievert.Core.Cozumleme;

/// <summary>Metot uzunluklarının dağılımı. Ortalama tek başına yanıltıcı olduğu için hepsi bir arada.</summary>
/// <param name="Ortalama">Metot başına ortalama satır sayısı.</param>
/// <param name="Medyan">Metotların ortadaki değeri. Çift sayıda metotta iki ortancanın ortalaması.</param>
/// <param name="P90">Metotların yüzde 90'ı bu satır sayısının altında.</param>
/// <param name="P95">Metotların yüzde 95'i bu satır sayısının altında.</param>
/// <param name="EnUzun">En uzun metodun satır sayısı.</param>
public sealed record MetotUzunlugu(double Ortalama, double Medyan, int P90, int P95, int EnUzun);

/// <summary>Kapalı <c>#if</c> dalları yüzünden hiç bakamadığımız kod.</summary>
/// <param name="DosyaSayisi">Koşullu derleme içeren dosya sayısı.</param>
/// <param name="SatirSayisi">Kapalı dallarda kalan toplam satır sayısı.</param>
/// <param name="Orani">Bu satırların taranan toplam satıra oranı, 0 ile 1 arası.</param>
public sealed record KorNokta(int DosyaSayisi, int SatirSayisi, double Orani);

/// <summary>Üretim ve test kodunun ayrı sayıları.</summary>
/// <param name="UretimDosyaSayisi">Test gibi görünmeyen dosya sayısı.</param>
/// <param name="TestDosyaSayisi">Test gibi görünen dosya sayısı.</param>
/// <param name="UretimMetotSayisi">Üretim dosyalarındaki metot sayısı.</param>
/// <param name="TestMetotSayisi">Test dosyalarındaki metot sayısı.</param>
public sealed record KodAyrimi(int UretimDosyaSayisi, int TestDosyaSayisi, int UretimMetotSayisi, int TestMetotSayisi);

/// <summary>Bir tarama sonundaki toplu sayilar.</summary>
/// <param name="DosyaSayisi">Taranan dosya sayisi.</param>
/// <param name="TipSayisi">Bulunan tip sayisi.</param>
/// <param name="MetotSayisi">Bulunan metot sayisi.</param>
/// <param name="AsyncMetotSayisi">Bunlarin kaci async.</param>
/// <param name="AsyncOrani">Async metotlarin tum metotlara orani, 0 ile 1 arasi.</param>
/// <param name="MetotUzunlugu">Metot uzunluklarinin dagilimi.</param>
/// <param name="AyristirilamayanDosyaSayisi">En az bir sozdizimi hatasi olan dosya sayisi.</param>
/// <param name="TipBulunamayanDosyaSayisi">Okundu ama icinde hic tip cikmayan dosya sayisi.</param>
/// <param name="KorNokta">Kapali #if dallarinda kalan kod.</param>
/// <param name="KodAyrimi">Uretim ve test kodunun ayri sayilari.</param>
public sealed record TaramaOzeti(
    int DosyaSayisi,
    int TipSayisi,
    int MetotSayisi,
    int AsyncMetotSayisi,
    double AsyncOrani,
    MetotUzunlugu MetotUzunlugu,
    int AyristirilamayanDosyaSayisi,
    int TipBulunamayanDosyaSayisi,
    KorNokta KorNokta,
    KodAyrimi KodAyrimi);

/// <summary>Bir metodun hangi dosyada ve hangi tipin icinde oldugu.</summary>
public sealed record MetotYeri(string DosyaYolu, string TipAdi, SievertMetot Metot);
