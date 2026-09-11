# SV001 dogrulama listesi icin kod baglami

**Tarih:** 2026-09-11  
**Repo:** [ShareX/ShareX](https://github.com/ShareX/ShareX) @ `b5a397ea6ccf00659cee593c981be4b01ab641fe`  
**Sievert commit:** `f7dda08`

[asama2-dogrulama-listesi.md](asama2-dogrulama-listesi.md) dosyasindaki 20 satirin
her biri icin klondan toplanan olgular. Karar yok, yorum yok; sadece ne yazdigi.

Her madde dort sey iceriyor: metodun tam imzasi, govdesinden ilk 10 satir, metodun
bir olaya bagli olup olmadigi, ve ikinci parametrenin tipinin nerede tanimlandigi.

Abonelik aramasi klonun tamaminda yapildi: `.cs` dosyalarinda `+= MetotAdi` ve
`AddHandler(..., MetotAdi)` kaliplari, `.axaml` / `.xaml` dosyalarinda
`Oznitelik="MetotAdi"` kaliplari. Klonda `.Designer.cs` dosyasi yok, o yuzden o
kaynaktan abonelik cikmadi. Metot adlari benzersiz degil (ornegin `OnOpened` 42
ayri yerde abone ediliyor), bu yuzden ayni dosyada veya ayni partial sinifta olan
abonelikler ayirt edildi; baska siniflara ait ayni adli abonelikler sayi olarak
belirtildi.

## Bulgular (1-10)

### 1. `TimerCallback`
`ShareX.HelpersLib/UpdateChecker/GitHubUpdateManager.cs:76` · [GitHub](https://github.com/ShareX/ShareX/blob/b5a397ea6ccf00659cee593c981be4b01ab641fe/ShareX.HelpersLib/UpdateChecker/GitHubUpdateManager.cs#L76)

**Imza**

```csharp
private async void TimerCallback(object state)
```

**Govde** (blok govdeli, bos satirlar haric 1 satir)

```csharp
await CheckUpdate();
```

**Olaya bagli mi**

Abonelik bulunamadi. `GitHubUpdateManager.cs:66` satirinda `updateTimer = new Timer(TimerCallback, null, TimeSpan.Zero, UpdateCheckInterval);` seklinde `System.Threading.Timer` callback'i olarak geciriliyor. C# event aboneligi (`+=`) degil.

**Ikinci parametrenin tipi**

Ikinci parametre yok. Parametre listesi: `object state`

### 2. `ShowImagePickerForReplacement`
`ShareX.ImageEditor/Presentation/Controllers/EditorSelectionController.cs:382` · [GitHub](https://github.com/ShareX/ShareX/blob/b5a397ea6ccf00659cee593c981be4b01ab641fe/ShareX.ImageEditor/Presentation/Controllers/EditorSelectionController.cs#L382)

**Imza**

```csharp
private async void ShowImagePickerForReplacement(global::Avalonia.Controls.Image imageControl, ImageAnnotation imageAnnotation)
```

**Govde** (blok govdeli, bos satirlar haric 9 satir)

```csharp
_selectedShape = imageControl;
UpdateBoundsObserver();
UpdateSelectionHandles();
SelectionChanged?.Invoke(true);
bool wasReplaced = await _view.ReplaceImageAnnotationFromFilePickerAsync(imageAnnotation, imageControl);
if (wasReplaced)
{
    UpdateSelectionHandles();
}
```

**Olaya bagli mi**

Abonelik bulunamadi. `EditorSelectionController.cs:346` satirindan dogrudan cagriliyor: `ShowImagePickerForReplacement(imageControl, imageAnnotation);`

**Ikinci parametrenin tipi**

`ImageAnnotation` ShareX icinde tanimli: `ShareX.ImageEditor/Core/Annotations/Shapes/ImageAnnotation.cs:33` -> `public partial class ImageAnnotation : Annotation, IDisposable`. Taban sinif `Annotation`, o da `ShareX.ImageEditor/Core/Annotations/Base/Annotation.cs:52`'de `public abstract class Annotation` olarak tanimli, taban sinif listesi bos.

### 3. `AutoCopyImageToClipboard`
`ShareX.ImageEditor/Presentation/Views/EditorView.axaml.cs:1197` · [GitHub](https://github.com/ShareX/ShareX/blob/b5a397ea6ccf00659cee593c981be4b01ab641fe/ShareX.ImageEditor/Presentation/Views/EditorView.axaml.cs#L1197)

**Imza**

```csharp
private async void AutoCopyImageToClipboard(MainViewModel vm)
```

**Govde** (blok govdeli, bos satirlar haric 12 satir, ilk 10'u asagida)

```csharp
if (_isWorkspaceHostMode || !vm.Options.AutoCopyImageToClipboard || !vm.HasPreviewImage)
{
    return;
}
try
{
    await vm.RequestCopyToClipboardAsync();
}
catch (Exception ex)
{
```

**Olaya bagli mi**

Abonelik bulunamadi. `EditorView.axaml.cs:1193` satirindan cagriliyor; cagiran metot `QueueAutoCopyImageToClipboard` (ayni dosya, 1177).

**Ikinci parametrenin tipi**

Ikinci parametre yok. Parametre listesi: `MainViewModel vm`

### 4. `OnLoadFromUrlRequested`
`ShareX.ImageEditor/Presentation/Views/EditorView.axaml.cs:2131` · [GitHub](https://github.com/ShareX/ShareX/blob/b5a397ea6ccf00659cee593c981be4b01ab641fe/ShareX.ImageEditor/Presentation/Views/EditorView.axaml.cs#L2131)

**Imza**

```csharp
private async void OnLoadFromUrlRequested(object? sender, string url)
```

**Govde** (blok govdeli, bos satirlar haric 30 satir, ilk 10'u asagida)

```csharp
if (DataContext is not MainViewModel vm) return;
StartScreenDialogViewModel? startScreenDialog = vm.ModalContent as StartScreenDialogViewModel;
startScreenDialog?.ClearStatus();
startScreenDialog?.SetUrlLoading(true);
try
{
    using var httpClient = new HttpClient();
    httpClient.Timeout = TimeSpan.FromSeconds(30);
    httpClient.DefaultRequestHeaders.Add("User-Agent", "ShareX");
    var response = await httpClient.GetAsync(url);
```

**Olaya bagli mi**

`ShareX.ImageEditor/Presentation/Views/EditorView.Subscriptions.cs:57` — `vm.LoadFromUrlRequested += OnLoadFromUrlRequested;` - `EditorView` partial sinifinin baska bir parcasi, yani ayni sinif.

**Ikinci parametrenin tipi**

`string` - framework tipi.

### 5. `PackagePresetAsync`
`ShareX.ImageEffectsLib/Presentation/ImageEffectsWindow.axaml.cs:100` · [GitHub](https://github.com/ShareX/ShareX/blob/b5a397ea6ccf00659cee593c981be4b01ab641fe/ShareX.ImageEffectsLib/Presentation/ImageEffectsWindow.axaml.cs#L100)

**Imza**

```csharp
private async void PackagePresetAsync(ImageEffectPreset preset)
```

**Govde** (blok govdeli, bos satirlar haric 14 satir, ilk 10'u asagida)

```csharp
if (string.IsNullOrWhiteSpace(preset.Name))
{
    return;
}
try
{
    string json = JsonHelpers.SerializeToString(preset, serializationBinder: _serializationBinder);
    string effectsFolder = HelpersOptions.ShareXSpecialFolders["ShareXImageEffects"];
    await new ImageEffectPackagerWindow(json, preset.Name, effectsFolder).ShowDialog(this);
}
```

**Olaya bagli mi**

Abonelik bulunamadi. `ImageEffectsWindow.axaml.cs:40` satirinda delegate alanina ataniyor: `ViewModel.PackagePresetRequested = PackagePresetAsync;`. Hedef `ImageEffectsViewModel.cs:111`'de `public Action<ImageEffectPreset>? PackagePresetRequested { get; set; }`, `ImageEffectsViewModel.cs:352`'de `PackagePresetRequested?.Invoke(...)` ile cagriliyor. `+=` degil, duz atama.

**Ikinci parametrenin tipi**

Ikinci parametre yok. Parametre listesi: `ImageEffectPreset preset`

### 6. `HandleHotkeys`
`ShareX/Forms/MainForm.cs:200` · [GitHub](https://github.com/ShareX/ShareX/blob/b5a397ea6ccf00659cee593c981be4b01ab641fe/ShareX/Forms/MainForm.cs#L200)

**Imza**

```csharp
private async void HandleHotkeys(HotkeySettings hotkeySetting)
```

**Govde** (blok govdeli, bos satirlar haric 2 satir)

```csharp
DebugHelper.WriteLine("Hotkey triggered. " + hotkeySetting);
await TaskHelpers.ExecuteJob(hotkeySetting.TaskSettings);
```

**Olaya bagli mi**

`ShareX/Forms/MainForm.cs:187` — `Program.HotkeyManager.HotkeyTrigger += HandleHotkeys;`

**Ikinci parametrenin tipi**

Ikinci parametre yok. Parametre listesi: `HotkeySettings hotkeySetting`

### 7. `StartStopRecording`
`ShareX/ScreenRecordManager.cs:46` · [GitHub](https://github.com/ShareX/ShareX/blob/b5a397ea6ccf00659cee593c981be4b01ab641fe/ShareX/ScreenRecordManager.cs#L46)

**Imza**

```csharp
public static async void StartStopRecording(ScreenRecordOutput outputType, ScreenRecordStartMethod startMethod, TaskSettings taskSettings)
```

**Govde** (blok govdeli, bos satirlar haric 11 satir, ilk 10'u asagida)

```csharp
if (IsRecording)
{
    if (recordForm != null && !recordForm.IsDisposed)
    {
        recordForm.StartStopRecording();
    }
}
else
{
    await StartRecording(outputType, taskSettings, startMethod);
```

**Olaya bagli mi**

Abonelik bulunamadi. `ScreenRecordManager.cs:52` satirinda ayni adli baska bir uye cagriliyor: `recordForm.StartStopRecording();`

**Ikinci parametrenin tipi**

`ScreenRecordStartMethod` ShareX icinde tanimli: `ShareX/Enums.cs:191` -> `public enum ScreenRecordStartMethod`. Enum, taban sinifi yok.

### 8. `OpenScreenColorPicker`
`ShareX/TaskHelpers.cs:875` · [GitHub](https://github.com/ShareX/ShareX/blob/b5a397ea6ccf00659cee593c981be4b01ab641fe/ShareX/TaskHelpers.cs#L875)

**Imza**

```csharp
public static async void OpenScreenColorPicker(TaskSettings taskSettings = null)
```

**Govde** (blok govdeli, bos satirlar haric 20 satir, ilk 10'u asagida)

```csharp
if (taskSettings == null) taskSettings = TaskSettings.GetDefaultTaskSettings();
ScreenColorPickerOptions options = taskSettings.ToolsSettingsReference.ScreenColorPickerOptions;
ScreenColorPickerResult result = await ToolsIntegration.PickScreenColorAsync(options);
if (result != null)
{
    string input = result.ControlPressed ? options.FormatCtrl : options.Format;
    if (!string.IsNullOrEmpty(input))
    {
        Color color = Color.FromArgb(result.Color.A, result.Color.R, result.Color.G, result.Color.B);
        Point position = new Point(result.Position.X, result.Position.Y);
```

**Olaya bagli mi**

Abonelik bulunamadi. `TaskHelpers.cs:195` satirindan dogrudan cagriliyor: `OpenScreenColorPicker(safeTaskSettings);`

**Ikinci parametrenin tipi**

Ikinci parametre yok. Parametre listesi: `TaskSettings taskSettings = null`

### 9. `SearchImageUsingGoogleLens`
`ShareX/UploadInfoManager.cs:429` · [GitHub](https://github.com/ShareX/ShareX/blob/b5a397ea6ccf00659cee593c981be4b01ab641fe/ShareX/UploadInfoManager.cs#L429)

**Imza**

```csharp
public async void SearchImageUsingGoogleLens()
```

**Govde** (blok govdeli, bos satirlar haric 4 satir)

```csharp
if (IsItemSelected && SelectedItem.IsURLExist)
{
    await TaskHelpers.SearchImageUsingGoogleLensAsync(SelectedItem.Info.Result.URL);
}
```

**Olaya bagli mi**

Abonelik bulunamadi. `UploadInfoManager.cs:433` satirinda govdesi `TaskHelpers.SearchImageUsingGoogleLensAsync(...)` cagiriyor. Metodun kendisine abonelik yok.

**Ikinci parametrenin tipi**

Ikinci parametre yok. Parametre listesi: `(bos)`

### 10. `SearchImageUsingBing`
`ShareX/UploadInfoManager.cs:437` · [GitHub](https://github.com/ShareX/ShareX/blob/b5a397ea6ccf00659cee593c981be4b01ab641fe/ShareX/UploadInfoManager.cs#L437)

**Imza**

```csharp
public async void SearchImageUsingBing()
```

**Govde** (blok govdeli, bos satirlar haric 4 satir)

```csharp
if (IsItemSelected && SelectedItem.IsURLExist)
{
    await TaskHelpers.SearchImageUsingBingAsync(SelectedItem.Info.Result.URL);
}
```

**Olaya bagli mi**

Abonelik bulunamadi. `UploadInfoManager.cs:441` satirinda govdesi `TaskHelpers.SearchImageUsingBingAsync(...)` cagiriyor. Metodun kendisine abonelik yok.

**Ikinci parametrenin tipi**

Ikinci parametre yok. Parametre listesi: `(bos)`

## Muaf tutulanlar (11-20)

### 11. `OnCopyHexClick`
`ShareX.HelpersLib/Presentation/ColorPicker/ColorPickerWindow.axaml.cs:371` · [GitHub](https://github.com/ShareX/ShareX/blob/b5a397ea6ccf00659cee593c981be4b01ab641fe/ShareX.HelpersLib/Presentation/ColorPicker/ColorPickerWindow.axaml.cs#L371)

**Imza**

```csharp
private async void OnCopyHexClick(object? sender, RoutedEventArgs e)
```

**Govde** (ifade govdeli, bos satirlar haric 1 satir)

```csharp
=> await CopyAsync("#" + ColorHelpers.ColorToHex(_currentColor));
```

**Olaya bagli mi**

`ShareX.HelpersLib/Presentation/ColorPicker/ColorPickerWindow.axaml:119` — `<MenuItem Header="..." Click="OnCopyHexClick"/>` - esli axaml dosyasi.

**Ikinci parametrenin tipi**

`RoutedEventArgs` ShareX klonunda tanimli degil, framework tipi. Klonda tanimi olmadigi icin taban sinifi buradan dogrulanamadi.

### 12. `OnWindowKeyDown`
`ShareX.HistoryLib/Views/HistoryWindow.axaml.cs:1084` · [GitHub](https://github.com/ShareX/ShareX/blob/b5a397ea6ccf00659cee593c981be4b01ab641fe/ShareX.HistoryLib/Views/HistoryWindow.axaml.cs#L1084)

**Imza**

```csharp
private async void OnWindowKeyDown(object? sender, KeyEventArgs e)
```

**Govde** (blok govdeli, bos satirlar haric 11 satir, ilk 10'u asagida)

```csharp
if (e.Key == Key.F5)
{
    e.Handled = true;
    await RefreshHistoryAsync();
}
else if (e.Key == Key.Escape)
{
    e.Handled = true;
    if (ModalOverlay.IsVisible) CloseModal();
    else Close();
```

**Olaya bagli mi**

`ShareX.HistoryLib/Views/HistoryWindow.axaml.cs:103` — `KeyDown += OnWindowKeyDown;` - ayni dosya. Klonda ayni adli 8 abonelik daha var, onlar baska siniflarin `OnWindowKeyDown` metotlari.

**Ikinci parametrenin tipi**

`KeyEventArgs` ShareX klonunda tanimli degil, framework tipi. Klonda tanimi olmadigi icin taban sinifi buradan dogrulanamadi.

### 13. `OnCanvasPointerPressed`
`ShareX.ImageEditor/Presentation/Controllers/EditorInputController.cs:129` · [GitHub](https://github.com/ShareX/ShareX/blob/b5a397ea6ccf00659cee593c981be4b01ab641fe/ShareX.ImageEditor/Presentation/Controllers/EditorInputController.cs#L129)

**Imza**

```csharp
public async void OnCanvasPointerPressed(object? sender, PointerPressedEventArgs e)
```

**Govde** (blok govdeli, bos satirlar haric 401 satir, ilk 10'u asagida)

```csharp
var vm = ViewModel;
if (vm == null) return;
var canvas = _view.FindControl<Canvas>("AnnotationCanvas") ?? sender as Canvas;
if (canvas == null) return;
var props = e.GetCurrentPoint(canvas).Properties;
if (props.IsMiddleButtonPressed)
{
    _zoomController.OnScrollViewerPointerPressed(_view.FindControl<ScrollViewer>("CanvasScrollViewer"), e);
    return;
}
```

**Olaya bagli mi**

`ShareX.ImageEditor/Presentation/Views/EditorView.axaml:507, :588, :666` — `PointerPressed="OnCanvasPointerPressed"` uc yerde. Ancak axaml, kod arkasi sinifi `EditorView`'e baglaniyor ve `EditorView.axaml.cs:1241`'de ayni adli ikinci bir metot var: `private void OnCanvasPointerPressed(object? sender, PointerPressedEventArgs e)` (async degil). O metot 1243. satirda `_inputController.OnCanvasPointerPressed(sender, e);` diyerek buradaki async void metodu cagiriyor. Yani bu metoda dogrudan abonelik yok, araya `EditorView` giriyor.

**Ikinci parametrenin tipi**

`PointerPressedEventArgs` ShareX klonunda tanimli degil, framework tipi. Klonda tanimi olmadigi icin taban sinifi buradan dogrulanamadi.

### 14. `OnPasteRequested`
`ShareX.ImageEditor/Presentation/Views/EditorView.ClipboardHandler.cs:114` · [GitHub](https://github.com/ShareX/ShareX/blob/b5a397ea6ccf00659cee593c981be4b01ab641fe/ShareX.ImageEditor/Presentation/Views/EditorView.ClipboardHandler.cs#L114)

**Imza**

```csharp
private async void OnPasteRequested(object? sender, EventArgs e)
```

**Govde** (blok govdeli, bos satirlar haric 38 satir, ilk 10'u asagida)

```csharp
var topLevel = TopLevel.GetTopLevel(this);
if (topLevel == null) return;
var clipboard = topLevel.Clipboard;
try
{
    // Priority 1: Check system clipboard for images/files (external content)
    // This allows users to copy from browser/explorer and paste even if they previously copied a shape
    if (clipboard != null)
    {
        // Check for files
```

**Olaya bagli mi**

`ShareX.ImageEditor/Presentation/Views/EditorView.Subscriptions.cs:47` — `vm.PasteRequested += OnPasteRequested;` - `EditorView` partial sinifinin baska bir parcasi, yani ayni sinif.

**Ikinci parametrenin tipi**

`EventArgs` ShareX klonunda tanimli degil, framework tipi. Klonda tanimi olmadigi icin taban sinifi buradan dogrulanamadi.

### 15. `OnOpened`
`ShareX.ScreenCaptureLib/Presentation/RegionCapture/RegionCaptureWindow.axaml.cs:441` · [GitHub](https://github.com/ShareX/ShareX/blob/b5a397ea6ccf00659cee593c981be4b01ab641fe/ShareX.ScreenCaptureLib/Presentation/RegionCapture/RegionCaptureWindow.axaml.cs#L441)

**Imza**

```csharp
private async void OnOpened(object? sender, EventArgs e)
```

**Govde** (blok govdeli, bos satirlar haric 44 satir, ilk 10'u asagida)

```csharp
try
{
    if (_request == null || _viewModel == null)
    {
        CancelCapture();
        return;
    }
    ApplyPixelSize();
    UpdatePixelTransforms();
    _editorWorkspace.ConfigureForFullscreenWorkspace();
```

**Olaya bagli mi**

`ShareX.ScreenCaptureLib/Presentation/RegionCapture/RegionCaptureWindow.axaml.cs:121` — `Opened += OnOpened;` - ayni dosya. Klonda ayni adli 41 abonelik daha var, onlar baska pencerelerin `OnOpened` metotlari.

**Ikinci parametrenin tipi**

`EventArgs` ShareX klonunda tanimli degil, framework tipi. Klonda tanimi olmadigi icin taban sinifi buradan dogrulanamadi.

### 16. `OnDrop`
`ShareX.Tools/Tools/Metadata/MetadataWindow.axaml.cs:87` · [GitHub](https://github.com/ShareX/ShareX/blob/b5a397ea6ccf00659cee593c981be4b01ab641fe/ShareX.Tools/Tools/Metadata/MetadataWindow.axaml.cs#L87)

**Imza**

```csharp
private async void OnDrop(object? sender, DragEventArgs e)
```

**Govde** (blok govdeli, bos satirlar haric 10 satir)

```csharp
if (_viewModel.IsBusy)
{
    return;
}
IStorageFile? file = e.DataTransfer.TryGetFiles()?.OfType<IStorageFile>().FirstOrDefault();
if (file != null)
{
    await _viewModel.OpenFileAsync(file.Path.LocalPath);
    e.Handled = true;
}
```

**Olaya bagli mi**

`ShareX.Tools/Tools/Metadata/MetadataWindow.axaml.cs:44` — `AddHandler(DragDrop.DropEvent, OnDrop);` - ayni dosya, `+=` yerine Avalonia'nin `AddHandler` cagrisi. Klonda ayni adli 16 abonelik daha var, onlar baska siniflarin `OnDrop` metotlari.

**Ikinci parametrenin tipi**

`DragEventArgs` ShareX klonunda tanimli degil, framework tipi. Klonda tanimi olmadigi icin taban sinifi buradan dogrulanamadi.

### 17. `OnCopyShortenedUrlClick`
`ShareX.UploadersLib/Presentation/Response/ResponseWindow.axaml.cs:187` · [GitHub](https://github.com/ShareX/ShareX/blob/b5a397ea6ccf00659cee593c981be4b01ab641fe/ShareX.UploadersLib/Presentation/Response/ResponseWindow.axaml.cs#L187)

**Imza**

```csharp
private async void OnCopyShortenedUrlClick(object? sender, RoutedEventArgs e)
```

**Govde** (ifade govdeli, bos satirlar haric 2 satir)

```csharp
=>
        await CopyTextAsync(Result.ShortenedURL);
```

**Olaya bagli mi**

`ShareX.UploadersLib/Presentation/Response/ResponseWindow.axaml:141` — `Click="OnCopyShortenedUrlClick">` - esli axaml dosyasi.

**Ikinci parametrenin tipi**

`RoutedEventArgs` ShareX klonunda tanimli degil, framework tipi. Klonda tanimi olmadigi icin taban sinifi buradan dogrulanamadi.

### 18. `OnMouseUp`
`ShareX/Presentation/MainWindow/TrayIconService.cs:102` · [GitHub](https://github.com/ShareX/ShareX/blob/b5a397ea6ccf00659cee593c981be4b01ab641fe/ShareX/Presentation/MainWindow/TrayIconService.cs#L102)

**Imza**

```csharp
private async void OnMouseUp(object? sender, MouseEventArgs e)
```

**Govde** (blok govdeli, bos satirlar haric 29 satir, ilk 10'u asagida)

```csharp
switch (e.Button)
{
    case MouseButtons.Left:
        if (Program.Settings.TrayLeftDoubleClickAction == HotkeyType.None)
        {
            await TaskHelpers.ExecuteJob(Program.Settings.TrayLeftClickAction);
        }
        else
        {
            _leftClickCount++;
```

**Olaya bagli mi**

`ShareX/Presentation/MainWindow/TrayIconService.cs:67` — `_notifyIcon.MouseUp += OnMouseUp;`

**Ikinci parametrenin tipi**

`MouseEventArgs` ShareX klonunda tanimli degil, framework tipi. Klonda tanimi olmadigi icin taban sinifi buradan dogrulanamadi.

### 19. `OnCardPointerMoved`
`ShareX/Presentation/Notification/NotificationWindow.cs:453` · [GitHub](https://github.com/ShareX/ShareX/blob/b5a397ea6ccf00659cee593c981be4b01ab641fe/ShareX/Presentation/Notification/NotificationWindow.cs#L453)

**Imza**

```csharp
private async void OnCardPointerMoved(object? sender, PointerEventArgs e)
```

**Govde** (blok govdeli, bos satirlar haric 39 satir, ilk 10'u asagida)

```csharp
if (!_isDragCandidate || _dragEvent == null || _config == null ||
    string.IsNullOrEmpty(_config.FilePath) || !File.Exists(_config.FilePath))
{
    return;
}
Point current = e.GetPosition(NotificationCard);
if (Math.Abs(current.X - _dragStart.X) < 20 && Math.Abs(current.Y - _dragStart.Y) < 20)
{
    return;
}
```

**Olaya bagli mi**

`ShareX/Presentation/Notification/NotificationWindow.axaml:58` — `PointerMoved="OnCardPointerMoved"` - `NotificationWindow` partial sinifinin axaml dosyasi.

**Ikinci parametrenin tipi**

`PointerEventArgs` ShareX klonunda tanimli degil, framework tipi. Klonda tanimi olmadigi icin taban sinifi buradan dogrulanamadi.

### 20. `fileWatcher_Created`
`ShareX/WatchFolder.cs:71` · [GitHub](https://github.com/ShareX/ShareX/blob/b5a397ea6ccf00659cee593c981be4b01ab641fe/ShareX/WatchFolder.cs#L71)

**Imza**

```csharp
private async void fileWatcher_Created(object sender, FileSystemEventArgs e)
```

**Govde** (blok govdeli, bos satirlar haric 30 satir, ilk 10'u asagida)

```csharp
CleanElapsedTimers();
string path = e.FullPath;
foreach (WatchFolderDuplicateEventTimer timer in timers)
{
    if (timer.IsDuplicateEvent(path))
    {
        return;
    }
}
timers.Add(new WatchFolderDuplicateEventTimer(path));
```

**Olaya bagli mi**

`ShareX/WatchFolder.cs:61` — `fileWatcher.Created += fileWatcher_Created;`

**Ikinci parametrenin tipi**

`FileSystemEventArgs` ShareX klonunda tanimli degil, framework tipi. Klonda tanimi olmadigi icin taban sinifi buradan dogrulanamadi.
