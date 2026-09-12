# 0017 - Taban cizgileri ve degerlendirme metrikleri

**Baglam:** Asama 5'in modeli kurulmadan once "iyi sonuc" diye bir sey yok. Bir F1
degerinin buyuk mu kucuk mu oldugunu soyleyebilmek icin ayni veri kumesinde, ayni bolmeyle
ve ayni formullerle olculmus baska sayilara ihtiyac var. Bu ADR o sayilarin hangileri
oldugunu ve nasil hesaplandigini karara bagliyor.

Ikinci sebep: metrik tanimlari sonradan secilirse, cikan sayiya gore secilmis olurlar. O
yuzden tanimlar (`docs/olcumler/asama5-metrik-sozlesmesi.md`, surum 1.0) koddan once
yazildi ve tabanlar o sozlesmeyle olculdu.

**Karar:** Uc taban cizgisi, accuracy'siz bir metrik kumesi ve tanimi yazili tek bir
PR-AUC hesabi.

## Neden accuracy yok

Test bolumunde 10 251 satirin 1066'si pozitif, yani negatif sinif dortte ucun uzerinde.
Hicbir sey yapmayan "her seye negatif" tabani bu kumede %89,6 accuracy alir; Polly'de
%98,8.

Accuracy raporlansaydi projenin en ise yaramaz tabani, en yuksek accuracy'li satir olurdu.
Bu sayi yontemleri birbirinden ayirmiyor, ustelik ayirdigi izlenimi veriyor. O yuzden
hesaplanmiyor bile: hesaplanip "dikkat, yaniltici" notuyla yazilsaydi eninde sonunda bir
tabloda tek basina kalirdi.

Bunun bedeli var: accuracy yaygin bir sayi ve raporu okuyan biri arayabilir.
Bulamayacak, sebebini burada okuyacak.

## Neden uc taban

Uc tabanin her biri farkli bir soruyu kapatiyor.

- **Her seye negatif:** "hic alarm uretmeyen bir sey ne kadar iyi gorunur." Metriklerin
  dogru sectigini gosteriyor; F1 ve recall bu tabanda 0 cikiyor, oysa accuracy yuksek
  cikardi.
- **Oranla rastgele:** "veriyi hic bilmeden, yalnizca taban orani kadar alarm uretsem ne
  alirim." Bir yontemin pozitif tahmin sayisindan gelen avantaji ile gercekten ogrendigi
  seyi ayiriyor.
- **LinesAdded esigi:** "tek bir sayiya bakarak ne kadar yol alinir." Modelin 15 oznitelikle
  yaptigi isin ne kadarinin tek bir boyut olcusunden geldigini gosterecek.

Ucu birlikte bir siralama veriyor. Model bu siralamanin uzerine cikamazsa, 15 oznitelikli
olmasi onu iyi yapmiyor.

## Neden her seye negatif tabani gerekli

Bu taban hicbir sey ogrenmiyor ve hicbir ise yaramiyor; olculmesinin sebebi de bu.

Iki isi var. Birincisi accuracy'nin neden yazilmadigini sayiyla gosteriyor. Ikincisi
PR-AUC icin bir taban cizgisi veriyor: sabit skorlu bir tabanin PR-AUC'si kumenin pozitif
orani cikiyor (asagida), yani bir modelin PR-AUC'si bu sayinin uzerine cikmiyorsa
siralama yetenegi gostermemis demektir. O sayiyi bilmeden PR-AUC okunamaz.

## Rastgele tabanda p neden egitimden geliyor

Olasilik her repo icin **kendi egitim bolumunun** pozitif orani: Polly 0,1300, ShareX
0,1487, Jellyfin 0,2345.

Test bolumunden alinsaydi taban cevabi bilirdi. "Test kumesinde pozitiflerin orani sudur"
bilgisi gercek kullanimda yok: bir commit gonderilirken o commit'in dahil oldugu kumenin
hata orani bilinmiyor. Egitimden almak, tabanin da modelle ayni bilgiyle calismasi demek.

**Uc oran birlestirilip tek bir p uretilmedi.** Oranlar birbirinden uzak (0,1300 /
0,1487 / 0,2345); ortak bir oran uc repoda da yanlis olurdu ve mikro toplam, kendisi
hatali bir tabanla karsilastirilirdi. Her repo kendi oranini kullaniyor, mikro toplam da
o tahminlerin birlesimi.

## Neden 1000 tekrar ve sabit tohum

Tek kosuda cikan sayi rastgeleligin kendisinden etkileniyor. Polly'de test bolumunde
yalnizca 10 pozitif var; bir kosuda hicbiri yakalanmayabiliyor (olculdu: F1'in p2,5
degeri 0,0000).

1000 tekrar, tek bir sayi yerine bir **dagilim** veriyor. Boylece "LinesAdded tabani
rastgeleden iyi" demek yerine "rastgele tabanin 1000 tekrarindaki en ust %2,5'lik dilimin
de uzerinde" denebiliyor.

Tohum sabit (ana tohum 20260912, tekrar tohumu = ana tohum + tekrar numarasi) cunku sonuc
tekrarlanabilmeli. Her tekrarda **tek** bir rastgele akis var ve depolar ordinal sirada
tuketiyor; repo basina ayni tohumla ayri akis acilsaydi bir reponun cizisi digerinin
oneki olur, yani repolar birbiriyle iliskili cikardi.

1000 sayisi bir olcumden degil, makul bir yerden geliyor: dagilimin p2,5 ve p97,5
uclarini oturtmaya yetiyor ve saniyeler suruyor. Daha fazlasinin gerekip gerekmedigi
olculmedi.

## LinesAdded esigi neden yalnizca egitimde seciliyor

Aday esikler yalnizca egitim bolumunde gorulen degerlerden uretiliyor ve esik egitim
F1'ine gore seciliyor. Esik secildikten sonra bir daha degistirilmeden teste uygulaniyor.

Test bolumune bakarak esik secilseydi olculen sayi gercek kullanimda alinamayacak bir
sayi olurdu: yeni bir commit geldiginde onun etiketine bakip esik ayarlanamaz. Bu, ADR
0016'daki bolme kararinin aynisinin esik tarafi.

Test degerlerinden aday uretmek de ayni sizintinin daha ince bir bicimi olurdu: esik
egitimden secilse bile aday kumesi test verisinin sekline bakarak kurulmus olurdu.

## Esik esitliginde neden daha yuksek esik

Secim sirasi: en yuksek egitim F1'i, esitlikte en yuksek egitim precision'i, hala
esitlikte **daha yuksek esik**.

Ucuncu kuralin gerekcesi: yuksek esik daha az alarm uretiyor. Iki taban ayni F1'i
veriyorsa daha az yanlis alarm ureteni tercih ediliyor; bir gelistiricinin gunde
bakabilecegi uyari sayisi sinirli.

**Bu iki kural tek oznitelikli monoton bir esikte sonucu degistiremiyor.** Cebiri su:
`t1 > t2` iki esik, `k1 < k2` tahmin edilen pozitif sayilari, `P` kumedeki gercek pozitif
sayisi. F1 = 2·TP / (k + P). Iki esigin F1'i esitse `TP2 = TP1·(k2+P)/(k1+P)`. Dusuk
esigin precision'i yuksek olsun istersek `TP2/k2 > TP1/k1` gerekir; yerine koyunca bu
`k1 > k2` demeye geliyor, oysa `k1 < k2`. Yani esit F1'de dusuk esik hicbir zaman daha
yuksek precision veremez. `TP1 = 0` hali de kapali: o zaman iki F1 de 0 olur, ama en
yuksek F1 hicbir zaman 0 olamaz cunku en dusuk esik butun pozitifleri yakalar.

Kurallari yine de yaziyorum ve test ediyorum: ileride monoton olmayan bir kural ya da
baska bir oznitelik gelirse gerekecekler, ve bir secim kuralinin yazili olmamasi
sonradan "hangi esigi neden sectik" sorusunu cevapsiz birakirdi.

## Mikro ve makro F1 farki

- **Mikro:** uc reponun test tahminleri tek havuzda toplanip TP/FP/FN/TN ustunde
  hesaplaniyor.
- **Makro:** her reponun F1'i ayri hesaplanip uc sayinin basit ortalamasi aliniyor.

Ikisi farkli sorulara cevap veriyor. Mikro "rastgele bir commit'e baktigimda ne olur"
diyor ama Jellyfin test satirlarinin %67,1'ini (6876 / 10 251) tasidigi icin buyuk olcude
o reponun sonucu. Makro "yeni bir repoya gittigimde ne beklerim" diyor ve uc repoyu esit
sayiyor.

Olculen fark kucuk degil: LinesAdded tabaninda mikro 0,3754, makro 0,2999. Tek bir sayi
yazilsaydi hangisinin yazildigi sonucu degistirirdi, o yuzden ikisi ayri sutunlarda
duruyor ve birbirinin yerine kullanilmiyor.

## PR-AUC'nin tam hesap tanimi

Tam tanim metrik sozlesmesinde; burada karar kismi:

1. Skorlar azalan siralanir.
2. **Esit skorlar tek esik grubu** olarak birlikte islenir, grup bolunmez. Bu yuzden sonuc
   esit skorlu satirlarin kendi aralarindaki sirasindan bagimsiz.
3. Her grubun sonunda `r = TP / P`, `p = TP / (TP + FP)` noktasi uretilir.
4. Egrinin basina `(0, ilk grubun precision'i)` eklenir.
5. **Yamuk kurali** ile integre edilir.

"Average precision" (basamak toplami) **kullanilmiyor**. Ikisi ayni egri icin farkli
sayilar uretir; hangisinin kullanildigi yazilmazsa iki calismanin PR-AUC'si
karsilastirilamaz. Hicbir kutuphanenin varsayilanina baglanilmadi, hesap elle yazildi ve
elle hesaplanmis bir ornekle (19/24) sinaniyor.

Esikli tabanlarda **iki ayri PR-AUC** raporlaniyor: ham oznitelik skorununki (siralama
yetenegi) ve esik sonrasi 0/1 skorununki (tek esigin ozeti). Modelle karsilastirma
birincisiyle yapilacak.

## Sabit skorlu tabanin PR-AUC sinirliligi

Butun satirlarin skoru ayniysa tek esik grubu olusur, `r = 1`, `p = P/N`, ve yamuk
toplami `P/N` cikar. Yani sabit skorlu bir tabanin PR-AUC'si tam olarak kumenin pozitif
orani.

Olculen degerler bunu dogruluyor: Polly 0,0121 (10/828), ShareX 0,0506 (129/2547),
Jellyfin 0,1348 (927/6876), mikro 0,1040 (1066/10 251).

Bu sayi bir basari olcusu **degil**. Sabit skorda siralama bilgisi yok; deger yontemin
degil veri kumesinin ozelligi. Raporda her zaman "sabit skor, siralama yetenegi yok"
notuyla yaziliyor ve model basarisi gibi yorumlanmiyor.

## Sag sansur neden bu adimda veriyi degistirmedi

Adim 1'de olculdu: Polly'nin test bolumunde 180 gunden az olgun 207 commit var ve hicbiri
pozitif degil. Polly'nin test pozitif orani %1,21, oysa egitim bolumunde %13,0.

Bu, taban sonuclarini dogrudan etkiliyor: Polly'nin butun test sayilari 10 pozitife
dayaniyor. Yine de **ana test kumesi degistirilmedi**: son 30/90/180 gunluk satirlar
cikarilmadi, 90 gunluk olgun alt kume hesaplanmadi, etiket ya da bolme degistirilmedi.

Sebebi: sansurlu satirlari atmak sonuclari duzeltir gorunur ama bu bir olcum degil, bir
tercih olurdu; ustelik hangi esigin (30, 90, 180 gun) secildigine gore farkli sayilar
cikar ve secim, cikan sayiya bakarak yapilmis olurdu. 90 gunluk duyarlilik kumesi Adim
1'de, sonuclar gorulmeden ilan edildi ve Adim 4'te ana sonucun **yaninda** hesaplanacak.

Ayni gerekce Asama 4'un 11 / 13 sonucu icin de gecerli: hedefli secilmis bir orneklemden
gelen o oran bu adimda hicbir yere girmedi, etiket degistirilmedi, agirlik verilmedi.

**Sonuc:** Uc taban `data/asama5/baseline-results.json` icinde ozetiyle birlikte duruyor.
Model kuruldugunda "iyi mi" sorusunun cevabi bu dosyadaki sayilara bakilarak verilecek.
