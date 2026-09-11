# Uc repoda tam madencilik

**Tarih:** 2026-09-11
**Repolar:** Polly `2247db24`, ShareX `b5a397ea`, Jellyfin `1d7b6d97` - Asama 3'tekilerin
ayni commit'leri, tam klon (shallow degil).

**Hangi kodla olculdu:** madencilik ve metrikler `dca4c8d`; etiketleme, huni ve
esdegerlik sayilari satir kaymasi duzeltildikten sonra, `9592508` ve sonrasi. Her tablonun
basinda hangisi oldugu ayrica yaziyor.

Bu dosya Asama 4'un butun boru hattini (madencilik, metrik, etiketleme) uc repoda
calistirip sayilari yan yana koyuyor. **Asama 3'un bulgu sayilariyla birlestirilmedi**,
o baska bir olcum ve kendi dosyasinda duruyor.

## Beklentiler (olcumden ONCE yazildi)

Polly'de olculenler: IsFix %10,6 (292 / 2759), etiket orani %9,9 (272 / 2759).

**ShareX** (9479 commit, masaustu uygulamasi, tek ana gelistirici agirlikli):

- IsFix (tam kelime) icin **%6-9** bekliyorum, yani Polly'nin altinda. Sebep: ShareX'in
  commit mesajlarinda gecmis zaman kalibi (`Fixed ...`) yaygin ve mevcut tanim tam kelime
  aradigi icin `Fixed` eslesmiyor.
- Kok eslesmesiyle oranin **belirgin sekilde** yukselmesini bekliyorum, Polly'deki
  %10,6 -> %12,0 farkindan daha genis: **%12-18** bandi. Iki sayi arasindaki makas
  ShareX'te Polly'dekinden buyuk olacak.
- Etiket orani IsFix'e yakin, **%6-10**. ShareX neredeyse tamamen C# oldugu icin
  ".cs dokunmayan duzeltme" kaybi Polly'deki %31'den az olmali; huninin en cok daraldigi
  yer Polly'de `.cs` dokunma kademesiydi, ShareX'te **silinen satir** kademesine kayabilir.

**Jellyfin** (30 004 commit, sunucu uygulamasi, PR akisi, cok katkici):

- IsFix (tam kelime) **%10-14**. PR basliklarinda `Fix ...` kalibi yaygin ve bu tam
  kelime olarak esliyor.
- Kok eslesmesiyle **%13-18**.
- Etiket orani **%8-14**. Cok katkicili bir repoda ayni satirlara birden fazla duzeltme
  dokunuyor, yani suclamalar ust uste binip farkli commit sayisini bastiriyor olabilir.
- Bot orani Polly'nin %31'inin **altinda** olmali; Polly'de dependabot cok yogundu.

**Sureler.** SZZ Polly'de 288 duzeltme icin 125 sn surdu. Jellyfin'de commit sayisi 10
kat, duzeltme sayisi da benzer oranda artarsa **20-45 dakika** bekliyorum; ustelik blame
daha buyuk bir tarihte geriye yurudugu icin duzeltme basina sure de artacak.

**Huninin en cok daraldigi yer.** Polly'de `.cs` dokunma kademesiydi (288 -> 200).
Uc repoda ayni kademe mi daraliyor, yoksa repo turune gore degisiyor mu - bu olcumun
dis gecerlilik sorusu. Tahminim: ShareX ve Jellyfin C# agirlikli oldugu icin o kademe
daha az daralacak ve darbogaz "silinen satiri olan" kademesine kayacak.

---

## Klonlar

| Repo | Commit | Klon suresi | Klon boyutu | Toplam commit (git) |
|---|---|---|---|---|
| Polly | `2247db24` | onceden klonlanmisti | 68 MB | 2953 |
| ShareX | `b5a397ea` | 36,5 sn | 129 MB | 9479 |
| Jellyfin | `1d7b6d97` | 29,9 sn | 110 MB | 30 004 |

Ucu de tam klon; `Shallow` satiri uc kosuda da `hayir` dedi.

## Madencilik

**Olcum surumu:** `dca4c8d`. Madencilik kodu B070'ten etkilenmiyor, sayilar gecerli.

| Olcu | Polly | ShareX | Jellyfin |
|---|---|---|---|
| Okunan commit | 2759 | 8490 | 22 917 |
| Atlanan birlestirme | 194 | 989 | 7087 |
| Tarih araligi (UTC) | 2013-05-05 - 2026-09-08 | 2013-10-08 - 2026-09-10 | 2012-07-12 - 2026-09-10 |
| Farkli yazar (eposta) | 140 | 313 | 1853 |
| Bot bayrakli commit | 854 / 2759 (%31,0) | 0 / 8490 (%0,0) | 928 / 22 917 (%4,0) |
| `Co-Authored-By` tasiyan commit | 700 / 2759 (%25,4) | 3 / 8490 (%0,04) | 951 / 22 917 (%4,1) |

Bot orani hakkindaki tahmin tuttu: Jellyfin (%4,0) Polly'nin (%31,0) cok altinda kaldi.
ShareX'te hic bot yok - depo tek gelistirici agirlikli ve bagimlilik guncellemeleri elle
yapilmis.

## IsFix: iki tanim yan yana

| Repo | Tam kelime | Kok eslesmesi | Sadece kokte yakalanan |
|---|---|---|---|
| Polly | 292 / 2759 (%10,6) | 330 / 2759 (%12,0) | 38 |
| ShareX | 642 / 8490 (%7,6) | 1341 / 8490 (%15,8) | 699 |
| Jellyfin | 3047 / 22 917 (%13,3) | 4066 / 22 917 (%17,7) | 1019 |

**Tahminler tuttu.** ShareX icin %6-9 demistim, %7,6 cikti; kok eslesmesi icin %12-18
demistim, %15,8 cikti. Jellyfin icin %10-14 ve %13-18 demistim, %13,3 ve %17,7 cikti.

Tahminin gerekcesi de doğrulandi: **iki tanim arasindaki makas ShareX'te acik ara en
genis.** Tam kelimeden kok eslesmesine gecince Polly'de sayi 1,13 kat, Jellyfin'de 1,33
kat, ShareX'te **2,09 kat** artiyor. ShareX'in commit mesajlarinda gecmis zaman kalibi
(`Fixed ...`) yaygin ve mevcut tanim onu goremiyor.

Bu, tek repoda olculen bir heuristigin baska bir repoda cok farkli davranabilecegini
gosteriyor: ayni kural ShareX'te duzeltmelerin yarisini kaciriyor olabilir.


## Etiketleme sirasinda bulunan hata: bir satirlik kayma

Bu adimin en onemli sonucu bir sayi degil, bir hata. Bosluk esdegerlik olcumu icin
bizim blame sonucumuz `git blame` ciktisiyla satir satir karsilastirilinca uyum
beklenenden dusuk cikti. Sayiyi kabul etmeden once satir numaralarini bir kaydirip
denedim:

| Karsilastirma | Ayni satir |
|---|---|
| Bizimki vs duz `git blame` | 1541 / 2243 (%68,7) |
| Bizimki **bir satir geri kaydirilmis** vs duz `git blame` | **2188 / 2243 (%97,5)** |
| Bizimki bir satir ileri kaydirilmis vs duz `git blame` | 1516 / 2243 (%67,6) |

Sebep: LibGit2Sharp'in `BlameHunk.FinalStartLineNumber` degeri **0 tabanli**, yamadan
cikardigimiz silinen satir numaralari ise **1 tabanli**. Ikisi dogrudan karsilastiriliyordu,
yani SZZ her seferinde bir alttaki satiri sucluyordu.

**Neden testler yakalamadi:** Adim 4'teki testlerin dosyalari kucuktu ve butun satirlari
tek bir commit'ten geliyordu. Bir satirlik kayma o durumda ayni commit'e denk geliyor ve
test geciyor. Yeni bir test eklendi: dosyanin satirlari iki ayri commit'ten geliyor ve
duzeltme yalnizca birincisinin satirini degistiriyor. O test hatali kodda **dusuyor**.

**Etiketlere etkisi:**

| Repo | Hatali kod | Duzeltilmis kod | Fark |
|---|---|---|---|
| Polly | 272 / 2759 (%9,9) | 261 / 2759 (%9,5) | -11 |
| ShareX | 1142 / 8490 (%13,5) | 1013 / 8490 (%11,9) | -129 |

Satir duzeyinde etki cok daha buyuk (%31 satirda farkli commit sucIaniyordu) ama etiket
kumesine yansimasi daha kucuk: ayni dosyada komsu satirlar cogu zaman ayni commit'ten
geliyor, o yuzden kayma cogu satirda ayni sonuca cikiyor. Asagidaki butun etiket sayilari
**duzeltilmis kodla** uretildi.

## Bosluk esdegerligi

Ayni 50 duzeltme commit'i (ayni tohum, ayni secim), 274 dosya, 2243 satir. Olcum iki kez
yapildi: once satir kaymasi duzeltilmeden (`dca4c8d`), sonra duzeltildikten sonra
(`9592508`).

| Karsilastirma | Eski olcum (`dca4c8d`) | Yeni olcum (`9592508`) | Fark |
|---|---|---|---|
| Bizimki vs duz `git blame` | 1541 / 2243 (%68,7) | **2190 / 2243 (%97,6)** | +649 satir |
| Bizimki vs `git blame -w` | 805 / 2243 (%35,9) | 762 / 2243 (%34,0) | -43 satir |
| `git blame -w` vs duz `git blame` | 1426 farkli (%63,6) | 1428 farkli (%63,7) | ~ayni |

Duz blame uyumunun %97,5'in uzerine cikmasi bekleniyordu, **%97,6 cikti**. Kaydirma
denemeleri de artik ters yonde: bir satir ileri kaydirinca uyum 1541'e, bir satir geri
kaydirinca 1564'e dusuyor - yani hizalama simdi dogru yerde.

**Esdegerlik yine gosterilemedi, hatta daha net gosterilemedi.** Kayma duzeltilince
bizimki duz blame'e neredeyse tam oturdu (%97,6) ama `git blame -w` ile uyum **dusuyor**
(%35,9'dan %34,0'a). Sebep ucuncu satirda: `-w` secenegi git'in kendi ciktisini bu
satirlarin %63,7'sinde degistiriyor. Yani bizim suzgecimiz blame'in cevabini hic
degistirmiyor, sadece hangi satirlarin soruldugunu eliyor; `-w` ise cevabin kendisini
degistiriyor. Ikisi ayni isi yapmiyor ve yeni sayilar bunu eskisinden daha acik soyluyor.

**Kalan %2,4 (53 satir) ne?** Duz `git blame` ile aramizda hâlâ 53 satirlik fark var ve
bunun ne oldugu **arastirilmadi**. Beklenen esigi gectigi icin durdum; iki blame
uygulamasinin diff heuristiklerinde (satir sonu, en kucuk fark secimi, sinir commit
islemesi) ayrildigi yerler olabilir ama bu bir tahmin, olculmedi.

## Metrik dagilimlari

15 olcunun uc repodaki min / medyan / p95 / max degerleri
`docs/olcumler/` altindaki `*-metrik.json` dosyalarindan geliyor.

| Olcu | Repo | Min | Medyan | P95 | Max |
|---|---|---|---|---|---|
| AuthorCommitCount | Polly | 0 | 125 | 638 | 775 |
|  | ShareX | 0 | 2483 | 6304 | 6728 |
|  | Jellyfin | 0 | 324 | 7197 | 8342 |
| AuthorFileExperience | Polly | 0 | 17 | 176 | 1324 |
|  | ShareX | 0 | 44 | 732 | 17521 |
|  | Jellyfin | 0 | 12 | 909 | 18411 |
| CsFilesChanged | Polly | 0 | 0 | 22 | 339 |
|  | ShareX | 0 | 2 | 9 | 972 |
|  | Jellyfin | 0 | 1 | 13 | 1278 |
| DirectoryCount | Polly | 0 | 1 | 10 | 64 |
|  | ShareX | 1 | 2 | 8 | 138 |
|  | Jellyfin | 0 | 1 | 10 | 256 |
| DistinctAuthorsOnFiles | Polly | 0 | 5 | 29 | 55 |
|  | ShareX | 0 | 5 | 45 | 186 |
|  | Jellyfin | 0 | 6 | 46 | 962 |
| Entropy | Polly | 0 | 0.54 | 3.94 | 7.95 |
|  | ShareX | 0 | 0.84 | 3.6 | 9.48 |
|  | Jellyfin | 0 | 0 | 3.33 | 8.99 |
| FilesChanged | Polly | 0 | 2 | 24 | 378 |
|  | ShareX | 1 | 2 | 24 | 1465 |
|  | Jellyfin | 0 | 1 | 18 | 1409 |
| IsFix | Polly | 0 | 0 | 1 | 1 |
|  | ShareX | 0 | 0 | 1 | 1 |
|  | Jellyfin | 0 | 0 | 1 | 1 |
| LinesAdded | Polly | 0 | 5 | 502 | 50030 |
|  | ShareX | 0 | 21 | 1316 | 165538 |
|  | Jellyfin | 0 | 6 | 261 | 103121 |
| LinesDeleted | Polly | 0 | 2 | 193 | 50333 |
|  | ShareX | 0 | 8 | 980 | 165545 |
|  | Jellyfin | 0 | 3 | 180 | 88471 |
| MaxFileAgeDays | Polly | 0 | 830 | 3628 | 4850 |
|  | ShareX | 0 | 915 | 4203 | 4690 |
|  | Jellyfin | 0 | 1077 | 3960 | 5170 |
| MinFileAgeDays | Polly | 0 | 383 | 2332 | 4497 |
|  | ShareX | 0 | 237 | 2904 | 4690 |
|  | Jellyfin | 0 | 427 | 3334 | 5122 |
| PriorChanges | Polly | 0 | 62 | 382 | 4576 |
|  | ShareX | 0 | 115 | 1071 | 21998 |
|  | Jellyfin | 0 | 95 | 1414 | 30432 |
| PriorFixes | Polly | 0 | 2 | 28 | 327 |
|  | ShareX | 0 | 5 | 49 | 820 |
|  | Jellyfin | 0 | 7 | 108 | 2457 |
| SubsystemCount | Polly | 0 | 1 | 3 | 7 |
|  | ShareX | 1 | 1 | 4 | 18 |
|  | Jellyfin | 0 | 1 | 5 | 27 |

`AuthorCommitCount` medyani ShareX'te 2483, Polly'de 125. ShareX'in commit'lerinin cogu
tek bir kisiden geliyor, o yuzden "yazarin onceki commit sayisi" cok yuksek bir tabandan
basliyor. Ayni olcunun Asama 5'te uc repoda ayni anlama gelmeyecegini simdiden soylemek
gerekiyor.

`Entropy` medyani Jellyfin'de 0 (commit'lerin yarisindan fazlasi tek dosya degistiriyor),
ShareX'te 0,84. `CsFilesChanged` medyani Polly'de 0 - Polly'nin commit'lerinin yarisindan
fazlasi hic `.cs` degistirmiyor, ki SZZ hunisindeki en buyuk kaybin sebebi de bu.

## Saglik kontrolleri

| # | Kontrol | Sonuc |
|---|---|---|
| 1 | Commit sayisi `git rev-list --count HEAD` ile uyusuyor mu | **GECTI** |
| 2 | Tarih araligi `git log` ile uyusuyor mu | **GECTI** |
| 3 | Eklenen/silinen satirlar mantikli mi | **GECTI** (uc deger isaretlendi) |
| 4 | Ayni sha iki kez yazilmis mi, kimlik cakismasi var mi | **GECTI** |
| 5 | Mutlak yol sizmis mi | **GECTI** |
| 6 | Zaman sizintisi: kayitli metrikler yeniden hesaplananla ayni mi | **GECTI** |
| 7 | `LocalPath` dolu mu, degilse arac net hata veriyor mu | **GECTI** |

**1) Commit sayisi.** Uc repoda da `git rev-list --count HEAD` eksi birlestirme sayisi,
bizim okudugumuz sayiya birebir esit: Polly 2953 - 194 = 2759, ShareX 9479 - 989 = 8490,
Jellyfin 30 004 - 7087 = 22 917.

**2) Tarih araligi.** `git log --no-merges` ile alinan ilk ve son tarih uc repoda da
veritabanindaki araliga esit (Polly 2013-05-05 / 2026-09-08, ShareX 2013-10-08 /
2026-09-10, Jellyfin 2012-07-12 / 2026-09-10).

**3) Satirlar.** Negatif deger yok. En buyuk commit'ler: ShareX'te 165 538 eklenen satir
(`NuGet packages`), 133 633 (`Initial commit of ShareX project r748`), 132 308
(`Update project files and generated resources`); Jellyfin'de 103 121 (`Pushing missing
changes`). Dordu de ya disaridan alinan kod ya da uretilen dosya; hata degil ama
Asama 5'te uc deger olarak dikkat isterler.

**4) Benzersizlik.** Repo ici tekrar eden `(RepositoryId, Sha)` cifti: 0. Ayni kimlige
sahip iki depo: 0. Uc repo ayni veritabaninda ve kimlikleri uzak adresten geliyor
(`github.com/app-vnext/polly`, `github.com/sharex/sharex`,
`github.com/jellyfin/jellyfin`). Birden fazla repoda gorunen ayni sha: 0.

**5) Mutlak yol.** `/` ile baslayan `Path` ya da `OldPath` kaydi: 0 (198 865 dosya
satiri icinde).

**6) Zaman sizintisi.** Her repodan rastgele 20 commit secilip butun olculeri yeniden
hesaplandi ve kayitliyla karsilastirildi. Polly 20/20 ayni, ShareX 20/20 ayni,
Jellyfin 20/20 ayni. Toplam 60 commit, 0 fark.

**7) LocalPath.** Uc kayitta da dolu ve klasorler mevcut. Yol bozuldugunda ne oldugu da
denendi: arac `Deponun yerel klasoru bulunamadi (...)` yazip cikis kodu 2 ile duruyor,
sessizce bos sonuc uretmiyor.

## SZZ hunisi

**Olcum surumu:** `9592508` (satir kaymasi duzeltilmis).

| Kademe | Polly | ShareX |
|---|---|---|
| Duzeltme commit'i (`IsFix`) | 292 | 642 |
| Islenen (ebeveyni var, dosya sinirini asmiyor) | 288 | 638 |
| **`.cs` degistiren** | **200 (%69,4)** | **561 (%87,9)** |
| Silinen `.cs` satiri olan | 180 | - |
| Birini suclayabilen | 176 (%61,1) | 486 (%76,2) |
| **Etiketlenen commit** | **261 / 2759 (%9,5)** | **1013 / 8490 (%11,9)** |
| Blame'e sorulan satir | 5102 | 9450 |
| Atlanan buyuk duzeltme | 4 | 4 |

**Huninin en cok daraldigi yer degisiyor.** Polly'de `.cs` dokunma kademesi duzeltmelerin
%30,6'sini goturuyor; ShareX'te ayni kademe yalnizca %12,1'ini goturuyor. Tahminim bu
yondeydi ve tuttu: ShareX neredeyse tamamen C# ve duzeltmelerinin cogu kaynak koda
dokunuyor.

Ama **etiket orani tahminim tutmadi.** ShareX icin %6-10 demistim, %11,9 cikti ve
`IsFix` oraninin (%7,6) UZERINDE. Gerekcem yanlisti: etiket oraninin IsFix oranina yakin
kalacagini varsaymistim, oysa suclayan her duzeltme ortalama birden fazla farkli commit
sucluyor. ShareX'te 486 suclayan duzeltme 1013 farkli commit etiketliyor, yani duzeltme
basina 2,08. Polly'de bu oran 261 / 176 = 1,48. Tek gelistiricili bir repoda ayni dosyanin
satirlari daha cok farkli commit'e dagilmis oluyor.

## Sure ve tepe bellek

Her katman, her repo. `/usr/bin/time -l`, Release yapisi, varsayilan GC.

| Katman | Polly | ShareX | Jellyfin |
|---|---|---|---|
| Madencilik (`mine --db`) | 5,44 sn / 323 MB | 115,59 sn / 479 MB | 55,10 sn / 676 MB |
| Metrik (`metrics`) | 0,61 sn / 169 MB | 1,55 sn / 179 MB | 2,62 sn / 195 MB |
| Etiketleme (`label`) | 117,71 sn / 218 MB | 851,77 sn / 442 MB | bitmedi, asagiya bak |

Madencilikte ShareX, Jellyfin'den **uzun** suruyor (115 sn'ye karsi 55 sn) hâlbuki
commit sayisi ucte biri. Sebep diff'lerin buyuklugu: ShareX'in tarihinde disaridan alinan
kod ve uretilen dosyalar var, tek commit'te 165 bin satir degisebiliyor.

Metrik katmani uc repoda da saniyeler mertebesinde ve bellek neredeyse sabit (169-195 MB);
veritabanindan okuyup aritmetik yapmanin bedeli commit sayisiyla dogrusal ve kucuk.

Etiketleme acik ara en pahali katman. Sebep ADR 0014'te yazili: blame, sorulan satir icin
degil o satirin gectigi her dosya icin butun tarihi geriye dogru yuruyor.

## Veritabani

| Tablo | Satir | Boyut |
|---|---|---|
| `CommitFiles` | 198 865 | 32 MB |
| `Commits` | 34 166 | 15 MB |
| `CommitMetrics` | 34 166 | 5,9 MB |
| `Repositories` | 3 | 48 kB |
| **Veritabani toplam** | | **60 MB** |

34 166 commit = 2759 + 8490 + 22 917. Uc repo tek veritabaninda, kimlik cakismasi yok.

## Jellyfin etiketlemesi bitmedi

Jellyfin'in etiketlemesi bu oturumda **tamamlanmadi**. Iki kez baslatildi: birincisi
hatali kodla bir saatten fazla kostu ve kayma hatasi bulununca durduruldu, ikincisi
duzeltilmis kodla baslatildi ve bir saati askin suredir suruyordu, rapor yazilirken hâlâ
bitmemisti.

Sebep ADR 0014'te yazili olanin buyuk olcekteki hâli: blame, sorulan satir icin degil o
satirin gectigi her dosya icin butun tarihi geriye dogru yuruyor. Jellyfin'de 22 917
commit ve 3047 duzeltme var; ShareX'te 638 duzeltme 852 sn surdugune gore, duzeltme
basina sure ayni kalsa bile 4000 sn'lik (yaklasik 68 dakika) bir taban var - ve
Jellyfin'in tarihi ShareX'inkinden uc kat uzun oldugu icin duzeltme basina sure de daha
yuksek.

**Tahmin tutmadi.** "20-45 dakika" demistim; gercek sure bunun en az iki kati. Tahminde
commit sayisinin 10 kati artmasinin sureyi 10 kat artiracagini varsaymisim, oysa blame'in
maliyeti tarihin uzunluguyla da carpiliyor, yani buyume dogrusaldan hizli.

Bu yuzden Jellyfin icin su an elde **etiket sayisi yok**; veritabanindaki
`IsBugIntroducing` sutunu o repo icin bos. Madencilik, metrikler ve butun saglik
kontrolleri Jellyfin'de tamamlandi, eksik olan yalnizca etiketleme.

Bir sonraki adimda yapilacak sey belli: etiketlemeyi kosturup sayiyi eklemek, ve
maliyeti dusurmek icin blame ciktisini dosya basina onbellege almak (ayni dosya birden
fazla duzeltme tarafindan blame ediliyor; su an her seferinde bastan hesaplaniyor).

## Ozet

| | Polly | ShareX | Jellyfin |
|---|---|---|---|
| Commit | 2759 | 8490 | 22 917 |
| `IsFix` (tam kelime) | 292 (%10,6) | 642 (%7,6) | 3047 (%13,3) |
| Etiket | 261 (%9,5) | 1013 (%11,9) | olculmedi |
| Madencilik | 5,44 sn | 115,59 sn | 55,10 sn |
| Metrik | 0,61 sn | 1,55 sn | 2,62 sn |
| Etiketleme | 117,71 sn | 851,77 sn | bitmedi |

Uc repo tek veritabaninda, kimlik cakismasi yok, yedi saglik kontrolunun yedisi de gecti.
