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
- `EventSubscription.cs` - SV001'in abonelik kaniti icin (ADR 0008). Buradaki
  metotlarin hicbiri `(object sender, EventArgs e)` kalibina uymuyor, yani karari
  imza degil abonelik veriyor. Iki tanesi muaf: biri ayni dosyada `+=` ile abone
  ediliyor, digeri ayni partial sinifin baska bir parcasinda. Ucu bulgu: hic abone
  olunmayan bir metot, aboneligi sadece yorum satirinda yazan bir metot ve duz
  atama (`=`) ile bir `Action` alanina baglanan bir metot.
- `Uploader.Subscriptions.cs` - `EventSubscription.cs` icindeki `Uploader`
  sinifinin ikinci parcasi. Tek isi, abonelik baska bir dosyada dururken de muaf
  tutuluyor mu diye kontrol etmek.
- `OtherSubscriber.cs` - dar kapsam testi. `Uploader` ile ayni klasorde ama onun
  parcasi degil; `Refresh` adini abone ediyor. `Uploader.Refresh` yine de bulgu
  uretmeli, cunku ad esitligi tek basina yetmiyor.
- `Blocking.cs` - SV002'nin ornekleri. Yakalanmasi gereken dort bloklama
  (`.Result`, `.Wait()`, `.GetAwaiter().GetResult()` ve task gibi duran bir alan),
  muaf tutulan bir `Main`, ve bulgu uretmemesi gereken iki sinir durum: Task gibi
  durmayan bir nesnenin kendi `Result` property'si, bir de dogru yazilmis `await`.
- `MissingAwait.cs` - SV003'un ornekleri. Iki bulgu (bosta birakilan cagri ve
  await edilmemis `ConfigureAwait` zinciri), iki muafiyet (`_ =` ile bilincli atma
  ve `Task.Run` icindeki fire-and-forget), ve bulgu uretmemesi gereken uc sinir
  durum: await edilen cagri, degiskene alinan cagri, adi `Async` ile bitmeyen cagri.
