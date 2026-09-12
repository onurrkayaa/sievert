# 0019 - Kalibrasyon ve istatistiksel belirsizlik

**Baglam:** Adim 3 iki sey birakti. Birincisi ham olasiliklarin tek yonlu bozuk oldugu:
model her kalibrasyon kutusunda gercekte olandan yuksek olasilik veriyordu. Ikincisi
tabanla arasindaki farkin buyuklugunun bilinmedigi: mikro F1 +0,0654, makro F1 +0,0089,
ama bu farklarin gurultuden buyuk olup olmadigi olculmemisti.

Bu ADR ikisini de karara bagliyor: kalibrasyon nasil olculur ve fark nasil sinanir.

**Karar:** Ayri bir kalibrasyon alt bolmesi, iki onceden ilan edilmis kalibrasyon
yontemi, iki kosullu basari tanimi ve eslesmis dairesel hareketli blok bootstrap.

## Neden ayri bir kalibrasyon bolumu

Kalibrasyon, modelin gormedigi veriden ogrenilmeli. Modelin egitildigi satirlarda
ogrenilseydi, model o satirlarda zaten iyi uydugu icin duzeltme gereksiz gorunurdu ve
test uzerinde ise yaramazdi.

Train bolumu zaman sirasiyla ikiye ayrildi: ilk %80 model-fit, kalan %20 calibration.
**Ana train/test manifesti degismedi**; bu bolme mevcut train'in kendi icinde.

Sira zaman sirali, cunku ayni gerekce burada da gecerli (ADR 0016): rastgele ayrilsaydi
kalibrasyon gelecekteki commit'lerden ogrenilmis olurdu.

Bunun bedeli var ve yaziliyor: bu deneyin modeli Adim 3'un modelinden **daha az veriyle**
egitildi (Polly'de 1544 satir, 1931 yerine). O yuzden bu modelin ham sonucu Adim 3'un ham
sonucunun yerine gecmiyor ve ikisi "kalibrasyon etkisi" diye karsilastirilmiyor.

## Neden iki yontem ve neden kazanan secilmedi

**Platt scaling** bir bicim varsayiyor: bozukluk logit olceginde dogrusal. Az veriyle bile
kararli, ama bozukluk o bicimde degilse duzeltemiyor.

**Isotonic regression** bicim varsaymiyor, yalnizca monotonluk. Daha esnek, ama kucuk
kumelerde basamakli ve veriye duyarli.

Ikisi de **onceden** ilan edildi. Test sonucuna bakip aralarindan biri secilseydi, secilen
yontemin sonucu artik bir olcum olmazdi: test kumesi hem secimde hem raporda kullanilmis
olurdu. Ikisi de raporda yan yana duruyor.

## Kalibrasyon basarisinin iki kosulu

"Kalibrasyonu iyilestirdi" cumlesi yalnizca **hem Brier hem ECE** hamdan dusukse
yazilabiliyor. Biri iyilesip digeri kotulesirse **"karisik sonuc"** yaziliyor.

Sebep ikisinin farkli seyleri yakalamasi (ADR 0018): ECE kutu bazinda ortalama sapmayi
olcuyor ama model her satira ayni degeri verse bile kucuk cikabiliyor; Brier tek tek
satirlarin hatasini topluyor. Tek bir olcuye bakilsaydi, yalnizca onu iyilestiren bir
donusum "basarili" gorunurdu.

Olculen ornek bu kuralin ise yaradigini gosterdi: Jellyfin'de iki yontem de ECE'yi
dusurdu ama Brier'i yukseltti. Tek kosul olsaydi "iyilestirdi" yazilacakti; iki kosulla
"karisik sonuc" yazildi.

## Neden Newton-Raphson ve neden sonumlu

Platt'in iki parametresi kapali bicimde gradyan ve Hessian ile cozulebiliyor. Hazir bir
trainer kullanilsaydi onun varsayilan duzenlilestirmesi parametreleri kucultur ve
olculen sey "kalibrasyon arti bilinmeyen bir buzulme" olurdu.

**Sonumleme (step halving)** eklendi: tam Newton adimi olabilirligi kotulestirirse adim
yariya boluniyor, en fazla 50 kez. Sonumleme olmadan ilk adim asiri buyuk olabiliyor,
butun tahminler doyuma gidiyor ve Hessian tekillesiyor. Bu davranis gercek veriye
bakilmadan, bir birim testinde goruldu ve sozlesme o sirada guncellendi. Sonumleme
deterministik; tohum ya da rastgelelik icermiyor.

Yakinsamayan ya da egimi pozitif olmayan bir fit **sessizce kullanilmiyor**, hata
veriliyor. Monotonluk sart, cunku azalan bir donusum siralamayi tersine cevirirdi ve
"kalibrasyon" olmaktan cikardi.

## PR-AUC'nin kalibrasyondan nasil etkilendigi

**Platt monoton artan oldugu icin repo icindeki siralamayi degistirmiyor** ve olculen
PR-AUC uc repoda da ondalik basamagina kadar ayni cikti. Beklenen buydu.

**Mikro havuzda Platt yine de degistirdi** (0,4742 → 0,4770). Sebep: mikro havuz uc
reponun satirlarini birlestiriyor ve her repo **kendi** Platt donusumunu goruyor. Repo
icindeki sira korunuyor, repolar **arasindaki** sira degisebiliyor. Bu bir kalibrasyon
basarisi degil, mikro havuzun yapisindan gelen bir etki; beklenti dosyasinda bu ayrim
yazilmamisti ve sonradan fark edildi.

**Isotonic PR-AUC'yi her yerde bir miktar degistirdi.** PAV bloklari icindeki satirlar
ayni degeri aliyor, yani daha once ayri olan skorlar esitleniyor ve esit skorlar tek esik
grubu olarak isleniyor (metrik sozlesmesi 1.0). Bu degisiklik **kalibrasyon basarisi
sayilmiyor**, yonu ne olursa olsun; sozlesmede onceden boyle yaziliydi.

## Neden blok bootstrap, neden eslesmis, neden dairesel

**Blok:** test satirlari zaman sirali ve komsu commit'ler birbirine benziyor. Tek tek
satir cekmek bagimsizlik varsayardi ve belirsizligi oldugundan **dar** gosterirdi. Blok
uzunlugu `ceil(sqrt(N))`, yani veri buyudukce blok da buyuyor; sabit bir uzunluk uc
reponun cok farkli boyutlarinda ayni anlama gelmezdi.

**Eslesmis (paired):** model ve taban her tekrarda **ayni** yeniden orneklenmis satirlari
kullaniyor. Ayri orneklenselerdi, ikisi arasindaki fark orneklemin kendisinden gelen
degiskenligi de tasirdi.

**Dairesel:** bloklar repo sonunda basa sariyor. Sarma olmasaydi sondaki satirlar
bastakilere gore daha az secilir ve orneklem sistematik olarak kayardi.

**2000 tekrar:** dagilimin p2,5 ve p97,5 uclarini oturtmaya yetiyor. Sayi bir olcumden
degil makul bir yerden geliyor; daha fazlasinin gerekip gerekmedigi olculmedi.

## Bootstrap neyi kapsamiyor

**Model egitim belirsizligini kapsamiyor.** Tahminler sabit: mevcut `model-predictions.csv`
okunuyor ve model yeniden egitilmiyor. Olculen sey yalnizca **sabit tahminler uzerindeki
zamansal test ornekleme belirsizligi**.

Gercek belirsizlik bundan genis olabilir: farkli bir egitim bolumu farkli katsayilar ve
farkli bir esik uretirdi. Bu deney o kaynagi olcmuyor.

## Neden "p-degeri" ve "istatistiksel olarak anlamli" yazilmiyor

Bu bir hipotez testi degil. Aralik, tek bir veri kumesinin zaman bloklarindan yeniden
ornekleme ile uretildi; bir nul dagilim kurulmadi ve bir test istatistigi hesaplanmadi.

Yorum uc cumleden biriyle yaziliyor:

- Araligin alt siniri > 0 ise **"bootstrap araligi tabanin ustunde"**.
- Aralik 0'i iceriyorsa **"nokta tahmini ustunde, aralik fark yok degerini iceriyor"**.
- Ust sinir < 0 ise **"bootstrap araligi tabanin altinda"**.

Olculen sonuc bu ayrimi gerektirdi: mikro F1 ve mikro PR-AUC araliklari sifirin ustunde
kaldi, ama **makro F1 araligi sifiri iceriyor** ([-0,0296, 0,0563]) ve Polly'de nokta
tahmini tabanin altinda. "Model tabani gecti" cumlesi uc toplu olcutle Adim 3'te
yazilmisti; bu adim o cumlenin makro tarafinin dar oldugunu sayiyla gosteriyor.

## Kalibrasyon neden ana sonucun yerine gecmiyor

Adim 3'un ham Brier/ECE sayilari **silinmedi ve degistirilmedi**. Kalibre sonuclar ayri
dosyalarda (`calibration-results.json`) ve ayri raporda duruyor.

Sebep ADR 0018'deki sirayla ayni: once ham olasiligin ne kadar bozuk oldugu olculur, sonra
duzeltilir. Ham sayilar silinirse duzeltmenin ne kadar ise yaradigini soyleyecek referans
kalmaz.

**Sonuc:** Iki kalibrasyon yontemi de mikro toplamda hem Brier'i hem ECE'yi dusurdu;
Jellyfin'de ikisi de karisik sonuc verdi. Mikro F1 ve mikro PR-AUC farklarinin bootstrap
araliklari tabanin ustunde, makro F1 farkinin araligi fark yok degerini iceriyor.
