# Gorsellestirme olcumu

**Tarih:** 2026-09-13
**Beklentiler:** `docs/olcumler/asama6-gorsellestirme-beklenti.md`, kod yazilmadan once yazildi.
**Ureten kod:** `91a83f7`.

Bu dosyanin 1. ve 2. bolumu **olcum kosulmadan once** yazildi ve kendi commit'inde
duruyor; sonuclar sonraki commit'te eklendi. Sebebi basit: hangi kombinasyonlari olctugumu sonuclara bakarak
secersem, iyi gorunen kombinasyonlari secmis olurum.

## 1. Kosulun sabitlenmesi

- Derleme **Release**, `dotnet publish` ciktisindan kosuyor.
- API ve panel **ayri sureclerde**, ikisi de loopback'te.
- Veritabani ayni makinede, Docker'da (PostgreSQL 17.11).
- Tek istemci.
- Uc sureleri icin **tarayici yok**: tek bir `HttpClient` istegi atiyor.
- Tarayici olcumu ayri kosuyor ve orada **soguk ve isinmis onbellek ayri** yaziliyor.
- Her kombinasyon icin **5 kosu**. Ortanca ve 5 kosudaki **en yuksek** deger yaziliyor;
  p95 yazmiyorum, 5 kosudan p95 hesaplamak olculmemis bir kesinlik iddiasi olurdu.
- Her depo icin bir **isinma istegi** var ve sonuclara girmiyor.
- Yalniz **tamamlanmis** (kismi olmayan) risk isleri olculuyor.
- Ekran goruntusu kosusu ayri; bu kosuda goruntu alinmiyor.
- Makine: Apple M2, 8 cekirdek, 16 GB, macOS 26.6.2.

## 2. Matris secimi (olcumden once yazildi)

Butun carpimi kosmak - 3 depo x 5 pencere x 4 limit x 6 siralama x 5 kosu - binlerce
istek ederdi. Onun yerine su temsilci matris secildi:

**Dosya etkinlik haritasi.** Uc depo (Polly, ShareX, Jellyfin) x

- pencere **50 / 200 / 1000**, limit varsayilanda (100) sabit
- limit **50 / 100 / 200**, pencere varsayilanda (200) sabit

Yani pencere ve limit ayri ayri gezdiriliyor, ikisi birden degil. Sebebi: sorgu
maliyetinin pencereye, cevap boyutunun limite bagli oldugunu bekliyorum; carpimi
kosmadan ikisini ayri gormek yeterli. Siralama sabit (`mean-risk-desc`, varsayilan)
cunku siralama bellekte yapiliyor, sorguyu degistirmiyor.

**Zaman cizelgesi.** Uc depo x nokta sayisi **50 / 100 / 500** (en kucuk secim,
varsayilan, en buyuk secim).

**Panel sayfalari.** Ilk depo icin harita ve zaman sekmesinin HTML suresi.

Bu secim degistirilmeyecek; sonuclar bu matrisin sonuclari olarak yaziliyor.

## 3. Dosya etkinlik haritasi ucunun sureleri

Cikti: `data/asama6/gorsellestirme-performans.json`. "Satir" cevaptaki dosya sayisi,
"komut" o istek sirasinda API'nin calistirdigi veritabani komutu.

Pencere gezdiriliyor, limit varsayilanda (100):

| Depo | Pencere | Satir | Cevap | Ortanca | En dusuk | 5 kosuda en yuksek | Komut |
|---|---|---|---|---|---|---|---|
| jellyfin-full | 50 | 68 | 57.8 KB | 15.8 ms | 11.6 ms | 21.6 ms | 5 |
| jellyfin-full | 200 | 100 | 80.6 KB | 19.2 ms | 16.2 ms | 23.7 ms | 5 |
| jellyfin-full | 1000 | 100 | 70.8 KB | 31.9 ms | 25.9 ms | 49.4 ms | 5 |
| polly-full | 50 | 16 | 11.6 KB | 23.5 ms | 16.2 ms | 28.7 ms | 5 |
| polly-full | 200 | 51 | 37.5 KB | 49.9 ms | 45.8 ms | 52.1 ms | 5 |
| polly-full | 1000 | 100 | 88.9 KB | 50.8 ms | 33.2 ms | 61.0 ms | 5 |
| sharex-full | 50 | 100 | 79.9 KB | 18.0 ms | 14.7 ms | 24.0 ms | 5 |
| sharex-full | 200 | 100 | 86.2 KB | 25.4 ms | 21.8 ms | 26.2 ms | 5 |
| sharex-full | 1000 | 100 | 98.6 KB | 58.3 ms | 44.1 ms | 61.9 ms | 5 |

Limit gezdiriliyor, pencere varsayilanda (200):

| Depo | Limit | Satir | Cevap | Ortanca | 5 kosuda en yuksek | Komut |
|---|---|---|---|---|---|---|
| jellyfin-full | 50 | 50 | 37.8 KB | 15.2 ms | 19.7 ms | 5 |
| jellyfin-full | 100 | 100 | 80.6 KB | 8.0 ms | 20.0 ms | 5 |
| jellyfin-full | 200 | 185 | 162.4 KB | 24.0 ms | 30.0 ms | 5 |
| polly-full | 50 | 50 | 36.8 KB | 38.3 ms | 44.0 ms | 5 |
| polly-full | 100 | 51 | 37.5 KB | 49.4 ms | 66.4 ms | 5 |
| polly-full | 200 | 51 | 37.5 KB | 45.7 ms | 53.6 ms | 5 |
| sharex-full | 50 | 50 | 45.1 KB | 23.9 ms | 25.1 ms | 5 |
| sharex-full | 100 | 100 | 86.2 KB | 26.4 ms | 28.4 ms | 5 |
| sharex-full | 200 | 200 | 155.6 KB | 23.8 ms | 28.1 ms | 5 |

Uc sey goruluyor:

1. **Sure pencereye bagli, limite degil.** Pencere 50'den 1000'e cikinca ortanca
   ikiye-uce katlaniyor (sharex 18.0 -> 58.3 ms). Limit 50'den 200'e cikarken ayni
   depoda ortanca neredeyse sabit (23.9 -> 23.8 ms). Beklenen buydu: pencere okunan
   commit sayisini, limit yalniz kesilen listenin boyunu degistiriyor.
2. **Veritabani komut sayisi her zaman 5.** Pencere ya da limit ne olursa olsun
   degismiyor; yani dosya sayisiyla artan bir sorgu yok.
3. **Limit her zaman dolmuyor.** Polly'de 200 commit'lik pencerede 51 dosya var, limit
   100 ya da 200 olmasi bir sey degistirmiyor. Jellyfin'de limit 200 iken 185 satir
   donuyor; 100 iken tam 100. Yani "limit 100" cogu depoda gercekten kesiyor.

`nokta 500` gibi buyuk secimlerde cevap 290-305 KB'a cikiyor. Bu HTTP cevabinin boyutu;
sayfaya giden HTML bundan cok daha kucuk, cunku panel veriyi ozetleyip ciziyor.

## 4. Zaman cizelgesi ucunun sureleri

| Depo | Nokta | Satir | Cevap | Ortanca | En dusuk | 5 kosuda en yuksek | Komut |
|---|---|---|---|---|---|---|---|
| jellyfin-full | 50 | 50 | 29.0 KB | 8.1 ms | 6.5 ms | 58.2 ms | 4 |
| jellyfin-full | 100 | 100 | 58.2 KB | 11.2 ms | 7.5 ms | 14.5 ms | 4 |
| jellyfin-full | 500 | 500 | 290.0 KB | 30.0 ms | 26.7 ms | 32.8 ms | 4 |
| polly-full | 50 | 50 | 30.9 KB | 19.4 ms | 7.4 ms | 22.0 ms | 4 |
| polly-full | 100 | 100 | 61.8 KB | 25.9 ms | 13.6 ms | 28.7 ms | 4 |
| polly-full | 500 | 500 | 305.0 KB | 40.5 ms | 35.8 ms | 41.9 ms | 4 |
| sharex-full | 50 | 50 | 29.8 KB | 11.1 ms | 4.7 ms | 13.4 ms | 4 |
| sharex-full | 100 | 100 | 58.9 KB | 14.7 ms | 11.2 ms | 16.1 ms | 4 |
| sharex-full | 500 | 500 | 295.0 KB | 22.0 ms | 14.3 ms | 39.4 ms | 4 |

Sure nokta sayisiyla artiyor ama dogrusal degil: 50'den 500'e, yani on kat noktaya
cikarken ortanca en fazla ikiye katlaniyor. Komut sayisi hep 4.

Jellyfin'de `nokta 50` satirinin **en yuksek degeri 58.2 ms**, ortancasi 8.1 ms. Bu tek
bir kosudaki sicrama; sebebini olcmedim, o yuzden bir aciklama uydurmuyorum. Ayni
kombinasyonun diger dort kosusu 6.5-9 ms araliginda.

## 5. Panel sayfalarinin sunucu suresi

Tarayici yok; tek bir `HttpClient` sayfayi istiyor, yani olculen sey on-islemenin suresi.

| Sayfa | Ilk kosu | Ortanca | 5 kosuda en yuksek | HTML | API istegi |
|---|---|---|---|---|---|
| Dosya haritasi | 122.4 ms | 43.5 ms | 122.4 ms | 42.1 KB | 4 |
| Zaman cizelgesi | 43.1 ms | 36.0 ms | 76.9 ms | 42.0 KB | 4 |

**On-islenen HTML'de harita ve grafik yok.** Bunu dogrulamak icin cevabi diske yazip
arattim: `map-grid` 0 kez, `class="chart"` 0 kez geciyor. Bu bilerek boyle: gorsellestirme
verisi yalniz devre baglandiktan sonra cekiliyor (bolum 7). Yani 42 KB HTML sayfanin
cercevesi - baslik, sekmeler, arac cubugu, is listesi.

## 6. Tarayicida olculen sayilar

Cikti: `data/asama6/gorsellestirme-tarayici.json`. Gercek tarayici, sharex-full deposu,
tamamlanmis bir risk isi. Her satir 5 kosunun ortancasi; parantez icinde 5 kosudaki en
yuksek deger.

**Soguk onbellek** (her kosu icin yeni tarayici baglami):

| Sayfa | Ilk boyama (FCP) | Gorsel hazir | HTML | Varliklar | Agdan inen istek |
|---|---|---|---|---|---|
| Dosya haritasi | 120 ms (128) | 172.6 ms (184.8) | 42.7 KB | 54.2 KB | 7 |
| Zaman cizelgesi | 116 ms (128) | 127.9 ms (144.6) | 42.7 KB | 54.2 KB | 7 |

**Isinmis onbellek** (ayni baglamda sayfa tekrar aciliyor):

| Sayfa | Ilk boyama (FCP) | Gorsel hazir | HTML | Varliklar | Agdan inen istek |
|---|---|---|---|---|---|
| Dosya haritasi | 72 ms (76) | 108.1 ms (113.2) | 42.7 KB | 0.9 KB | 2 |
| Zaman cizelgesi | 64 ms (76) | 72.8 ms (93.5) | 42.7 KB | 0.9 KB | 2 |

"Gorsel hazir" = haritada `.map-grid`, cizelgede `svg.chart` belgeye girdigi an. Olcum
icin sayfaya urun kodu eklenmedi; tarayiciya sayfa acilmadan once bir gozlemci
(MutationObserver) takildi, o yazdi.

Onbellek isinmasinin kazandirdigi sey varlik indirme: 54.2 KB'lik varlik trafigi 0.9
KB'a dusuyor, ag istegi 7'den 2'ye iniyor. Ilk boyama yaklasik yariya iniyor. **Gorsel
hazir olma suresi ise ayni buyuklukte kaliyor** (172 -> 108 ms), cunku o sure
cogunlukla devrenin acilmasi ve API cagrisi; onbellekle ilgisi yok.

Beklenti 17 ilk anlamli boyama icin **1500 ms** demisti; soguk onbellekte olculen en
yuksek FCP **128 ms**.

## 7. PersistentComponentState: once ve sonra

Adim 4'te sayfa acilisinda veri iki kez cekiliyordu: bir kez on-islemede, bir kez devre
baglaninca. Bu tur onu `PersistentComponentState` ile kapatti.

Karsilastirma icin **Adim 4 sonundaki surum** (`aea7a59`) Release olarak yayimlanip ayni
API'ye baglandi; iki surum de gercek tarayiciyla acildi ve panelin kendi gunlugundeki
istek satirlari sayildi.

| Sayfa | Eski API cagrisi | Yeni API cagrisi | Azalma | Sorgu degisince yeni istek |
|---|---|---|---|---|
| Genel bakis | 14 | 8 | 6 (%43) | - |
| Depo genel | 8 | 7 | 1 (%13) | - |
| Dosya haritasi | sayfa yoktu | 8 | - | evet |
| Zaman cizelgesi | sayfa yoktu | 8 | - | evet |

Genel bakista eski surum yedi istegin **hepsini** iki kez atiyordu; yeni surumde ikinci
turda yalniz saglik kontrolu kaliyor (o bilerek kalici degil, canli durum gostergesi).

Depo genelinde azalma daha kucuk gorunuyor ama sayilar ayni seyler degil: eski surumde
dort istegin dordu tekrarlaniyordu; yeni surumde on-islemenin dort istegi tekrarlanmiyor,
devre acilinca yalniz **arac cubugunun is listeleri** cekiliyor - onlar Adim 5'te eklendi
ve on-islemede hic yoktu.

Harita ve zaman cizelgesi sayfalari icin "eski" sayi yok, cunku o sayfalar Adim 4'te
yoktu. Bu iki sayfada gorsellestirme istegi **yalniz devre acikken** gidiyor: on-isleme
hic istemiyor, yani sayfa basina tek istek.

**Sorgu degisince yeni istek gidiyor mu?** Haritada pencere 200'den 1000'e alindi, sonra
geri donuldu. Panelin gunlugunde uc ayri `file-activity` istegi sayildi. Yani kalici
durum yalniz **ayni sorgu** icin kullaniliyor; parametre degisince yeni veri cekiliyor.
Kalici durumun anahtarinda pencere, limit, siralama ve is kimligi var, bunu saglayan o.

### Kalici durumun boyu

Devre acilirken sayfadaki kalici durum metni istemciden sunucuya gidiyor ve Blazor
Server'in varsayilan mesaj siniri **32 KB**. Bu tur ilk denemede bu sinir asilmisti:
sayfa aciliyordu ama devre sessizce kapaniyordu ve **hicbir tiklama calismiyordu**.

Simdiki degerler, sayfa HTML'inden okundu:

| Sayfa | Kalici durum | Sinira oran |
|---|---|---|
| Depo genel | 19 036 karakter | %58 |
| Dosya haritasi | 19 036 karakter | %58 |
| Zaman cizelgesi | 19 036 karakter | %58 |

Uc sayfada da ayni: gorsellestirme verisi zaten kalici duruma yazilmiyor, yazilan sey
depo ozeti ve is listesi.

## 8. Gorunum ve konsol kontrolu

Uc genislikte, gercek tarayicida:

| Sayfa | Genislik | Yatay sayfa tasmasi | Konsol hatasi | Basarisiz istek |
|---|---|---|---|---|
| Dosya haritasi | 1440 | 0 | 0 | 0 |
| Dosya haritasi | 1024 | 0 | 0 | 0 |
| Dosya haritasi | 390 | 0 | 0 | 0 |
| Zaman cizelgesi | 1440 | 0 | 0 | 0 |
| Zaman cizelgesi | 1024 | 0 | 0 | 0 |
| Zaman cizelgesi | 390 | 0 | 0 | 0 |

390 pikselde zaman cizelgesinin SVG'si 656 piksel genisliginde ve 341 piksellik kutusunun
icinde kayiyor; sayfa kaymiyor. Genis ekranlarda grafik kutusuna sigiyor (1123 icerik /
1123 kutu).

## 9. Beklentiler karsisinda

Beklenti dosyasi: `docs/olcumler/asama6-gorsellestirme-beklenti.md`. Bu turda olculen
beklentiler:

| # | Beklenti | Sonuc |
|---|---|---|
| 1 | Ayni sayfa verisi bir kez cekilmeli | tuttu (bolum 7) |
| 2 | Durum anahtari sorgu parametrelerini icermeli | tuttu (bolum 7) |
| 3 | Grafik uclari mevcut sayfa acilislarini belirgin yavaslatmamali | tuttu, asagida |
| 4 | Harita ucu Polly/ShareX 100 ms, Jellyfin 300 ms altinda | tuttu |
| 5 | Zaman cizelgesi 100 nokta icin 100 ms altinda | tuttu |
| 16 | Mobil gorunumde grafik kirpilmamali | tuttu (bolum 8) |
| 17 | Ilk anlamli boyama 1500 ms altinda | tuttu (bolum 6) |

**Beklenti 3.** Depo sayfasinin kendisi (harita/zaman sekmesi acik degilken) ayni
kosulda olculdu: ilk kosu 78.7 ms, sonraki bes kosunun ortancasi **20.3 ms**, en
yuksegi 22.2 ms. Adim 4'te ayni sayfanin ortancasi 16.9 ms'ti. Aradaki 3.4 ms'lik fark
arac cubugunun is listelerinden geliyor olabilir ama bunu ayri olcmedim; soyleyebilecegim
sey sadece sayfanin belirgin yavaslamadigi.

**Beklenti 4 ayrintisi.** Olculen en yuksek ortancalar: Polly 49.9 ms, ShareX 58.3 ms,
Jellyfin 31.9 ms. Tek tek kosulardaki en yuksek degerler de sinirin altinda kaldi
(Polly 66.4 ms, ShareX 61.9 ms, Jellyfin 49.4 ms).

**Beklenti 5 ayrintisi.** 100 nokta icin ortancalar 11.2 / 25.9 / 14.7 ms, en yuksek
tek kosu 28.7 ms.

Beklenti 6-15 bu dosyada degil; onlar test ve dogrulama ciktilariyla kontrol edildi
(`data/asama6/gorsellestirme-dogrulama.json` ve Adim 5 testleri).

## 10. Olculmeyenler

- **Cok kullanicili yuk.** Tek istemci, tek devre. Es zamanli kullanicilarda devre basina
  bellek ve API kuyrugu olculmedi.
- **Uzaktaki veritabani.** Her sey ayni makinede.
- **Gercek ag gecikmesi.** Loopback; tarayici ile sunucu arasinda gecikme yok sayilir.
- **Boyama sonrasi etkilesim gecikmesi.** "Gorsel hazir" DOM'a girme ani; tarayicinin
  100 hucreyi cizmesi ve ilk tiklamanin cevap suresi ayrica olculmedi.
- **Jellyfin `nokta 50` sicramasinin sebebi.** Gorundu, kaydedildi, sebebi olculmedi.
- **1000'den buyuk pencere.** Sinir 1000, ustu denenmedi.
