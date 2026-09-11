# SV001-SV006 precision olcumu

**Tarih:** 2026-09-11
**Repo:** [jellyfin/jellyfin](https://github.com/jellyfin/jellyfin) @ `1d7b6d97844c8cc848ed3fb5c4b48bb9cdd5b139`
**Orneklem:** 30 bulgu, her kuraldan 5 ([asama3-dogrulama-listesi.md](asama3-dogrulama-listesi.md))

30 bulgu kaynak koda bakilarak yazar tarafindan siniflandirildi; bagimsiz bir
degerlendirici kullanilmadi. Her satirin kod baglami
[asama3-dogrulama-baglami.md](asama3-dogrulama-baglami.md) dosyasinda.

## Kural bazinda

| Kural | Seviye | E (gercek) | H (yanlis pozitif) | Belirsiz | Precision | Havuzdaki toplam bulgu |
|---|---|---|---|---|---|---|
| SV001 | error | 5 | 0 | 0 | **5/5 = %100** | 11 |
| SV002 | warning | 5 | 0 | 0 | **5/5 = %100** | 23 |
| SV003 | warning | 0 | 5 | 0 | **0/5 = %0** | 26 |
| SV004 | warning | 1 | 4 | 0 | **1/5 = %20** | 277 |
| SV005 | warning | 0 | 5 | 0 | **0/5 = %0** | 120 |
| SV006 | info | 4 | 1 | 0 | **4/5 = %80** | 421 |
| **Alti kural birlikte** | | **15** | **15** | **0** | **15/30 = %50** | 878 |

Belirsiz satir kalmadi. Ilk incelemede iki satir belirsizdi (`MediaEncoder.cs:546` ve
`StreamExtensions.cs:96`); kaynak acilip bakilinca ikisinde de nesnenin bir sonraki
satirda `await using` ile atildigi goruldu ve ikisi de H olarak yazildi.

## Asama 2 ile ayni tabloda ama ayri satir

| Olcum | Kural | Repo | Orneklem | Precision |
|---|---|---|---|---|
| Asama 2 | sadece SV001 | ShareX | 20 bulgu (10 bulgu + 10 muafiyet) | %80 |
| Asama 3 | alti kural | Jellyfin | 30 bulgu | %50 |

**Bu iki sayi dogrudan karsilastirilamaz.** Farkli kural kumesi, farkli repo, farkli
orneklem buyuklugu ve farkli orneklem tanimi. Asama 2'nin %80'i tek bir kuralin bir
repodaki halini olcuyordu ve o orneklemin yarisi muafiyetlerden olusuyordu. Asama 3'un
%50'si alti kuralin ortalamasi ve sadece bulgulardan olusuyor. "Precision %80'den %50'ye
dustu" demek yanlis olur; iki olcum ayni seyi olcmuyor.

Asama 2'nin SV001'i ayni repoda bugun ne verir, olculmedi. SV001'in Asama 3 orneklemindeki
degeri %100 ama bu Jellyfin'de, ShareX'te degil.

## ADR 0010'daki esik

ADR 0010'da, olcum yapilmadan once su esik yazilmisti:

> SV004 icin bir esik koyuyorum: gercek bir repoda ornekleme bakildiginda precision
> %50'nin altina duserse, kuralin ad temelli hali kullanilamaz demektir.

Esik SV004 icin ilan edilmisti ve olcumden sonra degistirilmedi. Olcum sonucunda esigin
altinda kalan kurallar:

| Kural | Precision | Esigin altinda mi |
|---|---|---|
| SV003 | %0 | **evet** |
| SV004 | %20 | **evet** (esik bu kural icin ilan edilmisti) |
| SV005 | %0 | **evet** |
| SV006 | %80 | hayir |
| SV001 | %100 | hayir |
| SV002 | %100 | hayir |

Uc kural esigin altinda. Bunlarin ikisi (SV003, SV005) varsayilan kural kumesinden
cikarildi; SV004 acik birakildi, cunku yanlis pozitiflerinin tek ve duzeltilebilir bir
kok nedeni var. Ayrintisi README'de ve ADR 0010'un "Olculen sonuc" bolumunde.

## Orneklem buyuklugunun sinirlari

Kural basina 5 bulgu az. Bir kuralin precision'i icin 5 satirlik bir orneklem, gercek
oranin etrafinda genis bir aralik birakiyor: 5/5 ve 0/5 gibi uc degerler, gercek oranin
%100 ya da %0 oldugunu degil, o yone dogru guclu bir isaret oldugunu soyluyor. SV004 icin
bu ozellikle onemli, cunku havuzda 277 bulgu var ve bunlarin 5'ine bakildi.

Havuz buyukluklerine gore bakildiginda orneklemin havuza orani: SV001 %45, SV002 %22,
SV003 %19, SV004 %1,8, SV005 %4,2, SV006 %1,2. Yani SV001 hakkindaki sayi, SV006
hakkindaki sayidan cok daha saglam.
