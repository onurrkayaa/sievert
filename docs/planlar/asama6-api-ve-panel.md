# Asama 6 yol haritasi: API ve panel

**Tarih:** 2026-09-12
**Durum:** Adim 0, 1 ve 2 bu turda yapildi. Geri kalan adimlar **yapilmadi**.

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

**Yapilmayanlar.** Yazma islemi yok, arama yok, statik tarama endpoint'i yok.

### 2. Commit risk aciklamasi — **bu turda yapildi**

**Amac.** Tek bir commit icin ham model skoru, iki karar, goreli endeks, oznitelik
katkilari ve uyarilar.

**Uretilen.** `data/asama6/model-score-reference.json`,
`/api/v1/repositories/{id}/commits/{sha}/risk`, katki hesabi ve uyari sistemi.

**Basari olcutu.** Katki toplami modelin logit'iyle 1e-6 tolerans icinde esliyor;
`RiskIndex` monoton; uc zorunlu uyari her cevapta; `CsFilesChanged = 0` commit'lerde
kapsam uyarisi.

**Yapilmayanlar.** Statik analiz calistirilmiyor, birlesik skor yok, bilinmeyen repo
skorlanmiyor.

### 3. Arka plan analiz isleri ve iptal — **yapilmadi**

**Amac.** Uzun suren islemleri (madencilik, metrik hesabi, etiketleme) istek disina
tasimak; ilerleme ve iptal.

**Uretilecek.** Is kuyrugu, is durumu endpoint'leri, iptal jetonu, is kayitlari.

**Basari olcutu.** Bir is baslatilip iptal edilebiliyor, durum sorgulanabiliyor, API
istegi bloke olmuyor.

**Yapilmayanlar.** Dagitik kuyruk, birden fazla islemci.

**Devralinan risk.** Uzun isler model dosyalarina ve veritabanina ayni anda dokunuyor;
dondurulmus kanit dosyalari bu islerden **etkilenmemeli**.

### 4. Blazor panel iskeleti — **yapilmadi**

**Amac.** Repo listesi, commit listesi ve tek commit risk goruntusu.

**Basari olcutu.** Panel API'den okuyor, kendi hesabini yapmiyor; uyarilar ve
"kalibre degil" notu ekranda gorunuyor.

**Devralinan risk.** Arayuzde yuzde isareti gormek, skoru olasilik sanmaya en kolay yol.
Risk sozlesmesindeki dil arayuzde de gecerli.

### 5. Isi haritasi ve risk zaman cizelgesi — **yapilmadi**

**Amac.** Dosya/dizin bazinda yogunluk ve zaman icinde risk egrisi.

**Basari olcutu.** Gorsellestirme, altindaki sayinin ne oldugunu (goreli endeks) gizlemiyor.

**Devralinan risk.** Renk olcegi olasilik izlenimi verebilir; efsanede endeksin goreli
oldugu yazili olmali.

### 6. Rapor/PDF ve demo akisi — **yapilmadi**

**Amac.** Tek sayfalik ozet cikti ve tekrarlanabilir bir demo senaryosu.

**Basari olcutu.** Rapor, sinirliliklari ve model profilini tasiyor.

### 7. Kullanilabilirlik kontrolu ve kapanis — **yapilmadi**

**Amac.** Arayuzu birkac kisiye kullandirip yanlis anlasilan yerleri yazmak; Asama 6
kapanis raporu.

**Basari olcutu.** "Bu yuzde ne demek" sorusunun kac kiside ciktigi sayilmis olacak.

## Bu turda biten maddeler

- [x] Adim 0 — urun dili, risk sozlesmesi, beklentiler, ADR 0023
- [x] Adim 1 — read-only API ve model kayit defteri
- [x] Adim 2 — commit risk skoru ve aciklama
- [ ] Adim 3 — arka plan isleri
- [ ] Adim 4 — Blazor panel
- [ ] Adim 5 — isi haritasi ve zaman cizelgesi
- [ ] Adim 6 — rapor ve demo
- [ ] Adim 7 — kullanilabilirlik ve kapanis

**Asama 6 tamamlanmadi.** Sonraki nokta Adim 3: arka plan analiz isleri ve iptal.
