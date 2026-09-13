# Panel temel olcumu

**Tarih:** 2026-09-13
**Beklentiler:** `docs/olcumler/asama6-panel-oncesi-beklenti.md`, kod yazilmadan once yazildi.
**Ureten kod:** `7f4c8e5`. **Cikti:** `data/asama6/panel-temel.json`, `data/asama6/panel-polling.json`.

Bu dosyadaki sayilar **API'nin Debug olcumleriyle ayni tabloya konmuyor**. Kosullar
farkli: burada Release yayim ciktisi kosuyor, orada Debug derlemesi.

## 1. Kosulun sabitlenmesi

- Derleme **Release**, ve `dotnet publish` ciktisindan kosuyor. `bin/Release` altindan
  dogrudan kosarken statik varliklar sunulamiyor; ilk denemede tam bunu yasadim ve
  olctugum sayfa gercek sayfa degildi.
- API ve panel **ayri sureclerde**, ikisi de loopback'te.
- Veritabani ayni makinede, Docker'da (PostgreSQL 17.11).
- Tek istemci.
- Sayfa sureleri icin **tarayici yok**: tek bir `HttpClient` sayfayi istiyor. Blazor
  on-isleme yaptigi icin ilk cevabin govdesi zaten sayfanin icerigi.
- **Tarayici onbellegi kullanilmiyor**; her kosu yeni bir istek.
- Her sayfa icin **5 kosu**. Asagida hem ortanca hem 5 kosudaki **en yuksek** deger var;
  p95 yazmiyorum, 5 kosudan p95 hesaplamak olculmemis bir kesinlik iddiasi olurdu.
- Ilk kosu soguk: model onbellegi bos ve JIT isinmamis. Ayri sutunda duruyor.
- Makine: Apple M2, 8 cekirdek, 16 GB, macOS 26.6.2.

## 2. Sayfa sureleri

| Sayfa | Ilk kosu (soguk) | Ortanca | 5 kosuda en yuksek | API istegi | HTML | En yavas API cagrisi |
|---|---|---|---|---|---|---|
| Genel bakis | 21.8 ms | 18.3 ms | 23.3 ms | 7 | 12.7 KB | 6.3 ms |
| Repolar | 9.7 ms | 3.7 ms | 9.7 ms | 2 | 9.4 KB | 5.4 ms |
| Depo ayrintisi | 49.0 ms | 16.9 ms | 49.0 ms | 4 | 25.4 KB | 31.5 ms |
| Analiz isleri | 15.3 ms | 10.9 ms | 15.3 ms | 3 | 20.4 KB | 4.2 ms |
| Is ayrintisi | 30.6 ms | 6.5 ms | 30.6 ms | 3 | 27.7 KB | 17.4 ms |
| Commit riski | 139.0 ms | 6.5 ms | 139.0 ms | 2 | 22.0 KB | 130.0 ms |
| Modeller | 6.9 ms | 4.2 ms | 6.9 ms | 3 | 12.5 KB | 4.0 ms |
| Sistem durumu | 7.6 ms | 5.3 ms | 7.6 ms | 2 | 9.8 KB | 4.9 ms |

Beklenti 9 **1500 ms** diyordu; olculen en yuksek deger **139.0 ms**.
Beklenti 10 isinmis gecisler icin **500 ms** diyordu; en yuksek ortanca
**18.3 ms**.

Dikkat ceken tek sayi commit riski sayfasinin ilk kosusu: **139.0 ms**,
sonraki kosularda **6.5 ms**. Aradaki fark modelin ilk kez
diskten okunmasi; ayni buyukluk bellek olcumunde de cikmisti (model yuklemesi 149-167 ms,
`docs/olcumler/asama6-bellek-ayristirma.md` bolum 2).

Genel bakis sayfasi 7 API istegi atiyor: saglik, model listesi, depo listesi ve her depo
icin son isler. Bu sayfa icin bir beklenti yazmamistim; kayda geciyorum.

## 3. Durum sorma dongusu

Kosul: gercek bir tarayici, calisan bir is, sayfa **30 saniye** acik, sonra baska bir
sayfaya gecis. Sayilar panelin kendi gunlugundeki istek satirlarindan.

| Olcu | Deger |
|---|---|
| Toplam is durumu istegi | **18** |
| Ayni anda acik en fazla istek | **1** |
| Ortanca aralik | **1004 ms** |
| En kisa aralik | 134 ms |
| En uzun aralik | 1016 ms |
| Is terminal duruma gectikten sonraki istek | **0** |
| Sayfadan ayrildiktan sonraki istek | **0** |

Ortanca aralik **1004 ms**, yani beklenen bir saniye.

**En kisa aralik 134 ms** ve bunun sebebi biliniyor: Blazor Web App sayfayi once
sunucuda on-isliyor, sonra devre baglaninca bileseni yeniden olusturuyor. Iki olusturma
da `OnParametersSetAsync` icinde durumu bir kez soruyor, yani sayfa acilisinda **iki**
istek gidiyor. Bu bir kusur degil ama bir maliyet; `PersistentComponentState` ile
onlenebilir, bu turda yapilmadi.

Es zamanli istek sayisi **1**: dongu zamanlayici degil, tek bir dongu; bir sonraki istek
oncekinin cevabi gelmeden baslamiyor.

Isin terminal duruma gectigi an ile son istegin ani:

- Is `completedAtUtc`: 07:26:21.487
- Son is durumu istegi: 07:26:21.763
- Sayfa acik kaldigi son an: 07:26:45 (24 saniye daha)
- Bu 24 saniyede giden istek: **0**

Yani dongu isin bittigini gordugu istekte duruyor, sonrasinda hic sormuyor.

## 4. Gorsel kontrol

1440x900 ve 390x844 gorunumlerinde her sayfa acilip bakildi. Kontrol edilenler ve sonuc:

| Kontrol | Sonuc |
|---|---|
| Yatay sayfa tasmasi | yok (ikisinde de `scrollWidth == clientWidth`) |
| Kirpilan metin | yok; genis tablolar kendi kutularinda kayiyor |
| Ust uste binen kart | yok |
| Odak gorunurlugu | var; gezinme sonrasi odak h1'e gidiyor ve cerceve goruluyor |
| Renk disinda durum metni | her durum rozetinde metin var |
| Konsol hatasi | yok (yayim ciktisindan kosarken) |
| Basarisiz ag istegi | yok |
| "hata olasiligi" yasakli ifadesi | yalniz olumsuzlayan uyari metinlerinde |
| Ham skorda yuzde isareti | yok |
| LocalPath / baglanti dizesi sizintisi | yok |

Ekran goruntuleri `docs/images/asama6/` altinda.

## 5. Gorsel kontrolde bulunan kusur

Katki cubuklarinin genisligi kultura bagli bicimleniyordu. Sunucunun kulturu virgullu
ondalik kullandiginda satira `width:67,8%` yaziliyor, tarayici bunu gecersiz sayiyor ve
`span` kutusunu tamamen dolduruyordu. Sonuc: **0,4322 ile 0,0256 katkinin cubugu ayni
uzunlukta gorunuyordu.**

Ekran goruntusune bakarken fark edildi, tarayicidan `style` degerleri okunarak dogrulandi
ve degismez kulturle bicimlendirilerek duzeltildi. Regresyon testi
`tests/Sievert.Web.Tests/PanelTests.cs` icinde.

Sayinin kendisi her satirda isaretiyle yazili oldugu icin yanlis bilgi verilmiyordu; ama
cubuk goze yanlis bir siralama gosteriyordu.

## 6. Olculmeyenler

- **Tarayicidaki ilk boyama (paint) suresi.** Olculen sey sunucunun HTML'i uretme suresi;
  tarayicinin onu cizmesi buna dahil degil.
- **Aktarilan toplam byte.** Yalniz HTML govdesi sayildi; statik varliklar (CSS, Blazor
  betikleri) sayilmadi.
- **Cok sayida es zamanli kullanici.** Tek istemci, tek devre.
- **Uzaktaki veritabani.**
- **Hata sonrasi geri cekilme araliklari gercek kosuda.** Merdiven (1-2-5 sn) birim
  testiyle sinandi, gercek bir API kesintisiyle olculmedi.
