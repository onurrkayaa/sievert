# Demo senaryosu

**Sure:** 5-7 dakika.
**Amac:** aracin ne yaptigini ve neyi **yapmadigini** gostermek.

Demo verisi kamuya acik Polly gecmisinden alinmis sabit bir alt kume. Canli bir depo
analizi degil; secim kurali `data/asama6/demo/README.md` icinde yaziyor.

## Baslamadan once

Gerekenler: .NET 10 SDK, calisan Docker, `postgres:17` imaji yerelde. Bu ucu varsa
internet gerekmiyor.

```bash
dotnet run --project tools/Sievert.Demo
```

Komut sirayla: PostgreSQL container'i acar, migration uygular, demo verisini yazar,
API'yi ve paneli baslatir, saglik uclarini bekler, adresleri yazar ve tarayiciyi acar.
Olculen hazir olma suresi 7,8-10,3 saniye (`docs/olcumler/asama6-rapor-ve-demo.md`).

Kapatmak: Ctrl+C. Panel, API ve container sirayla kapaniyor.

## Akis

### 1. Tek komut

**Tikla:** terminalde komutu calistir.
**Soyle:** "Tek komutla veritabani, API ve panel aciliyor; veri sabit ve kaynagi kayitli."
**Beklenen ekran:** adimlar ve sureleri, sonra panel ve API adresleri.
**Sorun olursa:** Docker kapaliysa arac bunu tek cumleyle soyluyor. Port doluysa
`--api-port` ve `--web-port` ile ver.

### 2. Genel bakis

**Tikla:** panel adresi.
**Soyle:** "Panel API'den okuyor; kendi hesabini yapmiyor."
**Beklenen ekran:** depo sayisi, model profilleri, son isler.
**Sorun olursa:** tarayici otomatik acilmazsa adresi elle yaz.

### 3. Demo kaynagi uyarisi

**Tikla:** depo sayfasi.
**Soyle:** "Bu veri sabit bir alt kume; bunu ekranda da yaziyoruz."
**Beklenen ekran:** basligin altinda "Sabit demo veri kumesi" kutusu.

### 4. Risk zaman cizelgesi

**Tikla:** "Risk Zaman Cizelgesi" sekmesi.
**Soyle:** "Dikey eksen goreli endeks, yatay eksen tarih. Iki cizgi iki ayri karar
esigi - biri modelin ham skorunun orta noktasi, digeri egitimde secilen esik."
**Beklenen ekran:** SVG cizelge, iki kesikli cizgi ve etiketleri.

### 5. Dosya etkinlik haritasi

**Tikla:** "Dosya Etkinligi" sekmesi.
**Soyle:** "Renk dosyanin kusurlu oldugunu **gostermiyor**. Secilen pencerede o dosyaya
dokunan commit'lerin endekslerinin ortalamasi."
**Beklenen ekran:** hucreler, her hucrede sayi, altta efsane.

### 6. Tek commit ayrintisi

**Tikla:** bir hucre, sonra listeden bir commit.
**Soyle:** "Ham skor, goreli endeks ve iki karar ayri ayri duruyor; hicbiri tek bir
'risk' sayisinda birlestirilmiyor."
**Beklenen ekran:** dort ayri kart ve zorunlu uyarilar.

### 7. Statik bulgularin ayriligi

**Tikla:** ayni sayfada statik bulgu bolumu.
**Soyle:** "Bunlar kural tabanli bulgular ve model skoruna dahil **degil**. Kurallardan
ikisinin precision'i sifir olctugu icin varsayilan olarak kapali."
**Beklenen ekran:** ayri bir kart ve "model skoruna dahil degil" cumlesi.

### 8. Arka plan isi ve iptal

**Tikla:** "Analizler" sekmesi, calisan bir is varsa iptal dugmesi.
**Soyle:** "Uzun isler istek disinda kosuyor; iptal edilen bir isin kismi sonucu tam
sonuc gibi sunulmuyor."
**Beklenen ekran:** ilerleme, iptalden sonra kismi uyarisi.
**Not:** demo verisinde isler zaten tamamlanmis geliyor; istersen yeni bir statik tarama
baslatip iptal edebilirsin.

### 9. PDF raporu olusturma

**Tikla:** "Rapor" sekmesi, formu birak, "Rapor olustur".
**Soyle:** "Rapor arka planda uretiliyor ve kaynaklari kendi icinde yaziyor."
**Beklenen ekran:** rapor sayfasi, once 'Uretiliyor', sonra 'Hazir'.
**Sorun olursa:** uzarsa `docs/demo/sievert-polly-ornek-rapor.pdf` hazir duruyor.

### 10. PDF indirme

**Tikla:** "PDF indir".
**Soyle:** "Dosya verilmeden once ozeti yeniden dogrulaniyor; tutmazsa gonderilmiyor."
**Beklenen ekran:** 8 sayfalik PDF; kapakta 'Kalibre edilmedi' ve 'Bu rapor kesin kusur
karari degildir'.

### 11. Raporun sinirliliklar bolumu

**Tikla:** PDF'in sonlarina git.
**Soyle:** "Raporun son iki bolumu sinirliliklar ve kaynak. Manifest ozeti burada; ayni
veriyle uretilen rapor ayni ozeti veriyor."
**Beklenen ekran:** on maddelik sinirliliklar listesi ve ozet tablosu.

### 12. Kapanis

**Soyle:** "Arac bir dosyanin hatali oldugunu soylemiyor. Neye once bakilacagina dair
deneysel bir siralama veriyor ve her sayinin nereden geldigini yaziyor."

## Soylenmeyecekler

- "Model hatalari buluyor."
- "Yuzde X ihtimalle hata var."
- "Bu dosya hatali."
- "Butun C# projelerinde calisir."

Bunlarin hicbiri olculmedi. Olculen sey `docs/raporlar/asama5-kapanis.md` ve
`docs/sinirliliklar.md` icinde.

## Yedek plan

| Sorun | Ne yapilir |
|---|---|
| Tarayici acilmiyor | Terminalde yazan adresi elle ac |
| Port dolu | `--api-port 5101 --web-port 5102` |
| Docker kapali | Arac tek cumleyle soyluyor; Docker Desktop'i ac |
| Rapor uzuyor | `docs/demo/sievert-polly-ornek-rapor.pdf` dosyasini goster |
| Demo hic acilmiyor | Ekran goruntuleri `docs/images/asama6/` altinda |
