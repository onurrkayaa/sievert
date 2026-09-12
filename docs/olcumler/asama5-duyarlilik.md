# Adim 4: duyarlilik deneyleri

**Tarih:** 2026-09-12
**Durum:** Alti deney kosuldu. Ana model, ana bolme, ana test kumesi ve dondurulmus
dosyalar **degistirilmedi**. Her deney ayri bir "ya soyle olsaydi" hesabi.

## 0. Veri ve checksum

Girdiler: `commit-metrics.csv` (`1b8e5a5c…`), `split-manifest.csv` (`01b5cafa…`),
`model-predictions.csv` (`21b69877…`). Ucu de adimin basinda ve sonunda dogrulandi.

Uretilen: `data/asama5/sensitivity-results.json`, ozeti
`9b0c4706c7f69bc6ea167671a920e758a7551d3c5e131257b24164e327915266`. Ureten commit
`403b270`. Ayri klasor: `data/asama5/sensitivity/rename/`.

Botsuz yeniden egitim, ablasyon, C# alt kumesi ve sentetik gurultu deneylerinde esik
**her zaman o deneyin kendi egitim alt kumesinde** secildi; hedef test etiketleri secime
girmedi.

## 1. Bot duyarliligi

Uc politika: **A** ana sonuc (botlar train ve testte var), **B** yalniz degerlendirme
filtresi (ana model degismiyor, testten `BotMu = 1` cikariliyor), **C** bot haric yeniden
egitim (donusum, model ve esik bot olmayan train'de).

| Repo | Train bot | Test bot |
|---|---|---|
| Polly | 297 / 1931 (%15,4) | **557 / 828 (%67,3)** |
| ShareX | 0 / 5943 | 0 / 2547 |
| Jellyfin | 107 / 16 041 (%0,7) | 821 / 6876 (%11,9) |

Polly'nin **test** bolumunde bot orani %67,3, yani butun tarihteki %31,0'in cok
uzerinde: dependabot etkinligi son yillarda yogunlasmis.

| Repo | Politika | N | Pozitif | TP | FP | FN | TN | Precision | Recall | F1 | PR-AUC | Brier | ECE |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Polly | A | 828 | 10 | 7 | 39 | 3 | 779 | 0,1522 (7 / 46) | 0,7000 (7 / 10) | 0,2500 | 0,3033 | 0,0132 | 0,0292 |
| Polly | B | 271 | 10 | 7 | 39 | 3 | 222 | 0,1522 (7 / 46) | 0,7000 (7 / 10) | 0,2500 | 0,3044 | 0,0398 | 0,0664 |
| Polly | C | 271 | 10 | 7 | 39 | 3 | 222 | 0,1522 (7 / 46) | 0,7000 (7 / 10) | 0,2500 | 0,3046 | 0,0388 | 0,0690 |
| ShareX | A | 2547 | 129 | 44 | 298 | 85 | 2120 | 0,1287 (44 / 342) | 0,3411 (44 / 129) | 0,1868 | 0,1192 | 0,0559 | 0,0625 |
| ShareX | B | 2547 | 129 | 44 | 298 | 85 | 2120 | 0,1287 (44 / 342) | 0,3411 (44 / 129) | 0,1868 | 0,1192 | 0,0559 | 0,0625 |
| ShareX | C | 2547 | 129 | 44 | 298 | 85 | 2120 | 0,1287 (44 / 342) | 0,3411 (44 / 129) | 0,1868 | 0,1192 | 0,0559 | 0,0625 |
| Jellyfin | A | 6876 | 927 | 654 | 1091 | 273 | 4858 | 0,3748 (654 / 1745) | 0,7055 (654 / 927) | 0,4895 | 0,5199 | 0,0998 | 0,0810 |
| Jellyfin | B | 6055 | 926 | 654 | 1090 | 272 | 4039 | 0,3750 (654 / 1744) | 0,7063 (654 / 926) | 0,4899 | 0,5204 | 0,1131 | 0,0892 |
| Jellyfin | C | 6055 | 926 | 654 | 1089 | 272 | 4040 | 0,3752 (654 / 1743) | 0,7063 (654 / 926) | 0,4901 | 0,5205 | 0,1132 | 0,0897 |

**B ile C arasindaki fark cok kucuk.** Polly'de F1 ayni (0,2500), PR-AUC 0,3044 → 0,3046.
Jellyfin'de F1 0,4899 → 0,4901, PR-AUC 0,5204 → 0,5205 ve tek bir satirin tahmini
degisiyor (FP 1090 → 1089). ShareX'te hicbir sey degismiyor, cunku bot yok.

**Botlari cikarmak Polly'de 557 satir goturuyor ama 0 pozitif:** Polly'nin 10 test
pozitifinin hepsi bot olmayan commit'ler. Ayni sebeple TP/FP/FN sayilari A ile B'de birebir
ayni; degisen yalnizca TN (779 → 222) ve ona bagli olarak Brier (0,0132 → 0,0398) ve ECE
(0,0292 → 0,0664). Yani **bot satirlari cikarilinca Polly'nin kalibrasyonu kotu
gorunuyor**: kolay negatifler gidince ortalama hata yukseliyor.

Botlar ana snapshot'tan **silinmedi** ve bot haric tutma karari tek bir metrige bakilarak
verilmedi.

## 2. Sag sansur: en az 90 gun olgun alt kume

Ana model ve esik degismedi; yeniden egitim yok. `MaturityDays >= 90` olan test satirlari
secildi.

| Repo | Ana N | Ana pozitif | Olgun N | Olgun pozitif | Cikarilan N | Cikarilan pozitif |
|---|---|---|---|---|---|---|
| Polly | 828 | 10 (%1,21) | 729 | 10 (%1,37) | 99 | **0** |
| ShareX | 2547 | 129 (%5,06) | 1864 | 116 (%6,22) | 683 | 13 |
| Jellyfin | 6876 | 927 (%13,48) | 6317 | 887 (%14,04) | 559 | 40 |

**Uc repoda da olgun alt kumenin pozitif orani ana testten yuksek.**

| Repo | Kume | TP | FP | FN | TN | Precision | Recall | F1 | PR-AUC | Brier | ECE | Ort tahmin |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Polly | ana | 7 | 39 | 3 | 779 | 0,1522 (7 / 46) | 0,7000 (7 / 10) | 0,2500 | 0,3033 | 0,0132 | 0,0292 | 0,0405 |
| Polly | olgun | 7 | 38 | 3 | 681 | 0,1556 (7 / 45) | 0,7000 (7 / 10) | 0,2545 | 0,3053 | 0,0147 | 0,0301 | 0,0430 |
| ShareX | ana | 44 | 298 | 85 | 2120 | 0,1287 (44 / 342) | 0,3411 (44 / 129) | 0,1868 | 0,1192 | 0,0559 | 0,0625 | 0,1131 |
| ShareX | olgun | 39 | 182 | 77 | 1566 | 0,1765 (39 / 221) | 0,3362 (39 / 116) | 0,2315 | 0,1535 | 0,0606 | 0,0463 | 0,1085 |
| Jellyfin | ana | 654 | 1091 | 273 | 4858 | 0,3748 (654 / 1745) | 0,7055 (654 / 927) | 0,4895 | 0,5199 | 0,0998 | 0,0810 | 0,2158 |
| Jellyfin | olgun | 623 | 935 | 264 | 4495 | 0,3999 (623 / 1558) | 0,7024 (623 / 887) | 0,5096 | 0,5508 | 0,0963 | 0,0705 | 0,2109 |

**Uc repoda da olgun alt kumede F1 ve PR-AUC ana testten yuksek** (F1 +0,0045 / +0,0447 /
+0,0201; PR-AUC +0,0020 / +0,0343 / +0,0309). ECE ShareX ve Jellyfin'de dusuyor.

**Bu analiz:** ana test sonucunun yerine gecmez; sansurlu satirlarin gercekte negatif
oldugunu **kanitlamaz**; yalnizca en az 90 gun gozlenmis alt kumedeki sonucu gosterir.
Polly'de cikarilan 99 satirin 0 pozitifi olmasi, o satirlarin hata getirmedigini degil,
onlari suclayacak bir duzeltmenin veri kumesinin kapsadigi sure icinde gorulmedigini
soyluyor.

## 3. C# etiket uygunlugu ve `CsFilesChanged`

### Mekanik bag

| Repo | `CsFilesChanged = 0` | Pozitif | `CsFilesChanged > 0` | Pozitif |
|---|---|---|---|---|
| Polly | 1628 | **0** | 1131 | 261 |
| ShareX | 1310 | **0** | 7180 | 1013 |
| Jellyfin | 5669 | **0** | 17 248 | 4688 |

**Uc repoda da hic `.cs` dosyasi degistirmeyen commit'lerin pozitif etiket sayisi tam
olarak 0.** Toplamda 8607 commit (34 166'nin %25,2'si) yapisal olarak pozitif etiket
alamiyor. Sebep etiketin uretim bicimi: SZZ yalnizca silinen `.cs` satirlarini blame
ediyor (ADR 0014). Bu bir mekanik bag, veriden cikan bir bulgu degil.

### Uc analiz

| Repo | Analiz | N | Pozitif | TP | FP | FN | TN | Precision | Recall | F1 | PR-AUC |
|---|---|---|---|---|---|---|---|---|---|---|---|
| Polly | A ana model | 828 | 10 | 7 | 39 | 3 | 779 | 0,1522 (7 / 46) | 0,7000 (7 / 10) | 0,2500 | 0,3033 |
| Polly | B ablasyon | 828 | 10 | 6 | 30 | 4 | 788 | 0,1667 (6 / 36) | 0,6000 (6 / 10) | 0,2609 | 0,3194 |
| Polly | C yalniz C# | 151 | 10 | 7 | 36 | 3 | 105 | 0,1628 (7 / 43) | 0,7000 (7 / 10) | 0,2642 | 0,2841 |
| ShareX | A | 2547 | 129 | 44 | 298 | 85 | 2120 | 0,1287 (44 / 342) | 0,3411 (44 / 129) | 0,1868 | 0,1192 |
| ShareX | B | 2547 | 129 | 56 | 447 | 73 | 1971 | 0,1113 (56 / 503) | 0,4341 (56 / 129) | 0,1772 | 0,1023 |
| ShareX | C | 2011 | 129 | 47 | 340 | 82 | 1542 | 0,1214 (47 / 387) | 0,3643 (47 / 129) | 0,1822 | 0,1178 |
| Jellyfin | A | 6876 | 927 | 654 | 1091 | 273 | 4858 | 0,3748 (654 / 1745) | 0,7055 (654 / 927) | 0,4895 | 0,5199 |
| Jellyfin | B | 6876 | 927 | 708 | 1424 | 219 | 4525 | 0,3321 (708 / 2132) | 0,7638 (708 / 927) | 0,4629 | 0,4941 |
| Jellyfin | C | 4222 | 927 | 670 | 1185 | 257 | 2110 | 0,3612 (670 / 1855) | 0,7228 (670 / 927) | 0,4817 | 0,5202 |

**Ablasyon (B) sonucu repoya gore degisiyor:** Jellyfin'de F1 -0,0266 ve PR-AUC -0,0258
dustu, ShareX'te F1 -0,0096 ve PR-AUC -0,0169 dustu, **Polly'de yukseldi** (F1 +0,0109,
PR-AUC +0,0161). Yani `CsFilesChanged` kaldirilinca performansin dusecegi beklentisi uc
repodan ikisinde tuttu, birinde tutmadi.

**Yalniz C# commit'lerinde (C)** PR-AUC iki repoda ana modelin altinda (Polly 0,2841 /
0,3033; ShareX 0,1178 / 0,1192), Jellyfin'de neredeyse ayni (0,5202 / 0,5199). F1 uc
repoda da ana modele yakin.

Ayni alt kumede `LinesAdded` tabani da train'de yeniden secilip testte hesaplandi:

| Repo | Esik | TP | FP | FN | F1 | PR-AUC |
|---|---|---|---|---|---|---|
| Polly | 107 | 7 | 30 | 3 | 0,2979 | 0,2750 |
| ShareX | 35 | 87 | 805 | 42 | 0,1704 | 0,1247 |
| Jellyfin | 25 | 593 | 795 | 334 | 0,5123 | 0,5015 |

**C# alt kumesinde model tabani F1'de iki repoda geciyor** (ShareX +0,0118, Jellyfin
-0,0306 → geride; Polly 0,2642 - 0,2979 = -0,0337 → geride). Yani C# alt kumesinde model
F1 acisindan uc repodan ikisinde `LinesAdded` tabanini **geride kaldi**. PR-AUC'de model
uc repoda da onde (0,2841 / 0,2750, 0,1178 / 0,1247 → geride, 0,5202 / 0,5015).

Duzeltme: PR-AUC'de model Polly'de (+0,0091) ve Jellyfin'de (+0,0187) onde, ShareX'te
(-0,0069) geride.

### Ne kanitlamiyor

- `CsFilesChanged` guclu katsayi cikti diye **nedensellik iddiasi kurulmuyor**.
- Ablasyon dususu hem gercek sinyalden hem etiketleme kapsamindan gelebilir; **bu deney
  iki kaynagi tamamen ayiramaz**.
- C# alt kumesinde degerlendirmenin daha zor olmasi bekleniyordu; PR-AUC'de iki repoda
  hafif dustu, F1'de dusmedi. Karisik sonuc.

## 4. Train araligi disi degerler

Ana model donusumleri kullanildi. Kirpma yok, yeniden egitim yok, oznitelik cikarma yok.

| Repo | Aralik disi satir | Aralik ici satir |
|---|---|---|
| Polly | 620 / 828 (%74,9) | 208 |
| ShareX | 2411 / 2547 (%94,7) | 136 |
| Jellyfin | 2308 / 6876 (%33,6) | 4568 |

En cok tasan oznitelikler (hepsi **ust** siniri asiyor; alt sinirin altinda kalan deger uc
repoda da 0):

| Repo | Oznitelik | Ustunde | Testin yuzdesi |
|---|---|---|---|
| Polly | `AuthorCommitCount` | 591 | %71,4 |
| Polly | `MaxFileAgeDays` | 42 | %5,1 |
| ShareX | `AuthorCommitCount` | 2306 | %90,5 |
| ShareX | `MaxFileAgeDays` | 1253 | %49,2 |
| ShareX | `MinFileAgeDays` | 468 | %18,4 |
| Jellyfin | `MaxFileAgeDays` | 2296 | %33,4 |
| Jellyfin | `MinFileAgeDays` | 1266 | %18,4 |

Butun tasmalar tek yonlu ve aciklamasi yapisal: `AuthorCommitCount`, `MaxFileAgeDays` ve
`MinFileAgeDays` zamanla **buyuyen** olculer, yani test bolumunun degerleri egitim
bolumunun gordugu en buyuk degerin uzerine cikiyor.

| Repo | Grup | N | Pozitif | F1 | PR-AUC | Brier | ECE |
|---|---|---|---|---|---|---|---|
| Polly | aralik disi | 620 | 1 (%0,16) | 0,1333 | 0,2500 | 0,0048 | 0,0226 |
| Polly | aralik ici | 208 | 9 (%4,33) | 0,2927 | 0,3570 | 0,0382 | 0,0534 |
| ShareX | aralik disi | 2411 | 120 (%4,98) | 0,1854 | 0,1161 | 0,0559 | 0,0651 |
| ShareX | aralik ici | 136 | 9 (%6,62) | 0,2222 | 0,2293 | 0,0555 | 0,0466 |
| Jellyfin | aralik disi | 2308 | 436 (%18,89) | 0,5057 | 0,6099 | 0,1453 | 0,1626 |
| Jellyfin | aralik ici | 4568 | 491 (%10,75) | 0,4691 | 0,4534 | 0,0769 | 0,0398 |

**Iki grubun pozitif orani farkli ve yonu repoya gore degisiyor:** Polly ve ShareX'te
aralik disi grubun orani daha dusuk, Jellyfin'de daha yuksek.

**Bu analiz neden-sonuc kanitlamaz.** Aralik disi olmak ile sonucun kotu ya da iyi olmasi
arasinda bir bag gosterilmedi; iki grup baska bakimlardan da (tarih, boyut, taban oran)
farkli.

## 5. Ad degisimi duyarliligi

Iki kavram ayri tutuldu ve karistirilmadi:

1. **Zincir:** `OldPath` zinciri takip edilsin mi (`MetricOptions.FollowRenames`). Git'e
   gitmeden ayni diff'lerden hesaplanabiliyor.
2. **Esik:** git'in ad degisimi benzerlik esigi (40 / 50 / 60). Git tarihinin farkli bir
   esikle yeniden okunmasini gerektiriyor.

Esik degerleri **sonuc gormeden** sabitlendi: 40, 50 (ana), 60. Ayni uc repo hash'i, ayni
birlestirme ve tarih politikasi. Etiketler dondurulmus hedeften SHA ile baglandi. Ana
snapshot ve veritabani **degismedi**; cikti `data/asama5/sensitivity/rename/` altinda.

**Tahmini sure, kosudan once yazildi:** ana madencilik 50 esiginde uc repoda toplam 176 sn
surmustu; uc esik x uc repo icin yaklasik 9 dakikalik git okumasi bekliyordum.

**Olculen sure:** git okumasi Polly'de 4,9 - 6,5 sn, Jellyfin'de 62,1 - 72,8 sn,
ShareX'te 102,2 - 151,3 sn (esik basina). Toplam yaklasik 9,5 dakika, tahminle uyumlu.

Cikti: `data/asama5/sensitivity/rename/rename-results.json`, ozeti
`b412b262d188cb24c99e7883ae7b90963a49f074a8f11095f166cb7fe03af034`.

### Zincir etkisi (esik 50, ayni diff'ler)

| Repo | Etkilenen commit | `PriorChanges` farkli | En buyuk fark | Toplam (takipli / takipsiz) |
|---|---|---|---|---|
| Polly | 1234 / 2759 (%44,7) | 1233 | 3047 | 361 826 / 263 506 |
| ShareX | 3978 / 8490 (%46,9) | 3978 | 2669 | 2 500 811 / 2 183 940 |
| Jellyfin | 13 340 / 22 917 (%58,2) | 13 305 | 8682 | 8 123 060 / 6 421 480 |

| Repo | `MaxFileAgeDays` farkli | En buyuk fark (gun) | Toplam (takipli / takipsiz) |
|---|---|---|---|
| Polly | 875 | 3636 | 3 009 436 / 2 494 802 |
| ShareX | 2009 | 4590 | 11 243 593 / 10 163 122 |
| Jellyfin | 10 585 | 4448 | 31 675 274 / 26 567 001 |

**Bagimsiz kontrol:** bu aracin Polly icin urettigi sayilar `asama4-ad-degisimi.md`
icindeki sayilarla **birebir ayni** (1234 etkilenen commit, `PriorChanges` 1233 farkli ve
en buyuk 3047, `MaxFileAgeDays` 875 farkli ve en buyuk 3636, toplamlar 361 826 / 263 506 ve
3 009 436 / 2 494 802). Arac git'i bagimsiz okudugu icin bu, hem araci hem Asama 4'un
sayilarini dogruluyor.

Zincir etkisi uc repoda da genis: ad degisimi iceren commit orani kucuk olsa da
metrikleri etkilenen commit orani %44,7 - %58,2.

### Esik duyarliligi (40 / 50 / 60)

| Repo | Esik | Bulunan ad degisimi | 50'ye gore metrigi degisen commit |
|---|---|---|---|
| Polly | 40 | 1045 | 197 |
| Polly | 50 | 1029 | 0 |
| Polly | 60 | 1006 | 109 |
| ShareX | 40 | 1783 | 598 |
| ShareX | 50 | 1740 | 0 |
| ShareX | 60 | 1709 | 245 |
| Jellyfin | 40 | 3586 | 2757 |
| Jellyfin | 50 | 3413 | 0 |
| Jellyfin | 60 | 3301 | 2514 |

| Repo | Esik | Secilen olasilik esigi | TP | FP | FN | TN | F1 | PR-AUC | Brier | ECE |
|---|---|---|---|---|---|---|---|---|---|---|
| Polly | 40 | 0,2432 | 7 | 38 | 3 | 780 | 0,2545 | 0,2906 | 0,0132 | 0,0306 |
| Polly | 50 | 0,2311 | 7 | 40 | 3 | 778 | 0,2456 | 0,3015 | 0,0132 | 0,0306 |
| Polly | 60 | 0,2342 | 7 | 40 | 3 | 778 | 0,2456 | 0,3028 | 0,0131 | 0,0291 |
| ShareX | 40 | 0,2076 | 46 | 326 | 83 | 2092 | 0,1836 | 0,1187 | 0,0559 | 0,0624 |
| ShareX | 50 | 0,2170 | 44 | 297 | 85 | 2121 | 0,1872 | 0,1192 | 0,0559 | 0,0625 |
| ShareX | 60 | 0,2168 | 43 | 300 | 86 | 2118 | 0,1822 | 0,1197 | 0,0559 | 0,0628 |
| Jellyfin | 40 | 0,3191 | 654 | 1091 | 273 | 4858 | 0,4895 | 0,5197 | 0,0998 | 0,0809 |
| Jellyfin | 50 | 0,3198 | 654 | 1091 | 273 | 4858 | 0,4895 | 0,5200 | 0,0999 | 0,0810 |
| Jellyfin | 60 | 0,3195 | 653 | 1088 | 274 | 4861 | 0,4895 | 0,5202 | 0,0999 | 0,0811 |

**Esik degisikligi ad degisimi sayisini ve tarihsel metrikleri belirgin degistiriyor ama
model sonucunu cok az degistiriyor.** Jellyfin'de 40 ile 60 arasinda 2757 ve 2514 commit'in
metrigi degisiyor, F1 ise uc esikte de 0,4895. En buyuk F1 farki ShareX'te: 0,1836 ile
0,1872 arasinda 0,0036.

**Sinir:** bu aracin commit sirasi tarih + SHA ordinal, ana boru hattinin sirasi tarih +
satir kimligi. Uc esik de bu arac icinde ayni kurali kullandigi icin **esikler arasi
karsilastirma gecerli**, ama bu aracin 50 esigindeki sayilari ana sonucla birebir ayni
degil: Polly 0,2456 (ana 0,2500), ShareX 0,1872 (ana 0,1868), Jellyfin 0,4895 (ana 0,4895).
Farklar 0,0044 / 0,0004 / 0,0000.

**Ne kanitlamiyor:** hangi esigin **dogru** oldugu. Asama 4'te yazildigi gibi, bunun icin
ad degisimlerinden bir orneklem alip kaynak koda bakarak dogru/yanlis siniflandirmak
gerekir ve o olcum **yapilmadi**.

## 6. Sentetik tek yonlu etiket gurultusu

**Bu bir etiket duzeltmesi degil, stress testidir.** Asama 4'un 11 / 13 (%84,6) orani
hedefli secilmis bir orneklemden geldi ve populasyona genellenemez; bu deneyde
**kullanilmadi**.

Aday gizli pozitifler: mevcut etiketi negatif **ve** `CsFilesChanged > 0` olan satirlar.
Her oran icin 100 tekrar, ana tohum 20260912, tekrar tohumu ana tohum + tekrar numarasi.
Her tekrarda model yeniden egitildi, donusum ve esik yalniz sentetik train'de secildi,
degerlendirme sentetik test etiketinde yapildi. **Orijinal snapshot degismedi.**

| Repo | Aday (train / test) | Oran | F1 ort | F1 p2,5 - p97,5 | PR-AUC ort | Brier ort | ECE ort | Esik ort | Sentetik test pozitifi ort |
|---|---|---|---|---|---|---|---|---|---|
| Polly | 729 / 141 | %5 | 0,2707 | 0,2051 - 0,3415 | 0,2776 | 0,0207 | 0,0310 | 0,2658 | 16,9 |
| Polly | 729 / 141 | %10 | 0,2984 | 0,2121 - 0,3953 | 0,2903 | 0,0274 | 0,0336 | 0,2844 | 23,5 |
| Polly | 729 / 141 | %20 | 0,3882 | 0,3036 - 0,4776 | 0,3519 | 0,0397 | 0,0405 | 0,2551 | 38,4 |
| ShareX | 4285 / 1882 | %5 | 0,2250 | 0,2015 - 0,2462 | 0,1558 | 0,0856 | 0,0602 | 0,2086 | 221,9 |
| ShareX | 4285 / 1882 | %10 | 0,2734 | 0,2456 - 0,3015 | 0,1967 | 0,1126 | 0,0574 | 0,2237 | 315,5 |
| ShareX | 4285 / 1882 | %20 | 0,3850 | 0,3602 - 0,4130 | 0,2807 | 0,1579 | 0,0556 | 0,2312 | 504,1 |
| Jellyfin | 9265 / 3295 | %5 | 0,4923 | 0,4836 - 0,5006 | 0,5194 | 0,1125 | 0,0775 | 0,2930 | 1090,7 |
| Jellyfin | 9265 / 3295 | %10 | 0,5096 | 0,4984 - 0,5208 | 0,5277 | 0,1237 | 0,0747 | 0,2876 | 1254,8 |
| Jellyfin | 9265 / 3295 | %20 | 0,5535 | 0,5396 - 0,5665 | 0,5580 | 0,1414 | 0,0709 | 0,2764 | 1583,2 |

**Beklentinin tersi cikti.** Oran arttikca F1 ve PR-AUC **dusmedi, yukseldi**: Polly'de F1
0,2707 → 0,3882, ShareX'te 0,2250 → 0,3850, Jellyfin'de 0,4923 → 0,5535. Brier uc repoda
da yukseldi (kotulesti), ECE ShareX ve Jellyfin'de dustu.

**Nedeni olculmedi.** Akla gelen aciklama: sentetik pozitifler yalnizca
`CsFilesChanged > 0` satirlardan secildigi icin pozitif sinif, modelin zaten en guclu
kullandigi ozniteligin (`CsFilesChanged`) tanimladigi kumeye daha cok benziyor; ayrica
taban oran yukseliyor ve F1 taban orana duyarli. Bu bir tahmin; deney iki etkiyi
ayirmiyor.

**Acikca:** bu deney gercek etiketleri tahmin etmiyor; %5 / %10 / %20 **gercek kacirma
orani iddiasi degil**; yalnizca tek yonlu gizli pozitif varsayimina karsi model
metriklerinin hassasiyetini gosteriyor.

## 7. Beklenti karsilastirmasi

`asama5-duyarlilik-beklenti.md` **degistirilmedi**.

| # | Beklenti | Olculen | Sonuc |
|---|---|---|---|
| 1 | Botlari cikarmak Polly'yi en cok etkiler | Polly testinde 557 / 828 satir cikti, ShareX'te 0 | **tuttu** |
| 2 | B ile C farki kucuk olacak | F1 farki Polly 0,0000, Jellyfin 0,0002 | **tuttu** |
| 3 | Olgun alt kumede pozitif oran ana testten yuksek olabilir | uc repoda da yuksek (%1,37 / %6,22 / %14,04) | **tuttu** |
| 4 | 90 gunluk filtre en cok Polly'yi degistirir | F1 degisimi Polly +0,0045, ShareX +0,0447, Jellyfin +0,0201 | **TUTMADI** |
| 5 | `CsFilesChanged` kaldirilinca performans duser | Jellyfin ve ShareX'te dustu, Polly'de yukseldi | **kismen tuttu** |
| 6 | Iki kaynak tamamen ayrilamaz | ayrilmadi ve ayrildigi iddia edilmedi | **tuttu** (yontem karari) |
| 7 | Yalniz C# alt kumesinde degerlendirme daha zor olacak | PR-AUC iki repoda hafif dustu, F1 dusmedi | **TUTMADI** |
| 11 | Flip orani arttikca F1 ve PR-AUC bozulur | ucu de yukseldi | **TUTMADI** |
| 12 | Deney gercek kacirma orani iddiasi degil | oyle yazildi | **tuttu** (yontem karari) |

4. madde tutmadi: 90 gunluk filtre Polly'de en az degisikligi yaptigi, ShareX'te en
cogunu yaptigi olculdu. **Nedeni olculmedi;** Polly'de cikarilan 99 satirin 0 pozitifi
oldugu icin pay degismedi, ShareX'te 683 satir ve 13 pozitif cikti - ama bunun F1
degisimini ne kadar acikladigi hesaplanmadi.

7. ve 11. maddeler tutmadi ve sebepleri yukarida, **tahmin oldugu yazilarak** duruyor.

## 8. Bilinen sinirliliklar

- **Polly'nin test bolumunde 10 pozitif var** ve bot filtresi, olgunluk filtresi ve C#
  alt kumesi o 10 satiri hic degistirmiyor; degisen yalnizca negatif taraf. Polly'nin
  butun F1 sayilari bu 10 satira dayaniyor.
- **Hicbir deney guven araligi uretmiyor** (sentetik gurultu disinda, o da yalnizca
  sentetik rastgeleligin dagilimi).
- **Aralik disi analizi neden-sonuc gostermiyor.**
- **Ablasyon iki kaynagi ayiramiyor** ve ayirdigi iddia edilmiyor.
- **Sentetik gurultu tek yonlu**; iki yonlu gurultu (yanlis pozitiflerin negatife
  cevrilmesi) denenmedi.
- **Bot, olgunluk ve C# deneyleri birbirinden bagimsiz kosuldu**; birlesik etkileri
  olculmedi.
