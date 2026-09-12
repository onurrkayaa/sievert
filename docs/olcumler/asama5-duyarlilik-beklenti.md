# Adim 4 duyarlilik deneyleri beklentileri

**Beklentiler olculmeden once yazildi.**

**Tarih:** 2026-09-12
**Durum:** Hicbir duyarlilik deneyi kosturulmadan yazildi.

Bu deneylerin hicbiri ana modeli ya da ana sonucu degistirmiyor. Her biri ayri bir
"ya soyle olsaydi" sorusu.

## Bot duyarliligi

1. **Botlari cikarmak Polly'yi digerlerinden daha cok etkileyecek.** Polly commit'lerinin
   %31,0'i bot bayrakli (854 / 2759), Jellyfin'de %4,0 (928 / 22 917), ShareX'te 0.
   ShareX'te hicbir sey degismemeli.

2. **Yalniz degerlendirme filtresi (B) ile yeniden egitim (C) arasindaki fark kucuk
   olacak.** Asama 4'te olculdu: botlari cikarmak bot disi commit'lerin yalnizca
   %19,5'inin metriklerini degistiriyordu ve `AuthorCommitCount` hic degismemisti.

## Sag sansur / 90 gun olgunluk

3. **Olgun alt kumede pozitif oran ana testten yuksek olabilir.** Sansurlu satirlarin bir
   kismi henuz suclanmamis olabilir; onlari cikarinca kalan kumede pozitif orani yukselir.

4. **90 gunluk filtre en cok Polly'yi degistirecek.** Polly'nin testinde 90 gunden az
   olgun 99 satir var ve hicbiri pozitif degil; 828 satirlik bir kumede 99 satir
   cikarmanin etkisi, Jellyfin'in 6876 satirinda 559 satir cikarmaktan buyuk olur.

## C# etiket uygunlugu

5. **`CsFilesChanged` kaldirilinca model performansi dusecek.** Uc repoda da en buyuk ya
   da ikinci buyuk mutlak katsayiydi.

6. **Dususun ne kadarinin gercek sinyal, ne kadarinin etiketleme kapsamindan geldigi
   bilinmiyor.** SZZ yalnizca silinen `.cs` satirlarini blame ediyor, yani hic `.cs`
   dosyasina dokunmayan bir commit pozitif etiket **alamiyor**. Bu deney iki kaynagi
   tamamen ayiramaz ve ayirdigini iddia etmeyecegim.

7. **Yalniz `CsFilesChanged > 0` commit'lerde degerlendirme daha zor olacak.** O alt
   kumede "hic `.cs` degistirmeyen commit kesin negatif" kolayligi ortadan kalkiyor;
   F1 ve PR-AUC'nin tum veri sonucunun altinda kalmasini bekliyorum.

## Train araligi disi degerler

8. **ShareX'in yuksek aralik disi orani aktarimi zayiflatabilir.** Adim 3'te olculdu:
   ShareX'te test satirlarinin %94,7'si en az bir ozniteligiyle egitim araliginin
   disinda. Bu, aralik disi satirlarin ana modelde daha kotu sonuc verecegi anlamina
   gelmek zorunda degil; iki grubun karsilastirmasi olculecek.

## Ad degisimi

9. **Ad degisimi karari tarihsel metrikleri ve model sonucunu degistirecek; yonu
   bilinmiyor.** Asama 4'te olculdu: Polly'de ad degisimi iceren commit orani %2,9 ama
   metrikleri etkilenen commit orani %44,7. Model sonucunun hangi yone gidecegi hakkinda
   beklenti yazmiyorum.

10. **Threshold (40/50/60) duyarliligi icin sonuc beklentisi yazmiyorum.** Once teknik
    olarak yapilabilir mi, o belirlenecek.

## Sentetik etiket gurultusu

11. **Flip orani arttikca F1 ve PR-AUC dagilimlari bozulacak.** %5'ten %20'ye giderken
    metriklerin dusmesini ve dagilimlarin genislemesini bekliyorum.

12. **Bu deney gercek etiketleri tahmin etmiyor.** %5 / %10 / %20 oranlari gercek kacirma
    orani iddiasi **degil**. Asama 4'un 11 / 13 (%84,6) orani hedefli secilmis bir
    orneklemden geldi ve populasyona genellenemez; bu deneyde kullanilmayacak.

## Beklenti yazmadigim yerler

- Hangi deneyin metrikleri en cok degistirecegi (bot, olgunluk, ablasyon arasinda).
- Ad degisimi zincir etkisinin model F1'ini hangi yone tasiyacagi.
- Sentetik gurultude esik dagiliminin nasil kayacagi.
