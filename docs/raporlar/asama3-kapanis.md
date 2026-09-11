# Asama 3 kapanis raporu

**Tarih:** 2026-09-11
**Kapsam:** `6039dfb` ... `657de6e` (17 commit)

Asama 3'un amaci kural katalogunu kurmak, alti tespit kurali yazmak ve bunlari
gercek kod uzerinde olcmekti. Risk skoru, git gecmisi ve veritabani bu asamanin
disindaydi, hicbiri yazilmadi.

Bu asamanin ayirt edici yani su: ilk kez kurallar sadece yazilmakla kalmadi,
ciktilarina elle bakilip dogru olup olmadiklari sayildi. Cikan sayi kurallarin
yarisinin varsayilan kumeden cikarilmasina yol acti.

## 1. Ne yapildi

- **Alti kural** `IRule` arkasinda yazildi: SV001 async void (Asama 2'den devir),
  SV002 bloklayan gorev beklemesi, SV003 kayip gorev, SV004 dongu icinde sorgu,
  SV005 atilmayan nesne, SV006 iptal edilemeyen islem (`4419bad`, `6bbc389`).
- **Kural katalogu**: kod ile kural sinifi arasindaki esleme tek bir yerde
  (`RuleCatalog`); CLI'da ikinci bir liste yok (`47ec8a1`, ADR 0009).
- **Yapilandirma**: opsiyonel `sievert.json` kurallari acip kapatiyor ve bulgu
  seviyesini eziyor. Kural mantigi kodda kaldi (`47ec8a1`, ADR 0009).
- **Dislama**: `--exclude <kalip>` bayragi, elle yazilmis glob, eslesmeyen kalip
  uyarisi, atlanan klasorlerin raporlanmasi (`3008c4b`).
- **CI kendi kendini tariyor**: `check .` artik repo kokunde temiz donuyor,
  ayarlar repodaki `sievert.json`'dan geliyor (`3008c4b`, `47ec8a1`).
- **SV001'e ikinci kanit kaynagi** (Kademe 1): ayni dosyada ya da ayni partial
  sinifin parcasinda `+= MetotAdi` aramasi (`6039dfb`, ADR 0008).
- **Kurallar diski okumayi birakti**: taranan dosya kumesi kurala baglam olarak
  veriliyor, dislanan dosya artik kanit olamiyor (`47ec8a1`).
- **Uc repo olculdu**: Polly, ShareX (ikisi de Asama 2'den, ayni commit'lerde) ve
  yeni olarak Jellyfin (`a91b890`).
- **Kademe 1'in etkisi ayrica olculdu**: ayni repo, ayni commit, once/sonra
  (`f8906a4`).
- **30 bulgu elle dogrulandi**: Jellyfin'den her kuraldan bes bulgu, once kod
  baglami cikarildi (`9853173`), sonra kaynak koda bakilarak siniflandirildi
  (`c48ea9b`, `898d836`).
- **Varsayilan kume daraltildi**: SV003 ve SV005 olculen precision yuzunden
  varsayilan olarak kapatildi (`cde8dcf`).

**Dogrulama turu:** 30 bulgu kaynak koda bakilarak yazar tarafindan
siniflandirildi; bagimsiz bir degerlendirici kullanilmadi. Bu bir yanlilik
kaynagi ve `sinirliliklar.md`'nin genel bolumunde madde olarak duruyor.

## 2. Olculen sayilar

### Asama 2 sonu ile Asama 3 sonu

| Olcum | Asama 2 sonu (`ba43b97`) | Asama 3 sonu (`657de6e`) |
|---|---|---|
| Kural sayisi | 1 | 6 (4'u varsayilan acik) |
| Test sayisi | 141 (asama2-kapanis.md) | 272 (olculdu) |
| ADR sayisi | 7 | 10 |
| Sinirlilik maddesi | 12 (sayildi) | 33 (sayildi) |
| Komut | `scan`, `check` | ayni |
| Yapilandirma dosyasi | yok | `sievert.json`, opsiyonel |
| Taranan repo | 2 | 3 |
| Elle dogrulanmis bulgu | 20 (yarisi muafiyet) | 30 (hepsi bulgu) |

**Sinirlilik sayisi hakkinda bir duzeltme:** Asama 2 kapanis raporu o tarihte 14
madde oldugunu yaziyor. `ba43b97` commit'indeki dosyayi programatik olarak saydim,
12 cikti. Eski rapordaki sayi yanlis; silmiyorum ama dogrusu 12.

### Uc repo yan yana

Ayni Sievert surumu (`f8906a4`), alti kural acik (`asama3-jellyfin.md`).

| Olcum | Polly `2247db24` | ShareX `b5a397ea` | Jellyfin `1d7b6d97` |
|---|---|---|---|
| Dosya | 797 | 1138 | 2184 |
| Toplam satir | 104 388 | 224 930 | 351 887 |
| SV001 | 0 | 8 | 11 |
| SV002 | 22 | 35 | 23 |
| SV003 | 19 | 57 | 26 |
| SV004 | 5 | 304 | 277 |
| SV005 | 7 | 59 | 120 |
| SV006 | 178 | 96 | 421 |
| **Toplam bulgu (alti kural)** | **231** | **559** | **878** |
| **Varsayilan kumeyle** | **205** | **443** | **732** |

Jellyfin'de 878 ile 732 arasindaki 146'lik fark, varsayilan kumeden cikarilan iki
kuraldan geliyor: SV003'un 26 ve SV005'in 120 bulgusu.

### Bin satir basina bulgu yogunlugu

**Satir sayisi nereden geldi:** aracin kendi `scan --json` ciktisindaki dosya basina
`totalLineCount` alanlari toplanarak. Yani harici bir sayac kullanilmadi, olcum
aracin kendi okudugu satir sayisina dayaniyor. Bu sayi bos satirlari ve yorumlari da
iceriyor; "kod satiri" degil, "dosya satiri".

| Repo | Toplam satir | Bulgu (alti kural) | 1000 satirda | Varsayilan kumeyle |
|---|---|---|---|---|
| Polly | 104 388 | 231 | **2,21** | 1,96 |
| ShareX | 224 930 | 559 | **2,49** | 1,97 |
| Jellyfin | 351 887 | 878 | **2,50** | 2,08 |

Uc reponun yogunlugu birbirine yakin cikti (2,21 - 2,50). Bu, kurallarin repo
turunden bagimsiz sabit bir oranda atesledigi anlamina **gelmiyor**; kural bazinda
dagilim taban tabana zit. Polly'de bulgularin %77'si SV006, ShareX'te %54'u SV004,
Jellyfin'de %48'i SV006. Toplamin benzer cikmasi, farkli kurallarin farkli
repolarda birbirinin yerini almasindan geliyor.

## 3. Precision

30 bulgu, her kuraldan bes, Jellyfin uzerinde (`a64fa98`,
[asama3-precision.md](../olcumler/asama3-precision.md)).

| Kural | Seviye | E | H | Belirsiz | Precision | Havuzdaki bulgu |
|---|---|---|---|---|---|---|
| SV001 | error | 5 | 0 | 0 | **%100** | 11 |
| SV002 | warning | 5 | 0 | 0 | **%100** | 23 |
| SV003 | warning | 0 | 5 | 0 | **%0** | 26 |
| SV004 | warning | 1 | 4 | 0 | **%20** | 277 |
| SV005 | warning | 0 | 5 | 0 | **%0** | 120 |
| SV006 | info | 4 | 1 | 0 | **%80** | 421 |
| **Alti kural birlikte** | | **15** | **15** | **0** | **%50** | 878 |

| Asama 2'nin sayisi | Kural | Repo | Orneklem | Precision |
|---|---|---|---|---|
| Asama 2 | sadece SV001 | ShareX | 20 satir, yarisi muafiyet | %80 |

**Iki sayi kiyaslanamaz.** Farkli kural kumesi, farkli repo, farkli orneklem
tanimi. Asama 2'nin orneklemi yari muafiyetten olusuyordu, Asama 3'unki sadece
bulgudan. "Precision %80'den %50'ye dustu" demek yanlis olur.

**Ad temelli tespit en pahaliya SV004'te mal oldu.** Sebep tek basina precision
degil, precision ile hacmin carpimi: SV004 %20 precision ile Jellyfin'de 277,
ShareX'te 304 bulgu uretiyor; yani tahminen 220 ve 240 civari yanlis pozitif.
SV003 ve SV005 de %0 ama hacimleri kucuk (26 ve 120). Kullanicinin gordugu gurultu
acisindan en agir kural SV004.

## 4. Alinan kararlar

| Karar | Gerekce ve dayandigi olcum | ADR |
|---|---|---|
| SV001'in muafiyetine ikinci kanit kaynagi (`+= MetotAdi`) eklendi, kapsam ayni dosya ve ayni partial sinifla sinirli tutuldu | ShareX'te iki yanlis pozitifin ikisi de gercek olay abonesiydi; repo genelinde ad aramasi yapilamazdi cunku klonda `OnOpened` 42, `OnDrop` 17 ayri yerde abone ediliyor | ADR 0008 |
| Kural katalogu tek yerde, `sievert.json` sadece acip kapatiyor ve seviye eziyor; kural mantigi JSON'a tasinmadi | Mantigi JSON'a tasimak icine gomulu bir kural dili yazmaya cikiyor; bilinmeyen kural kodu ve bilinmeyen alan hata sayiliyor ki yazim hatasi sessizce kural kapatmasin | ADR 0009 |
| Alti kural da ad temelli tahmin yapiyor, uretecekleri yanlis pozitifler bilerek kabul edildi | Semantic model acmak derleme ve NuGet cozumu gerektiriyor; olcumden sonra bu kararin bedeli sayiyla belli oldu: 15 yanlis pozitifin 14'u dogrudan tip bilgisi eksikliginden | ADR 0010 |

### Varsayilan kume karari (`cde8dcf`)

Varsayilan kume artik "tanidigim her kural" degil, precision'i olculmus kume.

**SV003 ve SV005 kapatildi.** Ikisi de %0. SV003'un bes yanlis pozitifinin besi de
Moq kurulum ifadesi; cagri bir ifade agacinin icinde duruyor ve hic calismiyor.
SV005'in besinden ucu `MediaStream` uzerinde (`IDisposable` olmayan bir veri
sinifi), ikisi bir sonraki satirda `await using` ile atilan nesneler. Ikisi de acik
kalsaydi Jellyfin'de kullanicinin gordugu 878 bulgunun 146'si tamamen gurultu
olacakti ve bu 146'nin tamami yanlis.

**SV004 acik birakildi.** O da esigin altinda (%20), ama iki farki var. Birincisi,
dort yanlis pozitifinin tek bir kok nedeni var ve giderilebilir: ikisi bellekteki
koleksiyon, ikisi `Math.Max`. Ikincisi, %0 degil: kural en azindan bir gercek N+1
buldu (`context.LinkedChildren.Any(...)` dongu icinde). %0 olan bir kural duzeltme
yazilana kadar hicbir ise yaramiyor; %20 olan bir kural, gurultulu de olsa is
goruyor.

Kapatilan kurallar `sievert.json` ile acilabiliyor ve ozet ciktisinda
`Kapali kural : SV003, SV005` diye gorunuyorlar; sessiz bir daraltma degil.

## 5. Kademe 1'in olculen etkisi

Ayni repo, ayni commit, sadece SV001 acik (`f8906a4`,
[asama3-kademe1-etkisi.md](../olcumler/asama3-kademe1-etkisi.md)).

| Olcum | Once (`ba43b97`) | Sonra (`6bbc389`) |
|---|---|---|
| `async void` aday | 106 | 106 |
| SV001 bulgusu | 10 | **8** |
| Muaf tutulan | 96 | **98** |

Kaybolan iki bulgu, Asama 2'de elle "yanlis pozitif" diye isaretlenen 4 ve 6
numarali satirlarin ta kendisi. Yeni kaybolan bulgu yok, yani fazla muafiyet
uretilmedi. 98 muafiyetin 96'si imzadan, 2'si abonelikten geliyor; abonelik
kaynaklilarin biri ayni dosyadan, biri kardes partial dosyadan.

**Bu duzeltme Jellyfin orneklemindeki SV001 sonucuna etki etmedi.** Jellyfin'deki
bes SV001 bulgusunun dordu zamanlayici geri cagrisi, biri arayuz uygulamasi; Kademe
1'in duzelttigi "olay abonesi ama ozel delegate imzali" kalibi o ornekleme hic
girmedi. Yani **SV001'in Jellyfin'deki %100'u Kademe 1'e mal edilemez**; o bes satir
Kademe 1 olmasaydi da ayni sonucu verirdi. Kademe 1'in kanitlanmis etkisi ShareX'teki
iki satirla sinirli.

## 6. Bilerek birakilan sinirliliklar

`docs/sinirliliklar.md` 33 maddeye cikti (Asama 2 sonunda 12). Asama 3'te eklenen
baslica maddeler kural bazinda gruplandi. Duzeltilmeyen uc somut kusur:

- **SV003 ifade agaci korlugu.** Lambda ifadesi agaci icindeki cagrilar aday
  sayiliyor; orada duran cagri hic calismiyor. Bes yanlis pozitifin besi bu.
- **SV005'in iki sorunu.** Tip listesi ad ekine bakiyor (`MediaStream` yakalaniyor),
  ve `var x = new ...; await using (x...)` biciminde iki adimli `using` kalibi
  taninmiyor.
- **SV004'un `Math` elemesi yok.** `Max` ve `Min` sorgu bitirici listesinde ve
  `Math` uzerinde de ayni adla cagriliyorlar. 277 SV004 bulgusunun 41'i `Math`
  uzerinde (`asama3-dogrulama-baglami.md`).

Ucu de bilerek yazilmadi. Sebep: duzeltme oncesi sayilarin dosyada durmasi gerekiyor.
Duzeltmeyi yazip ayni orneklemi yeniden olcmek, Asama 2'de ogrenilen tuzaga dusmek
olurdu.

Bunlarin disinda tasinan yapisal sinirliliklar: semantic model yok, degerlendirici
yanliligi (kural yazari ile degerlendiren ayni kisi), SV006 arayuz bildirimlerini de
tariyor, SV002 orneklemi tek bicimden olustu.

## 7. Asama 4'e tasinan acik isler

- **Uc kural kusuru duzeltilecek ve yeniden olculecek.** Duzeltme ayri bir adimda;
  olcum **yeni bir orneklemle** yapilacak, bu 30 satirla degil.
- **Orneklem orani kurallar arasinda cok dengesiz.** Orneklemin havuza orani: SV001
  %45, SV002 %22, SV003 %19, SV005 %4,2, SV004 %1,8, SV006 %1,2. SV004 ve SV006
  hakkindaki sayilar en zayif olanlar; ikisi de en cok bulgu ureten kurallar.
- **Bellek buyumesi cozulmedi.** `ScannedFileSet` butun dosyalari ayni anda bellekte
  tutuyor. Ayni is icin tepe bellek %72 artti (ShareX'te 99 MB'den 170 MB'ye,
  `asama3-kademe1-etkisi.md`). Uc repoda egilim kabaca dogrusal: ~85 MB taban artı
  dosya basina 60-77 KB. Bu hizla 10 000 dosyalik bir repo 800 MB civarina cikar.
  Cozum onerilmedi, karar verilmedi.
- **Yollar repo kokune relatif olmali.** Su an `--config` calisma dizinine gore
  cozuluyor, tarama kokune gore degil. Baska bir repoyu tararken kendi dizinindeki
  dosya okunuyor; bu surpriz olabilir.
- **Gecmis bir daha yeniden yazilmayacak.** Asama 3'te iki kez yazildi (commit
  mesajlari, sonra yazar kimligi) ve butun hash'ler degisti. Olcum dosyalarindaki
  atiflar duzeltildi ve esleme tablosu `olcumler/commit-esleme.md`'ye yazildi, ama
  bir daha yapilirsa ayni is tekrarlanmak zorunda kalir. Olcum tekrarlanabilirligi
  olculen reponun degil olcen aracin surumune de bagli; o zincir kirilmamali.
- **SV002'nin iki dali hic sinanmadi.** `.Result` ve `.Wait()` varyantlari
  ornekleme girmedi; olculen %100 kuralin sadece bir dalini kapsiyor.
- **Ayni satirda cift bulgu.** SV004 ShareX'te 304 bulgunun 22'sinde, Jellyfin'de
  277'nin 7'sinde ayni satira iki kart basiyor. SV002 uc repoda da hic ciftlemedi.
  Davranis degistirilmedi, karar verilmedi.

---

## Tez icin kullanilabilecek bulgular

- **Ad temelli statik analizde yanlis pozitiflerin kok nedeni tek cinsten cikiyor.**
  Uc ayri kuralin uc ayri yanlis pozitifi ayni cumlenin ornegiydi: ad, tipin yerine
  kullanildi. `ReturnsAsync` gorev sanildi, `MediaStream` `IDisposable` sanildi,
  `Math.Max` LINQ sanildi. 15 yanlis pozitifin 14'u dogrudan tip bilgisi
  eksikliginden geliyor; kalan biri cerceve bilgisiyle cozulur.
- **Bir kuralin zarari precision ile hacmin carpimi.** SV003 (%0, 26 bulgu) ve SV004
  (%20, 277 bulgu) ayni tabloda cok farkli seyler. Sadece precision'a bakan bir
  siralama SV003'u daha kotu gosterir; kullanicinin gordugu gurultuye bakan bir
  siralama SV004'u.
- **Esik olcumden once ilan edilmezse ise yaramiyor.** ADR 0010'daki %50 esigi olcum
  yapilmadan once yazildi. SV004 %20 cikti ve esigi asagi cekmek mumkundu; oyle
  yapilmadi. Bir esigin degeri, olcum hosa gitmedigi zaman da gecerli olmasinda.
- **Toplam bulgu yogunlugu repo turunden bagimsiz gorunuyor ama dagilim degil.**
  Uc repoda 1000 satirda 2,21 - 2,50 bulgu cikti; ama Polly'de bulgularin %77'si
  SV006, ShareX'te %54'u SV004. Toplam benzerligi, farkli kurallarin yerini
  almasindan geliyor. Tek bir "yogunluk" sayisi repo hakkinda cok az sey soyluyor.
- **Duzeltmenin etkisi, duzeltmenin tasarlandigi orneklemin disinda olculmeli.**
  Kademe 1 ShareX'te iki yanlis pozitifi kapatti ama Jellyfin orneklemindeki SV001
  sonucuna hic etki etmedi, cunku o kalip orada hic cikmadi. Ayni duzeltmenin bir
  repoda buyuk bir repoda sifir etkisi olabiliyor.

## Mulakatta anlatilabilecek hikayeler

- **"Yazdigim alti kuralin ikisini kendi olcumumle kapattim."** Alti kural yazip
  hepsini acik birakmak kolaydi. 30 bulguya elle bakinca ikisinin precision'i %0
  cikti ve varsayilan kumeden cikardim. Kurallari silmedim, calisir halde
  duruyorlar ve isteyen aciyor; ama varsayilan olarak, olcemedigim bir seyi
  kullaniciya dayatmiyorum.
- **"Esigi olcumden once yazmistim, olcum esigin altinda cikti ve esigi
  degistirmedim."** ADR 0010'a "SV004 %50'nin altina duserse kullanilamaz" diye
  yazmistim. %20 cikti. O cumleyi yumusatmak ya da esigi dusurmek mumkundu; onun
  yerine kurali esigin altinda kabul edip neden yine de acik biraktigimi yazdim.
- **"Kendi duzeltmemin etkisini abartmaktan kil payi dondum."** Kademe 1'i yazdim,
  ShareX'te iki yanlis pozitifi kapatti. Sonra Jellyfin'de SV001 %100 cikti ve ilk
  refleksim bunu Kademe 1'e baglamakti. Bes bulguya tek tek bakinca dordunun
  zamanlayici geri cagrisi, birinin arayuz uygulamasi oldugunu gordum; Kademe 1'in
  duzelttigi kalip o ornekleme hic girmemisti. Duzeltme olmasaydi da ayni sonuc
  cikardi.
- **"Iki belirsiz satiri kaynak acarak cozdum."** Dogrulama sirasinda iki SV005
  bulgusunda nesnenin atilip atilmadigini goremiyordum. Tahmin edip "yanlis pozitif"
  yazmak yerine dosyalari actim: ikisinde de nesne bir sonraki satirda
  `await using` ile atiliyordu. Kuralin muafiyeti iki adimli bu kalibi tanimiyormus;
  hem satirlar dogru siniflandi hem de kuralin nereden yanildigi ortaya cikti.
- **"Kendi raporumdaki sayiyi sayinca yanlis buldum."** Asama 2 kapanis raporu 14
  sinirlilik maddesi oldugunu yaziyordu. Bu raporu yazarken o commit'teki dosyayi
  programatik olarak saydim: 12. Eski sayiyi silmedim, duzeltmeyi yanina yazdim.
