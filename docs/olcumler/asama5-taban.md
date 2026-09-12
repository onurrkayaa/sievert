# Asama 5 taban cizgileri

**Tarih:** 2026-09-12
**Durum:** Uc taban olculdu. Model kurulmadi, ML.NET eklenmedi, normalizasyon ya da
donusum uygulanmadi, oznitelik secilmedi.

Bundan sonra kurulacak lojistik regresyon bu uc sayiya karsi degerlendirilecek. Tek
basina "F1 0,48" gibi bir sayinin anlami yok; hangi tabanin uzerine ciktigi yazilmadan
bir yontem iyi ya da kotu sayilamaz.

## 1. Veri ve checksum'lar

Iki girdi de dondurulmus; bu adim ikisini de yalnizca **okudu**.

| | |
|---|---|
| Anlik goruntu | `data/asama5/commit-metrics.csv` |
| Anlik goruntu SHA-256 | `1b8e5a5c64ff9ccc6f8495f90711a05b95b5340b2e778dce59a9c6cea4d46e95` |
| Bolme manifesti | `data/asama5/split-manifest.csv` |
| Manifest SHA-256 | `01b5cafa2c82cde3dffc98afc0ed571918ebbc648bb8c74fad2bc071b601b8cb` |
| Sonuc dosyasi | `data/asama5/baseline-results.json` |
| Sonuc SHA-256 (v2, gecerli) | `dd62daa7ca7a077645c2053732bcf93291b3459cd1e958797f66cd10f6f38d65` |
| Ureten commit (v2) | `15fc80f` (`tools/Sievert.Measure`, `baseline` komutu) |
| Metrik sozlesmesi | surum 1.0 |

Sonuc dosyasinin iki surumu var ve eskisi silinmedi, git gecmisinde duruyor:

| Surum | SHA-256 | Ureten commit | Fark |
|---|---|---|---|
| v1 | `eaebcf67605bff679e30ecbb50f9bd649e571ee0a6167c22c7eba290a73aa7b2` | `0e20959` | rastgele tabanin makro F1'i eksik |
| **v2** | `dd62daa7ca7a077645c2053732bcf93291b3459cd1e958797f66cd10f6f38d65` | `15fc80f` | makro F1 eklendi |

Iki dosya `macroF1` ve `codeCommit` disinda **birebir ayni**: v1 ile v2 alan alan
karsilastirildi, baska hicbir sayi degismedi. Makro F1 depo F1'lerinden turetiliyor,
fazladan rastgele sayi cekmiyor; rastgele tabanin tohumu, tekrar sayisi, repo ve satir
sirasi, p degerleri aynen korundu.

Bolme manifestten okundu, yeniden hesaplanmadi. Sayilar Adim 1'dekiyle birebir ayni;
farkli ciksaydi arac sonucu yazmadan duracakti.

| Repo | Train | Train pozitif | Test | Test pozitif | Test orani |
|---|---|---|---|---|---|
| Polly | 1931 | 251 / 1931 | 828 | 10 / 828 | %1,21 |
| ShareX | 5943 | 884 / 5943 | 2547 | 129 / 2547 | %5,06 |
| Jellyfin | 16 041 | 3761 / 16 041 | 6876 | 927 / 6876 | %13,48 |
| **Mikro** | **23 915** | **4896 / 23 915** | **10 251** | **1066 / 10 251** | **%10,40** |

Sonuc dosyasi iki kez uretildi ve `cmp` ile bayt bayt ayni cikti.

## 2. Metrik sozlesmesi

Tam tanim `asama5-metrik-sozlesmesi.md` (surum 1.0) icinde ve **koddan once** yazildi.
Ozet:

- TP / FP / FN / TN her zaman birlikte yaziliyor.
- Precision = TP / (TP + FP), recall = TP / (TP + FN), F1 = 2PR / (P + R).
- Pozitif tahmin yoksa **precision N/A**, F1 = 0; ikisi birlikte yaziliyor.
- PR-AUC: skorlar azalan siralaniyor, esit skorlar tek esik grubu, egrinin basina
  (0, ilk grubun precision'i) ekleniyor, **yamuk kuraliyla** integre ediliyor. Elle
  hesaplanmis bir ornekle (19/24) sinaniyor.
- **Accuracy hesaplanmadi ve raporlanmadi** (ADR 0017).

## 3. Her seye negatif tabani

Hicbir satira pozitif demiyor, skoru sabit 0.

| Repo | TP | FP | FN | TN | Precision | Recall | F1 | PR-AUC |
|---|---|---|---|---|---|---|---|---|
| Polly | 0 | 0 | 10 | 818 | N/A (0 / 0) | 0,0000 (0 / 10) | 0,0000 | 0,0121 |
| ShareX | 0 | 0 | 129 | 2418 | N/A (0 / 0) | 0,0000 (0 / 129) | 0,0000 | 0,0506 |
| Jellyfin | 0 | 0 | 927 | 5949 | N/A (0 / 0) | 0,0000 (0 / 927) | 0,0000 | 0,1348 |
| **Mikro** | **0** | **0** | **1066** | **9185** | **N/A (0 / 0)** | **0,0000 (0 / 1066)** | **0,0000** | **0,1040** |

Makro F1: 0,0000.

**Precision N/A, F1 0 birlikte okunmali.** Precision'in yoklugu "yaptigi tahminlerin
hepsi yanlis" demek degil; hic tahmin yok. F1'in 0 yazilmasi sozlesmenin secimi, gerekcesi
orada.

**PR-AUC sutunu bir basari olcusu degil.** Sabit skorda siralama bilgisi yok; cikan deger
tam olarak o kumenin pozitif orani (0,0121 = 10/828, 0,0506 = 129/2547, 0,1348 = 927/6876,
0,1040 = 1066/10 251). Sozlesmede yazili sonucun ta kendisi. Bir modelin PR-AUC'si bu
sayinin uzerine cikmiyorsa siralama yetenegi gostermemis olur.

## 4. Rastgele taban

Her repo **kendi egitim bolumunun** pozitif oranini olasilik olarak kullaniyor; uc oran
birlestirilip tek bir p uretilmedi. Test etiketlerinden hicbir oran cikarilmadi.

1000 tekrar, ana tohum **20260912**, tekrar tohumu = ana tohum + tekrar numarasi. Tek bir
rastgele akis her tekrarda depolar ordinal sirada, satirlar manifest sirasinda tuketiyor.

| Repo | p (train) | Precision ort (p2,5 - p97,5) | Recall ort | F1 ort (p2,5 - p97,5) | PR-AUC ort |
|---|---|---|---|---|---|
| Polly | 0,1300 | 0,0140 (0,0000 - 0,0367) | 0,1520 | 0,0257 (0,0000 - 0,0672) | 0,0175 |
| ShareX | 0,1487 | 0,0506 (0,0311 - 0,0708) | 0,1489 | 0,0755 (0,0463 - 0,1046) | 0,0520 |
| Jellyfin | 0,2345 | 0,1347 (0,1213 - 0,1487) | 0,2341 | 0,1710 (0,1534 - 0,1889) | 0,1354 |
| **Mikro** | her repo kendi orani | 0,1133 (0,1022 - 0,1254) | 0,2230 | 0,1502 (0,1356 - 0,1661) | 0,1045 |

Medyanlar: Polly F1 0,0190, ShareX 0,0757, Jellyfin 0,1707, mikro 0,1501.

**Makro F1 dagilimi.** Her tekrarda uc reponun F1'i ayri hesaplanip basit ortalamasi
alindi; asagidaki sayilar o 1000 ortalamanin dagilimi.

| Olcu | Deger |
|---|---|
| Ortalama | 0,0907 |
| p2,5 | 0,0746 |
| Medyan | 0,0900 |
| p97,5 | 0,1088 |

Mikro ile karistirilmamali: mikro ortalama 0,1502, makro ortalama 0,0907. Mikro'da
Jellyfin (test satirlarinin %67,1'i ve uc repo icinde en yuksek p'ye sahip olan) sonucu
yukari cekiyor; makro Polly'nin dusuk F1'ini de esit agirlikla sayiyor.

Tahmin edilen pozitif sayisi:

| Repo | Ortalama | Min | Max |
|---|---|---|---|
| Polly | 108,2 | 77 | 137 |
| ShareX | 379,4 | 317 | 449 |
| Jellyfin | 1611,8 | 1524 | 1729 |

Precision ortalamalari test bolumunun pozitif oranina oturuyor: ShareX 0,0506'ya karsi
test orani 0,0506; Jellyfin 0,1347'ye karsi 0,1348. Recall ortalamalari egitim oranina
oturuyor: ShareX 0,1489 / 0,1487, Jellyfin 0,2341 / 0,2345. Ikisi de sozlesmedeki
beklentiyle ayni yonde.

Polly'de bant cok genis (F1 p2,5 = 0,0000) cunku test bolumunde yalnizca 10 pozitif var;
bazi tekrarlarda hicbiri yakalanmiyor.

## 5. LinesAdded tabani

Kural: **`LinesAdded >= esik` ise pozitif.** Esik yalnizca o reponun egitim bolumunde
secildi; aday esikler yalnizca egitimde gorulen benzersiz degerler. Secim sirasi: en
yuksek egitim F1'i, esitlikte en yuksek egitim precision'i, hala esitlikte daha yuksek
esik. Esik secildikten sonra degistirilmedi.

| Repo | Esik | Aday sayisi |
|---|---|---|
| Polly | 126 | 403 |
| ShareX | 35 | 879 |
| Jellyfin | 25 | 855 |

### Egitim bolumu (esigin secildigi yer)

| Repo | TP | FP | FN | TN | Precision | Recall | F1 |
|---|---|---|---|---|---|---|---|
| Polly | 174 | 218 | 77 | 1462 | 0,4439 (174 / 392) | 0,6932 (174 / 251) | 0,5412 |
| ShareX | 600 | 1791 | 284 | 3268 | 0,2509 (600 / 2391) | 0,6787 (600 / 884) | 0,3664 |
| Jellyfin | 2526 | 2587 | 1235 | 9693 | 0,4940 (2526 / 5113) | 0,6716 (2526 / 3761) | 0,5693 |

### Test bolumu

| Repo | TP | FP | FN | TN | Precision | Recall | F1 |
|---|---|---|---|---|---|---|---|
| Polly | 6 | 29 | 4 | 789 | 0,1714 (6 / 35) | 0,6000 (6 / 10) | 0,2667 |
| ShareX | 87 | 929 | 42 | 1489 | 0,0856 (87 / 1016) | 0,6744 (87 / 129) | 0,1520 |
| Jellyfin | 593 | 945 | 334 | 5004 | 0,3856 (593 / 1538) | 0,6397 (593 / 927) | 0,4811 |
| **Mikro** | **686** | **1903** | **380** | **7282** | **0,2650 (686 / 2589)** | **0,6435 (686 / 1066)** | **0,3754** |

Tahmin edilen pozitif sayisi: egitimde Polly 392, ShareX 2391, Jellyfin 5113; testte
Polly 35, ShareX 1016, Jellyfin 1538, mikro 2589.

### Iki ayri PR-AUC

Bu iki sayi ayni sey degil ve birbirinin yerine kullanilmiyor.

| Repo | Ham `LinesAdded` skoru | Esik sonrasi 0/1 skoru |
|---|---|---|
| Polly (train) | 0,4751 | 0,3957 |
| Polly (test) | **0,2610** | 0,1396 |
| ShareX (train) | 0,2551 | 0,2345 |
| ShareX (test) | **0,0970** | 0,0799 |
| Jellyfin (train) | 0,5465 | 0,4514 |
| Jellyfin (test) | **0,4663** | 0,3404 |
| Mikro (test) | **0,2611** | 0,2363 |

**Modelle karsilastirilacak olan kalin yazilan sutun**, yani ham `LinesAdded` skorunun
PR-AUC'si. Esik sonrasi 0/1 skoru yalnizca iki grup uretiyor ve siralamayi degil tek bir
esigi ozetliyor.

### Precision testte belirgin dusuyor

Uc repoda da egitimden teste precision dusuyor (Polly 0,4439 -> 0,1714, ShareX
0,2509 -> 0,0856, Jellyfin 0,4940 -> 0,3856), recall ise yerinde kaliyor
(0,69 -> 0,60, 0,68 -> 0,67, 0,67 -> 0,64).

Bunun **sebebi bu adimda olculmedi.** Akla gelen aciklama test bolumundeki pozitif
oranin egitimden dusuk olmasi (Polly %13,0 -> %1,21, ShareX %14,9 -> %5,06, Jellyfin
%23,4 -> %13,48): ayni esik, pozitifi daha seyrek bir kumede daha cok yanlis alarm
uretiyor. Ama bu bir tahmin; ayrica sag sansurun payi olabilir ve o ayrim yapilmadi.

## 6. Mikro / makro karsilastirma

| Taban | Mikro F1 | Makro F1 |
|---|---|---|
| Her seye negatif | 0,0000 | 0,0000 |
| Rastgele (1000 tekrar ortalamasi) | 0,1502 | 0,0907 |
| LinesAdded esigi | 0,3754 | 0,2999 |

Mikro ve makro ayni sey degil. Mikro'da Jellyfin tek basina test satirlarinin
6876 / 10 251'ini tasiyor, yani mikro buyuk olcude Jellyfin'in sonucu. Makro uc repoyu
esit sayiyor ve Polly ile ShareX'in dusuk F1'leri sayiyi asagi cekiyor: 0,3754'e karsi
0,2999.

Rastgele tabanin makro sutunu 1000 tekrarin **ortalamasi**; dagilimin tamami yukarida
(p2,5 0,0746, medyan 0,0900, p97,5 0,1088). Tek bir sayi olarak okunmamali.

LinesAdded tabaninin makro F1'i (0,2999) rastgele tabanin makro dagiliminin p97,5
degerinin (0,1088) uzerinde.

**LinesAdded tabani her repoda rastgele tabanin ustunde.** Test F1'leri: Polly 0,2667'ye
karsi rastgele ortalama 0,0257 (rastgele p97,5 = 0,0672), ShareX 0,1520'ye karsi 0,0755
(p97,5 = 0,1046), Jellyfin 0,4811'e karsi 0,1710 (p97,5 = 0,1889). Uc repoda da rastgele
tabanin 1000 tekrarindaki en ust %2,5'lik dilimin de uzerinde.

Ham PR-AUC'de de ayni yon: Polly 0,2610 / 0,0175, ShareX 0,0970 / 0,0520, Jellyfin
0,4663 / 0,1354, mikro 0,2611 / 0,1045.

## 7. Beklenti karsilastirmasi

`asama5-beklenti.md` **degistirilmedi.**

### LinesAdded F1

Beklenti dosyasindan aynen:

> Tek kural: `LinesAdded > esik` ise pozitif. Esik egitim bolumunden secilecek.
>
> - **Ayni repo F1 beklentim: %15-32.**

| Repo | Olculen test F1 | Bant | Sonuc |
|---|---|---|---|
| Polly | %26,7 | %15-32 | **tuttu** |
| ShareX | %15,2 | %15-32 | **tuttu** (alt ucunda) |
| Jellyfin | %48,1 | %15-32 | **TUTMADI**, bandin ustunde |
| Makro | %30,0 | — | bant icinde |
| Mikro | %37,5 | — | bandin ustunde |

Jellyfin'de tahmin tutmadi. **Nedeni olculmedi.** Beklentinin gerekcesi "tek bir sayi
commit'in ne yaptigini bilmiyor" idi ve bu Jellyfin'de dusundugumden az sinirlayici
cikti; ama bunun taban oranin yuksekliginden mi (test %13,48, digerlerinin iki-on kati),
`LinesAdded` dagiliminin bu repoda farkli olmasindan mi, yoksa baska bir seyden mi
geldigini olcmedim.

**Kural farki, ayrica yaziyorum:** beklenti dosyasi kurali `LinesAdded > esik` diye
yazmisti, bu adimda `LinesAdded >= esik` kullanildi. Ana sonuc `>=` protokoluyle kaldi:
olcumden once, Adim 2'nin promptunda ilan edilen uygulama budur.

Iki yazimin ayni tahminleri uretip uretmedigi **gercek veride sayildi**. Her repo icin
secilen `>=` esigine karsilik gelen `>` esigi, egitimde gorulen ve esikten kucuk en buyuk
deger olarak alindi; sonra iki tahmin vektoru satir satir karsilastirildi.

| Repo | Uygulanan | Esdeger `>` esigi | Train farkli tahmin | Test farkli tahmin |
|---|---|---|---|---|
| Polly | `>= 126` | `> 125` | 0 / 1931 | 0 / 828 |
| ShareX | `>= 35` | `> 34` | 0 / 5943 | 0 / 2547 |
| Jellyfin | `>= 25` | `> 24` | 0 / 16 041 | 0 / 6876 |
| **Toplam** | | | **0 / 23 915** | **0 / 10 251** |

Tek bir satirda bile fark yok, yani bu veride iki operator ayni siniflandiriciyi
uretiyor ve bant karsilastirmasi bundan etkilenmiyor.

Bunu bir sonuc olarak degil bir **hipotez** olarak kontrol ettim ve sonuc hipotezle ayni
cikti. Fark cikabilecek yer testte egitimde hic gorulmemis bir `LinesAdded` degerinin iki
esigin arasina dusmesiydi; uc repoda da oyle bir deger yok. Bunun sebebi `LinesAdded`'in
tam sayi olmasi ve esiklerin etrafinda yogun olmasi: esdeger `>` esikleri uc repoda da
`esik - 1` cikti, yani arada bosluk kalmadi. Bu bir olcum, garanti degil; baska bir esikte
ya da baska bir oznitelikte ayni sonucun cikacagi gosterilmedi.

### Her seye negatif tabani

Beklenti: recall tam olarak 0, precision tanimsiz. **Ikisi de tuttu:** recall 0 (0 / 1066),
precision N/A (0 / 0).

**Bir tanim farki var ve bu bir beklenti sapmasi degil.** Iki dokuman da sonuc
gorulmeden yazildi ve ayni durum icin farkli sey soyluyorlar:

- `asama5-beklenti.md` (3. madde): precision'in paydasi 0 iken F1 de **N/A**.
- `asama5-metrik-sozlesmesi.md` surum 1.0: precision **N/A**, recall **0**, F1 **0**.

**Uygulanan sonuc F1 = 0 sozlesmesidir.** Sozlesme bu adimin metrik tanimi ve koddan once
yazildi; gerekcesi orada duruyor.

Farkin kaynagi veri degil: olculen sayilarin hicbiri iki tanim arasinda degismiyor
(TP 0, FP 0, FN 1066, TN 9185 her iki tanimda da ayni). Degisen yalnizca bos hucreye ne
yazildigi. Bu yuzden bunu "beklenti tutmadi" satirina koymuyorum; onceden yazilmis iki
dokuman arasindaki bir tanim farki.

**Eski beklenti silinmedi**, `asama5-beklenti.md` degistirilmedi. Metrik kodu da
degistirilmedi.

Beklenti ayrica accuracy icin bant vermisti (Jellyfin %81-92, Polly %91-97). **Accuracy
bu adimda hesaplanmadi**, o yuzden o beklentinin tutup tutmadigi olculmedi.

### Rastgele taban

Beklenti: precision ≈ test pozitif orani, recall ≈ egitim orani, F1 ≈ p; bantlar Polly
%3-9, ShareX %4-11, Jellyfin %8-19.

| Repo | Olculen F1 ortalamasi | Bant | Sonuc |
|---|---|---|---|
| Polly | %2,57 | %3-9 | **TUTMADI**, bandin altinda |
| ShareX | %7,55 | %4-11 | **tuttu** |
| Jellyfin | %17,10 | %8-19 | **tuttu** |

Precision ve recall'un yonu uc repoda da tuttu (yukarida sayilari var).

Polly'nin sapmasi aritmetikten geliyor ve **bunu olcumden once yazmadim:** beklentideki
"F1 ≈ p" cumlesi "p ile q birbirine yakin" varsayimina dayaniyordu. Polly'de yakin
degiller (test orani p = 0,0121, egitim orani q = 0,1300), ve bu durumda F1'in beklenen
degeri p degil 2pq / (p + q) = 0,0221 oluyor; olculen 0,0257 buna yakin. Yani hesap
tutarli, beklentinin varsayimi Polly icin gecersizdi. Bu varsayimin gecersiz olmasinin
sebebi Adim 1'de zaten kayda gecmis olan bulgu: Polly'nin test pozitif orani beklenen
bandin altinda cikti.

## 8. Bilinen sinirliliklar

- **Polly'nin test bolumunde 10 pozitif var.** Butun Polly sayilari bu 10 satira dayaniyor;
  bir satirin yer degistirmesi F1'i gozle gorulur oynatir. Rastgele tabanin Polly
  bandinin p2,5 = 0,0000 olmasi da bundan.
- **Tek bolme, tek esik.** LinesAdded tabani icin guven araligi yok: esik bir kez secildi
  ve bir kez uygulandi. Rastgele tabanda dagilim var cunku orada rastgelelik kaynagi biziz;
  esikli tabanda tekrar edilecek bir sey yok.
- **Esitlik kurallari gercek veride sonucu degistirmedi.** Tek oznitelikli monoton bir
  esikte, iki esik ayni F1'i veriyorsa daha dusuk esigin precision'i hicbir zaman daha
  yuksek olamaz; cebiri ADR 0017'de. Kurallar yine de duruyor, cunku secim algoritmasinin
  deterministik ve her durumda tanimli olmasi gerekiyor; ayrica ileride monoton olmayan
  bir kural ya da baska bir oznitelik gelirse gerekecekler.
- **Sabit skorlu PR-AUC yorumlanamaz.** Her seye negatif tabaninin PR-AUC'si kumenin
  pozitif orani; yontemin degil verinin ozelligi.
- **Mikro toplam Jellyfin agirlikli.** Test satirlarinin %67,1'i (6876 / 10 251) Jellyfin'den.
  Mikro sonuclar buyuk olcude o reponun sonucu ve bu, uc repoya genelleme degil.
- **Sag sansur duruyor.** Ana test kumesi degistirilmedi, 30/90/180 gunluk alt kumeler
  cikarilmadi, Polly'nin dusuk test orani yuzunden hicbir etiket ya da bolme
  degistirilmedi. Duyarlilik deneyi Adim 4'te, daha once ilan edildigi gibi.
- **Precision'in testte dusme sebebi olculmedi.** Yukarida bir aciklama adayi yazdim, o
  bir tahmin.
- **11 / 13 sonucu kullanilmadi.** Asama 4'un hedefli secilmis etiketsiz orneklemindeki
  kacirma orani bu adimda hicbir yere girmedi: etiket degistirilmedi, agirlik verilmedi.
