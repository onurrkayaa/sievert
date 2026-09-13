# Bellegin nereden geldigi

**Tarih:** 2026-09-13
**Beklentiler:** `docs/olcumler/asama6-panel-oncesi-beklenti.md`, kod yazilmadan once yazildi.
**Ureten kod:** `1600424`. **Cikti:** `data/asama6/bellek-ayristirma.json`.

Adim 3'un olcumunde API surecinin gorulen en yuksek bellegi **1267 MB** cikmisti
(`docs/olcumler/asama6-arka-plan-isleri.md` bolum 7). O sayi silinmedi ve bu dosya onun
yerine gecmiyor. Ama o kosuda alti is ayni surecte pesi sira kosmustu; sayi "bir isin
bellegi" degil, "alti isten sonra surecin ulastigi yer" idi. Burada her is kendi taze
surecinde kosuyor.

## 1. Kosulun sabitlenmesi

- Her kosu icin **yeni bir API sureci**. Kosu bitince surec olduruluyor.
- Worker es zamanliligi **1** (es zamanlilik bolumu haric).
- Kuyruk her kosuda bos basliyor, model onbellegi soguk.
- Her kosu icin yeni bir `AnalysisJob`; onceki islerin sonuclari okunmuyor.
- Derleme **Debug**. Release ile olculmedi.
- Calisma kumesi **50 ms**'de bir orneklendi. Bu **gercek tepe degil**, olculen en
  yuksek deger; iki ornek arasinda kalan bir sicramayi kimse gormez.
- Yonetilen yigin ve nesil bazinda toplama sayilari saglik ucundan, **500 ms**'de bir.
- **`GC.Collect` cagrilmadi, GC ayari degistirilmedi.** Zorlanmis bir toplama sayiyi
  guzellestirirdi ve olculen sey normal calisma olmaktan cikardi.
- Makine: Apple M2, 8 cekirdek, 16 GB, macOS 26.6.2. PostgreSQL 17.11, Docker, ayni makine.

Olcumun kendisi de bir maliyet: is kosarken 50 ms'de bir is ucu, 500 ms'de bir saglik ucu
cagriliyor. Bu istekler de bellek harciyor ve sayinin icinde.

## 2. Model yuklemesi tek basina

Ayri bir surec, tek bir commit'in skorlanmasi. Farkin tamami modelin diskten okunmasi.

| Depo | Soguk | Sicak | Fark | Yonetilen yigin farki | Sure |
|---|---|---|---|---|---|
| polly | 166,2 MB | 182,2 MB | **16,0 MB** | 9,2 MB | 160 ms |
| jellyfin | 166,2 MB | 182,4 MB | **16,1 MB** | 9,2 MB | 167 ms |
| sharex | 166,2 MB | 182,2 MB | **16,0 MB** | 9,2 MB | 149 ms |

Uc profilde de ayni: **16 MB**. Yani en buyuk kosuda goruleni (504 MB) aciklayan sey model
degil; model, tepe degerin yaklasik **%3**'u.

Bu olcum ilk denemede yapilamadi. Ilk kosuda "model yukleme oncesi" ve "sonrasi" ayni
sayiyi verdi, cunku ikisi de ayni saglik orneginden geliyordu: faz gecisi 500 ms'lik
ornekleme araligindan kisa. Ayni sayiyi iki ayri ada yazip fark "0" demek, olculmemis bir
seyi olculmus gibi gostermek olurdu.

## 3. Her is, taze surecte

| Depo | Is | Oge | Sonuc | Sure | Taban | Tepe-orneklenen | Artis | Terminal | +5 sn |
|---|---|---|---|---|---|---|---|---|---|
| polly | static-scan | 797 | 203 | 2607 ms | 164,9 MB | 288,0 MB | 123,1 MB | 280,5 MB | 254,7 MB |
| jellyfin | static-scan | 2184 | 687 | 6153 ms | 160,7 MB | 458,6 MB | 297,9 MB | 450,7 MB | 458,6 MB |
| sharex | static-scan | 1138 | 248 | 3163 ms | 165,3 MB | 361,3 MB | 196,0 MB | 358,5 MB | 290,4 MB |
| polly | risk-score-all | 2759 | 2759 | 1492 ms | 160,5 MB | 409,5 MB | 249,1 MB | 403,6 MB | 409,8 MB |
| jellyfin | risk-score-all | 22 917 | 22 917 | 10 394 ms | 160,7 MB | **486,1 MB** | 325,4 MB | 302,2 MB | 302,6 MB |
| sharex | risk-score-all | 8490 | 8490 | 4736 ms | 160,4 MB | **504,5 MB** | 344,1 MB | 498,4 MB | 504,5 MB |

Taban her kosuda 160-165 MB: bu, is baslamadan once API surecinin kendi maliyeti (.NET
calisma zamani, ASP.NET Core, EF Core ve acilan veritabani baglantisi).

**Olculen en yuksek deger 504,5 MB.** Adim 3'teki 1267 MB taze bir surecte tekrarlanmadi;
bu beklenen sonuctu (beklenti 7) ama ne kadar dusecegi icin bir beklentim yoktu.

### Toplama sayilari ve yonetilen yigin

| Depo | Is | gen0 | gen1 | gen2 | Terminal yonetilen yigin |
|---|---|---|---|---|---|
| polly | static-scan | 1 | 0 | 0 | 124,4 MB |
| jellyfin | static-scan | 2 | 1 | 1 | 333,2 MB |
| sharex | static-scan | 1 | 0 | 0 | 210,8 MB |
| polly | risk-score-all | 1 | 0 | 0 | 232,6 MB |
| jellyfin | risk-score-all | **21** | 11 | 4 | **52,8 MB** |
| sharex | risk-score-all | 4 | 2 | 1 | 174,9 MB |

Jellyfin'in risk isi tek basina ayirt edici: 21 gen0 toplamasi yapiyor ve is bittiginde
yonetilen yigin **52,8 MB**'a iniyor - yani skorlama boyunca ayrilan nesneler gercekten
birakiliyor. Kisa isler hic gen2 toplamasi gormeden bitiyor, o yuzden yiginlarinda bir
seyler duruyor gibi gorunuyor; bu "sizinti" degil, toplama hic gerekmedigi icin
yapilmamis olmasi.

### Bin ogede bir calisma kumesi

Jellyfin'in risk isi (22 917 commit):

| Oge | 1000 | 2000 | 3000 | 4000 | 5000 | 8000 | 12 000 | 16 000 | 19 000 | 20 000 | 22 000 |
|---|---|---|---|---|---|---|---|---|---|---|---|
| RSS | 241 | 315 | 407 | 483 | 483 | 451 | 462 | 465 | 419 | 346 | 318 |

Ilk 4000 ogede yukseliyor, sonra **460 MB civarinda duruyor** ve sonlara dogru dusuyor.
Commit sayisiyla dogrusal buyuyen bir egri yok. ShareX'te de ayni kalip: 4000'de 485 MB,
8490'da 491 MB.

## 4. Sizinti kapisi

Beklenti dosyasindaki 8. madde: takipci oge sayisi commit sayisiyla dogrusal buyumemeli.

| Depo | Is | Takipcide gorulen en yuksek | Temizlikten sonra en yuksek |
|---|---|---|---|
| polly | static-scan | 203 | **0** |
| jellyfin | static-scan | **500** | **0** |
| sharex | static-scan | 248 | **0** |
| polly | risk-score-all | **250** | **0** |
| jellyfin | risk-score-all | **250** | **0** |
| sharex | risk-score-all | **250** | **0** |

Risk isinde takipci **her zaman 250**, yani obek boyutu. 2759 commit'lik iste de 22 917
commit'lik iste de ayni. Statik taramada 687 bulgusu olan jellyfin'de 500'de kaliyor, yani
obek boyutu; iki obek arasinda birikmiyor. Temizlikten sonra hepsi **sifir**.

Sonuc gerecine bakildiginda:

- Takipci obek sonlarinda tabana donuyor: **evet**.
- Sonuc nesneleri butun is boyunca listede tutuluyor mu: **hayir**, obek listesi her
  turda yeniden kuruluyor.
- `IQueryable` beklenmeden tum repo yukleniyor mu: **hayir**, `Skip/Take` ile sayfali.
- Risk snapshot obekleri yazildiktan sonra tutuluyor mu: **hayir**.
- Statik bulgular butun tarama boyunca tutuluyor mu: **evet, ama taramanin dogasi geregi**.
  `ScanService` butun bulgulari dondurup sonra yaziyor; 687 bulgu icin bu bir sorun degil
  ama cok buyuk bir depoda olurdu. Bu bir kusur degil bir **sinir**; degistirmedim, cunku
  degistirmek taramanin CLI ile ayni servisten gectigi kurali ile ugrasmak demek ve
  olculen bir sorun yok.

**Gercek bir tutma bulunamadi**, o yuzden urun kodunda bellek icin bir degisiklik
yapilmadi. Batch boyutu sonuc goruldukten sonra degistirilmedi.

## 5. Kaynagi ayristirilabilen ve ayristirilamayan

Ayristirilabildi:

- **Taban** (~162 MB): is baslamadan once surecin kendisi.
- **Model** (~16 MB): uc profilde de ayni, ayri olcum.
- **Takipci** (en fazla 250-500 kayit): obek boyutunda, is boyutuyla buyumuyor.

Ayristirilamadi:

- Tepe degerin geri kalani. Statik taramada Roslyn'in sozdizimi agaclari ile EF'in
  materyalizasyonunu ayirmadim; risk isinde EF materyalizasyonu ile ML.NET
  degerlendirmesini ayirmadim. Bunun icin profil alici (dotnet-counters, dotnet-gcdump)
  gerekirdi ve bu turda kullanilmadi. **Sebep olculmedi, o yuzden iddia etmiyorum.**

## 6. Es zamanlilik 1 ve 2

Ayni is cifti (polly + sharex `risk-score-all`), taze surec, bos kuyruk.

| Olcu | Es zamanlilik 1 | Es zamanlilik 2 |
|---|---|---|
| Toplam duvar saati | 5941 ms | **4689 ms** |
| polly isinin suresi | 1501 ms | 1467 ms |
| sharex isinin suresi | 4269 ms | 4522 ms |
| Tepe-orneklenen RSS | 524,8 MB | 524,8 MB |
| Terminal RSS | 521,5 MB | 523,6 MB |
| Veritabani baglantisi (tepe) | 5 | 6 |
| Hata / zaman asimi | 0 | 0 |
| Sonuc satiri | 2759 + 8490 | 2759 + 8490 |

Iki worker toplam sureyi **%21** kisaltti (5941 -> 4689 ms). Bunun bedeli sharex isinin
kendi suresinin %5,9 uzamasi (4269 -> 4522 ms); iki is ayni veritabanini ve ayni cekirdek
kumesini paylasiyor.

Tepe bellek iki kosuda da **ayni** cikti (524,8 MB). Bunu tek kosudan genellemiyorum;
beklenen sey iki isin ayni anda kosarken daha cok bellek tutmasiydi ve bu kosuda
gorulmedi.

Baglanti sayisi 5'ten 6'ya cikti. Bu sayinin icinde olcum aracinin kendi baglantisi da
var; mutlak degeri degil, farki okumak dogru.

**Varsayilan degismiyor: es zamanlilik 1.** 2 bu kosuda hata vermedi ama tek bir kosu bir
varsayilani degistirmek icin yeterli degil ve bu is birlikte karar verilmek uzere duruyor.

Es zamanlilik **4 gercek yukle olculmedi**. Yalnizca kontrollu bir entegrasyon
isleyicisiyle, ayni anda kosan isleyici sayisinin tam olarak ayarin kendisi oldugu
sinandi (`WorkerConcurrencyTests`, 1 / 2 / 4). "4'e kadar olculdu" demiyorum.

## 7. Olculmeyenler

- **Release derlemesi.** Butun sayilar Debug.
- **Tekrar.** Her kosu tek sefer; medyan ya da dagilim yok.
- **Gercek tepe.** 50 ms ornekleme, ornekler arasi sicrama gorunmez.
- **Tepe bellegin icindeki paylar** (Roslyn / EF / ML.NET) - yukarida yaziyor.
- **Es zamanlilik 3 ve 4 gercek repolarla.**
- **Uzaktaki veritabani.** Ayni makinede, Docker'da.
