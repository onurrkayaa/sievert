# Model aciklamasinin sayisal tolerans sozlesmesi

**Surum:** 2.0
**Tarih:** 2026-09-13
**Durum:** Kod yazilmadan ve yeniden olculmeden once sabitlendi.

Bu dosya tek bir soruyu cevapliyor: oznitelik katkilarinin toplami, modelin kendi
verdigi logit'ten **ne kadar** ayrilirsa aciklama yanlis sayilir.

Lojistik regresyonda ayristirma tam olmali:

```
logit = intercept + toplam(katsayi_i * deger_i)
```

Yani ayrilma ancak iki sebepten olabilir: ya aciklama gercekten yanlis, ya da iki farkli
sayisal duyarlikta toplama yapildigi icin temsil hatasi olusuyor. Sozlesmenin isi bu
ikisini birbirinden ayirmak.

## v1 - mutlak tolerans (Asama 6 Adim 2)

```
abs(explanationLogit - modelLogit) <= 1e-6
```

Bu kural Adim 2'de olcumden once ilan edildi ve **oldugu gibi duruyor**. Olculen sonucu
da duruyor: 34 166 satirin 11'inde asildi, en buyuk fark 1,783e-6
(`docs/olcumler/asama6-api-temel.md` bolum 7). O olcum silinmiyor.

v1'in kusuru sonradan anlasildi. Sabit bir mutlak tolerans, hatanin nereden geldigini
yanlis modelliyor: ML.NET skoru `float` ile topluyor ve `float` temsil hatasi **sonucun**
degil **terimlerin** buyuklugune gore olusuyor. En kotu satirda logit 2,33 iken katkilarin
mutlak toplami 27,39'du. 27,39 civarinda `float` cozunurlugu zaten 1,9e-6; yani 1e-6'lik
sabit bir tolerans o satirda fiziksel olarak saglanamazdi.

## v2 - olcege bagli tolerans

```
u = 2^-23

scale = max(1, abs(intercept) + toplam(abs(contribution_i)))

allowedError = 1e-6 + u * scale

gecis: abs(explanationLogit - modelLogit) <= allowedError
```

### Parcalarin neden boyle oldugu

**`u = 2^-23`.** `float` icin makine epsilonu. Degeri kodda **acikca** `2^-23` olarak
hesaplanir.

`float.Epsilon` **kullanilmaz**. O sabit makine epsilonu degil, temsil edilebilir en
kucuk pozitif subnormal sayidir (yaklasik 1,4e-45) ve buraya konursa tolerans sifir
sayilir. Bu yaygin bir karistirma ve bir test bunu acikca sinar.

**`abs(intercept) + toplam(abs(contribution_i))`.** Olcek terimlerin buyuklugunden
geliyor, sonucun buyuklugunden degil.

Logit'i ya da ham skoru tek basina olcek yapmak yanlis olurdu: buyuk pozitif ve buyuk
negatif katkilar birbirini goturebiliyor, sonuc kucuk kaliyor ama toplama sirasinda
olusan hata buyuk terimlerin olceginde oluyor. Adim 2'de olculen en kotu satir tam olarak
buydu.

**`max(1, ...)`.** Butun katkilarin cok kucuk oldugu bir satirda olcegin sifira gitmesini
engelliyor. Taban 1 olunca `u * scale` en az `u` kadar oluyor.

**`1e-6` tabani.** v1'in mutlak toleransi taban olarak kaliyor. Olcek 1 oldugunda kural
pratikte v1 ile ayni sertlikte.

### Bu bir "sonuca bakip buyutme" degil

Toleransi 2e-6 ya da 5e-6 gibi sabit bir sayiya cikarmak, olculen en buyuk farki gorup
onun ustune bir sayi secmek olurdu; o zaman tolerans olcumun kendisinden turemis olurdu.

Buradaki kural olculen sayidan degil, **IEEE 754 tek duyarlik temsilinden** turuyor.
`u * scale`, o satirdaki toplamanin kacinilmaz temsil hatasinin buyuklugu. Olcum sadece
bu modelin dogru model oldugunu gosteriyor, sayiyi belirlemiyor.

### Normalize hata

Tolerans olcege gore buyudugu icin, ham mutlak farka bakmak artik yaniltici olur: buyuk
olcekli bir satirda 3e-6'lik bir fark normal, kucuk olcekli bir satirda ayni fark
felakettir. O yuzden raporlanan olcu:

```
normalizedError = absoluteError / allowedError
```

- `normalizedError <= 1` → gecer
- `normalizedError > 1` → hata

Bu sayi butun satirlarda ayni anlama geliyor, dagilimi (p50/p95/p99/max) raporlanabiliyor
ve gercek bir aciklama bozuklugu genis olcekte bile saklanmiyor.

## Gecisin API'ye yansimasi

- v2 geciyorsa aciklama normal donuyor.
- v2 kaliyorsa cevap **donmuyor**: `MODEL_EXPLANATION_MISMATCH`.

Hata ayrintisinda katsayi, oznitelik degeri, dosya yolu ya da sayisal ic durum
**yazilmaz**. Kullaniciya soylenen sey sudur: model aciklamasi dogrulanamadi, bu yuzden
aciklama uretilmedi. `errorCode` ve `traceId` korunuyor.

## Ne dogrulanacak

Sozlesme iki yonlu sinanmadan dogru sayilmaz.

1. **Gevsek olmadigi.** Aciklamaya bilerek bozukluk sokuldugunda (en az 1e-4 mutlak logit
   farki uretecek kadar) v2 bunu **reddetmeli**. Reddetmezse tolerans fazla genis demektir
   ve sozlesme burada durur.
2. **Sert olmadigi.** v1'de kalan 11 satirin float olcegi kaynakli oldugu dogruysa v2'de
   gecmeleri gerekir.

Ikisi de `docs/olcumler/asama6-aciklama-toleransi.md` icinde olculecek.
