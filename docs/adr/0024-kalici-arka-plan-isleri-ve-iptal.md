# 0024 - Kalici arka plan isleri ve iptal

**Baglam:** Adim 1 ve 2 salt-okunur bir API birakti: her istek bir istek-cevap
donusumunde bitiyordu. Ama araci gercekten kullanmak icin gereken iki is oyle degil. Bir
deponun calisma agacini taramak saniyeler suruyor, 22 917 commit'i skorlamak da oyle. Bu
isleri bir HTTP isteginin icinde yapmak, istemciyi dakikalarca bekletmek ve zaman asimina
ugrayan her istekte isin yarida kalmasi demek.

Bu ADR o isleri istek disina tasiyan altyapinin kararlarini yaziyor.

**Karar:** Isler veritabaninda kalici kayitlar; kuyruk yalniz uyandirma mekanizmasi;
tekillik veritabani kisitiyla; iptal iki katmanli; yarida kalan is otomatik devam etmiyor.

## Neden veritabani tek dogru kaynak

Bir isin var oldugu, hangi durumda oldugu ve ne urettigi **veritabaninda** duruyor.
Bellekteki kuyruk yalnizca "bakilacak bir is var" haberini tasiyor.

Tersi de mumkundu: isi yalnizca kuyrukta tutmak. O zaman surec kapandiginda kuyruk
bosalirdi ve kullanicinin baslattigi is, baslatildigina dair hicbir iz birakmadan
kaybolurdu. Kullanici da bunu ancak sonucu beklerken anlardi.

Bunun bedeli var: her durum degisimi bir veritabani yazimi. Kabul edildi, cunku is sayisi
kucuk ve degisim sayisi da kucuk - 22 917 commit'lik bir is toplam 18 ilerleme yazimi
yapti.

## Neden kanal yalniz uyandirma icin

`Channel` sinirli kapasiteli (varsayilan 100) ve doldugunda **bekliyor**; oge atmiyor.

En eskiyi ya da en yeniyi atmak, veritabaninda `queued` duran bir isin hic
calistirilmamasi demek olurdu ve kimse bunu fark etmezdi. Beklemek geri basinc uretiyor.
Ustelik kuyruk bir sekilde is kaybetse bile kayit duruyor: yeniden baslatma kurtarmasi
`queued` isleri yeniden kuyruga aliyor.

## Neden es zamanlilik varsayilan 1, ust sinir 4

Varsayilan **1**: bu turun butun olcumleri tek is uzerinden alindi ve paralel kosan iki
isin birbirinin suresini bozmasini istemedim. Olculen sayilarin ne anlama geldigi belli
olsun diye.

Ust sinir **4**: uzeri icin bir gerekcem yok. Isler hem veritabanina hem CPU'ya yukleniyor
ve makine 8 cekirdekli; daha yuksek bir sayiya izin vermek, olculmemis bir yapilandirmayi
mumkun kilmak olurdu. Sinir disi bir deger acilista **durduruyor**, sessizce sinira
cekilmiyor: sessiz duzeltme, kullanicinin istedigi degerle kostugunu sanmasina yol acar.

## Neden aktif tekillik veritabani seviyesinde

Ayni depo ve ayni is turu icin ayni anda tek aktif is. Dayanagi
`ActiveDeduplicationKey` sutunundaki **benzersiz indeks**; sutun terminal durumda `null`
oluyor ve PostgreSQL'de `NULL`'lar birbirinden farkli sayildigi icin biten isler kisiti
hic gormuyor.

Yalnizca uygulama kontrolu de yazilabilirdi: "aktif is var mi diye bak, yoksa ac". O
kontrol iki es zamanli istekte ikisinin de ayni anda gecmesine acik. Olcumde farkli
anahtarlarla es zamanli 10 istek gonderildi: 1 kabul, 9 catisma. Kontrol tek basina olsa
10 is acilabilirdi.

Uygulama kontrolu yine de duruyor, cunku catismayi anlamli bir cevaba cevirmek icin
catisilan isin kimligi gerekiyor.

## Idempotency semantigi

`Idempotency-Key` istege bagli ama destekleniyor.

- Ayni anahtar, ayni depo ve tur: mevcut is doner, **yeni satir acilmaz**. Cevap `202`
  degil **`200`**: "kabul edildi" demek yaniltici olurdu, kabul edilen sey zaten vardi.
- Ayni anahtar, baska depo ya da tur: `409 IDEMPOTENCY_KEY_REUSED`. Anahtari baska bir ise
  tasimak, istemcinin kendi tekrar mantiginin bozuk oldugunu gosterir; sessizce yeni is
  acmak o bozuklugu gizlerdi.
- Bos ya da 128 karakterden uzun anahtar: `400`.

Anahtarin tam hali **gunluge yazilmiyor**; istemcinin urettigi bir kimlik ve gunluge
dusmesi gerekmiyor.

## Durum makinesi

```
queued  -> running | canceled | failed
running -> succeeded | failed | canceled
```

Terminal uc durumdan (`succeeded`, `failed`, `canceled`) **cikis yok**, ayni duruma
yeniden gecis de yok.

`queued -> failed` listede var ama normal yol degil; worker her zaman once `running`
yapiyor. Bu gecis yalnizca acik bir servis karari icin duruyor.

Butun gecisler **kosullu tek bir UPDATE**: beklenen onceki durum sorgunun `WHERE`
kismina giriyor ve etkilenen satir sayisi gecisi kazanip kazanmadigimizi soyluyor. Once
okuyup sonra yazmak iki worker'in ayni isi almasina acik kapi birakirdi.

Her terminal geciste `CompletedAtUtc` doluyor, `ActiveDeduplicationKey` temizleniyor ve
`IsResultComplete` yalnizca `succeeded` icin `true` oluyor.

## Neden yarida kalan is yeniden baslatmada devam etmiyor

Onceki surecte `running` kalmis bir is otomatik devam **ETMIYOR**; `PROCESS_INTERRUPTED`
ile `failed` oluyor.

Devam ettirmek icin isin nerede kaldigini bilmek gerekir. Bilmiyoruz: surec oldugu yerde
oldu, ilerleme sayaci en son ne zaman yazildiysa oraya kadar dogru. Kaldigi yerden devam
etmis gibi yapmak, yarim bir sonucu tamamlanmis gibi gostermek olurdu. Kullanici isterse
yeniden baslatiyor - bu bir tiklama, ama yanlis bir "tamamlandi" geri alinmiyor.

Kurtarma **idempotent**: ikinci kosuda degistirecek satir kalmiyor. Olcumde dogrulandi.

## Kismi sonuc politikasi

Iptal edilen ya da basarisiz olan bir isin yazdigi satirlar **duruyor**, silinmiyor:
2500 commit skorlandiysa o 2500 satir gercekten hesaplandi ve dogru.

Ama tam sonuc gibi **sunulmuyor**. `IsResultComplete` false kaliyor, sonuc ucu
`partial: true` ve acik bir uyari metni donuyor.

Bu politikanin bir kusuru olcumde ortaya cikti ve duzeltildi: is jeton uzerinden iptal
edildiginde sayilar isleyiciden cikamiyor, is "0 satir yazdi" diye kaydediliyordu - oysa
2250 satir duruyordu. Kismi sonucun buyuklugunu yanlis soylemek, kismi sonucu hic
soylememekten kotu. Iptal sonucundaki satir sayisi artik bellekteki sayactan degil
**veritabanindan sayiliyor**.

## Neden is sonuclari ana tablolara yazilmiyor

`StaticAnalysisFindings` ve `CommitRiskSnapshots` ise bagli; `Commits` tablosunun uzerine
yazilmiyor.

Dort sebep:

1. **Tekrar kosular.** Ayni repo birden fazla kez skorlanabiliyor; uzerine yazmak eski
   sonucu yok etmek olurdu.
2. **Kismi sonuc.** Yarida kalan bir isin satirlari ana tabloya yazilsaydi, tam sonuc ile
   kismi sonuc ayni yerde karisirdi.
3. **Farkli model surumleri.** Ileride yeni bir model egitilirse hangi skorun hangi
   modelden geldigi sorulabilmeli.
4. **Izlenebilirlik.** Her satir bir ise, her is bir zamana ve bir model ozetine bagli.

Depo iliskisi `Cascade` degil **`Restrict`**: bir depo silindiginde is gecmisi sessizce
yok olmasin.

## Neden ilk iki is turu bunlar

`static-scan` ve `risk-score-all`, urunun zaten yaptigi iki isin uzun suren hali. Ikisi de
**gercek**: biri dosya tariyor, digeri model kosuyor. Sahte bir bekleme isi yazmak
altyapiyi sinamazdi - iptal, ilerleme ve kismi sonuc ancak gercek is uzerinde anlam
kazaniyor.

Git klonlama, tarih madenciligi, metrik hesabi ve SZZ etiketleme bu turda **yok**. Onlar
disaridan veri cekiyor ya da ana tablolari degistiriyor; ikisi de ayri kararlar istiyor.

## Neden taranacak yol istekten alinmiyor

`static-scan` taranacak klasoru `Repository.LocalPath`'ten aliyor, istek govdesinden degil.

Istekten yol almak, kimlik dogrulamasi olmayan bir servise sunucudaki herhangi bir klasoru
okutmak olurdu. Tam yol yalnizca surec icinde kullaniliyor; API cevabina ve gunluge
girmiyor, bulgular goreli yolla saklaniyor.

Klasorun git deposu oldugu `.git` varligina bakilarak kontrol ediliyor. LibGit2Sharp'i tek
bir dogru/yanlis icin API'ye baglamadim; repo klonlama ve tarih okumanin bu turda olmadigi
karari bulaniklasirdi.

## Neden CLI ayri bir surec olarak baslatilmiyor

Arka plan isi CLI'nin `check` komutunu bir surec olarak calistirabilirdi. Calistirmiyor:
o zaman API'nin sonucu baska bir programin **cikti bicimini ayristirmaya** baglanirdi ve
bicim degistiginde API sessizce bozulurdu.

Bunun yerine tarama motoru CLI'dan cikarilip `Sievert.Analysis.ScanService` icine alindi;
CLI de arka plan isi de ayni servisi cagiriyor. Olcumde uc repoda da bulgu kumeleri
birebir ayni cikti - bu, umut degil yapinin sonucu.

Ayni sey risk tarafinda da yapildi: risk ucu ile arka plan isi `CommitRiskCalculator`
uzerinden geciyor ve 300 commit'te skorlar bit duzeyinde ayni.

## Neden ilerleme 500 ms ile sinirli

Rutin ilerleme yazimi arasinda en az 500 ms var. Asama degisiminde ve terminal geciste
yazim zorunlu.

Oge basina yazmak 22 917 commit'lik bir iste 22 917 `UPDATE` demek olurdu ve olculen
surenin buyuk kismi ilerleme yazmak olurdu. Olculen sayi: 18 yazim.

Bedeli su: kullanicinin gordugu ilerleme yarim saniyeye kadar eski olabilir. Bir ilerleme
cubugu icin bu onemsiz.

## Neden kimlik dogrulama yokken varsayilan loopback

Bu turda kimlik dogrulama **yok**. Kimlik dogrulamasi olmayan bir servisin butun ag
arayuzlerinde dinlemesi, veritabanindaki commit verisini ve model ciktilarini aga acmak
demek.

O yuzden varsayilan yalnizca loopback. `0.0.0.0`, `*`, `+` ya da gercek bir IP verilirse
API acilista **duruyor** ve ne yapilmasi gerektigini yaziyor. `SIEVERT_ALLOW_REMOTE=true`
ile acilabiliyor; acildiginda da gunluge uyari dusuyor.

Yalnizca `true` aciyor; `1` ya da `yes` kabul edilmiyor. Yanlis yazilmis bir ayarin acmis
gibi gorunmesindense hic acmamasi daha iyi.

CORS acilmadi, yani ayni-origin disina izin yok.

## Yeni sayisal tolerans

Aciklama dogrulamasi artik surum 2.0 kuraliyla yapiliyor:
`1e-6 + 2^-23 * (|kesisim| + toplam(|katki|))`. Gerekcesi ve olcumu
`docs/urun/model-aciklama-sayisal-tolerans.md` ile
`docs/olcumler/asama6-aciklama-toleransi.md` icinde. Ozeti: hata sonucun degil terimlerin
olceginde olusuyor, cunku ML.NET `float` ile topluyor.

Toleransi olculen sayiya bakarak sabit bir degere buyutmedim; kural IEEE 754 temsilinden
turuyor. Gevsemedigi mutasyon testiyle gosterildi.

## `?profile=` kaldirildi

Adim 2'de risk ucunda `?profile=` diye bir parametre vardi ve gerekcem
`EXTERNAL_MODEL_PROFILE` uyarisini ulasilabilir yapmakti. Gerekce tersineymis: sozlesmede
bir uyari kodu duruyor diye urune kapi acilmaz. Ustelik acilan kapi, repo-arasi aktarimin
tutarsiz olculdugu kararina (ADR 0021) aykiri calisiyordu.

Parametre kaldirildi ve sessizce yok sayilmiyor: gonderilirse `400` ve
`PROFILE_SELECTION_NOT_SUPPORTED` donuyor. Yok sayilsa istegin sahibi sectigi profille
skorlandigini sanardi.

## HTTP kodu secimleri

| Durum | Kod | Neden |
|---|---|---|
| Yeni is acildi | `202` + `Location` | Is kabul edildi, sonuc henuz yok |
| Ayni tekrar anahtari | `200` + `Location` | Yeni satir acilmadi |
| Ayni repo + tur aktif | `409` | Istek karsilanamadi, catisilan isin adresi ekte |
| Kuyruktaki ise iptal | `200` | Iptal tamamlandi |
| Kosan ise iptal | `202` | Istek kaydedildi, durmasi obek sinirinda |
| Terminal ise iptal | `200` + is oldugu gibi | Idempotent; bitmis isi yeniden yazmiyoruz |
| Yanlis turun sonuc ucu | `409` | Is var ama istenen sonuc turu onda yok |

`ANALYSIS_NOT_CANCELABLE` kodu tanimli ama normal akista **ulasilamiyor**: yalnizca is
biz bakarken durum degistirirse dusen savunma dalinda kullaniliyor. Bunu bilerek boyle
biraktim; kodu silmek, yaris durumunda ne donecegini belirsiz birakmak olurdu.

## Bu kararin sinirlari

- Tek surecli. Birden fazla API ornegi ayni veritabanina baglanirsa tekillik yine calisir
  (indeks veritabaninda) ama iptalin surec ici jetonu yalnizca kendi surecinde ise yarar;
  digerinde iptal obek sinirinda goruluyor. Bu senaryo **denenmedi**.
- Is oncelikleri yok; kuyruk FIFO.
- Zaman asimi yok. Sonsuza kadar kosan bir isi durduran tek sey iptal istegi.
- Yeniden deneme yok. Basarisiz bir is otomatik tekrarlanmiyor.
- Es zamanlilik yalnizca 1 ile olculdu.
