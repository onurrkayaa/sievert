# Gorsellestirme beklentileri

**Beklentiler kod, sorgu ve hicbir gorsel sonuc uretilmeden once yazildi.**

**Tarih:** 2026-09-13
**Durum:** Adim 5 (dosya etkinlik haritasi ve risk zaman cizelgesi) yazilmadan once
yazildi. Bu dosya sonradan degistirilmeyecek.

Panel Adim 4'te sayilari tablo olarak gosteriyordu. Bu turda ayni sayilar renk ve
koordinata cevriliyor, yani okuyan kisi sayiya bakmadan da bir sey "anliyor". Gorsel
kodlama yanlis oldugunda kimse hata mesaji gormuyor; o yuzden beklentilerin yarisi
gorselin sayiyla tutarli olmasi hakkinda.

Asagidakiler tahmin. Tutmayanlar olcum dosyasinda "tutmadi" diye yazilacak, beklenti
geriye donuk duzeltilmeyecek.

## On-isleme ve istek sayisi

1. **`PersistentComponentState` ile on-isleme ve etkilesimli gecis arasinda ayni sayfa
   verisi bir kez cekilmeli.** Adim 4'te iki kez cekiliyordu.
2. **Durum anahtari route ve sorgu parametrelerini icermeli;** farkli filtrede eski veri
   kullanilmamali.
3. **Grafik veri uclari mevcut sayfa acilislarini belirgin yavaslatmamali.**

## API sureleri

4. **Dosya haritasi ucu isinmis durumda Polly ve ShareX'te 100 ms, Jellyfin'de 300 ms
   altinda olmali.**
5. **Zaman cizelgesi ucu 100 nokta icin isinmis durumda 100 ms altinda olmali.**

## Varsayilanlar

6. **Varsayilan commit penceresi 200 olmali.**
7. **Varsayilan zaman cizelgesi 100 commit olmali.**

## Gorsel kodlamanin sayiyla tutarliligi

8. **Dosya hucrelerinin renk sirasi `meanRiskIndex` ile monoton olmali.** Daha yuksek
   endeks, renk olceginde geri gitmemeli.
9. **Zaman cizelgesinin Y koordinati `RiskIndex` ile monoton olmali.**
10. **Ayni `RiskIndex` her kulturde ayni CSS/SVG degerini uretmeli.** Adim 4'te katki
    cubugunda tam bunun tersi oldu.
11. **Harita ve zaman cizelgesi sayilari ham veriye karsi 0 fark vermeli.**

## Urun dili ve ayrim

12. **Kismi bir isin haritasi ve zaman cizelgesi tam sonuc gibi sunulmamali.**
13. **Statik bulgu sayisi renk hesabina girmemeli.**
14. **`RiskIndex` ile `RawModelScore` ayni eksende karistirilmamali.**
15. **Zaman cizelgesinde kaynak esigi ve 0,5 esigi ayri referans cizgileri olarak
    gorunmeli.**

## Arayuz

16. **Mobil gorunumde grafikler kirpilmamali;** gerekirse kontrollu yatay kaydirma olmali.
17. **Ilk anlamli tarayici boyamasi yerel Release kosulunda 1500 ms altinda olmali.**

## Beklenti yazmadigim yerler

Bu sayilar olculecek ama "tuttu/tutmadi" diye degerlendirilmeyecek:

- Dosya haritasi ve zaman cizelgesi uclarinin donen byte boyutu.
- Sorgu basina veritabani komut sayisi (sayilacak, hedefi yok).
- Tarayicinin grafigi cizme suresi ile veriyi bekleme suresinin orani.
- Es zamanli birden fazla kullanicinin etkisi.
- Cok daha buyuk bir depoda (100 bin commit) davranis.
