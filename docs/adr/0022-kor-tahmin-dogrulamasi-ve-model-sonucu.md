# 0022 - Kor tahmin dogrulamasi ve Asama 5'in model sonucu

**Baglam:** Asama 5'in butun metrikleri SZZ etiketine karsi olculdu. Ama SZZ'nin kendisi
bir heuristik: yalnizca silinen `.cs` satirlarini blame ediyor, `IsFix` cekimli halleri
kaciriyor ve hicbir hata takip sistemine bagli degil (ADR 0014). Yani "model F1 0,4408"
cumlesi aslinda "model, SZZ'nin urettigi etiketle 0,4408 uyumlu" demek.

Bu ADR iki seyi karara bagliyor: modelin tahminleri bir insana **kor** olarak nasil
sorulur, ve Asama 5'in model sonucu hangi cumleyle yazilir.

**Karar:** Otuz commitlik kor orneklem, onceden ilan edilmis dort kategori, tahmin
sinifina esit dagitim, ve "isaret" dilinde raporlama.

## Neden kor degerlendirme

Metrikler bir etikete karsi olculuyor ve o etiketin kendisi olculmemis. Elde iki secenek
vardi: etiketi baska bir otomatik yontemle karsilastirmak ya da bir insana sormak.
Ikincisi secildi cunku sorulan sey tam olarak "bu commit gercekten bir kusur getirdi mi"
ve bunun otomatik bir cevabi yok - zaten olsaydi SZZ'ye gerek kalmazdi.

Degerlendirme **kor**: degerlendirici modelin tahminini, modelin olasiligini ve SZZ
etiketini gormedi. Malzeme dosyasinda o alanlarin **adlari bile gecmiyor**, cunku bagimsiz
sizinti kontrolu dosyada o adlari ariyor ve uyari metninde gecmeleri kontrolu
bulaniklastirirdi.

## Neden model tahmini ve SZZ etiketi gizlendi

Ikisi de karari yonlendirirdi. "Model bu commit'e %80 diyor" bilgisini goren bir
degerlendirici, diff'te kusur aramaya baslar ve buldugunu sanir; "SZZ bunu pozitif
etiketlemis" bilgisi ayni seyi yapar.

Gizlemenin bedeli var ve yaziliyor: **korluk yontemsel, teknik degil.** Anahtar dosyasi
(`prediction-validation-key.csv`) repoda duruyordu ve degerlendirici onu acmamayi kabul
etti. Tek kisilik bir projede daha guclu bir garanti kurulamazdi.

## Neden her repo ve tahmin sinifina esit ornek

Uc repodan 10'ar, her repoda 5 model-pozitif + 5 model-negatif.

Populasyon oranli rastgele bir orneklem alinsaydi 30 commit'in yaklasik 26'si
model-negatif olurdu (test kumesinde 2133 pozitif tahmine karsi 8118 negatif) ve
model-pozitif tarafi 4 ornekle temsil edilirdi. O zaman "modelin pozitif dedikleri
gercekten kusurlu mu" sorusu cevaplanamazdi.

Esit dagitimin bedeli dogrudan: **bu orneklem populasyonu temsil etmiyor.** O yuzden
asagidaki hesaplama yasagi var.

## Neden `CsFilesChanged > 0` uygunluk kosulu

Degerlendirici kaynak kod degisikligine bakacak. Hic `.cs` dosyasi degistirmeyen bir
commit'te bakilacak C# diff'i yok.

Bu tek uygunluk kosulu: boyut, churn, dosya sayisi filtresi uygulanmadi, botlar
dislanmadi, SZZ etiketine ya da karisiklik matrisi hucresine gore secim yapilmadi.

Bedeli: orneklem, veri kumesinin `CsFilesChanged = 0` olan %25,2'sini (8607 commit)
kapsamiyor. O commit'ler zaten yapisal olarak pozitif etiket alamiyor (ADR 0020), yani
kapsam disi kalmalari ayri bir bilgi kaybi degil - ama sonuc yalnizca `.cs` degistiren
commit'ler icin okunmali.

## Neden populasyon accuracy / precision / recall hesaplanmadi

Orneklem uc repoya esit, tahmin siniflarina esit dagitildi ve yalnizca `.cs` degistiren
commit'lerden secildi. Bu uc secim de populasyon oranlarini bozuyor.

Bu orneklemden populasyon precision'i hesaplamak, tabakalari geri agirliklandirmayi
gerektirirdi ve o agirliklandirma icin gereken sey zaten bilinmiyor (gercek kusur orani).
O yuzden su sayilarin hicbiri hesaplanmadi: populasyon accuracy, populasyon precision,
populasyon recall, genel hata orani, duzeltilmis F1, duzeltilmis etiket sayisi, yeni bir
model metrigi.

## "Precision isareti" ve "kacirma isareti"

Iki oran hesaplandi ve ikisi de **isaret** diye adlandirildi:

- **Insan dogrulama precision isareti** = model-pozitif orneklerde
  `KUSUR-GETIRDI / (KUSUR-GETIRDI + KUSUR-GETIRMEDI)`. Olculen: **1 / 14 = 0,0714**.
- **Insan dogrulama kacirma isareti** = model-negatif orneklerde ayni hesap.
  Olculen: **1 / 11 = 0,0909**.

"Isaret" kelimesi kasitli. Bunlar modelin precision'i ve recall'i **degil**; ayni hesap
ama farkli bir kumeden geliyorlar ve populasyona genellenmiyorlar. Iki sayi tek bir sayida
**birlestirilmiyor**, cunku birlestirmek esit dagitilmis iki tabakayi populasyon
oranindaymis gibi toplamak olurdu.

## VERI-YETMEDI ile BAKILMADI neden ayri

Ikisi de paydadan cikiyor ama ayri sayiliyor ve birbirinin yerine yazilmiyor:

- **VERI-YETMEDI** verinin sinirini soyluyor: diff ve sonraki tarih karar vermeye yetmedi.
- **BAKILMADI** degerlendiricinin sinirini soyluyor: satira zaman ayrilmadi.

Bu ayrim Asama 4'un kapanisindaki bir eksiklikten geldi ve Asama 5'in olcut dosyasinda
(`asama5-olcut.md`) zaten karara baglanmisti.

Olculen sonuc bu ayrimin neden gerektigini gosterdi: **bes ornek incelenmedi** ve bunlar
`BAKILMADI` olarak kaydedildi. `VERI-YETMEDI` yazilsaydi, verinin yetersiz oldugu iddia
edilmis olurdu - oysa o commit'lere bakilmadi ve verinin yetip yetmeyecegi **bilinmiyor**.
Kusur karari da uydurulmadi.

Bes `BAKILMADI`'nin hepsi Polly'de ve dordu model-negatif grubunda; o grubun paydasi
11'e, Polly'nin model-negatif paydasi 1'e dustu.

## Tek degerlendirici

Siniflandirmayi tek kisi yapti ve ayni kisi araci yazdi. Bagimsiz ikinci degerlendirici
yok, bu yuzden **Cohen kappa hesaplanmadi** ve degerlendiriciler arasi uyum iddiasi
kurulmadi.

Bu, projenin butun elle siniflandirmalarinda oldugu gibi bir sinirlilik olarak kaliyor ve
`docs/sinirliliklar.md` icinde yaziyor.

## Asama 5'in ana model sonucu

Sonuc tek cumleyle yazilamiyor; uc ayri duzeyde yaziliyor.

1. **Ayni-repo nokta sonucu:** lojistik regresyon, onceden ilan edilmis uc toplu kosulun
   **3 / 3'unu** gecti (mikro F1 0,4408 > 0,3754; makro F1 0,3088 > 0,2999; mikro ham
   PR-AUC 0,4775 > 0,2611).
2. **Belirsizlik sonucu:** mikro delta F1 araligi [0,0485, 0,0823] ve mikro delta PR-AUC
   araligi [0,1818, 0,2513] tabanin ustunde kaldi; **makro delta F1 araligi
   [-0,0296, 0,0563] fark yok degerini iceriyor.**
3. **Repo-arasi sonuc:** alti tek kaynakli aktarimin 3'unde ayni-repo F1'inin ustunde,
   3'unde altinda; leave-one-out uc hedefte de en iyi tek kaynagin altinda.

Bu yuzden sonuc **"her repoda ustun model"** degil, **"hacim agirlikli toplu olcumde
tabani gecen, fakat repo duzeyinde belirsizlik ve aktarim sorunu tasiyan model"** olarak
yaziliyor.

Kor insan dogrulamasi bu cumleyi **ne guclendiriyor ne zayiflatiyor**: model-pozitif
precision isareti 1 / 14 ve SZZ pozitif dogrulama isareti 0 / 6 dusuk, ama 25 karar
ve tahmin sinifina esit dagitilmis bir orneklemden populasyon iddiasi cikmiyor. Tek
soylenebilecek sey **orneklem sinirlari icinde**: incelenen 25 commit'in 23'unde
degerlendirici kusur bulmadi.

## Neden kalibrator secilmedi

Adim 3b'de iki kalibrasyon yontemi de mikro toplamda hem Brier'i hem ECE'yi dusurdu
(Platt 0,0843 → 0,0838 ve 0,0809 → 0,0738; isotonic 0,0833 ve 0,0699). Ama:

- Ikisi de **azaltilmis-egitim** modeline ait; tam-train modelin kalibratoru olarak
  olculmediler.
- Jellyfin'de ikisi de **karisik sonuc** verdi (ECE dustu, Brier yukseldi).
- Test sonucuna bakarak aralarindan kazanan **secilmedi**; ikisi onceden ilan edilmis iki
  ayri deneydi.

O yuzden uretim icin bir kalibrator **secilmedi** ve Asama 5'in ana modeli **kalibre
edilmemis** durumda. Risk olasiligi "kalibre edilmis olasilik" diye sunulamaz.

## Neden mikro ve makro birlikte okunuyor

Mikro toplam buyuk repoyu agirlikliyor: test satirlarinin %67,1'i Jellyfin'den. Makro uc
repoyu esit sayiyor ve Polly'nin 10 test pozitifine asiri duyarli.

Olculen fark kucuk degil ve yon konusunda ayrisiyorlar: mikro F1 farki +0,0654 ve
bootstrap araligi tabanin ustunde; makro F1 farki +0,0089 ve araligi sifiri iceriyor.
Tek birine bakilsaydi sonuc ya fazla iyimser ya fazla karamsar yazilirdi.

O yuzden ikisi her tabloda ayri sutunlarda duruyor ve kapanis cumlesinde ikisi de geciyor.

**Sonuc:** Kor dogrulama 30 commit'lik bir orneklemde yapildi, 25'i karara baglandi ve
sayilar "isaret" dilinde raporlandi. Asama 5'in model sonucu uc duzeyde yazildi;
uretim icin kalibrator secilmedi.
