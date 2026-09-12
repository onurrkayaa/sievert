# Asama 6 API temel olcumu

**Tarih:** 2026-09-12
**Durum:** Adim 1 ve Adim 2 uclari olculdu. Panel, arka plan isleri ve statik tarama ucu
**yok**; olculmedi.

Beklentiler olcum yapilmadan once `docs/olcumler/asama6-beklenti.md` icine yazildi. Bu
dosya o beklentilerin karsisina olculen degerleri koyuyor. Beklenti dosyasi
degistirilmedi.

## 1. Ne olculdu, nasil

Iki ayri yol var ve karistirilmamali.

**HTTP tarafi.** API gercek bir surec olarak baslatildi (Kestrel, yerel baglanti,
`--contentRoot` repo koku). Sabit tohumla (20 260 912) secilmis **300 commit** icin risk
istegi atildi; **50** tanesi `CsFilesChanged = 0` grubundan geliyor. Sure, cevap boyutu,
uyarilar ve bellek buradan.

**Surec ici taraf.** Aciklama-model farki ve endeks monotonlugu veritabanindaki
**34 166 satirin hepsinde**, HTTP'ye hic girmeden olculdu. Ayni hesabi 34 bin HTTP
istegiyle tekrarlamak gunler alirdi ve olculen sey yine ayni hesap olurdu.

Ureten komut:

```
dotnet run --project tools/Sievert.Measure -- api-baseline . <commit> data/asama6/api-baseline.json
```

Ureten kod: `af00974`. Cikti: `data/asama6/api-baseline.json`.

**Makine:** Apple M2, 8 cekirdek, 16 GB, macOS 26.6.2. PostgreSQL 17.11 Docker'da,
5433 portunda. Derleme `Debug`.

Sureler **tek kosudan** geliyor ve kosudan kosuya oynuyor (medyan dort kosuda 3,8 - 4,4 ms,
p95 5,7 - 7,9 ms). Sayilari kesin esik gibi okumak dogru olmaz.

## 2. Beklentilerin karsiligi

| # | Beklenti | Olculen | Tuttu mu |
|---|---|---|---|
| 1 | Isinmis risk istegi < 100 ms | medyan 3,8 ms, p95 6,9 ms, en buyuk 40,1 ms | evet |
| 2 | Ilk yukleme ayri olculecek | acilis 668 ms, ilk risk istegi 278 ms | evet |
| 3 | Model her istekte yuklenmemeli | ilk istek 278 ms, sonrakilerin medyani 3,8 ms | evet |
| 4 | `AsNoTracking` | baglam `QueryTrackingBehavior.NoTracking` ile kuruluyor | evet |
| 5 | Commit listesi sayfali | sayfali | evet |
| 6 | Varsayilan 25, en fazla 100 | varsayilan 25, 100 ustu `400` | evet |
| 7 | Bilinmeyen repo `422` | `422`, `UNKNOWN_REPOSITORY_MODEL` | evet |
| 8 | Eksik commit `404` | `404`, `COMMIT_NOT_FOUND` | evet |
| 9 | Bozuk model acik hata | ozet tutmazsa model yuklenmiyor, uc `409` donuyor | evet |
| 10 | Katki toplami logit ile <= 1e-6 | 34 166 satirin **11'inde asildi**, en buyuk fark 1,783e-6 | **hayir** |
| 11 | `RiskIndex` monoton | 34 166 satirda **0** ihlal | evet |
| 12 | Statik bulgu skoru degistirmiyor | statik analiz calistirilmiyor, skor girdisi degil | evet |

## 3. Sureler

| Olcu | Deger |
|---|---|
| Acilis (saglik ucu cevap verene kadar) | 668 ms |
| Ilk risk istegi (model + skor referansi yukleniyor) | 278 ms |
| Isinmis istek, en kucuk | 2,2 ms |
| Isinmis istek, medyan | 3,8 ms |
| Isinmis istek, p95 | 6,9 ms |
| Isinmis istek, en buyuk | 40,1 ms |

En buyuk deger medyanin on kati. Tek bir istekte; sebebini kovalamadim, cop toplama ya
da isletim sisteminin zamanlamasi olabilir. Bunu olcmedim, tahmin olarak yaziyorum.

Ilk istegin 278 ms'si modelin diskten yuklenmesi ve 676 KB'lik skor referansinin
ayristirilmasi. Sonraki isteklerin 3,8 ms'si, modelin gercekten bir kez yuklendigini
gosteriyor: her istekte yeniden yuklense hepsi 278 ms bandinda kalirdi.

## 4. Veritabani sorgu sayisi

Istek basina calisan komut sayisi. EF Core'un komut gunlugu acik ayri bir kosudan;
gunluk acikken sureler bozuldugu icin bu sayilar sure tablosuyla ayni kosudan gelmiyor.

| Uc | Komut |
|---|---|
| `/repositories/{id}/commits/{sha}/risk` | 3 |
| `/repositories/{id}` | 5 |
| `/repositories/{id}/commits` | 3 |
| `/repositories` | 2 |

Sabit sayilar: sonuc buyudukce artmiyorlar, yani N+1 yok. Depo ucunun 5 komutu dort ayri
sayim (commit, hata getiren, bot, olcusu olan) ve deponun kendisi.

## 5. Cevap boyutu ve bellek

| Olcu | Deger |
|---|---|
| Risk cevabi, en kucuk | 5105 bayt |
| Risk cevabi, ortalama | 6028 bayt |
| Risk cevabi, en buyuk | 6503 bayt |
| API surecinin olculen en yuksek bellegi | 234 MB |

Bellek icin not: macOS ve Linux'ta `PeakWorkingSet64` desteklenmiyor. Deger 50 ms'de bir
orneklenip en buyugu alindi, yani **olculen en yuksek deger**; gercek tepe bunun
uzerinde olabilir. 676 KB'lik skor referansi ayristirilmis halde bellekte duruyor.

Beklenti dosyasinda bu iki sayi icin beklenti yazilmamisti; burada sadece kayda geciyorlar.

## 6. Uyarilar

300 istegin hepsinde uc zorunlu uyari (`UNCALIBRATED_SCORE`, `SZZ_TARGET`,
`STATIC_ANALYSIS_NOT_INCLUDED`) cikti. Eksik uyari sayisi **0**.

| Uyari | Cikan istek |
|---|---|
| `CS_LABEL_COVERAGE_LIMIT` | 50 / 300 |
| `OUTSIDE_TRAIN_RANGE` | 46 / 300 |

Kapsam uyarisi tam olarak ornekteki `CsFilesChanged = 0` sayisi kadar cikti (50), yani
uyari kacirmiyor ve fazladan da uretmiyor.

`OUTSIDE_TRAIN_RANGE` 46 istekte cikti. Bu satirlarda deger **kirpilmadi**; skor oldugu
gibi verildi ve uyari eklendi.

## 7. Beklenti 10 tutmadi: aciklama ile modelin logit'i arasindaki fark

Beklenti dosyasinda "katki toplami model logit degeriyle mutlak fark <= 1e-6" yaziyordu.
Veritabanindaki 34 166 satirin **11'inde** bu tolerans asildi.

| Olcu | Deger |
|---|---|
| En buyuk fark | 1,783e-6 |
| O satirin `|logit|` degeri | 2,33 |
| O satirda mutlak katki toplami | 27,39 |
| Toleransi asan satir | 11 / 34 166 (%0,032) |
| Asan satirlarin `|logit|` araligi | 0,70 - 9,57 |

**Sebebi ne.** Fark bir hesap hatasi degil, iki farkli duyarlikta toplamanin farki.
ML.NET skoru `float` ile topluyor, aciklama `double` ile. Onemli olan sonucun buyuklugu
degil, **terimlerin** buyuklugu: en kotu satirda logit 2,33 ama katkilarin mutlak toplami
27,39. Yani buyuk terimler birbirini goturuyor ve hata terim olcegine gore olusuyor.
27,39 civarinda `float` cozunurlugu 1,9e-6; olculen 1,783e-6 tam olarak o bandin icinde.

Ilk yazdigim testte uc repodan 200'er satir baktim ve tolerans tuttu. Butun veri kumesine
bakinca tutmadi; ornek kucuk oldugu icin gormemisim.

**Ne yapmadim.** Toleransi buyutmedim. Beklenti sayisi olcumden once yazildi ve olcume
bakip degistirmek, beklentiyi sonuca uydurmak olurdu. Kod hala 1e-6 ile kontrol ediyor.

**Bunun sonucu ne.** O 11 commit icin API degerlendirme donmuyor;
`MODEL_EXPLANATION_MISMATCH` ile `500` doniyor. Yani araç bu satirlarda **yanlis bir
aciklama uretmek yerine hic uretmiyor**. Guvenli taraf bu ama bir kusur: bu commit'ler
skorlanabilir commit'ler ve kullanicinin gozunde sebepsiz bir hata gibi gorunuyor.

**Karar bende degil.** Toleransin mutlak yerine goreli tanimlanmasi (ornegin mutlak katki
toplamina oranli) teknik olarak daha dogru gorunuyor, ama bu bir sozlesme degisikligi ve
olcumden sonra yapiliyor. Yol haritasinda ayri bir madde olarak durmasi gerekir.

**Sonradan not (Adim 3).** Kural surum 2.0 olarak yeniden yazildi
(`docs/urun/model-aciklama-sayisal-tolerans.md`) ve ayni veri uzerinde yeniden olculdu:
`docs/olcumler/asama6-aciklama-toleransi.md`. Yukaridaki v1 sonucu **oldugu gibi
duruyor**; yeni kural eskisini silmiyor, yanina yaziliyor.

## 8. Olcum kodunda duzelttigim bir hata

Ilk kosuda HTTP tarafindaki monotonluk kontrolu **56 ihlal** verdi, ayni kontrol butun
veritabaninda **0** veriyordu. Sebep olculen seydeydi: ornek uc depodan geliyor ve her
deponun `RiskIndex` degeri kendi egitim dagilimina gore olcekleniyor. Uc deponun
noktalarini tek listede siralayip karsilastirmak, risk sozlesmesinin "farkli profillerin
endeksleri dogrudan karsilastirilamaz" dedigi seyi yapmak oluyordu.

Kontrol depo basina cevrildi (`25e2908`) ve iki taraf da 0 verdi. Bu sayiyi duzeltmeden
once raporlasaydim, olmayan bir kusuru raporlamis olurdum.

## 9. Olculmeyenler

- Es zamanli istek altinda davranis. Istekler sirayla gitti; yuk testi yok.
- Uzaktaki bir veritabaniyla sureler. Veritabani ayni makinede, Docker'da.
- `Release` derlemesi. Olcum `Debug` ile yapildi.
- Gercek tepe bellek. Orneklenmis en yuksek deger var.
- Bellegin zaman icindeki egrisi; yalnizca tepe tutuldu.
- Panel, arka plan isleri, statik tarama ucu. Bunlar yazilmadi.
