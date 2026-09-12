# Asama 5 dondurulmus veri kumesi

Bu klasorde Asama 5'in butun deneylerinin besleneceği tek dosya duruyor. Veritabanindan
bir kez cikarildi ve **degistirilmeyecek**. Sebebi: veritabani sonradan yeniden
madencilik gorurse eski olculerin hangi veriyle uretildigi belirsizlesir.

| Dosya | Ne |
|---|---|
| `commit-metrics.csv` | Anlik goruntunun kendisi, 34 166 satir |
| `commit-metrics.sha256` | Dosyanin SHA-256 ozeti |

Kontrol:

```
cd data/asama5
shasum -a 256 -c commit-metrics.sha256
```

Ureten komut (`SIEVERT_DB` tanimliyken):

```
dotnet run --project tools/Sievert.Measure -c Release -- snapshot data/asama5/commit-metrics.csv
```

Sayilar ve tarih araliklari `docs/olcumler/asama5-veri-kumesi.md` icinde.

## Bicim

UTF-8, BOM yok. Satir sonu `\n`. Ayrac virgul. Alan icinde virgul, tirnak ya da satir
sonu varsa alan cift tirnakla sarilir ve icerideki tirnak ikilenir; bu kumede oyle bir
alan cikmadi.

Mantiksal alanlar `0` / `1` yaziliyor. Ondalikli tek alan `Entropy` ve nokta ile,
kayipsiz (`R`) bicimde yaziliyor. Tarihler `yyyy-MM-ddTHH:mm:ssZ`, hepsi UTC.

**Satir sirasi deterministik:** `RepositoryIdentity`, sonra `AuthorDateUtc`, sonra `Sha`.
Siralama ordinal, yani veritabaninin harmanlama ayarindan bagimsiz. Ucu birlikte
benzersiz oldugu icin sira tek bir sonuca kilitleniyor; arac iki kez calistirildiginda
ayni SHA-256 cikiyor ve bu denendi.

## Sutunlar

| # | Sutun | Tip | Ne |
|---|---|---|---|
| 1 | `Repository` | metin | Deponun klasor adi. Gosterim icin. |
| 2 | `RepositoryIdentity` | metin | Uzak adresten normalize edilmis kimlik. Eslestirme bundan. |
| 3 | `Sha` | metin (40) | Commit'in tam sha'si. Kimlik alani. |
| 4 | `AuthorDateUtc` | tarih-saat (UTC) | Yazar tarihi. Committer tarihi degil (ADR 0011). |
| 5 | `LinesAdded` | tamsayi | Eklenen satir. |
| 6 | `LinesDeleted` | tamsayi | Silinen satir. |
| 7 | `FilesChanged` | tamsayi | Degisen dosya. |
| 8 | `CsFilesChanged` | tamsayi | Degisen `.cs` dosyasi. |
| 9 | `Entropy` | ondalik | Shannon entropisi; tek dosyalik commit'te 0. |
| 10 | `DirectoryCount` | tamsayi | Dokunulan farkli dizin. |
| 11 | `SubsystemCount` | tamsayi | Dokunulan farkli alt sistem (yolun ilk bileseni). |
| 12 | `MaxFileAgeDays` | tamsayi | Dokunulan dosyalarin en buyuk yasi, gun. |
| 13 | `MinFileAgeDays` | tamsayi | En kucuk yasi, gun. |
| 14 | `PriorChanges` | tamsayi | Bu dosyalarin daha once kac kez degistigi, toplam. |
| 15 | `PriorFixes` | tamsayi | Ayni sayim, sadece duzeltme commit'leri. |
| 16 | `DistinctAuthorsOnFiles` | tamsayi | Bu dosyalara daha once dokunmus farkli yazar. |
| 17 | `AuthorCommitCount` | tamsayi | Yazarin bu commit'ten onceki commit sayisi. |
| 18 | `AuthorFileExperience` | tamsayi | Yazarin bu dosyalara daha once kac kez dokundugu. |
| 19 | `IsFix` | 0/1 | Mesaj basligi duzeltme imasi tasiyor mu. Heuristik. |
| 20 | `IsBugIntroducing` | 0/1 | **Hedef degisken.** SZZ etiketi. |
| 21 | `LabelSource` | metin ya da bos | Etiketin kaynagi; su an tek deger `szz`, etiketsizde bos. |
| 22 | `BotMu` | 0/1 | Yazar bot gorunuyor mu. |

5-19 arasi 15 sutun commit ozniteligi ve ADR 0013'te tanimli. Hepsi **yalnizca o
commit'ten onceki** veriyle hesaplandi.

## Modele neyin girmedigi

Snapshot'ta duran her sutun oznitelik degil:

- `IsBugIntroducing` hedef degisken, oznitelik degil.
- `LabelSource` **denetim alani**; modele kesinlikle girmiyor. Dolu olup olmamasi hedefin
  kendisi: 5962 satirda dolu, hepsi pozitif. Modele girerse mukemmel ama bos bir sonuc
  uretir.
- `Sha` kimlik, modele girmiyor.
- `Repository` ve `RepositoryIdentity` kimlik; ayni-repo deneyinde modele girmiyor.
- `AuthorDateUtc` bolme icin; varsayilan model ozniteligi degil.
- `BotMu` duyarlilik deneyi icin; ana modelde kullanilmiyor.

Ayrintisi ve sebepleri `docs/olcumler/asama5-sizinti-kontrol.md` icinde.
