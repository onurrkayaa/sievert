# Ornek dosyalar

Buradaki .cs dosyalari test verisi. Cozumleyicinin dogru sayip saymadigini
kontrol etmek icin duruyorlar, gercek kod degiller.

Hicbir projeye referans verilmiyor, yani derlenmeleri beklenmiyor. Test projesi
bunlari sadece dosya olarak kopyalayip okuyor. Editorunde actiginda hata
gorursen normal.

- `Normal.cs` - gecerli C#. Icinde partial sinif, ic ice sinif, async metotlar,
  ifade govdeli metot, yerel fonksiyon, constructor ve property var. Her birinin
  dogru sayilip sayilmadigini test ediyorum.
- `Broken.cs` - **bilerek hatali**. Kapanmayan parantez ve eksik noktali virgul
  var. Amaci, sozdizimi bozuk bir dosyada cozumleyicinin exception firlatmadigini
  ve cozebildigi kadarini dondurdugunu dogrulamak.
- `AsyncVoid.cs` - SV001 kuralinin ornekleri. Icinde yakalanmasi gereken sade
  bir `async void`, muaf tutulan bir event handler, bulgu uretmemesi gereken bir
  `async Task` ve sinir durum olarak parametresiz bir `async void` var.
