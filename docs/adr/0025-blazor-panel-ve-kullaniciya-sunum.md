# 0025 - Blazor panel ve kullaniciya sunum

**Baglam:** Adim 1-3 calisan bir API birakti: uc model profili, commit basina
degerlendirme, arka plan isleri ve iptal. Ama API'yi kullanan tek sey `curl` idi. Asama 5
kapanisinda yazilan sinirliliklarin - skor kalibre degil, hedef SZZ, insan dogrulamasi
dar - urun dilinde dogru durup durmadigi ancak bir ekranda gorulebilir.

Bu ADR paneli yazarken alinan kararlari ve onlarin gerekcelerini tutuyor.

**Karar:** Blazor Web App (Interactive Server), sozlesme ayri bir projede, panel API'ye
yalnizca HTTP ile bagli, is durumu saniyede bir soruluyor, dort sayi dort ayri kartta.

## Neden Blazor Interactive Server

Ucuncu bir dil eklemek istemedim. Panelde gosterilecek her sayi zaten C# tarafinda
tanimli; bir JavaScript arayuzu yazmak ayni sozlesmeyi iki dilde tekrar yazmak olurdu ve
iki taraf zamanla ayrisirdi.

Interactive Server, WebAssembly degil: model dosyalari ve veritabani sunucuda ve panelin
tarayiciya indirilecek bir hesabi yok. Ustelik WebAssembly, API'yi tarayicidan cagirmayi
gerektirirdi ve o zaman CORS acmak zorunda kalirdim - kimlik dogrulamanin olmadigi bir
serviste istemedigim bir sey.

Bedeli: her kullanici icin sunucuda bir devre acik kalmasi ve baglanti koptugunda
sayfanin yeniden baglanmasi gerekmesi. Tek kullanicili yerel bir arac icin kabul edilebilir.

## Neden sozlesme ayri bir proje

`Sievert.Contracts`'in **hicbir paket referansi yok** ve olmayacak. Panel bu projeye
bakiyor; `Sievert.Api`, `Sievert.Data` ve `Sievert.Modeling`'e bakmiyor.

Sebep tek cumle: referans olsaydi panelde yanlislikla kendi hesabini yapan bir satir
yazmak serbest olurdu. "Panel API'den okuyor, kendi hesabini yapmiyor" cumlesini iyi
niyet degil derleyici korumali. Bu kural Asama 6'nin basari olcutlerinden biri.

Hata kodlari ve uyari kodlari da bu projede: ikisi de sozlesmenin kendisi, istemci onlara
gore dallaniyor.

## Neden panel Sievert.Api'ye referans vermiyor

Panel API'yi HTTP uzerinden kullaniyor, sinif cagirarak degil. Bu, panelin gordugu seyin
API'nin gercekten dondugu sey olmasini garanti ediyor. Sinif cagirsaydi JSON
serilestirmesindeki bir sorun panelde hic gorunmez, yalniz `curl` kullanan birine
gorunurdu.

## Neden polling, neden SignalR yok

Is durumu **saniyede bir soruluyor**. Blazor Server zaten bir SignalR devresi tutuyor,
yani sunucudan istemciye itmek teknik olarak zor degil. Ama itmek icin API'nin panele
haber vermesi gerekir ve API ile panel ayri sureclerde; arada bir mesaj yolu kurmak
gerekirdi. Bu turda o yol yok.

Bir saniyenin gerekcesi: daha kisasi isin hizini degistirmiyor, yalniz API'ye daha cok
istek atiyor; daha uzunu ilerleme cubugunu kekeme gosteriyor. Olculen sonuc 30 saniyede
18 istek, ortanca aralik 1004 ms.

Dongu zamanlayici degil, tek bir dongu: bir sonraki istek oncekinin cevabi gelmeden
baslamiyor. Zamanlayici olsaydi yavas bir cevabin ustune ikinci bir istek binerdi. Olculen
es zamanli istek sayisi 1.

Hata halinde 1-2-5 saniyelik sinirli bir geri cekilme var. Sonsuz degil: API geri
geldiginde panel makul bir surede fark etmeli.

## Neden statik ve tarihsel sonuc ayri

Statik bulgular ve model skoru **ayri kartlarda** ve statik kartin her gorunumunde "bu
bulgular model skoruna dahil degildir" yaziyor.

Sebep olculmus bir eksiklik: statik bulgular ile model skorunu birlestiren bir agirlik
Asama 5'te **olculmedi** (risk sozlesmesi, `CombinedRisk`). Iki sayiyi yan yana
koymak insana onlari toplatiyor; ayri kart ve acik cumle, o toplamanin yapilmadigini
soyluyor.

## Neden RawModelScore yuzde degil

Ham skor dort ondalikli ve **yuzde isareti yok**. Yuzde isareti, kalibre edilmemis bir
sayiyi olasilik gibi gostermenin en kisa yolu. Uretim icin bir kalibrator secilmedi
(ADR 0019, ADR 0022), o yuzden skorun "%17" diye okunmasi olculmemis bir iddia olurdu.

Ayni sebeple panelde "hata olasiligi" ifadesi **olumsuz haliyle bile** gecmiyor. Ilk
yazdigimda "hata olasiligi degildir" demistim; cumle dogru ama ifade ekranda duruyor ve
yarim okuyan biri ayni seyi goruyor.

## Neden RiskIndex goreli

`RiskIndex` 0-100 arasinda ama yuzde degil: secilen model profilinin egitim skor
dagilimindaki yuzdelik sirasi. Panelde sayinin yaninda bu cumle duruyor.

Farkli profillerin endeksleri birbiriyle karsilastirilamaz, cunku her biri kendi egitim
dagilimina gore olcekleniyor. Panel ayni tabloda iki profil gostermiyor.

## Neden kalibrasyon rozeti

Her model kartinda, her skor satirinda ve her commit degerlendirmesinde "kalibre edilmedi"
rozeti var. Modeller sayfasinda ayrica Platt ve isotonic denemelerinin yapildigi ama
**uretim kalibratorunun secilmedigi** yaziyor.

Rozet bir susleme degil: `isCalibrated` alani sozlesmede bilerek duruyor ki gelecekte
bir kalibrator secilirse degistigi gorulebilsin.

## Neden bilinmeyen repo panelden skorlanmiyor

Panel "tum commit'leri skorla" dugmesini bir model profili olmayan depoda **devre disi**
birakiyor, ve API zaten `422` donuyor.

Repo-arasi aktarim tutarsiz olculdu (ADR 0021). Bilinmeyen bir depoyu baska bir reponun
modeliyle skorlamak, olculmemis bir aktarimi kullaniciya secenek gibi sunmak olurdu.

## Neden Idempotency-Key

Is baslatma dugmesi istek boyunca kapali, ve her istek icin **yeni bir tekrar anahtari**
uretiliyor. Ikisi birlikte cift tiklamayi kesiyor: dugme yetismezse bile ayni anahtarla
giden ikinci istek yeni bir is acmiyor.

Anahtar istemcide uretiliyor, sunucuda degil. Sunucuda uretilseydi iki tiklama iki ayri
anahtar alir ve anahtarin varlik sebebi ortadan kalkardi.

## Neden kismi sonuc gorunur ama uyarili

Iptal edilmis ya da basarisiz olmus bir isin satirlari **gosteriliyor**, ama basliginda
"bu sonuc tamamlanmamistir" yazan sari bir kutu ile.

Satirlari gizlemek, gercekten hesaplanmis bir sonucu yok saymak olurdu; Adim 3'te bir kez
bu hatayi yaptim ve is 2250 satir yazmisken "0 sonuc" diye kaydedildi. Tam sonucla ayni
renkte gostermek ise eksik bir listeyi tam liste sandirirdi.

## Neden harici CDN yok

Hicbir betik, stil ya da yazi tipi disaridan indirilmiyor. Panel internet olmadan da
acilmali; ustelik bu bir analiz araci ve kullandigi kodun nereden geldigi belli olmali.

Yazi tipi sistem yazi tipi. Ikonlar yerel SVG.

## Neden bu turda grafik yok

Katki cubuklari duz CSS; grafik kutuphanesi eklenmedi. Cubuk yalnizca goze yardim ediyor
ve her satirda sayinin kendisi isaretiyle yazili.

Bir grafik kutuphanesi eklemek harici bir bagimlilik ve bu turda gosterilecek tek sey
on katkinin buyuklugu. Bunun icin kutuphane eklemek, olculecek bir fayda olmadan yuzey
buyutmek olurdu.

Bu kararin bedeli gorsel kontrolde ortaya cikti: cubugun genisligi kultura bagli
bicimlenince `width:67,8%` yaziliyor ve tarayici bunu yok sayip cubugu tamamen
dolduruyordu. Bir kutuphane bu hatayi yapmazdi. Duzeltildi ve regresyon testi yazildi.

## `ANALYSIS_NOT_CANCELABLE` neden duruyor

Adim 3'un raporunda bu kod icin "yaris durumunda ulasilabilir" yazmistim. Bu turda
gecisleri tek tek izledim ve **uretebilen bir yol bulamadim**. Kod su dalda donuyor:
`CancelQueuedAsync` hicbir satir guncellemedi, sonra yapilan okuma isi hala `queued`
gosterdi. Ikisinin ayni anda dogru olmasi icin isin `queued` disina cikip **geri
donmesi** gerekir; durum makinesinde `queued`'a donen bir gecis yok.

Yani kod normal akista da, yaris halinde de uretilemiyor. Yine de siliyorum diyemedim:
dal bir savunma ve sildigimde, ileride durum makinesine bir gecis eklenirse uc sessizce
yanlis bir cevap doner. Kodun ulasilamaz oldugu burada ve sinirliliklar dosyasinda
yaziyor; "yaris durumunda ulasilabilir" cumlesi duzeltildi.

`REMOTE_ACCESS_NOT_ALLOWED` ise bu turda hata katalogundan **cikarildi**: o bir HTTP
cevabi degil, acilista ilk istek gelmeden verilen bir ret.

## Bu kararin sinirlari

- **Kimlik dogrulama yok.** Panel de API gibi varsayilan olarak yalniz loopback dinliyor.
- **Cok kullanicili degil.** Tek istemciyle olculdu; es zamanli devre sayisi denenmedi.
- **Sayfa acilisinda iki istek gidiyor.** Blazor once sunucuda on-isliyor, sonra devre
  baglaninca bileseni yeniden olusturuyor ve veri iki kez cekiliyor.
  `PersistentComponentState` ile onlenebilir; bu turda yapilmadi.
- **Isi haritasi, zaman cizelgesi, rapor ve PDF yok.** Adim 5 ve 6.
