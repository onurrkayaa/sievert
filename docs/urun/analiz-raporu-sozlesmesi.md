# Analiz raporu sozlesmesi

**Surum:** 1.0
**Tarih:** 2026-09-13
**Durum:** Rapor kodu yazilmadan once sabitlendi.

Bu dosya PDF raporun ne oldugunu, ne olmadigini ve hangi girdilerle uretildigini
soyluyor. Risk sozlesmesinin (surum 1.1) urun dili kurallari burada da aynen gecerli;
bu dosya onun uzerine rapora ozel olanlari ekliyor.

## Raporun tanimi

> Secilen repository, risk analiz isi ve opsiyonel statik analiz isi icin uretilmis,
> kaynaklari ve sinirlari kayitli teknik inceleme ozeti.

Rapor **degildir**:

- Resmi bir denetim belgesi.
- Kesin kusur tespiti.
- Kalibre edilmis bir kusur tahmini.

Rapor **odur**: gelistiricinin neye once bakacagina karar vermesine yardim eden
**deneysel** bir cikti. Bu cumle raporun kapaginda da yaziyor.

## Girdi

| Alan | Tur | Varsayilan | Sinir |
|---|---|---|---|
| `RepositoryId` | int | - | zorunlu |
| `RiskAnalysisJobId` | Guid | - | zorunlu |
| `StaticAnalysisJobId` | Guid? | null | opsiyonel |
| `IncludePartial` | bool | `false` | - |
| `CommitWindow` | int | 200 | 50-1000 |
| `FileLimit` | int | 50 | 10-100 |
| `TimelineCount` | int | 100 | 20-500 |
| `TopCommitCount` | int | 20 | 5-100 |
| `FindingLimit` | int | 50 | 0-200 |
| `Culture` | string | `tr-TR` | `tr-TR` ya da `en-US` |
| `Title` | string? | null | en fazla 120 karakter |
| `Notes` | string? | null | en fazla 2000 karakter |

`CommitWindow` ve `FileLimit` varsayilanlari gorsellestirme uclarindan **farkli**
(orada 200 / 100). Sebep: PDF'de yuz satirlik bir tablo iki sayfa suruyor ve okunmuyor;
ekranda kaydirilabilen sey kagitta kaydirilamiyor.

### Title ve Notes

Ikisi de **duz metin**. HTML ya da Markdown yorumlanmaz; girilen isaretler oldugu gibi
basilir. Kontrol karakterleri temizlenir. Uzunluk sinirini asan istek reddedilir,
sessizce kirpilmaz.

## Kismi sonuc

`IncludePartial = false` (varsayilan) ve risk isi tamamlanmamissa istek reddedilir:

- HTTP 422
- `REPORT_PARTIAL_RESULT_NOT_ALLOWED`

`IncludePartial = true` ise rapor uretilir ve su bolumlerin hepsinde kismi oldugu
gorunur:

- Kapak (buyuk ve gorunur uyari)
- Yonetici ozeti
- Her toplu sayi ve gorsellestirme bolumu
- Sinirliliklar

Kismi raporda "en yuksek" gibi ifadeler **"kaydedilmis sonuclar icinde"** diye
nitelenir. Dosya adinda `-partial` bulunur.

Sebep: kismi bir isin "en riskli 20 commit"i, tam isin en riskli 20 commit'i degildir.
PDF elden ele dolasan bir dosya; uzerinde bu bilgi yoksa kaybolur.

## Statik analiz isi

Opsiyonel. Verilirse su kosullari saglamali:

- Ayni repository
- `static-scan` turu
- `succeeded`
- Sonucu tam (`IsResultComplete = true`)

Saglamazsa: `REPORT_STATIC_ANALYSIS_INCOMPATIBLE`.

Statik analiz **verilmezse** rapor "calistirilmadi / rapora eklenmedi" yazar.
**0 bulgu gibi sunulmaz**; ikisi ayni sey degil.

Her durumda rapor sunu yazar: **statik bulgular ham model skoruna dahil degildir.**

## Bolumler

1. Kapak
2. Yonetici ozeti
3. Risk sozlesmesi
4. Risk zaman cizelgesi
5. Dosya etkinlik ozeti
6. Onceliklendirilmis commit listesi
7. Statik bulgular
8. Model aciklamasi
9. Sinirliliklar
10. Kaynak ve butunluk

## Siralama kurallari

Onceliklendirilmis commit listesi:

1. `RiskIndex` azalan
2. Esitlikte tarih azalan
3. Esitlikte SHA ordinal

Ucuncu bir kural var cunku ilk ikisi esitlenebiliyor ve ayni istegin iki kez ayni
siralamayi vermesi gerekiyor.

## Sayi bicimi

- `RawModelScore`: 4 ondalik, **yuzde isareti yok**.
- `RiskIndex`: 1 ondalik, 0-100.
- Katki degerleri: 4 ondalik.
- Kultur `tr-TR` ise ondalik ayraci virgul, `en-US` ise nokta. Ikisinde de ayni sayi.

## Rapor kimligi ve butunluk

- Her raporun bir **canonical input manifest**'i var ve manifestin SHA-256'si raporda
  yaziyor.
- **PDF kendi SHA-256'sini icermez.** Iceremez: ozet dosyanin son halinden hesaplaniyor,
  dosyanin icine yazmak onu degistirir ve ozet artik tutmaz. PDF bunun yerine "ozetin
  indirme metadata'sinda oldugunu" yazar.
- PDF'nin ozeti artefakt metadata ucunda ve indirme cevabinin `ETag` basliginda.

## Yasak

Risk sozlesmesindeki yasak ifadelerin hepsi raporda da yasak. Ek olarak:

- Rapor bir bulguyu "kesin kusur" diye yazmaz.
- Model skorunun ortalamasi "kusur orani" diye sunulmaz.
- Statik bulgu sayisi ile model skoru tek bir sayida birlestirilmez.
- Nedensellik dili kullanilmaz: bir ozniteligin katkisi "bunu bozdu" degil, "skoru su
  yone itti" diye yazilir.
