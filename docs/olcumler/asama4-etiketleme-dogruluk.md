# SZZ etiketlerinin dogruluk sayimi

**Tarih:** 2026-09-12
**Malzeme:** `asama4-etiketleme-malzeme.md`
**Liste surumu:** `asama4-etiketleme-dogrulama.md` **v2**
**Olcut dosyasi:** `asama4-etiketleme-olcut.md`, commit `1e835c7`
**Hangi commit ile olculdu:** `3ccad83`
**Sayim programi:** `tools/Sievert.Measure`, `sayim` modu

## Karar durumu

| | Satir |
|---|---|
| Toplam | 30 |
| Karar dolu | 30 |
| Karar bos | 0 |

## Etiketli satirlar - dogru suclama orani

| | Satir |
|---|---|
| Satir | 15 |
| E (suclama dogru) | 12 |
| H (suclama yanlis) | 0 |
| Belirsiz (paydadan cikarildi) | 3 |
| **Pay** | **12** |
| **Payda** | **12** |
| **Oran** | **12/12 = %100,0** |

## Etiketsiz satirlar - kacirma orani

| | Satir |
|---|---|
| Satir | 15 |
| KACIRDI (suclanmaliydi) | 11 |
| DOGRU-RET (suclamamak dogru) | 2 |
| BELIRSIZ (paydadan cikarildi) | 2 |
| **Pay** | **11** |
| **Payda** | **13** |
| **Oran** | **11/13 = %84,6** |

Iki oran **tek sayida birlestirilmedi**; paydalari farkli kumeler (12 ve 13).
Paydadan cikarilan Belirsiz sayisi: etiketli kumede 3, etiketsiz kumede 2, toplam 5.

## Repo kirilimi

Etiketli satirlar:

| Repo | Satir | E | H | Belirsiz |
|---|---|---|---|---|
| Polly | 5 | 5 | 0 | 0 |
| ShareX | 5 | 4 | 0 | 1 |
| Jellyfin | 5 | 3 | 0 | 2 |

Etiketsiz satirlar:

| Repo | Satir | KACIRDI | DOGRU-RET | BELIRSIZ |
|---|---|---|---|---|
| Polly | 5 | 2 | 2 | 1 |
| ShareX | 5 | 4 | 0 | 1 |
| Jellyfin | 5 | 5 | 0 | 0 |

## Satir 21

Suclanan commit'in basligi `Merge pull request #7941 from jellyfin/fix-overflow` ama
commit tek ebeveynli (squash-merge), bu yuzden olcut 6 uygulanmadi ve satira E yazildi.

## Jellyfin etiketleme kosusu

| Olcu | Deger |
|---|---|
| Sure | 5456,49 sn (1,52 saat) |
| Tepe bellek | 1105 MB |
| Tahmin (carpimsal model) | 13 159 sn (3,7 saat) |
| Tahmin / gercek | 2,41 |

| Repo | Sure | Duzeltme | Commit | Katsayi (sn / duzeltme / commit) |
|---|---|---|---|---|
| Polly | 125,04 sn | 288 | 2953 | 1,47e-4 |
| ShareX | 851,77 sn | 638 | 9479 | 1,41e-4 |
| Jellyfin | 5456,49 sn | 3033 | 30 004 | 6,00e-5 |

Tahmin, Polly ve ShareX katsayilarinin ortalamasiyla (1,44e-4) yapilmisti; Jellyfin'in
olculen katsayisi bunun %41'i.

Etiket orani:

| Olcu | Deger |
|---|---|
| Pay (etiketlenen commit) | 4688 |
| Payda (toplam commit) | 22 917 |
| Oran | %20,5 |

## Ham sayim ciktisi

```
satir           : 30
karar dolu      : 30
karar bos       : 0

Repo kirilimi (birinci / ikinci / belirsiz / bos):
  Jellyfin   satir 10, 8 / 0 / 2 / 0
  Polly      satir 10, 7 / 2 / 1 / 0
  ShareX     satir 10, 8 / 0 / 2 / 0

Etiketli satirlar - dogru suclama orani:
  satir                               : 15
  E (suclama dogru)                   : 12
  H (suclama yanlis)                  : 0
  Belirsiz (paydadan cikarildi)       : 3
  bos                                 : 0
  pay                                 : 12
  payda                               : 12
  oran                                : 12/12 = %100.0

Etiketsiz satirlar - kacirma orani:
  satir                               : 15
  KACIRDI (suclanmaliydi)             : 11
  DOGRU-RET (suclamamak dogru)        : 2
  BELIRSIZ (paydadan cikarildi)       : 2
  bos                                 : 0
  pay                                 : 11
  payda                               : 13
  oran                                : 11/13 = %84.6

Iki oran birlestirilmedi: paydalari farkli kumeler.
```
