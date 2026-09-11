# SZZ etiketlerinin dogruluk sayimi

**Tarih:** 2026-09-12
**Malzeme:** `asama4-etiketleme-malzeme.md`
**Liste surumu:** `asama4-etiketleme-dogrulama.md` **v2**
**Olcut dosyasi:** `asama4-etiketleme-olcut.md`, commit `1e835c7`
**Hangi commit ile olculdu:** `b9d7f19`
**Sayim programi:** `tools/Sievert.Measure`, `sayim` modu

## Karar durumu

| | Satir |
|---|---|
| Toplam | 30 |
| Karar dolu | 25 |
| **Karar bos** | **5** |

Bos kalan satirlar: **10, 12, 16, 22, 24**.

**Dogruluk orani hesaplanmadi.** Olcut dosyasindaki kural: bos karar varsa oran
hesaplanmaz, eksik satir sayisi yazilir.

## Karar dagilimi

| Karar | Satir |
|---|---|
| E | 23 |
| H | 2 |
| Belirsiz | 0 |
| bos | 5 |

## Repo kirilimi

| Repo | Satir | E | H | Belirsiz | Bos |
|---|---|---|---|---|---|
| Polly | 10 | 7 | 2 | 0 | 1 |
| ShareX | 10 | 8 | 0 | 0 | 2 |
| Jellyfin | 10 | 8 | 0 | 0 | 2 |

## Etiketli ve etiketsiz satirlar ayri

Iki kume ayri raporlaniyor ve **tek sayida birlestirilmiyor**; paydalari farkli kumeler.

| Kume | Satir | E | H | Belirsiz | Bos | Pay | Payda | Oran |
|---|---|---|---|---|---|---|---|---|
| Etiketli (precision isareti) | 15 | 12 | 0 | 0 | 3 | - | - | hesaplanmadi |
| Etiketsiz (recall isareti) | 15 | 11 | 2 | 0 | 2 | - | - | hesaplanmadi |

Pay ve payda, bes satir bos oldugu icin yazilmadi. Oran hesaplandiginda pay ve payda ayri
yazilacak, Belirsizler paydadan cikarilacak ve kac tane cikarildigi belirtilecek.

Olcut dosyasindaki ters okuma kurali geregi, oran hesaplandiginda pay su olacak:
etiketli kumede **E** sayisi (suclama dogru), etiketsiz kumede **H** sayisi (suclamamak
dogru).

## Satir 21 notu sonuca yansidi mi

Yansimadi: satir 21'e E yazildi ve listede hic Belirsiz yok, yani olcut 6 (birlestirme
commit'i) hicbir satira uygulanmadi.

## Jellyfin etiketleme kosusu

| Olcu | Deger |
|---|---|
| Sure | 5456,49 sn (1,52 saat) |
| Tepe bellek | 1105 MB |
| Tahmin (carpimsal model) | 13 159 sn (3,7 saat) |
| Tahmin / gercek | 2,41 |

Modelin katsayisi (sn / duzeltme / commit):

| Repo | Sure | Duzeltme | Commit | Katsayi |
|---|---|---|---|---|
| Polly | 125,04 sn | 288 | 2953 | 1,47e-4 |
| ShareX | 851,77 sn | 638 | 9479 | 1,41e-4 |
| Jellyfin | 5456,49 sn | 3033 | 30 004 | 6,00e-5 |

Tahmin, Polly ve ShareX'in katsayilarinin ortalamasiyla (1,44e-4) yapilmisti. Jellyfin'in
olculen katsayisi bunun %41'i.

Etiket orani:

| Olcu | Deger |
|---|---|
| Pay (etiketlenen commit) | 4688 |
| Payda (toplam commit) | 22 917 |
| Oran | %20,5 |

Ayni sayilar `asama4-uc-repo.md` icinde de duruyor.

## Ham sayim ciktisi

```
satir           : 30
karar dolu      : 25
karar bos       : 5

Karar dagilimi:
  E        : 23
  H        : 2
  Belirsiz : 0
  bos      : 5

Repo kirilimi:
  Jellyfin   satir 10, E 8, H 0, Belirsiz 0, bos 2
  Polly      satir 10, E 7, H 2, Belirsiz 0, bos 1
  ShareX     satir 10, E 8, H 0, Belirsiz 0, bos 2

Etiket kirilimi:
  etiketli  satir 15, E 12, H 0, Belirsiz 0, bos 3
  etiketsiz satir 15, E 11, H 2, Belirsiz 0, bos 2

Dogruluk orani hesaplanmadi, eksik: 5 satir.
```
