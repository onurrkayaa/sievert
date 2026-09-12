# Adim 3b: kalibrasyon ve istatistiksel belirsizlik

**Tarih:** 2026-09-12
**Durum:** Iki kalibrasyon yontemi olculdu ve eslesmis blok bootstrap kosuldu. Ana model,
ana bolme, taban sonuclari ve Adim 3 sonuclari **degistirilmedi**.

## 1. Veri ve checksum

Girdilerin hepsi dondurulmus; adimin basinda ve sonunda dogrulandi, hicbiri degismedi.

| Dosya | SHA-256 |
|---|---|
| `commit-metrics.csv` | `1b8e5a5c…4d46e95` |
| `split-manifest.csv` | `01b5cafa…601b8cb` |
| `baseline-results.json` | `dd62daa7…f6f38d65` |
| `model-results.json` | `cd3b3e3d…49a0b5` |
| `model-predictions.csv` | `21b69877…3810ee4f` |

Uretilenler:

| Dosya | SHA-256 |
|---|---|
| `calibration-manifest.csv` | `e97337503e0ba37b33c69862bcee86f3bcc30cd3a3f02af6ce92f004ed19da58` |
| `calibration-results.json` | `7a3456cba3a6e56d33b7ac79d67f3c424955e04cc1d4b4f7c5c37f9e7d05debb` |
| `calibration-predictions.csv` | `8e54e656114fce2052ca6b6adf5b4dc287a70148912339152118e9e4809a74b7` |
| `bootstrap-results.json` | `5acd9b445dd4f612fe784e7304d414fe27246c6ee22e4529ab4655785a233467` |

Ureten commit `7463d66`. Dort dosya ve dort SVG iki kez uretildi, hepsi `cmp` ile bayt
bayt ayni.

## 2. Kalibrasyon alt bolmesi

Ana train/test manifesti **degismedi**. Her reponun mevcut train bolumu zaman sirasiyla
ikiye ayrildi: ilk `floor(trainN * 0,80)` model-fit, kalani calibration.

| Repo | Model-fit | Calibration | Test |
|---|---|---|---|
| Polly | 226 / 1544 | 25 / 387 | 10 / 828 |
| ShareX | 767 / 4754 | 117 / 1189 | 129 / 2547 |
| Jellyfin | 3064 / 12 832 | 697 / 3209 | 927 / 6876 |

Dokuz sayinin dokuzu da beklenenle ayni. Uc repoda da calibration bolumunde hem pozitif
hem negatif var; biri olmasaydi o repo icin kalibrasyon calistirilmayacakti.

Sinir commit'leri:

| Repo | Model-fit son | Calibration ilk | Calibration son | Test ilk |
|---|---|---|---|---|
| Polly | 2023-10-24T08:57:40Z `8dce9945` | 2023-10-24T09:29:07Z `f97143fe` | 2024-07-13T07:40:54Z `06b39ff4` | 2024-07-13T08:11:39Z `76a93a50` |
| ShareX | 2019-03-30T13:18:39Z `9689f3dc` | 2019-03-31T07:46:15Z `be3363ea` | 2021-08-22T18:20:38Z `c7de378b` | 2021-08-22T19:03:45Z `229cb4b2` |
| Jellyfin | 2020-05-29T14:20:47Z `0476acf5` | 2020-05-29T14:22:16Z `b79957d0` | 2021-05-29T08:56:38Z `d44025c6` | 2021-05-30T14:43:16Z `40a43f94` |

Uc repoda da **cakisma 0** ve **ters zaman cifti 0**.

## 3. Kalibrasyon sonuclari

Model yalnizca model-fit bolumunde egitildi; calibration trainer'a, test hicbir fit
islemine girmedi. **Bu azaltilmis-egitim modeli Adim 3'un tam-train modelinin yerine
gecmiyor.**

Platt parametreleri: Polly `a = 1,0016`, `b = -0,3212` (5 yineleme); ShareX `a = 0,9615`,
`b = -0,3500` (5 yineleme); Jellyfin `a = 1,0669`, `b = 0,0535` (4 yineleme). Uc egim de
pozitif, yani donusum monoton artan. Isotonic blok sayisi: Polly 266, ShareX 232,
Jellyfin 619.

### Brier ve ECE

| Kume | Yontem | Brier | ECE | Ort tahmin | Gercek |
|---|---|---|---|---|---|
| Polly | ham | 0,0142 | 0,0320 | 0,0441 | 10 / 828 |
| Polly | Platt | **0,0120** | **0,0232** | 0,0345 | 10 / 828 |
| Polly | isotonic | **0,0114** | **0,0150** | 0,0271 | 10 / 828 |
| ShareX | ham | 0,0582 | 0,0712 | 0,1218 | 129 / 2547 |
| ShareX | Platt | **0,0525** | **0,0467** | 0,0974 | 129 / 2547 |
| ShareX | isotonic | **0,0523** | **0,0441** | 0,0947 | 129 / 2547 |
| Jellyfin | ham | 0,1025 | 0,0903 | 0,2252 | 927 / 6876 |
| Jellyfin | Platt | 0,1040 | 0,0900 | 0,2248 | 927 / 6876 |
| Jellyfin | isotonic | 0,1035 | 0,0861 | 0,2209 | 927 / 6876 |
| **Mikro** | ham | 0,0843 | 0,0809 | 0,1849 | 1066 / 10 251 |
| **Mikro** | Platt | **0,0838** | **0,0738** | 0,1777 | 1066 / 10 251 |
| **Mikro** | isotonic | **0,0833** | **0,0699** | 0,1739 | 1066 / 10 251 |
| Makro | ham | 0,0583 | 0,0645 | | |
| Makro | Platt | 0,0562 | 0,0533 | | |
| Makro | isotonic | 0,0557 | 0,0484 | | |

### Kalibrasyon basari kosullari

Sozlesme: bir yontem icin "kalibrasyonu iyilestirdi" yalnizca **hem Brier hem ECE**
hamdan dusukse yazilabilir.

| Kume | Platt | Isotonic |
|---|---|---|
| **Mikro** | **iyilestirdi** (Brier 0,0843 → 0,0838; ECE 0,0809 → 0,0738) | **iyilestirdi** (Brier 0,0843 → 0,0833; ECE 0,0809 → 0,0699) |
| Polly | **iyilestirdi** (0,0142 → 0,0120; 0,0320 → 0,0232) | **iyilestirdi** (0,0142 → 0,0114; 0,0320 → 0,0150) |
| ShareX | **iyilestirdi** (0,0582 → 0,0525; 0,0712 → 0,0467) | **iyilestirdi** (0,0582 → 0,0523; 0,0712 → 0,0441) |
| Jellyfin | **karisik sonuc**: Brier 0,1025 → 0,1040 (yukseldi), ECE 0,0903 → 0,0900 (dustu) | **karisik sonuc**: Brier 0,1025 → 0,1035 (yukseldi), ECE 0,0903 → 0,0861 (dustu) |

Jellyfin'de iki yontem de karisik sonuc verdi ve tek sayiya indirgenmedi. Uc reponun
ikisinde iki yontem de iki kosulu birden sagladi.

**Platt ile isotonic arasinda kazanan secilmedi.** Ikisi onceden ilan edilmis iki ayri
deney ve ikisi de yukarida duruyor.

### PR-AUC

| Kume | ham | Platt | isotonic |
|---|---|---|---|
| Polly | 0,2700 | **0,2700** | 0,2837 |
| ShareX | 0,1176 | **0,1176** | 0,1213 |
| Jellyfin | 0,5174 | **0,5174** | 0,5147 |
| Mikro | 0,4742 | 0,4770 | 0,4755 |
| Makro | 0,3017 | **0,3017** | 0,3066 |

**Platt repo bazinda PR-AUC'yi hic degistirmedi** (uc repoda da ondalik basamagina kadar
ayni): donusum monoton artan, yani skorlarin sirasi korunuyor.

**Mikro PR-AUC'de Platt yine de degisti** (0,4742 → 0,4770). Sebep: mikro havuz uc reponun
satirlarini birlestiriyor ve her repo **kendi** Platt donusumunu goruyor. Repo icindeki
sira korunuyor ama repolar **arasindaki** sira degisebiliyor. Bu bir kalibrasyon basarisi
degil, mikro havuzun yapisindan gelen bir etki.

**Isotonic PR-AUC'yi her yerde bir miktar degistirdi** (Polly +0,0137, ShareX +0,0037,
Jellyfin -0,0027). Sebep sozlesmede yaziliydi: PAV bloklari icindeki satirlar ayni degeri
aliyor, yani daha once ayri olan skorlar esitleniyor ve esit skorlar tek esik grubu olarak
isleniyor. **Bu degisiklik kalibrasyon basarisi sayilmiyor**, yonu ne olursa olsun.

### 0,5 esiginde siniflandirma

| Kume | Yontem | TP | FP | FN | TN | Precision | Recall | F1 |
|---|---|---|---|---|---|---|---|---|
| Polly | ham | 4 | 5 | 6 | 813 | 0,4444 (4 / 9) | 0,4000 (4 / 10) | 0,4211 |
| Polly | Platt | 1 | 2 | 9 | 816 | 0,3333 (1 / 3) | 0,1000 (1 / 10) | 0,1538 |
| Polly | isotonic | 5 | 7 | 5 | 811 | 0,4167 (5 / 12) | 0,5000 (5 / 10) | 0,4545 |
| ShareX | ham | 10 | 37 | 119 | 2381 | 0,2128 (10 / 47) | 0,0775 (10 / 129) | 0,1136 |
| ShareX | Platt | 4 | 22 | 125 | 2396 | 0,1538 (4 / 26) | 0,0310 (4 / 129) | 0,0516 |
| ShareX | isotonic | 10 | 37 | 119 | 2381 | 0,2128 (10 / 47) | 0,0775 (10 / 129) | 0,1136 |
| Jellyfin | ham | 487 | 558 | 440 | 5391 | 0,4660 (487 / 1045) | 0,5254 (487 / 927) | 0,4939 |
| Jellyfin | Platt | 500 | 594 | 427 | 5355 | 0,4570 (500 / 1094) | 0,5394 (500 / 927) | 0,4948 |
| Jellyfin | isotonic | 485 | 558 | 442 | 5391 | 0,4650 (485 / 1043) | 0,5232 (485 / 927) | 0,4924 |
| **Mikro** | ham | 501 | 600 | 565 | 8585 | 0,4550 (501 / 1101) | 0,4700 (501 / 1066) | 0,4624 |
| **Mikro** | Platt | 505 | 618 | 561 | 8567 | 0,4497 (505 / 1123) | 0,4737 (505 / 1066) | 0,4614 |
| **Mikro** | isotonic | 500 | 602 | 566 | 8583 | 0,4537 (500 / 1102) | 0,4690 (500 / 1066) | 0,4613 |

Makro F1 (0,5 esiginde): ham 0,3429, Platt 0,2334, isotonic 0,3535.

Kalibrasyon olasiliklari asagi cektigi icin 0,5 esiginde daha az satir pozitif
isaretleniyor; Polly ve ShareX'te Platt'in F1'i belirgin dusuyor (0,4211 → 0,1538 ve
0,1136 → 0,0516). Bu beklenen bir yan etki: kalibrasyonun amaci olasiligi duzeltmek,
sabit bir esikteki F1'i buyutmek degil.

## 4. Guvenilirlik diyagramlari

`docs/olcumler/grafikler/` altinda dort SVG: `asama5-kalibrasyon-polly.svg`,
`-sharex.svg`, `-jellyfin.svg`, `-mikro.svg`.

Her grafikte ideal `y = x` kesikli cizgisi, ham/Platt/isotonic egrileri, X ekseninde
ortalama tahmin olasiligi, Y ekseninde gozlenen pozitif oran var. **Bos kutular nokta
olarak cizilmiyor** ama satir sayilari `calibration-results.json` icinde duruyor.

SVG'lerdeki nokta koordinatlari JSON'a karsi programatik dogrulandi: Polly 20, ShareX 23,
Jellyfin 29, mikro 29 nokta; eslesmeyen 0, eksen disina tasan 0. Dordu de goruntulenip
kirpilma, tasma ve okunmayan yazi olmadigi kontrol edildi.

Mikro grafikte uc egri de kosegenin **altinda** kaliyor, yani tahmin gozlenen orandan
yuksek. Polly grafigi belirgin basamakli ve zikzakli; o kumede 10 pozitif var.

## 5. Eslesmis dairesel hareketli blok bootstrap

Bu analiz **Adim 3'un tam-train modelini** `LinesAdded` tabaniyla karsilastiriyor. Model
yeniden egitilmedi; mevcut `model-predictions.csv` ve dondurulmus esikler (126 / 35 / 25)
kullanildi.

2000 tekrar, ana tohum 20260912, blok uzunlugu `ceil(sqrt(N))`: Polly 29, ShareX 51,
Jellyfin 83.

| Kume | Delta F1 ort | p2,5 | medyan | p97,5 | N/A |
|---|---|---|---|---|---|
| Polly | -0,0106 | -0,1241 | -0,0115 | 0,1140 | 2 |
| ShareX | 0,0349 | -0,0055 | 0,0347 | 0,0775 | 0 |
| Jellyfin | 0,0085 | -0,0088 | 0,0085 | 0,0257 | 0 |
| **Mikro** | **0,0655** | **0,0485** | 0,0653 | 0,0823 | 0 |
| Makro | 0,0109 | -0,0296 | 0,0109 | 0,0563 | 2 |

| Kume | Delta PR-AUC ort | p2,5 | medyan | p97,5 | N/A |
|---|---|---|---|---|---|
| Polly | 0,0469 | -0,1193 | 0,0367 | 0,2949 | 2 |
| ShareX | 0,0199 | -0,0075 | 0,0201 | 0,0453 | 0 |
| Jellyfin | **0,0531** | **0,0329** | 0,0531 | 0,0729 | 0 |
| **Mikro** | **0,2158** | **0,1818** | 0,2157 | 0,2513 | 0 |
| Makro | 0,0400 | -0,0155 | 0,0366 | 0,1216 | 2 |

Gecerli tekrar 2000; Polly'de 2 tekrarda ve makro toplamda 2 tekrarda deger **N/A**
cikti (o orneklemlerde hic pozitif kalmadi).

### Yorum

- **Mikro delta F1: bootstrap araligi tabanin ustunde** (p2,5 = 0,0485 > 0).
- **Mikro delta PR-AUC: bootstrap araligi tabanin ustunde** (p2,5 = 0,1818 > 0).
- **Makro delta F1: nokta tahmini ustunde, aralik fark yok degerini iceriyor**
  (0,0109; [-0,0296, 0,0563]).
- **Makro delta PR-AUC: nokta tahmini ustunde, aralik fark yok degerini iceriyor**
  (0,0400; [-0,0155, 0,1216]).
- **Jellyfin delta PR-AUC: bootstrap araligi tabanin ustunde** (p2,5 = 0,0329 > 0).
- Polly, ShareX ve Jellyfin'in delta F1 araliklari ve Polly ile ShareX'in delta PR-AUC
  araliklari **fark yok degerini iceriyor**.
- Polly'de delta F1'in nokta tahmini **tabanin altinda** (-0,0106) ve aralik sifiri
  iceriyor.

**Bu bootstrap model egitim belirsizligini kapsamaz; yalnizca sabit tahminler uzerindeki
zamansal test ornekleme belirsizligini olcer.**

## 6. Beklenti karsilastirmasi

`asama5-kalibrasyon-beklenti.md` **degistirilmedi**.

| # | Beklenti | Olculen | Sonuc |
|---|---|---|---|
| 1 | Mikro ECE dusecek | 0,0809 → Platt 0,0738, isotonic 0,0699 | **tuttu** |
| 2 | Mikro Brier dusecek | 0,0843 → Platt 0,0838, isotonic 0,0833 | **tuttu** |
| 3 | Polly isotonic daha basamakli/kararsiz | 266 blok, grafikte zikzakli, 0,5 esiginde F1 0,4545 | **tuttu** (nitel) |
| 4 | Platt PR-AUC siralamasini degistirmemeli | repo bazinda uc repoda da ayni; mikroda 0,4742 → 0,4770 | **kismen tuttu** |
| 5 | Isotonic PR-AUC'yi bir miktar degistirebilir | +0,0137 / +0,0037 / -0,0027 | **tuttu** |
| 8 | Makro F1 farkinin araligi sifiri icerecek | [-0,0296, 0,0563] | **tuttu** |
| 9 | Mikro PR-AUC farkinin araligi sifirin ustunde kalacak | [0,1818, 0,2513] | **tuttu** |
| 10 | Mikro F1 farki araligi sifirin ustunde | [0,0485, 0,0823] | **tuttu** |
| 11 | Polly araliklari genis olacak | delta F1 [-0,1241, 0,1140], genislik 0,2381 | **tuttu** |

4. madde icin "kismen": beklenti Platt'in siralamayi degistirmemesiydi ve repo bazinda
oyle oldu, ama mikro havuzda deger degisti. **Bunu olcumden once yazmadim.** Sebep
yukarida: mikro havuz uc reponun satirlarini birlestiriyor ve her repo kendi Platt
donusumunu goruyor, yani repolar arasindaki sira degisebiliyor. Beklentiyi yazarken
mikro havuzun bu ozelligini dusunmemistim.

## 7. Bilinen sinirliliklar

- **Azaltilmis-egitim modeli Adim 3'un modeli degil.** Ham Brier 0,0843 (bu adim) ile
  0,0819 (Adim 3, tam-train) arasindaki fark kalibrasyonun degil, egitim verisi
  miktarinin farki. Ikisi "kalibrasyon etkisi" olarak karsilastirilmadi.
- **Kalibrasyon Jellyfin'de karisik sonuc verdi** ve bu gizlenmedi.
- **Polly'nin calibration bolumunde 25 pozitif var.** Hem Platt hem isotonic bu 25 satira
  dayaniyor.
- **Bootstrap tahminleri sabit kabul ediyor.** Model yeniden egitilmedigi icin egitim
  belirsizligi olculmedi; gercek belirsizlik buradaki araliklardan genis olabilir.
- **0,5 esigindeki F1 kalibrasyon sonrasi dustu** (Polly ve ShareX'te belirgin). Bu
  beklenen bir yan etki ama F1 acisindan bir kayip ve yaziliyor.
- **Sonradan kalibrasyon yalnizca bu deneyde var.** Adim 3'un ham sonuclari silinmedi ve
  yerine kalibre sonuc yazilmadi.
