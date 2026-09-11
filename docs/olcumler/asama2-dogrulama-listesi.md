# SV001 elle dogrulama listesi

**Tarih:** 2026-09-11
**Repo:** [ShareX/ShareX](https://github.com/ShareX/ShareX) @ `b5a397ea6ccf00659cee593c981be4b01ab641fe`
**Sievert commit:** `2963802` (yeniden yazma oncesi: `3a81e84`)

SV001'in ShareX uzerindeki ciktisindan alinmis bir orneklem. Amac her satiri
elle acip bakmak ve kuralin dogru davranip davranmadigini isaretlemek.
Olcumun tamami [asama2-sv001-sharex.md](asama2-sv001-sharex.md) dosyasinda.

Iki tablo var, cunku iki farkli soru soruyorlar:

- **Bulgular:** kural bunlari `async void` diye isaretledi. Soru: gercekten
  duzeltilmesi gereken bir sey mi, yoksa yanlis pozitif mi?
- **Muaf tutulanlar:** bunlar da `async void` ama iki parametre aliyorlar ve
  ikinci parametrenin tip adi `EventArgs` ile bittigi icin kural onlari
  atladi. Soru: atlamakta hakli miydi, yoksa yanlis negatif mi?

Uc alan bos birakildi, onlari sen dolduracaksin. Her satirin imzasi, govdesinin
ilk 10 satiri, olaya bagli olup olmadigi ve ikinci parametresinin tipi
[asama2-dogrulama-baglami.md](asama2-dogrulama-baglami.md) dosyasinda.

ShareX taramasinda toplam 106 `async void` metot var: 10'u bulgu, 96'si muaf.
Asagidaki 20 satir bunlarin orneklemi: 18 ayri dosyadan, sekiz ayri projeden,
hem cok kisa hem cok uzun metotlar karistirilarak secildi.

## Bulgular (10 / 10 — hepsi listede)

| # | Dosya | Satir | Metot | Uzunluk | GitHub | Gercek sorun mu (E/H) | Neden | Kural nasil duzelmeli |
|---|---|---|---|---|---|---|---|---|
| 1 | ShareX.HelpersLib/UpdateChecker/GitHubUpdateManager.cs | 76 | `TimerCallback` | 4 satir | [link](https://github.com/ShareX/ShareX/blob/b5a397ea6ccf00659cee593c981be4b01ab641fe/ShareX.HelpersLib/UpdateChecker/GitHubUpdateManager.cs#L76) | E | Timer geri cagrisi, olay degil; hata gozlemlenemez. |Duzeltme gerekmez; kural hakli. Mesaj gelistirilebilir: bu bir olay degil, delegate/geri cagri. |
| 2 | ShareX.ImageEditor/Presentation/Controllers/EditorSelectionController.cs | 382 | `ShowImagePickerForReplacement` | 13 satir | [link](https://github.com/ShareX/ShareX/blob/b5a397ea6ccf00659cee593c981be4b01ab641fe/ShareX.ImageEditor/Presentation/Controllers/EditorSelectionController.cs#L382) | E | dogrudan cagriliyor, Task donebilirdi. |Duzeltme gerekmez; kural hakli. |
| 3 | ShareX.ImageEditor/Presentation/Views/EditorView.axaml.cs | 1197 | `AutoCopyImageToClipboard` | 16 satir | [link](https://github.com/ShareX/ShareX/blob/b5a397ea6ccf00659cee593c981be4b01ab641fe/ShareX.ImageEditor/Presentation/Views/EditorView.axaml.cs#L1197) | E | dogrudan cagriliyor, Task donebilirdi. |Duzeltme gerekmez; kural hakli. |
| 4 | ShareX.ImageEditor/Presentation/Views/EditorView.axaml.cs | 2131 | `OnLoadFromUrlRequested` | 39 satir | [link](https://github.com/ShareX/ShareX/blob/b5a397ea6ccf00659cee593c981be4b01ab641fe/ShareX.ImageEditor/Presentation/Views/EditorView.axaml.cs#L2131) | H | vm.LoadFromUrlRequested += ile abone; ozel delegate imzasi istisnaya uymadigi icin kacti. |Kademe 1 — ayni dosya/partial sinifta '+= MetotAdi' aramasi bu satiri kurtarir. |
| 5 | ShareX.ImageEffectsLib/Presentation/ImageEffectsWindow.axaml.cs | 100 | `PackagePresetAsync` | 18 satir | [link](https://github.com/ShareX/ShareX/blob/b5a397ea6ccf00659cee593c981be4b01ab641fe/ShareX.ImageEffectsLib/Presentation/ImageEffectsWindow.axaml.cs#L100) | E | Action<T> alanina atama; olay degil, Func<T,Task> secilebilirdi. |Duzeltme gerekmez; kural hakli. Func<T,Task> onerisi bulgu mesajina eklenebilir. |
| 6 | ShareX/Forms/MainForm.cs | 200 | `HandleHotkeys` | 5 satir | [link](https://github.com/ShareX/ShareX/blob/b5a397ea6ccf00659cee593c981be4b01ab641fe/ShareX/Forms/MainForm.cs#L200) | H | HotkeyTrigger += ile abone; ozel delegate, yanlis pozitif. |Kademe 1 — ayni dosya/partial sinifta '+= MetotAdi' aramasi bu satiri kurtarir. |
| 7 | ShareX/ScreenRecordManager.cs | 46 | `StartStopRecording` | 14 satir | [link](https://github.com/ShareX/ShareX/blob/b5a397ea6ccf00659cee593c981be4b01ab641fe/ShareX/ScreenRecordManager.cs#L46) | E | dogrudan cagriliyor, Task donebilirdi. |Duzeltme gerekmez; kural hakli. |
| 8 | ShareX/TaskHelpers.cs | 875 | `OpenScreenColorPicker` | 27 satir | [link](https://github.com/ShareX/ShareX/blob/b5a397ea6ccf00659cee593c981be4b01ab641fe/ShareX/TaskHelpers.cs#L875) | E | dogrudan cagriliyor, Task donebilirdi. |Duzeltme gerekmez; kural hakli. |
| 9 | ShareX/UploadInfoManager.cs | 429 | `SearchImageUsingGoogleLens` | 7 satir | [link](https://github.com/ShareX/ShareX/blob/b5a397ea6ccf00659cee593c981be4b01ab641fe/ShareX/UploadInfoManager.cs#L429) | E | dogrudan cagriliyor, Task donebilirdi. |Duzeltme gerekmez; kural hakli. |
| 10 | ShareX/UploadInfoManager.cs | 437 | `SearchImageUsingBing` | 7 satir | [link](https://github.com/ShareX/ShareX/blob/b5a397ea6ccf00659cee593c981be4b01ab641fe/ShareX/UploadInfoManager.cs#L437) | E | dogrudan cagriliyor, Task donebilirdi. |Duzeltme gerekmez; kural hakli. |

## Muaf tutulanlar (96'dan 10 ornek)

Bu satirlarda kural bulgu **uretmedi**. "Gercek sorun mu" alani burada
"muaf tutulmasi dogru muydu" anlamina geliyor.

| # | Dosya | Satir | Metot | Uzunluk | GitHub | Gercek sorun mu (E/H) | Neden | Kural nasil duzelmeli |
|---|---|---|---|---|---|---|---|---|
| 11 | ShareX.HelpersLib/Presentation/ColorPicker/ColorPickerWindow.axaml.cs | 371 | `OnCopyHexClick` | 1 satir | [link](https://github.com/ShareX/ShareX/blob/b5a397ea6ccf00659cee593c981be4b01ab641fe/ShareX.HelpersLib/Presentation/ColorPicker/ColorPickerWindow.axaml.cs#L371) | Haklı | gercek abonelik var |Duzeltme gerekmez; muafiyet dogru. |
| 12 | ShareX.HistoryLib/Views/HistoryWindow.axaml.cs | 1084 | `OnWindowKeyDown` | 14 satir | [link](https://github.com/ShareX/ShareX/blob/b5a397ea6ccf00659cee593c981be4b01ab641fe/ShareX.HistoryLib/Views/HistoryWindow.axaml.cs#L1084) | Haklı | gercek abonelik var |Duzeltme gerekmez; muafiyet dogru. |
| 13 | ShareX.ImageEditor/Presentation/Controllers/EditorInputController.cs | 129 | `OnCanvasPointerPressed` | 446 satir | [link](https://github.com/ShareX/ShareX/blob/b5a397ea6ccf00659cee593c981be4b01ab641fe/ShareX.ImageEditor/Presentation/Controllers/EditorInputController.cs#L129) | YANLIS NEGATIF | abonelik yok, EditorView'deki ayni adli void sarmalayici cagiriyor; hata yine kaybolur. |Kademe 3 — semantic model ve sembol esitligi gerekir. Kademe 2 (ad temelli xaml kontrolu) bu satiri duzeltmez, yanlis muafiyeti pekistirir. |
| 14 | ShareX.ImageEditor/Presentation/Views/EditorView.ClipboardHandler.cs | 114 | `OnPasteRequested` | 44 satir | [link](https://github.com/ShareX/ShareX/blob/b5a397ea6ccf00659cee593c981be4b01ab641fe/ShareX.ImageEditor/Presentation/Views/EditorView.ClipboardHandler.cs#L114) | Haklı | gercek abonelik var |Duzeltme gerekmez; muafiyet dogru. |
| 15 | ShareX.ScreenCaptureLib/Presentation/RegionCapture/RegionCaptureWindow.axaml.cs | 441 | `OnOpened` | 54 satir | [link](https://github.com/ShareX/ShareX/blob/b5a397ea6ccf00659cee593c981be4b01ab641fe/ShareX.ScreenCaptureLib/Presentation/RegionCapture/RegionCaptureWindow.axaml.cs#L441) | Haklı | gercek abonelik var |Duzeltme gerekmez; muafiyet dogru. |
| 16 | ShareX.Tools/Tools/Metadata/MetadataWindow.axaml.cs | 87 | `OnDrop` | 14 satir | [link](https://github.com/ShareX/ShareX/blob/b5a397ea6ccf00659cee593c981be4b01ab641fe/ShareX.Tools/Tools/Metadata/MetadataWindow.axaml.cs#L87) | Haklı | gercek abonelik var |Duzeltme gerekmez; muafiyet dogru. |
| 17 | ShareX.UploadersLib/Presentation/Response/ResponseWindow.axaml.cs | 187 | `OnCopyShortenedUrlClick` | 2 satir | [link](https://github.com/ShareX/ShareX/blob/b5a397ea6ccf00659cee593c981be4b01ab641fe/ShareX.UploadersLib/Presentation/Response/ResponseWindow.axaml.cs#L187) | Haklı | gercek abonelik var |Duzeltme gerekmez; muafiyet dogru. |
| 18 | ShareX/Presentation/MainWindow/TrayIconService.cs | 102 | `OnMouseUp` | 33 satir | [link](https://github.com/ShareX/ShareX/blob/b5a397ea6ccf00659cee593c981be4b01ab641fe/ShareX/Presentation/MainWindow/TrayIconService.cs#L102) | Haklı | gercek abonelik var |Duzeltme gerekmez; muafiyet dogru. |
| 19 | ShareX/Presentation/Notification/NotificationWindow.cs | 453 | `OnCardPointerMoved` | 47 satir | [link](https://github.com/ShareX/ShareX/blob/b5a397ea6ccf00659cee593c981be4b01ab641fe/ShareX/Presentation/Notification/NotificationWindow.cs#L453) | Haklı | gercek abonelik var |Duzeltme gerekmez; muafiyet dogru. |
| 20 | ShareX/WatchFolder.cs | 71 | `fileWatcher_Created` | 41 satir | [link](https://github.com/ShareX/ShareX/blob/b5a397ea6ccf00659cee593c981be4b01ab641fe/ShareX/WatchFolder.cs#L71) | Haklı | gercek abonelik var |Duzeltme gerekmez; muafiyet dogru. |

## Orneklem nasil secildi

Bulgularin hepsi listede, 10 tane zaten o kadardi. Muaf tutulanlardan 96 tane
vardi, 10'unu sectim; secerken su uce baktim:

- **Klasor cesitliligi:** 20 satir 18 ayri dosyadan geliyor. Iki dosya ikiser
  kez geciyor: `EditorView.axaml.cs` (1197 ve 2131, arada 900'den fazla satir
  var) ve `UploadInfoManager.cs` (429 ve 437, iki bulgu yan yana duruyor ve
  ikisi de bulgu listesinde olmali). Proje bazinda `HelpersLib`, `HistoryLib`,
  `ImageEditor`, `ImageEffectsLib`, `ScreenCaptureLib`, `Tools`,
  `UploadersLib` ve ana `ShareX` projesinin hepsinden ornek var.
- **Uzunluk cesitliligi:** en kisa 1 satir (`OnCopyHexClick`), en uzun
  446 satir (`OnCanvasPointerPressed`). Arada 2, 4, 5, 7, 13, 14, 16, 18, 27,
  33, 39, 41, 44, 47, 54 satirlik ornekler var.
- **Adlandirma cesitliligi:** muaf tutulanlarin cogu `On...` ile basliyor ama
  20 numaradaki `fileWatcher_Created` eski WinForms adlandirmasini kullaniyor;
  bulgular tarafinda da 4 numaradaki `OnLoadFromUrlRequested` `On...` ile
  basladigi halde bulgu uretmis.

Satir numaralarinin hepsini klondaki dosyalardan tek tek dogruladim, kaydirma
yok.

## Sonuc

### Precision

10 bulgunun 8'i gercek sorun, 2'si yanlis pozitif (4 ve 6).

**Precision = 8/10 = %80.**

Muaf tutulanlardan incelenen 10 ornegin 9'unda istisna hakliydi, 1'i yanlis
negatif (13).

### Uc hatanin tek kok nedeni

4, 6 ve 13 numarali satirlarin ucu de ayni sebepten yanlis: heuristik **imzaya**
bakiyor. Dogru olcut metodun imzasi degil, **bir olaya abone olup olmadigi**.

- 4 ve 6 gercekten olaya abone (`vm.LoadFromUrlRequested +=`, `HotkeyTrigger +=`)
  ama ozel delegate imzalari `(object sender, XEventArgs e)` kalibina uymadigi
  icin istisnaya giremediler ve yanlis pozitif oldular.
- 13 imza kalibina uyuyor, o yuzden muaf tutuldu; ama hicbir olaya abone degil.

### Uc kademeli iyilestirme

**(1) Ayni dosya ya da ayni partial sinifta `+= MetotAdi` varsa muaf tut.**

Bu kademe **4 ve 6 numarali satirlari kurtarir**, yani iki yanlis pozitifin
ikisini de:

- 4: `ShareX.ImageEditor/Presentation/Views/EditorView.Subscriptions.cs:57` —
  `vm.LoadFromUrlRequested += OnLoadFromUrlRequested;` (ayni partial sinif)
- 6: `ShareX/Forms/MainForm.cs:187` —
  `Program.HotkeyManager.HotkeyTrigger += HandleHotkeys;` (ayni dosya)

Precision bu kademeyle %80'den %100'e cikar.

**(2) `.axaml` / `.xaml` ozniteliklerini de abonelik say.**

Bu kademe bu orneklemde yeni bir satir kurtarmiyor, cunku iki yanlis pozitifin
ikisi de koddan abone ediliyor. Kazanci baska yerde: 11, 17 ve 19 numarali
satirlar su an sadece ikinci parametrenin tip adi `EventArgs` ile bittigi icin
muaf tutuluyor; bu kademeden sonra gercek abonelikleri uzerinden muaf
tutulacaklar.

- 11: `ColorPickerWindow.axaml:119` — `Click="OnCopyHexClick"`
- 17: `ResponseWindow.axaml:141` — `Click="OnCopyShortenedUrlClick"`
- 19: `NotificationWindow.axaml:58` — `PointerMoved="OnCardPointerMoved"`

Dikkat: bu kademe tek basina 13'u **duzeltmez**, tersine yanlis muafiyeti
surdurur. `EditorView.axaml` uc yerde `PointerPressed="OnCanvasPointerPressed"`
yaziyor ve ad esitligine bakan bir kontrol bunu 13 numarali metoda baglar.

**(3) Semantic model + sembol esitligi.**

Bu kademe **13 numarali satiri kurtarir**, yani tek yanlis negatifi:

`EditorView.axaml`'deki `PointerPressed="OnCanvasPointerPressed"` aslinda kod
arkasi sinifi `EditorView`'in `EditorView.axaml.cs:1241`'deki metoduna
baglaniyor; o metot da 1243. satirda `_inputController.OnCanvasPointerPressed(sender, e)`
diyerek 13 numaradaki async void metodu cagiriyor. Iki metot ayni adi tasiyor
ama ayri siniflara ait. Bunu ancak sembol esitligi ayirt edebilir; ad
karsilastirmasi ayiramaz.

### Uyari (2026-09-11)

Kademe 1'in bu orneklemde precision'i %100'e cikarmasi bir dogrulama degildir;
duzeltme bu 20 satira bakilarak tasarlandi. Kademe 1 uygulandiktan sonraki
olcum, bu listede olmayan yeni bir orneklemle yapilacaktir.

### Sinirlilik

Abonelik analizi metot adiyla yapilamaz. Ad benzersiz degil: klonda `OnOpened`
**42 ayri yerde**, `OnDrop` **17 ayri yerde** abone ediliyor ve bunlar farkli
siniflarin ayni adli metotlari. Ada bakan bir kontrol, bir sinifta yapilan
aboneligi baska bir sinifin metoduna mal eder. Bu yuzden (1) ve (2) kademeleri
kapsamlarini ayni dosya ya da ayni partial sinifla sinirlamak zorunda; ad
sinirini asan dogru cozum (3).

