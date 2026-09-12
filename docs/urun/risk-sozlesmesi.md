# Urun dili ve risk sozlesmesi

**Surum:** 1.0
**Tarih:** 2026-09-12
**Durum:** API yazilmadan once sabitlendi.

Bu dosya, modelin ciktisinin urunde nasil adlandirilacagini soyluyor. Amaci teknik degil:
Asama 5'in olctugu sinirlar urun diline gecmezse, kullanici elindeki sayiyi olmadigi bir
sey sanar.

## Neden bu dosya var

Asama 5'in kapanisindan uc sayi:

- **Uretim icin kalibrator secilmedi** (ADR 0019, ADR 0022). Model skoru kalibre edilmis
  bir olasilik **degil**.
- **Hedef SZZ'nin otomatik etiketi.** Kor insan ornekleminde model-pozitif precision
  isareti **1 / 14**, SZZ pozitif dogrulama isareti **0 / 6**.
- **`CsFilesChanged = 0` olan 8607 / 34 166 commit'in pozitifi 0.** Veri kumesinin
  %25,2'si tasarim geregi pozitif etiketlenemiyor.

## Yasak ifadeler

API cevabinda, alan adinda, OpenAPI aciklamasinda, arayuz metninde ve dokumanda
**kullanilmaz**:

- "Hata olasiligi"
- "Bu commit %X ihtimalle hata cikarir"
- "Model gercek hatayi tahmin eder"
- "Kalibre edilmis olasilik"
- "Tum C# repolarinda genellenir"
- "Statik analiz ve ML birlesik skoru"

`probability` **alan adi olarak da kullanilmaz**; alan adinin kendisi bir iddia.

## Kullanilabilecek ifadeler

- "Ham model skoru"
- "SZZ tabanli hedefe gore model skoru"
- "Goreli risk endeksi"
- "Secilen model profilinin egitim dagilimina gore yuzdelik"
- "Karar esigi"
- "Statik analiz bulgulari"
- "Tarihsel risk sinyalleri"
- "Deneysel model"

## Tanimlar

### RawModelScore

ML.NET lojistik regresyonunun ham `Probability` ciktisi, `[0, 1]` araliginda.

**Kalibre edilmis bir olasilik degil.** Kullaniciya yuzde hata ihtimali olarak
sunulmaz. API alan adi `rawModelScore`.

### IsCalibrated

Bu asamada **her zaman `false`**. API sozlesmesinde acik bir alan olarak duruyor ki
gelecekte bir kalibrator secilirse degistigi gorulebilsin.

### DecisionAt05

`RawModelScore >= 0,5`. Modelin kendi olasiligini oldugu gibi kullanan karar.

### DecisionAtTrainThreshold

`RawModelScore >= DecisionThreshold`. Repo modelinin egitim bolumunde secilmis esigine
gore karar.

### DecisionThreshold

Kullanilan egitim esigi. **`RawModelScore`'dan ayri bir alan**; ikisi ayni cevapta ayri
ayri duruyor ki karar ile skor karistirilmasin.

Olculen degerler: Polly 0,2381, ShareX 0,2169, Jellyfin 0,3200.

### RiskIndex

0 ile 100 arasinda **goreli siralama endeksi**.

- `RawModelScore * 100` **DEGIL**.
- Secilen model profilinin **egitim (train) skor dagilimindaki yuzdelik sirasi**.
- 0: egitim skorlarinin alt ucu. 100: ust ucu.
- **Gercek hata ihtimali degil.**
- **Farkli model profillerinin `RiskIndex` degerleri dogrudan karsilastirilamaz** - her
  profil kendi egitim dagilimina gore olceklenir.

Hesabi: bir skorun altinda ya da ona esit egitim skorlarinin ampirik yuzdeligi, esitlikte
orta sira (mid-rank):

```
percentile = (lessCount + 0,5 * equalCount) / trainCount
RiskIndex  = round(percentile * 100, 1)
```

Egitim minimumunun altinda **0,0**; maksimumunun ustunde **100,0**.

### StaticFindings

SV kurallarinin (SV001-SV007) bulgulari. **Model skorunun parcasi degil**, cevapta ayri
bir bolumde duruyor ve `includedInModelScore: false` tasiyor.

### HistoricalRisk

15 tarihsel oznitelik ve onlardan cikan lojistik model sonucu. Modelin gordugu tek girdi
bu.

### CombinedRisk

**Bu asamada yok.** Statik bulgular ile model skorunu birlestiren bir agirlik
**olculmedi**; olculmemis bir agirlikla birlestirmek, olmayan bir sayiyi uretmek olurdu.

API alanina **bile eklenmiyor** - bos bir alan bile "yakinda gelecek bir skor" izlenimi
verir.

### ModelProfile

Uc deger: `polly`, `sharex`, `jellyfin`. Her biri Asama 5'te ayri egitilmis bir
ayni-repo modeline karsilik geliyor.

### KnownRepository

`RepositoryIdentity` uc egitim reposundan biriyle tam (normalize) esliyorsa uygun profil
otomatik secilir.

### UnknownRepository

- Sessizce profil **secilmez**.
- Bu turda bilinmeyen repo icin **skorlama yok**; API `422` doner.
- Gelecekte kullanici profili acikca secebilir ya da ayri bir genel model karari
  verilebilir.
- **Varsayilan profil yok.**

Gerekce: repo-arasi deneyler tutarli bir evrensel profil gostermedi (tek kaynak 3 / 6,
leave-one-out 1 / 3 ayni-repo F1'inin ustunde).

### CoverageWarning

`CsFilesChanged = 0` ise:

- Model yine **teknik olarak** skor uretebilir.
- Ama SZZ hedefi bu grupta **0 / 8607** pozitiftir.
- API acik bir kapsam uyarisi doner: `CS_LABEL_COVERAGE_LIMIT`.

### BlindValidationWarning

Kor insan ornekleminde model-pozitif precision isareti **1 / 14**. Bu populasyon
precision'i **degildir**, fakat skorun gercek hata olasiligi olarak sunulmasini engelleyen
bir sinirliliktir. Kod: `HUMAN_VALIDATION_LIMITED`.

## Uyari kodlari

| Kod | Ne zaman |
|---|---|
| `UNCALIBRATED_SCORE` | her degerlendirmede |
| `SZZ_TARGET` | her degerlendirmede |
| `STATIC_ANALYSIS_NOT_INCLUDED` | her degerlendirmede |
| `HUMAN_VALIDATION_LIMITED` | her degerlendirmede |
| `CS_LABEL_COVERAGE_LIMIT` | `CsFilesChanged = 0` oldugunda |
| `OUTSIDE_TRAIN_RANGE` | en az bir surekli oznitelik egitim araliginin disindaysa |
| `EXTERNAL_MODEL_PROFILE` | kullanilan profil, commit'in reposundan farkliysa |
| `UNKNOWN_REPOSITORY_MODEL` | bilinmeyen repo icin profil istendiginde |

Ilk **uc** uyari (`UNCALIBRATED_SCORE`, `SZZ_TARGET`, `STATIC_ANALYSIS_NOT_INCLUDED`)
**her cevapta zorunlu**.

## Aciklama dili

Oznitelik katkilari **nedensel degil**. Modelde tasinan iliskiyi anlatir.

**Dogru:** "Model skorunu yukari tasidi", "Modelde pozitif iliski tasidi", "Bu deger
egitim ortalamasinin ustundeydi".

**Yanlis:** "Hataya neden oldu", "Bu commit kesin riskli", "Bu ozellik hatayi yaratti".

`FilesChanged` katsayisi negatif, `CsFilesChanged` pozitif cikabilir; bu bir hata degil
(ADR 0018) ve isaret degistirilmez.
