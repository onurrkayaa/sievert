# 0023 - API risk sozlesmesi ve modelin sunumu

**Baglam:** Asama 5 uc model birakti ve o modellerin ne olup ne olmadigini olctu: skor
kalibre edilmedi (ADR 0019, ADR 0022), hedef SZZ'nin otomatik etiketi (ADR 0014),
`CsFilesChanged = 0` olan 8607 commit'in pozitifi 0, repo-arasi aktarim tutarsiz
(ADR 0021), insan dogrulamasi 14 ornekle sinirli.

Asama 6 bu modelleri bir API'nin arkasina koyuyor. Teknik is kolay kismi. Zor kismi su:
bir sayiyi `"risk": 0.42` diye dondurmek, Asama 5'in olctugu butun sinirlari tek bir
alan adiyla silip atar. Kullanici onu yuzde olarak okur, kimse de yanlis oldugunu
soylemez.

**Karar:** Urun dili API yazilmadan once sabitlendi, cevapta tek bir "risk" sayisi yok,
model metadata'si koda kopyalanmiyor ve aciklama modelle dogrulanmadan donmuyor.

## Neden dort ayri sayi, tek bir risk yuzdesi degil

Cevapta `rawModelScore`, `decisionAt05`, `decisionAtTrainThreshold` ve `riskIndex` ayri
ayri duruyor. Hicbiri digerinin yerine gecmiyor.

Tek bir sayi vermek, aslinda dort ayri seyi tek bir sayiya sikistirmak olurdu: modelin
ham ciktisi, iki farkli esige gore karar, ve dagilimdaki siralama. Ikisi birbirinden
bagimsiz degisir - esik degisince karar degisir ama skor degismez - ve ayni alanda
durmalari bunu gorunmez yapar.

`probability` alan adi da kullanilmiyor. Alan adinin kendisi bir iddia: skor kalibre
edilmedi, yani "olasilik" demek olculmemis bir sey soylemek olurdu. Bir test butun
cevaplarda bu adin gecmedigini siniyor; kural belgede kalirsa bir gun bir alana sizar.

## Neden `RiskIndex` skorun yuz kati degil

`riskIndex`, skorun **egitim dagilimindaki yuzdelik sirasi**. `rawModelScore * 100`
degil.

Sebep dogrudan kalibrasyondan geliyor. Skor 0,42 iken "%42" yazmak, modelin o
commit'lerin %42'sinde hakli oldugunu ima eder; oysa kalibrasyon olculdugunde ECE
0,029'du ve uretim icin bir kalibrator secilmedi. Yuzdelik sira ise olculmus bir sey:
"bu commit, ayni modelin egitimde gordugu commit'lerin %87'sinden daha yuksek skor
aldi" cumlesi dogru ve dogrulanabilir.

Bedeli var ve cevapta yaziyor: **farkli profillerin endeksleri karsilastirilamaz.** Her
profil kendi egitim dagilimina gore olcekleniyor. Bu soyut bir uyari degil; olcum
kodunda tam olarak bu hatayi yaptim ve uc deponun endekslerini tek listede karsilastirip
olmayan 56 ihlal urettim (`docs/olcumler/asama6-api-temel.md` bolum 8).

Esitlikte orta sira (mid-rank) kullaniliyor, boylece endeks skor buyudukce hic dusmuyor
ve esit skorlar ayni degeri aliyor.

## Neden dagilim dosyadan okunuyor, istek aninda hesaplanmiyor

`data/asama6/model-score-reference.json` egitim bolumunun skorlarini artan sirada
tutuyor. Dagilimi her aciliste hesaplamak 24 bin satiri yeniden skorlamak olurdu ve
hangi surumle uretildigi izlenemezdi.

Dosya yazilmadan once bir kapi var: uretilen skorlar egitim esiginde
`model-results.json` icinde kayitli TP/FP/FN/TN degerlerini **aynen** vermek zorunda.
Uc repoda da aynen verdi. Bu, "dondurulmus model dosyasi + kayitli egitim
istatistikleri" yolunun Asama 5'te olculen seyi gercekten yeniden urettiginin kaniti.

## Neden model metadata'si koda kopyalanmiyor

Esik, katsayilar, egitim istatistikleri, trainer ve surum bilgisi `ModelRegistry`
tarafindan `model-results.json` dosyasindan okunuyor. Model zip'lerinin ozetleri
`models.sha256` dosyasindan.

Kopyalanmis bir sayi, kanit dosyasi degistiginde sessizce eskir ve iki yerde iki farkli
esik olur. Ustelik API'nin dokumanla ayni sayiyi soyledigini kimse dogrulayamaz.

Hicbir model ozeti dogrulanmadan kullanilmiyor. Ozet tutmazsa profil
`ChecksumMismatch` isaretleniyor, `Load` reddediyor ve uc `409` donuyor. "Belki iyidir"
diye devam etmek, hangi modelin skorladigini bilmemek demek.

Modeller profil basina bir kez yukleniyor (`Lazy`, `ExecutionAndPublication`). Olcumde
ilk istek 281 ms, sonrakilerin medyani 3,9 ms; her istekte yuklense hepsi 281 ms
bandinda kalirdi.

## Neden bilinmeyen depo skorlanmiyor

Uc egitim reposundan biri olmayan bir depo icin API `422` donuyor ve varsayilan bir
profil **secmiyor**.

Sebep ADR 0021'de: tek kaynakli alti yonun ucu ayni-repo F1'inin ustunde, ucu altinda.
Yani "hangi profili sececegimizi bilmiyoruz" cumlesi olculmus bir cumle. Sessizce bir
profil secmek, olculmemis bir aktarimi olculmus gibi gostermek olurdu.

Kullanici acikca `?profile=` ile baska bir profil secebiliyor; o zaman cevapta
`EXTERNAL_MODEL_PROFILE` uyarisi cikiyor. Secim gizli degil, isaretli.

Sozlesmede kucuk bir sapma var ve yazmak gerekiyor: risk sozlesmesi
`UNKNOWN_REPOSITORY_MODEL` kodunu **uyari** tablosunda listeliyor, API'de ise `422`
cevabinin **hata kodu** olarak duruyor. Sebep sudur: uyari bir degerlendirmenin yaninda
doner, burada ise degerlendirme hic uretilmiyor. Kod ayni, durdugu yer farkli.

## Neden statik bulgular ayri bolumde ve birlesik skor yok

`staticAnalysis` bolumu `status: "not-run"`, `findings: []` ve
`includedInModelScore: false` tasiyor. Bos liste "bulgu yok" demek degil, "calistirilmadi"
demek; ikisi farkli ve karistirilirsa arac bulmadigi bir seyi bulmus gibi gorunur.

Birlesik bir skor **alani bile yok**. Statik bulgularla model skorunu birlestirecek bir
agirlik olculmedi; olculmemis bir agirlikla birlestirmek, olmayan bir sayiyi uretmek
olurdu. Bos bir alan bile "yakinda gelecek bir skor" izlenimi verir.

## Neden aciklama dogrulanmadan donmuyor

Lojistik regresyonda ayristirma tam: logit = kesisim + toplam(katsayi * deger). Yaklasik
bir aciklama yontemi kullanilmadi, cunku gerek yok - ve yaklasik olsaydi toplaminin
skora esit oldugunu soyleyemezdik.

Hesap modelin kendi verdigi logit ile karsilastiriliyor. Fark toleransi asarsa cevap
**donmuyor**, `MODEL_EXPLANATION_MISMATCH` doniyor. Yanlis bir aciklama, aciklama
olmamasindan kotu: kullanici ona bakip karar verir.

Bu kapinin bedeli olculdu: 34 166 satirin 11'inde tolerans asiliyor ve o commit'ler
aciklama alamiyor. Sebebi hesap hatasi degil, ML.NET'in `float` ile toplamasi. Ayrinti
ve ne yapilmadigi `docs/olcumler/asama6-api-temel.md` bolum 7'de.

Katkilar **nedensel degil**. Metinler "model skoru yukari tasidi" diyor, "hataya yol
acti" demiyor. Yon basina en fazla bes katki doniyor; katkisi tam sifir olan oznitelik
listeye girmiyor cunku sifir bir yon degil.

## Neden minimal API, controller degil

Uc yonlendirme dosyasi ve alti uc var; hepsi tek bir istek-cevap donusumu, model
baglama, filtre, eylem sonucu donusumu gereken bir yer yok. Controller katmani burada
her uc icin fazladan bir sinif ve bir dosya olurdu.

Uc sayisi buyurse ya da ortak filtre ihtiyaci cikarsa bu karar yeniden bakilmali; simdi
bakildiginda karsiligi olmayan bir katman.

## Neden butun hatalar ProblemDetails + errorCode

Hatalar RFC 7807 bicimi ile doniyor, uzerine makine okunabilir bir `errorCode` ve
`traceId`. Istemci serbest metne gore degil koda gore dallaniyor; metin degisebilir,
kodlar degismez.

Istisna ayrintisi, dosya yolu ve baglanti dizesi cevaba **girmiyor**. Repo public;
sunucu adi ya da kullanici adi tasiyan bir istisna metni dogrudan sizinti olurdu.
Ayrinti sunucu gunlugunde kaliyor, kullanici `traceId` ile esliyor.

Baglanti dizesi yoksa API yine ayaga kalkiyor ve saglik ucu neyin eksik oldugunu
soyluyor. Baslangicta patlamak daha "temiz" gorunurdu ama o zaman kullanici bos bir
ekran gorur ve sebebini bilemez.

## Bu kararin sinirlari

- Butun uclar salt-okunur. Yazma islemi, repo klonlama ve uzun suren analiz isi bu
  turda **yok**.
- Statik tarama ucu yok; `Sievert.Analysis`'e referans bile verilmedi, cunku referans
  "birazdan gelecek" izlenimi verirdi.
- Kimlik dogrulama ve yetkilendirme yok. API yerel calistirilmak uzere yazildi.
- Es zamanli yuk altinda davranis olculmedi.
