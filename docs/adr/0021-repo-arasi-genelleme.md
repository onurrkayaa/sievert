# 0021 - Repo-arasi genelleme

**Baglam:** Adim 3 her repo icin ayri bir model kurdu ve her modeli kendi reposunda
olctu. O deney "bu repoda gecmisten gelecegi tahmin edebilir miyiz" sorusunu cevapliyor.
Bu adim baska bir soru soruyor: **bir repoda ogrenilen model baska bir repoda ise yarar
mi.** Ikisi ayri deney ve birinin sonucu digerinin yerine gecmiyor.

**Karar:** Alti tek kaynakli yon ve uc leave-one-repository-out deneyi; kaynak disinda
hicbir sey hedeften ogrenilmiyor; esik ve siralama ayri raporlaniyor.

## Neden alti yon ve neden ayrica leave-one-out

Alti yon, uc repodan her birinin kaynak ve hedef olarak ayri ayri gorulmesini sagliyor.
Tek bir yon olculseydi, cikan sonucun kaynaga mi hedefe mi ait oldugu ayrilamazdi.

Leave-one-repository-out ayri bir soru: **iki repoyu birlestirmek tek repodan iyi mi.**
Uygulamada gercek durum buna daha yakin - elde birkac repo var ve yenisine gidiliyor.

Olculen sonuc iki deneyin ayri tutulmasini hakli cikardi: uc hedefte de iki kaynakli
model, o hedefin **en iyi** tek kaynakli aktariminin altinda kaldi (-0,1412, -0,0700,
-0,0187). Ama yayilimi daha dar: korunan F1 orani tek kaynakta 0,768 - 1,500, iki
kaynakta 0,852 - 1,137.

## Neden hedeften hicbir sey ogrenilmiyor

Donusum, model ve esik **yalnizca kaynak train'den**. Hedef test hicbir fit islemine
girmiyor; hedefte oracle esik hesaplanmiyor ve hedef testle kalibrasyon yapilmiyor.

Sebep ADR 0016 ve 0018'deki ile ayni: hedefin etiketlerine bakarak ayarlanan bir sey,
gercek kullanimda yapilamaz. Yeni bir repoya gidildiginde o reponun etiketleri henuz
yoktur - zaten olsaydi modele ihtiyac olmazdi.

## Neden repo kimligi oznitelik degil

`Repository` ve `RepositoryIdentity` modele **girmiyor** (ADR 0016). Repo-arasi deneyde
bunun ek bir sebebi var: egitimde gorulmeyen bir deger test zamaninda geliyor. Model
"Jellyfin" gorup ogrendigi bir seyi, "Polly" degerini hic gormeden uygulayamaz.

## Neden agirlik esitlenmedi

Iki kaynakli deneylerde repolar **dogal satir sayilariyla** birlesti. Polly + Jellyfin
birlesiminde 17 972 satirin 16 041'i (%89,3) Jellyfin'den.

Agirlik esitlemek bir tercih olurdu ve bu adimda o tercih yapilmadi: hangi agirliklamanin
dogru oldugu olculmeden secilemez. Bunun buyuk reponun agirligini artirdigi raporda
sayiyla yaziyor; esitlenseydi ne olacagi **olculmedi**.

## Esik ve siralama neden ayri raporlaniyor

Iki sonuc yan yana duruyor: kaynak train'de secilen esikle ve sabit 0,5 ile. PR-AUC ham
olasilik uzerinden tek bir deger.

Ayrim su: **PR-AUC siralama aktarimini olcuyor**; **F1 hem siralamayi hem kaynakta
secilen esigin hedef taban oranina uyumunu birlikte olcuyor.**

Olculen ornek bunu net gosterdi: ShareX → Jellyfin'de PR-AUC 0,4941 (ayni-repo 0,5199'un
%95'i) ama 0,5 esiginde F1 0,0785, kaynak esigiyle 0,3761. Siralama buyuk olcude
tasinmis, sabit esik tasinmamis.

Bu sonuc **esik / taban orani uyumsuzluguyla uyumlu**, ama **tek basina kanit degil**:
kaynak ve hedefin oznitelik dagilimlari da farkli ve bu deney iki etkiyi ayirmiyor. Uc
reponun test taban oranlari %1,21 / %5,06 / %13,48, yani birinde secilen esigin
digerinde ayni anlama gelmesi icin bir sebep yok.

## Katsayilar nasil karsilastirilir

Her model **kendi kaynaginin** normalizasyon parametreleriyle egitildi. O yuzden ham
katsayi buyuklukleri kaynaklar arasinda dogrudan fark olarak yorumlanmiyor: iki modelde
ayni sayinin ayni anlama geldigi gosterilmedi.

**Isaret ve siralama daha guvenli karsilastirma** ve olculdu: dokuz modelin hepsinde ilk
uc ayni (`CsFilesChanged`, `FilesChanged`, `LinesAdded`), yalnizca ilk ikisinin sirasi
degisiyor; isaretler de korunuyor.

`PriorFixes`'in isareti kaynaga gore degisiyor (alti kombinasyonun ikisinde negatif,
dordunde pozitif). Bu, ADR 0018'de yazilan uyariyla tutarli: yuksek korelasyonlu
ciftlerde tek bir katsayinin isareti kararsiz olabiliyor.

**Nedensellik iddiasi kurulmuyor**; soylenebilecek olan modelde tasinan iliski.

## "Korunan performans" bir ad, iddia degil

`repo-arasi / ayni-repo` orani hesaplandi ve payda uc hedefte de sifirdan farkli. Orana
"korunan performans" deniyor ama bu **nedensellik iddiasi degil**: oranin 1'in ustunde
cikmasi modelin baska bir repoda "daha iyi ogrendigi" anlamina gelmiyor. Uc hedeften
ikisinde ayni-repo sonucu zaten dusuktu (ShareX 0,1868, Polly 0,2500) ve asilmasi kolay
bir esikti.

## Beklentiler neden degistirilmedi

`asama5-genelleme-beklenti.md` ve `asama5-beklenti.md`'nin 7. maddesi bu adimdan once
yazildi ve **degistirilmedi**. Ikisi de repo-arasi F1'in ayni-repo F1'inin altinda
kalmasini bekliyordu; olculen dokuz deneyin dordunde **ustunde** cikti ve `asama5-beklenti.md`'
nin "yarisi ile dortte ucu arasi" bandi da tutmadi.

Tutmayan beklenti silinmedi, raporda "TUTMADI" olarak duruyor. Sebebi **olculmedi**;
raporda bir aciklama adayi var ve tahmin oldugu yaziyor.

**Sonuc:** Dokuz genelleme modeli `data/asama5/generalization-results.json` icinde,
30 753 hedef tahmini `generalization-predictions.csv` icinde ozetleriyle duruyor. Ilk uc
oznitelik dokuz modelde de ayni; PR-AUC F1'den dar bir bantta aktarildi; iki kaynakli
modeller uc hedefte de en iyi tek kaynakli aktarimin altinda kaldi.
