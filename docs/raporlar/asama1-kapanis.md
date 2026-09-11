# Asama 1 kapanis raporu

**Tarih:** 2026-09-11
**Kapsam:** `1a7f116` ... `ae049e2`

Asama 1'in amaci bir C# dosyasini Roslyn ile okuyup icindekileri sayabilmekti.
Risk skoru, git gecmisi ve kural motoru bu asamanin disindaydi, hicbiri
yazilmadi.

## 1. Ne yapildi

- Roslyn ile tek dosya cozumleme: `CSharpSyntaxWalker` turevi bir gezgin
  tipleri ve metotlari topluyor. Yerel fonksiyon, constructor ve property
  metot sayilmiyor; partial siniflarin ayni dosyadaki parcalari birlesiyor.
- `sievert scan <yol>` komutu: dosya ya da klasor tariyor, `bin/obj/.git/
  node_modules` atliyor, ASCII agac basiyor. `--json` ve `--top N` var,
  arguman ayristirma elle yazildi.
- Kapali `#if` dallarinda kalan satirlarin sayilmasi. Kayip kapatilmiyor,
  olculuyor.
- Metot uzunlugu dagilimi (ortalama, medyan, p90, p95, maksimum) ve
  uretim/test kodu ayrimi ozete eklendi.
- Polly reposu iki kez tarandi, sonuclar `docs/olcumler/asama1-ilk-tarama.md`
  dosyasinda. 91 test yaziliyor ve geciyor.

## 2. Olculen sayilar

Hepsi tek bir repoda, App-vNext/Polly'nin `2247db24` commit'inde olculdu.
Baska repolarda farkli cikar; buradaki sayilar Polly hakkinda, C# hakkinda
degil.

| Olcum | `6211c80` | `ae049e2` |
|---|---|---|
| Dosya | 797 | 797 |
| Tip | 880 | 897 |
| Metot | 4655 | 4655 |
| Async metot orani | %21,3 | %21,3 |
| Ortalama / medyan metot uzunlugu | 15,8 / 12 | 15,8 / 12 |
| p90 / p95 / en uzun | 32 / 39 / 219 | 32 / 39 / 219 |
| 40 satiri asan metot | 210 (%4,5) | 210 (%4,5) |
| Hic tip bulunamayan dosya | 24 | 12 |
| Ayristirilamayan dosya | 1 | 1 |
| Kor nokta | olculmuyordu | 61 dosya, 275 satir, %0,26 |
| Uretim / test dosya | ayrilmiyordu | 479 / 318 |
| Uretim / test metot | ayrilmiyordu | 1678 / 2977 |
| Tarama suresi | 0,83 - 0,89 sn | 1,32 - 1,40 sn |

Uretim tarafinda medyan 7,0 ve async orani %8,6; test tarafinda medyan 15,0
ve async orani %28,4.

## 3. Beklemedigim bulgular

**Kor nokta tahminim gercegin iki katiymis.**
Ilk taramadan sonra `#if` kaybini elle yazdigim bir betikle yaklasik 537
satir diye yazmistim. Arac olcmeye baslayinca sayi 275 cikti. Betigim her
`#if` dalini kapali sayiyordu, oysa `#if !NETFRAMEWORK` gibi olumsuz kosullar
hicbir sembol tanimli degilken dogru oluyor ve o dal acik kaliyor. Onemli,
cunku yanlis sayiyi bir dokumana yazmistim ve olcum olmasa duzelmeyecekti.
Elle tahmin, "yaklasik" etiketi tasisa bile, olcumun yerine gecmiyor.

**Performans olcumumu bir kez yanlis yaptim.**
Yeni surumun yavasladigini gorunce eski surumu ayri bir worktree'de derleyip
0,12 saniye olctum ve arada 7 kat fark var sandim. Kontrol edince o surenin
taramaya degil, dotnet'in DLL'i bulamayip hata basmasina ait oldugu cikti.
Dogru yolla olcunce 0,83'e karsi 1,36 saniye. Onemli, cunku ilk olcum
"inandirici" bir sayiydi ve dogrulamasam rapora oyle girecekti.

**Tarama %60 yavasladi.**
Kor nokta olcumu icin her dosyanin butun trivia'sini gezmek gerekiyor. 797
dosyada 0,5 saniyelik bir fark kabul edilebilir duruyor ama bedava degil.

**"Hic tip bulunamadi" cikan dosyalarin yarisi bos degildi.**
24 dosyanin 12'si enum ya da delegate tanimliyordu; gezgin sadece class,
record, struct ve interface ziyaret ediyordu. Bu olcum yapilmadan once
farketmemistim.

**En uzun metotlar test kodundan geliyor.**
Polly'de en uzun 10 metodun 9'u test. 219 satirlik olani once olcum hatasi
sandim, dosyaya bakip dogruladim: tek bir metot govdesi. Uretim ve test
ayrilmadan hesaplanan bir risk skoru, buyuk olcude test dosyalarinin seklini
olcecekti.

**`#if` iceren her dosyada kayip yok.**
61 dosyanin 11'inde kosul olumsuz yazildigi icin acik dal seciliyor ve hicbir
sey kaybolmuyor. Bu yuzden "kosullu derleme var mi" ile "kac satir gorulmedi"
iki ayri alan oldu.

## 4. Alinan kararlar

| Karar | Gerekce | Bagli ADR |
|---|---|---|
| Roslyn paketi sadece Analysis projesinde, Core saf C# | Sonuc modelleri ileride raporlama ve veritabani tarafinda Roslyn surukletmeden kullanilabilsin | ADR 0004 |
| Sonuc modelleri `Sievert` onekiyle adlandirildi | Roslyn'in kendi tipleriyle ad cakismasini onlemek | ADR 0004 |
| Ic ice tipler duz listede nitelikli adla tutuluyor, agac kurulmuyor | Veri duz kalsin, agac sadece ekran ciktisinda olussun | - |
| Sozdizimi hatasinda exception yok; hatalar listeleniyor, cozulen kisim donuyor | Tek bozuk dosya butun taramayi durdurmasin | - |
| Kor nokta olculuyor ama duzeltilmiyor | Sembol tahmin etmek, eksik sonucu dogru gibi gostermek olurdu | - |
| Dosya yollari her ciktida tarama kokune gore goreli, mutlak yol JSON'da tek alanda | Ayni tarama iki formatta ayni yolu gostersin | - |
| Arguman ayristirma elle yazildi | Tek komut ve iki bayrak icin harici paket eklemeye deger gorunmedi | - |

## 5. Bilerek birakilan sinirliliklar

`docs/sinirliliklar.md` dosyasinda sekiz madde var, ozeti:

- Dosyalar arasi partial siniflar birlestirilmiyor (tek dosya okunuyor).
- Top-level statement iceren dosyalar "hic tip bulunamadi" olarak geciyor.
- Semantic model yok, sadece sozdizimi; tip cozumleme yapilamiyor.
- Uretilen kod dosyalari (`.g.cs`, `.Designer.cs`) ayirt edilmiyor.
- 40 satir esigi keyfi bir sayi.
- Kapali `#if` bloklarindaki kod sonuca girmiyor (artik olculuyor).
- .NET 10 dosya tabanli programlar (`#:package`) ayristirilamiyor.
- Uretim/test ayrimi yola ve dosya adina bakan bir tahmin, kesin degil.

## 6. Asama 2'ye tasinan acik sorular

- Semantic model ne zaman gerekecek? Syntax'in yetmedigi ilk kuralda mi, yoksa
  daha once mi? Maliyeti (derleme acma suresi) olculmedi.
- Test kodu risk skoruna girmeli mi, girecekse ayni agirlikla mi?
- Uretilen kod dosyalarini filtrelemek icin dosya adi kalibi yeterli mi, yoksa
  `<auto-generated>` basligini da okumak gerekiyor mu?
- Kor nokta orani risk skorunu etkilemeli mi? Cok `#if` iceren bir dosya
  hakkinda daha az sey biliyoruz; bu belirsizlik skora yansimali mi?
- Dosya bazli tarama commit bazli analize nasil donecek? Ayni commit'te
  degisen dosyalar birlikte mi degerlendirilecek?
- Tarama suresi daha buyuk repolarda ne oluyor? Tek olcum 797 dosyalik bir
  repoda yapildi.

---

## Tez icin kullanilabilecek bulgular

- Metot uzunlugu dagilimi saga carpik: Polly'de ortalama 15,8, medyan 12.
  Ortalamayi tek basina kullanan bir metrik, az sayidaki cok uzun metottan
  etkileniyor. Medyan ve yuzdelikler birlikte raporlanmali.
- Uretim ve test kodu ayni repoda farkli davraniyor: Polly'de uretim medyani
  7,0 ve async orani %8,6 iken test medyani 15,0 ve async orani %28,4.
  Ayrim yapilmadan hesaplanan bir skor, buyuk olcude test kodunu olcuyor.
- Sabit esik secimi tasiyici degil: 40 satir esigi bu repoda toplamda p95'e
  denk geliyor ama uretimde p95 34, testte 41. Ayni sayi ayni repo icinde bile
  iki farkli yuzdelige dusuyor.
- Statik cozumleme sessiz kayip uretebiliyor: preprocessor sembolu
  tanimlanmadan ayristirmada kapali `#if` dallari hicbir uyari vermeden
  sonuc disinda kaliyor. Polly'de bu oran %0,26 cikti; olcum yapilmadan
  bilinmiyordu ve orani repodan repoya degisir.
- Olculmeyen buyuklukler hakkinda yapilan tahminler sistematik olarak
  sapabiliyor: ayni buyuklugun elle tahmini 537, olcumu 275 cikti.

## Mulakatta anlatilabilecek hikayeler

- **"Kendi tahminimi olcup yanlis buldum."** Kor nokta buyuklugunu once elle
  yazdigim bir betikle tahmin ettim, dokumana yazdim. Sonra araca olcum
  ekleyince sayinin iki kati oldugu cikti; sebebi betigin olumsuz `#if`
  kosullarini yanlis degerlendirmesiydi. Yanlis sayiyi silmek yerine olcumle
  birlikte raporda birakip neden yanlis oldugunu yazdim.
- **"Inandirici bir performans sayisi urettim, dogrulayinca yanlis cikti."**
  Eski surumu 0,12 saniye olcup 7 kat hizli sandim; o sure aslinda dotnet'in
  hata verme suresiymis. Dogru olcum 0,83'e karsi 1,36 saniye. Ilk sayi makul
  gorundugu icin kontrol etmeseydim rapora girecekti.
- **"Ozelligi eklemek yerine kaybi gorunur yaptim."** `#if` bloklarindaki kod
  gorunmuyordu. Sembol tahmin edip ikinci kez ayristirmak mumkundu ama bu,
  eksik sonucu dogru gibi gosterirdi. Bunun yerine ne kadarini gormedigimi
  sayip ciktiya yazdim.
- **"219 satirlik metot olcum hatasi sandim, degilmis."** Sonuca guvenmeyip
  dosyaya baktim; gercekten tek bir test metodu 54. satirdan 272. satira
  kadar gidiyordu. Araci degil, olculen kodu sorgulamak gerektigini gosteren
  bir ornek.
- **"Bir sayiyi ikiye bolunce anlami degisti."** %21,3 async orani ilk bakista
  Polly'nin async agirlikli oldugunu soyluyordu. Uretim ve testi ayirinca
  uretim tarafinin %8,6 oldugu, async yogunlugunun testlerden geldigi cikti.
