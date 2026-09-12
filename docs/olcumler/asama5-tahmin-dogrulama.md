# Model tahminlerinin kor elle dogrulanmasi: sonuc

**Tarih:** 2026-09-12
**Olcut dosyasi:** `asama5-tahmin-dogrulama-olcut.md` (commit `a3088a0`, orneklem
secilmeden once ilan edildi)
**Insan karari commit'i:** `6806406`
**Incelenmeyenlerin kayda gecirildigi commit:** `b364f1a`
**Sonuc dosyasi:** `data/asama5/prediction-validation-results.json`, ozeti
`9c11cf943910039f76e4f4985a1da49e4dcf929a5557efa420c60fc1f9c73668`

## Yontem

> Otuz commitlik kor orneklemin 25'i yazar tarafindan commit diff'i ve sonraki ilgili
> degisiklikler incelenerek siniflandirildi; bes ornek incelenmedi ve paydalardan
> cikarildi. Model tahmini ve otomatik etiket kararlar tamamlanana kadar degerlendiriciye
> gosterilmedi; bagimsiz ikinci degerlendirici kullanilmadi.

Orneklem: her repodan 5 model-pozitif + 5 model-negatif, yalnizca `CsFilesChanged > 0`
olan test commit'lerinden, tohum 20260912. Anahtar dosyasi kararlar commit edildikten
**sonra** acildi.

**Bes ornek (SAMPLE-01, SAMPLE-02, SAMPLE-09, SAMPLE-20, SAMPLE-27, hepsi Polly)
incelenmedi.** Bunlar icin kusur karari uydurulmadi; olcut dosyasindaki `BAKILMADI`
kategorisi tam olarak bu durumu temsil ediyor ve bes ornek butun paydalardan cikarildi.

## Karar dagilimi

| Kume | Incelenen | KUSUR-GETIRDI | KUSUR-GETIRMEDI | VERI-YETMEDI | BAKILMADI | Karar verilen |
|---|---|---|---|---|---|---|
| **Toplam** | 30 | 2 | 23 | 0 | **5** | 25 |
| Model-pozitif | 15 | 1 | 13 | 0 | 1 | 14 |
| Model-negatif | 15 | 1 | 10 | 0 | 4 | 11 |
| Polly | 10 | 1 | 4 | 0 | 5 | 5 |
| ShareX | 10 | 0 | 10 | 0 | 0 | 10 |
| Jellyfin | 10 | 1 | 9 | 0 | 0 | 10 |

Bos karar **0**, taninmayan kategori **0**. Bes `BAKILMADI`'nin tamami Polly'de: 1'i
model-pozitif, 4'u model-negatif grubunda.

## Insan dogrulama precision isareti (yalniz model-pozitif ornekler)

| Kume | Pay | Payda | Oran | Cikarilan VERI-YETMEDI | Cikarilan BAKILMADI |
|---|---|---|---|---|---|
| **Toplam** | 1 | 14 | **0,0714** | 0 | 1 |
| Polly | 1 | 4 | 0,2500 | 0 | 1 |
| ShareX | 0 | 5 | 0,0000 | 0 | 0 |
| Jellyfin | 0 | 5 | 0,0000 | 0 | 0 |

Pay = `KUSUR-GETIRDI`, payda = `KUSUR-GETIRDI` + `KUSUR-GETIRMEDI`.

**Bu, test kumesinin precision'i degildir.** Orneklem tahmin siniflarina esit ve repo
basina sabit secildi; populasyon oranini temsil etmiyor.

## Insan dogrulama kacirma isareti (yalniz model-negatif ornekler)

| Kume | Pay | Payda | Oran | Cikarilan VERI-YETMEDI | Cikarilan BAKILMADI |
|---|---|---|---|---|---|
| **Toplam** | 1 | 11 | **0,0909** | 0 | 4 |
| Polly | 0 | 1 | 0,0000 | 0 | 4 |
| ShareX | 0 | 5 | 0,0000 | 0 | 0 |
| Jellyfin | 1 | 5 | 0,2000 | 0 | 0 |

**Bu, modelin recall'i ya da false-negative rate'i degildir.** Polly'nin model-negatif
paydasi 4 `BAKILMADI` sonrasi 1 satira dustu; o hucredeki oran tek bir karara dayaniyor.

## SZZ etiketi ile insan karari

Yalnizca karar verilmis 25 ornek:

| | KUSUR-GETIRDI | KUSUR-GETIRMEDI |
|---|---|---|
| **SZZ pozitif** | 0 | 6 |
| **SZZ negatif** | 2 | 17 |

VERI-YETMEDI 0, BAKILMADI 5 (ayri sayiliyor, tabloda yok).

- **SZZ pozitif dogrulama isareti:** 0 / 6 = **0,0000**
- **SZZ negatif kacirma isareti:** 2 / 19 = **0,1053**

Repo bazinda:

| Repo | SZZ+ / getirdi | SZZ+ / getirmedi | SZZ− / getirdi | SZZ− / getirmedi | Belirsiz |
|---|---|---|---|---|---|
| Polly | 0 | 1 | 1 | 3 | 5 |
| ShareX | 0 | 1 | 0 | 9 | 0 |
| Jellyfin | 0 | 4 | 1 | 5 | 0 |

**Bu sayilar Asama 4'un 12 / 12 ve 11 / 13 sonuclariyla birlestirilemez.** O orneklemler
farkli secildi (biri etiketli satirlardan, digeri "`IsFix` tarafindan dokunulmus fakat
etiketlenmemis" commit'lerden hedefli); buradaki orneklem model tahmin siniflarina gore
secildi.

## Model–SZZ anlasmazliklarinda insan karari

Anahtardaki `ConfusionAgainstSzz` hucresine gore:

| Hucre | Incelenen | KUSUR-GETIRDI | KUSUR-GETIRMEDI | VERI-YETMEDI | BAKILMADI |
|---|---|---|---|---|---|
| TP (model +, SZZ +) | 6 | 0 | 5 | 0 | 1 |
| FP (model +, SZZ −) | 9 | **1** | 8 | 0 | 0 |
| FN (model −, SZZ +) | 1 | 0 | **1** | 0 | 0 |
| TN (model −, SZZ −) | 14 | **1** | 9 | 0 | 4 |

Ozellikle:

- **Model FP gorunen ama insanin KUSUR-GETIRDI dedigi: 1 / 9.**
- **Model FN gorunen ama insanin KUSUR-GETIRMEDI dedigi: 1 / 1.**
- **Model ve SZZ ikisi de negatifken insanin KUSUR-GETIRDI dedigi: 1 / 14.**
- **Model ve SZZ ikisi de pozitifken insanin KUSUR-GETIRMEDI dedigi: 5 / 6.**

Bunlar **orneklem ici olgular**; tum veri kumesine genellenmiyor.

## Hesaplanmayanlar

Olcut dosyasi geregi su sayilarin hicbiri hesaplanmadi: populasyon accuracy, populasyon
precision, populasyon recall, genel hata orani, tum test kumesinin duzeltilmis F1'i,
duzeltilmis etiket sayisi, yeni model metrigi, 30 ornekten genisletilmis yeniden
agirliklandirma.

Bagimsiz ikinci degerlendirici olmadigi icin Cohen kappa hesaplanmadi ve
degerlendiriciler arasi uyum iddiasi kurulmadi.

## Sinirliliklar

- **Bes ornek incelenmedi** ve hepsi Polly'de; Polly'nin model-negatif paydasi 1 satira
  dustu.
- **Orneklem 30 commit** ve tahmin siniflarina esit dagitildi; populasyonu temsil etmiyor.
- **Tek degerlendirici** ve ayni kisi araci yazdi.
- **Korluk yontemsel:** anahtar dosyasi repoda duruyordu, teknik olarak engellenmis
  degildi. Kararlar commit edildikten sonra acildi.
- **14 ornekte diff 160 satirda kirpilmisti;** karar verilirken tam diff'e bakilip
  bakilmadigi kayitli degil.
