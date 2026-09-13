# Asama 6 yol haritasi: API ve panel

**Tarih:** 2026-09-13
**Durum:** Adim 0, 1, 2, 3, 3b, 4, 5 ve 6 yapildi. Adim 7 **yapilmadi**.

Asama 5 bir model birakti ve o modelin ne olup ne olmadigini olctu. Asama 6 o modeli
kullanilabilir hale getiriyor. Isin buyuk kismi teknik degil: **modelin ne soyleyip ne
soylemedigini urun dilinde dogru tutmak.**

## Asama 5'ten devralinan riskler

Her adimda bu besi tasiyoruz:

1. **Skor kalibre degil.** Uretim icin kalibrator secilmedi (ADR 0019, ADR 0022). Skor
   "hata olasiligi" diye sunulamaz.
2. **Hedef SZZ.** Butun metrikler otomatik bir etikete karsi olculdu; etiketin kendisi
   dogrulanmadi.
3. **Etiket kapsami dar.** `CsFilesChanged = 0` olan 8607 / 34 166 commit'in pozitifi 0;
   veri kumesinin %25,2'si tasarim geregi pozitif etiketlenemiyor.
4. **Repo-arasi aktarim tutarsiz.** Tek kaynakli alti yonun 3'u ayni-repo F1'inin ustunde,
   3'u altinda; genel bir model secilmedi.
5. **Insan dogrulamasi sinirli.** 30 ornekten 25'i karara baglandi; model-pozitif
   precision isareti 1 / 14, populasyonu temsil etmiyor.

## Adimlar

### 0. Urun dili, risk sozlesmesi ve mimari kararlar — **bu turda yapildi**

**Amac.** Modelin ciktisinin urun dilinde nasil adlandirilacagini, hangi ifadelerin
yasak oldugunu ve hangi alanlarin API sozlesmesinde bulunacagini sonuc gormeden sabitlemek.

**Uretilen.** `docs/urun/risk-sozlesmesi.md`, `docs/olcumler/asama6-beklenti.md`, bu
dosya, ADR 0023.

**Basari olcutu.** Yasak ifade listesi ve izin verilen ifade listesi yazili; `RiskIndex`,
`RawModelScore`, `IsCalibrated` ve `CombinedRisk` tanimlari belirsizlik birakmiyor.

**Yapilmayanlar.** Kod yok.

### 1. Read-only API ve model kayit defteri — **bu turda yapildi**

**Amac.** Mevcut uc modeli checksum dogrulamasiyla yukleyip repo ve commit verisini
salt-okunur olarak sunmak.

**Uretilen.** `src/Sievert.Api`, `ModelRegistry`, `/api/v1/health`, `/api/v1/models`,
`/api/v1/repositories`, `/api/v1/repositories/{id}`,
`/api/v1/repositories/{id}/commits`.

**Basari olcutu.** Uc profil checksum dogrulamasindan geciyor, model istek basina
yeniden yuklenmiyor, butun hatalar `ProblemDetails` + `errorCode` donuyor, baglanti
dizesi ve dosya yolu cevaba sizmiyor.

**Olculen.** `docs/olcumler/asama6-api-temel.md`. Ilk istek 278 ms, sonrakilerin medyani
3,8 ms; yani model bir kez yukleniyor.

**Yapilmayanlar.** Yazma islemi yok, arama yok, statik tarama endpoint'i yok.

### 2. Commit risk aciklamasi — **bu turda yapildi**

**Amac.** Tek bir commit icin ham model skoru, iki karar, goreli endeks, oznitelik
katkilari ve uyarilar.

**Uretilen.** `data/asama6/model-score-reference.json`,
`/api/v1/repositories/{id}/commits/{sha}/risk`, katki hesabi ve uyari sistemi.

**Basari olcutu.** Katki toplami modelin logit'iyle 1e-6 tolerans icinde esliyor;
`RiskIndex` monoton; uc zorunlu uyari her cevapta; `CsFilesChanged = 0` commit'lerde
kapsam uyarisi.

**Olculen.** `RiskIndex` 34 166 satirda monoton, 0 ihlal. Uc zorunlu uyari 300 istegin
hepsinde cikti. Kapsam uyarisi tam olarak 50 / 50 dogru yerde cikti. **Tolerans tutmadi:**
34 166 satirin 11'inde 1e-6 asildi, en buyuk fark 1,783e-6. Tolerans degistirilmedi;
ayrinti ve acik karar `docs/olcumler/asama6-api-temel.md` bolum 7'de.

**Yapilmayanlar.** Statik analiz calistirilmiyor, birlesik skor yok, bilinmeyen repo
skorlanmiyor.

### 3. Arka plan analiz isleri ve iptal — **yapildi**

**Amac.** Uzun suren islemleri istek disina tasimak; ilerleme ve iptal.

**Uretilen.** Uc tablo (`AnalysisJobs`, `StaticAnalysisFindings`, `CommitRiskSnapshots`),
durum makinesi, sinirli kapasiteli kuyruk, worker, yeniden baslatma kurtarmasi, iki
**gercek** is turu (`static-scan`, `risk-score-all`), alti uc ve iptal.

**Basari olcutu.** Bir is baslatilip iptal edilebiliyor, durum sorgulanabiliyor, API
istegi bloke olmuyor.

**Olculen.** `docs/olcumler/asama6-arka-plan-isleri.md`. Yirmi beklentinin yirmisi tuttu:
static-scan uc repoda da CLI ile 0 fark, risk-score-all 300 commit'te risk ucuyle bit
duzeyinde ayni, 22 917 commit'lik iste 18 ilerleme yazimi ve 1 model yuklemesi.

**Ayrica bu turda.** Aciklama toleransi surum 2.0'a cekildi
(`docs/urun/model-aciklama-sayisal-tolerans.md`), risk sozlesmesi surum 1.1 oldu,
`?profile=` kaldirildi ve uzak erisim kapatildi.

**Yapilmayanlar.** Git klonlama, tarih madenciligi, metrik hesabi ve SZZ etiketleme is
olarak yazilmadi. Dagitik kuyruk, is onceligi, zaman asimi ve otomatik yeniden deneme yok.

**Devralinan risk.** Uzun isler model dosyalarina ve veritabanina ayni anda dokunuyor;
dondurulmus kanit dosyalari bu islerden **etkilenmedi** - is sonuclari kendi tablolarinda
duruyor ve Asama 5/6 kanit dosyalari yalnizca okunuyor.

### 3b. Panel oncesi sertlestirme — **bu turda yapildi**

**Amac.** Adim 3'ten kalan uc borcu panelden once kapatmak: static taramanin neyi
taradigi kayitli degildi, bellegin nereden geldigi ayristirilmamisti ve es zamanlilik
yalniz 1 ile olculmustu.

**Uretilen.** Is kaydinda kaynak durumu (HEAD, calisma agaci, dogrulama), temiz agac
sarti, iki worker ornegiyle tekillik ve dis surecten iptal testleri, taze sureclerde
bellek olcumu, es zamanlilik 1/2 karsilastirmasi.

**Basari olcutu.** Kirli agacta tarama baslamiyor; tarama sirasinda depo degisirse is
basarili sayilmiyor; ikinci bir worker ayni isi ikinci kez kosturmuyor.

**Olculen.** `docs/olcumler/asama6-kaynak-durumu.md` ve
`docs/olcumler/asama6-bellek-ayristirma.md`. Ayni HEAD'de yedi kosu ayni bulgu kumesini
verdi; kirli agacta tarama 386 ms'de 0 bulguyla reddedildi. Taze surecte en yuksek deger
504,5 MB (onceki 1267 MB alti isin ayni surecte kosmasindan geliyormus), model yuklemesi
16 MB, takipci her zaman obek boyutunda.

**Yapilmayanlar.** Es zamanlilik 3 ve 4 gercek repolarla olculmedi; tepe bellegin
Roslyn / EF / ML.NET paylari ayrilmadi.

### 4. Blazor panel iskeleti — **yapildi**

**Amac.** Repo listesi, commit listesi ve tek commit risk goruntusu.

**Uretilen.** `src/Sievert.Contracts`, `src/Sievert.Web`, sekiz sayfa, tiplenmis API
istemcisi, ADR 0025.

**Basari olcutu.** Panel API'den okuyor, kendi hesabini yapmiyor - referans yok, yani bunu
derleyici koruyor. Uyarilar ve "kalibre edilmedi" rozeti her degerlendirmede ekranda.

**Olculen.** `docs/olcumler/asama6-panel-temel.md`. Sayfa sureleri Release'te 4-139 ms;
en yuksek deger commit riski sayfasinin soguk ilk kosusu. Durum sorma dongusu 30 saniyede
18 istek, ortanca aralik 1004 ms, ayni anda acik en fazla 1 istek, terminal durumdan
sonra 0 istek.

**Yapilmayanlar.** Grafik kutuphanesi, isi haritasi, zaman cizelgesi, PDF ve kimlik
dogrulama yok. Sayfa acilisinda veri iki kez cekiliyor (on-isleme + devre); bu Adim 5'te
kapatildi.

**Devralinan risk.** Arayuzde yuzde isareti gormek, skoru olasilik sanmaya en kolay yol.
Risk sozlesmesindeki dil arayuzde de gecerli.

### 5. Dosya etkinlik haritasi ve risk zaman cizelgesi — **bu turda yapildi**

**Amac.** Dosya bazinda yogunluk ve zaman icinde risk egrisi.

**Uretilen.** Iki yeni uc (`visualizations/file-activity`, `visualizations/risk-timeline`),
tek merkezi renk/koordinat olcegi, harita ve cizelge bilesenleri, arac cubugu, efsane,
kismi sonuc banner'i, bagimsiz dogrulama araci (`measure visualization-truth`), ADR 0026.
Ayrica `PersistentComponentState` eklendi ve Adim 4'ten kalan cift veri cekme kapatildi.

**Basari olcutu.** Gorsellestirme, altindaki sayinin ne oldugunu (goreli endeks)
gizlemiyor: her hucrede endeksin sayisi yazili, efsanede goreli oldugu yazili, yuzde
isareti yok.

**Olculen.** `docs/olcumler/asama6-gorsellestirme.md`. Harita ucu isinmis durumda en
yuksek ortanca 58.3 ms, zaman cizelgesi 100 nokta icin 25.9 ms; komut sayisi pencere ve
limitten bagimsiz sabit (5 ve 4). Tarayicida soguk onbellekte ilk boyama 120-128 ms,
grafigin ekrana gelmesi 173-185 ms. Ham tablolardan yeniden hesapla **0 fark** (30 dosya,
150 nokta).

**Bu turda bulunan kusur.** Kalici durum 32 KB'lik devre mesaj sinirini asiyordu: sayfa
aciliyor, grafik ciziliyor ama hicbir tiklama calismiyordu ve sunucu gunlugunde hicbir
sey yoktu. Butce, kirpilmis ozet ve "grafik verisi kalici duruma girmesin" kurali ile
kapatildi; en buyuk sayfa durumu 19 036 karakter.

**Yapilmayanlar.** Birlesik statik+ML skor yok, kalibrasyon yok, yakinlastirma/tooltip
yok, dizin bazinda toplama yok.

**Devralinan risk.** Renk olcegi olasilik izlenimi verebilir; efsanede endeksin goreli
oldugu yazili. Varsayilan siralamada (`mean-risk-desc`) gosterilen 100 dosya cok dar bir
endeks araligina dusebiliyor ve harita tek renk gibi gorunuyor; sayfa gosterilen aralik
bilgisini yaziyor.

### 6. Denetlenebilir PDF raporu ve tek komutluk demo — **bu turda yapildi**

**Amac.** Panelden uretilebilen, kaynagi izlenebilir bir PDF raporu ve temiz bir makinede
tek komutla acilan, gercek veriye dayali bir demo ortami.

**Uretilen.** `ReportArtifacts` tablosu, rapor sozlesmesi, canonical girdi manifesti,
atomik artefakt deposu, rapor uretim isi, bes rapor ucu, panel rapor akisi,
`tools/Sievert.Demo` (seed ureticisi, idempotent importer, tek komut orkestratoru),
sabit demo veri kumesi, bagimsiz PDF dogrulama araci, ADR 0027.

**Basari olcutu.** Rapor sinirliliklarini ve model profilini tasiyor; kapakta "kalibre
edilmedi" ve "bu rapor kesin kusur karari degildir" yaziyor; kismi sonuc her bolumde
gorunuyor.

**Olculen.** `docs/olcumler/asama6-rapor-ve-demo.md`. Rapor uretimi isinmis durumda
82-119 ms, istek 6-10 ms'de donuyor, rapor uretilirken saglik ucunun ortancasi 3 ms.
Tek komutluk demo bes kosuda 7,8-10,3 saniyede hazir. Bagimsiz dogrulama: uc raporda
**125 kontrol, 0 fark**.

**Bu turda bulunan kusurlar.** Rapor kaydi guncellenmiyordu (baglamin varsayilani
NoTracking), uyari kodlari JSON yerine virgulle ayrilmis metin sanilmisti, tablo basligi
sayfa asiminda tekrarlanmiyordu, cizelge etiketleri belge kulturunden farkli
bicimleniyordu, iptal edilen raporun kaydi sonsuza kadar "uretiliyor" kaliyordu ve demo
yalniz Ctrl+C ile temizleniyordu. Altisi da duzeltildi; ayrinti olcum dosyasinda.

**Yapilmayanlar.** Otomatik artefakt silme, e-posta ile gonderme, kimlik dogrulama,
es zamanli rapor olcumu.

### 7. Kullanilabilirlik kontrolu ve kapanis — **yapilmadi**

**Amac.** Arayuzu birkac kisiye kullandirip yanlis anlasilan yerleri yazmak; Asama 6
kapanis raporu.

**Basari olcutu.** "Bu yuzde ne demek" sorusunun kac kiside ciktigi sayilmis olacak.

## Bu turda biten maddeler

- [x] Adim 0 — urun dili, risk sozlesmesi, beklentiler, ADR 0023
- [x] Adim 1 — read-only API ve model kayit defteri
- [x] Adim 2 — commit risk skoru ve aciklama
- [x] Adim 3 — arka plan isleri, iptal ve sonuc saklama
- [x] Adim 3b — panel oncesi sertlestirme (kaynak durumu, bellek, es zamanlilik)
- [x] Adim 4 — Blazor panel
- [x] Adim 5 — dosya etkinlik haritasi ve risk zaman cizelgesi
- [x] Adim 6 — denetlenebilir PDF raporu ve tek komutluk demo
- [ ] Adim 7 — kullanilabilirlik ve kapanis

**Asama 6 tamamlanmadi.** Sonraki nokta Adim 7: kullanilabilirlik dogrulamasi ve Asama 6
kapanisi.
