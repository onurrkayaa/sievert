# SV001'in ShareX uzerindeki olcumu

**Tarih:** 2026-09-11
**Repo:** [ShareX/ShareX](https://github.com/ShareX/ShareX)
**ShareX commit:** `b5a397ea6ccf00659cee593c981be4b01ab641fe` (2026-09-10)
**Sievert commit:** `2963802` (yeniden yazma oncesi: `3a81e84`)
**Komut:** `sievert check <sharex>` ve `sievert check <sharex> --json` (Release derlemesi)

Polly taramasi hic bulgu vermemisti, cunku Polly bir kutuphane ve `async void`
esas olarak UI tarafinda cikan bir kalip. Bu yuzden ikinci orneklem olarak
masaustu uygulamasi sectim. ShareX bir ekran goruntusu araci, WinForms ve
Avalonia karisik, yani event handler dolu.

Repoyu Sievert'in disinda gecici bir klasore shallow olarak klonladim.

## Sonuc

| Olcum | Deger |
|---|---|
| Taranan dosya | 1138 |
| Tip | 1860 |
| Metot | 7376 |
| Async metot | 577 (%7,8) |
| **Toplam `async void` metot** | **106** |
| **SV001 bulgusu** | **10** |
| **Event handler istisnasiyla muaf tutulan** | **96** |
| Muaf tutulma orani | %90,6 |
| Cikis kodu | 1 |
| Tarama suresi | 1,93 - 2,02 sn (3 kosu) |

Bu olcumun asil sayisi 96. Reponun icindeki `async void` metotlarin onda
dokuzu event handler istisnasina takilip eleniyor. Kural ham haliyle
calissaydi 106 bulgu cikacakti; istisna bunu 10'a indiriyor.

### Muaf tutulanlarin nasil sayildigi

check ciktisi sadece muaf tutulmayanlari veriyor, muaf tutulan sayisini
dogrudan yazmiyor. Bu yuzden iki ciktiyi karsilastirdim: `scan --json` her
metodun `isAsync` ve `returnType` alanlarini verdigi icin toplam `async void`
sayisini oradan cikardim (106), `check --json` bulgulari verdi (10), farki
muaf tutulanlar olarak aldim (96). Ayni karsilastirmayi dosya + satir + metot
adi uclusu uzerinden yaptim, yani hangi metodun muaf tutuldugu tek tek belli.

## Uretim / test dagilimi

| | Uretim | Test | Toplam |
|---|---|---|---|
| Dosya | 1138 | 0 | 1138 |
| Metot | 7376 | 0 | 7376 |
| Async metot | 577 | 0 | 577 |
| `async void` | 106 | 0 | 106 |
| SV001 bulgusu | 10 | 0 | 10 |
| Muaf tutulan | 96 | 0 | 96 |

Test sutunu bastan asagi sifir. Bunun sebebi heuristigin yanilmasi degil:
ShareX'te test projesi yok. `test`/`tests` klasoru ya da `*Tests.cs` dosyasi
aradim, hicbiri cikmadi. Reponun kokunde `ShareX.sln` ve `ShareX.ImageEditor.sln`
var, ikisinde de test projesi bulunmuyor. Yani bu repoda uretim/test ayrimi
bilgi tasimiyor.

Polly'de tam tersiydi: 991 async metodun 846'si test kodundaydi. Iki reponun
bu kadar farkli olmasi, uretim/test ayrimini risk skoruna sokarken dikkatli
olmam gerektigini gosteriyor.

## Polly ile yan yana

Ayni kural, ayni Sievert surumu, iki farkli repo.

| Olcum | Polly `2247db24` | ShareX `b5a397ea` |
|---|---|---|
| Repo turu | kutuphane | masaustu uygulamasi |
| Dosya | 797 | 1138 |
| Metot | 4655 | 7376 |
| Async metot | 991 (%21,3) | 577 (%7,8) |
| `async void` metot | 0 | 106 |
| SV001 bulgusu | **0** | **10** |
| Muaf tutulan | 0 | 96 |
| Uretim / test dosya | 479 / 318 | 1138 / 0 |
| Cikis kodu | 0 | 1 |
| Sure | 0,95 - 1,00 sn | 1,93 - 2,02 sn |

Polly'nin sifiri duruyor, silmedim; [asama2-sv001-polly.md](asama2-sv001-polly.md)
dosyasinda detayi ve sifirin uc ayri yolla nasil dogrulandigi var.

Iki repo yan yana konunca su gorunuyor: async oranı yuksek olan repo (Polly,
%21,3) hic `async void` icermiyor, async orani dusuk olan repo (ShareX, %7,8)
106 tane iceriyor. Yani `async void` sikligi reponun ne kadar async oldugundan
degil, ne isi yaptigindan geliyor. Tek repoda olcum almak bu kural hakkinda
yaniltici bir izlenim veriyordu.

## Diger sayilar

| Olcum | Deger |
|---|---|
| Ayristirilamayan dosya | 0 |
| Hic tip bulunamayan dosya | 4 |
| Kosullu derleme iceren dosya | 12 |
| Kor noktada kalan satir | 62 (%0,03) |

Polly'de `cake.cs` dosya tabanli program oldugu icin ayristirilamiyordu;
ShareX'te oyle bir dosya yok, 1138 dosyanin hepsi sorunsuz ayristi.

## Sure

| Repo | Dosya | Sure (3 kosu) |
|---|---|---|
| Polly | 797 | 0,95 / 1,00 / 0,97 sn |
| ShareX | 1138 | 2,02 / 2,02 / 1,93 sn |

Dosya sayisi %43 artarken sure iki katina cikti. Dogrusal degil; ShareX
dosyalari daha buyuk (7376 metot, Polly'de 4655) ve `.axaml.cs` dosyalari
uzun. Yine de 1138 dosya icin 2 saniye kabul edilebilir. Daha buyuk repolarda
tekrar olcmek lazim.

## Elle dogrulama

Bulgularin ve muaf tutulanlarin bir orneklemi
[asama2-dogrulama-listesi.md](asama2-dogrulama-listesi.md) dosyasinda, elle
incelenmek uzere hazir bekliyor. Hangisinin gercek sorun oldugu, hangisinin
yanlis pozitif ya da yanlis negatif oldugu henuz belirlenmedi; o liste
doldurulmadan SV001'in dogrulugu hakkinda bir sey soyleyemem.
