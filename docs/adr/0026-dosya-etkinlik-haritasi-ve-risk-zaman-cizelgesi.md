# 0026 - Dosya etkinlik haritasi ve risk zaman cizelgesi

**Baglam:** Adim 4 panelinde her sey tabloydu. Tablo dogru ama okunmasi yavas: yuz
satirin icinden "bu pencerede en cok hangi dosyalar donuyor" sorusunun cevabini bulmak
goz isi. Bu turda ayni sayilar renk ve koordinata cevrildi.

Gorsellestirmenin kendine ozgu bir riski var: **yanlis bir gorsel kodlama hata vermez.**
Renk sirasi bozuksa kimse istisna gormez, sayfa acilir, grafik "guzel" gorunur ve okuyan
kisi yanlis sonuc cikarir. Bu ADR o riski azaltmak icin alinan kararlari tutuyor.

**Karar:** Grafikler C# tarafinda uretilen SVG ve CSS izgarasi; renk ve koordinat tek bir
yerden geliyor; renk yalniz `RiskIndex` ozetinden hesaplaniyor; her cevap hangi kumede
siralandigini ve sonucun tam olup olmadigini soyluyor.

## Neden grafik kutuphanesi yok

Dis bir grafik paketi de, CDN'den inen bir betik de eklenmedi. Uc sebep:

1. **Internet bagimliligi.** Arac yerelde, kapali bir makinede de calismali. CDN'den
   inen bir betik, o makinede bos bir kutu demek.
2. **Sozlesme ikiye bolunurdu.** Renk ve konum hesabi JavaScript'e gecseydi, ayni
   kurallar iki dilde yazili olurdu; zamanla ayrisirlardi.
3. **Test edilebilirlik.** Renk ve koordinat C# fonksiyonu oldugu icin birim testiyle
   sinaniyor: "endeks buyudukce renk geri gitmiyor" cumlesi bir iddia degil, bir assert.

Bedeli: yakinlastirma, tooltip, animasyon gibi hazir seyler yok. Grafikler sabit; ayrinti
icin hucreye tiklaniyor ve yanina tablo aciliyor.

## Renk neyi gosteriyor, neyi gostermiyor

Bir dosyanin rengi **o dosyanin kusurlu oldugunu gostermez.** Gosterdigi sey:
secilen commit penceresinde o dosyaya dokunan commit'lerin **goreli risk endekslerinin
ozeti** (varsayilan olarak ortalamasi).

Bu ayrimi korumak icin:

- Renk olceginin efsanesinde endeksin goreli oldugu yazili.
- Hucrede endeksin sayisi da yazili; renk tek basina birakilmiyor.
- Ham model skoru ile endeks ayni eksende gosterilmiyor.
- Yuzde isareti kullanilmiyor.

## Neden renk metnin arkasinda degil, ayri bir cubukta

Ilk tasarimda hucrenin arka plani dogrudan olcek rengiydi. Kontrast olcumu bunun
calismadigini gosterdi: koyu-acik gecen her olcegin ortasinda bir bant var ve orada ne
koyu ne acik yazi 4,5:1 orani tutturuyor. Olculen en kotu deger 4,45 idi.

Cozum olcegi degistirmek degil, **metni olcek renginden cikarmak** oldu: renk ayri bir
cubukta duruyor, hucrenin arka plani yuzeyin uzerine %18 tonlanmis hali. Boylece yazi
her zaman ayni zeminde ve kontrast endeksten bagimsiz.

## Pencere ve dosya siniri

Iki sinir var: kac commit geriye bakildigi (`commitWindow`, varsayilan 200) ve kac dosya
gosterildigi (`limit`, varsayilan 100).

Varsayilanlar sonuc gorulmeden secildi ve beklenti dosyasina yazildi. Olcum sonradan
ikisinin farkli sey maliyet ettigini gosterdi: sure pencereye bagli, cevap boyutu limite
(`docs/olcumler/asama6-gorsellestirme.md` bolum 3). Sayfa, gosterilen dosya sayisini ve
kesme olup olmadigini yaziyor; "ilk 100" ile "hepsi bu" karistirilmasin diye.

## Siralama kumesi cevapta yazili

Bir is iptal edilmis ama bir kisim satir yazmis olabilir. O zaman siralama **yazilmis
sonuclar icinde** yapiliyor, butun depo icinde degil. Cevapta bunu soyleyen bir alan var
(`rankingScope`) ve panelde de banner olarak gorunuyor.

Sebebi: kismi bir sonucun "en riskli 100 dosyasi", tam sonucun en riskli 100 dosyasi
degildir. Bu bilgi opsiyonel olsaydi arayuzun bir yerinde unutulurdu; zorunlu alan olarak
duruyor.

## Statik bulgular renge girmiyor

Statik tarama bulgulari haritada rozet olarak gorunuyor ama **renge dahil degil**.
Model skoru ile kural tabanli bulgulari toplamak birlesik bir skor uretmek olurdu; o
karar alinmadi (ADR 0023) ve bu turda da alinmiyor. Hucre acildiginda bulgu sayisinin
yanina "renge dahil degil" yaziyor.

## Zaman cizelgesinde iki referans cizgisi

Grafikte iki yatay cizgi var: modelin kaynak esigi ve 0,5. Ikisi ayri, cunku ayni sey
degiller - biri secilen profilin esigi, digeri ham skorun orta noktasi. Ust uste
dustuklerinde bunu soyleyen bir uyari kodu var (`THRESHOLDS_COINCIDE`), yoksa tek cizgi
gorunur ve okuyan kisi digerinin olmadigini sanardi.

## Kalici durum sinirli ve grafik verisi ona yazilmiyor

Blazor Web App sayfayi once sunucuda on-isliyor, sonra devre acilinca yeniden olusturuyor.
`PersistentComponentState` bu iki olusturma arasinda veriyi tasiyor, boylece ayni veri iki
kez cekilmiyor.

Ama kalici durum **devre acilirken istemciden sunucuya gonderiliyor** ve Blazor Server'in
varsayilan mesaj siniri 32 KB. Bu tur ilk denemede sinir asildi: sayfa aciliyor, grafik
ciziliyor ama **hicbir tiklama calismiyordu**, sunucu gunlugunde de hicbir sey yoktu.

Uc karar bundan cikti:

1. Anahtar basina ve sayfa basina bir butce var (8 KB / 12 KB) ve asilirsa durum
   yazilmiyor - sayfa yavaslar ama calisir.
2. Kalici duruma yazilan sey kirpilmis bir ozet; is kaydinin buyuk `resultSummary` alani
   disarida.
3. **Harita ve zaman cizelgesi verisi kalici duruma hic girmiyor.** Bu iki bilesen
   veriyi yalniz devre acikken cekiyor; on-islemede istek atmiyor. Yani veri devreden
   gecmiyor ve sayfa basina tek istek gidiyor.

Bedeli durust olmak gerekirse su: grafikler on-islenen HTML'de yok, devre acildiktan
sonra geliyor. Olculen fark soguk onbellekte 120 ms boyama, 173 ms grafik.

## Siralamada esitlik bozucu

Alti siralama secenegi var ve hepsinde esitlik durumunda dosya yolu ile ikinci bir
karsilastirma yapiliyor. Sebep: ayni istegin iki kez ayni sonucu vermesi. Esitlik bozucu
olmasaydi veritabaninin dondurdugu sira degistiginde harita da degisirdi ve bu "veri
degisti" gibi gorunurdu.

## Sayilarin bagimsiz dogrulanmasi

Uclerin dondugu sayilar, ayni kodu tekrar cagirarak degil, **ham tablolardan yeniden
hesaplanarak** kontrol ediliyor (`measure visualization-truth`). Sorgu yapisi bilerek
farkli. Ilk kosuda 30 dosya ve 150 nokta icin 0 fark cikti; cikti
`data/asama6/gorsellestirme-dogrulama.json`.

Bunu ayri bir arac olarak yazmanin sebebi: endpoint'i kendi koduyla test etmek, ayni
hatayi iki kere yapmayi yakalamaz.

## Sonuclari

- Panelde grafik var ama hala her sayinin yaninda metni duruyor.
- Renk olcegi tek bir dosyada; degistirilmesi gereken tek yer orasi.
- Gorsellestirme verisi devreden gecmiyor, bu yuzden 32 KB sinirina takilmiyor.
- Bir grafik kutuphanesi eklenmedigi icin yakinlastirma/tooltip yok; ihtiyac olursa bu
  karar yeniden acilir.
