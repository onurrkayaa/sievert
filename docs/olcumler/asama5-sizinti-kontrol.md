# Asama 5 sizinti kontrol listesi

**Tarih:** 2026-09-12
**Durum:** Adim 0'da yazildi, **Adim 1'de uygulanacak.** Burada yazan hicbir kontrol
henuz calistirilmadi; bu dosya kontrollerin listesi, sonucu degil.

Sizinti bu projede en pahali hata turu, cunku sessiz: model gelecekten bilgi gorurse
metrikler yuksek cikar, her sey calisiyor gorunur ve yanlis oldugu ancak gercek
kullanimda anlasilir. Asama 4'te ayni sebeple metrik hesabi zaman sirasina baglanmisti
(ADR 0013); burasi ayni disiplinin bolme ve model tarafi.

Her maddenin karsisinda ne zaman **GECTI** sayilacagi yaziyor. Bir madde gecmezse veri
degistirilmez, sonuc raporlanip durulur.

## 1. Zaman sirasi ve bolme

| # | Kontrol | Nasil gecer |
|---|---|---|
| 1.1 | Veri zaman sirasinda siralanacak | Satirlar `AuthorDateUtc` artan sirada; azalan bir cift yok |
| 1.2 | Her repo **kendi icinde** bolunecek: ilk %70 egitim, son %30 test | Uc repo icin de bolme ayri hesaplanir |
| 1.3 | Esit timestamp durumunda `Sha` ile deterministik siralama | Ayni `AuthorDateUtc` tasiyan satirlar `Sha` alfabetik sirada |
| 1.4 | Bolme siniri **hem sira numarasi hem UTC tarih** olarak kaydedilecek | Her repo icin iki deger de `asama5-veri-kumesi.md` icinde yazili |
| 1.5 | Rastgele train/test bolmesi yok | Kodda karistirma cagrisi yok |
| 1.6 | Rastgele capraz dogrulama yok | k-fold yok; kullanilirsa yalnizca zaman sirali (ileri giden) olur |

Bolme neden repo icinde: uc reponun tarih araliklari ortusuyor (Polly 2013-2026, ShareX
2013-2026, Jellyfin 2012-2026). Butun satirlari tek bir havuzda tarihe gore bolmek, bir
reponun gelecegini baska bir reponun gecmisiyle karistirirdi.

**%70/%30 sira numarasina gore.** Tarihe gore degil, cunku commit yogunlugu zaman icinde
degisiyor ve tarihe gore bolme repolar arasinda cok farkli buyuklukte test bolumleri
uretirdi.

## 2. Neyin nereden ogrenilecegi

| # | Kontrol | Nasil gecer |
|---|---|---|
| 2.1 | Normalizasyon yalnizca egitim bolumunden ogrenilecek | Olcekleme parametreleri egitim satirlarindan; teste **uygulanir**, testten **ogrenilmez** |
| 2.2 | Donusumler (log, kirpma) yalnizca egitim bolumunden | Kirpma sinirlari egitim dagilimindan |
| 2.3 | Esik secimi yalnizca egitim bolumunden | `LinesAdded` esigi egitimde secilir, testte yalnizca uygulanir |
| 2.4 | Eksik deger islemleri yalnizca egitim bolumunden | Doldurma degeri (medyan vb.) egitimden |
| 2.5 | Test bolumunun istatistikleri egitim sirasinda kullanilmayacak | Egitim kodu test dilimine hic dokunmaz |

Bunun pratik testi su: test bolumunun satirlarini silip egitimi tekrar calistirinca
egitilen modelin **degismemesi** gerekir.

## 3. Hangi alan modele girer, hangisi girmez

| Alan | Rolu | Modele girer mi |
|---|---|---|
| `IsBugIntroducing` | Hedef degisken | **Hayir**, oznitelik degildir |
| `LabelSource` | Denetim alani | **Kesinlikle hayir** |
| `Sha` | Kimlik | Hayir |
| `Repository` / `RepositoryIdentity` | Kimlik | Ayni-repo deneyinde **hayir** |
| `AuthorDateUtc` | Bolme icin | Varsayilan model ozniteligi **degil** |
| `BotMu` | Duyarlilik deneyi | Ana modelde **kullanilmiyor**; bu karar Adim 4'e kadar degismez |
| 15 commit ozniteligi | Oznitelik | Evet |

**`LabelSource` neden en tehlikelisi:** etiketlenmemis commit'te `null`, etiketlenmiste
`szz`. Yani alanin **dolu olup olmamasi hedefin kendisi**. Modele girerse mukemmel bir
sonuc ureten ve hicbir sey ogrenmeyen bir model cikar. Snapshot'ta duruyor olmasinin tek
sebebi denetim: etiketin nereden geldigini sonradan kontrol edebilmek.

**`Repository` neden ayni-repo deneyinde girmiyor:** o deneyde zaten tek repo var, sabit
bir sutun. Repo-arasi deneyde de girmez, cunku egitimde gorulmeyen bir degeri test
zamaninda gormek ise yaramaz.

**`AuthorDateUtc` neden varsayilan degil:** takvim tarihi modele girerse model "yeni
commit'ler daha az etiketli" iliskisini ogrenir, ki bu veri hakkinda bir gercek degil,
sag sansurun kendisi (bkz. `asama5-beklenti.md`, 1. madde). Kullanilacaksa ayri bir deney
olarak ve acikca yazilarak kullanilir.

## 4. Tarihsel ozniteliklerin gecmise bakma testi

Su yedi oznitelik commit'in **gecmisinden** hesaplaniyor ve sizintinin en olasi yeri
burasi:

- `PriorFixes`
- `PriorChanges`
- `AuthorCommitCount`
- `AuthorFileExperience`
- `DistinctAuthorsOnFiles`
- `MaxFileAgeDays`
- `MinFileAgeDays`

**Test (yedisi icin ayni):** bir commit secilir, veri kumesinden o commit'ten **sonraki**
butun kayitlar gecici olarak cikarilir, o commit'in metrigi yeniden hesaplanir.

**Gecer:** yeniden hesaplanan deger, snapshot'taki kayitli degere **birebir esit**.

**Kalir:** herhangi bir fark. Fark cikarsa oznitelik gelecekten bilgi tasiyor demektir ve
duzeltilmeden model kurulmaz.

Orneklem: her repodan en az 20 commit, biri egitim bolumunun basindan, biri test
bolumunun sonundan olmak uzere bolmenin iki tarafina da dagitilir; secim tohumu yazilir.
Yedi oznitelik ayri ayri raporlanir, "hepsi gecti" tek satirda toplanmaz.

Asama 4'te buna benzer bir kontrol vardi (uc repodan 60 commit, 0 fark) ama o kontrol
**butun olculeri birlikte** karsilastiriyordu; burada yedi tarihsel oznitelik ayri ayri
sayilacak, cunku bir tanesinde cikan fark digerlerinin icinde kaybolmamali.

## 5. Snapshot butunluk kontrolleri

| # | Kontrol | Nasil gecer |
|---|---|---|
| 5.1 | Ayni `repo + Sha` iki kez bulunmamali | Tekrar sayisi 0 |
| 5.2 | Hedef (`IsBugIntroducing`) NULL olmamali | NULL sayisi 0 |
| 5.3 | Sayisal ozniteliklerde NULL olmamali | Oznitelik bazinda sayilir, hepsi 0 |
| 5.4 | Sayisal ozniteliklerde NaN olmamali | Oznitelik bazinda sayilir, hepsi 0 |
| 5.5 | Sayisal ozniteliklerde sonsuz deger olmamali | Oznitelik bazinda sayilir, hepsi 0 |
| 5.6 | Tarihler UTC olmali | Sifir olmayan saat farki tasiyan satir yok |
| 5.7 | Uc repo disinda satir olmamali | Farkli `RepositoryIdentity` sayisi 3 |
| 5.8 | Satir ve etiket sayilari Asama 4 ile ayni | 34 166 satir, 5962 pozitif; repo bazinda da ayni |

`Entropy` tek `double` oznitelik, yani NaN ve sonsuz riski asil orada. Digerleri tam sayi
ve `IsFix` mantiksal; yine de hepsi sayilacak, cunku "olamaz" demek kontrol etmenin yerine
gecmez.

Bir sayi uyusmazsa: sessizce devam edilmez, veri degistirilmez, satir silinmez, snapshot
"gecerli" ilan edilmez. Fark raporlanip durulur.

## 6. 11 / 13 sonucu nasil kullanilmaz

Asama 4'un elle dogrulamasinda etiketsiz orneklemde 11 / 13 kacirma cikti. Bu orneklem
**hedefli** secildi: "`IsFix` tarafindan dokunulmus fakat etiketlenmemis" commit'lerden,
yani kacirma ihtimalinin en yuksek oldugu yerden. Butun negatiflerden alinmis rastgele
bir orneklem **degil**.

Bu yuzden:

- Negatif sinifin genel hata orani olarak **kullanilmaz**.
- Butun veri kumesine duzeltme orani olarak **uygulanmaz**.
- Agirliklandirmada ya da yeniden etiketlemede **kullanilmaz**.
- Yalnizca "hedefli secilmis etiketsiz orneklemde yuksek kacirma isareti" olarak yazilir.

Negatif sinifta etiket gurultusu riski oldugu soylenebilir; **yayginligi bu orneklemden
tahmin edilemez**. Tahmin icin butun negatiflerden rastgele orneklem gerekirdi ve o olcum
yapilmadi.

Ayni uyari `asama5-veri-kumesi.md` icinde de duruyor; iki dosyada birden durmasinin
sebebi, snapshot'i kullanan birinin beklenti dosyasini okumamis olabilmesi.
