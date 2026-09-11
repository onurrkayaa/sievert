# SZZ etiketlerini siniflandirma olcutleri

**Tarih:** 2026-09-11
**Durum:** Siniflandirma **baslamadan once** ilan edildi. Bir satira bakildiktan sonra
olcut degistirilmez.

Bu dosya, `asama4-etiketleme-dogrulama.md` icindeki 30 satirin nasil isaretlenecegini
soyluyor. Olcutleri onceden yazmanin sebebi B033'teki ile ayni: esik ya da tanim
olcumden sonra ayarlanirsa, cikan sayi olcum olmaktan cikip tercih olur.

Her satir ucunden biriyle isaretlenir: **E**, **H**, **Belirsiz**.

## Olcutler

| # | Durum | Karar |
|---|---|---|
| 1 | Suclanan satir, duzeltmenin giderdigi hatanin mantigini iceriyor | **E** |
| 2 | Suclanan satirdaki degisiklik sadece bicim, bosluk ya da yeniden adlandirma | **H** |
| 3 | Suclanan satir, duzeltilen yerin komsusu; hatayla mantiksal ilgisi yok | **H** |
| 4 | Duzeltme bir test hatasini gideriyor ve suclanan satir o testi yazan commit'ten geliyor | **E** |
| 5 | Duzeltme aslinda bir hata duzeltmesi degil (yazim yanlisi, dokuman, yeniden duzenleme) | **H** |
| 6 | Suclanan satir bir dosya tasima ya da birlestirme commit'inden geliyor | **Belirsiz** |
| 7 | Diff karar vermeye yetmiyor | **Belirsiz** |

## Olcutleri okurken

**Sira onemli.** Bir satir birden fazla olcuta uyuyorsa yukaridaki sira gecerli: once 1,
sonra 2, ... Ornegin duzeltme bir yazim yanlisini gideriyorsa (5 -> H), suclanan satirin
mantikla ilgisi olup olmadigina bakilmaz.

**"Hatanin mantigini iceriyor" ne demek:** suclanan satir, duzeltmenin degistirdigi
davranisi uretiyor olmali. Kosulun kendisi, yanlis degisken, eksik kontrolun yazildigi
yer. Satirin sadece ayni metotta ya da ayni blokta olmasi yetmez; o durum 3. olcut.

**Belirsiz bir kacis yolu degil.** 6 ve 7 numaralar, kararin veriye dayanmadigi
durumlar icin var. Bir satir hakkinda karar verilebiliyorsa E ya da H yazilir; emin
olunamadigi icin degil, veri yetmedigi icin Belirsiz yazilir.

**Etiketsiz satirlar ters yonden okunur.** Listenin ikinci yarisindaki satirlar SZZ'nin
suclamadigi commit'ler. Orada soru "bu commit suclanmali miydi" olur: suclanmaliysa
**E** (yani SZZ kacirmis), suclanmamaliysa **H** (yani SZZ hakli).

## Sayilar nasil cikacak

Dogruluk orani hesaplanirken **Belirsiz satirlar paydadan cikarilir** ve kac tane
cikarildigi yazilir. Pay ve payda her zaman ayri ayri yazilir; tek basina yuzde
yazilmaz.

Hicbir satirin karari bos kalmamali. Bos karar varsa oran hesaplanmaz, eksik satir
sayisi yazilir.

## Degerlendirici yanliligi

Siniflandirmayi tek kisi yapiyor ve ayni kisi araci da yazdi. Bagimsiz bir degerlendirici
yok. Bu, projenin butun elle siniflandirmalarinda oldugu gibi burada da bir sinirlilik
olarak kalir ve `docs/sinirliliklar.md` icinde yaziyor.
