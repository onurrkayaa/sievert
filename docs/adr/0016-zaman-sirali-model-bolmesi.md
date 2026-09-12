# 0016 - Zaman sirali model bolmesi ve modele girmeyen alanlar

**Baglam:** Asama 5'te 34 166 commit'lik dondurulmus veri kumesinden bir hata tahmin
modeli kurulacak. Veri kumesinin her satirinda 22 sutun var ama bunlarin yalnizca 15'i
oznitelik. Geri kalan yedisi hedef, kimlik ya da denetim alani ve her biri modele
girerse olculen basariyi gercekte olmayan bir yerden alir.

Ayni sekilde, veriyi egitim ve test diye ikiye ayirmanin birden fazla yolu var ve
yanlis olan yol sessizce yuksek metrik uretir. Bu ADR ikisini birlikte karara baglıyor,
cunku ikisi de ayni seyi soruyor: model tahmin ederken neyi bilebilir.

**Karar:** Bolme zaman sirali, her repo kendi icinde, ilk `floor(N * 0,70)` egitim.
Modele yalnizca ADR 0013'teki 15 commit olcusu giriyor.

## Neden rastgele bolme yok

Rastgele bolme, 2026'da yazilmis bir commit'i egitime, 2014'te yazilmis bir commit'i
teste koyabilir. Model o zaman gelecegi gormus olur.

Etkisi dolayli ama buyuk: `PriorChanges`, `PriorFixes`, `AuthorCommitCount` gibi olculer
bir dosyanin ve bir yazarin **birikmis** gecmisini tasiyor. Egitimde 2026'nin durumunu
goren bir model, 2014'un test commit'ini degerlendirirken o dosyanin ileride cok
degisecegini dolayli olarak biliyor olur. Gercek kullanimda o bilgi yok: bir commit
gonderilirken gelecegi henuz yazilmamis.

Ayni sebeple **rastgele capraz dogrulama da yok.** k-fold, veriyi tanim geregi zaman
sirasindan bagimsiz boluyor. Kullanilacaksa yalnizca ileri giden (zaman sirali) bir
bicimi kullanilir ve o zaman ayrica yazilir.

Bunun bedeli var ve yaziyorum: tek bir bolme, k-fold'dan daha gurultulu bir tahmin verir,
cunku sonuc tek bir sinir noktasina bagli. Bu bedeli kabul ediyorum; alternatifi olcumu
gecersiz kilan bir sizinti.

## Neden her repo kendi icinde

Uc reponun tarih araliklari buyuk olcude ortusuyor: Polly 2013-2026, ShareX 2013-2026,
Jellyfin 2012-2026. Butun satirlari tek havuza koyup tarihe gore bolseydik, bir reponun
2022'deki commit'i "test", baska bir reponun 2024'teki commit'i "egitim" olabilirdi.

Model o zaman bir repoda gelecegi gormus olmazdi ama **baska bir reponun** gelecegini
gormus olurdu. Ayni-repo deneyinin sorusu "bu repoda gecmisten gelecegi tahmin edebilir
miyiz" oldugu icin bolme de repo icinde olmali.

Repo basina ayri bolmenin ikinci faydasi: uc reponun taban orani cok farkli (%9,5 /
%11,9 / %20,5). Tek havuzda bolunseydi test kumesinin bilesimi rastlantiya kalirdi.

## Neden sira numarasina gore, tarihe gore degil

`%70` sira numarasindan hesaplaniyor: siralanmis satirlarin ilk `floor(N * 0,70)` tanesi.

Tarihe gore bolunseydi (ornegin "2021'e kadar egitim") bolumlerin buyuklugu commit
yogunluguna kalirdi. Jellyfin'in commit'leri son yillarda yogunlasiyor, Polly'nin ilk
yillarinda. Ayni tarih kurali uc repoda cok farkli buyuklukte test kumeleri uretirdi ve
sonuclar karsilastirilamazdi.

## Neden floor

`floor` seciliyor, `round` ya da `ceil` degil. Kalan satir her zaman **test** tarafina
gidiyor.

Sebep tercih degil, tutarlilik: `floor` ile sinir veri buyudukce tek yone kayiyor ve
kural tek cumleyle yazilabiliyor. `round` kullanilsaydi N'in tek/cift olmasina gore sinir
bir satir oynardi ve "neden 1932 degil 1931" sorusunun cevabi yuvarlama olurdu.

Olculen sonuc: Polly 1931 / 828, ShareX 5943 / 2547, Jellyfin 16 041 / 6876.

## Esit timestamp'te neden SHA

Ayni saniyede yazilmis iki commit olabiliyor. Siralama yalnizca tarihe dayansaydi ikisinin
sirasi veritabaninin ya da okuma sirasinin keyfine kalirdi ve manifest her uretimde
degisebilirdi.

Esitlik durumunda `Sha` ile, **ordinal** karsilastirmayla siralaniyor. Ordinal secilmesinin
sebebi kulture bagimliligi kesmek: kulture duyarli bir karsilastirma isletim sistemine ve
dil ayarina gore farkli sonuc verebiliyor, ordinal vermiyor.

Sonuc: `RepositoryIdentity` + `AuthorDateUtc` + `Sha` birlikte benzersiz oldugu icin sira
tek. Manifest iki kez uretildi, iki dosya bayt bayt ayni.

Bir ayrinti kayda deger: olculerin hesaplandigi Adim 4'te commit sirasi tarih + satir
kimligi (`Id`) idi, burada tarih + `Sha`. Ikisi farkli siralar ama farkli isler icin: biri
gecmis birikimini uretiyor, digeri bolme sinirini yerlestiriyor. Ayni olmalari gerekmiyor,
ikisinin de deterministik olmasi gerekiyor.

## Hangi alanlar modele girmiyor

| Alan | Neden girmiyor |
|---|---|
| `IsBugIntroducing` | Hedef degisken. |
| `LabelSource` | Denetim alani; asagida ayrica yaziyor. |
| `Sha` | Kimlik. Her satirda benzersiz, ogrenilecek bir sey yok. |
| `Repository` | Kimlik, ustelik makineye ozel klasor adi. |
| `RepositoryIdentity` | Gruplama alani; asagida ayrica yaziyor. |
| `AuthorDateUtc` | Bolme icin. |
| `BotMu` | Bot karari Adim 4'e birakildi; asagida. |

Liste belge cumlesi degil, calisan kod: `ModelFeatures.Excluded` sozlugu ve
`ModelFeatures.EnsureNoExcluded` kontrolu. Yasak bir ad oznitelik listesine girerse
program duruyor ve hangi ad oldugunu yaziyor. Testler bunu her alan icin ayri siniyor.

## `LabelSource` neden dogrudan hedef sizintisi

`LabelSource`, etiketin nereden geldigini yazan alan. Su an tek deger `szz`,
etiketlenmemis commit'te bos.

Yani **dolu olup olmamasi hedefin kendisi.** Anlik goruntude 5962 satirda dolu ve hepsi
pozitif; "dolu ama negatif" tek satir yok. Bu alan modele girse model %100 dogrulukla
calisir ve hicbir sey ogrenmis olmaz.

Tehlikesi tam da bunun fark edilmesinin zor olusu: metrikler mukemmel cikar, kod
calisiyor gorunur, hata ancak gercek bir commit'te - yani `LabelSource`'un henuz
bos oldugu bir yerde - ortaya cikar.

Anlik goruntude yine de duruyor, cunku denetim icin gerekiyor: bir etiketin nereden
geldigi sonradan sorulabilmeli. Cozum alani atmak degil, modele sokmamak.

## `RepositoryIdentity` neden gruplama alani ama oznitelik degil

Bolme `RepositoryIdentity` ile yapiliyor: hangi satirin hangi repoya ait oldugu bundan
biliniyor ve her repo kendi icinde bolunuyor.

Modele girmiyor. Ayni-repo deneyinde zaten sabit bir sutun, yani bilgi tasimiyor.
Repo-arasi deneyde ise durum daha kotu: egitimde gorulmeyen bir deger test zamaninda
geliyor ve model onunla ne yapacagini bilmiyor. Bir modelin "bu satir Jellyfin'den
geliyor" bilgisini kullanmasi, uc reponun taban oranlarini ezberlemesi demek olurdu; oysa
sorulan soru commit'in kendisi hakkinda.

## Bot karari neden Adim 4'e birakildi

Anlik goruntude `BotMu` alani var ve uc repoda cok farkli davraniyor: Polly'de 854 / 2759
(%31,0), Jellyfin'de 928 / 22 917 (%4,0), ShareX'te 0.

Bot commit'lerini simdiden atmak ya da simdiden oznitelik yapmak, olculmemis bir karari
veriye gomerdi. Atmak: bagimlilik guncellemeleri de hata getirebiliyor, atilan satirlarin
ne goturdugu bilinmiyor. Oznitelik yapmak: bot tespiti bir heuristik ve bu projede bir
kez yanildi (ADR 0011, "Jason Botwick" gercek bir kisiydi).

Karar: `BotMu` anlik goruntude **duruyor**, ana modele **girmiyor**, Adim 4'te ayri bir
duyarlilik deneyinin konusu oluyor. Adim 4'e kadar bu karar degismiyor.

## Sag sansur ve 90 gunluk duyarlilik kumesi

Etiket, commit'ten **sonra** gelen bir duzeltmeden uretiliyor. Deponun sonuna yakin
commit'lerin arkasinda daha az tarih kaldigi icin onlari suclayacak duzeltme henuz
yazilmamis olabilir. Etiketin yoklugu bu commit'lerde "hata getirmedi" demek olmayabilir.

Olculdu (`asama5-bolme.md`): Polly'nin test bolumunde 180 gunden az olgun 207 commit var
ve **hicbiri pozitif degil**, oysa ayni reponun egitim bolumunde oran %13,0.

Karar: **ana %70/%30 bolme degismiyor**, hicbir satir silinmiyor. Adim 4'te ana sonucun
**yaninda**, en az 90 gun gozlem suresi olan test commit'leriyle ayri bir duyarlilik
sonucu hesaplanacak. Yani sansurun sonuca ne yaptigi olculecek, ama sansurlu satirlar
"rahatsiz ettikleri icin" atilmayacak.

90 gun degeri sonuclar gorulmeden, bu adimda ilan edildi.

## 11 / 13 sonucu neden tum negatif sinifa genellenmiyor

Asama 4'un elle dogrulamasi etiketsiz orneklemde 13 satirin 11'inde SZZ'nin kacirdigini
buldu. Bu orneklem butun negatiflerden rastgele alinmadi: "`IsFix` tarafindan dokunulmus
fakat etiketlenmemis" commit'lerden, yani kacirma ihtimalinin en yuksek oldugu yerden
**hedefli** secildi.

Hedefli bir orneklemden cikan oran, secildigi kumenin disina genellenmez. Bu yuzden
%84,6 ne negatif sinifin hata orani olarak yaziliyor, ne yeniden etiketlemede, ne de
agirliklandirmada kullaniliyor. Sonuc yalnizca "hedefli secilmis etiketsiz orneklemde
yuksek kacirma isareti" olarak duruyor.

Negatif sinifta etiket gurultusu riski var; **yayginligi bilinmiyor.** Bilmek icin butun
negatiflerden rastgele bir orneklem alip elle bakmak gerekirdi ve o olcum yapilmadi.

**Sonuc:** Bolme deterministik ve zaman sirali, manifest `data/asama5/split-manifest.csv`
icinde ozetiyle birlikte duruyor, modele girecek alanlar kodda siniliyor. Bu adimda model
kurulmadi; kurulunca olculen sayinin nereden geldigi bu dosyadan okunabilecek.
