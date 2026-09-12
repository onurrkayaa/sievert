# 0018 - Lojistik regresyon ve ham kalibrasyon

**Baglam:** Adim 2'de uc taban cizgisi olculdu ve "iyi sonuc" un ne demek oldugu sayilara
baglandi. Bu adimda ilk gercek model kuruluyor. Karar verilecek sey yalnizca "hangi
algoritma" degil: donusumun ne oldugu, esigin nereden secildigi, olasiligin nasil
degerlendirildigi ve hangi cumlenin yazilabilecegi de burada sabitleniyor.

Hepsi sonuc gorulmeden karara baglandi. Sonradan secilen bir ayar, olcum olmaktan cikip
tercihe doner.

**Karar:** Tek trainer (`LbfgsLogisticRegression`, Microsoft.ML 5.0.0), repo basina ayri
model, agirliksiz egitim, log1p + yalnizca egitimde fit edilen standartlastirma, iki esik
ve yalnizca ham olasilik kalibrasyonu.

## Neden lojistik regresyon

Uc sebep, hepsi bu adimin isine yarayan seyler.

Birincisi **okunabilirlik**: katsayilar dogrudan okunabiliyor ve standartlastirilmis
olcekte birbiriyle karsilastirilabiliyor. Bu adimin isteklerinden biri 15 ozniteligin
katkisini raporlamak; agac toplulugu bunu ayni dogrudanlikla vermezdi.

Ikincisi **olasilik**: lojistik regresyonun cikisi zaten bir olasilik (skorun lojistik
donusumu), sonradan bir kalibrasyon katmani gerekmiyor. Bu adimin isi tam olarak o ham
olasiligin kalitesini olcmek.

Ucuncusu **taban olmasi**: bu, alanin en basit denetimli modeli. Daha karmasik bir model
kurulacaksa once bunun ne verdigi bilinmeli.

**Trainer secimi `LbfgsLogisticRegression`.** Alternatifi `SdcaLogisticRegression`'di.
LBFGS toplu (batch) bir eniyileyici: ayni veriden ayni sonucu veriyor. SDCA ornek ornek
ilerliyor ve paralel kosuda toplama sirasi degisebiliyor, yani deterministiklik ek
onlemler istiyor. Alternatif trainer'lar **denenip en iyisi secilmedi**; biri secildi,
gerekcesi yazildi ve degistirilmedi.

**Ayarlar.** `MLContext` tohumu 20260912, is parcacigi 1 (paralel gradyan toplamasinin
sirasi kosudan kosuya degismesin), L2 = 1,0 (ML.NET varsayilani), geri kalan her sey
varsayilan: tolerans 1e-7, gecmis boyutu 20, yineleme siniri yok, negatiflik kisiti
kapali. `CacheCheckpoint` eklenmedi: veri bellekte ve tek gecis yetiyor.

**L1 = 0 ve bu bir ayar aramasi degil.** ML.NET varsayilani 1,0 ve o degerde butun
agirliklar sifira dusuyor - kucuk bir ornek uzerinde denendi, uc agirligin ucu de 0
cikti. L1 duzenlilestirme katsayilari sifirlayarak **oznitelik secimi** yapar; oysa bu
adimda oznitelik eleme acikca yasak ve 15 katsayinin hepsi raporlanacak. Yani L1'i
kapatmak, test sonucuna degil bu adimin kendi kuralina dayanan bir karar.

L2'yi varsayilanda birakmanin bedeli var ve yaziyorum: duzenlilestirmenin katsayilari ne
kadar kucultugu **olculmedi**.

## Neden uc repo icin ayri model

Ayni-repo sorusu "bu repoda gecmisten gelecegi tahmin edebilir miyiz". Uc repo tek bir
modelde birlestirilseydi cevap "hangi repodan geldigini bilerek tahmin edebilir miyiz"
olurdu ve repo kimligi dolayli olarak modele girerdi.

Olculen sayilar bunu destekliyor: uc reponun taban oranlari cok farkli (egitimde %13,0 /
%14,9 / %23,4) ve `AuthorCommitCount` medyani ShareX'te 2483, Polly'de 125 (ADR 0016).
Ayni olcu uc repoda ayni seyi olcmuyor.

Repo-arasi genelleme ayri bir deney ve Adim 5'te. Bu adimda repo yalnizca uc is icin
kullaniliyor: ayri model egitmek, ayri esik secmek, sonuclari gruplamak.

## Neden agirlik ve yeniden orneklem yok

Sinif agirligi, oversampling, undersampling, SMOTE - hicbiri yok.

Sebep bu adimin amacinda: **olasiliklarin dogal taban oranindaki davranisini** olcmek.
Sinif agirligi vermek, modelin urettigi olasiligi taban orandan koparir; sonra olculen
Brier ve ECE "modelin olasiligi ne kadar iyi" sorusunu degil "verdigimiz agirlik ne
yapti" sorusunu cevaplar.

Agirlikli bir model ileride **ayri bir duyarlilik deneyi** olarak ele alinabilir. O zaman
da bu adimin sayilariyla yan yana konur, yerine gecmez.

## Neden log1p

13 sayim ve buyukluk olcusunun hepsi uzun kuyruklu. ShareX'te bir commit 165 538 satir
ekleyebiliyor, ayni reponun medyani 21. Log almadan standartlastirilirsa ortalama ve
standart sapmayi birkac uc deger belirler; butun normal commit'ler sifirin etrafinda bir
noktaya sikisir ve aralarindaki fark kaybolur.

`log(1 + x)` seciliyor, `log(x)` degil: bu alanlarin cogunda 0 gecerli bir deger
(`CsFilesChanged` Polly'de medyan 0) ve log(0) tanimsiz. log1p(0) = 0 oldugu icin sifir
anlamini koruyor.

**`Entropy` log gormuyor.** Zaten sinirli bir araliktaki bir olcu (0 ile log2(dosya
sayisi) arasi) ve uzun kuyrugu yok; log almak onu ikinci kez sikistirmak olurdu.
Standartlastiriliyor.

**`IsFix` ne log ne standartlastirma goruyor.** Mantiksal bir alan; 0/1 kalmasi
katsayisinin "duzeltme olmak olasiligi ne kadar degistiriyor" diye okunmasini sagliyor.
Bunun bedeli: `IsFix` katsayisi digerleriyle ayni olcekte degil ve onlarla dogrudan
karsilastirilamaz. Raporda yaziyor.

## Neden normalizasyon yalnizca egitimde fit ediliyor

Ortalama ve standart sapma **yalnizca egitim bolumunden** ogreniliyor. Test satirlari fit
asamasina hic girmiyor ve test kendi ortalamasiyla normalize edilmiyor.

Test istatistikleri kullanilsaydi model, egitim zamaninda bilemeyecegi bir seyi bilmis
olurdu: gelecekteki commit'lerin dagilimini. Etkisi dolayli ama gercek - bu, ADR 0016'daki
bolme kararinin donusum tarafi.

**Kirpma yok.** Testte egitim araliginin disinda kalan degerler ayni donusumle geciyor.
Kirpmak, gercek kullanimda gorulecek buyuk bir commit'i egitimde gorulen en buyugu gibi
gostermek olurdu. Bedeli olculdu ve buyuk: ShareX'te test satirlarinin %94,7'si en az bir
ozniteligiyle egitim araliginin disinda.

## Neden 0,5 ve train-tuned esik birlikte

Iki esik iki farkli soruyu cevapliyor.

**0,5** modelin kendi olasiligini oldugu gibi kullaniyor: "model bu commit'in hata
getirme ihtimalini yaridan yuksek buluyor mu". Hicbir ayar icermiyor.

**Egitimde secilen esik** F1'i en buyuten noktayi ariyor. Bu, taban cizgilerindeki
`LinesAdded` esigiyle ayni kural ve ayni kodu kullaniyor (ADR 0017), yani ikisi
karsilastirilabilir.

Ikisi birlikte raporlaniyor cunku olculen fark kucuk degil ve tek birini yazmak secim
olurdu: mikro F1 0,5 esiginde 0,4646, egitimde secilen esikte 0,4408. Yani egitimde F1'i
en cok buyuten esik testte daha kotu. Bu, tek basina bir sonuc; gizlenmemeli.

Ana taban karsilastirmasi yine de egitimde secilen esikle yapiliyor: tabanin esigi de oyle
secildi.

## Neden test esik secimine girmiyor

Aday esikler yalnizca **egitim tahminlerinden** uretiliyor ve esik egitim F1'ine gore
seciliyor. Esik secildikten sonra degistirilmeden teste uygulaniyor.

Test etiketlerine bakilarak esik secilseydi olculen sayi gercek kullanimda alinamayacak
bir sayi olurdu: yeni bir commit geldiginde onun etiketine bakip esik ayarlanamaz. Bu,
Adim 2'deki `LinesAdded` esigi kararinin aynisi.

## "Model tabani gecti" iddiasinin uc kosulu

Bu cumle ancak uc kosulun **tamami** saglanirsa yazilabilir:

1. Model train-tuned mikro F1 > 0,3754
2. Model train-tuned makro F1 > 0,2999
3. Model mikro ham PR-AUC > 0,2611

Ucu de saglandi (0,4408 / 0,3088 / 0,4775), yani cumle yazilabilir.

Uc kosulun birlikte istenmesinin sebebi: mikro F1 tek basina Jellyfin'in sonucu (test
satirlarinin %67,1'i orada), makro tek basina Polly'nin 10 pozitifine asiri duyarli,
PR-AUC ise esikten bagimsiz siralama yetenegini olcuyor. Biri gecip digeri kalirsa
"gecti" denemez.

**Repo bazindaki ters sonuc gizlenmiyor:** Polly'de model F1'i (0,2500) tabanin altinda
(0,2667). Makro farki da dar (0,3088'e karsi 0,2999). Uc toplu kosul gecti ama uc repodan
biri ters yonde ve bu raporda ayri bir satirda duruyor.

## Brier ve ECE tanimlari

**Brier** = `sum((p - y)^2) / N`. Butun hatayi tek sayida topluyor; hem siralama hem
kalibrasyon hatasini iceriyor ama ayrismasini gostermiyor.

**ECE** = `sum((kutu sayisi / N) * |ortalama tahmin - gozlenen oran|)`. Kutu bazinda
sapmayi gosteriyor.

**Ikisi birlikte yaziliyor, hicbiri tek basina degil.** ECE tek basina yaniltici: model
her satira ayni dusuk olasiligi verirse ve taban oran da dusukse ECE kucuk cikar, oysa
model hicbir sey ayirt etmiyordur. Brier o durumu de yakalar. Tersi de dogru: Brier tek
basina hatanin nereden geldigini soylemez.

Olculen ornek bu yuzden onemli: Polly'nin Brier'i en dusuk (0,0132) ama sebebi modelin
iyiligi degil, o kumede pozitif oraninin %1,21 olmasi.

**Accuracy yok**, gerekcesi ADR 0017'de.

## ECE'nin 10 esit kutu karari

10 esit genislikli kutu: [0,0-0,1), [0,1-0,2), ... [0,9-1,0]. Son kutu 1,0'i **iciyor**,
boylece olasiligi tam 1,0 olan bir satir kaybolmuyor.

Esit genislik seciliyor, esit sayili (quantile) kutu degil. Sebep okunabilirlik: "0,7
diyen tahminlerin gercekte ne kadari pozitif" sorusunun cevabi dogrudan bir satirda
duruyor. Esit sayili kutularda her kutunun sinirlari veriye gore degisir ve iki repo
karsilastirilamaz.

Bedeli var ve yaziliyor: bu veride olasiliklarin cogu ilk kutuda toplaniyor (mikro
tabloda 10 251 satirin 5627'si), yani ust kutular az satirla temsil ediliyor. Polly'de uc
kutu bos.

**Bos kutular toplama 0 agirlikla giriyor.** Icinde satir olmayan bir kutu icin ortalama
uydurulmuyor; kutu tabloda satir sayisi 0 ile duruyor ve ECE'ye katkisi yok.

## Neden kalibrasyon bu adimda uygulanmadi

Platt scaling, isotonic regression ya da baska bir sonradan kalibrasyon **uygulanmadi**.

Sebep sirali olmasi gereken iki is: once ham olasiligin ne kadar bozuk oldugu olculmeli,
sonra duzeltilmeli. Once duzeltilseydi, duzeltmenin ne kadar ise yaradigini soyleyecek bir
referans kalmazdi.

Bir ayrinti: ML.NET modeli bir `PlattCalibrator` ile sarmaliyor ama parametreleri sabit
(slope -1, offset 0), yani sonuc tam olarak sigmoid(skor). **Veriden ogrenilen bir Platt
olcekleme degil**, lojistik baglantinin kendisi. Kontrol edildi.

Sonradan kalibrasyon Adim 3b'de yapilacak. Bu adimin ham sayilari o zaman silinmeyecek,
yanina yazilacak.

## Katsayilar nedensellik degil

Katsayilar standartlastirilmis olcekte ve surekli oznitelikler arasinda
karsilastirilabilir. Yine de soylenebilecek sey **modelde tasidiklari iliski**, "bu
oznitelik hataya neden olur" degil.

Fark onemli. Olculen ornek: `CsFilesChanged` uc repoda da en buyuk ya da ikinci buyuk
pozitif katsayi. Bunun bir sebebi etiketin uretim bicimi olabilir - SZZ yalnizca silinen
`.cs` satirlarini blame ediyor, yani hic `.cs` dosyasina dokunmayan bir commit pozitif
etiket **alamiyor**. Bu durumda katsayi, "`.cs` degistirmek hata getirir" degil,
"`.cs` degistirmeyen commit etiketlenemez" bilgisini tasiyor olabilir. Beklenti dosyasinda
bu uyari onceden yaziliydi.

Hangi aciklamanin dogru oldugu **olculmedi**.

## Yuksek korelasyonun katsayi yorumuna etkisi

Yalnizca egitim bolumunde, 15 oznitelik arasinda Spearman korelasyonu hesaplandi; hedef
degisken matrise **girmedi**. `|rho| >= 0,80` olan ciftler raporlaniyor.

Oznitelik **elenmedi** ve model bu sonuca gore yeniden egitilmedi. Amac model secmek
degil, katsayi yorumunun sinirini gostermek.

Olculen: `FilesChanged` ile `Entropy` uc repoda da 0,89'un uzerinde (Jellyfin'de 0,9831).
Birbirine bu kadar yakin iki oznitelik arasinda katsayinin nasil bolunecegi veriye
duyarlidir; kucuk bir degisiklik buyuklugu ve isareti kaydirabilir. `FilesChanged` ile
`CsFilesChanged`'in zit isaretli cikmasi da boyle okunmali: model ikisinin farkini
kullaniyor olabilir, tek tek buyukluklerini degil.

Yani tablodaki tek bir satira bakip "sunun etkisi sudur" demek guvenli degil ve raporda
oyle yazilmadi.

## Sag sansur ham olasilik olcumunu etkileyebilir

Olculen sonuc tek yonlu: model her kalibrasyon kutusunda gercekte olandan **yuksek**
olasilik veriyor ve fark kutu yukseldikce buyuyor (mikro tabloda 0,0564'ten 0,3325'e).

Bu bulgu otomatik olarak modele baglanmiyor. Iki aday aciklama var:

1. **Zamansal taban oran degisimi.** Test bolumunun pozitif orani egitimden belirgin
   dusuk (Polly %13,0 -> %1,21, ShareX %14,9 -> %5,06, Jellyfin %23,4 -> %13,48). Modelin
   ogrendigi taban oran testte gecerli degil.
2. **Sag sansur.** Bir commit ancak sonraki bir duzeltme onu suclarsa pozitif etiket
   aliyor; test bolumundeki yeni commit'lerin bir kismi henuz suclanmamis olabilir. Adim
   1'de olculdu: Polly'nin testinde 180 gunden az olgun 207 commit var ve hicbiri pozitif
   degil.

Ikisi birbirini dislamiyor ve hangisinin ne kadar etkiledigi **bu adimda olculmedi**.
Sag sansur duyarliligi Adim 1'de ilan edildigi gibi Adim 4'te ayri bir deney olarak
olculecek; o zamana kadar bu adimin sayilari "kalibrasyon bozuk, sebebi bilinmiyor"
olarak duruyor.

Ana test kumesi bu adimda **degistirilmedi**: satir cikarilmadi, 90 gunluk olgunluk alt
kumesi hesaplanmadi, etiket ya da bolme degistirilmedi.

**Sonuc:** Uc model `data/asama5/models/` altinda, sonuclar `model-results.json` ve
`model-predictions.csv` icinde ozetleriyle duruyor. Model uc toplu olcutte de
`LinesAdded` tabanini gecti; Polly'de F1 tabanin altinda kaldi ve ham olasiliklar tek
yonlu asiri guvenli cikti.
