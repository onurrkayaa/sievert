# Asama 6 Adim 3 arka plan isleri beklentileri

**Beklentiler kod yazilmadan ve hicbir sey olculmeden once yazildi.**

**Tarih:** 2026-09-13
**Durum:** Arka plan altyapisi yazilmadan once yazildi. Bu dosya sonradan degistirilmeyecek.

Asama 5 ve Adim 2'de oldugu gibi: burada yazan seyler tahmin. Tutmayanlar olcum
dosyasinda "tutmadi" diye yazilacak, beklenti geriye donuk duzeltilmeyecek.

## Dogruluk

1. **Static scan sonucu CLI ile ayni olmali.** Ayni calisma agaci ve ayni yapilandirmayla
   `sievert check` ile arka plan isinin bulgu kumesi (kural kodu, seviye, yol, satir),
   susturma sayisi ve muafiyet sayisi ayni cikmali.
2. **Risk-score-all sonucu risk ucu ile ayni olmali.** Ayni commit icin arka planda
   yazilan skor, `/risk` ucundan donen skorla ayni olmali. Ayni model ve ayni donusum
   kullanildigi icin **bit duzeyinde** esitlik bekliyorum; fark cikarsa bu bir kusur.
3. **Iptal edilen ya da basarisiz olan bir isin kismi sonucu tam sonuc gibi
   sunulmamali.** Satirlar durabilir ama `isResultComplete` false kalmali.

## Es zamanlilik ve tekillik

4. **Ayni repo + ayni is turu icin ayni anda en fazla bir aktif is.** Aktif =
   `queued` ya da `running`.
5. **Ayni `Idempotency-Key` tekrarinda yeni is olusmamali.** Ayni anahtarla ardisik ya da
   es zamanli gonderilen istekler tek bir ise dusmeli.
6. **Farkli anahtarlarla ayni repo + tur icin es zamanli 10 istekten 1'i kabul, 9'u
   catisma almali.**
7. **Tekillik yalniz uygulama kontroluyle saglanmamali.** Veritabani seviyesinde benzersiz
   bir kisit da olmali; iki es zamanli istek yarisirsa veritabani ikinciyi reddetmeli.

## Iptal

8. **Kuyruktaki bir is iptal edilirse calismaya hic baslamamali.**
9. **Calisan bir is iptal istegini en gec bir sonraki batch ya da dosya sinirinda
   gozlemeli.** Her satirda kontrol beklemiyorum.
10. **Terminal duruma gecmis bir ise iptal istegi durumu degistirmemeli**, ve iki kez
    iptal istegi gondermek tek bir iptalden farkli sonuc uretmemeli.

## Durum ve ilerleme

11. **Terminal duruma gecen bir is yeniden calismamali.**
12. **Progress monoton olmali:** islenen oge sayisi azalmamali.
13. **Progress icin commit ya da dosya basina veritabani guncellemesi yapilmamali.**
    Rutin guncelleme en fazla 500 ms'de bir olmali; terminal geciste son ilerleme yazilmali.

## Yeniden baslatma

14. **API yeniden basladiginda `queued` isler yeniden kuyruga alinmali.**
15. **Onceki surecte `running` kalmis bir is otomatik devam ETMEMELI.** `PROCESS_INTERRUPTED`
    hata koduyla `failed` olmali.
16. **Kurtarma idempotent olmali:** iki kez kosulunca ikinci kosu hicbir satiri
    degistirmemeli.

## API yuzeyi

17. **Is baslatma `202` ve `Location` basligi donmeli.**
18. **Varsayilan worker es zamanliligi 1 olmali.**

## Performans

19. **Polly'nin `risk-score-all` isi Jellyfin'inkinden kisa surmeli.** Polly 2759,
    Jellyfin 22 917 commit; sirf buyukluk farkindan.
20. **Model is boyunca bir kez yuklenmeli**, her commit ya da her batch icin yeniden degil.

## Beklenti yazmadigim yerler

Bu sayilar icin bir beklentim yok; olculecekler ama "tuttu/tutmadi" diye
degerlendirilmeyecekler:

- Static scan ve risk-score-all'in saniye cinsinden suresi.
- Commit/saniye hizi.
- Tepe bellek kullanimi.
- Kuyruk bekleme suresi.
- Toplam progress guncelleme sayisinin tam degeri (yalnizca oge basina olmamasi bekleniyor).
- Iptal isteginden terminal duruma gecise kadar gecen sure.
