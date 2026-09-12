# Asama 5 zaman sirali bolme ve sizinti kontrolleri

**Tarih:** 2026-09-12
**Durum:** Bolme manifesti uretildi, kontroller calisti. Model kurulmadi, taban cizgisi
olculmedi, esik secilmedi, normalizasyon uygulanmadi.

Bu adim dondurulmus veri kumesini yalnizca **okudu**. `data/asama5/commit-metrics.csv`
degismedi; ozeti adimin basinda ve sonunda kontrol edildi, ikisinde de ayni.

## Dosyalar ve ozetler

| | |
|---|---|
| Anlik goruntu | `data/asama5/commit-metrics.csv` |
| Anlik goruntu SHA-256 | `1b8e5a5c64ff9ccc6f8495f90711a05b95b5340b2e778dce59a9c6cea4d46e95` |
| Manifest | `data/asama5/split-manifest.csv` |
| Manifest SHA-256 | `01b5cafa2c82cde3dffc98afc0ed571918ebbc648bb8c74fad2bc071b601b8cb` |
| Manifest boyutu | 3 264 416 bayt, 34 166 satir + baslik |
| Bolmeyi ureten commit | `1b4f627` (`tools/Sievert.Measure`, `split` komutu) |
| Bagimsiz kontrolu ureten commit | `736857d` (`tools/Sievert.Measure`, `history-check` komutu) |

Manifest dort alan tasiyor: `RepositoryIdentity`, `Sha`, `Split`, `AuthorDateUtc`.
Oznitelik ya da etiket tasimiyor; hedefi bilmeyen bir dosya olsun diye.

**Deterministik mi:** manifest ust uste iki kez uretildi ve iki dosya `cmp` ile bayt bayt
ayni cikti. Siralama kurali: `RepositoryIdentity` ordinal, sonra `AuthorDateUtc`, sonra
`Sha` ordinal.

## Bolme

Her repo **kendi icinde** siralandi, ilk `floor(N * 0,70)` satir egitim oldu, kalan
satirlar test. Rastgele bolme, karistirma ve capraz dogrulama yok.

| Repo | Toplam | Train | Test | Beklenen train / test |
|---|---|---|---|---|
| Polly | 2759 | 1931 | 828 | 1931 / 828 |
| ShareX | 8490 | 5943 | 2547 | 5943 / 2547 |
| Jellyfin | 22 917 | 16 041 | 6876 | 16 041 / 6876 |
| **Toplam** | **34 166** | **23 915** | **10 251** | **23 915 / 10 251** |

Alti sayi da beklenenle ayni cikti.

### Pozitif etiketler

Pay = pozitif etiketli commit, payda = o bolumun satir sayisi.

| Repo | Train pozitif | Train orani | Test pozitif | Test orani |
|---|---|---|---|---|
| Polly | 251 / 1931 | %13,0 | 10 / 828 | %1,2 |
| ShareX | 884 / 5943 | %14,9 | 129 / 2547 | %5,1 |
| Jellyfin | 3761 / 16 041 | %23,4 | 927 / 6876 | %13,5 |
| **Toplam** | **4896 / 23 915** | **%20,5** | **1066 / 10 251** | **%10,4** |

4896 + 1066 = 5962, yani anlik goruntudeki butun pozitifler bir tarafta duruyor.

**Test orani uc repoda da egitim oranindan dusuk.** Beklenen yon buydu (sag sansur), ama
farkin buyuklugu beklenenden fazla; asagida olculuyor.

### Sinir tarihleri ve commit'leri

| Repo | Train tarih araligi | Test tarih araligi |
|---|---|---|
| Polly | 2013-05-05T05:33:58Z - 2024-07-13T07:40:54Z | 2024-07-13T08:11:39Z - 2026-09-08T20:33:15Z |
| ShareX | 2013-10-08T23:32:10Z - 2021-08-22T18:20:38Z | 2021-08-22T19:03:45Z - 2026-09-10T19:47:52Z |
| Jellyfin | 2012-07-12T06:55:27Z - 2021-05-29T08:56:38Z | 2021-05-30T14:43:16Z - 2026-09-10T19:24:43Z |

| Repo | Son train commit'i | Ilk test commit'i |
|---|---|---|
| Polly | `06b39ff45ff0d3744c28ea2924ac2dd11dd5defc` | `76a93a504b9941d3aed9ea5debb998b97779784f` |
| ShareX | `c7de378b2ed17708fd2f603cf98b0c7e5948f355` | `229cb4b20604645c3d27ee15b8bccd4662ac1e0f` |
| Jellyfin | `d44025c62010fec8e5c7d2915be2f29ed2580782` | `40a43f9485bef407dfdf78ac6dfabcac6d031349` |

Polly'de sinirin iki yani ayni gun (2024-07-13), arada 31 dakika var. ShareX'te de ayni
gun, arada 43 dakika. Sinir sira numarasina gore konuldugu icin bu normal; tarihe gore
bolunseydi o gunun commit'leri bolunemezdi.

## Sag sansur / etiket olgunlugu

Bir commit ancak kendisinden **sonraki** bir duzeltme onu suclarsa pozitif etiket
alabiliyor. Deponun sonuna yakin commit'lerin gelecegi daha az gozlendi, yani etiketi
olmamasi "hata getirmedi" demek olmayabilir.

`MaturityDays` = o reponun **en son** commit tarihi eksi commit'in `AuthorDateUtc`
tarihi. Yalnizca test bolumu icin hesaplandi. Yuzdelikler en yakin sira yontemiyle,
`MetricDistribution` ile ayni kural.

| Repo | Test satiri | Min | Medyan | P95 | Max |
|---|---|---|---|---|---|
| Polly | 828 | 0 | 377 | 777 | 787 |
| ShareX | 2547 | 0 | 442 | 1775 | 1845 |
| Jellyfin | 6876 | 0 | 892 | 1835 | 1929 |

| Repo | < 30 gun | < 90 gun | < 180 gun |
|---|---|---|---|
| Polly | 0 / 25 | 0 / 99 | 0 / 207 |
| ShareX | 5 / 230 | 13 / 683 | 16 / 994 |
| Jellyfin | 11 / 207 | 40 / 559 | 95 / 1027 |

Hucrelerde pay = o gruptaki pozitif sayisi, payda = o gruptaki satir sayisi.

**Polly'nin test bolumunde 180 gunden az olgun 207 commit var ve hicbiri pozitif
degil.** Ayni reponun egitim bolumunde oran %13,0. Bu tek basina "bu commit'ler hata
getirmedi" demek degil; olculebilen tek sey, onlari suclayacak bir duzeltmenin veri
kumesinin kapsadigi sure icinde gorulmedigi.

**Ana bolme degistirilmedi.** Hicbir satir silinmedi, hicbir agirlik verilmedi. Burada
yalnizca olculdu.

**Onceden ilan edilen duyarlilik karari:** Adim 4'te ana test sonucunun yaninda, en az
**90 gun** gozlem suresi olan test commit'leriyle ayri bir duyarlilik sonucu
hesaplanacak. O kume Polly'de 828 - 99 = 729, ShareX'te 2547 - 683 = 1864, Jellyfin'de
6876 - 559 = 6317 satir olur. Bu sonuc **ana sonucun yerine gecmeyecek**, yaninda
duracak.

## Beklentiyle karsilastirma

`asama5-beklenti.md` bu adimdan once yazildi ve **degistirilmedi**. Ilk madde test
bolumundeki pozitif orani icin bant vermisti:

| Repo | Beklenen bant | Olculen | Tuttu mu |
|---|---|---|---|
| Polly | %3-9 | **%1,2** | **HAYIR**, bandin altinda |
| ShareX | %4-11 | %5,1 | evet |
| Jellyfin | %8-19 | %13,5 | evet |

Polly'de tahminim tutmadi. Yonu dogru bildim (test orani butun tarihin %9,5'inin
altinda kalacak) ama buyuklugu cok yanlis: bandin alt ucunu %3 koymustum, cikan %1,2.

Sebebini olcmedim, iki aday var ve ikisi de tahmin: Polly'nin test bolumu son iki yili
kapsiyor ve bu repoda duzeltme sayisi zaten dusuk (`IsFix` %10,6), yani gec commit'leri
suclayacak duzeltme havuzu kucuk; ayrica Polly'nin `.cs` dokunma kademesi hunide en cok
daraltan yer (`asama4-uc-repo.md`). Hangisinin ne kadar etkiledigi **olculmedi**.

Bu satir kapanista da duracak. Tutmayan beklenti silinmiyor.

## Sizinti kontrolleri

`asama5-sizinti-kontrol.md` icindeki liste calistirildi.

| # | Kontrol | Sonuc |
|---|---|---|
| 1 | Snapshot checksum'u kayitli degerle ayni | **GECTI** |
| 2 | Satir sayilari ve pozitif etiketler beklenenle ayni | **GECTI** |
| 3 | Anlik goruntude repo+SHA tekrari yok | **GECTI** (0 tekrar) |
| 4 | Train ile test ayni repo+SHA'yi paylasmiyor | **GECTI** (0 ortak anahtar) |
| 5 | Her repoda zaman sirasi korunuyor | **GECTI** (0 ters komsu cift) |
| 6 | max(train tarihi) <= min(test tarihi) | **GECTI** (uc repoda da) |
| 7 | `IsBugIntroducing` aday oznitelik listesinde degil | **GECTI** |
| 8 | `LabelSource` aday oznitelik listesinde degil | **GECTI** |
| 9 | `Sha`, `Repository`, `RepositoryIdentity`, `AuthorDateUtc` listede degil | **GECTI** |
| 10 | `BotMu` bu adimda listede degil | **GECTI** |
| 11 | Sayisal ozniteliklerde NULL / NaN / sonsuz yok | **GECTI** (0) |
| 12 | Normalizasyon ya da donusum uygulanmamis | **GECTI**, uygulanmadi |
| 13 | Bolme boyutlari beklenenle ayni | **GECTI** |

7-10 birer belge cumlesi degil, calisan bir kontrol: aday liste
`ModelFeatures.EnsureNoExcluded` icinden geciyor ve yasak bir ad gorurse duruyor.
Testler bunu her yasak alan icin ayri ayri siniyor.

12 nasil olculdu: 15 ozniteligin her degeri, CSV'deki ham metinle yeniden okunup
karsilastirildi. 34 166 x 15 = 512 490 karsilastirmanin hicbirinde fark yok, yani
oznitelikler dosyadaki degerin aynisi; log donusumu, olcekleme ya da kirpma uygulansa
burada fark cikardi.

Bir kontrol kalsaydi manifest **yazilmayacakti**; arac once kontrolleri calistirip sonra
yaziyor.

## Tarihsel ozniteliklerin bagimsiz kontrolu

Yedi gecmis olcusu, yalnizca commit'ten **onceki** satirlardan yeniden hesaplanip anlik
goruntudeki degerle karsilastirildi.

**Orneklem:** her repodan 10 train + 10 test, toplam 60 commit. Tohum **42**, sabit ve
kodda yazili; secim manifest sirasindaki satirlarin sabit tohumlu karistirilmasiyla
yapiliyor, yani tekrarlanabilir.

| Olcu | Karsilastirilan | Eslesen | Farkli | En buyuk mutlak fark |
|---|---|---|---|---|
| PriorFixes | 60 | 60 | 0 | 0 |
| PriorChanges | 60 | 60 | 0 | 0 |
| AuthorCommitCount | 60 | 60 | 0 | 0 |
| AuthorFileExperience | 60 | 60 | 0 | 0 |
| DistinctAuthorsOnFiles | 60 | 60 | 0 | 0 |
| MaxFileAgeDays | 60 | 60 | 0 | 0 |
| MinFileAgeDays | 60 | 60 | 0 | 0 |

420 karsilastirma, 0 fark. **GECTI.**

**Kontrol neden bagimsiz:** hesabi `MetricCalculator`'a yaptirmiyor, durumunu kendisi
biriktiriyor. Urun kodundan aldigi tek sey ham veri ve commit sirasi. Onceki commit'lerin
`IsFix` degeri bile anlik goruntuden okunuyor, heuristik yeniden calistirilmiyor; boylece
`PriorFixes` kontrolu `FixSubject`'e bagli kalmiyor.

**Kontrolun ise yaradigini nasil bildim:** kontrolu bilerek bozdum. Durum guncellemesi
olcumden ONCE yapilacak sekilde bir satir kaydirildi, yani commit kendi verisini gormus
oldu. O hâlde yedi olcunun besi fark verdi: `PriorChanges` 60/60 farkli (en buyuk fark
77), `AuthorFileExperience` 60/60 (77), `AuthorCommitCount` 60/60 (1), `PriorFixes` 4/60
(13), `DistinctAuthorsOnFiles` 10/60 (1).

**Iki olcu bu hatayi gormedi:** `MaxFileAgeDays` ve `MinFileAgeDays` bozuk kodda da 0
fark verdi. Sebebi su: commit kendi dosyasini duruma ekleyince dosyanin ilk gorulme
tarihi o commit'in tarihi oluyor ve yas 0 cikiyor, oysa dosya hic bulunamadiginda da yas
0 yaziliyor. Yani bu iki olcu bu **belirli** hataya duyarsiz; baska bir zaman sizintisi
turune duyarli olup olmadiklari bu deneyle gosterilmedi. Bozuk surum commit
edilmedi, deneyden sonra dosya geri alindi.

## CSV basligi ile C# ozelligi arasindaki esleme

Anlik goruntunun basligi `BotMu`; C# tarafinda ozelligin adi `IsBot`
(`SnapshotRow.IsBot`). Baslik degistirilmedi cunku dosya donduruldu ve ozeti kayitli;
basligi duzeltmek yeni bir SHA-256 demek olurdu.

Okuyucu basligi harfi harfine bekliyor: `BotMu` yerine `IsBot` yazan bir dosya okunmuyor,
hata veriyor. Bir test bunu siniyor.

## Bu adimda yapilmayanlar

- Model egitilmedi, ML.NET eklenmedi, yeni NuGet paketi eklenmedi.
- Taban cizgisi olculmedi, `LinesAdded` esigi secilmedi.
- Normalizasyon, log donusumu, eksik deger doldurma, oznitelik eleme yok.
- Bot commit'leri cikarilmadi; bot karari Adim 4'te.
- Etiketler degistirilmedi, sansurlu satirlar silinmedi.
- `asama5-beklenti.md` degistirilmedi.
- Dondurulmus anlik goruntu degistirilmedi.

## 11 / 13 sonucu hakkinda

Asama 4'un etiketsiz orneklemindeki 11 / 13 kacirma sonucu **hedefli secilmis** bir
orneklemden geliyor ("`IsFix` tarafindan dokunulmus fakat etiketlenmemis" commit'ler).
Butun negatif sinifin hata orani degil.

Bu adimda o sayiya dayanarak hicbir sey yapilmadi: satir silinmedi, agirlik verilmedi,
etiket degistirilmedi. Negatif sinifta gurultu riski duruyor; yayginligi bilinmiyor.
