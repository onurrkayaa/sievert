# SV001-SV006 elle dogrulama listesi

**Tarih:** 2026-09-11
**Repo:** [jellyfin/jellyfin](https://github.com/jellyfin/jellyfin) @ `1d7b6d97844c8cc848ed3fb5c4b48bb9cdd5b139`
**Sievert:** `f8906a4` (yeniden yazma oncesi: `5fc4846`)

Jellyfin taramasindan alinmis 30 satirlik bir orneklem: her kuraldan 5 bulgu. Amac her
satiri elle acip bakmak ve kuralin dogru davranip davranmadigini isaretlemek. Olcumun
tamami [asama3-jellyfin.md](asama3-jellyfin.md) dosyasinda.

Uc alan siniflandirma icin ayrildi ve dolduruldu:

- **Gercek sorun mu (E/H):** bu gercekten duzeltilmesi gereken bir sey mi, yoksa yanlis
  pozitif mi?
- **Neden:** kararin gerekcesi.
- **Kural nasil duzelmeli:** bulgu yanlissa kuralda ne degismeliydi.

**Yontem:** 30 bulgu kaynak koda bakilarak yazar tarafindan siniflandirildi; bagimsiz bir
degerlendirici kullanilmadi. Kuralin yazari ile bulgulari degerlendiren ayni kisi oldugu
icin bu bir yanlilik kaynagi; `sinirliliklar.md`'de madde olarak duruyor.

Jellyfin daha once hic taranmadi ve kurallarin hicbiri bu repoya bakilarak tasarlanmadi.
Asama 2'deki SV001 olcumunden farki bu: orada duzeltme, olcumun yapildigi orneklemin
uzerine tasarlanmisti ve o yuzden sonraki sayi bir dogrulama sayilmiyordu. Burada boyle
bir sorun yok.

## Orneklem nasil secildi

Her kuraldan 5 bulgu alindi. Secerken uzunluk yelpazesine bakildi: her kuralin bulgu
havuzu metot uzunluguna gore siralanip en kisa, %25, %50, %75 ve en uzun noktalardan
birer ornek secildi; her seferinde daha once secilmemis bir dosya, mumkunse daha once
secilmemis bir proje tercih edildi.

Sonuc: **30 bulgu, 30 ayri dosya.** Metot uzunluklari 1 satirdan 535 satira kadar
degisiyor. Proje cesitliligi kural bazinda degisiyor; SV003'un 26 bulgusunun hepsi tek
bir proje grubunda (test projeleri) oldugu icin o kuralin bes satiri da ayni yerden
geliyor, bu bir secim degil havuzun kendisi boyle.

| Kural | Havuzdaki bulgu | Listede | Ayri dosya | Ayri proje | Metot uzunluklari |
|---|---|---|---|---|---|
| SV001 | 11 | 5 | 5 | 2 | 9, 36, 28, 19, 45 |
| SV002 | 23 | 5 | 5 | 3 | 4, 116, 58, 58, 18 |
| SV003 | 26 | 5 | 5 | 1 | 38, 56, 43, 35, 9 |
| SV004 | 277 | 5 | 5 | 3 | 97, 54, 12, 162, 535 |
| SV005 | 120 | 5 | 5 | 3 | 10, 91, 364, 32, 54 |
| SV006 | 421 | 5 | 5 | 2 | 460, 37, 18, 10, 1 |

Hicbir kuralda 5'ten az bulgu olmadi, yani eksik satir yok.

Satir numaralarinin, metot adlarinin ve bulgunun isaret ettigi ifadenin kaynak
dosyalarda gercekten oldugu programatik olarak dogrulandi (bkz. en alttaki not).


## SV001 - async void metot (error)

| # | Dosya | Satir | Metot | Metot uzunlugu | Kaynak satir | GitHub | Gercek sorun mu (E/H) | Neden | Kural nasil duzelmeli |
|---|---|---|---|---|---|---|---|---|---|
| 1 | `Emby.Server.Implementations/ScheduledTasks/Triggers/StartupTrigger.cs` | 31 | `Start` | 9 satir | `public async void Start(TaskResult? lastResult, ILogger logger, string...` | [link](https://github.com/jellyfin/jellyfin/blob/1d7b6d97844c8cc848ed3fb5c4b48bb9cdd5b139/Emby.Server.Implementations/ScheduledTasks/Triggers/StartupTrigger.cs#L31) | E | `ITaskTrigger` arayuzunun `void` donduren uyesinin uygulamasi; imza `Task` yapilamiyor, istisna kaybolur. | Duzeltme gerekmez; kural hakli. |
| 2 | `Emby.Server.Implementations/Session/SessionManager.cs` | 639 | `CheckForIdlePlayback` | 36 satir | `private async void CheckForIdlePlayback(object state)` | [link](https://github.com/jellyfin/jellyfin/blob/1d7b6d97844c8cc848ed3fb5c4b48bb9cdd5b139/Emby.Server.Implementations/Session/SessionManager.cs#L639) | E | Zamanlayici geri cagrisi (`_inactiveTimer`). Istisna gozlemlenemez. | Duzeltme gerekmez; kural hakli. |
| 3 | `MediaBrowser.Controller/MediaEncoding/TranscodingSegmentCleaner.cs` | 88 | `TimerCallback` | 28 satir | `private async void TimerCallback(object? state)` | [link](https://github.com/jellyfin/jellyfin/blob/1d7b6d97844c8cc848ed3fb5c4b48bb9cdd5b139/MediaBrowser.Controller/MediaEncoding/TranscodingSegmentCleaner.cs#L88) | E | Timer geri cagrisi. Istisna kaybolur. | Duzeltme gerekmez; kural hakli. |
| 4 | `MediaBrowser.Controller/MediaEncoding/TranscodingThrottler.cs` | 108 | `TimerCallback` | 19 satir | `private async void TimerCallback(object? state)` | [link](https://github.com/jellyfin/jellyfin/blob/1d7b6d97844c8cc848ed3fb5c4b48bb9cdd5b139/MediaBrowser.Controller/MediaEncoding/TranscodingThrottler.cs#L108) | E | Timer geri cagrisi. Istisna kaybolur. | Duzeltme gerekmez; kural hakli. |
| 5 | `MediaBrowser.Controller/Session/SessionInfo.cs` | 406 | `OnProgressTimerCallback` | 45 satir | `private async void OnProgressTimerCallback(object state)` | [link](https://github.com/jellyfin/jellyfin/blob/1d7b6d97844c8cc848ed3fb5c4b48bb9cdd5b139/MediaBrowser.Controller/Session/SessionInfo.cs#L406) | E | Zamanlayici geri cagrisi (`_progressTimer`). Istisna kaybolur. | Duzeltme gerekmez; kural hakli. |

**Not:** Bes satirin dordu zamanlayici geri cagrisi, biri arayuz uygulamasi. Orneklemde tek bir kalip hakim; bu, SV001'in her tur `async void`'i ayni basariyla yakaladigi anlamina gelmez, sadece bu repoda cikan adaylarin cogunun ayni tur oldugunu gosterir.

## SV002 - bloklayan gorev beklemesi (warning)

| # | Dosya | Satir | Metot | Metot uzunlugu | Kaynak satir | GitHub | Gercek sorun mu (E/H) | Neden | Kural nasil duzelmeli |
|---|---|---|---|---|---|---|---|---|---|
| 1 | `Emby.Server.Implementations/Library/LibraryManager.cs` | 3663 | `UpdatePeople` | 4 satir | `UpdatePeopleAsync(item, people, CancellationToken.None).GetAwaiter().G...` | [link](https://github.com/jellyfin/jellyfin/blob/1d7b6d97844c8cc848ed3fb5c4b48bb9cdd5b139/Emby.Server.Implementations/Library/LibraryManager.cs#L3663) | E | Senkron metot icinden sync-over-async: `UpdatePeopleAsync(...).GetAwaiter().GetResult()`. | Duzeltme gerekmez; kural hakli. |
| 2 | `Emby.Server.Implementations/Library/UserViewManager.cs` | 121 | `GetUserViews` | 116 satir | `var channelResult = _channelManager.GetChannelsInternalAsync(new Chann...` | [link](https://github.com/jellyfin/jellyfin/blob/1d7b6d97844c8cc848ed3fb5c4b48bb9cdd5b139/Emby.Server.Implementations/Library/UserViewManager.cs#L121) | E | Senkron metot icinden sync-over-async (`GetChannelsInternalAsync`). | Duzeltme gerekmez; kural hakli. |
| 3 | `Jellyfin.Server/Filters/SecurityRequirementsOperationFilter.cs` | 79 | `Apply` | 58 satir | `var authorizationPolicy = _authorizationPolicyProvider.GetPolicyAsync(...` | [link](https://github.com/jellyfin/jellyfin/blob/1d7b6d97844c8cc848ed3fb5c4b48bb9cdd5b139/Jellyfin.Server/Filters/SecurityRequirementsOperationFilter.cs#L79) | E | Senkron `Apply` icinden `GetPolicyAsync(...).GetAwaiter().GetResult()`. | Duzeltme gerekmez; kural hakli. |
| 4 | `Jellyfin.Server/Migrations/Routines/20250420230000_MoveTrickplayFiles.cs` | 66 | `Perform` | 58 satir | `var trickplayInfos = _trickplayManager.GetTrickplayItemsAsync(Limit, o...` | [link](https://github.com/jellyfin/jellyfin/blob/1d7b6d97844c8cc848ed3fb5c4b48bb9cdd5b139/Jellyfin.Server/Migrations/Routines/20250420230000_MoveTrickplayFiles.cs#L66) | E | Senkron `Perform` icinden `GetTrickplayItemsAsync(...).GetAwaiter().GetResult()`. | Duzeltme gerekmez; kural hakli. |
| 5 | `src/Jellyfin.MediaEncoding.Hls/Cache/CacheDecorator.cs` | 56 | `TryExtractKeyframes` | 18 satir | `_keyframeRepository.SaveKeyframeDataAsync(itemId, keyframeData, Cancel...` | [link](https://github.com/jellyfin/jellyfin/blob/1d7b6d97844c8cc848ed3fb5c4b48bb9cdd5b139/src/Jellyfin.MediaEncoding.Hls/Cache/CacheDecorator.cs#L56) | E | Senkron `TryExtractKeyframes` icinden `SaveKeyframeDataAsync(...).GetAwaiter().GetResult()`. | Duzeltme gerekmez; kural hakli. |

**Not:** Bes satirin besinin de bicimi `GetAwaiter().GetResult()`. Kuralin aradigi diger iki varyant, `.Result` ve `.Wait()`, bu ornekleme hic girmedi; o iki dal bu olcumde sinanmamis kaliyor.

## SV003 - kayip gorev (warning)

| # | Dosya | Satir | Metot | Metot uzunlugu | Kaynak satir | GitHub | Gercek sorun mu (E/H) | Neden | Kural nasil duzelmeli |
|---|---|---|---|---|---|---|---|---|---|
| 1 | `tests/Jellyfin.Api.Tests/Helpers/MediaInfoHelperTests.cs` | 322 | `GetPlaybackInfo_TwoRequestsForSharedLiveStream_ReceiveIndependentSmartApiBases` | 38 satir | `mediaSourceManager` | [link](https://github.com/jellyfin/jellyfin/blob/1d7b6d97844c8cc848ed3fb5c4b48bb9cdd5b139/tests/Jellyfin.Api.Tests/Helpers/MediaInfoHelperTests.cs#L322) | H | Moq kurulum ifadesi. Cagri bir ifade agacinin icinde, hic calismiyor; `ReturnsAsync` gorev degil kurulum nesnesi donduruyor. | Ifade agaci (lambda) icindeki cagrilar aday sayilmamali; `ReturnsAsync` gibi kurulum API'leri gorev donduren cagri kabul edilmemeli. |
| 2 | `tests/Jellyfin.Providers.Tests/Manager/ItemImageProviderTests.cs` | 348 | `RefreshImages_PopulatedItemPopulatedProviderRemote_UpdatesImagesIfForced` | 56 satir | `providerManager.Setup(pm => pm.GetAvailableRemoteImages(It.IsAny<BaseI...` | [link](https://github.com/jellyfin/jellyfin/blob/1d7b6d97844c8cc848ed3fb5c4b48bb9cdd5b139/tests/Jellyfin.Providers.Tests/Manager/ItemImageProviderTests.cs#L348) | H | Moq kurulum ifadesi; cagri ifade agacinda, calismiyor. | Ayni: ifade agaci icindeki cagrilar elenmeli. |
| 3 | `tests/Jellyfin.Providers.Tests/Manager/MetadataServiceRefreshTests.cs` | 89 | `RefreshWithProviders_ReplaceAllMetadata_KeepsExistingDataWhenEveryRemoteProviderFails` | 43 satir | `local.Setup(p => p.GetMetadata(It.IsAny<ItemInfo>(), It.IsAny<IDirecto...` | [link](https://github.com/jellyfin/jellyfin/blob/1d7b6d97844c8cc848ed3fb5c4b48bb9cdd5b139/tests/Jellyfin.Providers.Tests/Manager/MetadataServiceRefreshTests.cs#L89) | H | Moq kurulum ifadesi; cagri ifade agacinda, calismiyor. | Ayni: ifade agaci icindeki cagrilar elenmeli. |
| 4 | `tests/Jellyfin.Server.Implementations.Tests/FullSystemBackup/BackupServiceTests.cs` | 127 | `CreateBackupService` | 35 satir | `factory.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>(...` | [link](https://github.com/jellyfin/jellyfin/blob/1d7b6d97844c8cc848ed3fb5c4b48bb9cdd5b139/tests/Jellyfin.Server.Implementations.Tests/FullSystemBackup/BackupServiceTests.cs#L127) | H | Moq kurulum ifadesi; `ReturnsAsync` kurulum nesnesi donduruyor, Task degil. | Ayni: ifade agaci icindeki cagrilar elenmeli. |
| 5 | `tests/Jellyfin.Server.Implementations.Tests/Item/SqliteDbTestFixture.cs` | 62 | `CreateDbContextFactory` | 9 satir | `factory.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>(...` | [link](https://github.com/jellyfin/jellyfin/blob/1d7b6d97844c8cc848ed3fb5c4b48bb9cdd5b139/tests/Jellyfin.Server.Implementations.Tests/Item/SqliteDbTestFixture.cs#L62) | H | Moq kurulum ifadesi; `CreateDbContextAsync` cagrisi ifade agacinda. | Ayni: ifade agaci icindeki cagrilar elenmeli. |

**Not:** Bes satirin besi de Moq kurulum ifadesi ve hepsi ayni kok nedenden yanlis: cagri bir ifade agacinin (lambda) icinde duruyor, hic calismiyor.

## SV004 - dongu icinde sorgu (warning)

| # | Dosya | Satir | Metot | Metot uzunlugu | Kaynak satir | GitHub | Gercek sorun mu (E/H) | Neden | Kural nasil duzelmeli |
|---|---|---|---|---|---|---|---|---|---|
| 1 | `Emby.Server.Implementations/Library/LibraryManager.cs` | 2679 | `UpdateItemsAsync` | 97 satir | `if (GetItemById(altId) is null && !allItems.Any(i => i.Id.Equals(altId...` | [link](https://github.com/jellyfin/jellyfin/blob/1d7b6d97844c8cc848ed3fb5c4b48bb9cdd5b139/Emby.Server.Implementations/Library/LibraryManager.cs#L2679) | H | `allItems` bellekteki bir koleksiyon; `Any` burada veritabani sorgusu degil. | Sorgu kaynagi ile bellekteki koleksiyon ayirt edilmeli; tip bilgisi olmadan bu ayrim yapilamiyor. |
| 2 | `Emby.Server.Implementations/Library/SimilarItems/SimilarItemsManager.cs` | 463 | `GetSimilarItemsRecommendationsAsync` | 54 satir | `similar = similar.Where(item => allowedIds.Contains(item.Id)).ToList()...` | [link](https://github.com/jellyfin/jellyfin/blob/1d7b6d97844c8cc848ed3fb5c4b48bb9cdd5b139/Emby.Server.Implementations/Library/SimilarItems/SimilarItemsManager.cs#L463) | H | `allowedIds` bellekteki bir kume; `ToList` bellekte calisiyor. | Ayni: sorgu kaynagi ile bellekteki koleksiyon ayirt edilmeli. |
| 3 | `Emby.Server.Implementations/SyncPlay/Group.cs` | 463 | `GetHighestPing` | 12 satir | `max = Math.Max(max, session.Ping);` | [link](https://github.com/jellyfin/jellyfin/blob/1d7b6d97844c8cc848ed3fb5c4b48bb9cdd5b139/Emby.Server.Implementations/SyncPlay/Group.cs#L463) | H | `Math.Max`; LINQ degil, statik yardimci cagrisi. | `Math` gibi LINQ disi alicilarda `Max`/`Min`/`Sum` sorgu bitirici sayilmamali. |
| 4 | `Jellyfin.Server/Migrations/Routines/20260508120000_MergeDuplicateMusicArtists.cs` | 147 | `PerformAsync` | 162 satir | `&& context.LinkedChildren.Any(k => k.ChildId == keeperId && k.ParentId...` | [link](https://github.com/jellyfin/jellyfin/blob/1d7b6d97844c8cc848ed3fb5c4b48bb9cdd5b139/Jellyfin.Server/Migrations/Routines/20260508120000_MergeDuplicateMusicArtists.cs#L147) | E | `context.LinkedChildren.Any(...)` dongu govdesinde; her adimda ayri sorgu, gercek N+1. | Duzeltme gerekmez; kural hakli. |
| 5 | `MediaBrowser.Model/Dlna/StreamBuilder.cs` | 2278 | `ApplyTranscodingConditions` | 535 satir | `item.MaxWidth = Math.Max(num, item.MaxWidth ?? num);` | [link](https://github.com/jellyfin/jellyfin/blob/1d7b6d97844c8cc848ed3fb5c4b48bb9cdd5b139/MediaBrowser.Model/Dlna/StreamBuilder.cs#L2278) | H | `Math.Max`; LINQ degil, statik yardimci cagrisi. | Ayni: `Math` uzerindeki `Max`/`Min` elenmeli. |

**Not:** Dort yanlis pozitifin ikisi bellekteki koleksiyon, ikisi `Math` uzerinde `Max`. Ikisi de ayni kok nedene cikiyor: ad, tipin yerine kullanildi.

## SV005 - atilmayan nesne (warning)

| # | Dosya | Satir | Metot | Metot uzunlugu | Kaynak satir | GitHub | Gercek sorun mu (E/H) | Neden | Kural nasil duzelmeli |
|---|---|---|---|---|---|---|---|---|---|
| 1 | `MediaBrowser.MediaEncoding/BdInfo/BdInfoExaminer.cs` | 163 | `AddSubtitleStream` | 10 satir | `streams.Add(new MediaStream` | [link](https://github.com/jellyfin/jellyfin/blob/1d7b6d97844c8cc848ed3fb5c4b48bb9cdd5b139/MediaBrowser.MediaEncoding/BdInfo/BdInfoExaminer.cs#L163) | H | `MediaStream` bir veri sinifi, `IDisposable` degil; sadece adi `Stream` ile bitiyor. | Tip adi ekiyle disposable tahmini yapilmamali; gercek tip hiyerarsisi gerekiyor. |
| 2 | `MediaBrowser.MediaEncoding/Encoder/MediaEncoder.cs` | 546 | `GetMediaInfoInternal` | 91 satir | `var memoryStream = new MemoryStream();` | [link](https://github.com/jellyfin/jellyfin/blob/1d7b6d97844c8cc848ed3fb5c4b48bb9cdd5b139/MediaBrowser.MediaEncoding/Encoder/MediaEncoder.cs#L546) | H | `memoryStream` bir sonraki satirda `await using (memoryStream.ConfigureAwait(false))` ile atiliyor. | Iki adimli `var x = new ...; await using (x...)` kalibi `using` kapsami sayilmali. |
| 3 | `MediaBrowser.MediaEncoding/Probing/ProbeResultNormalizer.cs` | 702 | `GetMediaStream` | 364 satir | `var stream = new MediaStream` | [link](https://github.com/jellyfin/jellyfin/blob/1d7b6d97844c8cc848ed3fb5c4b48bb9cdd5b139/MediaBrowser.MediaEncoding/Probing/ProbeResultNormalizer.cs#L702) | H | `MediaStream` veri sinifi, `IDisposable` degil. | Tip adi ekiyle disposable tahmini yapilmamali. |
| 4 | `src/Jellyfin.Extensions/StreamExtensions.cs` | 96 | `IsFileIdenticalAsync` | 32 satir | `var existingFileStream = new FileStream(` | [link](https://github.com/jellyfin/jellyfin/blob/1d7b6d97844c8cc848ed3fb5c4b48bb9cdd5b139/src/Jellyfin.Extensions/StreamExtensions.cs#L96) | H | `existingFileStream` bir sonraki satirda `await using (existingFileStream.ConfigureAwait(false))` ile atiliyor. | Iki adimli `await using` kalibi `using` kapsami sayilmali. |
| 5 | `tests/Jellyfin.Model.Tests/Dlna/StreamBuilderTests.cs` | 689 | `GetSubtitleProfile_RespectsExtractionSetting` | 54 satir | `var subtitleStream = new MediaStream` | [link](https://github.com/jellyfin/jellyfin/blob/1d7b6d97844c8cc848ed3fb5c4b48bb9cdd5b139/tests/Jellyfin.Model.Tests/Dlna/StreamBuilderTests.cs#L689) | H | `MediaStream` veri sinifi, `IDisposable` degil. | Tip adi ekiyle disposable tahmini yapilmamali. |

**Not:** Bes satirin besi de yanlis pozitif ama iki ayri sebepten. Ucunde tip (`MediaStream`) zaten `IDisposable` degil; ikisinde nesne gercekten disposable ama bir sonraki satirda `await using` ile atiliyor ve kuralin `using` muafiyeti iki adimli bu kalibi tanimiyor.

## SV006 - iptal edilemeyen islem (info)

| # | Dosya | Satir | Metot | Metot uzunlugu | Kaynak satir | GitHub | Gercek sorun mu (E/H) | Neden | Kural nasil duzelmeli |
|---|---|---|---|---|---|---|---|---|---|
| 1 | `Jellyfin.Api/Controllers/ItemsController.cs` | 173 | `GetItems` | 460 satir | `public async Task<ActionResult<QueryResult<BaseItemDto>>> GetItems(` | [link](https://github.com/jellyfin/jellyfin/blob/1d7b6d97844c8cc848ed3fb5c4b48bb9cdd5b139/Jellyfin.Api/Controllers/ItemsController.cs#L173) | E | API eylemi; `CancellationToken` parametresi eklenip asagi baglanabilir. | Duzeltme gerekmez; kural hakli. |
| 2 | `Jellyfin.Api/Controllers/PlaystateController.cs` | 143 | `MarkUnplayedItem` | 37 satir | `public async Task<ActionResult<UserItemDataDto?>> MarkUnplayedItem(` | [link](https://github.com/jellyfin/jellyfin/blob/1d7b6d97844c8cc848ed3fb5c4b48bb9cdd5b139/Jellyfin.Api/Controllers/PlaystateController.cs#L143) | E | API eylemi; token eklenebilir. | Duzeltme gerekmez; kural hakli. |
| 3 | `Jellyfin.Api/Controllers/UserController.cs` | 544 | `ForgotPassword` | 18 satir | `public async Task<ActionResult<ForgotPasswordResult>> ForgotPassword([...` | [link](https://github.com/jellyfin/jellyfin/blob/1d7b6d97844c8cc848ed3fb5c4b48bb9cdd5b139/Jellyfin.Api/Controllers/UserController.cs#L544) | E | API eylemi; token eklenebilir. | Duzeltme gerekmez; kural hakli. |
| 4 | `Jellyfin.Api/Middleware/QueryStringDecodingMiddleware.cs` | 28 | `Invoke` | 10 satir | `public async Task Invoke(HttpContext httpContext)` | [link](https://github.com/jellyfin/jellyfin/blob/1d7b6d97844c8cc848ed3fb5c4b48bb9cdd5b139/Jellyfin.Api/Middleware/QueryStringDecodingMiddleware.cs#L28) | H | Middleware `Invoke` imzasi cerceve tarafindan sabit; iptal jetonu zaten `httpContext.RequestAborted` icinde. | Cerceve tarafindan imzasi sabitlenen metotlar (middleware `Invoke`/`InvokeAsync`) muaf tutulmali. |
| 5 | `MediaBrowser.Controller/MediaEncoding/ITranscodeManager.cs` | 43 | `KillTranscodingJobs` | 1 satir | `public Task KillTranscodingJobs(string deviceId, string? playSessionId...` | [link](https://github.com/jellyfin/jellyfin/blob/1d7b6d97844c8cc848ed3fb5c4b48bb9cdd5b139/MediaBrowser.Controller/MediaEncoding/ITranscodeManager.cs#L43) | E | Arayuz bildiriminin kendisi; imzayi degistirmek icin dogru yer burasi. | Duzeltme gerekmez; kural hakli. |

**Not:** Tek yanlis pozitif, imzasi cerceve tarafindan sabitlenmis bir middleware `Invoke` metodu. Dogru bulgulardan biri (5 numara) arayuz bildiriminin kendisi, yani duzeltmenin yapilmasi gereken yer.

## Dogrulama notu

Bu listeyi yazdiktan sonra 30 satirin tamamini klondaki kaynak dosyalara karsi
programatik olarak kontrol ettim. Kontrol edilen seyler: satirin dosyada var olmasi,
metot bildirimine isaret eden kurallarda (SV001, SV006) metot adinin o satirda gecmesi,
ifadeye isaret eden kurallarda (SV002-SV005) beklenen ifadenin o satirdan baslayan
pencerede bulunmasi ve cevreleyen metodun yukarida bildirilmis olmasi.

Ilk kontrolde 6 sapma cikti:

- **2'si gercek kusurdu.** Oznitelikli metotlarda SV001 ve SV006 bulgu satiri olarak
  oznitelik satirini veriyordu (`[HttpGet("Items")]`), imza satirini degil. Sebebi
  Roslyn'de `MethodDeclaration` dugumunun konumunun oznitelik listesinden baslamasi.
  Duzeltildi: artik metot adinin bulundugu satir kullaniliyor. Iki test eklendi.
- **4'u kontrol betigimin hatasiydi.** Cok satira yayilan ifadelerde bulgu satiri
  ifadenin basini gosteriyor, aranan token ise alt satirlarda kaliyordu. Betik tek
  satira bakiyordu; pencereye cevirdim.

Duzeltmelerden sonra 30 satirin 30'u dogrulandi, sapma kalmadi.
