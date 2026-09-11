# 0006 - check komutunun cikis kodu ve --fail-on

**Baglam:** check komutu CI'da calisacak. Bir aracin build'i ne zaman kirmasi gerektigi kod detayi degil, tasarim karari: cok sert olursa insanlar araci kapatir, cok yumusak olursa kimse bulgulara bakmaz.

**Karar:** check, verilen esikte ya da ustunde en az bir bulgu varsa 1, yoksa 0 donuyor. Esik `--fail-on <seviye>` ile veriliyor, varsayilani `warning`. Seviyeler `info`, `warning`, `error`.

**Neden:** Varsayilani `error` yapsaydim uyari seviyesindeki kurallar pratikte hic okunmazdi. `info` yapsaydim ilk bilgi kuralini ekledigim gun herkesin build'i kirilirdi. `warning` ortada duruyor: gercekten bakilmasi gereken seyler build'i kiriyor, bilgi amacli olanlar ekrana basiliyor ama kimseyi durdurmuyor. Esigin altinda kalan bulgular da yine yaziliyor, sadece cikis kodunu etkilemiyorlar; boylece "gormezden gelmek" ile "gizlemek" ayni sey olmuyor.

Seviye adlarini Ingilizce birakip ekranda Turkce etiket (`[hata]`) gostermeyi sectim. Bayrakta yazilan deger enum uyeleriyle ve JSON ciktisindaki `severity` degeriyle ayni olsun istedim; JSON'u okuyup CI'da esik belirleyen biri iki farkli sozluk ogrenmek zorunda kalmasin.

**Sonuc:** `sievert check <yol>` bulgu bulursa 1 donuyor ve CI adimi kiriliyor. Bunun bilinen bir puruzu var (**cozuldu, asagidaki guncellemeye bak**): kullanim hatasi ve dosya bulunamama durumlari da 1 donuyor, yani "arac bozuldu" ile "bulgu var" cikis kodundan ayirt edilemiyor. Simdilik boyle biraktim cunku scan komutu da hatalarda 1 donuyor ve ikisi arasinda tutarsizlik istemedim. Ayri bir kod (ornegin kullanim hatasi icin 2) gerekirse Asama 7'de GitHub Action yazilirken karar veririm.

## Guncelleme (2026-09-11)

Yukaridaki puruzu cozdum: cikis kodu artik iki degil uc degerden birini aliyor ve bu hem scan hem check icin gecerli.

| Kod | Anlami |
|---|---|
| 0 | Komut calisti, esigi gecen bulgu yok |
| 1 | Komut calisti, esigi gecen bulgu var |
| 2 | Arac calisamadi: yol bulunamadi, gecersiz bayrak, beklenmeyen hata |

Boyle yaptim cunku CI'da bu ikisi tamamen farkli seyler. "Bulgu var" beklenen bir sonuc, insanin gidip koda bakmasi gerekiyor. "Arac calisamadi" ise bir yapilandirma ya da kurulum sorunu; o durumda bulgu listesi bos gelir ve eski haliyle bunu basarili bir tarama sanmak mumkundu. Ayri kod olunca CI adimi hangi durumda oldugunu bilebiliyor.

scan kural calistirmadigi icin 1 donduremiyor; basariliysa hep 0, hata alirsa 2 donuyor. Ikisinin ayni tabloyu kullanmasi, ileride baska komutlar eklenirse de ayni sozlesmenin gecerli olmasi icin.

Beklenmeyen hatalari da 2'ye baglamak icin komut akisini tek bir yerde try/catch'e aldim. Boylece bir dosya okunamadiginda ya da baska bir sey patladiginda arac sessizce 0 donup "her sey temiz" demiyor.
