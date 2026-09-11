# Ilk gercek repo taramasi

**Tarih:** 2026-09-11
**Repo:** [App-vNext/Polly](https://github.com/App-vNext/Polly)
**Commit:** `2247db2407713fa221d57011814e4be446361b1f` (2026-09-08)
**Komut:** `sievert scan <polly> --json` (Release derlemesi)

Polly'yi sectim cunku orta buyuklukte (797 .cs dosyasi, 15 MB), modern C# ile
yazilmis ve async agirlikli bir kutuphane. Ilk kuralim SV001 async void
olacagi icin async kullanan gercek bir repo uzerinde olcum almak istedim.
Repoyu Sievert'in disinda gecici bir klasore shallow olarak klonladim.

Ayni repoyu iki kez taradim. Ilk tarama enum/delegate desteginden ve kor nokta
olcumunden onceydi; ikinci tarama bunlar eklendikten sonra. Eski sayilari
silmiyorum, ikisi yan yana dursun ki neyin degistigi gorunsun.

- **Once:** Sievert `6211c80` (fix: cikti tutarliligi ve json sozlesmesi)
- **Sonra:** Sievert `5b1f5ce` (feat: enum, kor nokta olcumu, dagilim istatistikleri)

## Sonuclar

| Olcum | Once (`6211c80`) | Sonra (`5b1f5ce`) | Not |
|---|---|---|---|
| Dosya | 797 | 797 | degismedi |
| Tip | 880 | **897** | +17: 15 enum, 2 delegate |
| Metot | 4655 | 4655 | enum ve delegate metot icermiyor |
| Async metot | 991 (%21,3) | 991 (%21,3) | degismedi |
| Ortalama metot uzunlugu | 15,8 satir | 15,8 satir | degismedi |
| Medyan metot uzunlugu | 12 satir (elle hesapladim) | 12 satir (arac veriyor) | artik ozette |
| p90 / p95 | 32 / 39 (elle) | 32 / 39 (arac) | artik ozette |
| En uzun metot | 219 satir | 219 satir | degismedi |
| 40 satiri asan metot | 210 (%4,5) | 210 (%4,5) | degismedi |
| Hic tip bulunamayan dosya | 24 | **12** | 12'si aslinda enum/delegate dosyasiymis |
| Ayristirilamayan dosya | 1 | 1 | `cake.cs` |
| Kor nokta | olculmuyordu | **61 dosyada #if, 275 satir, %0,26** | yeni |
| Uretim / test dosya | ayrilmiyordu | **479 / 318** | yeni |
| Uretim / test metot | ayrilmiyordu | **1678 / 2977** | yeni |
| Tarama suresi | 0,83 - 0,89 sn | **1,32 - 1,40 sn** | ~%60 yavasladi |

### Tip turu dagilimi (ikinci tarama)

| Tur | Sayi |
|---|---|
| class | 787 |
| struct | 46 |
| interface | 29 |
| record | 18 |
| enum | 15 |
| delegate | 2 |
| **toplam** | **897** |

## Kor nokta

Kapali `#if` dallarinda kalan kodu duzeltmeye calismiyorum; sembol tahmin edip
ikinci kez ayristirmak, yanlis sonuclari dogru gibi gostermek olurdu. Amac
kaybi gorunur kilmak.

| Olcum | Deger |
|---|---|
| Kosullu derleme iceren dosya | 61 (797'nin %7,7'si) |
| Gercekten satir kaybi olan dosya | 50 |
| Gorulmeyen satir | 275 |
| Taranan toplam satir | 104.388 |
| Gorulmeyen oran | %0,26 |

En cok kaybi olan dosyalar:

| Satir | Dosya |
|---|---|
| 38 | src/Polly.Core/Utils/CancellationTokenSourcePool.Pooled.cs |
| 22 | src/Polly/Utilities/TimedLock.cs |
| 21 | test/Polly.Core.Tests/CircuitBreaker/BrokenCircuitExceptionTests.cs |
| 21 | test/Polly.TestUtils/BinarySerializationUtil.cs |
| 20 | src/Polly.Core/Utils/Pipeline/DelegatingComponent.cs |

61 dosyada `#if` var ama sadece 50'sinde satir kaybi oluyor. Aradaki 11 dosyada
kosul `#if !NETFRAMEWORK` gibi olumsuz yazilmis; hicbir sembol tanimli
olmadigi icin o dal acik kaliyor ve bir sey kaybetmiyoruz. Yine de dosyanin
baska bir hedefte farkli derlendigini bilmek istedigim icin bu dosyalari da
isaretliyorum.

## Uretim ve test kodu

Bu ayrim bir tahmin: yolunda `test`/`tests` klasoru gecen ya da adi
`Test.cs` / `Tests.cs` ile biten dosyalari test sayiyorum. Kesin degil.

| Olcum | Uretim | Test |
|---|---|---|
| Dosya | 479 | 318 |
| Metot | 1678 | 2977 |
| Ortalama uzunluk | 11,1 | 18,5 |
| Medyan uzunluk | 7,0 | 15,0 |
| p95 | 34 | 41 |
| En uzun metot | 123 | 219 |
| Async orani | %8,6 | %28,4 |
| 40 satiri asan | 57 | 153 |

### En uzun 10 metot

| Satir | Metot | Yer |
|---|---|---|
| 219 | `CacheTResultSpecs.Should_throw_when_cache_provider_is_null` | test/Polly.Specs/Caching/CacheTResultSpecs.cs:54 |
| 175 | `CacheTResultSpecs.Should_throw_when_on_cache_get_is_null` | test/Polly.Specs/Caching/CacheTResultSpecs.cs:570 |
| 175 | `CacheTResultSpecs.Should_throw_when_on_cache_miss_is_null` | test/Polly.Specs/Caching/CacheTResultSpecs.cs:746 |
| 175 | `CacheTResultSpecs.Should_throw_when_on_cache_put_is_null` | test/Polly.Specs/Caching/CacheTResultSpecs.cs:922 |
| 153 | `CacheTResultSpecs.Should_throw_when_cache_key_strategy_is_null` | test/Polly.Specs/Caching/CacheTResultSpecs.cs:416 |
| 141 | `CacheTResultSpecs.Should_throw_when_ttl_strategy_is_null` | test/Polly.Specs/Caching/CacheTResultSpecs.cs:274 |
| 126 | `BulkheadSpecsBase.Should_control_executions_per_specification` | test/Polly.Specs/Bulkhead/BulkheadSpecsBase.cs:83 |
| 123 | `Migration.Retry_V8` | src/Snippets/Docs/Migration.Retry.cs:72 |
| 121 | `CacheTResultAsyncSpecs.Should_not_throw_when_arguments_valid` | test/Polly.Specs/Caching/CacheTResultAsyncSpecs.cs:52 |
| 118 | `AdvancedCircuitBreakerAsyncSpecs.Should_allow_single_execution_..._integration_test` | test/Polly.Specs/CircuitBreaker/AdvancedCircuitBreakerAsyncSpecs.cs:1443 |

### Ayristirilamayan dosya

Tek dosya: `cake.cs`, 6 hata. Hepsi ayni sebepten: dosya `#:package ...`
satirlariyla basliyor, yani .NET 10'un dosya tabanli program sozdizimi.
Roslyn'e duz bir .cs dosyasi gibi verdigim icin bu satirlari taniyamiyor.
Beklenen bir durum, kotu bir dosya degil.

## Ilk taramada beklemediklerim

**Enum ve delegate hic sayilmiyordu.** "Hic tip bulunamadi" cikan 24 dosyanin
yarisi aslinda bos degildi: `CircuitState.cs`, `PollyVersion.cs`,
`TimeoutStrategy.cs` gibi dosyalar enum, `ExceptionPredicate.cs` ve
`ResultPredicate.cs` delegate tanimliyordu. Ikinci taramada bunlar sayiliyor,
sayi 24'ten 12'ye dustu.

**`#if` bloklari gorunmuyordu.** `BinarySerializationUtil.cs` dosyasinin tamami
`#if NETFRAMEWORK` icinde ve sessizce kayboluyordu. Artik kayip olculuyor.

**219 satirlik test metodu gercekten oyle.** Once olcum hatasi sandim, dosyaya
bakip dogruladim: `Should_throw_when_cache_provider_is_null` 54. satirda
basliyor, 272. satirda bitiyor, arasi tek bir metot govdesi.

**En uzun 10 metodun 9'u test kodu.** Tek istisna `Migration.Retry_V8`, o da
dokumantasyon ornegi.

**Uretilen kod dosyasi hic yok.** Polly'de `.g.cs` ya da `.Designer.cs`
bulamadim, yani "uretilen kod ayirt edilmiyor" maddesi bu olcumu etkilemedi.

## Ikinci taramada beklemediklerim

**Kor nokta tahminim iki katiymis.** Ilk taramadan sonra `#if` kaybini elle
yazdigim bir betikle yaklasik 537 satir diye tahmin etmistim. Roslyn'in
verdigi gercek sayi 275. Fark benim betigin hatasindan: her `#if` dalini
kapali sayiyordum, oysa `#if !NETFRAMEWORK` gibi olumsuz kosullar hicbir
sembol tanimli olmadiginda dogru oluyor ve o dal acik kaliyor. Elle yaptigim
tahmine guvenmemem gerektigini gosteren iyi bir ornek.

**Kor nokta orani sandigimdan cok kucuk.** 104.388 satirin 275'i, yani %0,26.
Ilk taramadan sonra bunu ciddi bir problem gibi yazmistim; olcunce oyle
olmadigi cikti. Yine de yerinde duruyor, cunku oran repodan repoya degisir ve
kaybin sessiz olmasi kucuk olmasindan daha onemli.

**Tarama %60 yavasladi.** 0,83 saniyeden 1,36 saniyeye cikti. Ayni makine,
ayni Release derlemesi, ayni girdi. Sebebi kor nokta olcumu icin her dosyanin
butun trivia'sini gezmem. 797 dosya icin hala kabul edilebilir ama bedava
degil; daha buyuk repolarda tekrar bakmak lazim.

**Ozetteki "uretim" sayisi src/ ile ayni degil.** Ilk taramada klasore gore
bolmustum: src/ 416 dosya, 1526 metot. Aracin "uretim" tanimi 479 dosya, 1678
metot cikiyor cunku bench/ ve samples/ klasorlerini de uretim sayiyor. Ikisi
de savunulabilir ama ayni sey degil, karsilastirirken dikkat etmek gerekiyor.

## Yorum

Ortalama 15,8 satir, medyan 12 satir. Aradaki fark kucuk gorunuyor ama yonu
onemli: ortalama medyanin uzerinde, yani dagilim saga carpik. Metotlarin
yarisi 12 satirin altinda, buna karsilik 219 satirlik bir kuyruk var ve
ortalamayi yukari cekiyor. Bu yuzden "ortalama metot uzunlugu" bir repoyu
anlatmak icin tek basina zayif bir sayi; birkac dev metot onu istedigi gibi
oynatabiliyor. Medyani ozete eklememin sebebi bu.

Uretim ve test ayrildiginda fark daha da belirginlesiyor. Uretim tarafinda
ortalama 11,1 medyan 7,0, yani metotlarin yarisi 7 satirin altinda ama
ortalama bunun bir buçuk kati. Test tarafinda ortalama 18,5 medyan 15. Test
kodu hem daha uzun hem daha carpik, ve 40 satiri asan 210 metodun 153'u test.
Risk skoru hesaplarken test kodunu uretim kodundan ayirmam gerekecek, yoksa
skor buyuk olcude test dosyalarinin sekline gore olusur.

Async orani %21,3 ilk bakista yuksek geldi, ama uretim tarafinda %8,6, test
tarafinda %28,4. Yani Polly'nin kendisi sanildigi kadar async degil; async
agirligi testlerden geliyor. SV001 gibi async kurallari yazarken bu ayrimi
akilda tutmak lazim, cunku kural cogunlukla test koduna atesliyor olabilir.

40 satir esigi su an sabit ve bu repoda metotlarin %4,5'i onu asiyor. p95
degeri 39 satir, yani esik neredeyse tam p95'e denk dusuyor - ama bu tamamen
**tesaduf**. Uretim ve test ayrildiginda bile ayni sayi iki farkli yere
dusuyor: uretimde p95 34 (yani 40 esigi p95'in uzerinde), testte p95 41 (esik
p95'in altinda). Baska bir repoda bambaska bir noktaya denk gelecek. Bu
yuzden Asama 5'te esigi sabit sayi olarak birakmayip repodaki metot
uzunluklarinin yuzdelik dagilimindan turetecegim.
