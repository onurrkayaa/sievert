# SZZ etiketlemesi: sayilar

> **Bu dosyadaki etiket sayilari hatali koddan geliyor.** Adim 5'te blame satir
> numaralarinda bir satirlik kayma bulundu ve duzeltildi; Polly'nin etiket sayisi
> 272'den 261'e dustu. Sayilari silmiyorum, neyin neden degistigi izlenebilsin diye
> duruyorlar. Guncel sayilar `asama4-uc-repo.md` dosyasinda.

**Tarih:** 2026-09-11
**Repo:** App-vNext/Polly, tam klon, `2247db24`. 2759 commit.
**Olcum programi:** `tools/Sievert.Measure`, `measure szz polly-full`
**Komut:** `sievert label polly-full --out ...`

## Sonuc

| Olcu | Deger |
|---|---|
| Commit | 2759 |
| Duzeltme commit'i (`IsFix`) | 292 (%10,6) |
| Islenen duzeltme | 288 |
| Dosya sayisi sinirini asip atlanan | 4 |
| **Etiketlenen commit** | **272** |
| **Etiket orani** | **%9,9** |
| Atilan etiket: zaman tutarsizligi | 5 |
| Atilan etiket: birlestirme commit'i | 11 |
| Blame'e sorulan satir | 5102 |

## Etiket orani beklenen bandin ALTINDA cikti

Olcumden once ilan edilen beklenti: literaturde %10-30. Cikan sayi **%9,9**, yani bandin
hemen altinda. Sayiyi kabul etmeden once sebebini aradim; huni soyle:

| Adim | Commit | Kayip |
|---|---|---|
| Duzeltme commit'i | 292 | |
| Ebeveyni olan ve dosya sinirini asmayan | 288 | -4 |
| **Icinde degisen `.cs` dosyasi olan** | **200** | **-88** |
| Silinen `.cs` satiri olan | 180 | -20 |
| En az bir commit'i suclayabilen | 176 | -4 |

Uc sebep var ve hicbiri "SZZ bozuk" demiyor:

1. **En buyuk kayip `.cs` dokunmayan duzeltmeler: 88 commit, yani duzeltmelerin %31'i.**
   Polly'de `Fix build`, `Fix workflow`, `fix typo in readme` gibi commit'ler kaynak koda
   hic dokunmuyor. SZZ sadece `.cs` bakiyor (ilan edilmis karar), o yuzden bunlar hicbir
   sey etiketlemiyor.
2. **Etiket orani `IsFix` oranina bagli.** `IsFix` %10,6 ve her duzeltme ortalama
   1,5 yeni commit sucluyor; ust sinir zaten bu civarda. Literaturdeki %10-30 bandi
   genelde duzeltmelerin hata takip sisteminden bulundugu calismalardan geliyor, orada
   duzeltme orani daha yuksek.
3. **Suclamalar ust uste biniyor.** 5102 satir blame edildi ama 272 farkli commit cikti;
   ayni commit'ler birden fazla duzeltme tarafindan suclaniyor.

Tanimi degistirip sayiyi bandin icine cekmedim. `IsFix`'in kok eslesmesine acilmasi
(`asama4-isfix-recall.md`) duzeltme sayisini 292'den 330'a cikarirdi, yani etiket orani
belki %11-12'ye gelirdi; hâlâ bandin alt ucunda. Karar Asama 5'e kaldi.

## Bosluk duyarliligi: olculdu

Bu adimin en buyuk riski, bicimlendirme ve girinti degisikliklerinin masum commit'leri
suclamasiydi. Iki bicimde calistirilip karsilastirildi (blame ikisinde de ayni, fark
sadece hangi silinen satirlarin blame'e sorulmasinda):

| | Blame'e sorulan satir | Etiket |
|---|---|---|
| Bosluk yok sayilarak (varsayilan) | 5102 | **272** |
| Bosluk sayilarak | 6384 | 286 |

- Bosluk degisiklikleri sayilinca **1282 satir daha** (+%25) blame'e gidiyor.
- Bunlar **14 fazla etiket** uretiyor (+%5,1).
- Ters yonde kayip **yok**: bosluk yok sayilinca kaybolan etiket 0.

Yani bosluk duyarsizligi sadece eliyor, hicbir gercek etiketi goturmuyor. Davranis
bosluk yok sayacak sekilde birakildi.

Bu filtre blame'in kendi ayari degil: LibGit2Sharp'in `BlameOptions` sinifinda
bosluk secenegi yok. Onun yerine blame'e **hangi satirlarin sorulacagi** suzuluyor -
tamamen bos satirlar ve kirpilmis hâli ayni hunk'ta eklenen satirlarda bulunanlar
atlaniyor. Bu, `git blame -w` ile ayni sey degil ama ayni sorunu hedefliyor.

## Ad degisimi: blame takip etmiyor

LibGit2Sharp'in `BlameStrategy` enum'unda tek deger var: `Default`. Yani kopya ve ad
degisimi takibi (`TrackCopiesSameFile` vb.) kutuphanede tanimli ama kullanilamiyor -
blame bir dosyanin gecmisini adi degistigi noktada birakiyor.

Maruziyet olculdu: duzeltme commit'lerinin dokundugu **625 farkli `.cs` dosyasindan
407'si** (%65) tarihinin bir yerinde ad degistirmis. Bu dosyalarda blame, ad degisiminden
onceki yazari goremiyor; suclama ad degisimini yapan commit'te duruyor.

Kac etiketin bu yuzden yanlis oldugu **olculmedi** - bunun icin her etiketin elle
dogrulanmasi gerekir. Davranis degistirilmedi.

## Diger sayilar

- **Blame hic basarisiz olmadi** (0 dosya bulunamadi).
- 30 satir hicbir blame hunk'ina dusmedi; bunlar dosyanin ebeveyn surumunde olmayan
  satirlar.
- 22 suclama birlestirme commit'ine dustugu icin, 10 tanesi zaman tutarsizligi yuzunden
  atildi (olcum programindaki sayim; urun ciktisindaki 11 ve 5, yalnizca varsayilan
  varyanti sayiyor).

## Sure ve bellek

Olcumden once yazilan beklenti: 60-180 sn, 300-450 MB.

| Kosu | Sure | Tepe bellek |
|---|---|---|
| Adim 1: git okuma + JSONL | 3,95 sn | 244 MB |
| Adim 2: git okuma + PostgreSQL | 5,44 sn | 323 MB |
| Adim 3: veritabanindan metrik | 0,61 sn | 169 MB |
| **Adim 4: SZZ etiketleme** | **125,04 sn** | **178 MB** |

Sure tahmini tuttu (125 sn, 60-180 araliginda). Bellek tahmini **tutmadi**: 300-450 MB
bekliyordum, 178 MB cikti. Blame'in hunk koleksiyonu dosya bitince birakiliyor ve ayni
anda tek dosyanin blame'i bellekte duruyor; tahminimde bunlarin birikecegini varsaymisim.

Sure onceki adimlarin 20-30 katina cikti ve sebebi tek bir sey: blame dosya basina
butun tarihi geriye dogru yuruyor. 5102 satir icin degil, o satirlarin gectigi her dosya
icin bir kez.
