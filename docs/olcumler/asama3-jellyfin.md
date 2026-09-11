# Jellyfin olcumu

**Tarih:** 2026-09-11
**Repo:** [jellyfin/jellyfin](https://github.com/jellyfin/jellyfin) @ `1d7b6d97844c8cc848ed3fb5c4b48bb9cdd5b139`
**Sievert:** `f8906a4` (yeniden yazma oncesi: `5fc4846`)
**Komut:** `sievert check <klon>` ve `sievert scan <klon> --json`

Ucuncu repo. Polly bir kutuphane, ShareX bir masaustu uygulamasi; Jellyfin ise sunucu
tarafinda calisan, ASP.NET Core tabanli, icinde gercek bir test projesi olan buyuk bir
uygulama. Alti kural da acik calistirildi.

Bu repo daha once hic taranmadi, yani kurallarin uzerinde tasarlanmadigi bir orneklem.
SV002-SV006 icin bu ilk gercek olcum.

## Repo ve tarama

| Olcum | Deger |
|---|---|
| Dosya | 2184 |
| Tip | 2268 |
| Metot | 7902 |
| Async metot | 1204 (%15.2) |
| Uretim / test dosya | 1926 / 258 |
| Uretim / test metot | 6282 / 1620 |
| Ayristirilamayan dosya | 0 |
| Tip bulunamayan dosya | 18 |
| Dislanan dosya | 0 |
| Atlanan klasor | 1 |
| Kor nokta (`#if`) | 0 dosya, 0 satir |
| Sure (uc kosu) | 4,11 / 3,67 / 4,27 sn |
| Tepe bellek | 210-221 MB |

Ayristirilamayan dosya sifir: 2184 dosyanin hepsi okundu. Kor nokta da sifir, yani bu
repoda kosullu derleme bloklari icinde kalan kod yok.

## Kural bazinda bulgular

| Kural | Seviye | Bulgu | Uretim | Test |
|---|---|---|---|---|
| SV001 | error | 11 | 11 | 0 |
| SV002 | warning | 23 | 18 | 5 |
| SV003 | warning | 26 | 0 | 26 |
| SV004 | warning | 277 | 271 | 6 |
| SV005 | warning | 120 | 68 | 52 |
| SV006 | info | 421 | 421 | 0 |
| **Toplam** | | **878** | 789 | 89 |

## Seviye dagilimi

| Seviye | Bulgu |
|---|---|
| error | 11 |
| warning | 446 |
| info | 421 |

**Info seviyesindeki 421 bulgunun hepsi SV006'dan geliyor** ve bunlar
`--fail-on` varsayilani `warning` oldugu icin cikis kodunu etkilemiyor. Toplam sayiya
bakip "878 sorun var" demek yaniltici olur: bulgularin yaklasik yarisi (%48) iptal
destegi eksikligi, yani hata degil eksik yetenek.

## Muafiyetler

Kurallarin bulgu uretmeden gectigi 689 yer:

| Sebep | Sayi | Hangi kural |
|---|---|---|
| `testCode` | 272 | SV006 |
| `notPublic` | 231 | SV006 |
| `usingScope` | 104 | SV005 |
| `ownedByType` | 26 | SV005 |
| `callerOwns` | 23 | SV005 |
| `inheritedSignature` | 20 | SV006 |
| `subscription` | 7 | SV001 |
| `signature` | 4 | SV001 |
| `discardedResult` | 2 | SV003 |

SV006'nin muafiyetleri (231+272+20 = 523) urettigi bulgudan (421) fazla.
Yani bu kuralda eleme, uretimden daha buyuk.

## Cikis kodu ve esik davranisi

| Komut | Cikis kodu |
|---|---|
| `check <klon>` (varsayilan `--fail-on warning`) | 1 |
| `check <klon> --fail-on error` | 1 |
| `check <klon> --fail-on error`, SV001 kapali | **0** |
| `check <klon>`, sadece SV006 acik | **0** |
| `check <klon> --fail-on info`, sadece SV006 acik | 1 |

Esik davranisi ilk kez gercek bir repoda dogrulanmis oldu. Ilk iki satirda cikis kodu 1,
cunku SV001 11 adet `error` seviyesinde bulgu uretiyor. SV001 kapatilinca geriye
sadece `warning` ve `info` kaliyor ve `--fail-on error` 0 donuyor: esigin altinda kalan
bulgular ekrana yaziliyor ama build'i kirmiyor. Dorduncu satir SV006'nin `info` seviyesi
tasariminin gercek bir repoda da calistigini gosteriyor.

## Uc repo yan yana

Onceki olcumlerin sayilari degistirilmedi; Polly ve ShareX ayni commit'lerde yeniden
tarandi, bu sefer alti kuralla.

| Olcum | Polly `2247db24` | ShareX `b5a397ea` | Jellyfin `1d7b6d97` |
|---|---|---|---|
| Repo turu | kutuphane | masaustu uygulamasi | sunucu uygulamasi |
| Dosya | 797 | 1138 | 2184 |
| Tip | 897 | 1860 | 2268 |
| Metot | 4655 | 7376 | 7902 |
| Async orani | %21.3 | %7.8 | %15.2 |
| Uretim / test dosya | 479 / 318 | 1138 / 0 | 1926 / 258 |
| SV001 | 0 | 8 | 11 |
| SV002 | 22 | 35 | 23 |
| SV003 | 19 | 57 | 26 |
| SV004 | 5 | 304 | 277 |
| SV005 | 7 | 59 | 120 |
| SV006 | 178 | 96 | 421 |
| **Toplam bulgu** | **231** | **559** | **878** |
| - error | 0 | 8 | 11 |
| - warning | 53 | 455 | 446 |
| - info | 178 | 96 | 421 |
| Cikis kodu | 1 | 1 | 1 |

Asama 2'de Polly'nin SV001 icin sifir vermesi "repo bu kural icin dogru orneklem degil"
diye yorumlanmisti. Alti kuralla bakinca Polly artik 231 bulgu veriyor; sifir olan sey
kural degil, o tek kuraldi. Bir kutuphanede `async void` olmamasi beklenen bir sey ama
`CancellationToken` eksikligi ya da dongu ici LINQ her repoda olabiliyor.

SV004 sayilari dikkat cekici: Polly'de 5, ShareX'te 304, Jellyfin'de 277. Polly'nin
dusuk kalmasi kutuphane olmasindan degil, kod tarzindan geliyor olabilir; bu sayilar
kuralin dogrulugu hakkinda degil, sadece ne kadar ateslediginle ilgili bir sey soyluyor.
Hangi bulgunun gercek oldugu `asama3-dogrulama-listesi.md`'de elle incelenecek.

## Ayni satirda birden fazla bulgu

Adim 5'ten devreden soru: bir zincir ayni satira iki kart basiyor mu, gercek repolarda
ne siklikta? (kural, dosya, satir) uclusunu paylasan bulgulari saydim.

| Repo | Toplam bulgu | Cift kart basan uclu | Hangi kural |
|---|---|---|---|
| Polly | 231 | 0 | - |
| ShareX | 559 | 23 | SV004 (22), SV005 (1) |
| Jellyfin | 878 | 7 | SV004 (7) |

**SV002 uc repoda da hic ciftlemedi.** Adim 5'te bulunan `pendingTask.Result.Wait()`
kalibi 4119 dosyada bir kez bile gecmedi; o durum teorik kaldi. Ciftleme SV004'te
oluyor ve sebebi farkli: ayni satirda iki sorgu bitirici cagri bulunmasi
(`a.Any() && b.Count()` gibi). Orani dusuk: ShareX'te 304 SV004 bulgusunun 22'si
(%7), Jellyfin'de 277'nin 7'si (%3).

Davranis degistirilmedi.
