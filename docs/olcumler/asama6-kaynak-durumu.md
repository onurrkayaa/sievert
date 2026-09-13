# Statik taramanin kaynagi ve panel oncesi beklentiler

**Tarih:** 2026-09-13
**Beklentiler:** `docs/olcumler/asama6-panel-oncesi-beklenti.md`, kod yazilmadan once yazildi.
**Ureten kod:** `a5e51b9`.

Adim 3'te statik tarama deponun o anki calisma agacini tariyordu ama hangi surumu
taradigini hicbir yere yazmiyordu. "203 bulgu" cumlesi, hangi agacta 203 bulgu oldugu
yazilmadan bir sey anlatmiyor. Bu dosya o eksigin kapandigini olcuyor ve turun butun
beklentilerini bir tabloda topluyor.

## 1. Kosulun sabitlenmesi

- Gercek API sureci, gercek PostgreSQL, uc gercek depo (polly, sharex, jellyfin).
- Derleme **Debug** (panel olcumu haric; o Release).
- Bulgu kumelerinin karsilastirmasi veritabaninda yapildi: her isin bulgulari
  `(kural, seviye, goreli yol, satir)` dortlusune indirgenip yola/satira gore siralandi
  ve tek bir ozete cevrildi. Iki kosu ancak butun dortluleri ayniysa ayni ozeti veriyor.

## 2. Ayni HEAD, ayni sonuc

Her depo icin, ayni HEAD uzerinde ve temiz calisma agaciyla kosan butun basarili
taramalar:

| Depo | HEAD | Calisma agaci | Basarili kosu | Farkli bulgu imzasi | Bulgu |
|---|---|---|---|---|---|
| polly | `2247db240771` | clean | 7 | **1** | 203 |
| sharex | `b5a397ea6ccf` | clean | 7 | **1** | 248 |
| jellyfin | `1d7b6d97844c` | clean | 7 | **1** | 687 |

Yedi kosunun hepsi **ayni bulgu kumesini** uretti; sayilar degil, kumelerin kendisi ayni.
Beklenti 2 buydu.

Uc deponun hicbirinde `sourceCommitChangedDuringAnalysis` true olmadi ve
`sourceVerified` hepsinde true: tarama basladigi HEAD'de bitti (beklenti 1).

## 3. Kirli calisma agaci

Polly'nin calisma agacinda tek bir dosya degistirildi (`README.md`), tarama istendi ve
degisiklik geri alindi.

| Olcu | Deger |
|---|---|
| Isin durumu | `failed` |
| Hata kodu | `REPOSITORY_WORKTREE_DIRTY` |
| Baslangictan terminale | 386 ms |
| Yazilan bulgu | **0** |
| `isResultComplete` | false |
| `sourceTreeState` | `dirty` |
| `sourceHeadSha` | kaydedildi (`2247db240771`) |
| `sourceVerified` | false (tarama hic baslamadi) |
| Cevapta gecen dosya adi | **yok**; yalniz "kaydedilmemis 1 degisiklik var" |
| Cevapta gecen dosya yolu | **yok** |

Beklenti 3 buydu. Bulgular ucunda da sayfa bos donuyor ve `partial: true` isaretli:
"0 bulgu" ile "temiz kod" ayni sey degil.

## 4. Tarama sirasinda degisim

Gercek uc repoda bu durum **olusmadi** - yedi kosunun hicbirinde depo degismedi. O yuzden
davranis testle sinandi: 200 dosyalik bir depo taranirken, isin kaynak durumu
veritabanina yazildiktan ve faz `scanning` olduktan sonra yeni bir commit atiliyor.

| Olcu | Sonuc |
|---|---|
| Isin durumu | `failed` |
| Hata kodu | `REPOSITORY_CHANGED_DURING_ANALYSIS` |
| `sourceTreeState` | `changed-during-analysis` |
| `sourceCommitChangedDuringAnalysis` | true |
| `isResultComplete` | false |
| Hesaplanmis bulgular | yaziliyor, ama sonuc kismi |

Test: `tests/Sievert.Tests/AnalysisSourceStateTests.cs`. Bes kez ust uste kosuldu, besinde
de ayni sonuc. Beklenti 4 buydu.

## 5. Iki worker ornegi

Ayni veritabanina bagli iki worker ornegi, ayri kuyruk ve ayri jeton defteri ile.
Gercek iki HTTP sureci acilmadi; olculmek istenen sey HTTP degil, iki worker'in
**veritabani uzerinden** nasil anlastigi.

| Deney | Sonuc |
|---|---|
| Ayni is iki kuyruga girdi | yalniz biri `running` gecisini kazandi |
| Isleyici kac kez kostu | **1** (sonuc satiri 5, 10 degil) |
| Oteki ornekten gelen iptal | bir sonraki obekte goruldu, is `canceled` |
| Iptal eden ornegin jetonu | isi kosan ornegi etkilemiyor; haber veritabanindan geldi |
| Iki kurtarma yan yana | ikisi de 0 satir degistirdi, is yine bir kez kostu |

Test: `tests/Sievert.Tests/MultipleWorkerTests.cs`. Bes kez ust uste kosuldu. Beklenti
5 ve 6 buydu.

Bu **dagitik kuyruk kuruldu demek degil**: kanal hala surec ici, ortak olan tek sey
veritabani.

## 6. Beklentilerin karsiligi

`docs/olcumler/asama6-panel-oncesi-beklenti.md` icindeki on bes beklenti:

| # | Beklenti | Sonuc | Durum |
|---|---|---|---|
| 1 | Tarama basi ve sonu ayni HEAD | 21 kosuda da `sourceVerified` true, degisim 0 | tuttu |
| 2 | Ayni HEAD ayni sonucu vermeli | her repoda 7 kosu, 1 bulgu imzasi | tuttu |
| 3 | Kirli agacta tarama baslamamali | `REPOSITORY_WORKTREE_DIRTY`, 0 bulgu | tuttu |
| 4 | Tarama sirasinda degisim tam sonuc vermemeli | `REPOSITORY_CHANGED_DURING_ANALYSIS`, complete false | tuttu |
| 5 | Dis surecten iptal obek sinirinda gorulmeli | iki ornekli testte goruldu | tuttu |
| 6 | Tekillik iki surecte de calismali | iki ornek, tek kosu, 5 satir | tuttu |
| 7 | 1267 MB taze surecte tekrarlanmamali | en yuksek 504,5 MB | tuttu |
| 8 | Takipci commit sayisiyla buyumemeli | 2759 ve 22 917 commit'te de 250 | tuttu |
| 9 | Ilk anlamli render < 1500 ms | en yuksek 139 ms (soguk) | tuttu |
| 10 | Isinmis gecis < 500 ms | en yuksek ortanca 18,3 ms | tuttu |
| 11 | Polling 1 sn, terminalde dursun | ortanca 1004 ms, terminalden sonra 0 istek | tuttu |
| 12 | 390 ve 1440'ta yatay tasma olmasin | ikisinde de `scrollWidth == clientWidth` | tuttu |
| 13 | Panelde olasilik dili olmasin | ilk yazimda vardi, duzeltildi; test ekli | **duzeltildikten sonra** tuttu |
| 14 | Dort sayi ayri gosterilsin | dort ayri kart | tuttu |
| 15 | Statik bulgular model skoruna dahil gibi durmasin | ayri kart, her gorunumde acik cumle | tuttu |

**On bes beklentinin on besi tuttu**, ama 13. madde ilk yazimda tutmuyordu: paneldeki
uyari metinleri yasak ifadeyi olumsuz haliyle iceriyordu ("hata olasiligi degildir").
Cumlenin anlami dogruydu; ifadenin kendisi ekranda duruyordu. Duzeltildi ve ekranda
gorunen metne yasak listeyi uygulayan bir test yazildi.

## 7. Olculmeyenler

- **Kirli agac kontrolunun suresi ayri olculmedi.** Kirli kosunun toplami 386 ms ve bunun
  ne kadarinin `git status` karsiligi oldugu ayrilmadi.
- **Cok buyuk bir depoda calisma agaci okuma maliyeti.** Uc depo 797-2184 dosya.
- **Iki gercek HTTP sureci.** Iki worker ornegi ayni surecte acildi.
- **Tarama sirasinda degisim gercek repolarda.** Yalnizca testte uretildi.
