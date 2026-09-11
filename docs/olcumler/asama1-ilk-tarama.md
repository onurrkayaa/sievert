# Ilk gercek repo taramasi

**Tarih:** 2026-09-11
**Repo:** [App-vNext/Polly](https://github.com/App-vNext/Polly)
**Commit:** `2247db2407713fa221d57011814e4be446361b1f` (2026-09-08)
**Komut:** `sievert scan <polly> --json` (Release derlemesi)

Polly'yi sectim cunku orta buyuklukte (797 .cs dosyasi, 15 MB), modern C# ile
yazilmis ve async agirlikli bir kutuphane. Ilk kuralim SV001 async void
olacagi icin async kullanan gercek bir repo uzerinde olcum almak istedim.
Repoyu Sievert'in disinda gecici bir klasore shallow olarak klonladim.

## Sonuclar

| Olcum | Deger |
|---|---|
| Dosya | 797 |
| Tip | 880 |
| Metot | 4655 |
| Async metot | 991 (%21,3) |
| Ortalama metot uzunlugu | 15,8 satir |
| Medyan metot uzunlugu | 12 satir |
| p90 / p95 / p99 | 32 / 39 / 68 satir |
| En uzun metot | 219 satir |
| 40 satiri asan metot | 210 (%4,5) |
| Hic tip bulunamayan dosya | 24 |
| Ayristirilamayan dosya | 1 |
| Tarama suresi | 0,83 - 0,89 sn (3 calistirma) |

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

### Klasore gore dagilim

| Klasor | Dosya | Metot | Ortalama | Medyan | Async | 40+ |
|---|---|---|---|---|---|---|
| src/ | 416 | 1526 | 11,5 | 8 | %7,9 | 55 |
| test/ | 318 | 2977 | 18,5 | 15 | %28,4 | 153 |
| bench/ | 42 | 141 | 6,2 | 3 | %14,9 | 2 |
| samples/ | 20 | 11 | 14,9 | 16 | %27,3 | 0 |

### Ayristirilamayan dosya

Tek dosya: `cake.cs`, 6 hata. Hepsi ayni sebepten: dosya `#:package ...`
satirlariyla basliyor, yani .NET 10'un dosya tabanli program sozdizimi.
Roslyn'e duz bir .cs dosyasi gibi verdigim icin bu satirlari taniyamiyor.
Beklenen bir durum, kotu bir dosya degil.

## Beklemedigim seyler

**Enum ve delegate hic sayilmiyor.** "Hic tip bulunamadi" cikan 24 dosyanin
yarisi aslinda bos degil: `CircuitState.cs`, `PollyVersion.cs`,
`TimeoutStrategy.cs` gibi dosyalar enum tanimliyor, `ExceptionPredicate.cs` ve
`ResultPredicate.cs` delegate tanimliyor. Walker'im sadece class, record,
struct ve interface ziyaret ediyor. Repoda yaklasik 15 enum ve 2 delegate var,
hicbiri sayilara girmedi.

**`#if` bloklari gorunmez.** `BinarySerializationUtil.cs` dosyasinin tamami
`#if NETFRAMEWORK` icinde. Hicbir preprocessor sembolu tanimlamadan
ayristirdigim icin Roslyn bu blogu devre disi metin sayiyor ve icindeki sinif
agaca hic girmiyor. Repoda 61 dosyada `#if` var; kaba bir hesapla yaklasik 537
satir, 15 tip ve 34 metot bu sekilde gorulmuyor. Hata da vermiyor, sessizce
kayboluyor - en rahatsiz edici kismi bu.

**219 satirlik test metodu gercekten oyle.** Once olcum hatasi sandim,
dosyaya bakip dogruladim: `Should_throw_when_cache_provider_is_null` 54.
satirda basliyor, 272. satirda bitiyor, arasi tek bir metot govdesi. Yani
uzunluk hesabim dogru calisiyor.

**En uzun 10 metodun 9'u test kodu.** Tek istisna `Migration.Retry_V8`, o da
dokumantasyon ornegi. Uretim kodunun en uzun metodu listeye bile giremiyor.

**Uretilen kod dosyasi hic yok.** Polly'de `.g.cs` ya da `.Designer.cs`
bulamadim, yani sinirliliklar arasinda yazdigim "uretilen kod ayirt
edilmiyor" maddesi bu olcumu etkilemedi. Baska bir repoda etkileyecektir.

## Yorum

Ortalama 15,8 satir, medyan 12 satir. Aradaki fark kucuk gorunuyor ama yonu
onemli: ortalama medyanin uzerinde, yani dagilim saga carpik. Metotlarin
yarisi 12 satirin altinda, buna karsilik 219 satirlik bir kuyruk var ve
ortalamayi yukari cekiyor. Bu yuzden "ortalama metot uzunlugu" bir repoyu
anlatmak icin tek basina zayif bir sayi; birkac dev metot onu istedigi gibi
oynatabiliyor.

Ayni fark klasor bazinda daha da belirginlesiyor. src/ tarafinda ortalama 11,5
medyan 8, test/ tarafinda ortalama 18,5 medyan 15. Test kodu hem daha uzun hem
daha carpik, ve 40 satiri asan 210 metodun 153'u test. Risk skoru hesaplarken
test kodunu uretim kodundan ayirmam gerekecek, yoksa skor buyuk olcude test
dosyalarinin sekline gore olusur.

Async orani %21,3 ilk bakista yuksek geldi, ama src/ tarafinda %7,9, test/
tarafinda %28,4. Yani Polly'nin kendisi sanildigi kadar async degil; async
agirligi testlerden geliyor. Kural motoruna gecerken bu ayrimi akilda tutmak
lazim.

Esik tarafinda su an 40 satiri sabit kullaniyorum ve bu repoda metotlarin
%4,5'i esigi asiyor. p95 degeri 39 satir, yani 40 esigi tesaduf eseri bu repo
icin p95'e denk dusmus. Baska bir repoda ayni sayi cok farkli bir noktaya
denk gelecek; esigi dagilimdan turetme fikri boylece daha da mantikli duruyor.
