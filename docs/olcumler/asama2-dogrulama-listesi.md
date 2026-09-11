# SV001 elle dogrulama listesi

**Tarih:** 2026-09-11
**Repo:** [ShareX/ShareX](https://github.com/ShareX/ShareX) @ `b5a397ea6ccf00659cee593c981be4b01ab641fe`
**Sievert commit:** `3a81e84`

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
| 1 | ShareX.HelpersLib/UpdateChecker/GitHubUpdateManager.cs | 76 | `TimerCallback` | 4 satir | [link](https://github.com/ShareX/ShareX/blob/b5a397ea6ccf00659cee593c981be4b01ab641fe/ShareX.HelpersLib/UpdateChecker/GitHubUpdateManager.cs#L76) | | | |
| 2 | ShareX.ImageEditor/Presentation/Controllers/EditorSelectionController.cs | 382 | `ShowImagePickerForReplacement` | 13 satir | [link](https://github.com/ShareX/ShareX/blob/b5a397ea6ccf00659cee593c981be4b01ab641fe/ShareX.ImageEditor/Presentation/Controllers/EditorSelectionController.cs#L382) | | | |
| 3 | ShareX.ImageEditor/Presentation/Views/EditorView.axaml.cs | 1197 | `AutoCopyImageToClipboard` | 16 satir | [link](https://github.com/ShareX/ShareX/blob/b5a397ea6ccf00659cee593c981be4b01ab641fe/ShareX.ImageEditor/Presentation/Views/EditorView.axaml.cs#L1197) | | | |
| 4 | ShareX.ImageEditor/Presentation/Views/EditorView.axaml.cs | 2131 | `OnLoadFromUrlRequested` | 39 satir | [link](https://github.com/ShareX/ShareX/blob/b5a397ea6ccf00659cee593c981be4b01ab641fe/ShareX.ImageEditor/Presentation/Views/EditorView.axaml.cs#L2131) | | | |
| 5 | ShareX.ImageEffectsLib/Presentation/ImageEffectsWindow.axaml.cs | 100 | `PackagePresetAsync` | 18 satir | [link](https://github.com/ShareX/ShareX/blob/b5a397ea6ccf00659cee593c981be4b01ab641fe/ShareX.ImageEffectsLib/Presentation/ImageEffectsWindow.axaml.cs#L100) | | | |
| 6 | ShareX/Forms/MainForm.cs | 200 | `HandleHotkeys` | 5 satir | [link](https://github.com/ShareX/ShareX/blob/b5a397ea6ccf00659cee593c981be4b01ab641fe/ShareX/Forms/MainForm.cs#L200) | | | |
| 7 | ShareX/ScreenRecordManager.cs | 46 | `StartStopRecording` | 14 satir | [link](https://github.com/ShareX/ShareX/blob/b5a397ea6ccf00659cee593c981be4b01ab641fe/ShareX/ScreenRecordManager.cs#L46) | | | |
| 8 | ShareX/TaskHelpers.cs | 875 | `OpenScreenColorPicker` | 27 satir | [link](https://github.com/ShareX/ShareX/blob/b5a397ea6ccf00659cee593c981be4b01ab641fe/ShareX/TaskHelpers.cs#L875) | | | |
| 9 | ShareX/UploadInfoManager.cs | 429 | `SearchImageUsingGoogleLens` | 7 satir | [link](https://github.com/ShareX/ShareX/blob/b5a397ea6ccf00659cee593c981be4b01ab641fe/ShareX/UploadInfoManager.cs#L429) | | | |
| 10 | ShareX/UploadInfoManager.cs | 437 | `SearchImageUsingBing` | 7 satir | [link](https://github.com/ShareX/ShareX/blob/b5a397ea6ccf00659cee593c981be4b01ab641fe/ShareX/UploadInfoManager.cs#L437) | | | |

## Muaf tutulanlar (96'dan 10 ornek)

Bu satirlarda kural bulgu **uretmedi**. "Gercek sorun mu" alani burada
"muaf tutulmasi dogru muydu" anlamina geliyor.

| # | Dosya | Satir | Metot | Uzunluk | GitHub | Gercek sorun mu (E/H) | Neden | Kural nasil duzelmeli |
|---|---|---|---|---|---|---|---|---|
| 11 | ShareX.HelpersLib/Presentation/ColorPicker/ColorPickerWindow.axaml.cs | 371 | `OnCopyHexClick` | 1 satir | [link](https://github.com/ShareX/ShareX/blob/b5a397ea6ccf00659cee593c981be4b01ab641fe/ShareX.HelpersLib/Presentation/ColorPicker/ColorPickerWindow.axaml.cs#L371) | | | |
| 12 | ShareX.HistoryLib/Views/HistoryWindow.axaml.cs | 1084 | `OnWindowKeyDown` | 14 satir | [link](https://github.com/ShareX/ShareX/blob/b5a397ea6ccf00659cee593c981be4b01ab641fe/ShareX.HistoryLib/Views/HistoryWindow.axaml.cs#L1084) | | | |
| 13 | ShareX.ImageEditor/Presentation/Controllers/EditorInputController.cs | 129 | `OnCanvasPointerPressed` | 446 satir | [link](https://github.com/ShareX/ShareX/blob/b5a397ea6ccf00659cee593c981be4b01ab641fe/ShareX.ImageEditor/Presentation/Controllers/EditorInputController.cs#L129) | | | |
| 14 | ShareX.ImageEditor/Presentation/Views/EditorView.ClipboardHandler.cs | 114 | `OnPasteRequested` | 44 satir | [link](https://github.com/ShareX/ShareX/blob/b5a397ea6ccf00659cee593c981be4b01ab641fe/ShareX.ImageEditor/Presentation/Views/EditorView.ClipboardHandler.cs#L114) | | | |
| 15 | ShareX.ScreenCaptureLib/Presentation/RegionCapture/RegionCaptureWindow.axaml.cs | 441 | `OnOpened` | 54 satir | [link](https://github.com/ShareX/ShareX/blob/b5a397ea6ccf00659cee593c981be4b01ab641fe/ShareX.ScreenCaptureLib/Presentation/RegionCapture/RegionCaptureWindow.axaml.cs#L441) | | | |
| 16 | ShareX.Tools/Tools/Metadata/MetadataWindow.axaml.cs | 87 | `OnDrop` | 14 satir | [link](https://github.com/ShareX/ShareX/blob/b5a397ea6ccf00659cee593c981be4b01ab641fe/ShareX.Tools/Tools/Metadata/MetadataWindow.axaml.cs#L87) | | | |
| 17 | ShareX.UploadersLib/Presentation/Response/ResponseWindow.axaml.cs | 187 | `OnCopyShortenedUrlClick` | 2 satir | [link](https://github.com/ShareX/ShareX/blob/b5a397ea6ccf00659cee593c981be4b01ab641fe/ShareX.UploadersLib/Presentation/Response/ResponseWindow.axaml.cs#L187) | | | |
| 18 | ShareX/Presentation/MainWindow/TrayIconService.cs | 102 | `OnMouseUp` | 33 satir | [link](https://github.com/ShareX/ShareX/blob/b5a397ea6ccf00659cee593c981be4b01ab641fe/ShareX/Presentation/MainWindow/TrayIconService.cs#L102) | | | |
| 19 | ShareX/Presentation/Notification/NotificationWindow.cs | 453 | `OnCardPointerMoved` | 47 satir | [link](https://github.com/ShareX/ShareX/blob/b5a397ea6ccf00659cee593c981be4b01ab641fe/ShareX/Presentation/Notification/NotificationWindow.cs#L453) | | | |
| 20 | ShareX/WatchFolder.cs | 71 | `fileWatcher_Created` | 41 satir | [link](https://github.com/ShareX/ShareX/blob/b5a397ea6ccf00659cee593c981be4b01ab641fe/ShareX/WatchFolder.cs#L71) | | | |

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
