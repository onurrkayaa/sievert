# IsFix: tam kelime mi, kok eslesmesi mi

**Tarih:** 2026-09-11
**Repo:** App-vNext/Polly, 2759 commit (birlestirmeler haric)
**Olcum programi:** `tools/Sievert.Measure`, `measure isfix polly-full`

ADR 0013'te `IsFix`'in iki kusuru yazilmisti: cekimli hâller kaciyor (`fixes`, `fixed`,
`hatayi`) ve duzeltme olmayan seyler yakalaniyor (`fix typo in readme`). Ikisi de
olculmemisti. Bu dosya o iki sayiyi koyuyor.

**Tanim degistirilmedi.** Kural hâlâ tam kelime esliyor. Asagidaki "kok eslesmesi"
sutunu, kural degistirilseydi ne olacagini gosteren bir hesap; kod o hâle gecmedi.

## Kac commit fark ediyor

| Tanim | IsFix cikan commit | Oran |
|---|---|---|
| Mevcut: tam kelime (`fix`, `bug`, `hata`, ...) | 292 | %10,6 |
| Kok eslesmesi (`fix*`, `hata*`, ...) | 330 | %12,0 |
| **Sadece kok eslesmesinde yakalanan** | **38** | %1,4 |

Yani mevcut tanim, kok eslesmesinin bulduklarinin %11,5'ini kaciriyor.

## Kacirilanlardan 20 ornek (recall)

Bu satirlar mevcut tanimda `IsFix = false`, kok eslesmesinde `true` olurdu.
E = gercekten bir duzeltme, H = degil.

| # | Baslik | E/H |
|---|---|---|
| 1 | Copy the Stryker config when patching | H |
| 2 | Correctly state .NETStandard1.0 dependencies | E |
| 3 | Mark issues and PRs as stale | H |
| 4 | (intellisense correction) | E |
| 5 | Corrections to PolicyRegistry in readme (#264) | H |
| 6 | Fixed minor typo. | H |
| 7 | corrected Thread.Sleep replacement | E |
| 8 | Correctly state System.ValueTuple dependency for NetStandard 1.1 tfm (#582) | E |
| 9 | Defend specs against AppVeyor timing issues (#212) | E |
| 10 | Fixed compilation for .NET 3.5 | E |
| 11 | Fixed code example typo | H |
| 12 | Alpha fixes and improvements (#1319) | E |
| 13 | Implemented rolling windows for the advanced circuit breaker ... used correctly ... | H |
| 14 | Defend spec against timing issues | E |
| 15 | Defend timeout specs against timing issues (#167) | E |
| 16 | Fixed name of CircuitBreaker to CircuitBreakerAsync | E |
| 17 | Minor fixes on pipeline registry | E |
| 18 | Defend spec against timing issues | E |
| 19 | Defend specs from timing issues | E |
| 20 | Correctly specify Nito.Async dependency in Net40Async.nuspec (#208) | E |

**14 E / 6 H.** Yani kok eslesmesine gecilseydi eklenen 38 commit'in kabaca %70'i gercek
duzeltme olurdu. Kacirilan gercek duzeltmelerin cogu iki kalipta topluyor: `Fixed ...`
(gecmis zaman) ve `Defend ... against timing issues` (`issues` cogul).

## Mevcut tanimin yakaladiklarindan 20 ornek (precision)

| # | Baslik | E/H |
|---|---|---|
| 1 | Fix CA2000/redundant suppressions (#1947) | E |
| 2 | Fix, tests and doco for issue 620 | E |
| 3 | Allow Fallback policies to take the handled error as input parameter | H |
| 4 | BUG-674: AsyncRetryPolicy does not use ContinueOnCapturedContext configuration | E |
| 5 | Fix issue I was experiencing in my time zone (#1650) | E |
| 6 | Correct intellisense to separate Policy, Policy&lt;TResult&gt;, AsyncPolicy ... | E |
| 7 | Issue 673: Use OperationKey, not ExecutionKey | E |
| 8 | Fix Context.PolicyKey issue 463 | E |
| 9 | Fix mutant | H |
| 10 | Fix S3215 | E |
| 11 | Fix build | E |
| 12 | Fix condition | E |
| 13 | Fix non-generic cache with policywrap | E |
| 14 | Fix samples | E |
| 15 | Fix-up trim warnings | E |
| 16 | Fix Retry strategy example code (#2527) | E |
| 17 | Fix typo | H |
| 18 | Add tests to verify the correct retryCount is being passed to onRetry delegate ... | H |
| 19 | Fix warnings in Polly project (#1514) | E |
| 20 | Fix Minor typo in Bulkhead intellisence (#246) | H |

**15 E / 5 H.** Yani mevcut tanimin yakaladiklarinin kabaca %75'i gercek duzeltme.
Yanlislarin uc tanesi ADR 0013'te tahmin edilen kalip: yazim yanlisi duzeltmesi
(`Fix typo`) ve icinde `error` / `correct` gecen ozellik commit'leri.

## Bu siniflandirmanin siniri

Siniflandirmayi **sadece commit basligina bakarak** yaptim; degisen koda bakmadim.
Bagimsiz bir degerlendirici de yok, ayni kisi hem olcumu yapti hem siniflandirdi.
Sinirda kalan satirlar var ve baskasi farkli isaretleyebilir:

- Test duzeltmeleri (`Defend spec against timing issues`) E sayildi - bozuk olan bir sey
  duzeltiliyor, ama urun kodunda bir hata degil.
- Analiz kurali ihlalleri (`Fix CA2000`, `Fix S3215`) E sayildi, `Fix mutant` H sayildi;
  ikisi de test/kalite tarafinda ve ayrimi tartisilabilir.
- Yazim yanlisi duzeltmeleri H sayildi.

Bu yuzden %70 ve %75 kesin sayilar degil, buyuklük mertebesi. Orneklem 20'ser satir,
yani gercek oran bu degerlerin etrafinda genis bir aralikta.

## Karar yok

Tanim degismedi. Iki sayi burada duruyor ve Asama 5'te veriyle tartisilacak. Su an
soylenebilecek tek sey su: iki tanim arasindaki fark 38 commit, yani toplam etiket
sayisini %13 buyuturdu ve o 38'in yaklasik dortte ucu gercek duzeltme.
