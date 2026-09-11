# 0006 - check komutunun cikis kodu ve --fail-on

**Baglam:** check komutu CI'da calisacak. Bir aracin build'i ne zaman kirmasi gerektigi kod detayi degil, tasarim karari: cok sert olursa insanlar araci kapatir, cok yumusak olursa kimse bulgulara bakmaz.

**Karar:** check, verilen esikte ya da ustunde en az bir bulgu varsa 1, yoksa 0 donuyor. Esik `--fail-on <seviye>` ile veriliyor, varsayilani `warning`. Seviyeler `info`, `warning`, `error`.

**Neden:** Varsayilani `error` yapsaydim uyari seviyesindeki kurallar pratikte hic okunmazdi. `info` yapsaydim ilk bilgi kuralini ekledigim gun herkesin build'i kirilirdi. `warning` ortada duruyor: gercekten bakilmasi gereken seyler build'i kiriyor, bilgi amacli olanlar ekrana basiliyor ama kimseyi durdurmuyor. Esigin altinda kalan bulgular da yine yaziliyor, sadece cikis kodunu etkilemiyorlar; boylece "gormezden gelmek" ile "gizlemek" ayni sey olmuyor.

Seviye adlarini Ingilizce birakip ekranda Turkce etiket (`[hata]`) gostermeyi sectim. Bayrakta yazilan deger enum uyeleriyle ve JSON ciktisindaki `severity` degeriyle ayni olsun istedim; JSON'u okuyup CI'da esik belirleyen biri iki farkli sozluk ogrenmek zorunda kalmasin.

**Sonuc:** `sievert check <yol>` bulgu bulursa 1 donuyor ve CI adimi kiriliyor. Bunun bilinen bir puruzu var: kullanim hatasi ve dosya bulunamama durumlari da 1 donuyor, yani "arac bozuldu" ile "bulgu var" cikis kodundan ayirt edilemiyor. Simdilik boyle biraktim cunku scan komutu da hatalarda 1 donuyor ve ikisi arasinda tutarsizlik istemedim. Ayri bir kod (ornegin kullanim hatasi icin 2) gerekirse Asama 7'de GitHub Action yazilirken karar veririm.
