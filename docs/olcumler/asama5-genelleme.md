# Adim 5: repo-arasi genelleme

**Tarih:** 2026-09-12
**Durum:** Alti tek kaynakli yon ve uc leave-one-repository-out deneyi kosuldu. Ana model,
ana bolme ve dondurulmus dosyalar **degistirilmedi**.

## 1. Veri, protokol ve checksum

Girdiler dondurulmus: `commit-metrics.csv` (`1b8e5a5c…`), `split-manifest.csv`
(`01b5cafa…`). Uretilenler:

| Dosya | SHA-256 |
|---|---|
| `generalization-results.json` | `515f561c797c22c296c07c1ca4e7c7412309a1f12896ee2b8d08dc3014159b89` |
| `generalization-predictions.csv` | `99e82ca974c75b32d91ecb87ee25e3a5063359ed53577b73720bd98e27f863ef` |

Ureten commit `6604d58`. Iki dosya da iki kez uretildi; CSV bayt bayt ayni, JSON
`codeCommit` disinda ayni.

> Duzeltme (Adim 7): bu tabloda `generalization-results.json` icin once
> `87506b05…` yaziliydi. O deger dosyanin ilk uretimine aitti; dosya determinizm
> kontrolu sirasinda `codeCommit` alani `6604d58` yazilarak yeniden uretildi ve
> commit edilen surumun ozeti `515f561c…`. Kapanis dogrulamasinda fark edildi ve
> tablo commit edilen dosyaya gore duzeltildi.

Tahmin dosyasi: **30 753 satir** (tek-kaynak 20 502 + iki-kaynak 10 251), deney + hedef +
SHA tekrari **0**. Hedef basina satir sayilari: tek-kaynakta Polly 1656, ShareX 5094,
Jellyfin 13 752; iki-kaynakta 828 / 2547 / 6876.

**Protokol (dokuz deneyin hepsinde ayni):** donusum yalniz kaynak train'de fit edildi,
model yalniz kaynak train'de egitildi, esik yalniz kaynak train'de secildi. Hedef test
hicbir fit islemine girmedi. Ayni 15 oznitelik, ayni trainer ve ayarlar (ADR 0018), sinif
agirligi yok, kalibrasyon yok. `Repository` ve `RepositoryIdentity` **oznitelik degil**.

Iki kaynakli deneylerde kaynak repolarin train satirlari **dogal sayilariyla** birlesti;
agirlik esitleme yapilmadi. Bu, buyuk reponun agirligini artiriyor: Jellyfin + ShareX
birlesiminde 21 984 satirin 16 041'i (%73,0) Jellyfin'den, Polly + Jellyfin birlesiminde
17 972 satirin 16 041'i (%89,3) Jellyfin'den, Polly + ShareX birlesiminde 7874 satirin
5943'u (%75,5) ShareX'ten.

## 2. Alti yonlu tek kaynak

Ayni-repo karsilastirmasi Adim 3'un sonuclariyla: Polly F1 0,2500 / PR-AUC 0,3033,
ShareX 0,1868 / 0,1192, Jellyfin 0,4895 / 0,5199.

| Kaynak | Hedef | Esik | TP | FP | FN | TN | Precision | Recall | F1 | PR-AUC |
|---|---|---|---|---|---|---|---|---|---|---|
| Polly | Jellyfin | 0,2381 | 442 | 447 | 485 | 5502 | 0,4972 (442 / 889) | 0,4768 (442 / 927) | 0,4868 | 0,4886 |
| Polly | ShareX | 0,2381 | 52 | 269 | 77 | 2149 | 0,1620 (52 / 321) | 0,4031 (52 / 129) | 0,2311 | 0,1312 |
| Jellyfin | Polly | 0,3200 | 10 | 62 | 0 | 756 | 0,1389 (10 / 72) | 1,0000 (10 / 10) | 0,2439 | 0,3202 |
| Jellyfin | ShareX | 0,3200 | 80 | 554 | 49 | 1864 | 0,1262 (80 / 634) | 0,6202 (80 / 129) | 0,2097 | 0,1457 |
| ShareX | Polly | 0,2169 | 6 | 16 | 4 | 802 | 0,2727 (6 / 22) | 0,6000 (6 / 10) | 0,3750 | 0,3166 |
| ShareX | Jellyfin | 0,2169 | 255 | 174 | 672 | 5775 | 0,5944 (255 / 429) | 0,2751 (255 / 927) | 0,3761 | 0,4941 |

### Ayni-repoya gore fark

| Kaynak → Hedef | ΔF1 | ΔPR-AUC | ΔBrier | ΔECE | Korunan F1 | Korunan PR-AUC |
|---|---|---|---|---|---|---|
| Polly → Jellyfin | -0,0027 | -0,0313 | -0,0082 | -0,0460 | 0,994 | 0,940 |
| Polly → ShareX | +0,0443 | +0,0120 | +0,0043 | -0,0068 | 1,237 | 1,101 |
| Jellyfin → Polly | -0,0061 | +0,0169 | +0,0190 | +0,0396 | 0,976 | 1,056 |
| Jellyfin → ShareX | +0,0229 | +0,0265 | +0,0452 | +0,1013 | 1,123 | 1,222 |
| ShareX → Polly | +0,1250 | +0,0133 | -0,0020 | +0,0084 | 1,500 | 1,044 |
| ShareX → Jellyfin | -0,1134 | -0,0258 | +0,0004 | -0,0172 | 0,768 | 0,950 |

"Korunan performans" = repo-arasi / ayni-repo. Payda uc hedefte de sifirdan farkli.
Bu bir ad; **nedensellik iddiasi degil**.

**Alti yonun ucunde F1 ayni-repo modelinin ustunde cikti:** Polly → ShareX (+0,0443),
Jellyfin → ShareX (+0,0229), ShareX → Polly (+0,1250). Ucunde altinda: Polly → Jellyfin
(-0,0027), Jellyfin → Polly (-0,0061), ShareX → Jellyfin (-0,1134).

Bu, ayni-repo sonucunun her zaman ust sinir olmadigini gosteriyor - ozellikle ayni-repo
sonucu zaten dusuk oldugunda (ShareX 0,1868, Polly 0,2500).

## 3. Uc leave-one-repository-out

| Kaynaklar | Hedef | Kaynak satir | Esik | TP | FP | FN | TN | Precision | Recall | F1 | PR-AUC |
|---|---|---|---|---|---|---|---|---|---|---|---|
| Jellyfin + ShareX | Polly | 4645 / 21 984 | 0,2814 | 9 | 58 | 1 | 760 | 0,1343 (9 / 67) | 0,9000 (9 / 10) | 0,2338 | 0,2923 |
| Polly + ShareX | Jellyfin | 1135 / 7874 | 0,2278 | 302 | 220 | 625 | 5729 | 0,5785 (302 / 522) | 0,3258 (302 / 927) | 0,4168 | 0,4953 |
| Polly + Jellyfin | ShareX | 4012 / 17 972 | 0,2965 | 82 | 561 | 47 | 1857 | 0,1275 (82 / 643) | 0,6357 (82 / 129) | 0,2124 | 0,1469 |

| Hedef | ΔF1 (ayni-repo) | ΔPR-AUC | Korunan F1 | Korunan PR-AUC | En iyi tek kaynak F1 | Farki |
|---|---|---|---|---|---|---|
| Polly | -0,0162 | -0,0110 | 0,935 | 0,964 | 0,3750 (ShareX) | -0,1412 |
| Jellyfin | -0,0727 | -0,0246 | 0,852 | 0,953 | 0,4868 (Polly) | -0,0700 |
| ShareX | +0,0256 | +0,0277 | 1,137 | 1,232 | 0,2311 (Polly) | -0,0187 |

**Uc hedefte de iki kaynakli model, o hedefin en iyi tek kaynakli aktariminin altinda
kaldi** (-0,1412, -0,0700, -0,0187).

**Yayilim daha dar.** Korunan F1 oranlari: tek kaynakta 0,768 - 1,500 (genislik 0,732),
iki kaynakta 0,852 - 1,137 (genislik 0,285). Korunan PR-AUC: tek kaynakta
0,940 - 1,222 (0,282), iki kaynakta 0,953 - 1,232 (0,279).

## 4. Esik ve siralama ayrimi

| Kaynak → Hedef | F1 (kaynak esigi) | F1 (0,5) | Fark |
|---|---|---|---|
| Polly → Jellyfin | 0,4868 | 0,3581 | -0,1287 |
| Polly → ShareX | 0,2311 | 0,1161 | -0,1150 |
| Jellyfin → Polly | 0,2439 | 0,2963 | +0,0524 |
| Jellyfin → ShareX | 0,2097 | 0,2316 | +0,0219 |
| ShareX → Polly | 0,3750 | 0,4706 | +0,0956 |
| ShareX → Jellyfin | 0,3761 | 0,0785 | -0,2976 |
| Jellyfin + ShareX → Polly | 0,2338 | 0,2727 | +0,0389 |
| Polly + ShareX → Jellyfin | 0,4168 | 0,1461 | -0,2707 |
| Polly + Jellyfin → ShareX | 0,2124 | 0,2488 | +0,0364 |

**PR-AUC siralama aktarimini olcuyor; F1 hem siralamayi hem kaynakta secilen esigin hedef
taban oranina uyumunu birlikte olcuyor.**

Olculen ornek bu ayrimi gosteriyor: **ShareX → Jellyfin**'de PR-AUC 0,4941 (ayni-repo
0,5199'un %95'i) ama 0,5 esiginde F1 yalnizca 0,0785. Siralama buyuk olcude tasinmis,
sabit esik tasinmamis. Kaynak esigiyle (0,2169) ayni yon F1 0,3761 veriyor.

Bu, **esik / taban orani uyumsuzlugu ile uyumlu**, fakat **tek basina kanit degil**:
kaynak ve hedefin oznitelik dagilimlari da farkli ve bu deney iki etkiyi ayirmiyor.

Hedef test sonucuna bakarak yeni esik secilmedi, hedefte oracle esik hesaplanmadi, hedef
testle kalibrasyon yapilmadi.

## 5. Katsayi aktarimi

Katsayilar standartlastirilmis olcekte, ama **her model kendi kaynaginin normalizasyon
parametreleriyle** olculdu; bu yuzden ham buyuklukler kaynaklar arasinda dogrudan fark
olarak yorumlanmiyor. Isaret ve siralama daha guvenli karsilastirma.

| Model | 1 | 2 | 3 | `PriorFixes` |
|---|---|---|---|---|
| Polly (tek kaynak) | `CsFilesChanged` +2,078 | `FilesChanged` -1,504 | `LinesAdded` +1,093 | -0,4180 |
| Jellyfin (tek kaynak) | `FilesChanged` -1,640 | `CsFilesChanged` +1,492 | `LinesAdded` +1,040 | +0,4511 |
| ShareX (tek kaynak) | `CsFilesChanged` +0,970 | `FilesChanged` -0,737 | `LinesAdded` +0,601 | +0,0299 |
| Jellyfin + ShareX | `FilesChanged` -1,623 | `CsFilesChanged` +1,383 | `LinesAdded` +0,859 | +0,5923 |
| Polly + ShareX | `CsFilesChanged` +1,260 | `FilesChanged` -0,928 | `LinesAdded` +0,746 | -0,0421 |
| Polly + Jellyfin | `FilesChanged` -1,779 | `CsFilesChanged` +1,611 | `LinesAdded` +1,023 | +0,3959 |

**Ilk ucteki oznitelikler dokuz modelin hepsinde ayni:** `CsFilesChanged`, `FilesChanged`,
`LinesAdded`. Yalnizca ilk ikisinin sirasi degisiyor. Ortak oznitelik sayisi her model
cifti icin **3 / 3**.

**Isaretler de korunuyor:** `CsFilesChanged` her yerde pozitif, `FilesChanged` her yerde
negatif, `LinesAdded` her yerde pozitif.

**`PriorFixes`'in isareti degisiyor:** Polly'de -0,4180, Jellyfin'de +0,4511, ShareX'te
+0,0299; iki kaynakli modellerde +0,5923, -0,0421, +0,3959. Alti farkli kaynak
kombinasyonundan ikisinde negatif, dordunde pozitif. Bu, Adim 3'te yazilan uyariyla
tutarli: `PriorFixes`, `PriorChanges` ile yuksek korelasyonlu (ShareX 0,9336, Jellyfin
0,9222) ve boyle ciftlerde katsayinin isareti kararsiz olabiliyor.

**Nedensellik iddiasi kurulmuyor.** Bunlar modelde tasinan iliskiler; "su oznitelik hataya
neden olur" denmiyor.

## 5b. Betimsel ayristirma

Yeni model egitilmedi, yeni esik secilmedi, hedef testte kalibrasyon yapilmadi ve mevcut
genelleme sonucu degistirilmedi. Bu bolum yalnizca mevcut sayilari yan yana koyuyor.

Sonuc dosyasi `data/asama5/generalization-decomposition.json`, ozeti
`a56585d1adf33e07fe111b054c96c484dcad01cca15944867e301e550d52a622`.

**"Belirgin dusus" siniri sonuc gormeden sabitlendi:** mutlak F1 farki `<= -0,05`.

| Kaynak → Hedef | ΔPR-AUC | ΔF1 (kaynak esigi) | ΔF1 (0,5) | Ort tahmin | Hedef orani | Mutlak fark | Kaynak train orani | Kaynak − hedef |
|---|---|---|---|---|---|---|---|---|
| polly → jellyfin | -0.0313 | -0.0027 | -0.1393 | 0.1063 | 0.1348 | 0.0285 | 0.1300 | -0.0048 |
| polly → sharex | +0.0120 | +0.0443 | +0.0318 | 0.1064 | 0.0506 | 0.0557 | 0.1300 | +0.0793 |
| jellyfin → polly | +0.0169 | -0.0061 | -0.1743 | 0.0809 | 0.0121 | 0.0688 | 0.2345 | +0.2224 |
| jellyfin → sharex | +0.0265 | +0.0229 | +0.1473 | 0.2145 | 0.0506 | 0.1638 | 0.2345 | +0.1838 |
| sharex → polly | +0.0133 | +0.1250 | -0.0000 | 0.0488 | 0.0121 | 0.0367 | 0.1487 | +0.1367 |
| sharex → jellyfin | -0.0258 | -0.1134 | -0.4189 | 0.0710 | 0.1348 | 0.0638 | 0.1487 | +0.0139 |
| jellyfin+sharex → polly | -0.0110 | -0.0162 | -0.1979 | 0.0706 | 0.0121 | 0.0586 | 0.2113 | +0.1992 |
| polly+sharex → jellyfin | -0.0246 | -0.0727 | -0.3513 | 0.0782 | 0.1348 | 0.0566 | 0.1441 | +0.0093 |
| polly+jellyfin → sharex | +0.0277 | +0.0256 | +0.1645 | 0.2035 | 0.0506 | 0.1528 | 0.2232 | +0.1726 |

### Sayimlar

| Olcu | Sayi |
|---|---|
| PR-AUC korunup kaynak esigi F1'i belirgin dusen deney | **2 / 9** |
| 0,5 esigi kaynak esiginden iyi olan deney | **5 / 9** |
| Ortalama tahmin hedef orandan **yuksek** | **6 / 9** |
| Ortalama tahmin hedef orandan **dusuk** | **3 / 9** |
| Tek kaynakta ayni-repo F1'inin ustunde | **3 / 6** |
| Leave-one-out'ta ayni-repo F1'inin ustunde | **1 / 3** |

### Okuma

**PR-AUC korunup F1'i belirgin dusen iki deney var:** ShareX → Jellyfin (ΔPR-AUC -0,0258
ama ΔF1 -0,1134) ve Polly + ShareX → Jellyfin (ΔPR-AUC -0,0246 ama ΔF1 -0,0727). Ikisinde
de hedef Jellyfin ve kaynak, Jellyfin'den **dusuk** taban oranli bir kume.

**Ortalama tahmin ile hedef oran arasindaki fark en buyuk iki yerde**, hedefi ShareX olan
Jellyfin kaynakli deneylerde: Jellyfin → ShareX'te ortalama tahmin 0,2145, hedef oran
0,0506 (fark 0,1638); Polly + Jellyfin → ShareX'te 0,2035 / 0,0506 (fark 0,1528). Ayni iki
deneyde ECE de en yuksek (0,1638 ve 0,1528).

Bu, **esik ve taban orani uyumsuzluguyla uyumlu** bir tablo. Ama **neden kanitlamiyor**:
kaynak ve hedefin oznitelik dagilimlari da farkli ve bu ayristirma iki etkiyi ayirmiyor.

## 6. Beklenti karsilastirmasi

`asama5-genelleme-beklenti.md` **degistirilmedi**.

| # | Beklenti | Olculen | Sonuc |
|---|---|---|---|
| 1 | Repo-arasi F1 genel olarak ayni-repo F1'inin altinda | 9 deneyin 5'inde altinda, 4'unde ustunde | **TUTMADI** |
| 2 | Polly hedef oldugunda kararsiz | Polly hedefli dort deneyde F1 0,2338 - 0,3750 (genislik 0,1412) | **tuttu** |
| 3 | ShareX'in kaymasi aktarimi zorlastirabilir | ShareX hedefken korunan F1 1,123 - 1,237; ShareX kaynakken 0,768 ve 1,500 | **belirsiz** |
| 4 | Iki kaynakli model daha istikrarli | korunan F1 genisligi 0,285 (iki kaynak) / 0,732 (tek kaynak) | **tuttu** |
| 5 | PR-AUC F1'den daha tutarli aktarilir | korunan PR-AUC genisligi 0,282; korunan F1 0,732 | **tuttu** |
| 6 | Kaynak esigi precision ya da recall'u bozar | dokuz deneyin hepsinde F1 iki esikte farkli; en buyuk fark 0,2976 | **tuttu** |

1. madde tutmadi. `asama5-beklenti.md`'nin 7. maddesi de repo-arasi F1'in ayni-repo
F1'inin **yarisi ile dortte ucu arasina** dusmesini bekliyordu; olculen korunan oranlar
0,768 - 1,500, yani o bant da tutmadi.

**Nedeni olculmedi.** Akla gelen aciklama, ayni-repo sonuclarinin iki repoda zaten dusuk
olmasi (ShareX 0,1868, Polly 0,2500), yani asilmasi zor bir esik olmamasi; ayrica Polly'nin
hedef testinde 10 pozitif var ve o kumede F1 cok oynak. Ama bu bir tahmin, **olculmedi**.

3. madde icin "belirsiz": ShareX hedefken korunan F1 1'in ustunde, kaynakken bir yonde
0,768 bir yonde 1,500. Kayma etkisini bu deney tek basina ayiramiyor.

## 7. Bilinen sinirliliklar

- **Polly hedefli her sonuc 10 pozitife dayaniyor.** Jellyfin → Polly'de TP 10, FN 0, yani
  recall 1,0000 (10 / 10); bu, 10 satirlik bir kumeden geliyor ve saglamligi
  gosterilmedi.
- **Uc repo var, yani "repo-arasi" uc noktali bir gozlem.** Uc repoluk bir kumeden
  genel bir genelleme davranisi cikarilamaz.
- **Agirlik esitleme yok.** Iki kaynakli modellerde buyuk repo agir basiyor (Polly +
  Jellyfin'de %89,3 Jellyfin). Esitlenseydi sonuclarin ne olacagi **olculmedi**.
- **Tek kosu, guven araligi yok.** Dokuz modelin her biri bir kez egitildi; farklarin
  gurultuden buyuk olup olmadigi bu adimda sinanmadi.
- **Kalibrasyon uygulanmadi**, yani Brier/ECE degerleri ham olasiliklarin.
- **Katsayi buyuklukleri kaynaklar arasinda dogrudan karsilastirilmiyor**; her model kendi
  normalizasyon parametrelerini kullaniyor.
