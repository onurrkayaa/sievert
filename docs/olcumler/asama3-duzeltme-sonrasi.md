# Uc kural duzeltmesinin etkisi: once/sonra

**Tarih:** 2026-09-11
**Sievert:** kural duzeltmelerinin yapildigi commit (bu dosyayla ayni commit)
**Komut:** `sievert check <klon> --json`, alti kural da acik
**Repolar:** Polly `2247db24`, ShareX `b5a397ea`, Jellyfin `1d7b6d97` (Asama 3'tekinin ayni)

Asama 3'un dogrulama listesi uc kuralda tek ve duzeltilebilir kok nedenler gosterdi.
Bu dosya o duzeltmelerin bulgu sayilarina ne yaptigini yaziyor. Olculen sey sadece
**sayilar**: kaynak koda bakip yeniden E/H siniflandirmasi yapilmadi, yani asagida
yeni bir precision rakami yok.

Duzeltme oncesi sayilar, duzeltmeden onceki commit'ten derlenmis CLI ile yeniden
uretildi ve Asama 3'te yazilmis olan sayilarla birebir ayni cikti (231 / 559 / 878).
Yani karsilastirmanin iki tarafi da ayni kosuda olculdu.

## Ne degisti

| Kural | Duzeltme |
|---|---|
| SV003 | Zincirin alicisinda lambda varsa (`factory.Setup(f => ...).ReturnsAsync(x)`) aday sayilmiyor, `expressionTree` muafiyeti yaziliyor |
| SV004 | `Math` uzerindeki `Max`/`Min`/`Sum` sorgu bitirici sayilmiyor |
| SV005 | `Stream` ekiyle tip tahmini kaldirildi, yerine acik tip listesi geldi; ayrica iki adimli `var x = new ...; await using (x...)` kalibi `using` kapsami sayiliyor |

**Sayfadaki plandan sapma:** SV003 icin "lambdanin icindeki cagrilar muaf olsun"
yazilmisti. Kural zaten lambdanin icine bakmiyordu; isaretledigi cagri zincirin
sonundaki `ReturnsAsync`, yani lambdanin disinda. Muafiyet bu yuzden aliciya bakacak
sekilde yazildi. Ayrica plandaki `Directory.EnumerateFileSystemEntries` maddesi
uygulanmadi, cunku o ad SV004'un bitirici listesinde zaten yok.

## Kural bazinda bulgu sayisi

| Kural | Polly once | Polly sonra | ShareX once | ShareX sonra | Jellyfin once | Jellyfin sonra |
|---|---|---|---|---|---|---|
| SV001 | 0 | 0 | 8 | 8 | 11 | 11 |
| SV002 | 22 | 22 | 35 | 35 | 23 | 23 |
| SV003 | 19 | 19 | 57 | 57 | 26 | **0** |
| SV004 | 5 | **3** | 304 | **109** | 277 | **232** |
| SV005 | 7 | 7 | 59 | **58** | 120 | **27** |
| SV006 | 178 | 178 | 96 | 96 | 421 | 421 |
| **Toplam** | **231** | **229** | **559** | **363** | **878** | **714** |

## Bin satir basina yogunluk

Satir sayilari Asama 3'teki ile ayni (`scan --json` ciktisindaki `totalLineCount`
toplami); repolar degismedi.

| Repo | Toplam satir | Alti kural once | Alti kural sonra | Varsayilan kume once | Varsayilan kume sonra |
|---|---|---|---|---|---|
| Polly | 104 388 | 2,21 | 2,19 | 1,96 | 1,94 |
| ShareX | 224 930 | 2,49 | 1,61 | 1,97 | 1,10 |
| Jellyfin | 351 887 | 2,50 | 2,03 | 2,08 | 1,95 |

Varsayilan kumedeki toplamlar: Polly 205 → 203, ShareX 443 → 248, Jellyfin 732 → 687.

## Kural kural ne oldu

**SV003.** Jellyfin'de 26 bulgunun 26'si da dustu ve hepsi `expressionTree` muafiyeti
olarak kaydedildi, yani sessizce kaybolmadilar; JSON ciktisinda sayilabiliyorlar.
Polly ve ShareX'te **hicbir sey degismedi** (19 ve 57 bulgu aynen duruyor). Duzeltme
bir Moq kalibini eliyor ve o kalip sadece Jellyfin'in test projelerinde yogun.
Asama 3'te bakilan bes bulgunun besi de Jellyfin'den geldigi icin, o orneklem
SV003'un tamamini degil yalnizca bu kalibi temsil ediyormus. Polly ve ShareX'teki
76 bulgunun ne oldugu hakkinda hicbir sey soyleyemem, onlara bakilmadi.

**SV004.** Beklenenden cok daha buyuk bir etki: ShareX'te 304 bulgunun 195'i `Math`
uzerindeki `Max`/`Min` cagrilariymis. ShareX bir goruntu isleme uygulamasi, dongu
icinde `Math.Max` yazmak orada siradan bir sey. Jellyfin'de 45, Polly'de 2 bulgu
dustu. Geriye kalan yanlis pozitifler duruyor: orneklemdeki dort yanlis pozitifin
ikisi bellekteki koleksiyon uzerinde calisan LINQ idi (`allItems.Any(...)`), onlari
ad'a bakarak sorgudan ayirmak mumkun degil. O kisim semantic model isi.

**SV005.** Jellyfin'de 120 bulgu 27'ye indi. Muafiyet dokumunde `usingScope` 104'ten
108'e cikti (iki adimli `await using` kalibi artik taniniyor) ve `callerOwns` 23'ten
15'e dustu (dusenler `MediaStream` donduren metotlardi, artik o tip disposable
sayilmiyor). ShareX'te bir, Polly'de sifir bulgu dustu; ikisinde de `Stream` ekiyle
yakalanan tipler zaten gercek stream'lermis.

## Ne olculmedi

- **Precision yeniden olculmedi.** Asama 3'teki 30 bulguluk orneklem yeniden
  siniflandirilmadi, yeni bir orneklem de cekilmedi. Asagi inen sayilarin hepsinin
  yanlis pozitif oldugu **gosterilmedi**; sadece dusen bulgularin hangi kaliplara
  denk geldigi biliniyor.
- Dogru pozitif kaybi olup olmadigina bakilmadi. Ozellikle SV005'te `Stream` ekiyle
  yakalanan gercek bir disposable tip artik listede olmadigi icin kaciyor olabilir;
  uc repoda bu fark 1 bulgu, ama baska bir repoda daha buyuk olabilir.
- SV003'un Polly ve ShareX'teki 76 bulgusu hic incelenmedi.
