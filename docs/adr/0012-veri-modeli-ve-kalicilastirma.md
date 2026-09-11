# 0012 - Veri modeli ve commit verisinin kalicilastirilmasi

**Baglam:** Adim 1 git tarihini okuyup commit basina veri cikariyordu ama hicbir yere
yazmiyordu; her calistirmada her sey bastan okunuyordu. Adim 5'te "bu dosya son 90 gunde
kac kez degisti" gibi sorular sorulacak ve bunlari her seferinde git'i bastan yurutup
cevaplamak anlamsiz.

## Neden ayri proje ve hangi yone bakiyor

Veritabani kodu `src/Sievert.Data` icinde. `Sievert.Mining` buna bagimli degil, tersi de
degil: **ikisi de sadece `Sievert.Core`'a bakiyor.** Yazma isini `Sievert.Cli` koordine
ediyor - Mining okuyor, Data yaziyor, ikisini birbirine baglayan tek yer komut katmani.

Bagimlilik kursaydim iki yonde de zarari olurdu. Mining -> Data olsaydi git okumak icin
EF Core gerekirdi ve `mine --out` gibi veritabanina hic dokunmayan bir kullanim bile
Npgsql suruklerdi. Data -> Mining olsaydi veritabanina yazmak icin LibGit2Sharp gerekirdi;
oysa ileride veri baska bir kaynaktan da gelebilir. ADR 0004 ve 0011'deki cizgi ayni:
kutuphaneler katman sinirini gecmiyor, sinirdan gecen sey Core'daki saf veri.

## Neden dort tablo

| Tablo | Ne tutuyor |
|---|---|
| `Repositories` | Taranan depo ve son tarama kosusu (ne zaman, hangi HEAD) |
| `Commits` | Commit'in kendisi: yazar, tarih, mesaj, toplamlar |
| `CommitFiles` | Commit'in her dosyaya yaptigi degisiklik |
| `CommitMetrics` | Turetilmis olculer - bu adimda **bos** |

Dosyalari `Commits` icinde bir JSON sutununda tutmak da mumkundu. Tutmadim, cunku Adim
5'in ana sorusu dosya bazinda: "bu yol gecmiste kac kez degisti". JSON icinde arama
yapmak indeks kullanamamak demek.

**`CommitMetrics` neden simdiden var ve neden bos:** ham veri ile turetilmis veri ayri
dursun istedim. `Commits` icindeki her sey git'ten okundu ve git degismedigi surece
degismez. Turetilmis olculer ise tanimi degisebilecek seyler - "son 90 gun" penceresi
degisirse ya da bir olcunun formulu duzelirse o tablonun silinip yeniden hesaplanmasi
gerekir. Ayni tabloda olsalardi her formul degisikliginde ham veriyi de riske atardim.
Tablo bos ama semada: Adim 3'un nereye yazacagi bugunden belli olsun diye.

## Co-Authored-By nereye gitti

Adim 1 mesajdaki `Co-Authored-By` satirlarini ad ve eposta olarak ayristiriyor. Semada
bunlarin yeri yok; **`Commits.CoAuthorCount` diye bir sayi** tutuyorum, adlar ve
epostalar veritabanina girmiyor.

Ayri bir `CommitCoAuthors` tablosu da yazabilirdim ve kisi bazli sorular icin daha
dogru olurdu. Iki sebeple yazmadim. Birincisi, su an planlanan hicbir ozellik ortak
yazarlarin kim oldugunu sormuyor; simdiden tablo acmak, kullanilmayan bir semayi
Adim 3 ve 5 boyunca tasimak demek. Ikincisi, bu veri **kaybolmuyor**: git deposunda
duruyor ve `mine --out` ciktisinda tam hâliyle yaziliyor. Veritabani burada git'in
yerine gecen bir kaynak degil, uzerine sorgu atilan bir onbellek; ihtiyac dogarsa
tablo eklenip veri yeniden okunur.

Sayiyi tutmamin sebebi ise su: "bu commit'i kac kisi yazdi" sorusu Adim 5'te ise
yarayabilir ve bunun icin adlara gerek yok.

## Indeksler

Uc indeks var ve ucu de Adim 5'in soracagi sorulara gore secildi.

**`(RepositoryId, Sha)` benzersiz.** Ayni depoda ayni commit iki kez olamaz. Bu hem
idempotent yazmanin dayanagi - "zaten var mi" sorusunun cevabi veritabaninin kendi
garantisi, kodun dikkatli olmasi degil - hem de "su sha'nin verisi" sorgusunun yolu.

**`Commits.AuthorDateUtc`.** Risk tahmininin butun ozellikleri zaman pencereli:
"son 30 gunde", "son 90 gunde". Bu sorgularin hepsi tarih araligindan geciyor.

**`CommitFiles.Path`.** Adim 5'in en sik sorgusu "bu dosya gecmiste kac kez degisti".
Indekssiz her soru butun dosya tablosunu tarardi; Polly'nin tarihinde bu tablo 17 428
satir ve tek bir repo icin.

Dorduncu bir aday `Commits.AuthorEmail` idi, koymadim: kisi bazli sorgular su an
planda yok ve ADR 0011'de yazildigi gibi eposta kimligi zaten guvenilir degil.
Gerekirse sonra eklenir; indeks eklemek ucuz, yanlis indeksle yasamak degil.

## Idempotent yazma

Ayni depo ikinci kez islenirse **varsayilan davranis atlamak**. Once o deponun kayitli
sha'lari okunuyor, akistan gelen commit zaten kayitliysa atlaniyor ve sayilip ozete
yaziliyor. Polly'de olculdu: ikinci kosuda 0 yazildi, 2759 atlandi.

Varsayilanin "sil ve bastan yaz" olmamasinin sebebi su: normal kullanim, bir repoyu
gunler sonra tekrar tarayip yeni commit'leri eklemek. Her seferinde silip yazmak, hem
gereksiz is hem de Adim 3'un `CommitMetrics` tablosunu her tarama sonrasi silerdi
(basamakli silme). `--overwrite` bayragi bilerek acik bir tercih olarak duruyor.

Bu bayrak once `--yeniden-yaz` diye yazilmisti. ADR 0005 kod tanimlayicilarinin ve CLI
yuzeyinin Ingilizce, ciktinin Turkce olmasini soyluyor; `--out`, `--since`,
`--max-commits` Ingilizceyken tek bir bayragin Turkce olmasi benim hatamdi. Geriye
uyumluluk birakmadim: eski yazim "bilinmeyen secenek" hatasi veriyor. Sessizce kabul
etseydim, eski adi yazan bir betik hicbir sey silmeden basariyla bitmis gorunurdu.

**Islem butunlugu:** yazmanin tamami tek bir transaction icinde. Yarida kesilirse depo
satiri dahil hicbir sey kalmiyor. Yarim yazilmis bir depo idempotent yazmayi da kalici
olarak yaniltirdi: eksik commit'ler "zaten var" sanilmazdi ama tarih araligi ve toplam
sayi yanlis kalirdi.

**Toplu ekleme:** 500 commit birikince `SaveChanges`, sonra degisiklik izleyici
temizleniyor. Commit basina tek tek INSERT her commit icin ayri gidis donus demekti;
hepsini biriktirmek ise Adim 1'de kacinilan "belleğe topla" hatasinin aynisi olurdu.
Izleyiciyi temizlemek sart: temizlenmezse yazilan her commit bellekte kalir ve akis
ozelligi hicbir ise yaramaz.

## Baglanti dizesi koda yazilmiyor

Once `SIEVERT_DB` ortam degiskeni, sonra calisilan klasordeki `appsettings.json`
(`ConnectionStrings:Sievert`). Ikisi de yoksa arac ne yapilmasi gerektigini yazip
cikis kodu 2 ile duruyor. Repo public; sifre iceren bir dizeyi koda yazmak dogrudan
sizdirmak olurdu.

**Sema kendiliginden guncellenmiyor.** Bekleyen migration varsa arac calistirilacak
komutu yazip duruyor. Baskasinin veritabaninda sessizce sema degistirmek bir tarama
aracinin isi degil.

**Sonuc:** `sievert mine <repo> --db` git tarihini okuyup PostgreSQL'e yaziyor, ayni
akistan hem dosyaya hem veritabanina. Turetilmis olculer bir sonraki adimda.
