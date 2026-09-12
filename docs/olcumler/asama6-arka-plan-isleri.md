# Asama 6 arka plan isleri olcumu

**Tarih:** 2026-09-13
**Beklentiler:** `docs/olcumler/asama6-arka-plan-beklenti.md`, kod yazilmadan once yazildi
ve degistirilmedi.

## 1. Kosulun sabitlenmesi

Olcumden once sabitlenen kosullar:

- Worker es zamanliligi **1**.
- Yerel PostgreSQL 17.11, Docker'da, 5433 portunda; API ile ayni makinede.
- Derleme **Debug**. Release ile olculmedi; asagida "olculmeyenler"de yaziyor.
- Tek istemci; es zamanli istek yalnizca tekillik olcumunde var ve orada bilerek.
- Her asamada kuyruk bos basliyor.
- Bellek 50 ms'de bir orneklendi. Bu **gercek tepe degil**, olculen en yuksek deger.
- Makine: Apple M2, 8 cekirdek, 16 GB, macOS 26.6.2.

Ureten komut:

```
dotnet run --project tools/Sievert.Measure -- background-jobs . <commit> data/asama6/background-jobs.json
```

Ureten kod: `7f32a27`. Cikti: `data/asama6/background-jobs.json`.

Sayilar tek kosudan ve makineye bagli; dosyanin ozeti yok cunku iki kosu ayni dosyayi
vermiyor.

## 2. static-scan

Uc deponun calisma agaci tarandi.

| Depo | Dosya | Bulgu | Susturma | Muafiyet | Elenen dosya | Atlanan klasor |
|---|---|---|---|---|---|---|
| polly | 797 | 203 | 0 | 972 | 0 | 3 |
| jellyfin | 2184 | 687 | 0 | 534 | 0 | 3 |
| sharex | 1138 | 248 | 0 | 308 | 0 | 3 |

| Depo | Kuyruk | Calisma | Toplam | Ilerleme yazimi |
|---|---|---|---|---|
| polly | 94 ms | 2150 ms | 2290 ms | 5 |
| jellyfin | 4 ms | 3738 ms | 3745 ms | 8 |
| sharex | 2 ms | 1940 ms | 1944 ms | 4 |

Bulgular veritabanina 500'luk obekler halinde yazildi.

### CLI esdegerligi

Ayni calisma agaci ayni yapilandirmayla **dogrudan** tarandi
(`ScanService.Run`, CLI'nin `check` komutunun gectigi servis) ve iki sonuc
karsilastirildi: kural kodu, seviye, goreli yol ve satir dortlusu.

| Depo | Dogrudan bulgu | Is bulgusu | Fark |
|---|---|---|---|
| polly | 203 | 203 | **0** |
| jellyfin | 687 | 687 | **0** |
| sharex | 248 | 248 | **0** |

Susturma, muafiyet, elenen dosya ve atlanan klasor sayilari da ayni cikti. Bu beklenen
sonuc: iki taraf **ayni servisi** cagiriyor, iki ayri kopya yok. Olcum bunu dogrulamak
icin var, ummak icin degil.

Dogrudan taramanin suresi (polly 1877 ms, jellyfin 4223 ms, sharex 1008 ms) is icindeki
calisma suresiyle ayni buyukluk bandinda. Jellyfin'de is daha hizli, ShareX'te daha yavas
gorunuyor; tek kosudan cikan bu fark isletim sisteminin dosya onbellegiyle aciklanabilir
ama **olculmedi**, o yuzden sebep iddia etmiyorum.

## 3. risk-score-all

| Depo | Commit | Yazilan satir | Kuyruk | Calisma | commit/sn |
|---|---|---|---|---|---|
| polly | 2759 | 2759 | 3 ms | 1172 ms | 2353 |
| jellyfin | 22 917 | 22 917 | 2 ms | 8830 ms | 2595 |
| sharex | 8490 | 8490 | 2 ms | 2384 ms | 3561 |

| Depo | Ilerleme yazimi | Model yukleme | Aciklama uyusmazligi |
|---|---|---|---|
| polly | 5 | **1** | 0 |
| jellyfin | 18 | **1** | 0 |
| sharex | 7 | **1** | 0 |

Model yukleme sayisi tahmin degil: sayac `ModelRegistry`'deki `Lazy`'nin icinde duruyor,
yani model diskten gercekten bir kez okundu.

Ilerleme yazimi 22 917 commit'lik iste **18**. Oge basina yazilsaydi 22 917 olurdu.

Aciklama uyusmazligi uc repoda da 0: yeni tolerans kurali (surum 2.0) butun satirlarda
tuttu, `docs/olcumler/asama6-aciklama-toleransi.md` ile ayni sonuc.

### Risk ucu esdegerligi

Her repodan sabit tohumla (20 260 913) secilen **100 commit**, hem arka planda kaydedilen
satirdan hem de `/risk` ucundan okundu.

| Depo | Karsilastirilan | Farkli | En buyuk skor farki |
|---|---|---|---|
| polly | 100 | **0** | 0 |
| jellyfin | 100 | **0** | 0 |
| sharex | 100 | **0** | 0 |

Karsilastirilan alanlar: ham model skoru, goreli endeks, iki karar, egitim esigi, model
profili ve uyari kodlari kumesi. Skor farki **tam olarak sifir**, yani bit duzeyinde ayni.
Beklenen buydu: iki yol da `CommitRiskCalculator` uzerinden geciyor.

## 4. Iptal

| Olcu | Deger |
|---|---|
| Kuyruktaki ise iptal | `200`, `canceled`, 4 ms |
| Kuyruktaki is hic basladi mi | **hayir** (`startedAtUtc` null) |
| Kosan ise iptal | `202`, terminal duruma 69 ms'de gecti |
| Iptal isteginden once islenmis oge | 2500 |
| Terminal durumdaki islenmis oge | 2500 |
| Iptal sonrasi islenen ek oge | **0** |
| Kalan kismi sonuc | 2500 satir |
| `isResultComplete` | **false** |
| Terminal ise iptal | durum degismedi (`canceled`) |
| Iki kez iptal | ayni sonuc |

Kismi sonuc duruyor ama tam sonuc gibi sunulmuyor: `/risks` ucu `partial: true`,
`isResultComplete: false` ve acik bir uyari metni donuyor.

## 5. Tekrar anahtari ve yaris

| Deney | Sonuc |
|---|---|
| Ayni anahtarla ardisik 10 istek | **1** is |
| Ayni anahtarla es zamanli 10 istek | **1** is |
| Farkli anahtarlarla es zamanli 10 istek | 1 kabul, **9 catisma** |
| Bu asamada acilan toplam is satiri | 2 |

Es zamanli yarisin dayanagi uygulama kontrolu degil, `ActiveDeduplicationKey` uzerindeki
benzersiz indeks. Ayni yaris test ortaminda da kosuyor ve orada da tek is cikiyor.

## 6. Yeniden baslatma

Kosan bir is varken API sureci **oldurduldu** (duzgun kapanma degil) ve yeniden acildi.

| Olcu | Sonuc |
|---|---|
| Yarida kalan isin durumu | `failed` |
| Hata kodu | `PROCESS_INTERRUPTED` |
| `isResultComplete` | false |
| Kurtarmanin yarida kalmis saydigi is | 1 |
| Yeniden kuyruga alinan is | 1 |
| API durdurulduktan sonra birinci kurtarma | 1 degisiklik |
| Ikinci kurtarma | **0 degisiklik** |

Yeniden kuyruga alinan is, API acilir acilmaz worker tarafindan alinip `running` oldu;
bu yuzden API durdurulduktan sonraki birinci kurtarma onu da yarida kalmis sayip 1
degisiklik yapti. Ikinci kurtarma hicbir satir degistirmedi.

## 7. Cevap boyutu, bellek ve sorgu

| Olcu | Deger |
|---|---|
| API surecinin olculen en yuksek bellegi | 1267 MB |

Bu sayi butun olcum boyunca goruldu, en yuksegi Jellyfin'in 22 917 satirlik risk isi
sirasinda. Sayi yuksek ve beklenti dosyasinda bunun icin bir beklenti yazilmamisti; burada
yalnizca kayda geciyor. Olculmeyen sey su: bellegin ne kadarinin EF'in obek yazimindan,
ne kadarinin ML.NET'ten geldigi **ayristirilmadi**.

Istek basina veritabani komutu sayisi bu turda **olculmedi**; Adim 2'nin olcumunde
(`asama6-api-temel.md`) salt-okunur uclar icin olculmustu ve o uclar degismedi.

## 8. Beklentilerin karsiligi

| # | Beklenti | Sonuc | Durum |
|---|---|---|---|
| 1 | Static scan CLI ile ayni | uc repoda da 0 fark | tuttu |
| 2 | Risk sonucu risk ucuyle ayni | 300 commit, 0 fark, skor farki 0 | tuttu |
| 3 | Kismi sonuc tam gibi sunulmamali | `partial: true`, `isResultComplete: false` | tuttu |
| 4 | Ayni repo + tur icin tek aktif is | 9 catisma / 10 istek | tuttu |
| 5 | Ayni anahtar yeni is acmamali | ardisik ve es zamanli 10'ar istekte 1 is | tuttu |
| 6 | Farkli anahtarlarla 10 istek -> 1 kabul | 1 kabul, 9 catisma | tuttu |
| 7 | Tekillik veritabani seviyesinde | benzersiz indeks; yaris testleri geciyor | tuttu |
| 8 | Kuyruktaki is hic baslamamali | `startedAtUtc` null | tuttu |
| 9 | Iptal en gec bir sonraki obekte gorulmeli | iptal sonrasi 0 ek oge | tuttu |
| 10 | Terminal ise iptal degistirmemeli | durum ayni, iki kez idempotent | tuttu |
| 11 | Terminal is yeniden calismamali | durum makinesi testleriyle | tuttu |
| 12 | Progress monoton | uctan uca testte dogrulandi | tuttu |
| 13 | Oge basina UPDATE olmamali | 22 917 commit -> 18 yazim | tuttu |
| 14 | Queued isler yeniden kuyruga | 1 yeniden kuyruga alindi | tuttu |
| 15 | Running is devam etmemeli | `PROCESS_INTERRUPTED` | tuttu |
| 16 | Kurtarma idempotent | ikinci kosu 0 degisiklik | tuttu |
| 17 | `202` + `Location` | ikisi de doniyor | tuttu |
| 18 | Varsayilan es zamanlilik 1 | saglik ucu 1 diyor | tuttu |
| 19 | Polly, Jellyfin'den kisa surmeli | 1172 ms / 8830 ms | tuttu |
| 20 | Model bir kez yuklenmeli | uc repoda da `modelLoads` 1 | tuttu |

Yirmi beklentinin yirmisi de tuttu. Bu, kodun hatasiz oldugunu **gostermiyor**: asagidaki
iki kusur tam olarak bu olcumler sirasinda ortaya cikti ve duzeltildi.

## 9. Olcum sirasinda bulunan iki kusur

### 9.1 Iptal edilen is yazdigi satirlari yok sayiyordu

Ilk kosuda iptal olcumu "iptal sonrasi islenen ek oge **-2250**" ve "kismi sonuc **0**
satir" verdi. Negatif sayi kusurun kendisiydi.

Sebep: is jeton uzerinden iptal edilince `OperationCanceledException` isleyiciden cikip
worker'a gidiyordu ve worker sayilari bilmedigi icin `0` yaziyordu. Oysa veritabaninda
2250 satir duruyordu. Yani arac, gercekten var olan bir kismi sonucu "hic sonuc yok" diye
raporluyordu - kismi sonucu yanlis buyuklukte gostermek, hic gostermemekten kotu.

Duzeltme: jeton dongunun icindeki her `await`'i kesebiliyor (obek sorgusu, kayit, ilerleme
yazimi); hepsi tek yerde yakalaniyor ve iptal sonucu **veritabanindan sayilan** gercek
satir sayisiyla donuyor. Regresyon testi yazildi. Duzeltmeden sonra ayni olcum "kismi
sonuc 2500 satir, iptal sonrasi 0 ek oge" verdi.

### 9.2 EF sorgusu cevrilemiyordu

Risk isi ilk yazildiginda hicbir repoda calismadi: EF, kendi kurdugu kaydin icine bakip
siralama yapamiyordu. Siralamayi projeksiyondan **once** yazinca duzeldi. Bu kusur birim
testlerinde gorunmedi, gercek veritabanina karsi kosunca cikti.

## 10. Olcum kodunda duzelttigim iki hata

Bunlar urunde degil, olcumun kendisinde:

1. **Yeniden baslatma asamasi bos kimlikle devam ediyordu.** Onceki asamalarin actigi
   isler bitmemisti, tekillik kisiti yeni isi reddediyordu ve komut `Guid.Empty` ile devam
   ediyordu. Artik once bekleniyor ve is acilamazsa komut duruyor.
2. **Kurtarma idempotentligi calisan sisteme karsi olculuyordu.** API hala kosarken ikinci
   kurtarmayi kosturunca, o anda calisan isi yarida kalmis saydi ve "1 degisiklik" cikti.
   Olculen sey idempotentlik degil kendi mudahalemdi. Artik karsilastirma API
   durdurulduktan sonra yapiliyor.

Ikisi de raporlanmadan once duzeltildi; duzeltilmeseydi ikisi de olmayan bir kusuru
raporlamis olurdum.

## 11. Olculmeyenler

- **Release derlemesi.** Butun sayilar Debug ile.
- **Es zamanlilik 2, 3, 4.** Yalnizca 1 ile olculdu; ust sinir 4 olarak yazildi ama
  denenmedi.
- **Uzaktaki veritabani.** Veritabani ayni makinede, Docker'da.
- **Istek basina veritabani komut sayisi** bu turda olculmedi.
- **Bellegin nereden geldigi.** Tepe 1267 MB; EF ile ML.NET paylari ayristirilmadi.
- **Kuyruk doldugunda ne oluyor.** Kapasite 100 ve olcumde kuyruk hic dolmadi.
- **Cok sayida es zamanli istemci.** Tek istemci.
- **Git clone, tarih madenciligi, metrik hesabi ve SZZ** bu turda is olarak yazilmadi.
