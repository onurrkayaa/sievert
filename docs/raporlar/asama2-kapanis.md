# Asama 2 kapanis raporu

**Tarih:** 2026-09-11
**Kapsam:** `6fde0c3` (yeniden yazma oncesi: `7eac257`) ... `889fbef` (yeniden yazma oncesi: `b60e07e`)

Asama 2'nin amaci ilk kurali yazmak ve onu gercek kod uzerinde olcmekti. Risk
skoru, git gecmisi ve veritabani bu asamanin disindaydi, hicbiri yazilmadi.
Kural sayisi bir: SV001 async void.

Bu asamada iki repo tarandi (N = 2): App-vNext/Polly ve ShareX/ShareX. Iki repo
istatistik icin az; asagidaki her sayi bu iki repo hakkinda, C# hakkinda degil.

## 1. Ne yapildi

- Kural altyapisi: Core'da `Severity` ve `Finding`, Analysis'te `IRule`,
  `AsyncVoidRule` ve `RuleRunner`. `IRule` Core'a konmadi cunku Roslyn tipi
  aliyor (ADR 0004, ADR 0007).
- Ilk kural SV001: async isaretli, `void` donen metotlari buluyor. Iki
  parametre alan ve ikincisinin tip adi `EventArgs` ile biten metotlar event
  handler sayilip muaf tutuluyor.
- `sievert check <yol>` komutu: kurallari calistirip bulgulari teshis karti
  halinde basiyor, `--json` ve `--fail-on <seviye>` var.
- Cikis kodu uclemesi: 0 temiz, 1 esigi gecen bulgu var, 2 arac calisamadi
  (ADR 0006).
- Butun kod Ingilizce adlandirmaya cevrildi (ADR 0005). Yorumlar ve CLI'in
  ekrana bastigi metinler Turkce kaldi.
- Polly ve ShareX tarandi, ShareX ciktisindan 20 satirlik bir orneklem elle
  dogrulandi. 141 test yaziliyor ve geciyor (Asama 1 sonunda 91'di).

## 2. Olculen sayilar

### Iki repo yan yana

Ayni kural, ayni Sievert surumu, iki farkli repo.

| Olcum | Polly `2247db24` | ShareX `b5a397ea` |
|---|---|---|
| Repo turu | kutuphane | masaustu uygulamasi |
| Dosya | 797 | 1138 |
| Metot | 4655 | 7376 |
| Async metot | 991 (%21,3) | 577 (%7,8) |
| `async void` aday | **0** | **106** |
| SV001 bulgusu | **0** | **10** |
| Muaf tutulan | 0 | **96** |
| Uretim / test dosya | 479 / 318 | 1138 / 0 |
| Cikis kodu | 0 | 1 |
| Sure | 0,95 - 1,00 sn | 1,93 - 2,02 sn |

**Polly'nin sifiri bir sonuctur, eksiklik degil.** Polly bir kutuphane;
`async void` esas olarak UI event handler'larinda cikan bir kalip, kutuphane
kodunda zaten beklenmemesi gerekiyordu. Sifir, kuralin bozuk oldugunu degil,
reponun bu kural icin dogru orneklem olmadigini soyluyor. Sifirin aracin
hatasindan gelmedigi uc ayri yolla dogrulandi: aracin kendi `scan --json`
ciktisindan sayarak, Roslyn'den bagimsiz duz metin aramasiyla (10 eslesmenin
onu da yorum satiri), ve kuralin ornek dosyada hala atesledigini kontrol
ederek.

Iki repo yan yana konunca ters bir sey gorunuyor: async orani **yuksek** olan
repo (Polly %21,3) hic `async void` icermiyor, async orani **dusuk** olan repo
(ShareX %7,8) 106 tane iceriyor. `async void` sikligi reponun ne kadar async
oldugundan degil, ne isi yaptigindan geliyor.

### Asil sayi 96

ShareX'te 106 `async void` metot var. Bunlarin 10'u bulgu olarak cikti, **96'si
event handler istisnasiyla elendi**. Yani kuralin ciktisini belirleyen sey
kuralin kendisi degil, istisnasi: ham kural 106 bulgu verecekti, istisna bunu
10'a indirdi.

**Kuralin kalitesi = istisnanin kalitesi.** SV001'in "async void bul" kismi
onemsiz denecek kadar basit ve zaten dogru calisiyor. Kuralin ise yarayip
yaramamasini belirleyen, o 96 elemenin dogru olup olmadigi.

### Elle dogrulama

ShareX ciktisindan 20 satirlik bir orneklem elle incelendi: 10 bulgunun hepsi
ve 96 muaf tutulandan 10 ornek, 18 ayri dosyadan, sekiz projeden, 1 satirdan
446 satira kadar metotlar.

| Olcum | Deger |
|---|---|
| Bulgu | 10 |
| Gercek sorun | 8 |
| Yanlis pozitif | 2 (satir 4 ve 6) |
| **Precision** | **8/10 = %80** |
| Incelenen muafiyet | 10 |
| Dogru muafiyet | 9 |
| Yanlis negatif | 1 (satir 13) |

### Uc hatanin tek kok nedeni

4, 6 ve 13 ucu de ayni sebepten yanlis: heuristik **imzaya** bakiyor. Dogru
olcut metodun imzasi degil, **bir olaya abone olup olmadigi**.

- 4 ve 6 gercekten olaya abone (`vm.LoadFromUrlRequested +=`,
  `HotkeyTrigger +=`) ama ozel delegate imzalari `(object sender, XEventArgs e)`
  kalibina uymadigi icin istisnaya giremediler.
- 13 imza kalibina uyuyor, o yuzden muaf tutuldu; ama hicbir olaya abone degil.

### Uc kademeli iyilestirme

| Kademe | Ne yapar | Hangi satiri kurtarir |
|---|---|---|
| 1 | Ayni dosya ya da ayni partial sinifta `+= MetotAdi` ara | **4 ve 6** — iki yanlis pozitifin ikisi |
| 2 | `.axaml` / `.xaml` ozniteliklerini abonelik say | Bu orneklemde yeni satir kurtarmiyor; 11, 17, 19'u ad tahmini yerine gercek abonelige baglar |
| 3 | Semantic model + sembol esitligi | **13** — tek yanlis negatif |

Kademe 2 tek basina 13'u duzeltmez, tersine yanlis muafiyeti pekistirir:
`EditorView.axaml` uc yerde `PointerPressed="OnCanvasPointerPressed"` yaziyor ve
ada bakan bir kontrol bunu 13 numaraya baglar. Oysa o oznitelik kod arkasi
sinifi `EditorView`'in `EditorView.axaml.cs:1241`'deki ayni adli void metoduna
baglaniyor; async void olan metot `EditorInputController.cs:129`'da ve ona
sadece 1243. satirdan cagri yapiliyor.

**Uyari:** Kademe 1'in bu orneklemde precision'i %100'e cikarmasi bir dogrulama
degildir; duzeltme tam da bu 20 satira bakilarak tasarlandi. Kademe 1
uygulandiktan sonraki olcum, bu listede olmayan yeni bir orneklemle
yapilacaktir.

### Sayilar onceki halleriyle

| Olcum | Asama 1 sonu (`4c8b5dc`, yeniden yazma oncesi `ae049e2`) | Asama 2 sonu (`889fbef`, yeniden yazma oncesi `b60e07e`) |
|---|---|---|
| Kural sayisi | 0 | 1 |
| Komut | `scan` | `scan`, `check` |
| Test | 91 | 141 |
| Taranan repo | 1 (Polly) | 2 (Polly, ShareX) |
| Cikis kodu | 0 ya da 1 (iki anlamli) | 0 / 1 / 2 |
| `byRuleCode` JSON | yoktu | dizi (once dinamik anahtarli nesneydi) |
| Sinirliliklar maddesi | 8 | 14 |
| ADR | 4 | 7 |
| Polly tarama suresi | 1,32 - 1,40 sn (`scan`) | 0,95 - 1,00 sn (`check`) |

## 3. Beklemedigim bulgular

**Ilk orneklemim kural hakkinda hicbir sey soylemedi.**
Polly'yi SV001'i dogrulamak icin sectim ve sifir bulgu cikti. Sifirin kendisi
dogru bir sonuc ama dogrulama acisindan kullanissiz: event handler istisnasinin
calisip calismadigini olcemedim, cunku muaf tutulacak tek bir metot bile yoktu.
Kural hakkinda o taramadan ogrendigim tek sey yanlis pozitif uretmedigiydi.
Orneklem secimi, olcum yapmak kadar onemliymis.

**Kuralin ciktisini istisna belirliyor, kural degil.**
106 adayin 96'si istisnayla eleniyor. Bunu yazmadan once "kural" ve "istisna"yi
esit agirlikta iki parca saniyordum; olcum, isin %90'inin istisnada oldugunu
gosterdi. Bir sonraki kuralda istisnayi kuralin kendisi kadar ciddiye almam
gerekiyor.

**Ucu de ayni kok nedenden.** Iki yanlis pozitif ve bir yanlis negatif ilk
bakista uc ayri problem gibi duruyordu. Elle inceleme, ucunun de "imzaya bakmak
yerine abonelige bakmak gerekiyor" cumlesine indigini gosterdi. Tek bir olcut
degisikligi ucunu birden cozuyor.

**Polly'nin kendi test kodu, goremedigim tuzagi anlatiyor.**
`async void` diye grep atinca cikan 10 satirin onu da yorumdu ve bu yorumlar
`async void` **lambda** problemini anlatiyordu
(`test/Polly.Specs/Retry/RetryAsyncSpecs.cs:400`). SV001 sadece
`MethodDeclaration` dugumlerine baktigi icin bunu goremiyor. Kapsami bilerek dar
tutmustum ama gercek bir repoda tam o tuzagin yaziyla anlatildigini gormek,
darligi somutlastirdi.

**Ayni ad iki farkli metot olabiliyor, sik sik.**
Klonda `OnOpened` 42 ayri yerde, `OnDrop` 17 ayri yerde abone ediliyor ve
bunlar farkli siniflarin ayni adli metotlari. Abonelik analizini metot adiyla
yapmanin neden calismayacagini bu sayilar gosterdi.

**Iki repo uretim/test ayriminda taban tabana zit.**
Polly'de 991 async metodun 846'si test kodunda; ShareX'te test projesi hic yok,
1138 dosyanin hepsi uretim. Uretim/test ayrimini risk skoruna sokarken bu
zitligi hesaba katmak gerekecek.

## 4. Alinan kararlar

| Karar | Gerekce | Bagli ADR |
|---|---|---|
| Butun kod Ingilizce adlandirildi, yorumlar Turkce kaldi | Iki dili ayni satirda karistirmak okumayi zorlastiriyordu; Roslyn ve .NET API'lari zaten Ingilizce | ADR 0005 |
| `IRule` Analysis'te, `Finding` Core'da | `IRule` Roslyn `SyntaxTree` aliyor; `Finding` saf veri, ileride veritabani ve API tarafinda Roslyn surukletmeden kullanilacak | ADR 0007 |
| `Finding` kendi basina serialize edilebilir; `Title` ve `Rationale` kuraldan kopyalaniyor | Bulguyu basan taraf kural katalogunu tasimasin; eski bir bulgu, kurali degisse bile kendini anlatabilsin | ADR 0007 |
| Cikis kodu uclendi: 0 / 1 / 2 | CI'da "arac bozuldu" ile "bulgu var" farkli seyler; ikisi ayni kodu donerse bos bulgu listesi basarili tarama sanilabiliyordu | ADR 0006 |
| `--fail-on` varsayilani `warning` | `error` olsaydi uyari kurallari hic okunmazdi, `info` olsaydi ilk bilgi kuralinda herkesin build'i kirilirdi | ADR 0006 |
| `--fail-on` degerleri Ingilizce, ekran etiketi Turkce | Bayraga yazilan deger JSON'daki `severity` ile ayni olsun, CI'da iki sozluk ogrenilmesin | ADR 0006 |
| `byRuleCode` dizi, dinamik anahtarli nesne degil | Sabit sema; sema dogrulama ve guclu tipli deserialize Asama 4 ve 6 icin gerekecek | - |
| Event handler istisnasi tip **adina** bakiyor | Semantic model yok; gercek tip hiyerarsisi gorulemiyor | - |
| Kural listesi CLI'da sabit, `RuleRunner` disaridan aliyor | Kurallarin JSON'dan okunmasi Asama 3'un isi | - |

## 5. Bilerek birakilan sinirliliklar

`docs/sinirliliklar.md` 14 maddeye cikti. Asama 2'de eklenen alti madde:

- Event handler istisnasi tip adina bakiyor, gercek tip hiyerarsisine degil.
- SV001 lambda ve anonim delegate'leri gormuyor.
- Dolayli abonelik yanlis negatif uretiyor (satir 13,
  `EditorInputController.cs:129`).
- `EventArgs`'tan turedigi klon kaynagindan dogrulanamiyor.
- (Asama 1'den devam) Semantic model yok, sadece sozdizimi.
- (Asama 1'den devam) Uretim/test ayrimi yola ve dosya adina bakan bir tahmin.

## 6. Puruzler: kapananlar ve kapanmayanlar

| Puruz | Nereden geldi | Durum |
|---|---|---|
| Cikis kodu 1 iki anlama geliyordu | Asama 2, ADR 0006'da puruz olarak yazilmisti | **Kapandi.** 0/1/2 uclemesi, ADR 0006'ya tarihli guncelleme eklendi, eski metin silinmedi |
| `byRuleCode` dinamik anahtarli nesneydi | Asama 2, check komutu raporunda not edilmisti | **Kapandi.** `ruleCode` + `count` alanli dizi, kural koduna gore alfabetik sirali |
| README "sadece banner basiyor" diyordu | Asama 1'den devir | **Kapandi.** Iki komut ve cikis kodu tablosu yazildi |
| Asama 1 olcum raporundaki JSON anahtarlari eskimisti | Asama 1'den devir | **Kapandi.** Rapora tarihli not dusuldu, eski sayilar ve ornek ciktilar silinmedi |
| Semantic model ne zaman gerekecek? | Asama 1'den devir | **Kapanmadi ama netlesti.** SV001 icin gerekmedi; Kademe 3 icin sart. Maliyeti hala olculmedi |
| Test kodu risk skoruna girmeli mi? | Asama 1'den devir | **Kapanmadi.** ShareX'te test projesi olmamasi soruyu zorlastirdi |
| Uretilen kod dosyalarini filtreleme | Asama 1'den devir | **Kapanmadi.** Iki repoda da `.g.cs` / `.Designer.cs` cikmadigi icin aciliyet olusmadi |
| Kor nokta orani skoru etkilemeli mi? | Asama 1'den devir | **Kapanmadi.** Dokunulmadi |
| Dosya bazli tarama commit bazli analize nasil donecek? | Asama 1'den devir | **Kapanmadi.** Asama 4'un isi |
| Tarama suresi buyuk repolarda | Asama 1'den devir | **Kismen olculdu.** 1138 dosyada 2 sn; dosya sayisi %43 artarken sure iki katina cikti, dogrusal degil |

## 7. Asama 3'e tasinan acik isler

- **`--fail-on none`** yok. "Hicbir zaman kirma" diyebilmek icin bir deger
  gerekebilir; su an sadece `info` / `warning` / `error` var.
- **`--exclude` bayragi** yok. Ornek dosyalari ya da uretilen kodu tarama
  disinda birakmanin yolu yok.
- **CI'da repo kokunde `check` calistirilamiyor.** `samples/Patients/AsyncVoid.cs`
  bilerek yazilmis iki `async void` iceriyor, bu yuzden `check .` cikis kodu 1
  donuyor. Su an CI'da sadece build ve test var; check adimi eklenirse ya
  sadece `src/` taranmali ya da `--exclude` gerekecek.
- **Esik alti bulgunun uctan uca testi yok.** Tek kuralim `error` seviyesinde,
  bu yuzden "bulgu var ama esigi gecmiyor, cikis kodu 0" senaryosu sadece
  `CheckCommand` seviyesinde test edilebiliyor. Ikinci kural `warning` ya da
  `info` seviyesinde gelince uctan uca yazilacak.
- **Kademe 1 uygulanip yeni bir orneklemle olculecek.** Bu listedeki 20 satir
  duzeltmenin tasarlandigi veri; ayni veriyle olcum yapmak dogrulama sayilmaz.
- **Kural katalogu JSON'dan okunacak.** Su an liste `CommandRunner` icinde
  sabit.

---

## Tez icin kullanilabilecek bulgular

- **Tek repoda yapilan olcum bir kural hakkinda yaniltici olabiliyor.** Ayni
  kural Polly'de 0, ShareX'te 106 aday buldu. Sifir sonucu kuralin yanlis
  oldugunu degil, orneklemin uygun olmadigini gosteriyordu; bu ancak ikinci
  repo taranarak anlasildi. Statik analiz kurallarinin degerlendirilmesinde
  orneklem secimi, olcumun kendisi kadar belirleyici.
- **Bir kuralin ciktisini kuralin kendisi degil istisnasi belirliyor.**
  ShareX'te 106 adayin 96'si (%90,6) event handler istisnasiyla elendi.
  Kuralin "async void bul" kismi basit ve dogru calisiyor; kullanilabilirligi
  tamamen eleme olcutunun kalitesine bagli.
- **Sozdizimine dayali heuristikler yanlis olcute bakabiliyor.** SV001'in
  istisnasi metot **imzasina** bakiyor, oysa dogru olcut metodun bir olaya
  **abone olup olmadigi**. Iki yanlis pozitif ve bir yanlis negatifin ucu de
  bu tek kok nedenden cikti; olcut degisikligi ucunu birden cozuyor.
- **Tanimlayici adiyla yapilan analiz olceklenmiyor.** Klonda `OnOpened` 42,
  `OnDrop` 17 ayri yerde abone ediliyor ve bunlar farkli siniflarin ayni adli
  metotlari. Ad esitligine dayanan bir kontrol, bir sinifta yapilan aboneligi
  baska bir sinifin metoduna mal ediyor; dogru cozum sembol esitligi, yani
  semantic model.
- **Olcumun uzerinde tasarlanan duzeltme, ayni olcumle dogrulanamaz.** Kademe
  1, precision'i bu 20 satirlik orneklemde %80'den %100'e cikariyor; ancak
  duzeltme tam da o 20 satira bakilarak tasarlandi. Bu sayi bir dogrulama
  degil, tasarim hedefidir; gecerlilik ancak ayri bir orneklemle olculebilir.

## Mulakatta anlatilabilecek hikayeler

- **"Listeye yazdigim satir numarasi yanlisti, kontrol yakaladi."** Elle
  dogrulama listesini yazdiktan sonra 20 satirin hepsini kaynak dosyalarla
  programatik olarak karsilastirdim. 11 numarali satirda
  `ColorPickerWindow.axaml.cs:370` icin `OnCopyHexClick` yazmisim; o satirda
  aslinda `OnCopyRgbClick` var, `OnCopyHexClick` 371'de. Bir satirlik kayma,
  ama listeyi inceleyecek kisiyi yanlis metoda goturecekti. Kendi yazdigim
  belgeyi kaynaga karsi dogrulamasaydim fark etmeyecektim.
- **"Kendi belgemde sayamadigim bir iddia yazdim."** Ayni kontrolde "20 satirin
  20'si ayri dosyadan" diye yazdigim cumlenin yanlis oldugu cikti: 20 satir 18
  ayri dosyadan geliyordu, iki dosya ikiser kez geciyordu. Cesitlilik iddiasi
  onemliydi cunku orneklemin temsil gucunu anlatiyordu; sayiyi dogrulamadan
  yazmisim.
- **"Kendi tahminimi olcup yanlis buldum."** (Asama 1'den devam) Kor nokta
  buyuklugunu elle yazdigim bir betikle 537 satir diye tahmin edip dokumana
  yazmistim; arac olcmeye baslayinca 275 cikti. Betigim `#if !NETFRAMEWORK`
  gibi olumsuz kosullari yanlis degerlendiriyordu. Yanlis sayiyi silmek yerine
  olcumle birlikte raporda birakip neden yanlis oldugunu yazdim; Asama 2'de de
  ayni yaklasimi surdurup Polly'nin sifirini ve eski JSON anahtarlarini
  silmedim.
- **"Sifir sonucu kabul etmeyip uc kez dogruladim."** Polly taramasi hic bulgu
  vermeyince bunu "temiz repo" diye yazmak yerine aracin bozuk olma ihtimalini
  test ettim: aracin kendi `scan` ciktisindan saydim, Roslyn'den bagimsiz duz
  metin aramasi yaptim, ve kuralin ornek dosyada hala atesledigini kontrol
  ettim. Ucu de ayni sonucu verdi. Bir aracin "bir sey bulamadim" demesi ile
  "bakamadim" demesi ayni ciktiyi uretiyor.
- **"Kuralin degerini yanlis yerde ariyordum."** SV001'i yazarken asil isin
  `async void` tespiti oldugunu, event handler istisnasinin kucuk bir detay
  oldugunu dusunuyordum. Olcum tersini gosterdi: 106 adayin 96'si istisnayla
  eleniyor, yani kuralin ciktisini belirleyen sey istisna. Ustelik bulunan uc
  hatanin ucu de istisnadan kaynaklaniyordu, tespit kismindan degil.
