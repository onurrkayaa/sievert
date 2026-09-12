# Asama 6 Adim 0-2 beklentileri

**Beklentiler olculmeden once yazildi.**

**Tarih:** 2026-09-12
**Durum:** API yazilmadan ve hicbir sure olculmeden yazildi. Bu dosya sonradan
degistirilmeyecek.

## Performans

1. **Isinmis risk istegi 100 ms'in altinda donmeli** — veritabani yerel ve model
   bellekteyken.
2. **Ilk model yukleme bundan yavas olabilir** ve **ayri olculecek**; isinmis sayilarla
   ayni tabloda toplanmayacak.
3. **Model dosyalari her istekte yeniden yuklenmemeli.** Olcumde modelin gercekten bir kez
   yuklendigi sayilacak.

## Sorgu ve sayfalama

4. **EF sorgulari `AsNoTracking` olmali.**
5. **Commit listesi sayfali olmali.**
6. **Varsayilan sayfa boyutu 25, en fazla 100.**

## Hata davranisi

7. **Bilinmeyen repo icin model istegi `422` donmeli.**
8. **Eksik commit `404`.**
9. **Bozuk model ya da checksum uyusmazligi**, uygulama baslangicinda veya ilk model
   erisiminde **acik hata** vermeli; sessizce devam etmemeli.

## Dogruluk

10. **Aciklama katkilarinin toplami model logit degeriyle tolerans icinde eslesmeli**
    (mutlak fark <= 1e-6).
11. **`RiskIndex` monoton olmali:** daha yuksek `RawModelScore` daha dusuk `RiskIndex`
    uretmemeli.
12. **Statik bulgu sayisi `RawModelScore`'u degistirmemeli.**

## Beklenti yazmadigim yerler

- Response boyutunun ne kadar olacagi.
- Tepe bellek kullanimi.
- Veritabani sorgu sayisinin tam degeri (yalnizca N+1 olmamasi bekleniyor).
- Ilk yukleme suresinin sayisal bandi.
