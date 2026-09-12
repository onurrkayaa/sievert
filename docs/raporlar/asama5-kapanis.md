# Asama 5 kapanis

**Tarih:** 2026-09-12
**Kapsam:** Dondurulmus veri kumesinden baslayip kor insan dogrulamasina kadar.
**Yeni deney yok:** buradaki her sayi mevcut olcum dosyalarindan ve JSON'lardan derlendi.

## 1. Ne yapildi

1. **Dondurulmus veri kumesi** — 34 166 commit, uc repo, tek CSV ve SHA-256
   (`asama5-veri-kumesi.md`).
2. **Zaman sirali bolme** — her repo kendi icinde `floor(N·0,70)`, manifest dondurulmus
   (`asama5-bolme.md`, ADR 0016).
3. **Uc taban cizgisi** — her seye negatif, oranla rastgele, `LinesAdded` esigi
   (`asama5-taban.md`, ADR 0017).
4. **Lojistik regresyon** — repo basina ayri model, 15 oznitelik, kalibre edilmemis ham
   olasilik (`asama5-model.md`, ADR 0018).
5. **Kalibrasyon ve bootstrap** — Platt ve isotonic, eslesmis dairesel blok bootstrap
   (`asama5-kalibrasyon.md`, ADR 0019).
6. **Duyarlilik ve repo-arasi genelleme** — alti duyarlilik deneyi, dokuz aktarim modeli
   (`asama5-duyarlilik.md`, `asama5-genelleme.md`, ADR 0020, ADR 0021).
7. **Kor insan dogrulamasi** — 30 commit, dort kategori, anahtar kararlardan sonra acildi
   (`asama5-tahmin-dogrulama.md`, ADR 0022).

## 2. Veri ve bolme

Kaynak: `asama5-veri-kumesi.md`, `asama5-bolme.md`.
Snapshot `1b8e5a5c…4d46e95`, manifest `01b5cafa…601b8cb`.

| | Satir | Pozitif | Oran |
|---|---|---|---|
| Toplam | 34 166 | 5962 / 34 166 | %17,4 |
| Train | 23 915 | 4896 / 23 915 | %20,5 |
| Test | 10 251 | 1066 / 10 251 | %10,4 |

| Repo | Train pozitif | Test pozitif |
|---|---|---|
| Polly | 251 / 1931 (%13,0) | 10 / 828 (%1,21) |
| ShareX | 884 / 5943 (%14,9) | 129 / 2547 (%5,06) |
| Jellyfin | 3761 / 16 041 (%23,4) | 927 / 6876 (%13,48) |

**Test pozitif orani uc repoda da egitimden dusuk.** Sebep adaylarindan biri sag sansur:
bir commit ancak sonraki bir duzeltme onu suclarsa pozitif etiket aliyor ve test bolumu
her reponun en yeni commit'leri. Olculdu: Polly'nin testinde 180 gunden az olgun 207
commit var ve **hicbiri pozitif degil**.

**11 / 13 (%84,6) hakkinda:** Asama 4'un etiketsiz orneklemindeki kacirma orani
**hedefli** secilmis bir orneklemden geldi ("`IsFix` tarafindan dokunulmus fakat
etiketlenmemis" commit'lerden). Tum negatif populasyona **genellenmedi**, Asama 5'te
hicbir yerde duzeltme ya da agirlik olarak kullanilmadi.

## 3. Tabanlar

Kaynak: `asama5-taban.md`, `baseline-results.json` (`dd62daa7…f6f38d65`).

**Her seye negatif** (mikro): TP 0, FP 0, FN 1066, TN 9185 →
precision **N/A (0 / 0)**, recall **0,0000 (0 / 1066)**, F1 **0,0000**.

**Oranla rastgele** (1000 tekrar, tohum 20260912): mikro F1 ortalamasi **0,1502**, makro
F1 ortalamasi **0,0907**. Dagilimlarin tamami `asama5-taban.md` 4. bolumde.

**`LinesAdded` esigi** (mikro): F1 **0,3754**, makro F1 **0,2999**, mikro ham PR-AUC
**0,2611**.

| Repo | Esik | Test F1 | Ham PR-AUC |
|---|---|---|---|
| Polly | 126 | 0,2667 | 0,2610 |
| ShareX | 35 | 0,1520 | 0,0970 |
| Jellyfin | 25 | 0,4811 | 0,4663 |

Accuracy hicbir yerde hesaplanmadi (ADR 0017).

## 4. Ana lojistik regresyon

Kaynak: `asama5-model.md`, `model-results.json` (`cd3b3e3d…49a0b5`).
Microsoft.ML 5.0.0, `LbfgsLogisticRegression`, tohum 20260912, L1 = 0, L2 = 1,0.

Mikro (train'de secilen esikle): TP 705, FP 1428, FN 361, TN 7757 →
precision 0,3305 (705 / 2133), recall 0,6614 (705 / 1066), **F1 0,4408**.
**Makro F1 0,3088**, **mikro ham PR-AUC 0,4775**.

**Onceden ilan edilmis uc toplu kosulun 3 / 3'u saglandi:** mikro F1 0,4408 > 0,3754;
makro F1 0,3088 > 0,2999; mikro PR-AUC 0,4775 > 0,2611.

| Repo | Model F1 | Taban F1 | Model PR-AUC | Taban PR-AUC |
|---|---|---|---|---|
| Polly | **0,2500** | **0,2667** | 0,3033 | 0,2610 |
| ShareX | 0,1868 | 0,1520 | 0,1192 | 0,0970 |
| Jellyfin | 0,4895 | 0,4811 | 0,5199 | 0,4663 |

**Polly'de model F1'i tabanin altinda** (0,2500 / 0,2667). "Model her repoda daha iyi"
denemez.

**En guclu uc katsayi uc repoda da ayni:** `CsFilesChanged` (+), `FilesChanged` (-),
`LinesAdded` (+). **`PriorFixes` hicbir repoda ilk ucte degil** ve isareti degisiyor
(Polly -0,4180, ShareX +0,0299, Jellyfin +0,4511).

Katsayilar **nedensellik degil**: `FilesChanged` ile `Entropy` uc repoda da 0,89'un
uzerinde korelasyonlu (Jellyfin'de 0,9831); boyle ciftlerde isaret ve buyukluk veriye
duyarli.

## 5. Belirsizlik

Kaynak: `asama5-kalibrasyon.md` 5. bolum, `bootstrap-results.json` (`5acd9b44…5785a233467`).
2000 tekrar, tohum 20260912, blok `ceil(sqrt(N))`.

| Kume | Delta F1 [p2,5 – p97,5] | Delta PR-AUC [p2,5 – p97,5] |
|---|---|---|
| **Mikro** | **[0,0485 – 0,0823]** | **[0,1818 – 0,2513]** |
| Makro | **[-0,0296 – 0,0563]** | [-0,0155 – 0,1216] |
| Polly | [-0,1241 – 0,1140] | [-0,1193 – 0,2949] |
| ShareX | [-0,0055 – 0,0775] | [-0,0075 – 0,0453] |
| Jellyfin | [-0,0088 – 0,0257] | [0,0329 – 0,0729] |

> **Model, hacim agirlikli mikro karsilastirmada LinesAdded tabaninin uzerinde kaldi;
> repo basina esit agirlik veren makro F1 araligi fark yok degerini icerdi.**

**Bu bootstrap egitim belirsizligini kapsamiyor:** tahminler sabit, model yeniden
egitilmedi. Olculen sey yalnizca zamansal test ornekleme belirsizligi.

## 6. Kalibrasyon

Kaynak: `asama5-kalibrasyon.md`, `calibration-results.json` (`7a3456cb…7d05debb`).

Mikro, **azaltilmis-egitim** modeli uzerinde:

| Yontem | Brier | ECE |
|---|---|---|
| Ham | 0,0843 | 0,0809 |
| Platt | **0,0838** | **0,0738** |
| Isotonic | **0,0833** | **0,0699** |

**Iki yontem de mikro basari kosulunu sagladi** (hem Brier hem ECE dustu).
**Jellyfin'de ikisi de karisik sonuc verdi:** ECE dustu (0,0903 → 0,0900 ve 0,0861), Brier
yukseldi (0,1025 → 0,1040 ve 0,1035).

- Test sonucuna bakarak **kazanan secilmedi**; ikisi onceden ilan edilmis iki ayri deney.
- Kalibrasyon modelleri **azaltilmis-egitim** deneyine ait; tam-train ham modelin **yerine
  gecmiyorlar**.
- Platt repo bazinda PR-AUC'yi degistirmedi ama mikro havuzda degistirdi (0,4742 →
  0,4770): mikro havuz uc reponun satirlarini birlestiriyor ve her repo **kendi** Platt
  donusumunu goruyor, yani repolar **arasindaki** sira degisiyor.
- Guvenilirlik diyagramlari: `docs/olcumler/grafikler/asama5-kalibrasyon-*.svg`.

**Uretim icin kalibrator secilmedi.**

## 7. Duyarlilik

Kaynak: `asama5-duyarlilik.md`, `sensitivity-results.json` (`9b0c4706…27915266`),
`rename-results-v2.json` (`6fdef8cd…7cdaf4ea`), `noise-normalized-results.json`
(`92748960…b253763b`).

| Deney | Ana sonuc | Alternatif | Bulgu |
|---|---|---|---|
| **Bot** | F1 Polly 0,2500 / Jellyfin 0,4895 | botsuz yeniden egitim | Polly F1 **ayni**, Jellyfin'de **tek bir tahmin** degisti (FP 1090 → 1089), ShareX'te bot **0** |
| **90 gun** | Polly 0,2500 / ShareX 0,1868 / Jellyfin 0,4895 | olgun alt kume | uc repoda da pozitif oran, F1 ve PR-AUC **daha yuksek** (F1 +0,0045 / +0,0447 / +0,0201) |
| **C# uygunlugu** | 15 oznitelik | ablasyon ve yalniz C# | `CsFilesChanged = 0` olan **8607 / 34 166** commit'in pozitifi **0**; ablasyon Jellyfin -0,0266, ShareX -0,0096, **Polly +0,0109** |
| **Aralik disi** | — | grup ayrimi | Polly **620 / 828**, ShareX **2411 / 2547**, Jellyfin **2308 / 6876**; tasmalarin hepsi **ust yonlu** |
| **Ad degisimi** | esik 50 | 40 ve 60 | v2 esik 50 ana veriyle **512 490 / 512 490 hucre** esti; model sonuclari az degisti (en buyuk F1 farki Polly'de 0,0289), metrigi degisen commit sayilari buyuk (Jellyfin 2757 ve 2514) |
| **Sentetik gurultu** | — | %5 / %10 / %20 | mutlak F1 ve PR-AUC yukseldi; tabanlara fark da yukseldi, ama **Jellyfin'de PR-AUC lift 0,3608 → 0,3277 dustu** |

Notlar:

- Bot deneyinde B ve C'nin Brier degerleri **farkli populasyonlara** ait (A 828 satir, B/C
  271 satir); dogrudan "etki" gibi karsilastirilmadi.
- 90 gunluk alt kume, cikarilan commit'lerin gercekte pozitif oldugunu **kanitlamiyor**.
  Ana test kumesi degistirilmedi.
- Veri kumesinin **%25,2'si** SZZ tasarimi geregi pozitif etiketlenemiyor.
  `CsFilesChanged`'in gucunun bir kismi bu etiket uretim biciminden geliyor olabilir;
  ablasyon iki kaynagi **ayiramadi**.
- Aralik disi analizi **neden-sonuc gostermiyor**.
- Hangi ad degisimi esiginin gercek ad degisimini daha dogru buldugu **kanitlanmadi**.
- Sentetik oranlar **gercek gurultu orani degil**; 11 / 13 orani **uygulanmadi**.

## 8. Repo-arasi genelleme

Kaynak: `asama5-genelleme.md`, `generalization-results.json` (`515f561c…3014159b89`),
`generalization-decomposition.json` (`a56585d1…50d52a622`).

- Alti tek kaynakli aktarimin **3 / 6'si** ayni-repo F1'inin **ustunde**, 3'u altinda.
- Leave-one-out'un **1 / 3'u** ayni-repo F1'inin ustunde.
- **LORO uc hedefte de en iyi tek kaynakli aktarimin altinda** (-0,1412 / -0,0700 /
  -0,0187), ama yayilimi daha dar (korunan F1 0,852–1,137'ye karsi 0,768–1,500).
- **PR-AUC korunup F1'in en az 0,05 dustugu 2 / 9 deney** (ikisinde de hedef Jellyfin).
- **0,5 esigi kaynak esiginden iyi olan 5 / 9.**
- **Ortalama tahmin hedef orandan yuksek 6 / 9, dusuk 3 / 9.**

Uc repo oldugu icin genel bir genelleme iddiasi kurulamiyor. **Esik aktarimi ile siralama
aktarimi farkli problemler:** ShareX → Jellyfin'de PR-AUC 0,4941 (ayni-reponun %95'i) ama
0,5 esiginde F1 0,0785.

## 9. Kor insan dogrulamasi

Kaynak: `asama5-tahmin-dogrulama.md`, `prediction-validation-results.json`
(`9c11cf94…f1f9c73668`). Insan karari commit'i `6806406`, incelenmeyenlerin kaydi
`b364f1a`.

> Otuz commitlik kor orneklemin 25'i yazar tarafindan commit diff'i ve sonraki ilgili
> degisiklikler incelenerek siniflandirildi; bes ornek incelenmedi ve paydalardan
> cikarildi. Model tahmini ve otomatik etiket kararlar tamamlanana kadar degerlendiriciye
> gosterilmedi; bagimsiz ikinci degerlendirici kullanilmadi.

| Kume | KUSUR-GETIRDI | KUSUR-GETIRMEDI | VERI-YETMEDI | BAKILMADI | Karar verilen |
|---|---|---|---|---|---|
| Toplam | 2 | 23 | 0 | **5** | 25 |
| Model-pozitif | 1 | 13 | 0 | 1 | 14 |
| Model-negatif | 1 | 10 | 0 | 4 | 11 |
| Polly | 1 | 4 | 0 | 5 | 5 |
| ShareX | 0 | 10 | 0 | 0 | 10 |
| Jellyfin | 1 | 9 | 0 | 0 | 10 |

- **Insan dogrulama precision isareti:** 1 / 14 = **0,0714** (Polly 1/4, ShareX 0/5,
  Jellyfin 0/5).
- **Insan dogrulama kacirma isareti:** 1 / 11 = **0,0909** (Polly 0/1, ShareX 0/5,
  Jellyfin 1/5).

SZZ ile insan karari (25 karar):

| | KUSUR-GETIRDI | KUSUR-GETIRMEDI |
|---|---|---|
| SZZ pozitif | 0 | 6 |
| SZZ negatif | 2 | 17 |

SZZ pozitif dogrulama isareti **0 / 6**, SZZ negatif kacirma isareti **2 / 19 = 0,1053**.

Model–SZZ anlasmazliklarinda: model FP gorunup insanin KUSUR-GETIRDI dedigi **1 / 9**;
model FN gorunup insanin KUSUR-GETIRMEDI dedigi **1 / 1**; ikisi de negatifken insanin
KUSUR-GETIRDI dedigi **1 / 14**; ikisi de pozitifken insanin KUSUR-GETIRMEDI dedigi
**5 / 6**.

**Orneklem kisitlari:** uc repoya esit, tahmin siniflarina esit, yalniz
`CsFilesChanged > 0`. Populasyon accuracy / precision / recall **hesaplanmadi**. Bes
`BAKILMADI` paydalardan cikarildi ve hepsi Polly'de.

## 10. Beklentiler

Dort beklenti dosyasi da **degistirilmedi**: `asama5-beklenti.md`,
`asama5-kalibrasyon-beklenti.md`, `asama5-duyarlilik-beklenti.md`,
`asama5-genelleme-beklenti.md`.

| Sonuc | Sayi |
|---|---|
| Tuttu | 24 |
| Kismen tuttu | 3 |
| Tutmadi | 8 |
| Olculemedi | 1 |

**Tutmayan sapmalar:**

1. **Polly test pozitif orani beklenenden dusuk** — bant %3-9, olculen %1,21. Nedeni
   olculmedi.
2. **Jellyfin `LinesAdded` F1 beklenenden yuksek** — bant %15-32, olculen %48,1. Nedeni
   olculmedi.
3. **Polly rastgele taban F1 bandin altinda** — bant %3-9, olculen %2,57. Sonradan fark
   edildi: beklentinin "F1 ≈ p" varsayimi p ile q'nun yakin olmasina dayaniyordu, Polly'de
   yakin degiller.
4. **`PriorFixes` ilk ucte degil** — uc repoda da yok, isareti degisiyor. Nedeni olculmedi.
5. **Repo-arasi F1 genel olarak dusmedi** — 9 deneyin 4'unde ayni-reponun ustunde;
   `asama5-beklenti.md`'nin "yarisi ile dortte ucu arasi" bandi da tutmadi. Nedeni
   olculmedi.
6. **90 gun filtresi en cok Polly'yi degistirmedi** — F1 degisimi Polly +0,0045, ShareX
   +0,0447. Nedeni olculmedi.
7. **Sentetik gurultude mutlak F1 ve PR-AUC yukseldi** — beklenti dusus yonundeydi. Ek
   olcumle (6b) mutlak artisin tamaminin taban oranindan gelmedigi gosterildi; sebep
   **kanitlanmadi**.
8. **Yalniz C# alt kumesinde degerlendirme daha zor olmadi** — PR-AUC iki repoda hafif
   dustu, F1 dusmedi.

**Kismen tutanlar:** `CsFilesChanged` ablasyonu (iki repoda dustu, Polly'de yukseldi);
Platt'in PR-AUC'yi degistirmemesi (repo bazinda tuttu, mikro havuzda tutmadi — bunu
**olcumden once yazmadim**); ShareX kaymasinin aktarimi zorlastirmasi (belirsiz).

**Olculemedi:** her seye negatif tabaninin accuracy bandi — accuracy bilerek
hesaplanmadi.

**Ek olarak iki dokuman arasi tanim farki:** `asama5-beklenti.md` precision N/A iken F1'i
de N/A bekliyordu; metrik sozlesmesi F1 = 0 dedi. Ikisi de sonuc gorulmeden yazildi; bu
bir beklenti sapmasi degil, tanim farki.

## 11. Bilerek birakilan sinirliliklar

Tam liste `docs/sinirliliklar.md` icinde; ozet:

- **SZZ negatif sinifi eksik olabilir** ve yayginligi bilinmiyor.
- **8607 commit (%25,2) yapisal olarak pozitif etiketlenemiyor.**
- **Sag sansur:** test pozitif oranlari egitimden dusuk.
- **Polly testinde yalniz 10 pozitif var.**
- **Uc repo** — genel genelleme iddiasi kurulamiyor.
- **Tek insan degerlendirici;** Cohen kappa hesaplanmadi.
- **Kor anahtar repoda duruyordu;** korluk yontemsel.
- **14 ornekte diff kirpildi.**
- **Kor orneklem populasyonu temsil etmiyor.**
- **Bootstrap yalniz test ornekleme belirsizligini kapsiyor;** egitim belirsizligi yok.
- **Duyarlilik deneyleri bagimsiz kosuldu;** birlesik etkiler olculmedi.
- **Kalibrator secilmedi.**
- **L2 duyarliligi olculmedi.**
- **Aralik disi degerlerin nedensel etkisi bilinmiyor.**
- **Ad degisimi esiklerinden hangisinin dogru oldugu bilinmiyor.**
- **Sentetik gurultu gercek gurultu tahmini degil.**
- **Katsayilar korelasyon nedeniyle nedensel okunamaz.**

## 12. Asama 5'in sonucu

**1) Ayni-repo nokta sonucu.** Lojistik regresyon, onceden ilan edilmis uc toplu kosulun
**3 / 3'unu** gecti: mikro F1 0,4408 > 0,3754, makro F1 0,3088 > 0,2999, mikro ham PR-AUC
0,4775 > 0,2611. Repo duzeyinde ise Polly'de F1 tabanin **altinda** (0,2500 / 0,2667).

**2) Belirsizlik sonucu.** Mikro delta F1 araligi [0,0485, 0,0823] ve mikro delta PR-AUC
araligi [0,1818, 0,2513] tabanin ustunde kaldi. **Makro delta F1 araligi
[-0,0296, 0,0563] fark yok degerini iceriyor.** Bu bootstrap egitim belirsizligini
kapsamiyor.

**3) Repo-arasi sonuc.** Alti tek kaynakli aktarimin 3'u ayni-repo F1'inin ustunde, 3'u
altinda; leave-one-out uc hedefte de en iyi tek kaynagin altinda. Yon tutarli degil ve uc
repo, genel bir sonuc icin yeterli degil.

**Bu nedenle sonuc "her repoda ustun model" degil, "hacim agirlikli toplu olcumde tabani
gecen, fakat repo duzeyinde belirsizlik ve aktarim sorunu tasiyan model" olarak
yaziliyor.**

Kor insan dogrulamasi bu cumleyi **ne guclendiriyor ne zayiflatiyor**; yalnizca orneklem
sinirlari icinde sunu ekliyor: incelenen 25 commit'in 23'unde degerlendirici kusur
bulmadi, model-pozitif precision isareti 1 / 14 ve SZZ pozitif dogrulama isareti 0 / 6
cikti. Bu sayilar 25 karara ve tahmin siniflarina esit dagitilmis bir orneklege dayaniyor;
populasyon iddiasi degil.

## 13. Asama 6'ya devir

- **Ana referans model:** tam-train, **kalibre edilmemis** uc ayni-repo lojistik model
  (`data/asama5/models/*.zip`).
- **Uretim kalibratoru secilmedi.** Risk olasiligi "kalibre edilmis olasilik" diye
  sunulamaz.
- **UI / API'de model kaynagi ve hangi repo modelinin kullanildigi belirtilmeli.**
- **Bilinmeyen bir repo icin hangi modelin kullanilacagi henuz karara baglanmadi;**
  Asama 6'da sessizce secilmemeli.
- **`CsFilesChanged = 0` commit'lerde etiket kapsam siniri gorunur olmali** — o commit'ler
  tasarim geregi pozitif etiketlenemiyor.
- **`Probability` ile karar esigi ayri alanlar olmali.**
- **Varsayilan esik konusunda 0,5 ve train-tuned sonucu birlikte korunmali** (mikro F1
  0,4646'ya karsi 0,4408).
- **Kalibrasyon grafikleri tez ve panel icin hazir:** `docs/olcumler/grafikler/`.

---

## Tezde kullanilabilecek en guclu yedi bulgu

1. **Veri kumesinin %25,2'si yapisal olarak pozitif etiketlenemiyor.** `CsFilesChanged = 0`
   olan 8607 commit'in pozitif sayisi uc repoda da tam olarak 0. Bu, SZZ'nin kapsam
   siniri; olculen bir bulgu, varsayim degil.
2. **Sag sansur test tarafini olcusulebilir sekilde bozuyor.** Polly'nin testinde 180
   gunden az olgun 207 commit var ve hicbiri pozitif degil; test pozitif orani %13,0'ten
   %1,21'e dusuyor.
3. **Mikro ve makro ayni yonu gostermiyor.** Mikro delta F1 araligi [0,0485, 0,0823]
   tabanin ustunde, makro delta F1 araligi [-0,0296, 0,0563] sifiri iceriyor. Tek bir
   toplama yontemi secilseydi sonuc ya fazla iyimser ya fazla karamsar yazilirdi.
4. **Siralama aktarilabiliyor, esik aktarilamiyor.** ShareX → Jellyfin'de PR-AUC 0,4941
   (ayni-reponun %95'i) ama 0,5 esiginde F1 0,0785.
5. **Kalibrasyon yontemi secimi sonuca baglanamaz.** Iki yontem de mikro toplamda hem
   Brier'i hem ECE'yi dusurdu, ama Jellyfin'de ikisi de karisik sonuc verdi
   (ECE dustu, Brier yukseldi).
6. **Mutlak metrik artisi tek basina iyilesme demek degil.** Sentetik gurultude F1 ve
   PR-AUC yukseldi; eslesmis tabanlar ve normalize olculer eklenince Jellyfin'de PR-AUC
   lift'in 0,3608'den 0,3277'ye **dustugu** gorundu.
7. **Kor dogrulama etiketle model arasindaki farki gosteriyor.** 25 karar icinde SZZ
   pozitif dogrulama isareti 0 / 6, SZZ negatif kacirma isareti 2 / 19; ikisi de orneklem
   sinirlari icinde okunuyor.

## Mulakatta anlatilabilecek en guclu yedi hikaye

1. **Bir satirlik kayma hatasi.** `BlameHunk.FinalStartLineNumber` 0 tabanli, bizim
   satirlarimiz 1 tabanliydi; SZZ her seferinde bir alttaki satiri sucluyordu. 355 testin
   hicbiri gormemisti cunku test depolarindaki dosyalarin butun satirlari tek commit'ten
   geliyordu. Duz `git blame` ile uyum %68,7'den %97,5'e cikti.
2. **Varsayilan ayarin modeli sifirlamasi.** ML.NET'in `LbfgsLogisticRegression`
   varsayilani L1 = 1,0 ve o degerde butun agirliklar sifira dusuyor. L1 = 0 secildi -
   sonuca bakarak degil, "bu adimda oznitelik eleme yasak" kuralindan.
3. **Artimli derlemenin uyariyi gizlemesi.** Yerelde `-warnaserror` temiz gorunuyordu ama
   CI iki xUnit analiz uyarisi gosterdi; test projesi yeniden derlenmiyordu.
   `--no-incremental` kurala baglandi.
4. **Siralama uyusmazliginin yakalanmasi.** Ad degisimi deneyi tarih + SHA siralamis,
   ana boru hatti tarih + madencilik sirasi kullaniyordu. Esik 50 kapisi eklendi:
   512 490 hucrenin tamami esleyene kadar 40 ve 60 kosulmadi.
5. **Sonumsuz Newton'un patlamasi.** Platt fit'inde ilk adim asiri buyuk oluyor, butun
   tahminler doyuma gidiyor ve Hessian tekillesiyordu. Gercek veriye bakilmadan, bir birim
   testinde goruldu ve sozlesme o sirada guncellendi.
6. **Kor degerlendirmenin kurgusu.** Anahtar dosyasi kararlar commit edilene kadar
   acilmadi; malzeme dosyasinda korlenen alanlarin **adlari bile** gecmiyor, cunku bagimsiz
   sizinti kontrolu dosyada o adlari ariyor.
7. **Eksik kararin uydurulmamasi.** Bes ornek incelenmedi; kusur karari uretmek yerine
   `BAKILMADI` olarak kaydedildi ve paydalardan cikarildi. `VERI-YETMEDI` yazilsaydi
   verinin yetersiz oldugu iddia edilmis olurdu - oysa o commit'lere bakilmadi.
