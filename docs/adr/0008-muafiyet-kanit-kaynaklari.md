# 0008 - SV001 muafiyeti icin iki kanit kaynagi

**Baglam:** Asama 2'de SV001'i ShareX uzerinde olcup ciktisindan 20 satiri elle inceledim. 10 bulgunun 2'si yanlis pozitif cikti ve ikisinin de sebebi ayniydi: metot gercekten bir olaya abone ama imzasi `(object sender, XEventArgs e)` kalibina uymuyor, cunku olay ozel bir delegate tipiyle tanimlanmis.

- `EditorView.Subscriptions.cs:57` — `vm.LoadFromUrlRequested += OnLoadFromUrlRequested;`
- `MainForm.cs:187` — `Program.HotkeyManager.HotkeyTrigger += HandleHotkeys;`

Yani muafiyet yanlis seye bakiyordu. Dogru olcut metodun imzasi degil, bir olaya abone olup olmadigi. Ayrintisi `docs/olcumler/asama2-dogrulama-listesi.md` icindeki Sonuc bolumunde.

**Karar:** Muafiyet artik iki ayri kanit kaynagindan gelebiliyor ve biri yetiyor:

1. **Imza** (eskiden beri var): metot iki parametre aliyor ve ikincisinin tip adi `EventArgs` ile bitiyor.
2. **Abonelik** (yeni): metoda `+= MetotAdi` biciminde abone olunuyor.

Ikinci kanitin arama kapsami bilerek dar: sadece metodun kendi dosyasi, bir de ayni klasordeki ayni partial sinifin diger parcalari. Eski imza heuristigi kaldirilmadi, yaninda calisiyor.

**Neden repo genelinde ad aramasi yok:** Metot adi benzersiz degil. ShareX klonunda `OnOpened` 42, `OnDrop` 17 ayri yerde abone ediliyor ve bunlar farkli siniflarin ayni adli metotlari. Repo genelinde ada bakan bir arama, bir sinifta yapilan aboneligi baska bir sinifin metoduna mal eder. Pratikte de ise yaramaz: bu kadar yaygin adlarla neredeyse her `async void` metot bir eslesme bulur ve kural hicbir sey uretmez hale gelir. Kapsami dar tutmak, ad esitliginin yanlis eslesme uretme ihtimalini kucultuyor; tamamen yok etmiyor.

**Neden sadece `+=`, duz atama degil:** Once `= MetotAdi` biciminde duz atamayi da kanit saymayi dusundum ama olcumdeki 5 numarali satir bunu bozuyor. `ImageEffectsWindow.axaml.cs:40`'ta `ViewModel.PackagePresetRequested = PackagePresetAsync;` yaziyor ve abone olunan metot ayni dosyada, 100. satirda. Elle inceleme bu satira "gercek sorun" demisti: hedef bir olay degil, `Action<ImageEffectPreset>` tipinde bir alan; orada dogru cozum `Func<T, Task>` kullanmakti. Duz atamayi kanit saysaydim iki yanlis pozitifi kapatirken yeni bir yanlis negatif acacaktim. Iki yanlis pozitifin ikisi de zaten `+=` kullaniyor, yani `+=` tek basina hedefe ulasiyor.

**Neden muafiyet sebebi kaydediliyor:** Kuralin ciktisini belirleyen sey kuralin kendisi degil istisnasi — ShareX'te 106 adayin 96'si istisna ile elendi. Bir metodun bulgu listesinde neden gorunmedigi sorulabilmeli. Bu yuzden muaf tutulan her metot, sebebiyle birlikte (`signature` ya da `subscription`) kaydediliyor ve `check --json` ciktisinda `exemptions` alaninda veriliyor. Alan opsiyonel: hic muafiyet yoksa JSON'a yazilmiyor. Ekran ciktisina hic girmiyor, cunku orasi duzeltilmesi gereken seyleri gostermek icin var; muaf tutulanlar olcum ve hata ayiklama verisi.

**Partial parcalari nasil buluyorum:** Ayni klasordeki `.cs` dosyalarina bakip, icinde ayni adi tasiyan `partial` bir tip bildirimi olanlari parca sayiyorum. Bu bir heuristik. Dosya adina bakmiyorum (`X.cs` / `X.Bir.cs` gibi bir kural yok, ShareX'te `EditorView.axaml.cs` ile `EditorView.Subscriptions.cs` boyle esleniyor ama her repoda esleyecegi garanti degil), namespace'e de bakmiyorum. Ayni klasorde ayni adi tasiyan iki ayri namespace'ten partial sinif olsa ikisini tek sinif sanardim.

**Semantic model gelirse ne degisir:** Bu kademenin tamami ad esitligine dayaniyor; semantic model geldiginde sembol esitligine cevrilir.

- Partial parcalar klasor tahminiyle degil, derleyicinin kendi tip sembolunden bulunur; klasor ve namespace varsayimi tamamen duser.
- `+= MetotAdi` ifadesinin gercekten o metoda mi baglandigi kesin bilinir. O zaman arama kapsamini dar tutmaya gerek kalmaz, cunku 42 tane `OnOpened` birbirinden ayirt edilebilir; kapsam repo geneline acilabilir.
- Sol tarafin gercekten bir `event` mi yoksa delegate tipli bir alan mi oldugu gorulur. Su an `+=` ile `=` arasindaki ayrim bu sorunun kaba bir vekili; semantik bilgi olunca dogrudan sorulabilir ve 5 numaradaki gibi satirlar da dogru siniflanir.
- Imza heuristigi da gereksizlesir: parametrenin gercekten `System.EventArgs`'tan turuyor mu diye bakilabilir, ada bakmak yerine.

**Sonuc:** Olcumdeki iki yanlis pozitif kapaniyor. Ama bunun bir dogrulama olmadigini yazmak lazim: bu duzeltme tam da o 20 satira bakilarak tasarlandi, o yuzden ayni orneklemde %100 precision cikmasi beklenen sey, kanit degil. Kademe 1'in gercekten ise yarayip yaramadigi ancak bu listede olmayan yeni bir orneklemle olculunce anlasilir. Tek yanlis negatif (`EditorInputController.cs:129`) bu kademeden etkilenmiyor, onun icin Kademe 3 gerekiyor.
