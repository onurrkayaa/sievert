# Rapor ve demo olcumu

**Tarih:** 2026-09-13
**Beklentiler:** `docs/olcumler/asama6-rapor-ve-demo-beklenti.md`, kod yazilmadan once yazildi.

Bu dosyanin 1. ve 2. bolumu **olcum kosulmadan once** yazildi ve kendi commit'inde
duruyor. Sebebi: hangi kosuyu kac kez kosacagimi sonuclara bakarak secersem, iyi gorunen
kosuyu secmis olurum.

## 1. Kosulun sabitlenmesi

- Derleme **Release**, `dotnet publish` ciktisindan kosuyor.
- API ayri bir surecte, loopback'te.
- Veritabani ayni makinede, Docker'da (PostgreSQL 17.11).
- Tek istemci.
- Rapor uretimi arka plan isinde; olculen sure **istek atildigi andan** artefakt hazir
  olana kadar.
- Rapor uretilirken ayni anda saglik ucu ve bir risk ucu cagriliyor; onlarin sureleri de
  yaziliyor. Amac: rapor uretimi normal istekleri bekletiyor mu.
- Her rapor ayri bir tekrar anahtariyla isteniyor; ayni artefaktin geri donmesi ayri bir
  olcum.
- Makine: Apple M2, 8 cekirdek, 16 GB, macOS 26.6.2.

## 2. Kosu sayisi (olcumden once yazildi)

Uc deponun uculunde de rapor uretiliyor. Kosu sayilari farkli, cunku Jellyfin'in isi
22 bin satirlik ve her kosu pahali:

| Depo | Kosu |
|---|---|
| Polly | 5 |
| ShareX | 3 |
| Jellyfin | 3 |

Rapor parametreleri butun kosularda ayni ve varsayilan: pencere 200, dosya 50, zaman
cizelgesi 100, commit 20, bulgu 50.

Ayrica birer kez olculecek, tekrarsiz:

- Ayni tekrar anahtariyla ikinci istek (yeni is aciliyor mu, dosya ayni mi).
- Bir bayti degistirilmis artefaktin indirilmeye calisilmasi.
- Kuyruktaki ve kosan bir rapor isinin iptali.
- Tek komutluk demo: **en az 5 temiz kosu**.

Demo kosularinda soguk imaj indirme suresi **ayri** yaziliyor; ana tabloya katilmiyor,
cunku o ag hizina bagli.

## 3. Rapor uretiminin suresi

Cikti: `data/asama6/rapor-performans.json`. Sure, POST istegi ile artefaktin hazir
olmasi arasinda gecen sure; kuyruk beklemesi bunun icinde.

| Depo | Kosu | Kabul (ortanca) | Toplam (ortanca) | En yuksek | Sayfa | Boyut | Komut |
|---|---|---|---|---|---|---|---|
| polly-full | 5 | 8.9 ms | 107.2 ms | 119.0 ms | 8 | 154.9 KB | 138 |
| sharex-full | 3 | 6.8 ms | 86.9 ms | 95.9 ms | 9 | 159.2 KB | 129 |
| jellyfin-full | 3 | 10.3 ms | 103.8 ms | 721.8 ms | 9 | 164.3 KB | 111 |

"Kabul" istegin cevap verme suresi: rapor arka planda uretildigi icin istek beklemiyor.

**Jellyfin'in en yuksek degeri 721.8 ms** ve sebebi biliniyor: o kosu surecin ilk
raporuydu, yani model ve PDF kutuphanesi ilk kez yukleniyordu. Ayni kosuda veritabani
komutu da 444 (digerlerinde 111): acilista bir kez kosan sorgular o ilk olcume giriyor.
Sonraki iki kosu 88.1 ve 98.0 ms.

Beklenti 27 **10 saniye** demisti; olculen en yuksek deger **721.8 ms**, isinmis
durumda en yuksek **119.0 ms**.

## 4. Rapor uretilirken API

Rapor uretilirken ayni anda saglik ucu ve commit listesi cagrildi.

| Depo | Saglik (ortanca) | Saglik (en yuksek) | Commit listesi (ortanca) |
|---|---|---|---|
| polly-full | 3.0 ms | 8.2 ms | 3.9 ms |
| sharex-full | 2.6 ms | 4.5 ms | 3.5 ms |
| jellyfin-full | 3.0 ms | 18.0 ms | 4.9 ms |

Beklenti 16 rapor uretilirken saglik ucunun ortancasi icin **200 ms** demisti; olculen
en yuksek ortanca **3.0 ms**, tek tek olcumlerde en yuksek **18.0 ms**. Yani rapor
uretimi normal istekleri bekletmiyor.

## 5. Tekrar anahtari, bozulma ve iptal

| Olcu | Sonuc |
|---|---|
| Ayni anahtar, ayni govde | Ilk istek **202**, ikinci istek **200** |
| Ayni rapor mu | evet |
| Indirilen dosya bayt olarak ayni mi | evet |
| Ikinci istegin suresi | 4.8 ms (ilk istek 8.1 ms) |

**Bozuk artefakt.** Hazir bir raporun dosyasindaki tek bir bayt degistirildi:

- Indirme **500** dondu, gövde `REPORT_ARTIFACT_CORRUPTED`.
- Kayit `corrupted` durumuna gecti.
- Dosya kullaniciya **gonderilmedi**.

**Iptal.** Rapor isi istek atildiktan hemen sonra iptal edildi:

- Iptal istegi **202**.
- Is **canceled** durumunda bitti, terminal duruma gecme suresi 128.7 ms.
- Rapor kaydi **failed**; ortada hazir bir artefakt kalmadi.

Bu son satir ilk olcumde **tutmuyordu**: is iptal ediliyor ama rapor kaydi sonsuza kadar
`pending` kaliyordu. Olcumde gorundu, duzeltildi ve iki ayri yola da kondu (kuyrukta
iptal ve kosarken iptal). Ayrinti 16. bolumde.

## 6. Tek komutluk demo

Bes temiz kosu, hepsi `--smoke-test` kipinde (acar, kontrol eder, kapatir):

| Kosu | Veritabani | Migration | Import | API | Panel | Toplam | PDF | Cikis |
|---|---|---|---|---|---|---|---|---|
| 1 | 2.7 sn | 0.6 sn | 0.6 sn | 3.8 sn | 2.5 sn | **10.3 sn** | 154.9 KB | 0 |
| 2 | 2.7 sn | 0.6 sn | 0.6 sn | 2.2 sn | 1.8 sn | **7.9 sn** | 154.9 KB | 0 |
| 3 | 2.6 sn | 0.6 sn | 0.6 sn | 2.2 sn | 1.7 sn | **7.8 sn** | 154.9 KB | 0 |
| 4 | 2.6 sn | 0.6 sn | 0.6 sn | 2.2 sn | 1.8 sn | **7.8 sn** | 154.9 KB | 0 |
| 5 | 2.7 sn | 0.6 sn | 0.6 sn | 2.2 sn | 1.8 sn | **7.9 sn** | 155.0 KB | 0 |

Her kosuda duman testi gecti: saglik, panel ana sayfasi, depo listesi, depo sayfasi,
dosya etkinlik ucu, zaman cizelgesi ucu, rapor istegi (202), rapor hazir ve indirilen
PDF'in ozeti metadata ile ayni.

Beklenti 28 **90 saniye** demisti; olculen en yuksek deger **10.3 saniye**.

Ilk kosunun 2.4 saniye uzun olmasinin sebebi: API ve panel ilk kez `dotnet run` ile
derlenip baslatiliyor.

**Soguk imaj indirme dahil degil.** `postgres:17` imaji yereldeydi. Imaj yoksa indirme
suresi ag hizina bagli ve bu tabloya katilmiyor.

**Temizlik.** Bes kosudan sonra:

- Yetim `Sievert.Api` ya da `Sievert.Web` sureci: **0** (kosudan once 0, sonra 0).
- Yetim demo veritabani container'i: **0**.
- Acik kalan tek container Testcontainers'in kendi temizleyicisi (`ryuk`); o bir sonraki
  kosuda yeniden kullaniliyor ve demo verisi tutmuyor.

## 7. Bagimsiz dogrulama

Cikti: `data/asama6/rapor-dogrulama.json`. Uc depo icin uretilen raporun **PDF metni**
ayri bir cikariciyla okundu ve degerler ham tablolardan yeniden hesaplandi.

| Depo | Sayfa | Boyut | Kontrol | Fark |
|---|---|---|---|---|
| jellyfin-full | 8 | 154.0 KB | 42 | **0** |
| polly-full | 7 | 145.9 KB | 41 | **0** |
| sharex-full | 7 | 148.8 KB | 42 | **0** |

Kontrol edilenler: depo kimligi, model profili, manifest ozeti (hem metadata hem PDF),
kapsanan commit sayisi, ortalama/medyan/en yuksek endeks, iki esige gore karar sayilari,
en yuksek endeksli 10 commit'in kisa sha'si **ve sirasi**, ilk bes dosyanin yolu ve
ortalamasi, ilk/son tarih, statik bulgu sayisi, kural dagilimi, uc zorunlu cumle ve
yasakli ifade taramasi.

## 8. Olculmeyenler

- **Es zamanli rapor istegi.** Worker es zamanliligi 1; ayni anda iki rapor istenirse
  ikincisinin ne kadar bekledigi olculmedi.
- **Cok daha buyuk bir rapor.** En buyuk olculen 10 sayfa / 194 KB (sinir raporu).
  20 MB sinirina yaklasan bir rapor uretilmedi.
- **Tepe bellek.** Rapor uretiminin bellegi ayri olculmedi.
- **Uzaktaki veritabani ve cok kullanicili demo.**
- **Soguk imaj indirme.**
