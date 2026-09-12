# Adim 3b kalibrasyon sozlesmesi

**Surum:** 1.0
**Tarih:** 2026-09-12
**Durum:** Koddan ve sonuclardan **once** yazildi.

Iki yontem uygulanacak: **Platt scaling** ve **isotonic regression**. Ikisi onceden ilan
edilmis iki ayri deney; test sonucuna bakarak aralarindan kazanan **secilmeyecek**.

Yeni NuGet paketi eklenmiyor; ikisi de mevcut proje ve framework ile yazildi.

## Ortak kurallar

- Kalibrasyon **yalnizca calibration bolumunun etiketlerinden** ogreniliyor.
- **Test etiketleri fit'e hic girmiyor.**
- Model-fit bolumu trainer'a, calibration bolumu kalibrasyona, test hicbir fit islemine
  girmiyor.
- Her repo kendi kalibrasyonunu ogreniyor.
- Cikti her zaman [0, 1] araliginda.

## A) Platt scaling

**Girdi:** modelin ham `Probability` degeri.

**Adimlar:**

1. `p` degeri `[1e-15, 1 - 1e-15]` araligina kirpilir. Sebep: ham olasilik tam 0 ya da
   tam 1 olabiliyor ve `log(p / (1-p))` o noktalarda tanimsiz.
2. `x = log(p / (1 - p))`.
3. Calibration kumesinde tek degiskenli lojistik donusum fit edilir: `sigmoid(a*x + b)`.
4. Cikti `sigmoid(a*x + b)`.

**Fit yontemi:** Newton-Raphson (IRLS). Iki parametre icin gradyan ve Hessian kapali
bicimde hesaplaniyor.

- Baslangic: `a = 1`, `b = 0` (yani ham olasiligin kendisi).
- **Sonumleme (step halving):** tam Newton adimi negatif log-olabilirligi
  kotulestirirse adim yariya boluniyor; en fazla **50** kez. Sonumleme olmadan ilk adim
  asiri buyuk olabiliyor, butun tahminler doyuma gidiyor ve Hessian tekillesiyor - bu
  davranis bir birim testinde goruldu ve algoritma o yuzden sonumlu yazildi. Sonumleme
  deterministik; tohum ya da rastgelelik icermiyor.
- Yakinsama olcutu: uygulanan (sonumlenmis) parametre degisiminin en buyuk mutlak degeri
  `< 1e-10`.
- En fazla yineleme: **200**.
- Hessian'in determinanti `< 1e-12` olursa (tekil matris) hata verilir.
- 200 yinelemede yakinsamazsa hata verilir. Yakinsamayan sonuc **sessizce kullanilmaz**.

**Monotonluk:** donusum monoton **artan** olmali, yani `a > 0`. `a <= 0` cikarsa sonuc
sessizce kullanilmaz; islem durur ve raporlanir.

**Neden Newton-Raphson:** iki parametreli bir problem icin deterministik, tohum
gerektirmiyor ve yakinsama olcutu ile yineleme siniri acikca yazilabiliyor. Hazir bir
trainer kullanilsaydi onun varsayilan duzenlilestirmesi `a` ve `b`'yi kucultur ve
"kalibrasyon" yerine "kalibrasyon artı bilinmeyen bir buzulme" olculurdu.

## B) Isotonic regression

**Girdi:** modelin ham `Probability` degeri.

**Adimlar:**

1. Calibration satirlari ham `Probability` degerine gore **artan** siralanir.
2. **Esit `Probability` degerleri tek grup** olarak ele alinir; grup bolunmez. Bu, sonucu
   esit skorlu satirlarin kendi aralarindaki sirasindan bagimsiz yapar.
3. Her gruba baslangic degeri olarak grubun **gozlenen pozitif orani**, agirlik olarak
   grubun satir sayisi verilir.
4. **Pool Adjacent Violators (PAV)** uygulanir: soldan saga gidilir, bir blogun degeri
   solundaki blogun degerinden kucukse iki blok birlestirilir ve birlesik blogun degeri
   agirlikli ortalama olur. Ihlal kalmayana kadar tekrarlanir.
5. Sonuc **azalmayan** bir basamak fonksiyonu.

**Test skorunun haritalanmasi:**

- Skor, blok araliklarindan birine dusuyorsa o blogun degeri.
- Skor calibration araliginin **altindaysa** ilk blogun degeri.
- Skor calibration araliginin **ustundeyse** son blogun degeri.
- Blok sinirinda esitlik: skor bir blogun ust sinirina tam esitse **o blogun** degeri
  kullanilir (sinirlar `[alt, ust]` kapali; ardisik bloklarin sinirlari cakismadigi icin
  belirsizlik yok).

**Deterministiklik:** siralama `Probability` artan, esitlikte gruplama; PAV'in kendisi
deterministik. Tohum yok.

## Olculecekler

Her repo TEST kumesinde uc sonuc yan yana:

- **Ham** (azaltilmis-egitim modelinin kendi olasiligi)
- **Platt**
- **Isotonic**

Her biri icin: Brier, ECE, ortalama tahmin olasiligi, gercek pozitif / toplam,
10 ECE kutusunun tam tablosu, PR-AUC, `Probability >= 0,5` icin TP/FP/FN/TN, precision,
recall, F1.

Ayrica mikro ve makro Brier / ECE / PR-AUC / F1.

Brier ve ECE tanimlari `asama5-metrik-sozlesmesi.md` surum 1.0'da; degismedi.

## Kalibrasyon basarisi tanimi

Bir yontem icin **"mikro kalibrasyonu iyilestirdi"** cumlesi yalnizca su iki kosulun
**ikisi birden** saglanirsa yazilabilir:

- Mikro Brier hamdan **dusuk**, **ve**
- Mikro ECE hamdan **dusuk**.

Biri iyilesip digeri kotulesirse **"karisik sonuc"** yazilir ve tek sayiya indirgenmez.
Repo bazinda da ayni iki kosul ayri ayri uygulanir.

## Karsilastirma noktasi

Kalibrasyon etkisinin dogru karsilastirmasi **azaltilmis-egitim modelinin ham sonucu ->
ayni modelin Platt/isotonic sonucu**.

Adim 3'un tam-train modelinin ham Brier/ECE sonucu tabloda **ayri bir referans** olarak
durabilir, fakat azaltilmis-egitim modelinin ham sonucuyla dogrudan "kalibrasyon etkisi"
diye karsilastirilmaz: ikisi farkli miktarda veriyle egitilmis iki ayri model.

Azaltilmis-egitim modeli Adim 3'un modelinin **yerine gecmez**.
