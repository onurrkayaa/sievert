# Gorsellestirme olcumu

**Tarih:** 2026-09-13
**Beklentiler:** `docs/olcumler/asama6-gorsellestirme-beklenti.md`, kod yazilmadan once yazildi.
**Ureten kod:** `91a83f7`.

Bu dosyanin 1. ve 2. bolumu **olcum kosulmadan once** yazildi ve kendi commit'inde
duruyor; sonuclar sonraki commit'te eklendi. Sebebi basit: hangi kombinasyonlari olctugumu sonuclara bakarak
secersem, iyi gorunen kombinasyonlari secmis olurum.

## 1. Kosulun sabitlenmesi

- Derleme **Release**, `dotnet publish` ciktisindan kosuyor.
- API ve panel **ayri sureclerde**, ikisi de loopback'te.
- Veritabani ayni makinede, Docker'da (PostgreSQL 17.11).
- Tek istemci.
- Uc sureleri icin **tarayici yok**: tek bir `HttpClient` istegi atiyor.
- Tarayici olcumu ayri kosuyor ve orada **soguk ve isinmis onbellek ayri** yaziliyor.
- Her kombinasyon icin **5 kosu**. Ortanca ve 5 kosudaki **en yuksek** deger yaziliyor;
  p95 yazmiyorum, 5 kosudan p95 hesaplamak olculmemis bir kesinlik iddiasi olurdu.
- Her depo icin bir **isinma istegi** var ve sonuclara girmiyor.
- Yalniz **tamamlanmis** (kismi olmayan) risk isleri olculuyor.
- Ekran goruntusu kosusu ayri; bu kosuda goruntu alinmiyor.
- Makine: Apple M2, 8 cekirdek, 16 GB, macOS 26.6.2.

## 2. Matris secimi (olcumden once yazildi)

Butun carpimi kosmak - 3 depo x 5 pencere x 4 limit x 6 siralama x 5 kosu - binlerce
istek ederdi. Onun yerine su temsilci matris secildi:

**Dosya etkinlik haritasi.** Uc depo (Polly, ShareX, Jellyfin) x

- pencere **50 / 200 / 1000**, limit varsayilanda (100) sabit
- limit **50 / 100 / 200**, pencere varsayilanda (200) sabit

Yani pencere ve limit ayri ayri gezdiriliyor, ikisi birden degil. Sebebi: sorgu
maliyetinin pencereye, cevap boyutunun limite bagli oldugunu bekliyorum; carpimi
kosmadan ikisini ayri gormek yeterli. Siralama sabit (`mean-risk-desc`, varsayilan)
cunku siralama bellekte yapiliyor, sorguyu degistirmiyor.

**Zaman cizelgesi.** Uc depo x nokta sayisi **50 / 100 / 500** (en kucuk secim,
varsayilan, en buyuk secim).

**Panel sayfalari.** Ilk depo icin harita ve zaman sekmesinin HTML suresi.

Bu secim degistirilmeyecek; sonuclar bu matrisin sonuclari olarak yaziliyor.
