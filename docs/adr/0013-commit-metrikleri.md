# 0013 - Commit metrikleri

**Baglam:** Adim 2 commit'leri PostgreSQL'e yazdi ama `CommitMetrics` tablosu bilerek bos
birakildi. Asama 5'in modeli ham commit verisiyle degil, ondan turetilen olculerle
calisacak: bir commit'in ne kadar buyuk oldugu, degisikligin ne kadar daginik oldugu,
dokundugu dosyalarin gecmisi, yazarin deneyimi.

**Hesaplama git'e gitmiyor.** Kaynak `Commits` ve `CommitFiles` tablolari. Git'i tekrar
yurutmek hem yavas olurdu hem de ayni veriyi iki kez okumak demekti; Adim 1 tarihi bir
kez okudu, burasi onun uzerinde calisiyor. Olculen fark buyuk: ayni 2759 commit git'ten
okunurken 3,95 sn suruyor, veritabanindan okunup hesaplanirken 0,61 sn
(`docs/olcumler/asama4-mine-bellek.md`).

## Olculer ve tanimlari

**Boyut.** `LinesAdded`, `LinesDeleted`, `FilesChanged`, `CsFilesChanged`. Commit'in
dosya satirlarindan toplaniyor. `.cs` ayri sayiliyor cunku risk tahmini C# kodunu
ilgilendiriyor; yuzlerce satir JSON degistirip tek satir kod degistiren bir commit
"buyuk" gorunmemeli.

**Daginiklik.** Degisiklik tek yerde mi toplanmis, repoya mi yayilmis.

- `Entropy`: Shannon entropisi, **-sum(p_i * log2(p_i))**. Burada `p_i`, o dosyanin
  degisen satir sayisinin (eklenen + silinen) commit'teki toplam degisen satira orani.
  Tek dosyalik commit'te 0 cikiyor: p = 1 ve log2(1) = 0. Iki dosyaya esit dagilmis bir
  commit'te 1,0. Hic satir degismeyen commit'te 0.
- `DirectoryCount`: dokunulan farkli dizin sayisi. Dizin = yolun tamami eksi dosya adi.
- `SubsystemCount`: dokunulan farkli alt sistem sayisi. Alt sistem = yolun **ilk** dizin
  bileseni (`src/Core/a.cs` icin `src`).

**Gecmis.** Dokunulan dosyalarin bu commit'ten onceki hâli.

- `MaxFileAgeDays` / `MinFileAgeDays`: dosyanin ilk gorulusunden bu commit'e kadar gecen
  gun. Ilk kez gorulen dosyada 0.
- `PriorChanges`: bu commit'in dokundugu dosyalarin bu commit'ten **once** kac kez
  degistirildigi, toplam.
- `PriorFixes`: ayni sayim, sadece `IsFix` olan commit'ler.
- `DistinctAuthorsOnFiles`: bu dosyalara daha once dokunmus farkli yazar sayisi.

**Gelistirici.**

- `AuthorCommitCount`: yazarin bu commit'ten onceki commit sayisi.
- `AuthorFileExperience`: yazarin bu commit'in dosyalarina daha once kac kez dokundugu,
  dosya basina sayilarin toplami.

Yazar kimligi eposta (ADR 0011). Bot bayrakli commit'ler metrige **giriyor**; haric
tutulacaklarsa bu Asama 5'in karari, veriden simdiden atmak o karari elimizden alirdi.

**Amac.** `IsFix`: mesajin **basligi** bir duzeltme imasi tasiyor mu.

## IsFix heuristigi ve bilinen kusuru

Aranan kelimeler: `fix`, `bug`, `hata`, `patch`, `defect`, `error`, `crash`, `issue`,
`resolve`, `correct`. **Tam kelime olarak** araniyor, alt-dizgi olarak degil: kelime
siniri harf ve rakam disindaki her karakter. Yani `Fix:` eslesiyor, `prefix` eslemiyor.

Bu ayrimi ozellikle yaziyorum cunku ayni hata bu projede bir kez yapildi: bot tespiti
`bot` parcasini alt-dizgi olarak ariyordu ve **Jason Botwick** adli gercek bir kisiyi bot
saydi (ADR 0011). Ad ya da metin icinde parca aramak, kimlik ya da tip yerine gecmez.

Bilinen kusurlari, hicbiri bu adimda duzeltilmiyor:

- **Cekimli hâller kaciyor.** `fixes`, `fixed`, `bugs`, `hatayi`, `hatalari` eslesmiyor,
  cunku tam kelime araniyor. Gercek duzeltmelerin bir kismi bu yuzden `IsFix = false`
  cikiyor. Kac tanesi oldugu **olculmedi**.
- **Duzeltme olmayan seyler eslesiyor.** `fix typo in readme` de `IsFix` oluyor, oysa
  ortada bir hata duzeltmesi yok. Ayni sekilde `add error handling` bir duzeltme degil
  ama `error` gecti diye eslesiyor.
- **Kapali bir hata takip sistemi yok.** Gercek olcut, commit'in bir hata kaydina
  baglanip baglanmadigi olurdu; o veri elimizde yok.

Bu heuristik Asama 4-5'te olculecek: `IsFix` olan commit'lerden bir orneklem alinip
kaynak koda ve mesaja bakilarak dogru/yanlis siniflandirilacak. O olcumden once
duzeltmeye calismak, ne kadar yanildigini bilmeden ayar yapmak olurdu (B033).

Polly'nin tarihinde `IsFix` medyani 0, p95 1; yani commit'lerin yarisindan azi
duzeltme sayiliyor. Tam oran `docs/olcumler/` altindaki dagilim ozetinde.

## Zaman sizintisi: en onemli karar

Her olcu **yalnizca o commit'ten onceki veriyle** hesaplaniyor. Commit'ler tarih
sirasinda (eskiden yeniye) isleniyor ve durum ilerleyerek birikiyor: bir commit'in
olculeri hesaplanirken durumda sadece ondan onceki commit'ler var, hesap bittikten
**sonra** durum o commit'le guncelleniyor.

Bu sira tesadufi degil, tek onemli sey. Gelecekten bilgi sizarsa Asama 5'in modeli
gercekte olmayan bir basari gosterir: egitim sirasinda "bu dosya ileride duzeltilecek"
bilgisini gormus olur ve dogruluk yuksek cikar, ama gercek kullanimda o bilgi yoktur.
Ustelik fark etmek cok zordur, cunku her sey calisiyor gorunur.

Iki test bunu siniyor: bir commit'in metrikleri, sonrasina yeni bir commit eklenince
**degismiyor**; ve sonradan gelen bir duzeltme onceki commit'in `PriorFixes`'ini
artirmiyor.

## Ad degisimi

Hesaplama ad degisimini takip ediyor: bir dosyanin adi degisince gecmisi yeni yola
tasiniyor. Aksi hâlde dosya sifirdan baslamis gibi gorunurdu.

Bu secimin etkisi olculdu ve buyuk cikti: Polly'de ad degisimi iceren commit sayisi 80
(%2,9) ama **metrikleri bu secimden etkilenen commit sayisi 1234 (%44,7)**. Tek bir
yanlis ad eslesmesi, o dosyaya sonradan dokunan butun commit'lerin gecmisini boyuyor.
Ayrintisi `docs/olcumler/asama4-ad-degisimi.md` dosyasinda.

Esik (%50) bu adimda **degistirilmedi**. Degistirmek icin once ad degisimlerinin
dogrulugunu olcmek gerekiyor ve o olcum yapilmadi.

## Nerede duruyor

Hesaplama kodu `Sievert.Data/Metrics` altinda. Ayri bir proje acmadim: olculer ayni
veritabaninin turetilmis sutunlari ve hesap veritabanindan okuyup veritabanina yaziyor.
Yine de saf hesap (`MetricCalculator`, `FixSubject`) EF Core'a hic dokunmuyor, duz
kayitlarla calisiyor; entropy, `IsFix` ve zaman sizintisi testleri bu yuzden veritabani
olmadan kosuyor.

**Yeniden hesaplama normal.** Ayni depo ikinci kez hesaplanirsa eski olculer silinip
yenileri yaziliyor. Ham veride idempotent yazma (ADR 0012) dogru davranisti cunku git
gecmisi degismez; turetilmis veride tersi dogru, cunku bir olcunun tanimi degisince
tablonun yeniden hesaplanabilmesi gerekiyor. Hesap deterministik oldugu icin ayni
veriden ayni sonuc cikiyor; bir test bunu siniyor.

**Sonuc:** `sievert metrics <repo-adi>` veritabanindaki ham veriden 15 olcuyu hesaplayip
`CommitMetrics` tablosuna yaziyor. `--out <dosya>` ile her olcunun min/medyan/p95/max
dagilimi JSON olarak yaziliyor; Asama 5'te esik secilecekse (B033: esikler olcumden
once ilan edilir) bakilacak yer orasi.
