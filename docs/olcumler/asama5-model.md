# Asama 5 lojistik regresyon modeli

**Tarih:** 2026-09-12
**Durum:** Uc repo icin ayri model egitildi ve kendi test bolumlerinde olculdu. Sonradan
kalibrasyon (Platt, isotonic) uygulanmadi; repo-arasi deney yapilmadi.

Bu adim **repo-arasi genelleme degil**: her model kendi reposunun egitim bolumunde
egitilip yine kendi reposunun test bolumunde olculdu. Repo-arasi deney Adim 5'te.

## 1. Veri ve checksum

Uc girdi de dondurulmus ve bu adim ucunu de yalnizca **okudu**.

| Dosya | SHA-256 |
|---|---|
| `data/asama5/commit-metrics.csv` | `1b8e5a5c64ff9ccc6f8495f90711a05b95b5340b2e778dce59a9c6cea4d46e95` |
| `data/asama5/split-manifest.csv` | `01b5cafa2c82cde3dffc98afc0ed571918ebbc648bb8c74fad2bc071b601b8cb` |
| `data/asama5/baseline-results.json` | `dd62daa7ca7a077645c2053732bcf93291b3459cd1e958797f66cd10f6f38d65` |

Uretilen dosyalar:

| Dosya | SHA-256 |
|---|---|
| `data/asama5/model-results.json` | `cd3b3e3d7575859b7d7bc446024df1ee140aec7f5fb710a4f4b265578249a0b5` |
| `data/asama5/model-predictions.csv` | `21b6987790610530d1d42f43b9d595236864f95a09b6a8ccdbed61d23810ee4f` |
| `data/asama5/models/*.zip` | `data/asama5/models/models.sha256` icinde |

Ureten commit: `3a4c2ce`. Metrik sozlesmesi surum 1.0.

Bolme dogrulandi: train 4896 / 23 915, test 1066 / 10 251; repo bazinda Polly
251 / 1931 ve 10 / 828, ShareX 884 / 5943 ve 129 / 2547, Jellyfin 3761 / 16 041 ve
927 / 6876. Hepsi Adim 1'deki sayilarla ayni.

## 2. Model ve donusum sozlesmesi

| | |
|---|---|
| Paket | **Microsoft.ML 5.0.0** (`Directory.Packages.props` icinde sabit) |
| Trainer | **`LbfgsLogisticRegression`** |
| `MLContext` tohumu | 20260912 |
| L1 duzenlilestirme | **0** |
| L2 duzenlilestirme | 1,0 (ML.NET varsayilani) |
| Is parcacigi | 1 |
| Optimizasyon toleransi | 1e-7 (varsayilan) |
| Gecmis boyutu | 20 (varsayilan) |
| En fazla yineleme | `int.MaxValue` (varsayilan) |
| Negatiflik kisiti | kapali (varsayilan) |
| Baslangic agirlik capi | 0 (varsayilan) |
| Sinif agirligi | yok |
| Yeniden orneklem | yok |
| Karistirma | yok |
| `CacheCheckpoint` | eklenmedi |

**L1 = 0 secimi sonuca bakilarak yapilmadi.** ML.NET varsayilani 1,0 ve o degerde butun
agirliklar sifira dusuyor (kucuk bir ornek uzerinde denendi, 3 agirligin ucu de 0 cikti).
L1 duzenlilestirme katsayilari sifirlayarak **oznitelik secimi** yapiyor, oysa bu adimda
oznitelik eleme yasak ve 15 katsayinin hepsi raporlanacak. Yani L1'i kapatmak bu adimin
kendi kuralindan cikan bir karar; hicbir test sonucu gorulmeden verildi. L2 varsayilan
degerinde birakildi.

**`CacheCheckpoint` eklenmedi:** veri zaten bellekte ve tek gecis yetiyor; eklemek
yalnizca bellek kullanimini artirirdi.

**Olasilik sonradan kalibre edilmedi.** ML.NET modeli bir `PlattCalibrator` ile
sarmaliyor ama parametreleri sabit (slope -1, offset 0), yani cikan sayi tam olarak
sigmoid(skor). Veriden ogrenilen bir Platt olcekleme degil; lojistik baglantinin kendisi.
Kontrol edildi: `Probability` ile `1 / (1 + e^-skor)` ayni.

### Donusum

Su 13 olcuye `log(1 + x)`: `LinesAdded`, `LinesDeleted`, `FilesChanged`,
`CsFilesChanged`, `DirectoryCount`, `SubsystemCount`, `MaxFileAgeDays`,
`MinFileAgeDays`, `PriorChanges`, `PriorFixes`, `DistinctAuthorsOnFiles`,
`AuthorCommitCount`, `AuthorFileExperience`.

`Entropy`: log yok, standartlastirma var. `IsFix`: ikisi de yok, 0/1 kaliyor.

Log sonrasi 14 surekli oznitelik ortalama 0, standart sapma 1 olacak sekilde
standartlastirildi. **Parametreler yalnizca egitim bolumunden ogrenildi**; test satirlari
fit asamasina girmedi ve her repo kendi egitim parametrelerini kullandi.

Negatif deger kontrolu: log uygulanan alanlarda negatif deger yok (olsaydi model
egitilmeden durulacakti). Sifir varyans kontrolu: uc reponun egitim bolumunde 14 surekli
ozniteligin hicbiri sifir varyansli degil.

**Kirpma yok.** Testte egitim araliginin disinda kalan degerler ayni donusumle gecti:

| Repo | Aralik disi deger | Aralik disi satir |
|---|---|---|
| Polly | 641 | 620 / 828 |
| ShareX | 4047 | 2411 / 2547 |
| Jellyfin | 3569 | 2308 / 6876 |

ShareX'te test satirlarinin %94,7'si en az bir ozniteligiyle egitim araliginin disinda.
Bu, egitim ve test bolumlerinin ayni dagilimdan gelmedigine dair dogrudan bir isaret.

## 3. Repo bazinda train/test sonuclari

### Egitim bolumu (train esigiyle)

| Repo | TP | FP | FN | TN | Precision | Recall | F1 | PR-AUC |
|---|---|---|---|---|---|---|---|---|
| Polly | 191 | 226 | 60 | 1454 | 0,4580 (191 / 417) | 0,7610 (191 / 251) | 0,5719 | 0,5416 |
| ShareX | 462 | 865 | 422 | 4194 | 0,3482 (462 / 1327) | 0,5226 (462 / 884) | 0,4179 | 0,3549 |
| Jellyfin | 2456 | 1836 | 1305 | 10 444 | 0,5722 (2456 / 4292) | 0,6530 (2456 / 3761) | 0,6100 | 0,6567 |

### Test bolumu, train'de secilen esikle

| Repo | TP | FP | FN | TN | Precision | Recall | F1 | PR-AUC |
|---|---|---|---|---|---|---|---|---|
| Polly | 7 | 39 | 3 | 779 | 0,1522 (7 / 46) | 0,7000 (7 / 10) | 0,2500 | 0,3033 |
| ShareX | 44 | 298 | 85 | 2120 | 0,1287 (44 / 342) | 0,3411 (44 / 129) | 0,1868 | 0,1192 |
| Jellyfin | 654 | 1091 | 273 | 4858 | 0,3748 (654 / 1745) | 0,7055 (654 / 927) | 0,4895 | 0,5199 |
| **Mikro** | **705** | **1428** | **361** | **7757** | **0,3305 (705 / 2133)** | **0,6614 (705 / 1066)** | **0,4408** | **0,4775** |

Makro F1 0,3088; makro PR-AUC 0,3141.

PR-AUC her iki esikte de ayni cikiyor cunku **ham olasilikla** hesaplaniyor; esik yalnizca
ikili tahmini degistiriyor, siralamayi degil.

Tahmin edilen pozitif sayisi (test, train esigi): Polly 46, ShareX 342, Jellyfin 1745,
mikro 2133. Gercek pozitif 1066.

## 4. 0,5 ve train-tuned esik karsilastirmasi

Secilen esikler: Polly **0,238111** (1931 aday), ShareX **0,216916** (5942 aday),
Jellyfin **0,319985** (16 009 aday). Ucu de 0,5'in altinda, yani egitimde F1'i en cok
buyuten esik daha cok alarm ureten taraftaydi.

| Repo | Esik | TP | FP | FN | TN | Precision | Recall | F1 |
|---|---|---|---|---|---|---|---|---|
| Polly | 0,5 | 4 | 3 | 6 | 815 | 0,5714 (4 / 7) | 0,4000 (4 / 10) | **0,4706** |
| Polly | 0,2381 | 7 | 39 | 3 | 779 | 0,1522 (7 / 46) | 0,7000 (7 / 10) | 0,2500 |
| ShareX | 0,5 | 7 | 30 | 122 | 2388 | 0,1892 (7 / 37) | 0,0543 (7 / 129) | 0,0843 |
| ShareX | 0,2169 | 44 | 298 | 85 | 2120 | 0,1287 (44 / 342) | 0,3411 (44 / 129) | **0,1868** |
| Jellyfin | 0,5 | 478 | 517 | 449 | 5432 | 0,4804 (478 / 995) | 0,5156 (478 / 927) | **0,4974** |
| Jellyfin | 0,3200 | 654 | 1091 | 273 | 4858 | 0,3748 (654 / 1745) | 0,7055 (654 / 927) | 0,4895 |
| Mikro | 0,5 | 489 | 550 | 577 | 8635 | 0,4706 (489 / 1039) | 0,4587 (489 / 1066) | **0,4646** |
| Mikro | train esigi | 705 | 1428 | 361 | 7757 | 0,3305 (705 / 2133) | 0,6614 (705 / 1066) | 0,4408 |

**Sabit 0,5 esigi testte mikro F1'de train esigini geciyor (0,4646'ya karsi 0,4408).**
Uc repodan ikisinde (Polly ve Jellyfin) 0,5 daha yuksek F1 veriyor, ShareX'te train esigi
daha yuksek.

Bu, egitimde F1'i en cok buyuten esigin testte ayni isi yapmadigini gosteriyor. Sebep
olculmedi; aday aciklama test bolumunun pozitif oraninin egitimden dusuk olmasi
(Polly %13,0 -> %1,21, ShareX %14,9 -> %5,06, Jellyfin %23,4 -> %13,48), yani egitimde
"dogru" olan alarm sikligi testte fazla geliyor. **Bu bir tahmin; nedeni olculmedi.**

Ana karsilastirma yine de **train esigiyle** yapiliyor: esik olcumden once ilan edildigi
gibi yalnizca egitimden secildi ve sonuc gorulup degistirilmedi. 0,5 sonucu siliniyor
degil, yukarida duruyor.

## 5. LinesAdded taban karsilastirmasi

Uc toplu kosulun tamami saglandi:

| # | Kosul | Model | Taban | Sonuc |
|---|---|---|---|---|
| 1 | Mikro F1 | 0,4408 | 0,3754 | **GECTI** |
| 2 | Makro F1 | 0,3088 | 0,2999 | **GECTI** |
| 3 | Mikro ham PR-AUC | 0,4775 | 0,2611 | **GECTI** |

**3 / 3.** Yani "model tabani genel olarak gecti" cumlesi yazilabilir.

Ama repo bazinda bir **ters sonuc** var ve gizlenmiyor:

| Repo | Model F1 | Taban F1 | Model PR-AUC | Taban PR-AUC |
|---|---|---|---|---|
| Polly | **0,2500** | **0,2667** | 0,3033 | 0,2610 |
| ShareX | 0,1868 | 0,1520 | 0,1192 | 0,0970 |
| Jellyfin | 0,4895 | 0,4811 | 0,5199 | 0,4663 |

**Polly'de model, tek oznitelikli `LinesAdded` esiginin F1'inin ALTINDA kaldi**
(0,2500'e karsi 0,2667). Ayni repoda PR-AUC'de model onde (0,3033'e karsi 0,2610), yani
siralama daha iyi ama secilen esikle ikili tahmin daha kotu.

Makro F1 farki da dar: 0,3088'e karsi 0,2999, aradaki fark 0,0089. Uc reponun biri ters
yonde oldugu ve Polly'nin testinde yalnizca 10 pozitif bulundugu icin bu farkin
saglamligi gosterilmedi.

## 6. Ham Brier ve ECE

Sonradan kalibrasyon **uygulanmadi**; asagidakiler modelin kendi urettigi ham
olasiliklarin olcusu.

| Kume | Brier | ECE |
|---|---|---|
| Polly | 0,0132 | 0,0292 |
| ShareX | 0,0559 | 0,0625 |
| Jellyfin | 0,0998 | 0,0810 |
| **Mikro** | **0,0819** | **0,0722** |
| **Makro** | **0,0563** | **0,0576** |

Brier ve ECE birlikte okunmali. Polly'nin Brier'i en dusuk (0,0132) ama bu iyi model
demek degil: o kumede pozitif orani %1,21 ve model neredeyse her satira dusuk olasilik
veriyor, yani kare hata kucuk kaliyor.

### Kalibrasyon kutulari

10 esit genislikli kutu; son kutu 1,0'i iceriyor. Bos kutular ECE toplamina 0 agirlikla
giriyor ve tabloda satir sayisi 0 ile duruyor.

**Polly** (bos kutular: [0,6-0,7), [0,8-0,9), [0,9-1,0])

| Kutu | n | Ort tahmin | Gercek | Fark |
|---|---|---|---|---|
| [0,0-0,1) | 746 | 0,0133 | 1 / 746 | 0,0119 |
| [0,1-0,2) | 25 | 0,1544 | 2 / 25 | 0,0744 |
| [0,2-0,3) | 27 | 0,2494 | 1 / 27 | 0,2123 |
| [0,3-0,4) | 15 | 0,3508 | 1 / 15 | 0,2841 |
| [0,4-0,5) | 8 | 0,4506 | 1 / 8 | 0,3256 |
| [0,5-0,6) | 5 | 0,5417 | 3 / 5 | 0,0583 |
| [0,7-0,8) | 2 | 0,7515 | 1 / 2 | 0,2515 |

**ShareX** (bos kutu: [0,9-1,0))

| Kutu | n | Ort tahmin | Gercek | Fark |
|---|---|---|---|---|
| [0,0-0,1) | 1552 | 0,0461 | 30 / 1552 | 0,0268 |
| [0,1-0,2) | 592 | 0,1430 | 47 / 592 | 0,0636 |
| [0,2-0,3) | 224 | 0,2416 | 25 / 224 | 0,1300 |
| [0,3-0,4) | 93 | 0,3531 | 12 / 93 | 0,2241 |
| [0,4-0,5) | 49 | 0,4411 | 8 / 49 | 0,2778 |
| [0,5-0,6) | 15 | 0,5452 | 4 / 15 | 0,2785 |
| [0,6-0,7) | 13 | 0,6549 | 3 / 13 | 0,4241 |
| [0,7-0,8) | 7 | 0,7246 | 0 / 7 | 0,7246 |
| [0,8-0,9) | 2 | 0,8139 | 0 / 2 | 0,8139 |

**Jellyfin** (bos kutu yok)

| Kutu | n | Ort tahmin | Gercek | Fark |
|---|---|---|---|---|
| [0,0-0,1) | 3329 | 0,0362 | 50 / 3329 | 0,0212 |
| [0,1-0,2) | 1053 | 0,1450 | 98 / 1053 | 0,0520 |
| [0,2-0,3) | 643 | 0,2453 | 108 / 643 | 0,0773 |
| [0,3-0,4) | 468 | 0,3476 | 98 / 468 | 0,1382 |
| [0,4-0,5) | 388 | 0,4496 | 95 / 388 | 0,2047 |
| [0,5-0,6) | 243 | 0,5481 | 79 / 243 | 0,2230 |
| [0,6-0,7) | 247 | 0,6508 | 94 / 247 | 0,2703 |
| [0,7-0,8) | 196 | 0,7530 | 85 / 196 | 0,3194 |
| [0,8-0,9) | 179 | 0,8495 | 111 / 179 | 0,2294 |
| [0,9-1,0) | 130 | 0,9407 | 109 / 130 | 0,1023 |

**Mikro** (uc test kumesi birlikte)

| Kutu | n | Ort tahmin | Gercek | Fark |
|---|---|---|---|---|
| [0,0-0,1) | 5627 | 0,0359 | 81 / 5627 | 0,0215 |
| [0,1-0,2) | 1670 | 0,1445 | 147 / 1670 | 0,0564 |
| [0,2-0,3) | 894 | 0,2445 | 134 / 894 | 0,0946 |
| [0,3-0,4) | 576 | 0,3486 | 111 / 576 | 0,1559 |
| [0,4-0,5) | 445 | 0,4487 | 104 / 445 | 0,2149 |
| [0,5-0,6) | 263 | 0,5478 | 86 / 263 | 0,2208 |
| [0,6-0,7) | 260 | 0,6510 | 97 / 260 | 0,2779 |
| [0,7-0,8) | 205 | 0,7521 | 86 / 205 | 0,3325 |
| [0,8-0,9) | 181 | 0,8491 | 111 / 181 | 0,2358 |
| [0,9-1,0) | 130 | 0,9407 | 109 / 130 | 0,1023 |

**Yon tek tarafli: model her kutuda gercekte olandan yuksek olasilik veriyor.** Mikro
tabloda 0,1'in uzerindeki butun kutularda ortalama tahmin gozlenen orandan buyuk ve fark
kutu yukseldikce buyuyor (0,0564'ten 0,3325'e).

**Bu bulguyu otomatik olarak modele baglamiyorum.** Iki aday aciklama var ve ikisi de
olculmedi: (1) test bolumunun pozitif orani egitimden belirgin dusuk, yani modelin
ogrendigi taban oran testte gecerli degil; (2) sag sansur, yani test bolumundeki yeni
commit'lerin bir kismi henuz suclanmamis olabilir (Adim 1'de olculdu: Polly'nin testinde
180 gunden az olgun 207 commit var ve hicbiri pozitif degil). Hangisinin ne kadar
etkiledigi **Adim 4'te olculecek**.

Bu ham sonuclar dondu. Adim 3b'de sonradan kalibrasyon yapilirsa bu tablolar silinmeyecek,
yanina yazilacak.

## 7. Katsayilar ve yuksek korelasyonlar

Katsayilar standartlastirilmis olcekte, yani surekli oznitelikler arasinda
karsilastirilabilir. `IsFix` standartlastirilmadigi icin onun katsayisi 0/1 olcegindedir
ve digerleriyle dogrudan karsilastirilamaz.

| Oznitelik | Polly | ShareX | Jellyfin |
|---|---|---|---|
| `LinesAdded` | +1,0928 | +0,6006 | +1,0400 |
| `LinesDeleted` | +0,0333 | +0,0024 | +0,0755 |
| `FilesChanged` | -1,5044 | -0,7369 | -1,6402 |
| `CsFilesChanged` | +2,0784 | +0,9705 | +1,4920 |
| `Entropy` | -0,1295 | -0,0562 | +0,0528 |
| `DirectoryCount` | -0,1104 | -0,1235 | +0,2503 |
| `SubsystemCount` | +0,2837 | -0,0274 | +0,0872 |
| `MaxFileAgeDays` | +0,0782 | -0,0244 | -0,0455 |
| `MinFileAgeDays` | -0,2124 | -0,2431 | -0,0552 |
| `PriorChanges` | +0,4094 | +0,5911 | -0,0055 |
| `PriorFixes` | -0,4180 | +0,0299 | +0,4511 |
| `DistinctAuthorsOnFiles` | +0,2021 | -0,5205 | +0,0482 |
| `AuthorCommitCount` | -0,1936 | -0,3621 | -0,1621 |
| `AuthorFileExperience` | +0,0745 | +0,2593 | +0,0186 |
| `IsFix` | +0,2807 | +0,2978 | +0,2434 |
| **Intercept** | -3,0713 | -2,1597 | -1,7120 |

En buyuk uc mutlak katsayi:

| Repo | 1 | 2 | 3 |
|---|---|---|---|
| Polly | `CsFilesChanged` +2,0784 | `FilesChanged` -1,5044 | `LinesAdded` +1,0928 |
| ShareX | `CsFilesChanged` +0,9705 | `FilesChanged` -0,7369 | `LinesAdded` +0,6006 |
| Jellyfin | `FilesChanged` -1,6402 | `CsFilesChanged` +1,4920 | `LinesAdded` +1,0400 |

**Ayni uclu, ucunde de.** `CsFilesChanged` ve `LinesAdded` pozitif, `FilesChanged`
negatif isaretli.

**Bunlar nedensellik degil.** "Bu oznitelik hataya neden olur" demiyorum; soylenebilecek
olan, **modelde pozitif ya da negatif iliski tasidigi**. Ozellikle `FilesChanged` ile
`CsFilesChanged` birbirine cok yakin iki olcu (Polly'de rho 0,8058) ve zit isaretli
cikmalari, modelin ikisinin **farkini** kullandigi anlamina geliyor olabilir: `.cs`
disinda cok dosyaya dokunan commit'ler asagi cekiliyor. Bu bir yorum, olculmedi.

### |rho| >= 0,80 olan oznitelik ciftleri (yalnizca train, hedef dahil degil)

Oznitelik **elenmedi** ve model bu sonuca gore yeniden egitilmedi.

| Repo | Cift | rho |
|---|---|---|
| Polly | `FilesChanged` - `Entropy` | 0,9707 |
| Polly | `FilesChanged` - `DirectoryCount` | 0,8544 |
| Polly | `MaxFileAgeDays` - `DistinctAuthorsOnFiles` | 0,8376 |
| Polly | `Entropy` - `DirectoryCount` | 0,8133 |
| Polly | `FilesChanged` - `CsFilesChanged` | 0,8058 |
| ShareX | `PriorChanges` - `PriorFixes` | 0,9336 |
| ShareX | `FilesChanged` - `Entropy` | 0,8988 |
| ShareX | `FilesChanged` - `DirectoryCount` | 0,8763 |
| ShareX | `PriorChanges` - `DistinctAuthorsOnFiles` | 0,8608 |
| ShareX | `Entropy` - `DirectoryCount` | 0,8009 |
| Jellyfin | `FilesChanged` - `Entropy` | 0,9831 |
| Jellyfin | `FilesChanged` - `DirectoryCount` | 0,9257 |
| Jellyfin | `PriorChanges` - `PriorFixes` | 0,9222 |
| Jellyfin | `Entropy` - `DirectoryCount` | 0,9046 |
| Jellyfin | `DirectoryCount` - `SubsystemCount` | 0,8906 |
| Jellyfin | `FilesChanged` - `SubsystemCount` | 0,8146 |

`FilesChanged` ile `Entropy` uc repoda da 0,89'un uzerinde. Bu kadar iliskili iki
oznitelik arasinda katsayinin nasil bolunecegi veriye duyarli: kucuk bir degisiklik
buyuklugu ve isareti kaydirabilir. Yani tek tek katsayilara bakip "sunun etkisi sudur"
demek bu tabloda guvenli degil.

Bu bolumun amaci model secmek degil, **katsayi yorumunun sinirini gostermek**.

## 8. Deterministiklik

| Kontrol | Sonuc |
|---|---|
| `model-results.json` iki kosuda | **bayt bayt ayni** |
| `model-predictions.csv` iki kosuda | **bayt bayt ayni** |
| Model zip dosyalari iki kosuda | **bayt bayt FARKLI** |
| Zip icerikleri (acilmis) | **ayni** |
| Iki egitimin `Probability` farki | **en buyuk mutlak fark 0,0** |
| Iki egitimin 0,5 sinif tahmini farki | **0 satir** |
| Iki egitimin esik sinif tahmini farki | **0 satir** |

Zip dosyalari bayt bayt ayni degil; sebep ML.NET'in yazdigi arsiv meta verisi (dosya
zaman damgalari). Arsivler acilip iceriklerine bakildiginda dosyalar birebir ayni ve
tahminler bit duzeyinde ayni cikiyor. **Model yeniden paketlenmedi**, sayilar oldugu gibi
yaziliyor.

`model-predictions.csv` kontrolleri: 10 251 satir, 10 251 benzersiz repo+SHA, tekrar 0,
manifestteki test sirasiyla birebir ayni sira, egitim SHA'si sizmasi 0, eksik test satiri
0. Olasiliklar sonlu ve [0, 1] araliginda (en kucuk 7,7e-05, en buyuk 0,9937).

## 9. Beklenti karsilastirmasi

`asama5-beklenti.md` **degistirilmedi.**

### En guclu uc oznitelik

Beklenti dosyasindan aynen:

> 1. **CsFilesChanged**
> 2. **LinesAdded**
> 3. **PriorFixes**

| Sira | Beklenen | Polly | ShareX | Jellyfin |
|---|---|---|---|---|
| 1 | `CsFilesChanged` | `CsFilesChanged` | `CsFilesChanged` | `FilesChanged` |
| 2 | `LinesAdded` | `FilesChanged` | `FilesChanged` | `CsFilesChanged` |
| 3 | `PriorFixes` | `LinesAdded` | `LinesAdded` | `LinesAdded` |

**Kismen tuttu.** Uc olcunun ikisi (`CsFilesChanged`, `LinesAdded`) uc repoda da ilk
ucte. `PriorFixes` **hicbir repoda ilk ucte degil** (Polly -0,4180, ShareX +0,0299,
Jellyfin +0,4511) ve yerini beklemedigim `FilesChanged` aldi.

`CsFilesChanged`'in ilk sirada olmasi beklentinin gerekcesiyle uyumlu: etiket yalnizca
silinen `.cs` satirlari blame edilerek uretildigi icin hic `.cs` dosyasina dokunmayan bir
commit pozitif etiket alamiyor. Beklenti dosyasinda bunun bir uyari oldugu da yaziyordu.

`PriorFixes` neden dusuk kaldi, **nedeni olculmedi**. Isaretinin repolar arasinda
degismesi (Polly'de negatif, Jellyfin'de pozitif) ve `PriorChanges` ile yuksek korelasyonu
(ShareX 0,9336, Jellyfin 0,9222) akla gelen adaylar, ama bu bir tahmin.

### Ayni-repo F1 beklentisi

Beklenti dosyasi (6. madde): lojistik regresyonun ayni repo icindeki F1'i icin **%20-45**,
ve LinesAdded esiginin **uzerinde** ama farkin buyuk olmamasi.

| Repo | Model test F1 (train esigi) | Bant | Sonuc |
|---|---|---|---|
| Polly | %25,0 | %20-45 | tuttu |
| ShareX | %18,7 | %20-45 | **TUTMADI**, bandin altinda |
| Jellyfin | %48,9 | %20-45 | **TUTMADI**, bandin ustunde |
| Mikro | %44,1 | — | bant icinde |
| Makro | %30,9 | — | bant icinde |

Uc repodan biri bandin altinda, biri ustunde kaldi. **Nedeni olculmedi.**

"LinesAdded esiginin uzerinde ama fark buyuk degil" kismi: uc repodan ikisinde uzerinde
(ShareX +0,0348, Jellyfin +0,0084), birinde **altinda** (Polly -0,0167). Farkin kucuk
olacagi beklentisi tuttu; uzerinde olacagi beklentisi Polly'de tutmadi.

## 10. Bilinen sinirliliklar

- **Polly'nin test bolumunde 10 pozitif var.** O reponun butun F1, precision ve recall
  sayilari bu 10 satira dayaniyor; tek bir satirin yer degistirmesi F1'i gozle gorulur
  oynatir. Polly'deki ters sonuc (model tabanin altinda) da ayni kirilganlikta.
- **Tek egitim, tek esik, guven araligi yok.** Model bir kez egitildi; tabanlardaki
  rastgele deneyin aksine burada tekrar edilecek bir rastgelelik yok, o yuzden
  "0,4408 ile 0,3754 arasindaki fark gurultuden buyuk mu" sorusu **yanitlanmadi**.
- **Kalibrasyon tek yonlu bozuk ve sebebi olculmedi.** Model her kutuda gercekten yuksek
  olasilik veriyor. Test/egitim taban oran farki ve sag sansur aday aciklamalar; ikisi de
  Adim 4'te olculecek.
- **Egitim ve test dagilimlari ayni degil.** ShareX'te test satirlarinin %94,7'si en az
  bir ozniteligiyle egitim araliginin disinda. Kirpma yapilmadi, bu bir secim; etkisi
  olculmedi.
- **Katsayilar korelasyonlu ozniteliklerle birlikte okunmali.** `FilesChanged` ile
  `Entropy` uc repoda da 0,89'un uzerinde; boyle ciftlerde tek bir katsayinin isareti ve
  buyuklugu kararsiz olabiliyor.
- **L2 = 1,0 varsayilan degerinde birakildi ve etkisi olculmedi.** Duzenlilestirmenin
  katsayilari ne kadar kucultugu sinanmadi; hiperparametre aramasi bu adimda yasakti.
- **Sinif agirligi ve yeniden orneklem yok.** Olasiliklarin dogal taban oranindaki
  davranisini olcmek icin bilerek boyle; agirlikli bir model ayri bir duyarlilik deneyi
  olur ve bu adimda yapilmadi.
- **11 / 13 sonucu kullanilmadi.** Hedefli secilmis orneklemden gelen o oran bu adimda
  hicbir yere girmedi: etiket degistirilmedi, agirlik verilmedi.
