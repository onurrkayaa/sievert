# Asama 5 elle inceleme olcutleri

**Tarih:** 2026-09-12
**Durum:** Inceleme **baslamadan once** ilan edildi. Bir commit'e bakildiktan sonra olcut
degistirilmez.

Asama 4'un olcut dosyasi (`asama4-etiketleme-olcut.md`) yerinde duruyor ve
degistirilmedi; orada uretilen sayilar o dosyanin kurallariyla uretildi. Bu dosya Asama
5'te yapilacak elle incelemeler icin gecerli ve Asama 4'ten tek yerde ayriliyor:
**Belirsiz ikiye bolundu.**

Sebebi Asama 4 kapanisinda yazmisti: "veri karar vermeye yetmiyor" ile "degerlendirici
karar vermedi" ayni kova degil. Birincisi verinin sinirini soyluyor, ikincisi
degerlendiricinin. Tek kovada durduklarinda ikisi birbirini gizliyor.

## Kategoriler

Her satir dorttunden **tam olarak biriyle** isaretlenir:

| Kod | Anlami |
|---|---|
| **E** | Karar evet. Etiketli satirda "suclama dogru", etiketsiz satirda "suclanmaliydi". |
| **H** | Karar hayir. Etiketli satirda "suclama yanlis", etiketsiz satirda "suclanmamali". |
| **VERI-YETMEDI** | Diff ve baglam karar vermek icin yeterli degil. |
| **BAKILMADI** | Degerlendirici bu satira zaman ayirmadi. |

**Bos karar gecersizdir.** Karar verilemiyorsa VERI-YETMEDI, incelenmediyse BAKILMADI
yazilir. Bos birakilan bir hucre varsa oran hesaplanmaz; eksik satir sayisi yazilir.

Bu iki kod birbirinin yerine kullanilmaz. VERI-YETMEDI, satira bakildigini ve verinin
yetmedigini soyler; BAKILMADI, satira hic bakilmadigini soyler. Zaman yetmedigi icin
atlanan bir satira VERI-YETMEDI yazmak, verinin sinirini oldugundan genis gostermek olur.

## Sayim kurali

- Payda **yalnizca E ve H** satirlarindan olusur.
- VERI-YETMEDI ve BAKILMADI paydadan cikarilir.
- Ikisi **ayri ayri** sayilir ve ayri ayri yazilir; tek bir "cikarilan" sayisinda
  toplanmaz.
- Pay ve payda her zaman ayri yazilir; tek basina yuzde yazilmaz.
- Her tablonun altinda dort sayinin toplami (E + H + VERI-YETMEDI + BAKILMADI) orneklem
  buyuklugune esit olmali; esit degilse oran hesaplanmaz.

Ornek yazim bicimi: "dogru suclama 12 / 12; ayrica 3 VERI-YETMEDI, 0 BAKILMADI, orneklem 15".

BAKILMADI sayisi sifirdan buyukse bu bir sonuc degil, **eksik istir**; oyle yazilir ve
kapanista oldugu gibi kalir.

## Yontem

> Onceden ilan edilmis olcutlerle yazar tarafindan kaynak koda bakilarak siniflandirildi;
> bagimsiz ikinci degerlendirici kullanilmadi.

Bu cumle Asama 5'te elle inceleme iceren her tabloda aynen tekrarlanir.

## Karar olcutleri

Asama 4'teki sira korunuyor; tek fark, eski "Belirsiz" kararlarinin yerine hangi yeni
kodun geldigi.

| # | Durum | Karar |
|---|---|---|
| 1 | Suclanan satir, duzeltmenin giderdigi hatanin mantigini iceriyor | **E** |
| 2 | Suclanan satirdaki degisiklik sadece bicim, bosluk ya da yeniden adlandirma | **H** |
| 3 | Suclanan satir, duzeltilen yerin komsusu; hatayla mantiksal ilgisi yok | **H** |
| 4 | Duzeltme bir test hatasini gideriyor ve suclanan satir o testi yazan commit'ten geliyor | **E** |
| 5 | Duzeltme aslinda bir hata duzeltmesi degil (yazim yanlisi, dokuman, yeniden duzenleme) | **H** |
| 6 | Suclanan satir bir dosya tasima ya da birlestirme commit'inden geliyor | **VERI-YETMEDI** |
| 7 | Diff karar vermeye yetmiyor | **VERI-YETMEDI** |

**Sira onemli.** Bir satir birden fazla olcuta uyuyorsa yukaridaki sira gecerli: once 1,
sonra 2, ... Ornegin duzeltme bir yazim yanlisini gideriyorsa (5 -> H), suclanan satirin
mantikla ilgisi olup olmadigina bakilmaz.

**"Hatanin mantigini iceriyor" ne demek:** suclanan satir, duzeltmenin degistirdigi
davranisi uretiyor olmali. Kosulun kendisi, yanlis degisken, eksik kontrolun yazildigi
yer. Satirin sadece ayni metotta ya da ayni blokta olmasi yetmez; o durum 3. olcut.

**Etiketsiz satirlar ters yonden okunur.** Etiketsiz bir commit icin soru "bu commit
suclanmali miydi" olur: suclanmaliysa **E** (SZZ kacirmis), suclanmamaliysa **H** (SZZ
hakli).

**VERI-YETMEDI bir kacis yolu degil.** Karar verilebiliyorsa E ya da H yazilir. Emin
olunamadigi icin degil, veri yetmedigi icin VERI-YETMEDI yazilir.

## Orneklem nasil secildi yazilir

Her elle inceleme tablosunun basinda orneklemin **nereden** secildigi yazilir: hangi
kumeden, hangi kuralla, rastgele mi hedefli mi, rastgeleyse hangi tohumla.

Bu kural Asama 4'un en onemli dersinden geliyor. Oradaki 11 / 13 sonucu rastgele bir
negatif orneklemden degil, "`IsFix` tarafindan dokunulmus fakat etiketlenmemis"
commit'lerden **hedefli** secilmis bir orneklemden cikti. Orneklemin nasil secildigi
yazilmazsa o sayi butun negatif sinifin hata orani gibi okunur ve oyle okunmasi yanlistir.

Hedefli secilmis bir orneklemden cikan oran, secildigi kumenin disina genellenmez.

## Degerlendirici yanliligi

Siniflandirmayi tek kisi yapiyor ve ayni kisi araci da yazdi. Bagimsiz bir degerlendirici
yok. Bu, projenin butun elle siniflandirmalarinda oldugu gibi burada da bir sinirlilik
olarak kalir ve `docs/sinirliliklar.md` icinde yaziyor.
