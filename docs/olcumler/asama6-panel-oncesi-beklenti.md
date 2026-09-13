# Panel oncesi beklentiler

**Beklentiler kod yazilmadan ve hicbir sey olculmeden once yazildi.**

**Tarih:** 2026-09-13
**Durum:** Adim 3b (arka plan sertlestirme) ve Adim 4 (panel iskeleti) yazilmadan once
yazildi. Bu dosya sonradan degistirilmeyecek.

Adim 3'un olcumu yirmi beklentinin yirmisini tutturdu ama geriye uc acik borc birakti:
static tarama neyi taradigini kaydetmiyordu, bellek sayisinin nereden geldigi
ayristirilmamisti ve es zamanlilik yalniz 1 ile olculmustu. Panelden once bu uc borcu
kapatiyorum; panelin gosterecegi sey zaten bu isin sonucu.

Asagidakiler tahmin. Tutmayanlar olcum dosyasinda "tutmadi" diye yazilacak, beklenti
geriye donuk duzeltilmeyecek.

## Static taramanin kaynagi

1. **Taramanin basindaki ve sonundaki HEAD SHA'si ayni olmali.** Is, basladigi anda
   hangi commit'i tariyorsa bitiste de onu tariyor olmali.
2. **Temiz calisma agacinda uretilen sonuc HEAD SHA ile tekrar uretilebilir olmali.**
   Ayni SHA'ya donup ayni taramayi kosunca ayni bulgu kumesi cikmali.
3. **Kirli calisma agacinda static tarama baslatilmamali.** Kaydedilen SHA, diskteki
   dosyalari anlatmiyorsa sonuc yaniltici olur.
4. **Tarama sirasinda HEAD ya da calisma agaci degisirse is tam sonuc uretememeli.**
   Satirlar durabilir ama `isResultComplete` false kalmali.

## Coklu surec

5. **Ikinci bir API surecinden gelen iptal, calisan worker tarafindan en gec bir sonraki
   dosya/batch sinirinda gorulmeli.** Surec ici jeton tek guvence olmamali.
6. **Veritabani tekilligi iki surecte de ayni isi engellemeli.** Iki ayri worker ayni
   kuyruktaki isi almaya calisirsa yalniz biri kazanmali.

## Bellek

7. **1267 MB degerinin taze bir API surecinde birebir tekrarlanmasini beklemiyorum.**
   Onceki olcum butun isleri ayni surecte kosturdu; her repo ve her is turu icin ayri
   surec acilinca sayinin daha dusuk cikmasini bekliyorum. Ne kadar dusecegi icin bir
   beklentim yok.
8. **Risk skorlamasi sirasinda EF ChangeTracker oge sayisi commit sayisiyla dogrusal
   buyumemeli.** Obek sonlarinda tabana donmeli.

## Panel

9. **Blazor ilk anlamli render Release/localhost kosulunda 1500 ms altinda olmali.**
10. **Isinmis sayfa gecisleri 500 ms altinda olmali.**
11. **Polling araligi 1 saniye olmali ve terminal duruma gecen isle birlikte durmali.**
12. **Mobil 390 px ve masaustu 1440 px gorunumlerinde yatay sayfa tasmasi olmamali.**

## Panel dili

13. **Panelde "hata olasiligi" ve yuzde olasilik dili bulunmamali.**
14. **`RawModelScore`, `RiskIndex` ve iki karar esigi ayri gosterilmeli.** Biri
    otekinin yerine gecmemeli.
15. **Statik bulgular ham model skoruna dahilmis gibi sunulmamali.**

## Beklenti yazmadigim yerler

Bu sayilar olculecek ama "tuttu/tutmadi" diye degerlendirilmeyecek:

- Taze surecte olculen tepe bellegin kendisi.
- Es zamanlilik 2'nin toplam duvar saatini ne kadar kisalttigi (ya da uzattigi).
- Sayfa basina API istek sayisi ve aktarilan byte.
- Dirty worktree kontrolunun ne kadar surdugu.
- Iki surec arasindaki iptalin kac milisaniyede gorulecegi.
