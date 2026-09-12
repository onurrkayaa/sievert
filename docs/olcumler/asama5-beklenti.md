# Asama 5 beklentileri

**Beklentiler olculmeden once yazildi.**

**Tarih:** 2026-09-12
**Durum:** Asama 5 Adim 0. Veri anlik goruntusu alinmadan, bolme yapilmadan, hicbir taban
cizgisi ya da model olculmeden yazildi. Elde yalnizca Asama 4'un kapanmis sayilari var.

Bu dosya sonradan degistirilmeyecek. Tutmayan beklentiler kapanista silinmeyecek, oldugu
gibi korunup yanina olculen deger yazilacak. Sebebi B033'teki ile ayni: sonucu gordukten
sonra ayarlanan bir beklenti, beklenti olmaktan cikip sonucun tekrari olur.

## Elde ne var

Asama 4'ten gelen, bu dosyanin dayandigi tek sayilar:

| Repo | Commit | Pozitif etiket | Oran |
|---|---|---|---|
| Polly | 2759 | 261 | %9,5 |
| ShareX | 8490 | 1013 | %11,9 |
| Jellyfin | 22 917 | 4688 | %20,5 |
| **Toplam** | **34 166** | **5962** | **%17,4** |

Bunlar butun tarihin oranlari. Asagidaki beklentilerin cogu **test bolumu** hakkinda,
yani her reponun kendi icinde son %30'u hakkinda; ikisi ayni sey degil.

## 1. Test bolumundeki pozitif sinif orani

Her repo kendi icinde zaman sirasina dizilip ilk %70 egitim, son %30 test olacak
(`asama5-sizinti-kontrol.md`). Test bolumu deponun **en yeni** commit'leri.

| Repo | Butun tarihte olculen | Test bolumu icin beklentim |
|---|---|---|
| Polly | %9,5 | **%3-9** |
| ShareX | %11,9 | **%4-11** |
| Jellyfin | %20,5 | **%8-19** |

Gerekce: bir commit ancak kendisinden **sonra gelen** bir duzeltme onu suclarsa pozitif
etiket aliyor, en yeni commit'lerin ise arkasinda daha az tarih kaldigi icin onlari
suclayacak duzeltme henuz yazilmamis olabilir; bu yuzden uc bandin da butun tarihin
oraninin altinda kalmasini bekliyorum.

Uc repoyu birlestiren tek bir test bolumu kurulmayacak, o yuzden birlesik oran icin
beklenti yazmiyorum.

## 2. Her seye negatif diyen taban

Bu taban hicbir commit'e pozitif demiyor.

- **Recall:** tam olarak **0**; pay 0, payda test bolumundeki pozitif commit sayisi.
- **Precision:** tanimsiz; pay 0, payda 0.
- **F1:** precision tanimsiz oldugu icin tanimsiz.
- **Accuracy:** 1 eksi test bolumunun pozitif orani, yani 1. maddedeki bantlardan
  cikacak; Jellyfin'de %81-92, Polly'de %91-97 arasi.

Gerekce: bu tabanin sayilari veriden degil taniminin kendisinden cikiyor, o yuzden burada
bant degil kesin deger yaziyorum.

Bu tabani olcmenin amaci da bu: accuracy'nin tek basina hicbir sey soylemedigini gostermek.
Hicbir sey yapmayan bir yontem Jellyfin'de %81-92 accuracy aliyor.

## 3. Pozitif tahmin yapmayan bir yontemde precision nasil raporlanacak

Pay da payda da 0 oldugunda precision **N/A** yazilacak; tercih edilen budur.

Kullanilan bir kutuphane ya da formul bu durumu sifira zorluyorsa, o zaman **hem** N/A
oldugu **hem de** hesabin 0 urettigi ayri ayri yazilacak; 0 tek basina yazilmayacak.
Gerekce: "precision 0" ifadesi "yaptigi pozitif tahminlerin hepsi yanlis" demektir, oysa
burada hic pozitif tahmin yok ve ikisi farkli seylerdir.

Ayni kural F1 icin de gecerli: precision N/A ise F1 de N/A.

## 4. Rastgele tahmin tabani

Taban, etiketten bagimsiz olarak commit'lere **p** olasilikla pozitif diyecek; p olarak
egitim bolumunun pozitif orani kullanilacak.

Beklenen degerler (q = tahmin olasiligi, p = gercek pozitif orani):

- **Precision:** yaklasik **p**, yani test bolumunun pozitif orani; 1. maddedeki bantlar.
- **Recall:** yaklasik **q**, yani egitim bolumunun pozitif orani; yine ayni buyukluk
  mertebesinde.
- **F1:** p ve q birbirine yakin oldugu icin yaklasik **p**. Bant olarak: Polly **%3-9**,
  ShareX **%4-11**, Jellyfin **%8-19**.

Gerekce: tahmin etiketten bagimsiz oldugu icin pozitif dedigi commit'lerin pozitif cikma
orani veri kumesinin taban orani kadar olur, yani precision tabana esitlenir.

Orneklem kucuk oldugu icin olculen degerin bu bantlarin biraz disina tasmasi sasirtici
olmaz; tek kosuda rastgeleligin kendisi de bir hata kaynagi.

## 5. Tek oznitelikli LinesAdded esigi

Tek kural: `LinesAdded > esik` ise pozitif. Esik egitim bolumunden secilecek.

- **Ayni repo F1 beklentim: %15-32.**

Gerekce: buyuk commit'lerin daha cok hata getirdigi literaturde tekrarlanan bir bulgu ve
bu tabanin rastgele tabanin uzerine cikmasini bekliyorum, ama tek bir sayi commit'in ne
yaptigini bilmedigi icin bandin ust ucunu dusuk tutuyorum.

Bir uyari: `LinesAdded` uc degerleri cok buyuk (ShareX'te maks 165 538) ve medyan cok
kucuk (Polly 5, ShareX 21, Jellyfin 6), yani esik secimi dagilimin cok dar bir yerinde
oynayacak.

## 6. Lojistik regresyon, ayni repo icinde

15 oznitelikle, ayni reponun egitim bolumunde egitilip kendi test bolumunde olculecek.

- **F1 beklentim: %20-45.**
- LinesAdded esiginin **uzerinde** olmasini bekliyorum ama farkin buyuk olmamasini.

Gerekce: oznitelik kumesinin cogu boyut ve gecmis olculeri ve bunlar birbiriyle iliskili,
yani 15 oznitelik 15 bagimsiz bilgi tasimiyor.

Bandi genis tutmamin sebebi uc reponun taban oranlarinin cok farkli olmasi; Jellyfin'in
%20,5'lik tabaninda ayni model Polly'nin %9,5'lik tabanindakinden yuksek F1 uretir, cunku
F1 taban oranina duyarli.

## 7. Repo-arasi F1 hangi yone gidecek

Iki repoda egitip ucuncude olcmek.

- **Beklenti: ayni-repo F1'inin ALTINDA.** Yon konusunda eminim, buyuklugu konusunda
  degilim; kabaca ayni-repo F1'inin yarisi ile dortte ucu arasina dusmesini bekliyorum.

Gerekce: ayni olcu uc repoda ayni seyi olcmuyor - `AuthorCommitCount` medyani ShareX'te
2483, Polly'de 125, yani bir repoda "deneyimli yazar" sayilan deger digerinde en ustteki
%5'e giriyor.

Taban oranlari da farkli (%9,5 / %11,9 / %20,5), yani bir repoda ogrenilen esikler
digerinde yanlis yerde duracak.

## 8. En guclu olmasi beklenen uc oznitelik

Sirasiyla:

1. **CsFilesChanged**
2. **LinesAdded**
3. **PriorFixes**

Gerekceler, her biri tek cumle:

- `CsFilesChanged`: etiket yalnizca silinen **`.cs`** satirlarini blame ederek uretildigi
  icin hic `.cs` dosyasina dokunmayan bir commit pozitif etiket alamaz, yani bu oznitelik
  etiketin uretim bicimiyle dogrudan bagli.
- `LinesAdded`: commit ne kadar cok satir yazdiysa sonradan suclanacak bir satir icermesi
  o kadar olasi.
- `PriorFixes`: daha once cok duzeltilmis dosyalara dokunan commit'in yine duzeltilecek
  bir yere dokunmasi daha olasi.

`CsFilesChanged`'i basa koymamin bir sebebi de bunun bir uyari olmasi: bu oznitelik
guclu cikarsa model "hata getiren commit"i degil, kismen "etiketlenebilir commit"i
ogrenmis olur. Bu ayrimi Adim 4'te ayrica yazacagim.

## 9. Etiket gurultusunun metriklere beklenen yonu

Asama 4'un elle dogrulamasi iki sey soyledi:

- Etiketli orneklemde dogru suclama **12 / 12**.
- **Hedefli secilmis** etiketsiz orneklemde kacirma **11 / 13**.
- 5 satir Belirsiz olarak paydadan cikarildi.

Beklentim: gurultunun agirligi **negatif sinifta** ve yonu **olculen precision'i asagi
cekmek**. Sebep: gercekte hata getiren ama etiketlenmemis bir commit'e model pozitif
derse bu FP olarak sayilir, oysa model hakli olabilir.

Recall'un yonu icin beklenti yazmiyorum: payda da (gercek pozitifler) eksik oldugu icin
kacan etiketler hem payi hem paydayi ayni anda etkiliyor ve net yon belli degil.

**11 / 13 sayisini butun negatif sinifa genellemiyorum.** O orneklem rastgele degil;
"`IsFix` tarafindan dokunulmus fakat etiketlenmemis" commit'lerden **hedefli** secildi,
yani zaten kacirma ihtimali en yuksek yerden. Bu sayi "hedefli secilmis etiketsiz
orneklemde yuksek kacirma isareti" olarak okunur; negatif sinifin genel hata orani olarak
okunamaz, duzeltme orani olarak uygulanamaz, agirliklandirmada kullanilamaz.

Negatif sinifta etiket gurultusu riski oldugunu soyluyorum; yayginligini bu orneklemden
tahmin etmiyorum. Tahmin etmek isteseydim butun negatiflerden rastgele bir orneklem alip
elle bakmak gerekirdi ve o olcum yapilmadi.

## Bu dosyada beklenti yazmadigim yerler

Acikca yaziyorum ki sonradan "bunu da tahmin etmistim" denmesin:

- PR-AUC icin sayisal bant yazmadim.
- Bot commit'lerinin cikarilmasinin metrikleri hangi yone cekecegi hakkinda beklenti
  yazmadim; bu Adim 4'un duyarlilik deneyi.
- Hangi esigin secilecegi hakkinda beklenti yazmadim, yalnizca secim kuralini yazdim.
