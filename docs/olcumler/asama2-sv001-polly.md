# SV001'in Polly uzerindeki olcumu

**Tarih:** 2026-09-11
**Repo:** [App-vNext/Polly](https://github.com/App-vNext/Polly)
**Polly commit:** `2247db2407713fa221d57011814e4be446361b1f` (2026-09-08)
**Sievert commit:** `c142311`
**Komut:** `sievert check <polly>` ve `sievert check <polly> --json` (Release derlemesi)

Asama 1'de scan ettigim repoyu, ayni commit'te, bu sefer check ile taradim.
Amac SV001'in gercek kod uzerinde ne buldugunu gormekti.

## Sonuc

**Hic bulgu cikmadi.** Polly'de tek bir `async void` metot yok.

| Olcum | Deger |
|---|---|
| Taranan dosya | 797 |
| SV001 bulgusu | 0 |
| Event handler istisnasiyla muaf tutulan | 0 |
| Toplam async metot | 991 |
| Async metotlara oran | %0 |
| Cikis kodu | 0 |
| Tarama suresi | 0,95 - 1,00 sn (3 kosu) |

Bulgu olmadigi icin uretim/test dagilimi da bos. Karsilastirma olsun diye
async metotlarin kendi dagilimini cikardim, Asama 1'deki ayni heuristikle
(yolunda `test`/`tests` klasoru gecen ya da adi `Test.cs`/`Tests.cs` ile
biten dosyalar test sayiliyor):

| | Uretim | Test | Toplam |
|---|---|---|---|
| Async metot | 145 | 846 | 991 |
| SV001 bulgusu | 0 | 0 | 0 |

Yani 991 async metodun %85'i test kodunda ama hicbiri `async void` degil.

## Sifir sonucu dogrulama

Sifir bulgu, "kural calisiyor ama bir sey yok" da olabilir, "kural bozuk,
hicbir sey bulamiyor" da. Ikisini ayirmak icin uc ayri kontrol yaptim.

**1. Aracin kendi scan ciktisindan saydim.** `scan --json` her metodun
`isAsync` ve `returnType` alanlarini veriyor. JSON'u ayri bir betikle okuyup
`isAsync == true && returnType == "void"` olanlari saydim: 0 cikti. Ayni
betik toplam metodu 4655, async metodu 991 buluyor, ikisi de Asama 1'deki
sayilarla birebir ayni.

**2. Roslyn'den tamamen bagimsiz, duz metin aramasi yaptim.**
`grep -rn "async void"` Polly'de 10 satir buluyor. Hepsine tek tek baktim:
onunun onu da yorum satiri. Besi `RetryAsyncSpecs.cs`, `RetryTResultAsyncSpecs.cs`,
`RetryForeverAsyncSpecs.cs`, `WaitAndRetryAsyncSpecs.cs` ve
`WaitAndRetryForeverAsyncSpecs.cs` dosyalarinda ayni aciklamanin kopyasi.
Tek bir `async void` metot tanimi yok.

**3. Kuralin ateslenebildigini dogruladim.** Ayni Release derlemesiyle
`samples/Patients` klasorunu taradim, beklenen iki bulguyu verdi. Yani kural
sessizce bosa donmuyor.

Async donus tiplerinin dagilimi da tabloyu tamamliyor:

| Sayi | Donus tipi |
|---|---|
| 888 | `Task` |
| 33 | `ValueTask` |
| 17 | `Task<TResult>` |
| 15 | `ValueTask<Outcome<TResult>>` |
| 8 | `ValueTask<Outcome<T>>` |
| 4 | `ValueTask<TResult>` |
| 30 | digerleri (hepsi `Task<...>` ya da `ValueTask<...>`) |

## Bunun anlami

Polly bir kutuphane. `async void` esas olarak UI event handler'larinda
cikan bir kalip; kutuphane kodunda zaten beklenmemesi gerekiyordu. Yani bu
sifir, SV001'in yanlis oldugunu degil, test ettigim reponun bu kural icin
kotu bir orneklem oldugunu gosteriyor.

Isin can sikici tarafi su: SV001'i dogrulamak icin sectigim repo, kuralin
hicbir yonunu dogrulamiyor. Event handler istisnasinin dogru calisip
calismadigini da olcemedim, cunku muaf tutulacak tek bir metot bile cikmadi.
Kuralin gercek kod uzerindeki davranisi hakkinda bu olcumden ogrendigim tek
sey, yanlis pozitif uretmedigi.

## SV001'in goremedigi bir sey, tam da bu repoda yaziyor

Yukaridaki grep sonuclarinda cikan yorumlar aslinda gercek bir tuzagi
anlatiyor. Polly'nin test kodunda soyle bir not var (ornegin
`test/Polly.Specs/Retry/RetryAsyncSpecs.cs:400`):

> An async (...) => { ... } anonymous delegate with no return type may compile
> to either an async void or an async Task method (...) if it compiles to
> async void, then the delegate, when run, will return at the first await, and
> execution continues without waiting for the Action to complete.

Bu, `async void` lambda problemi. SV001 bunu goremiyor, cunku kural sadece
`MethodDeclaration` dugumlerine bakiyor; lambda ve anonim delegate'ler
disarida. Ustelik bir lambda'nin `async void` mi `async Task` mi olduguna
karar vermek icin atandigi hedefin tipini bilmek gerekiyor, yani semantic
model lazim; suan elimde yok.

Kurali yazarken bunu bilincli olarak kapsam disi birakmistim ama gercek bir
repoda tam bu tuzagin yaziyla anlatildigini gormek, kapsamin ne kadar dar
oldugunu somutlastirdi. `sinirliliklar.md`'ye ayri bir madde olarak ekledim.

## Sievert kendi kodunu tarayinca

Ayni komutu Sievert'in kendi reposunda da calistirdim.

| Yol | Dosya | Bulgu | Cikis kodu |
|---|---|---|---|
| Repo koku (`check .`) | 46 | 2 | 1 |
| Sadece `src/` (`check src`) | 26 | 0 | 0 |

Iki bulgunun ikisi de `samples/Patients/AsyncVoid.cs` dosyasindan geliyor:
10. satirdaki `Save` ve 28. satirdaki `Tick`. Bu dosya zaten SV001'i test
etmek icin bilerek yazilmis bir ornek, yani gercek bir sorun degil. Ayni
dosyadaki `OnSaved` event handler istisnasiyla, `SaveAsync` de `async Task`
oldugu icin bulgu uretmiyor; beklenen davranis bu.

Uretim kodunda (`src/`) bulgu yok. Su an repo kokunde `check` calistirmak
cikis kodu 1 donduruyor, cunku ornek dosya da taraniyor. CI'a check adimi
eklersem ya sadece `src/` taranmali ya da ornek klasorunun disarida
birakilmasi gerekecek; bunu henuz yapmadim.

## Sure

| Komut | Sure (3 kosu) |
|---|---|
| `check` | 0,95 / 1,00 / 0,97 sn |
| `scan` | 1,43 / 1,37 / 1,38 sn |

check, scan'den yaklasik %30 hizli. Sebebi belli: check her dosyayi bir kez
ayristirip tek kural calistiriyor, scan ise ayrica butun trivia'yi gezip kor
nokta olcuyor ve dagilim istatistikleri hesapliyor. Ayni makine, ayni Release
derlemesi, ayni girdi.
