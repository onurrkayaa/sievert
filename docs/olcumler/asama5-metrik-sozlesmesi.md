# Asama 5 metrik sozlesmesi

**Surum:** 1.0
**Tarih:** 2026-09-12
**Durum:** Koddan **once** yazildi. Taban cizgileri de, sonraki adimlarda kurulacak
lojistik regresyon da ayni hesaplari kullanacak; ikisi farkli formullerle olculurse
karsilastirilamazlar.

Bu dosya "hangi sayi nasil cikiyor" sorusunun tek cevabi. Bir tanim degisirse surum
numarasi artar ve eski sayilarin hangi surumle uretildigi
`data/asama5/baseline-results.json` icinde yazili kalir.

## Sayim

Tahmin ikili (pozitif / negatif), gercek etiket `IsBugIntroducing`.

| | Gercek pozitif | Gercek negatif |
|---|---|---|
| **Tahmin pozitif** | TP | FP |
| **Tahmin negatif** | FN | TN |

Dort sayi her zaman birlikte yazilir. TP + FP + FN + TN, o kumedeki satir sayisina esit
olmali; esit degilse oran hesaplanmaz.

## Oranlar

- **Precision** = TP / (TP + FP)
- **Recall** = TP / (TP + FN)
- **F1** = 2 · Precision · Recall / (Precision + Recall)

Her oranin yaninda **pay ve payda ayri** yazilir. Tek basina yuzde yazilmaz.

### Precision'in paydasi 0 ise

Hic pozitif tahmin yapilmamis demektir. O zaman:

- **Precision = N/A.** Sessizce 0 ya da 1 yapilmaz.
- **F1 = 0** kabul edilir.

F1'in 0 sayilmasinin gerekcesi: F1 "bulunan pozitiflerin ne kadar dogru ve ne kadar
eksiksiz oldugunu" ozetliyor. Hic pozitif bulunmamissa eksiksizlik tarafi (recall) zaten
0 ve tabanin bulusu yok; siralama tablosunda bos hucre birakmak yerine 0 yaziliyor ki
tablolar toplanabilsin.

Precision'in N/A oldugu her yerde **ikisi birden yazilir**: "Precision N/A (0 / 0),
F1 0". Yalnizca "F1 0" yazmak, tabanin yanlis tahmin yaptigini ima ederdi; oysa hic
tahmin yok. Ikisi farkli seyler.

> Not: `asama5-beklenti.md` (3. madde) bu durumda F1 icin "N/A" yazmisti. Bu adimda F1 = 0
> secildi. Beklenti dosyasi **degistirilmedi**; fark burada duruyor ve `asama5-taban.md`
> icinde ayrica yaziyor.

### Recall'un paydasi 0 ise

Kumede hic gercek pozitif yok demektir.

- **Recall = N/A**, **F1 = N/A**, **PR-AUC = N/A**.

Bu veri kumesinde beklenmiyor: uc reponun da hem egitim hem test bolumunde pozitif var.
Olursa rapor durur ve sebep arastirilir.

## Accuracy

**Hesaplanmiyor ve raporlanmiyor.** Gerekcesi ADR 0017'de: bu veri kumesinde negatif
sinif buyuk (test bolumunde 1066 / 10 251 pozitif), yani hicbir sey yapmayan bir taban
yuksek accuracy aliyor ve bu sayi tabanlari birbirinden ayirmiyor.

## PR-AUC

PR-AUC bir **alan** olcusu ve skaler olarak raporlanir. Payi ve paydasi yoktur; oyle bir
sey yazilmaz.

### Hesap

1. Satirlar **skora gore azalan** siralanir.
2. **Esit skorlar tek bir esik grubu** olarak birlikte islenir. Bir grup bolunmez; bu
   yuzden sonuc, esit skorlu satirlarin kendi aralarindaki sirasindan bagimsizdir.
3. Gruplar sirayla islenir. `i`. grup islendikten **sonra** o ana kadar biriken degerler:
   - `TP_i` = skoru bu esikten buyuk ya da esit olan gercek pozitif sayisi
   - `FP_i` = ayni kumedeki gercek negatif sayisi
   - `r_i = TP_i / P` (P = kumedeki toplam gercek pozitif)
   - `p_i = TP_i / (TP_i + FP_i)`
4. Egrinin basina `r_0 = 0`, `p_0 = p_1` noktasi eklenir. Yani ilk grubun precision'i
   sola dogru sabit uzatilir.
5. **Integrasyon: yamuk (trapez) kurali.**

   `PR-AUC = toplam( (r_i − r_(i−1)) · (p_i + p_(i−1)) / 2 )`, i = 1..k

Basamak (step) toplami ya da "average precision" formulu **kullanilmiyor**. Ikisi ayni
egri icin farkli sayilar uretir; hangisinin kullanildigi yazilmazsa sayilar
karsilastirilamaz.

Hicbir kutuphanenin varsayilanina baglanilmadi; hesap bu tanima gore elle yazildi ve
elle hesaplanmis bir ornekle sinaniyor.

### Elle hesaplanan ornek

Skorlar `[0,9, 0,8, 0,7, 0,6]`, etiketler `[1, 0, 1, 0]`, P = 2.

| Grup | TP | FP | r | p |
|---|---|---|---|---|
| 0,9 | 1 | 0 | 0,5 | 1 |
| 0,8 | 1 | 1 | 0,5 | 0,5 |
| 0,7 | 2 | 1 | 1,0 | 2/3 |
| 0,6 | 2 | 2 | 1,0 | 0,5 |

Basa (0, 1) eklenir. Yamuk toplami:

- (0,5 − 0) · (1 + 1) / 2 = 0,5
- (0,5 − 0,5) · … = 0
- (1,0 − 0,5) · (2/3 + 0,5) / 2 = 0,5 · (7/6) / 2 = 7/24
- (1,0 − 1,0) · … = 0

Toplam = 0,5 + 7/24 = **19/24 = 0,791666…**

Bu deger bir testte sabit olarak duruyor.

### Sabit skorlu taban

Butun satirlarin skoru ayniysa tek bir esik grubu olusur: `r_1 = 1`, `p_1 = P / N`.
Basa (0, P/N) eklenince yamuk toplami `P / N` cikar, yani **kumenin pozitif orani**.

Bu sayi bir basari olcusu **degil**. Sabit skorda siralama bilgisi yoktur; cikan deger
yontemin degil veri kumesinin ozelligi. Raporda her zaman "sabit skor, siralama yetenegi
yok" notuyla birlikte yaziliyor.

### Esikli tabanlarda iki ayri PR-AUC

`LinesAdded` esigi gibi bir taban iki farkli skor uretebilir ve ikisi ayni sey degil:

1. **Ham oznitelik skoru:** skor = `LinesAdded`. Bu, tabanin **siralama** yetenegini
   olcer ve ileride modelin PR-AUC'siyle karsilastirilacak olan sayidir.
2. **Esik sonrasi 0/1 skoru:** skor = tahmin. Bu yalnizca iki grup uretir ve sayisi
   siralamayi degil tek bir esigi ozetler.

Ikisi ayri satirlarda raporlanir ve birbirinin yerine kullanilmaz.

## Mikro ve makro toplama

- **Mikro:** uc reponun test tahminleri tek bir havuzda birlestirilir, TP/FP/FN/TN
  toplanir, oranlar o toplamdan hesaplanir. Buyuk repo sonuca daha cok agirlik verir;
  Jellyfin tek basina test satirlarinin 6876 / 10 251'i.
- **Makro:** her reponun F1'i ayri hesaplanir, sonra uc sayinin **basit ortalamasi**
  alinir. Her repo esit agirlikli.

Ikisi ayni sey degil ve birbirinin yerine yazilmaz. Ana karsilastirma tablosunda ayri
sutunlarda duruyorlar.

Makro ortalama alinirken N/A bir F1 varsa ortalama hesaplanmaz; kac reponun N/A oldugu
yazilir.

## Repo kimliginin rolu

`RepositoryIdentity` modele oznitelik olarak **girmiyor** (ADR 0016). Yalnizca uc is icin
kullaniliyor: her repo icin ayri esik secmek, sonuclari gruplamak, mikro/makro toplama
yapmak.
