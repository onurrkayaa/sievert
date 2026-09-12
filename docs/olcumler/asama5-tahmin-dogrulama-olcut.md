# Model tahminlerinin elle dogrulanmasi: olcutler

**Tarih:** 2026-09-12
**Durum:** Orneklem **secilmeden once** ilan edildi. Bir commit'e bakildiktan sonra olcut
degistirilmez.

Bu dosya, `asama5-tahmin-dogrulama-malzeme.md` icindeki 30 commit'in nasil
isaretlenecegini soyluyor. Degerlendirme **kor**: degerlendirici model tahminini,
model olasiligini ve SZZ etiketini gormuyor.

Asama 5'in genel elle inceleme olcutleri (`asama5-olcut.md`) SZZ etiketlerini
degerlendirmek icindi. Bu dosya farkli bir soruyu soruyor: **commit gercekten bir kusur
getirmis mi.** Kategori adlari da o yuzden farkli.

## Kategoriler

Her satir dorttunden **tam olarak biriyle** isaretlenir.

**KUSUR-GETIRDI**
Commit, daha sonra hata duzeltmesi gerektiren davranissal bir kusuru kaynak koda eklemis
ya da mevcut bir kusuru davranissal olarak agirlastirmistir.

**KUSUR-GETIRMEDI**
Degisiklik ozellik, yeniden duzenleme, bicimlendirme, dokumantasyon, test bakimi ya da
davranissal kusur getirmeyen baska bir degisikliktir; sonraki bir duzeltmeyle nedensel
bag gosterilememistir.

**VERI-YETMEDI**
Commit'in diff'i ve gorulebilen sonraki tarih karar vermek icin yeterli degildir.

**BAKILMADI**
Degerlendirici bu satira zaman ayirmamistir.

## Kurallar

- **VERI-YETMEDI ile BAKILMADI ayri sayilir** ve tek bir kovada toplanmaz. Birincisi
  verinin sinirini, ikincisi degerlendiricinin sinirini soyluyor.
- Ikisi de oran **paydasindan cikarilir**.
- **Bos karar gecersizdir.** Karar verilemiyorsa VERI-YETMEDI, incelenmediyse BAKILMADI
  yazilir.
- **Sonraki bir commit ayni dosyaya dokundu diye ilk commit otomatik kusurlu sayilmaz.**
  Ayni dosyanin degismesi sikca olan bir sey; aranan sey nedensel bag.
- **Mesajda `fix` ya da `bug` gecmesi tek basina kanit degildir.** Bir duzeltme
  mesajinin baska bir commit'i suclamasi icin degistirdigi davranisin o commit'ten
  geldigi gorulmeli.
- **SZZ etiketi degerlendiriciye gosterilmez.**
- **Model tahmini ve olasiligi degerlendiriciye gosterilmez.**
- Degerlendirici yalnizca malzeme dosyasindaki hazirlanmis olgulara ve baglantilara bakar.

## Sayim kurali

- Payda yalnizca **KUSUR-GETIRDI ve KUSUR-GETIRMEDI** satirlarindan olusur.
- VERI-YETMEDI ve BAKILMADI ayri ayri sayilir ve ayri ayri yazilir.
- Pay ve payda her zaman ayri yazilir.
- Model-pozitif ve model-negatif ornekler **tek bir sayida birlestirilmez**.
- Orneklem siniflara esit dagitildigi icin **populasyon accuracy, genel precision, genel
  recall ya da genel hata orani hesaplanmaz**.
- Model-pozitif kumede hesaplanan oran **"precision isareti"**, model-negatif kumede
  hesaplanan oran **"kacirma isareti"** olarak adlandirilir. Ikisi de isaret; populasyon
  degeri degil.

## Yontem

> Otuz commit, onceden ilan edilmis olcutlerle yazar tarafindan commit diff'i ve sonraki
> ilgili degisiklikler incelenerek siniflandirildi; model tahmini ve otomatik etiket
> degerlendiriciye gosterilmedi, bagimsiz ikinci degerlendirici kullanilmadi.

Bu cumle sonuc tablosunda aynen tekrarlanir.

## Korlugun siniri

Kor anahtar (`data/asama5/prediction-validation-key.csv`) ayni repoda duruyor.
Korluk **yontemsel**: degerlendirici o dosyayi kararlar bitene kadar acmamayi kabul
ediyor. Teknik olarak engellenmis degil ve degerlendirici tek kisi, ayni zamanda araci
yazan kisi. Bu, projenin butun elle siniflandirmalarinda oldugu gibi bir sinirlilik olarak
kalir.
