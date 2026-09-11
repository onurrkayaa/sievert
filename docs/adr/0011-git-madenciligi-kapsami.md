# 0011 - Git madenciliginin kapsami ve mine komutu

**Baglam:** Asama 4'un hedefi commit bazli risk tahmini. Once tarihin okunmasi lazim.
Bu adimda sadece git okunuyor; veritabani ve EF Core yok. Iki yeni teknolojiyi ayni
anda devreye almak, bir sey patladiginda hangisinin suclu oldugunu bilinemez yapardi.

## Neden ayri proje

Git okuma `src/Sievert.Mining` icinde, `Sievert.Analysis`'e bagimli degil. Iki isin
ortak noktasi yok: biri kaynak kodu ayristiriyor, digeri commit tarihini yuruyor.
Bagimlilik kursaydim Roslyn ile LibGit2Sharp ayni projede olurdu ve ADR 0004'teki
"kutuphane tipleri katman sinirini gecmez" karari iki kat zorlasirdi.

Commit modelleri (`CommitRecord`, `FileChange`, `MiningSummary`) `Sievert.Core/Mining`
icinde duruyor, yani hicbir LibGit2Sharp tipi disari cikmiyor. Bu, ADR 0007'de
`Finding`'in Core'da durmasiyla ayni kalip: veriyi tuketen taraf (ileride veritabani,
Web API, rapor) git kutuphanesini suruklemek zorunda kalmiyor.

**ADR 0001'e duzeltme:** Orada Analysis katmani "Roslyn ve git analizi" diye
tanimlanmisti. Git analizi artik ayri bir projede; o cumle bu ADR ile guncelleniyor.

## Neden birlestirme commit'leri disarida

Birlestirme commit'inin kendi diff'i yok, ebeveynine gore hesaplanan bir diff'i var ve
hangi ebeveynin secildigine gore tamamen degisiyor. Ilk ebeveyne gore bakarsan yan
daldaki butun degisiklikler tek bir dev commit gibi gorunur ve o satirlar ikinci kez
sayilir. Ikinci ebeveyne gore bakarsan ana daldaki degisiklikler ayni sekilde tekrarlanir.

Risk tahmini "bu commit ne kadar kod degistirdi" sorusunu soruyor; birlestirme commit'i
bu soruya durustce cevap veremiyor. O yuzden veriden cikariliyor. Ama **sayiliyor** ve
ozette raporlaniyor, cunku "kac commit okundu" ile "repoda kac commit var" arasindaki
farkin nereden geldigi gorulebilmeli. Polly'de 2953 commit'in 194'u birlestirme.

## Neden kimlik eposta ile

Yazar adi kararli degil: ayni kisi "Onur Kaya", "onur", "Onur KAYA" diye uc farkli ad
kullanabiliyor, ve iki farkli kisinin ayni adi tasimasi da mumkun. Eposta en azindan
git yapilandirmasinda tek bir alan ve genelde sabit kaliyor. Kucuk harfe cevrilip
karsilastiriliyor.

Bu mukemmel degil: ayni kisinin is ve kisisel epostasi ayri iki kisi sayilir, ve
GitHub'in `users.noreply.github.com` adresleri ayni kisiyi ayri gosterebilir. Bilinen
bir sinirlilik, `docs/sinirliliklar.md`'ye yazildi. Yazar adi yine de veride duruyor,
sadece kimlik olarak kullanilmiyor.

## Neden yazar tarihi, committer tarihi degil

Rebase, cherry-pick ve amend committer tarihini degistiriyor ama yazar tarihini
korumuyor - tam tersi, yazar tarihi korunuyor. Yani "bu kod ne zaman yazildi" sorusunun
cevabi yazar tarihinde. Bir dalin ana dala rebase edilmesi, alti ay once yazilmis bir
commit'in committer tarihini bugune cekiyor; committer tarihiyle calissaydim o commit
bugun yazilmis gibi gorunurdu.

Hepsi UTC'ye cevriliyor. Saat dilimi korunsaydi ayni veri kumesindeki iki commit
karsilastirilamazdi. `--since` de ayni sebeple, saat dilimi yazilmamis bir tarihi UTC
sayiyor: ayni komut iki makinede ayni commit kumesini okumali.

## Ad degisimi esigi

Ad degisimi takibi acik ve benzerlik esigi **%50**. Bu LibGit2Sharp'in (ve git'in)
varsayilani; acikca yazdim ki raporda hangi sayinin kullanildigi belli olsun. Yani bir
dosyanin icerigi eski hâline en az %50 benziyorsa, "sil + ekle" degil "ad degisti"
sayiliyor ve eski yol kayitta duruyor.

Esigi degistirmedim cunku degistirecek bir olcum yok. Polly'nin tarihinde 1029 ad
degisimi bulundu; bunlarin ne kadarinin dogru oldugu elle kontrol edilmedi. Esik
raporda duruyor, ileride bir olcume dayanarak degistirilebilir.

## Neden JSONL

Tam veri `--json <dosya>` ile satir satir JSON yaziliyor: her satir bir commit, dosya
tek bir dev dizi degil. Iki sebep var.

Birincisi yazan taraf: tek dizi olsaydi butun commit'leri bellekte toplayip sonunda
serialize etmek gerekirdi. Asama 3'te tarama tarafinda tam olarak bu yapilmisti ve tepe
bellek %72 buyumustu (99 MB'den 170 MB'ye). Burada commit'ler `yield return` ile tek tek
geliyor, yazilip birakiliyor.

Ikincisi okuyan taraf: JSONL'i satir satir okumak mumkun, tek dizi olsaydi okuyanin da
tamamini belleğe almasi gerekirdi.

**Ozet bu dosyaya yazilmiyor.** Dosyada tek bir kayit turu var; ozet ekrana basiliyor.
Son satira farkli sekilli bir ozet nesnesi koysaydim, dosyayi okuyan her program once
"bu satir commit mi ozet mi" diye bakmak zorunda kalirdi.

## Bot yazarlar: karar uygulandi, itiraz burada

Karar suydu: adinda ya da epostasinda `bot`, `[bot]`, `dependabot` ya da `github-actions`
gecen yazarlar bayraklansin, veriden cikarilmasin. Cikarmama kismina katiliyorum - bot
commit'leri gercekten var olan commit'ler, onlari sonraki asamada modelin kendisi
degerlendirsin.

**Katilmadigim kisim `bot` parcasinin duz "iceriyor mu" diye aranmasi.** Bu, icinde bot
gecen her adi yakaliyor. Polly'nin tarihinde tam da bu oldu: 855 commit bot bayragi aldi
ve bunlardan biri **Jason Botwick** adli gercek bir kisinin commit'i. Digerleri dogru
(dependabot 776, polly-updater-bot 70, github-actions 8), yani bayragin kendisi calisiyor;
sorun sadece `bot` parcasinin kelime siniri aranmadan kullanilmasi. Abbott, Botond,
Talbot gibi adlar da ayni sekilde yakalanir.

Karari sessizce degistirmedim, yazildigi gibi uyguladim.

### Guncelleme: tanim degisti, eski sayi duruyor

Itiraz kabul edildi ve tanim degistirildi. Ikisi de burada dursun:

| | Tanim | Polly'de bayrakli commit |
|---|---|---|
| **Eski** | Ad ya da epostada `bot`, `[bot]`, `dependabot` ya da `github-actions` parcasi **gecerse** (duz alt-dizgi aramasi) | **855** |
| **Yeni** | Ad ya da epostada `bot` **ayri bir kelime olarak** gecerse. Kelime siniri harf ve rakam disindaki her karakter | **854** |

Yeni tanim `[bot]`, `-bot` ve `bot@` biciminin ucunu de tek kuralla yakaliyor: koseli
parantez, tire ve `@` zaten kelime siniri sayiliyor, yani `dependabot[bot]` icinde `bot`
ayri bir kelime olarak duruyor. `dependabot` ve `github-actions` parcalarini listeden
cikardim, cunku ikisi de `[bot]` eki tasiyor ve o ek zaten yakalaniyor.

Iki tanim arasindaki tek fark Polly'nin tarihinde **Jason Botwick**'in tek commit'i; baska
hicbir commit'in bayragi degismedi, yeni bayraklanan da olmadi. 855 rakami yanlis oldugu
icin silinmiyor, neyin neden degistigi izlenebilsin diye yukarida duruyor.

## Bellek: olculdu, tam duz cikmadi

Hedef, Asama 3'teki bellek buyumesini bastan onlemekti. Commit'ler bellekte biriktirilmiyor;
tek buyuyen sey farkli eposta adreslerinin kumesi (Polly'de 140 kayit). Buna ragmen tepe
bellek commit sayisiyla birlikte buyuyor. Sayilar `docs/olcumler/asama4-mine-bellek.md`
dosyasinda.

Buyumenin nedenini aradim. libgit2'nin nesne onbellegini kapatmak (`SetEnableCaching(false)`)
bir sey degistirmedi, sadece %50 yavaslatti; o degisikligi geri aldim. Yonetilen yigina sert
sinir koyunca ise is degisti: 64 MB'lik bir yigin siniriyla 2759 commit'in tamami sorunsuz
okundu ve cikti birebir ayni cikti. 32 MB'de bellek yetmedi.

Yani buyuyen sey tutulan veri degil, henuz toplanmamis cop: cop toplayici bol bellek varken
toplamiyor, sinir koyunca topluyor. Bu bir sizinti degil ama "tepe bellek sabit" de diyemem.
Projeye bir GC ayari koymadim; olcum ortada dursun, gerekirse sonraki asamada karar veririm.

## mine ciktisini --out aliyor, --json degil

Bu bayrak once `--json <dosya>` diye yazilmisti ve `scan`/`check` komutlarindaki `--json`
bayragiyla ayni adi tasiyordu, ama ayni sey demiyordu: orada bir bayrak ve cikti ekrana
gidiyor, burada bir dosya yolu bekleniyordu. Ayni adin iki anlami olmasi, uc komutu birlikte
cagiran bir betikte sessiz hataya cikar - `mine ... --json` yazan biri ciktinin ekrana
gelmesini bekler, oysa bir sonraki argumani dosya adi sanilir.

Bayrak `--out <dosya>` olarak degistirildi. `--json` **kabul edilmiyor**, uyumluluk icin
sessizce kabul de edilmiyor: eski yazimi kullanan bir betik "bilinmeyen secenek" hatasi ve
cikis kodu 2 aliyor. Sessiz kabul en kotusu olurdu, cunku komut basariyla biter ve dosya
hic yazilmaz.

Tam verinin neden ekrana degil dosyaya gittigi ayri bir konu: Polly'nin tarihi 2759 satir
ve 3,9 MB, ekrana basilacak bir sey degil.

**Sonuc:** `sievert mine <repo-yolu>` calisiyor. Ekranda ozet, `--json <dosya>` ile tam
veri. `--since` ve `--max-commits` tarihin ne kadarinin okunacagini sinirliyor. Veritabani
bir sonraki adimda.
