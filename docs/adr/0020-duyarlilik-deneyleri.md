# 0020 - Duyarlilik deneyleri

**Baglam:** Adim 3'ten sonra elde bir model ve bir sonuc var, ama o sonucun hangi
kararlara ne kadar bagli oldugu bilinmiyor. Bot commit'leri veride duruyor, test
bolumunun bir kismi sag sansurlu, `CsFilesChanged` etiketin uretim bicimiyle bagli,
test degerlerinin buyuk kismi egitim araliginin disinda ve ad degisimi esigi hicbir zaman
sinanmadi.

Bu ADR o sorularin her birini ayri bir deneye bagliyor ve **hicbirinin ana sonucu
degistirmedigini** karara bagliyor.

**Karar:** Alti bagimsiz "ya soyle olsaydi" deneyi; ana model, ana bolme, ana test kumesi
ve dondurulmus dosyalar degismiyor; her deneyin neyi kanitlamadigi ayrica yaziliyor.

## Neden hicbiri ana sonucu degistirmiyor

Bir duyarlilik deneyi, sonucun daha iyi gorundugu bir varyanti bulup onu ana sonuc yapmak
icin degil, mevcut sonucun hangi kararlara bagli oldugunu olcmek icin var.

Olculen ornek bunun neden onemli oldugunu gosteriyor: 90 gunluk olgun alt kumede uc repoda
da F1 ve PR-AUC ana testten **yuksek** cikti (F1 +0,0045 / +0,0447 / +0,0201). O alt kume
ana sonuc yapilsaydi rapor daha iyi gorunurdu, ama o sayi "en az 90 gun gozlenmis
commit'lerde" gecerli bir sayi olurdu, "commit'lerde" degil. Sansurlu satirlarin gercekte
negatif oldugu **kanitlanmadi**.

Ayni gerekce botlar icin de gecerli: botlar ana snapshot'tan silinmedi ve bot haric tutma
karari tek bir metrige bakilarak verilmedi.

## Bot: neden uc politika

Uc politika farkli sorulari cevapliyor:

- **A** (ana): botlar hem egitimde hem testte. Mevcut dondurulmus sonuc.
- **B** (yalniz degerlendirme filtresi): ana model degismiyor, yalnizca testten botlar
  cikiyor. "Model bot olmayan commit'lerde ne yapiyor" sorusu.
- **C** (bot haric yeniden egitim): donusum, model ve esik bot olmayan train'de. "Botlari
  hic gormeseydi ne olurdu" sorusu.

B ile C'nin farki, **botlari egitimden cikarmanin** bot olmayan testteki etkisi. Olculen
fark cok kucuk: Polly'de F1 birebir ayni, Jellyfin'de 0,4899 → 0,4901 ve tek bir satirin
tahmini degisiyor. Asama 4'te olculen sey (botlar dar bir dosya kumesinde donuyor, bot disi
commit'lerin yalnizca %19,5'inin metrigini degistiriyorlar) bu sonucu destekliyor.

Beklenmeyen bir sayi cikti ve raporda duruyor: Polly'nin **test** bolumunde bot orani
%67,3, butun tarihteki %31,0'in cok uzerinde. Botlari cikarinca Polly'nin Brier'i 0,0132'den
0,0398'e yukseliyor - kolay negatifler gidince ortalama hata buyuyor. Bu, kalibrasyonun
kotulesmesi degil, kumenin degismesi.

## 90 gun: neden onceden ilan edildi

90 gunluk esik **Adim 1'de**, hicbir model sonucu gorulmeden ilan edildi. Sonradan
secilseydi uc aday (30 / 90 / 180) arasindan en iyi sonucu vereni secmek mumkun olurdu ve
o zaman sayi bir olcum olmaktan cikardi.

Ana test kumesi degistirilmedi, yeniden egitim yapilmadi, esik degistirilmedi. Yalnizca
mevcut modelin mevcut tahminleri bir alt kumede yeniden sayildi.

## `CsFilesChanged`: neden ablasyon iki kaynagi ayiramiyor

Olculen mekanik bag su: uc repoda da hic `.cs` dosyasi degistirmeyen commit'lerin pozitif
etiket sayisi **tam olarak 0** (0 / 1628, 0 / 1310, 0 / 5669). Toplam 8607 commit, yani
veri kumesinin %25,2'si, yapisal olarak pozitif etiket **alamiyor**. Sebep ADR 0014'te:
SZZ yalnizca silinen `.cs` satirlarini blame ediyor.

Bu yuzden `CsFilesChanged`'in guclu katsayisi iki ayri seyden gelebiliyor:

1. `.cs` degistiren commit'lerin gercekten daha cok hata getirmesi,
2. `.cs` degistirmeyen commit'lerin etiketlenememesi.

Ablasyon (ozniteligi cikarip yeniden egitmek) ikisini **ayiramaz**: oznitelik cikarilsa
bile etiketler ayni sekilde uretilmis olarak kaliyor. C analizi (yalniz
`CsFilesChanged > 0` commit'ler) ikinci kaynagi kismen kapatiyor, ama o alt kumede de
etiketin uretim bicimi degismiyor.

Bu sinir raporda yaziyor ve **nedensellik iddiasi kurulmuyor**. Ablasyonun Polly'de
performansi yukseltmesi (F1 +0,0109) de bu belirsizligin bir parcasi.

## Train araligi disi: neden kirpma yok

Kirpma yapilmadi, yeniden egitim yapilmadi, oznitelik cikarilmadi. Kirpmak, gercek
kullanimda gorulecek bir degeri egitimde gorulen en buyugu gibi gostermek olurdu
(ADR 0018).

Olculen tasmalarin hepsi **tek yonlu**: uc repoda da alt sinirin altinda kalan deger 0, ust
sinirin ustunde kalan cok. Aciklamasi yapisal: `AuthorCommitCount`, `MaxFileAgeDays` ve
`MinFileAgeDays` zamanla buyuyen olculer ve test bolumu her zaman egitimden sonra geliyor.

**Bu analiz neden-sonuc gostermiyor.** Aralik disi ve aralik ici gruplarin pozitif
oranlari farkli ve yonu repoya gore degisiyor (Polly ve ShareX'te disi grup daha dusuk,
Jellyfin'de daha yuksek). Iki grup baska bakimlardan da farkli.

## Ad degisimi: iki kavram neden ayri

Karistirilmamasi gereken iki sey var:

1. **Zincir takibi:** bir dosyanin adi degisince gecmisi yeni yola tasinsin mi
   (`MetricOptions.FollowRenames`). Bu bir **bizim** kararimiz ve ayni diff'lerden
   hesaplanabiliyor.
2. **Benzerlik esigi:** git'in iki dosyayi "ayni dosya" sayma esigi. Bu **git'in**
   karari ve degistirmek tarihi yeniden okumayi gerektiriyor.

Asama 4'te yalnizca birincisi olculmustu. Ikincisi icin esikler **sonuc gormeden**
sabitlendi: 40, 50 (ana), 60.

Deney `tools/` altinda ayri bir yol: ana snapshot ve veritabani okunmuyor bile, git
dogrudan okunuyor ve cikti ayri bir klasore yaziliyor. Etiketler dondurulmus hedeften SHA
ile baglaniyor, yani etiketleme yeniden yapilmiyor.

Bir sinir yaziyla kayda geciyor: bu aracin commit sirasi **tarih + SHA ordinal**, ana boru
hattinin sirasi ise tarih + satir kimligi. Uc esik de bu arac icinde ayni kurali kullandigi
icin **esikler arasi karsilastirma gecerli**, ama bu aracin 50 esigindeki sayilari ana
snapshot'in sayilariyla birebir ayni olmak zorunda degil.

## Sentetik gurultu: neden 11 / 13 kullanilmiyor

Asama 4'un elle dogrulamasindaki 11 / 13 (%84,6) orani **hedefli secilmis** bir
orneklemden geldi ve populasyona genellenemez. Bu deneyde kullanilmadi.

Onun yerine uc oran (%5 / %10 / %20) **kesfedilmis degil secilmis** degerler olarak ilan
edildi. Bunlar gercek kacirma orani iddiasi degil; "gizli pozitifler su kadar olsaydi
metrikler ne kadar oynardi" sorusunun uc noktasi.

Aday gizli pozitifler yalnizca `IsBugIntroducing = false` **ve** `CsFilesChanged > 0`
satirlar, cunku `.cs` degistirmeyen bir commit'in SZZ tarafindan kacirilmis olmasi mumkun
degil - orada etiket hic uretilmiyor.

**Orijinal snapshot hicbir zaman degismiyor:** cevirme islemi yeni bir liste uretiyor.

Olculen sonuc beklentinin **tersi** cikti: oran arttikca F1 ve PR-AUC dustu degil,
**yukseldi**. Raporda bir aciklama adayi var ve tahmin oldugu yaziyor; nedeni **olculmedi**.
Bu, deneyin bir basarisizligi degil: stress testinin isi metriklerin hangi yone ne kadar
oynadigini gostermek ve yonu de gosterdi.

## Neden her deneyin "neyi kanitlamadigi" yaziliyor

Duyarlilik deneyleri kolayca yanlis okunur. "Botlari cikarinca sonuc degismedi" cumlesi
"botlar onemsiz" diye okunabilir; oysa olculen sey yalnizca **bu modelin bu testindeki**
etki. "Olgun alt kumede F1 yuksek" cumlesi "sansurlu satirlar aslinda negatif" diye
okunabilir; oysa o **gosterilmedi**.

O yuzden her deneyin altinda ne gosterildigi ve ne gosterilmedigi ayri ayri yaziyor.

## Ad degisimi deneyinin commit sirasi (v2 duzeltmesi)

Deney iki kez kosuldu. **v1'de aracin commit sirasi ana metrik boru hattiyla ayni
degildi**: arac tarih + SHA ordinal siraliyordu, ana boru hatti ise tarih + madencilik
sirasi (veritabanindaki `Id`). Ayni saniyeye dusen commit'lerde bu iki siralama farkli
sonuc veriyor - olculdu: ayni `AuthorDateUtc` degerini paylasan gruplarin Polly'de
69'unun 40'inda, Jellyfin'de 321'inin 173'unde, ShareX'te 9'unun 4'unde `Id` sirasi ile
SHA sirasi farkli.

O yuzden v1'de esik 50 sonucu ana sonucla birebir uyusmuyordu ve "esik 50 ana kosudur"
iddiasi tam dogrulanmis degildi.

**v2'de siralama kurali tek bir yere yazildi** (`Sievert.Data.Metrics.CommitOrdering`) ve
arac o kurali kullaniyor; kural iki yerde metin olarak kopyalanmiyor. Ana boru hattinin
davranisi **degistirilmedi**: `MetricsRunner.Read` hala EF tarafinda
`OrderBy(AuthorDateUtc).ThenBy(Id)` diyor; ortak kural o cumlenin ne anlama geldigini
yazip test eden yer.

**Esik 50 kapisi** eklendi: v2 once yalniz 50'yi kosuyor, 15 ozniteligin tamamini
repo+SHA bazinda ana snapshot'la karsilastiriyor ve fark sifir degilse 40 ile 60'i
**kosmuyor**. Olculen: 512 490 hucre, 0 fark. Model sonucu da ana sonucla ayni cikti.

v1'in sayilari silinmedi; raporda siralama uyusmazligi notuyla duruyor.

## Sentetik gurultuyu taban oranindan ayirmak

Adim 4'te mutlak F1 ve PR-AUC oran arttikca yukselmisti. Bu tek basina modelin
gurultuden faydalandigini gostermez: sabit skorlu bir tabanin PR-AUC'si taban oranina
esit (ADR 0017), yani pozitif oran yukselince "hicbir sey yapmayan" bir yontemin PR-AUC'si
de yukselir.

O yuzden **mevcut deney degistirilmeden** uzerine eslenmis tabanlar eklendi: ayni
flip'ler, ayni tohumlar, ayni 100 tekrar. Her tekrarda ayni sentetik etiketler uzerinde
lojistik regresyon, `LinesAdded` esigi (esik yalniz sentetik train'de secilerek) ve
egitim oraniyla rastgele taban hesaplandi. Arac mevcut model sonuclarinin degismedigini
dogruladi ve dokuz durumun dokuzunda ayni cikti.

Dort ek olcu: PR-AUC lift, normalize PR-AUC, climatology Brier ve Brier skill score.
Tanimlari `asama5-gurultu-normalizasyon-sozlesmesi.md` surum 1.0'da, sonuc gormeden
yazildi.

**Yorum kapisi** da sozlesmede onceden yaziliydi: mutlak metrikler artiyor ama lift,
normalize PR-AUC ve tabanlara fark artmiyor, skill dusuyorsa "model gurultu arttikca
iyilesti" **yazilmaz**. Olculen sonucta tabanlara gore fark dokuz durumun dokuzunda da
artti, yani kapi kapanmadi - ama Jellyfin'de PR-AUC lift **dustu** (0,3608 → 0,3277), yani
artisin bir kismi taban oranindan geliyor. Rapor bunu repo bazinda sayilarla yaziyor.

Sebep **kanitlanmadi**: sonuc "sinif orani etkisiyle uyumlu" diye yaziliyor, "sinif orani
nedeniyle oldu" diye degil.

**Sonuc:** Alti deneyin hicbiri ana modeli ya da ana sonucu degistirmedi. Uc beklenti
tutmadi (90 gunluk filtrenin en cok Polly'yi degistirecegi, C# alt kumesinde
degerlendirmenin zorlasacagi, sentetik gurultunun metrikleri bozacagi) ve ucu de
"nedeni olculmedi" notuyla raporda duruyor.
