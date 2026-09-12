# Asama 5 dondurulmus veri kumesi

**Tarih:** 2026-09-12
**Durum:** Anlik goruntu alindi ve butunluk kontrolleri gecti. Bolme yapilmadi, model
kurulmadi, hicbir taban cizgisi olculmedi.

Asama 5'in butun deneyleri veritabanindan degil bu tek dosyadan beslenecek. Sebebi:
veritabani sonradan yeniden madencilik gorurse hangi olcunun hangi veriyle uretildigi
belirsizlesir ve karsilastirilan sayilar ayni kumeden gelmemis olur.

## Dosya

| | |
|---|---|
| Dosya | `data/asama5/commit-metrics.csv` |
| SHA-256 | `1b8e5a5c64ff9ccc6f8495f90711a05b95b5340b2e778dce59a9c6cea4d46e95` |
| Boyut | 5 369 775 bayt (5,1 MiB) |
| Satir | 34 166 veri satiri + 1 baslik |
| Sutun | 22 |
| Ozeti tutan dosya | `data/asama5/commit-metrics.sha256` |
| Sema | `data/asama5/README.md` |

**Ureten Sievert commit'i:** `687a140` (`tools/Sievert.Measure`, `snapshot` komutu).

**Kaynak repo commit'leri** - Asama 4'te madencilik yapilan commit'lerin ayni:

| Repo | Commit |
|---|---|
| Polly | `2247db24` |
| ShareX | `b5a397ea6ccf00659cee593c981be4b01ab641fe` |
| Jellyfin | `1d7b6d97844c8cc848ed3fb5c4b48bb9cdd5b139` |

## Deterministik siralama

Satirlar `RepositoryIdentity`, sonra `AuthorDateUtc`, sonra `Sha` sirasinda. Metin
karsilastirmasi ordinal, yani veritabaninin harmanlama ayarindan bagimsiz; siralama
bellekte yapiliyor.

Bu ucu birlikte benzersiz (asagida tekrar sayisi 0), o yuzden sira tek bir sonuca
kilitleniyor. Denendi: arac ust uste iki kez calistirildi ve iki dosyanin SHA-256'si
ayni cikti.

## Satir ve etiket sayilari

Pay = pozitif etiketli commit, payda = o reponun commit sayisi.

| Repo | Pozitif | Commit | Oran |
|---|---|---|---|
| Polly | 261 | 2759 | %9,5 |
| ShareX | 1013 | 8490 | %11,9 |
| Jellyfin | 4688 | 22 917 | %20,5 |
| **Toplam** | **5962** | **34 166** | **%17,4** |

Uc sayi da Asama 4'te (`asama4-uc-repo.md`) olculen degerlerle birebir ayni. Beklenen
34 166 satir ve 5962 pozitif; cikan da o.

## Tarih araliklari

`AuthorDateUtc`, hepsi UTC.

| Repo | Ilk commit | Son commit |
|---|---|---|
| Polly | 2013-05-05T05:33:58Z | 2026-09-08T20:33:15Z |
| ShareX | 2013-10-08T23:32:10Z | 2026-09-10T19:47:52Z |
| Jellyfin | 2012-07-12T06:55:27Z | 2026-09-10T19:24:43Z |

Uc araligin buyuk olcude **ortustugune** dikkat: bu yuzden bolme butun satirlari tek
havuza koyup tarihe gore degil, her repo kendi icinde yapilacak
(`asama5-sizinti-kontrol.md`, 1.2).

## Butunluk kontrolleri

| Kontrol | Beklenen | Cikan |
|---|---|---|
| Toplam satir | 34 166 | 34 166 |
| Toplam pozitif | 5962 | 5962 |
| Polly | 261 / 2759 | 261 / 2759 |
| ShareX | 1013 / 8490 | 1013 / 8490 |
| Jellyfin | 4688 / 22 917 | 4688 / 22 917 |
| Tekrarlanan `repo + Sha` | 0 | **0** |
| Hedef (`IsBugIntroducing`) NULL | 0 | **0** |
| UTC olmayan tarih | 0 | **0** |
| Farkli `RepositoryIdentity` | 3 | **3** |

### Oznitelik bazinda NULL / NaN / sonsuz

15 ozniteligin hepsi ayri ayri sayildi:

| Oznitelik | NULL | NaN | Sonsuz |
|---|---|---|---|
| LinesAdded | 0 | 0 | 0 |
| LinesDeleted | 0 | 0 | 0 |
| FilesChanged | 0 | 0 | 0 |
| CsFilesChanged | 0 | 0 | 0 |
| Entropy | 0 | 0 | 0 |
| DirectoryCount | 0 | 0 | 0 |
| SubsystemCount | 0 | 0 | 0 |
| MaxFileAgeDays | 0 | 0 | 0 |
| MinFileAgeDays | 0 | 0 | 0 |
| PriorChanges | 0 | 0 | 0 |
| PriorFixes | 0 | 0 | 0 |
| DistinctAuthorsOnFiles | 0 | 0 | 0 |
| AuthorCommitCount | 0 | 0 | 0 |
| AuthorFileExperience | 0 | 0 | 0 |
| IsFix | 0 | 0 | 0 |

`Entropy` tek ondalikli oznitelik, yani NaN ve sonsuz riski asil orada; digerleri tam
sayi ve veritabaninda NOT NULL. Yine de hepsi sayildi, cunku "olamaz" demek kontrol
etmenin yerine gecmiyor.

### Kontroller nasil yapildi

Sayilar iki kez uretildi: once anlik goruntuyu yazan aracin kendi raporuyla, sonra
dosyayi bastan okuyan ayri bir betikle. Ikisi ayni sayilari verdi. Ikinci okuma
siralamanin dogrulugunu da kontrol etti (`RepositoryIdentity`, `AuthorDateUtc`, `Sha`
sirasi bozulmamis).

Bunu yapmamin sebebi Asama 4'teki dersin ayni olmasi: uretici kendi ciktisini kontrol
ettiginde yalnizca deterministik oldugunu gosterir, dogru oldugunu gostermez.

## `LabelSource` neden snapshot'ta duruyor

`LabelSource` **denetim alani**: etiketin nereden geldigini sonradan kontrol
edebilmek icin var. Su an tek deger `szz`, etiketlenmemis commit'te bos.

**Modele kesinlikle girmiyor.** Dolu olup olmamasi hedefin kendisi: 5962 satirda dolu ve
hepsi pozitif, 0 satirda "dolu ama negatif" durumu var. Bu alani ozniteligine cevirmek
mukemmel bir metrik ureten ve hicbir sey ogrenmemis bir model verirdi.

Ayni sebeple `Sha` kimlik alani ve modele girmiyor, `AuthorDateUtc` yalnizca bolme icin
kullaniliyor. Tam liste `asama5-sizinti-kontrol.md`, 3. bolum.

## Etiketlerin dogrulugu hakkinda ne biliyoruz

Asama 4'un elle dogrulamasi iki sayi verdi:

- Etiketli orneklemde dogru suclama **12 / 12**.
- Etiketsiz orneklemde kacirma **11 / 13**.
- 5 satir Belirsiz olarak paydadan cikarildi.

**Ikinci sayi hedefli secilmis bir orneklemden geliyor.** Butun negatiflerden rastgele
alinmadi; "`IsFix` tarafindan dokunulmus fakat etiketlenmemis" commit'lerden secildi,
yani kacirma ihtimalinin en yuksek oldugu yerden.

Bu yuzden 11 / 13 (%84,6):

- Negatif sinifin genel hata orani **degildir**.
- Bu veri kumesine duzeltme ya da agirlik orani olarak **uygulanmayacaktir**.
- Yeniden etiketlemede **kullanilmayacaktir**.
- Yalnizca "hedefli secilmis etiketsiz orneklemde yuksek kacirma isareti" olarak okunur.

Negatif sinifta etiket gurultusu riski oldugunu soyluyoruz; **yayginligini bu orneklemden
tahmin etmiyoruz.** Tahmin icin butun negatiflerden rastgele bir orneklem alip elle
bakmak gerekirdi ve o olcum yapilmadi.

Ayni uyari `asama5-beklenti.md` ve `asama5-sizinti-kontrol.md` icinde de duruyor.

## Bu adimda yapilmayanlar

Snapshot alindi, uzerinde hicbir sey yapilmadi:

- Train/test bolmesi gerceklestirilmedi.
- Normalizasyon ya da donusum uygulanmadi.
- Esik secilmedi, taban cizgisi olculmedi, model egitilmedi.
- Etiketler yeniden yazilmadi, bot commit'leri cikarilmadi.
- Ad degisimi esigi degistirilmedi.
