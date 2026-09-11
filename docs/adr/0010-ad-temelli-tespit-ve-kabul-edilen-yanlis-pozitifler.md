# 0010 - Ad temelli tespit ve kabul edilen yanlis pozitifler

**Baglam:** Alti kuralin da ortak bir sorunu var: bir ifadenin gercekte ne oldugunu bilmiyorum. `x.Result` bir gorev mi bekliyor yoksa siradan bir property mi okuyor, `ToList()` veritabanina mi gidiyor yoksa bellekteki bir listeyi mi kopyaliyor, `new FileStream(...)` gercekten `IDisposable` mi - bunlarin hicbirine kaynak metnine bakarak kesin cevap veremiyorum. Cevap semantic model'de: derleyicinin tip cozumlemesi.

**Karar:** Semantic model'i simdilik acmiyorum. Kurallar ada bakiyor ve urettikleri yanlis pozitifleri bilerek kabul ediyorum. Kabul ettigim her seyi `docs/sinirliliklar.md`'ye madde madde yaziyorum.

## Neden semantic model yok

**Kapsam.** Semantic model icin projeyi derlemek gerekiyor: `MSBuildWorkspace` acmak, NuGet paketlerini geri yuklemek, referanslarin cozulmesini beklemek. Bu, "bir klasordeki .cs dosyalarini oku" isinden cok farkli bir is. Taradigim iki repodan biri (ShareX) Avalonia ve onlarca pakete bagli; o agaci gercekten derlemek, aracin kendi isinden daha buyuk bir kurulum problemi olurdu. Su anki hali herhangi bir klasorde, hatta tek bir `.cs` dosyasinda calisiyor - derlenmeyen, yarim, kopyalanmis kodda bile.

**Hiz.** Sozdizimi ayristirmasi ShareX'in 1138 dosyasini iki saniyede okuyor. Derleme acmak bunu dakikalara cikarir. Aracin amaci commit basina risk tahmini; bir commit'i degerlendirmek dakikalar suruyorsa kimse kullanmaz.

**Asama sirasi.** Asil hedef git gecmisi ve risk skoru (Asama 4 ve 5). Kural motoruna semantic model eklemek o isleri geciktirir. Once "kural mantiginin kendisi ise yariyor mu" sorusunu ucuz yoldan cevaplamak istedim.

## Hangi kural neye bakiyor

| Kural | Ad temelli tahmin | Yanildiginda ne olur |
|---|---|---|
| SV001 | Ikinci parametrenin tip adi `EventArgs` ile bitiyor mu; `+= MetotAdi` var mi | Iki yone de yanilir; olculdu |
| SV002 | Uzerinde `.Result` / `.Wait()` cagrilan ifadenin adi `Async` / `Task` ile bitiyor mu | Adi uymayan gercek gorevler kacar; `sonucTask` adli siradan bir nesne bulgu uretir |
| SV003 | Cagrilan metodun adi `Async` ile bitiyor mu | `Task Save()` gibi metotlar hic gorulmez |
| SV004 | Cagri adi `ToList` / `Any` / `Count` gibi bir sorgu bitirici mi | LINQ-to-objects uzerinde de esleser; bellekteki liste sorgu sanilir |
| SV005 | Tip adi listede mi, ya da `Stream` ile mi bitiyor | Adi listede olmayan disposable'lar kacar |
| SV006 | Donus tipi `Task` / `ValueTask` yazilmis mi; `override` ya da explicit arayuz uygulamasi mi | Ortuk arayuz uygulamalari muaf tutulamaz, gereksiz bulgu uretir |

## Bedeli ne, ve %80 ile iliskisi

Asama 2'de SV001'in ShareX uzerindeki ciktisindan 20 satir elle incelendi ve precision %80 cikti: 10 bulgunun 8'i gercek. Uc hatanin ucu de ayni kok nedenden geliyordu - heuristik yanlis olcute, metot **imzasina** bakiyordu. Kademe 1'de ikinci bir kanit kaynagi (`+= MetotAdi`) eklendi ve iki yanlis pozitif kapandi.

Buradan cikardigim iki sey var, ve yeni kurallari yazarken ikisini de kullandim:

1. **Bir kuralin kalitesi istisnasinin kalitesidir.** ShareX'te 106 adayin 96'si istisnayla elendi. Yeni kurallarin hepsinde istisnalari kuralin kendisi kadar ciddiye aldim: SV005'in dort, SV006'nin uc muafiyeti var ve her biri sebebiyle kaydediliyor.
2. **Bir kanit kaynagi yetmiyor.** SV001 iki kanit kaynagina gecince duzeldi. Ayni fikri SV004'te farkli bir bicimde uyguladim (asagida).

Ama durust olmak gerekirse: **%80 sayisi yeni kurallar icin gecerli degil.** O olcum SV001'e ait, tek bir repoda, 20 satirlik bir orneklemde yapildi. SV002-SV006 icin hicbir olcum yok. Bu kurallarin precision'i %80'den cok daha dusuk olabilir - ozellikle SV004'un, cunku LINQ her yerde kullaniliyor ve kural veritabani sorgusu ile bellekteki liste islemini ayirt edemiyor. Bunu bilerek boyle birakiyorum; bir sonraki is olcmek.

## SV004'te ipucunu filtre olarak kullanmama karari

SV004, cagrinin uzerindeki ifadede `Context`, `Db`, `Repo` ya da `Set` gectiginde bunun gercekten bir veritabani sorgusu olma ihtimalinin yuksek oldugunu biliyor. Bu ipucunu **bulgu mesajina** koydum ama **filtreye** koymadim.

Filtre yapsaydim precision hemen yukselirdi ve rakam daha iyi gorunurdu. Ama `_uow.Visits.ToList()` ya da `patients.Where(...).ToList()` gibi, adinda bu kelimeler gecmeyen gercek N+1'ler sessizce kacardi. Hangisinin daha pahali oldugunu bilmiyorum - cunku olcmedim. Olcmeden recall'u dusuren bir karar vermek istemedim; su an gereksiz bulgulari gormek, olcum icin gereken veriyi de veriyor. Filtre eklemek her zaman mumkun, kacirilmis bulguyu geri getirmek degil.

## Ne zaman semantic model'e gecmek gerekir

Su uc isaretten biri cikarsa:

- **Olcum precision'i kabul edilemez gosterirse.** SV004 icin bir esik koyuyorum: gercek bir repoda ornekleme bakildiginda precision %50'nin altina duserse, kuralin ad temelli hali kullanilamaz demektir. O noktada ya kural kaldirilir ya semantic model acilir.
- **Kullanici bulgulari kapatmaya baslarsa.** `sievert.json` ile bir kural kapatiliyorsa, o kural ise yaramiyor demektir. Yapilandirma bu yuzden var: hangi kuralin gurultu oldugunu gormek icin.
- **Risk skoru kalibrasyonu bozulursa.** Asama 5'te bulgular risk skoruna girecek. Yanlis pozitifler skoru sistematik olarak sisirirse, kalibrasyon (Brier, ECE) bunu gosterecek. O zaman sorun kuralin kendisinde degil, ona duyulan guvende olur.

Gecis olursa tek seferde olmayacak: once semantic model'in maliyeti olculecek (ayni iki repoda sure ve bellek), sonra sadece ona ihtiyaci olan kurallar tasinacak. SV001'in imza heuristigi gibi ucuz ve yeterince dogru calisan parcalar yerinde kalabilir.

**Sonuc:** Alti kural da calisiyor ve hepsi yanlis pozitif uretebilir. Bu bir kusur degil, bilincli bir takas: dogruluk yerine kapsam ve hiz. Takasin bedeli `docs/sinirliliklar.md`'de 12 maddeye yazildi. Takasi gecerli kilan sey olcum: olculmemis bir heuristik, tahmin edilmis bir heuristikten farksiz. SV001 olculdu, digerleri henuz olculmedi ve bunu README'de de yaziyorum.

## Olculen sonuc (2026-09-11)

Yukarisi olcumden once yazilmisti. Asama 3 Adim 6-8'de alti kural Jellyfin uzerinde
calistirildi ve her kuraldan bes bulguya elle bakildi. Sonuclar
`docs/olcumler/asama3-precision.md`'de; burada olcumun bu ADR'deki iddialara ne yaptigi
yaziyor.

**Esik olcumden once ilan edilmisti ve sonuca gore degistirilmedi.** Yukaridaki "ne zaman
semantic model'e gecmek gerekir" bolumunde SV004 icin %50 esigi yaziliydi ve o metin
olcum yapilmadan once yazilmisti. Olcum SV004'u %20 gosterdi, yani esigin altinda. Esigi
yukaridan asagi cekip kurali kurtarmak ya da "aslinda %20 de kabul edilebilir" demek
mumkundu; yapmadim. Bir esigin ise yaramasi, olcum hosuma gitmedigi zaman da gecerli
olmasina bagli.

**Alti kuralin birlikte precision'i %50: 15 dogru, 15 yanlis pozitif.** Dagilim esit degil:
SV001 ve SV002 %100, SV006 %80, SV004 %20, SV003 ve SV005 %0.

### Uc ayri kuralin yanlis pozitifi tek cinsten cikti

Olcumden once "ad temelli tahmin yanilir" diye yazmistim ama nasil yanilacagini
bilmiyordum. Elle inceleme sunu gosterdi: uc ayri kuralin uc ayri yanlis pozitifi, ayni
cumlenin uc ornegi. **Her uculunde de ad, tipin yerine kullanildi.**

| Kural | Gorulen ad | Varsayilan sey | Gercek sey |
|---|---|---|---|
| SV003 | `ReturnsAsync` | `Async` ile bitiyor, demek ki `Task` donduruyor | Moq kurulum nesnesi donduruyor; ustelik cagri bir ifade agacinin icinde, hic calismiyor |
| SV005 | `MediaStream` | `Stream` ile bitiyor, demek ki `IDisposable` | Siradan bir veri sinifi, `IDisposable` degil |
| SV004 | `Math.Max` | `Max` bir sorgu bitirici, demek ki LINQ | `System.Math` uzerinde statik bir cagri, LINQ ile ilgisi yok |

Ucu de "ad soyle goruniyor, o halde tip soyledir" cikarimi. Ucunde de tip bilgisi olsaydi
soru bir anda kolay olurdu: `ReturnsAsync`'in donus tipi `Task` mi, `MediaStream`
`IDisposable` uyguluyor mu, `Math` bir `IEnumerable` mi. Bunlarin hepsi semantic model'in
tek satirda cevapladigi sorular.

**Semantic model'e gecis gerekcesi artik olcume dayaniyor.** Bu ADR'yi yazarken gerekce
"ad temelli tahmin yanilabilir" diye teorikti. Simdi 30 bulgunun 15'i yanlis ve bunlarin
14'u (SV003'un besi, SV005'in besi, SV004'un dordu) dogrudan tip bilgisi eksikliginden
geliyor. Geriye kalan tek yanlis pozitif (SV006'nin middleware `Invoke`'u) tip bilgisiyle
degil, cerceve bilgisiyle cozulur.

### Karar: varsayilan kume daraltildi

SV003 ve SV005 varsayilan kural kumesinden cikarildi, `sievert.json` ile acilabiliyorlar.
SV004 esigin altinda olmasina ragmen acik birakildi, cunku yanlis pozitiflerinin tek ve
giderilebilir bir kok nedeni var; SV003 ve SV005'inki de oyle ama onlarda oran %0, yani
duzeltme yazilana kadar kural hicbir dogru bulgu uretmiyor.

Bu, "kuralin ad temelli hali kullanilamaz" sonucunun uygulanmis hali. Kurallar silinmedi;
olculen halleriyle varsayilan olarak calismiyorlar.

### Duzeltmeler bu adimda yazilmadi

SV003'un ifade agaci muafiyeti, SV005'in tip listesi ve SV004'un `Math` elemesi bilerek
yazilmadi. Sebep: duzeltme oncesi sayilarin dosyada durmasi gerekiyor. Duzeltme yazilip
ayni orneklem yeniden olculurse, Asama 2'de ogrenilen tuzaga dusulmus olur - duzeltmenin
uzerinde tasarlandigi orneklemle olculmesi bir dogrulama degildir. Duzeltmeler ayri bir
adimda, olcum ise yeni bir orneklemle yapilacak.
