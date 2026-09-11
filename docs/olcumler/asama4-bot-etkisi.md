# Bot commit'lerinin metriklere etkisi

**Tarih:** 2026-09-11
**Repo:** App-vNext/Polly, 2759 commit (birlestirmeler haric)
**Olcum programi:** `tools/Sievert.Measure`, `measure bot polly-full`

Polly'de commit'lerin **%31'i bot bayrakli** (854 commit). Bu bir kenar durum degil, o
yuzden "bot commit'leri metriklere girsin mi" sorusunun bedeli olculmeden birakilamazdi.

**Haric tutma karari verilmedi.** Asagidaki sayilar, Asama 5'te o karar verilirken
bakilacak veri.

## Ne olculdu

Ayni veri uzerinde metrikler iki kez hesaplandi: birincisinde butun commit'ler,
ikincisinde bot bayrakli commit'ler akistan tamamen cikarilmis hâlde. Karsilastirma
yalnizca **bot disi** commit'ler uzerinden, cunku bot commit'leri ikinci hesapta zaten yok.

| Olcu | Deger |
|---|---|
| Commit | 2759 |
| Bot bayrakli | 854 (%31,0) |
| Bot disi | 1905 |
| **Metrikleri degisen bot disi commit** | **372 / 1905 (%19,5)** |

## Dagilimlar

| Olcu | Hesap | Min | Medyan | P95 | Max |
|---|---|---|---|---|---|
| AuthorCommitCount | bot dahil | 0 | 72 | 359 | 454 |
| AuthorCommitCount | bot haric | 0 | 72 | 359 | 454 |
| PriorChanges | bot dahil | 0 | 49 | 425 | 4576 |
| PriorChanges | bot haric | 0 | 45 | 414 | 4576 |
| DistinctAuthorsOnFiles | bot dahil | 0 | 5 | 34 | 55 |
| DistinctAuthorsOnFiles | bot haric | 0 | 5 | 33 | 54 |

## Beklentinin tersi cikti

Ad degisimi olcumunde (`asama4-ad-degisimi.md`) sunu gormustuk: ad degisimi iceren
commit orani %2,9 iken metrikleri etkilenen commit orani %44,7'ydi, yani etki iceren
orandan cok daha genise yayilmisti. Burada **tersi** oldu: bot orani %31, etkilenen
oran %19,5.

Sebebi veriye bakinca goruluyor. Bot commit'leri 1453 dosya satiri uretmis ama bunlar
yalnizca **101 farkli yola** dagiliyor ve iclerinde sadece **38 tanesi `.cs`**. En cok
dokunulan yollar `Directory.Packages.props` (209), `.github/workflows/ossf-scorecard.yml`
(173), `.github/workflows/build.yml` (154), `.config/dotnet-tools.json` (107). Yani
bot'lar dar bir dosya kumesinde donup duruyor ve insanlarin dokundugu dosyalarin cogunu
hic gormuyorlar.

`AuthorCommitCount` hic degismedi, cunku bot'lar ayri yazarlar; onlari cikarmak bir
insanin kendi commit sayisini degistirmiyor.

## Bu ne demiyor

Bu sayilar "bot'lar onemsiz" demiyor. Bir bot'un dokundugu 101 yoldan biri gercek bir
kaynak dosyaysa, o dosyanin `PriorChanges` ve `DistinctAuthorsOnFiles` degerleri sisiyor.
Medyan `PriorChanges` 49'dan 45'e dusuyor, yani tipik bir commit'in gecmis sayisinin
yaklasik %8'i bot'lardan geliyor.

Ayrica bu tek bir repo. Bot'larin kaynak koda dokundugu bir repoda (ornegin otomatik
bicimlendirme ya da kod uretimi yapan bir bot) etki cok daha buyuk olurdu; olculmedi.
