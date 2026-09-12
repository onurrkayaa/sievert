# Adim 3b kalibrasyon ve belirsizlik beklentileri

**Beklentiler olculmeden once yazildi.**

**Tarih:** 2026-09-12
**Durum:** Hicbir kalibrasyon ya da bootstrap sonucu uretilmeden yazildi. Elde yalnizca
Adim 3'un kapanmis sayilari var.

Bu dosya sonradan degistirilmeyecek. Tutmayan beklentiler silinmeyecek, yanina olculen
deger yazilacak.

## Elde ne var

Adim 3'un tam-train modeli, ham olasilikla:

| Kume | Brier | ECE |
|---|---|---|
| Mikro | 0,0819 | 0,0722 |
| Makro | 0,0563 | 0,0576 |

Olculen yon: model her kalibrasyon kutusunda gercekte olandan **yuksek** olasilik
veriyor ve fark kutu yukseldikce buyuyor.

Nokta farklari (model eksi `LinesAdded` tabani): mikro F1 +0,0654, makro F1 +0,0089,
mikro PR-AUC +0,2164.

## Kalibrasyon beklentileri

1. **Mikro ECE dusecek.** Test taban oranlari egitimden dusuk ve ham model yuksek
   olasiliklarda fazla tahmin yapiyor; iki kalibrasyon yontemi de bu tek yonlu sapmayi
   kucultmek icin uygun.

2. **Mikro Brier dusecek.** Her repoda ayri ayri dusmesi zorunlu beklenti degil; yalnizca
   mikro toplamda dusmesini bekliyorum.

3. **Polly'nin isotonic sonucu daha basamakli ve kararsiz olacak.** Polly'nin kalibrasyon
   bolumu ucunun en kucugu (beklenen 387 satir) ve o bolumdeki pozitif sayisi az; PAV
   algoritmasi az sayida genis blok uretir.

4. **Platt PR-AUC siralamasini degistirmeyecek.** Donusum monoton artan, yani skorlarin
   sirasi korunur; PR-AUC siraya bagli oldugu icin ayni kalmali. Kucuk sayisal
   yuvarlamalar disinda fark beklemiyorum.

5. **Isotonic PR-AUC'yi bir miktar degistirebilir.** PAV bloklari icindeki satirlara ayni
   degeri verdigi icin daha once ayri olan skorlar esitlenebiliyor; esit skorlar tek esik
   grubu olarak islendiginden egri degisebilir. **Bu degisiklik kalibrasyon basarisi
   sayilmaz**, yonu ne olursa olsun.

6. **Kalibrasyon basarisi iki kosullu.** Bir yontem icin "mikro kalibrasyonu iyilestirdi"
   yalnizca hem mikro Brier hem mikro ECE hamdan dusukse yazilacak. Biri iyilesip digeri
   kotulesirse "karisik sonuc" yazilacak.

7. **Platt ile isotonic arasinda kazanan secilmeyecek.** Ikisi onceden ilan edilmis iki
   ayri deney; test sonucuna bakip biri secilmeyecek.

## Bootstrap beklentileri

8. **Makro F1 farkinin araligi sifiri icerecek.** Nokta farki yalnizca +0,0089 ve uc
   repodan birinde (Polly) model tabanin altinda; bu kadar dar bir farkin 2000 tekrarlik
   bir aralikta sifirin ustunde kalmasini beklemiyorum.

9. **Mikro PR-AUC farkinin araligi sifirin ustunde kalacak.** Nokta farki +0,2164, yani
   diger iki farktan buyuklukce cok daha genis.

10. **Mikro F1 farki icin yon beklentisi yaziyorum ama bant yazmiyorum.** Nokta farki
    +0,0654; araligin sifirin ustunde kalmasini bekliyorum, ama ne kadar guvenle
    kalacagini tahmin etmiyorum.

11. **Polly'nin araliklari genis olacak.** Test bolumunde 10 pozitif var; blok
    orneklemesinde bazi tekrarlarda hic pozitif kalmayabilir.

12. **Bootstrap model egitim belirsizligini kapsamiyor.** Yalnizca sabit tahminler
    uzerindeki zamansal test ornekleme belirsizligini olcuyor. Bu cumle rapora aynen
    yazilacak.

## Beklenti yazmadigim yerler

- Platt ile isotonic'in birbirine gore hangisinin daha iyi cikacagi.
- Repo bazinda hangi kalibrasyonun hangi yonde degisecegi.
- Azaltilmis egitim (model-fit) modelinin ham sonucunun Adim 3'un tam-train modeline gore
  nerede duracagi. Ikisi ayni model degil; kalibrasyon etkisi yalnizca azaltilmis modelin
  kendi ham sonucuyla karsilastirilacak.
