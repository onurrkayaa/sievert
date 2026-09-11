# Dogrulama listesi icin kod baglami

**Tarih:** 2026-09-11
**Repo:** [jellyfin/jellyfin](https://github.com/jellyfin/jellyfin) @ `1d7b6d97844c8cc848ed3fb5c4b48bb9cdd5b139`

[asama3-dogrulama-listesi.md](asama3-dogrulama-listesi.md) icindeki 30 bulgunun her biri
icin, karar verirken dosyayi acmak zorunda kalmayasin diye toplanmis kod baglami.

Burada hukum yok: E/H yazmadim, "bu dogru bulgu" ya da "bu yanlis pozitif" demedim.
Sadece olgu var. Kaynaktan goremedigim bir sey oldugunda tahmin etmek yerine
"goremedim" yazdim.

Her bolumde ortak alanlar: metodun tam imzasi, icinde bulundugu tipin bildirimi, bulgu
satiri ve etrafindaki bes satir. Ondan sonra kurala ozel kanit geliyor; karar icin
gereken sey genelde orasi.

Kod parcalarinda `>>` isareti bulgunun bildirildigi satiri gosteriyor.


---

# SV001 - async void metot

## SV001.1 `Start`
`Emby.Server.Implementations/ScheduledTasks/Triggers/StartupTrigger.cs:31` · [GitHub](https://github.com/jellyfin/jellyfin/blob/1d7b6d97844c8cc848ed3fb5c4b48bb9cdd5b139/Emby.Server.Implementations/ScheduledTasks/Triggers/StartupTrigger.cs#L31)

**Tipin bildirimi** (satir 11)

```csharp
public sealed class StartupTrigger : ITaskTrigger
```

- Tip turu: `class` · partial mi: hayir
- Taban listesi: `ITaskTrigger`

**Metodun imzasi** (satir 31)

```csharp
public async void Start(TaskResult? lastResult, ILogger logger, string taskName, bool isApplicationStartup)
```

**Bulgu satiri ve etrafi**

```csharp
      26 | 
      27 |     /// <inheritdoc />
      28 |     public TaskOptions TaskOptions { get; }
      29 | 
      30 |     /// <inheritdoc />
>>    31 |     public async void Start(TaskResult? lastResult, ILogger logger, string taskName, bool isApplicationStartup)
      32 |     {
      33 |         if (isApplicationStartup)
      34 |         {
      35 |             await Task.Delay(DelayMs).ConfigureAwait(false);
      36 | 
```

**Kanit**

- `+= Start` aramasi (repo geneli): 0 eslesme - yok
- Parametre sayisi: 4
- Ikinci parametrenin tipi: `ILogger`
- `Start(` biciminde cagri aramasi (bildirim satirlari elendi): 46 ham eslesme
  - `tests/Jellyfin.MediaEncoding.Tests/Encoder/ProcessWrapperTests.cs:31:            process.Start();`
  - `tests/Jellyfin.MediaEncoding.Tests/Encoder/ProcessWrapperTests.cs:53:            process.Start();`
  - `tests/Jellyfin.MediaEncoding.Tests/Encoder/ProcessWrapperTests.cs:69:        process.Start();`
  - `tests/Jellyfin.Server.Integration.Tests/JellyfinApplicationFactory.cs:118:            host.Start();`
  - `Jellyfin.Server/Migrations/Routines/20250420200000_MigrateLibraryDb.cs:78:        fullOperationTimer.Start();`

## SV001.2 `CheckForIdlePlayback`
`Emby.Server.Implementations/Session/SessionManager.cs:639` · [GitHub](https://github.com/jellyfin/jellyfin/blob/1d7b6d97844c8cc848ed3fb5c4b48bb9cdd5b139/Emby.Server.Implementations/Session/SessionManager.cs#L639)

**Tipin bildirimi** (satir 49)

```csharp
public sealed class SessionManager : ISessionManager, IAsyncDisposable
```

- Tip turu: `class` · partial mi: hayir
- Taban listesi: `ISessionManager`, `IAsyncDisposable`

**Metodun imzasi** (satir 639)

```csharp
private async void CheckForIdlePlayback(object state)
```

**Bulgu satiri ve etrafi**

```csharp
     634 |                 _inactiveTimer.Dispose();
     635 |                 _inactiveTimer = null;
     636 |             }
     637 |         }
     638 | 
>>   639 |         private async void CheckForIdlePlayback(object state)
     640 |         {
     641 |             var playingSessions = Sessions.Where(i => i.NowPlayingItem is not null)
     642 |                 .ToList();
     643 | 
     644 |             if (playingSessions.Count > 0)
```

**Kanit**

- `+= CheckForIdlePlayback` aramasi (repo geneli): 0 eslesme - yok
- Parametre sayisi: 1
- Ikinci parametrenin tipi: ikinci parametre yok
- `CheckForIdlePlayback(` biciminde cagri aramasi (bildirim satirlari elendi): 1 ham eslesme - gosterilecek satir kalmadi

## SV001.3 `TimerCallback`
`MediaBrowser.Controller/MediaEncoding/TranscodingSegmentCleaner.cs:88` · [GitHub](https://github.com/jellyfin/jellyfin/blob/1d7b6d97844c8cc848ed3fb5c4b48bb9cdd5b139/MediaBrowser.Controller/MediaEncoding/TranscodingSegmentCleaner.cs#L88)

**Tipin bildirimi** (satir 17)

```csharp
public class TranscodingSegmentCleaner : IDisposable
```

- Tip turu: `class` · partial mi: hayir
- Taban listesi: `IDisposable`

**Metodun imzasi** (satir 88)

```csharp
private async void TimerCallback(object? state)
```

**Bulgu satiri ve etrafi**

```csharp
      83 |     private EncodingOptions GetOptions()
      84 |     {
      85 |         return _config.GetEncodingOptions();
      86 |     }
      87 | 
>>    88 |     private async void TimerCallback(object? state)
      89 |     {
      90 |         if (_job.HasExited)
      91 |         {
      92 |             DisposeTimer();
      93 |             return;
```

**Kanit**

- `+= TimerCallback` aramasi (repo geneli): 0 eslesme - yok
- Parametre sayisi: 1
- Ikinci parametrenin tipi: ikinci parametre yok
- `TimerCallback(` biciminde cagri aramasi (bildirim satirlari elendi): 4 ham eslesme
  - `MediaBrowser.Controller/MediaEncoding/TranscodingJob.cs:225:                _killTimer = new Timer(new TimerCallback(callback), this, intervalMs, Timeout.Infinite);`

## SV001.4 `TimerCallback`
`MediaBrowser.Controller/MediaEncoding/TranscodingThrottler.cs:108` · [GitHub](https://github.com/jellyfin/jellyfin/blob/1d7b6d97844c8cc848ed3fb5c4b48bb9cdd5b139/MediaBrowser.Controller/MediaEncoding/TranscodingThrottler.cs#L108)

**Tipin bildirimi** (satir 14)

```csharp
public class TranscodingThrottler : IDisposable
```

- Tip turu: `class` · partial mi: hayir
- Taban listesi: `IDisposable`

**Metodun imzasi** (satir 108)

```csharp
private async void TimerCallback(object? state)
```

**Bulgu satiri ve etrafi**

```csharp
     103 |     private EncodingOptions GetOptions()
     104 |     {
     105 |         return _config.GetEncodingOptions();
     106 |     }
     107 | 
>>   108 |     private async void TimerCallback(object? state)
     109 |     {
     110 |         if (_job.HasExited)
     111 |         {
     112 |             DisposeTimer();
     113 |             return;
```

**Kanit**

- `+= TimerCallback` aramasi (repo geneli): 0 eslesme - yok
- Parametre sayisi: 1
- Ikinci parametrenin tipi: ikinci parametre yok
- `TimerCallback(` biciminde cagri aramasi (bildirim satirlari elendi): 4 ham eslesme
  - `MediaBrowser.Controller/MediaEncoding/TranscodingJob.cs:225:                _killTimer = new Timer(new TimerCallback(callback), this, intervalMs, Timeout.Infinite);`

## SV001.5 `OnProgressTimerCallback`
`MediaBrowser.Controller/Session/SessionInfo.cs:406` · [GitHub](https://github.com/jellyfin/jellyfin/blob/1d7b6d97844c8cc848ed3fb5c4b48bb9cdd5b139/MediaBrowser.Controller/Session/SessionInfo.cs#L406)

**Tipin bildirimi** (satir 20)

```csharp
public sealed class SessionInfo : IAsyncDisposable
```

- Tip turu: `class` · partial mi: hayir
- Taban listesi: `IAsyncDisposable`

**Metodun imzasi** (satir 406)

```csharp
private async void OnProgressTimerCallback(object state)
```

**Bulgu satiri ve etrafi**

```csharp
     401 |                     _progressTimer.Change(1000, 1000);
     402 |                 }
     403 |             }
     404 |         }
     405 | 
>>   406 |         private async void OnProgressTimerCallback(object state)
     407 |         {
     408 |             if (_disposed)
     409 |             {
     410 |                 return;
     411 |             }
```

**Kanit**

- `+= OnProgressTimerCallback` aramasi (repo geneli): 0 eslesme - yok
- Parametre sayisi: 1
- Ikinci parametrenin tipi: ikinci parametre yok
- `OnProgressTimerCallback(` biciminde cagri aramasi (bildirim satirlari elendi): 1 ham eslesme - gosterilecek satir kalmadi


---

# SV002 - bloklayan gorev beklemesi

## SV002.1 `UpdatePeople`
`Emby.Server.Implementations/Library/LibraryManager.cs:3663` · [GitHub](https://github.com/jellyfin/jellyfin/blob/1d7b6d97844c8cc848ed3fb5c4b48bb9cdd5b139/Emby.Server.Implementations/Library/LibraryManager.cs#L3663)

**Tipin bildirimi** (satir 64)

```csharp
public class LibraryManager : ILibraryManager
```

- Tip turu: `class` · partial mi: hayir
- Taban listesi: `ILibraryManager`

**Metodun imzasi** (satir 3661)

```csharp
public void UpdatePeople(BaseItem item, List<PersonInfo> people)
```

**Bulgu satiri ve etrafi**

```csharp
    3658 |             return _peopleRepository.GetPeopleByItems(itemIds);
    3659 |         }
    3660 | 
    3661 |         public void UpdatePeople(BaseItem item, List<PersonInfo> people)
    3662 |         {
>>  3663 |             UpdatePeopleAsync(item, people, CancellationToken.None).GetAwaiter().GetResult();
    3664 |         }
    3665 | 
    3666 |         /// <inheritdoc />
    3667 |         public async Task UpdatePeopleAsync(BaseItem item, IReadOnlyList<PersonInfo> people, CancellationToken cancellationToken)
    3668 |         {
```

**Kanit**

- Metot adi `Main` mi: hayir
- Dosya test kodu mu (yola gore): hayir
- Uzerinde beklenen ifade: `UpdatePeopleAsync(item, people, CancellationToken.None)`
- Ifadenin tipi: `Task` (ayni dosyada bildirimi bulundu)

## SV002.2 `GetUserViews`
`Emby.Server.Implementations/Library/UserViewManager.cs:121` · [GitHub](https://github.com/jellyfin/jellyfin/blob/1d7b6d97844c8cc848ed3fb5c4b48bb9cdd5b139/Emby.Server.Implementations/Library/UserViewManager.cs#L121)

**Tipin bildirimi** (satir 27)

```csharp
public class UserViewManager : IUserViewManager
```

- Tip turu: `class` · partial mi: hayir
- Taban listesi: `IUserViewManager`

**Metodun imzasi** (satir 45)

```csharp
public Folder[] GetUserViews(UserViewQuery query)
```

**Bulgu satiri ve etrafi**

```csharp
     116 |                 list.Add(_libraryManager.GetNamedView(name, CollectionType.folders, string.Empty));
     117 |             }
     118 | 
     119 |             if (query.IncludeExternalContent)
     120 |             {
>>   121 |                 var channelResult = _channelManager.GetChannelsInternalAsync(new ChannelQuery
     122 |                 {
     123 |                     UserId = user.Id
     124 |                 }).GetAwaiter().GetResult();
     125 | 
     126 |                 var channels = channelResult.Items;
```

**Kanit**

- Metot adi `Main` mi: hayir
- Dosya test kodu mu (yola gore): hayir
- Uzerinde beklenen ifade: `coz(u)lemedi`
- Ifadenin tipi: goremedim

## SV002.3 `Apply`
`Jellyfin.Server/Filters/SecurityRequirementsOperationFilter.cs:79` · [GitHub](https://github.com/jellyfin/jellyfin/blob/1d7b6d97844c8cc848ed3fb5c4b48bb9cdd5b139/Jellyfin.Server/Filters/SecurityRequirementsOperationFilter.cs#L79)

**Tipin bildirimi** (satir 16)

```csharp
public class SecurityRequirementsOperationFilter : IOperationFilter
```

- Tip turu: `class` · partial mi: hayir
- Taban listesi: `IOperationFilter`

**Metodun imzasi** (satir 33)

```csharp
public void Apply(OpenApiOperation operation, OperationFilterContext context)
```

**Bulgu satiri ve etrafi**

```csharp
      74 |         // Add DefaultAuthorization scope to any endpoint that has a policy with a requirement that is a subset of DefaultAuthorization.
      75 |         if (!requiredScopes.Contains(DefaultAuthPolicy.AsSpan(), StringComparison.Ordinal))
      76 |         {
      77 |             foreach (var scope in requiredScopes)
      78 |             {
>>    79 |                 var authorizationPolicy = _authorizationPolicyProvider.GetPolicyAsync(scope).GetAwaiter().GetResult();
      80 |                 if (authorizationPolicy is not null
      81 |                     && authorizationPolicy.Requirements.Any(r => r is DefaultAuthorizationRequirement))
      82 |                 {
      83 |                     requiredScopes.Add(DefaultAuthPolicy);
      84 |                     break;
```

**Kanit**

- Metot adi `Main` mi: hayir
- Dosya test kodu mu (yola gore): hayir
- Uzerinde beklenen ifade: `_authorizationPolicyProvider.GetPolicyAsync(scope)`
- Ifadenin tipi: goremedim

## SV002.4 `Perform`
`Jellyfin.Server/Migrations/Routines/20250420230000_MoveTrickplayFiles.cs:66` · [GitHub](https://github.com/jellyfin/jellyfin/blob/1d7b6d97844c8cc848ed3fb5c4b48bb9cdd5b139/Jellyfin.Server/Migrations/Routines/20250420230000_MoveTrickplayFiles.cs#L66)

**Tipin bildirimi** (satir 21)

```csharp
public class MoveTrickplayFiles : IMigrationRoutine #pragma warning restore CS0618 // Type or member is obsolete
```

- Tip turu: `class` · partial mi: hayir
- Taban listesi: `IMigrationRoutine #pragma warning restore CS0618 // Type or member is obsolete`

**Metodun imzasi** (satir 49)

```csharp
public void Perform()
```

**Bulgu satiri ve etrafi**

```csharp
      61 |             IncludeOwnedItems = true
      62 |         };
      63 | 
      64 |         do
      65 |         {
>>    66 |             var trickplayInfos = _trickplayManager.GetTrickplayItemsAsync(Limit, offset).GetAwaiter().GetResult();
      67 |             trickplayQuery.ItemIds = trickplayInfos.Select(i => i.ItemId).Distinct().ToArray();
      68 |             var items = _libraryManager.GetItemList(trickplayQuery);
      69 |             foreach (var trickplayInfo in trickplayInfos)
      70 |             {
      71 |                 var item = items.OfType<Video>().FirstOrDefault(i => i.Id.Equals(trickplayInfo.ItemId));
```

**Kanit**

- Metot adi `Main` mi: hayir
- Dosya test kodu mu (yola gore): hayir
- Uzerinde beklenen ifade: `_trickplayManager.GetTrickplayItemsAsync(Limit, offset)`
- Ifadenin tipi: goremedim

## SV002.5 `TryExtractKeyframes`
`src/Jellyfin.MediaEncoding.Hls/Cache/CacheDecorator.cs:56` · [GitHub](https://github.com/jellyfin/jellyfin/blob/1d7b6d97844c8cc848ed3fb5c4b48bb9cdd5b139/src/Jellyfin.MediaEncoding.Hls/Cache/CacheDecorator.cs#L56)

**Tipin bildirimi** (satir 15)

```csharp
public class CacheDecorator : IKeyframeExtractor
```

- Tip turu: `class` · partial mi: hayir
- Taban listesi: `IKeyframeExtractor`

**Metodun imzasi** (satir 43)

```csharp
public bool TryExtractKeyframes(Guid itemId, string filePath, [NotNullWhen(true)] out KeyframeData? keyframeData)
```

**Bulgu satiri ve etrafi**

```csharp
      51 |                 return false;
      52 |             }
      53 | 
      54 |             _logger.LogDebug("Successfully extracted keyframes using {ExtractorName}", _keyframeExtractorName);
      55 |             keyframeData = result;
>>    56 |             _keyframeRepository.SaveKeyframeDataAsync(itemId, keyframeData, CancellationToken.None).GetAwaiter().GetResult();
      57 |         }
      58 | 
      59 |         return true;
      60 |     }
      61 | }
```

**Kanit**

- Metot adi `Main` mi: hayir
- Dosya test kodu mu (yola gore): hayir
- Uzerinde beklenen ifade: `_keyframeRepository.SaveKeyframeDataAsync(itemId, keyframeData, CancellationToken.None)`
- Ifadenin tipi: goremedim


---

# SV003 - kayip gorev

## SV003.1 `GetPlaybackInfo_TwoRequestsForSharedLiveStream_ReceiveIndependentSmartApiBases`
`tests/Jellyfin.Api.Tests/Helpers/MediaInfoHelperTests.cs:322` · [GitHub](https://github.com/jellyfin/jellyfin/blob/1d7b6d97844c8cc848ed3fb5c4b48bb9cdd5b139/tests/Jellyfin.Api.Tests/Helpers/MediaInfoHelperTests.cs#L322)

**Tipin bildirimi** (satir 25)

```csharp
public class MediaInfoHelperTests
```

- Tip turu: `class` · partial mi: hayir
- Taban listesi: yok

**Metodun imzasi** (satir 308)

```csharp
public async Task GetPlaybackInfo_TwoRequestsForSharedLiveStream_ReceiveIndependentSmartApiBases()
```

**Bulgu satiri ve etrafi**

```csharp
     317 |                 Path = LocalPath,
     318 |                 LiveStreamId = "livestream-1"
     319 |             };
     320 | 
     321 |             var mediaSourceManager = new Mock<IMediaSourceManager>();
>>   322 |             mediaSourceManager
     323 |                 .Setup(x => x.GetLiveStream(It.IsAny<string>(), It.IsAny<CancellationToken>()))
     324 |                 .ReturnsAsync(sharedLiveSource);
     325 | 
     326 |             var requestA = new DefaultHttpContext().Request;
     327 |             var requestB = new DefaultHttpContext().Request;
```

**Kanit**

- Sonuc kullaniliyor mu: hayir, ifade tek basina duruyor
- Cevreleyen metot `async` mi: evet
- Bulgu satirinda `await` var mi: hayir

## SV003.2 `RefreshImages_PopulatedItemPopulatedProviderRemote_UpdatesImagesIfForced`
`tests/Jellyfin.Providers.Tests/Manager/ItemImageProviderTests.cs:348` · [GitHub](https://github.com/jellyfin/jellyfin/blob/1d7b6d97844c8cc848ed3fb5c4b48bb9cdd5b139/tests/Jellyfin.Providers.Tests/Manager/ItemImageProviderTests.cs#L348)

**Tipin bildirimi** (satir 29)

```csharp
public partial class ItemImageProviderTests
```

- Tip turu: `class` · partial mi: evet
- Taban listesi: yok

**Metodun imzasi** (satir 318)

```csharp
public async Task RefreshImages_PopulatedItemPopulatedProviderRemote_UpdatesImagesIfForced(ImageType imageType, int imageCount, bool forceRefresh)
```

**Bulgu satiri ve etrafi**

```csharp
     343 |                     Url = "image url " + i
     344 |                 };
     345 |             }
     346 | 
     347 |             var providerManager = new Mock<IProviderManager>(MockBehavior.Strict);
>>   348 |             providerManager.Setup(pm => pm.GetAvailableRemoteImages(It.IsAny<BaseItem>(), It.IsAny<RemoteImageQuery>(), It.IsAny<CancellationToken>()))
     349 |                 .ReturnsAsync(remoteInfo);
     350 |             var itemImageProvider = GetItemImageProvider(providerManager.Object, new Mock<IFileSystem>());
     351 |             var result = await itemImageProvider.RefreshImages(item, libraryOptions, new List<IImageProvider> { remoteProvider.Object }, refreshOptions, CancellationToken.None);
     352 | 
     353 |             Assert.Equal(forceRefresh, result.UpdateType.HasFlag(ItemUpdateType.ImageUpdate));
```

**Kanit**

- Sonuc kullaniliyor mu: hayir, ifade tek basina duruyor
- Cevreleyen metot `async` mi: evet
- Bulgu satirinda `await` var mi: hayir

## SV003.3 `RefreshWithProviders_ReplaceAllMetadata_KeepsExistingDataWhenEveryRemoteProviderFails`
`tests/Jellyfin.Providers.Tests/Manager/MetadataServiceRefreshTests.cs:89` · [GitHub](https://github.com/jellyfin/jellyfin/blob/1d7b6d97844c8cc848ed3fb5c4b48bb9cdd5b139/tests/Jellyfin.Providers.Tests/Manager/MetadataServiceRefreshTests.cs#L89)

**Tipin bildirimi** (satir 25)

```csharp
public class MetadataServiceRefreshTests
```

- Tip turu: `class` · partial mi: hayir
- Taban listesi: yok

**Metodun imzasi** (satir 77)

```csharp
public async Task RefreshWithProviders_ReplaceAllMetadata_KeepsExistingDataWhenEveryRemoteProviderFails()
```

**Bulgu satiri ve etrafi**

```csharp
      84 | 
      85 |             // Something has to contribute for the merge to run at all, otherwise the item is never touched
      86 |             // and the case is moot. The local provider is the replacement the remote ones did not deliver.
      87 |             var local = new Mock<ILocalMetadataProvider<Movie>>(MockBehavior.Loose);
      88 |             local.Setup(p => p.Name).Returns("Local");
>>    89 |             local.Setup(p => p.GetMetadata(It.IsAny<ItemInfo>(), It.IsAny<IDirectoryService>(), It.IsAny<CancellationToken>()))
      90 |                 .ReturnsAsync(new MetadataResult<Movie>
      91 |                 {
      92 |                     HasMetadata = true,
      93 |                     Item = new Movie { Name = "Test Movie", Tagline = "new tagline" }
      94 |                 });
```

**Kanit**

- Sonuc kullaniliyor mu: hayir, ifade tek basina duruyor
- Cevreleyen metot `async` mi: evet
- Bulgu satirinda `await` var mi: hayir

## SV003.4 `CreateBackupService`
`tests/Jellyfin.Server.Implementations.Tests/FullSystemBackup/BackupServiceTests.cs:127` · [GitHub](https://github.com/jellyfin/jellyfin/blob/1d7b6d97844c8cc848ed3fb5c4b48bb9cdd5b139/tests/Jellyfin.Server.Implementations.Tests/FullSystemBackup/BackupServiceTests.cs#L127)

**Tipin bildirimi** (satir 31)

```csharp
public sealed class BackupServiceTests : IDisposable
```

- Tip turu: `class` · partial mi: hayir
- Taban listesi: `IDisposable`

**Metodun imzasi** (satir 123)

```csharp
private BackupService CreateBackupService()
```

**Bulgu satiri ve etrafi**

```csharp
     122 | 
     123 |     private BackupService CreateBackupService()
     124 |     {
     125 |         var factory = new Mock<IDbContextFactory<JellyfinDbContext>>();
     126 |         factory.Setup(f => f.CreateDbContext()).Returns(CreateDbContext);
>>   127 |         factory.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>())).ReturnsAsync(CreateDbContext);
     128 | 
     129 |         var applicationHost = new Mock<IServerApplicationHost>();
     130 |         applicationHost.Setup(a => a.ApplicationVersion).Returns(new Version(10, 11, 0));
     131 | 
     132 |         var applicationPaths = new Mock<IServerApplicationPaths>();
```

**Kanit**

- Sonuc kullaniliyor mu: hayir, ifade tek basina duruyor
- Cevreleyen metot `async` mi: hayir
- Bulgu satirinda `await` var mi: hayir

## SV003.5 `CreateDbContextFactory`
`tests/Jellyfin.Server.Implementations.Tests/Item/SqliteDbTestFixture.cs:62` · [GitHub](https://github.com/jellyfin/jellyfin/blob/1d7b6d97844c8cc848ed3fb5c4b48bb9cdd5b139/tests/Jellyfin.Server.Implementations.Tests/Item/SqliteDbTestFixture.cs#L62)

**Tipin bildirimi** (satir 24)

```csharp
public abstract class SqliteDbTestFixture : IDisposable
```

- Tip turu: `class` · partial mi: hayir
- Taban listesi: `IDisposable`

**Metodun imzasi** (satir 58)

```csharp
protected IDbContextFactory<JellyfinDbContext> CreateDbContextFactory()
```

**Bulgu satiri ve etrafi**

```csharp
      57 | 
      58 |     protected IDbContextFactory<JellyfinDbContext> CreateDbContextFactory()
      59 |     {
      60 |         var factory = new Mock<IDbContextFactory<JellyfinDbContext>>();
      61 |         factory.Setup(f => f.CreateDbContext()).Returns(CreateDbContext);
>>    62 |         factory.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
      63 |             .ReturnsAsync(CreateDbContext);
      64 | 
      65 |         return factory.Object;
      66 |     }
      67 | 
```

**Kanit**

- Sonuc kullaniliyor mu: hayir, ifade tek basina duruyor
- Cevreleyen metot `async` mi: hayir
- Bulgu satirinda `await` var mi: hayir


---

# SV004 - dongu icinde sorgu

## SV004.1 `UpdateItemsAsync`
`Emby.Server.Implementations/Library/LibraryManager.cs:2679` · [GitHub](https://github.com/jellyfin/jellyfin/blob/1d7b6d97844c8cc848ed3fb5c4b48bb9cdd5b139/Emby.Server.Implementations/Library/LibraryManager.cs#L2679)

**Tipin bildirimi** (satir 64)

```csharp
public class LibraryManager : ILibraryManager
```

- Tip turu: `class` · partial mi: hayir
- Taban listesi: `ILibraryManager`

**Metodun imzasi** (satir 2647)

```csharp
public async Task UpdateItemsAsync(IReadOnlyList<BaseItem> items, BaseItem parent, ItemUpdateType updateReason, CancellationToken cancellationToken)
```

**Bulgu satiri ve etrafi**

```csharp
    2674 |                             continue;
    2675 |                         }
    2676 | 
    2677 |                         // Use the primary video's type for ID calculation to ensure consistency
    2678 |                         var altId = GetNewItemId(path, videoType);
>>  2679 |                         if (GetItemById(altId) is null && !allItems.Any(i => i.Id.Equals(altId)))
    2680 |                         {
    2681 |                             // Alternate version doesn't exist, resolve and create it
    2682 |                             // ensuring it has the same type as the primary video
    2683 |                             var altVideo = ResolveAlternateVersion(path, videoType, parentFolder, parentCollectionType);
    2684 |                             if (altVideo is not null)
```

**Kanit**

- Cagrinin uzerindeki ifade: `allItems`
- Cagri: `Any`
- En yakin dongu basligi: satir 2670, `foreach (var path in video.LocalAlternateVersions)`
- Dongu degiskeni: `path`
- Bulgu satiri dongu degiskenine baglimi: hayir

## SV004.2 `GetSimilarItemsRecommendationsAsync`
`Emby.Server.Implementations/Library/SimilarItems/SimilarItemsManager.cs:463` · [GitHub](https://github.com/jellyfin/jellyfin/blob/1d7b6d97844c8cc848ed3fb5c4b48bb9cdd5b139/Emby.Server.Implementations/Library/SimilarItems/SimilarItemsManager.cs#L463)

**Tipin bildirimi** (satir 35)

```csharp
public class SimilarItemsManager : ISimilarItemsManager
```

- Tip turu: `class` · partial mi: hayir
- Taban listesi: `ISimilarItemsManager`

**Metodun imzasi** (satir 427)

```csharp
private async Task<IReadOnlyList<SimilarItemsRecommendation>> GetSimilarItemsRecommendationsAsync( IReadOnlyList<BaseItem> baselineItems, RecommendationType recommendationType, SimilarItemsQuery query, CancellationToken cancellationToken)
```

**Bulgu satiri ve etrafi**

```csharp
     458 |                 continue;
     459 |             }
     460 | 
     461 |             if (allowedIds is not null)
     462 |             {
>>   463 |                 similar = similar.Where(item => allowedIds.Contains(item.Id)).ToList();
     464 |                 if (similar.Count == 0)
     465 |                 {
     466 |                     continue;
     467 |                 }
     468 |             }
```

**Kanit**

- Cagrinin uzerindeki ifade: `allowedIds.Contains(item.Id))`
- Cagri: `ToList`
- En yakin dongu basligi: satir 454, `foreach (var baseline in baselineItems)`
- Dongu degiskeni: `baseline`
- Bulgu satiri dongu degiskenine baglimi: hayir

## SV004.3 `GetHighestPing`
`Emby.Server.Implementations/SyncPlay/Group.cs:463` · [GitHub](https://github.com/jellyfin/jellyfin/blob/1d7b6d97844c8cc848ed3fb5c4b48bb9cdd5b139/Emby.Server.Implementations/SyncPlay/Group.cs#L463)

**Tipin bildirimi** (satir 27)

```csharp
public class Group : IGroupStateContext
```

- Tip turu: `class` · partial mi: hayir
- Taban listesi: `IGroupStateContext`

**Metodun imzasi** (satir 458)

```csharp
public long GetHighestPing()
```

**Bulgu satiri ve etrafi**

```csharp
     458 |         public long GetHighestPing()
     459 |         {
     460 |             long max = long.MinValue;
     461 |             foreach (var session in _participants.Values)
     462 |             {
>>   463 |                 max = Math.Max(max, session.Ping);
     464 |             }
     465 | 
     466 |             // A group with no participants has no ping to report. Returning long.MinValue would
     467 |             // overflow the callers that scale this value into ticks, so fall back to the default.
     468 |             return max == long.MinValue ? DefaultPing : max;
```

**Kanit**

- Cagrinin uzerindeki ifade: `Math`
- Cagri: `Max`
- **LINQ disi olabilir**: `Math` bir LINQ kaynagi degil, .NET'in statik yardimci tipi gibi duruyor.
- En yakin dongu basligi: satir 461, `foreach (var session in _participants.Values)`
- Dongu degiskeni: `session`
- Bulgu satiri dongu degiskenine baglimi: evet

## SV004.4 `PerformAsync`
`Jellyfin.Server/Migrations/Routines/20260508120000_MergeDuplicateMusicArtists.cs:147` · [GitHub](https://github.com/jellyfin/jellyfin/blob/1d7b6d97844c8cc848ed3fb5c4b48bb9cdd5b139/Jellyfin.Server/Migrations/Routines/20260508120000_MergeDuplicateMusicArtists.cs#L147)

**Tipin bildirimi** (satir 25)

```csharp
public class MergeDuplicateMusicArtists : IAsyncMigrationRoutine
```

- Tip turu: `class` · partial mi: hayir
- Taban listesi: `IAsyncMigrationRoutine`

**Metodun imzasi** (satir 54)

```csharp
public async Task PerformAsync(CancellationToken cancellationToken)
```

**Bulgu satiri ve etrafi**

```csharp
     142 |                         .Where(l => l.ParentId == dupId)
     143 |                         .ExecuteUpdateAsync(s => s.SetProperty(l => l.ParentId, keeperId), cancellationToken)
     144 |                         .ConfigureAwait(false);
     145 |                     await context.LinkedChildren
     146 |                         .Where(l => l.ChildId == dupId
>>   147 |                             && context.LinkedChildren.Any(k => k.ChildId == keeperId && k.ParentId == l.ParentId))
     148 |                         .ExecuteDeleteAsync(cancellationToken)
     149 |                         .ConfigureAwait(false);
     150 |                     await context.LinkedChildren
     151 |                         .Where(l => l.ChildId == dupId)
     152 |                         .ExecuteUpdateAsync(s => s.SetProperty(l => l.ChildId, keeperId), cancellationToken)
```

**Kanit**

- Cagrinin uzerindeki ifade: `context.LinkedChildren`
- Cagri: `Any`
- En yakin dongu basligi: satir 109, `foreach (var dup in stats.Where(s => s.Id != keeper.Id))`
- Dongu degiskeni: `dup`
- Bulgu satiri dongu degiskenine baglimi: hayir

## SV004.5 `ApplyTranscodingConditions`
`MediaBrowser.Model/Dlna/StreamBuilder.cs:2278` · [GitHub](https://github.com/jellyfin/jellyfin/blob/1d7b6d97844c8cc848ed3fb5c4b48bb9cdd5b139/MediaBrowser.Model/Dlna/StreamBuilder.cs#L2278)

**Tipin bildirimi** (satir 19)

```csharp
public class StreamBuilder
```

- Tip turu: `class` · partial mi: hayir
- Taban listesi: yok

**Metodun imzasi** (satir 1755)

```csharp
private void ApplyTranscodingConditions(StreamInfo item, IEnumerable<ProfileCondition> conditions, string? qualifier, bool enableQualifiedConditions, bool enableNonQualifiedConditions)
```

**Bulgu satiri ve etrafi**

```csharp
    2273 |                                 {
    2274 |                                     item.MaxWidth = Math.Min(num, item.MaxWidth ?? num);
    2275 |                                 }
    2276 |                                 else if (condition.Condition == ProfileConditionType.GreaterThanEqual)
    2277 |                                 {
>>  2278 |                                     item.MaxWidth = Math.Max(num, item.MaxWidth ?? num);
    2279 |                                 }
    2280 |                             }
    2281 | 
    2282 |                             break;
    2283 |                         }
```

**Kanit**

- Cagrinin uzerindeki ifade: `Math`
- Cagri: `Max`
- **LINQ disi olabilir**: `Math` bir LINQ kaynagi degil, .NET'in statik yardimci tipi gibi duruyor.
- En yakin dongu basligi: goremedim


---

# SV005 - atilmayan nesne

## SV005.1 `AddSubtitleStream`
`MediaBrowser.MediaEncoding/BdInfo/BdInfoExaminer.cs:163` · [GitHub](https://github.com/jellyfin/jellyfin/blob/1d7b6d97844c8cc848ed3fb5c4b48bb9cdd5b139/MediaBrowser.MediaEncoding/BdInfo/BdInfoExaminer.cs#L163)

**Tipin bildirimi** (satir 16)

```csharp
public class BdInfoExaminer : IBlurayExaminer
```

- Tip turu: `class` · partial mi: hayir
- Taban listesi: `IBlurayExaminer`

**Metodun imzasi** (satir 161)

```csharp
private void AddSubtitleStream(List<MediaStream> streams, int index, TSStream stream)
```

**Bulgu satiri ve etrafi**

```csharp
     158 |     /// <param name="streams">The streams.</param>
     159 |     /// <param name="index">The stream index.</param>
     160 |     /// <param name="stream">The stream.</param>
     161 |     private void AddSubtitleStream(List<MediaStream> streams, int index, TSStream stream)
     162 |     {
>>   163 |         streams.Add(new MediaStream
     164 |         {
     165 |             Language = stream.LanguageCode,
     166 |             Codec = GetNormalizedCodec(stream),
     167 |             Type = MediaStreamType.Subtitle,
     168 |             Index = index
```

**Kanit**

- Olusturulan tip: `MediaStream`
- Kural bu tipi neden yakaladi: `Stream` ekiyle bitiyor
- Satirda `using` var mi: hayir
- Satir `return` ile mi donuyor: hayir
- Bir alana/degiskene atama: goremedim
- Ayni metotta `Dispose` gecen satir: hayir
- Ayni metotta `using` gecen satir: hayir

## SV005.2 `GetMediaInfoInternal`
`MediaBrowser.MediaEncoding/Encoder/MediaEncoder.cs:546` · [GitHub](https://github.com/jellyfin/jellyfin/blob/1d7b6d97844c8cc848ed3fb5c4b48bb9cdd5b139/MediaBrowser.MediaEncoding/Encoder/MediaEncoder.cs#L546)

**Tipin bildirimi** (satir 43)

```csharp
public partial class MediaEncoder : IMediaEncoder, IDisposable
```

- Tip turu: `class` · partial mi: evet
- Taban listesi: `IMediaEncoder`, `IDisposable`

**Metodun imzasi** (satir 503)

```csharp
private async Task<MediaInfo> GetMediaInfoInternal( string inputPath, string primaryPath, MediaProtocol protocol, bool extractChapters, string probeSizeArgument, bool isAudio, VideoType? videoType, CancellationToken cancellationToken)
```

**Bulgu satiri ve etrafi**

```csharp
     541 |                 EnableRaisingEvents = true
     542 |             };
     543 | 
     544 |             _logger.LogDebug("Starting {ProcessFileName} with args {ProcessArgs}", _ffprobePath, args);
     545 | 
>>   546 |             var memoryStream = new MemoryStream();
     547 |             await using (memoryStream.ConfigureAwait(false))
     548 |             using (var processWrapper = new ProcessWrapper(process, this))
     549 |             {
     550 |                 StartProcess(processWrapper);
     551 |                 using var reader = process.StandardOutput;
```

**Kanit**

- Olusturulan tip: `MemoryStream`
- Kural bu tipi neden yakaladi: `Stream` ekiyle bitiyor
- Satirda `using` var mi: hayir
- Satir `return` ile mi donuyor: hayir
- Bir alana/degiskene atama: goremedim
- Ayni metotta `Dispose` gecen satir: hayir
- Ayni metotta `using` gecen satir: evet

## SV005.3 `GetMediaStream`
`MediaBrowser.MediaEncoding/Probing/ProbeResultNormalizer.cs:702` · [GitHub](https://github.com/jellyfin/jellyfin/blob/1d7b6d97844c8cc848ed3fb5c4b48bb9cdd5b139/MediaBrowser.MediaEncoding/Probing/ProbeResultNormalizer.cs#L702)

**Tipin bildirimi** (satir 26)

```csharp
public partial class ProbeResultNormalizer
```

- Tip turu: `class` · partial mi: evet
- Taban listesi: yok

**Metodun imzasi** (satir 700)

```csharp
private MediaStream GetMediaStream(bool isAudio, MediaStreamInfo streamInfo, MediaFormatInfo formatInfo, IReadOnlyList<MediaFrameInfo> frameInfoList)
```

**Bulgu satiri ve etrafi**

```csharp
     697 |         /// <param name="formatInfo">The format info.</param>
     698 |         /// <param name="frameInfoList">The frame info.</param>
     699 |         /// <returns>MediaStream.</returns>
     700 |         private MediaStream GetMediaStream(bool isAudio, MediaStreamInfo streamInfo, MediaFormatInfo formatInfo, IReadOnlyList<MediaFrameInfo> frameInfoList)
     701 |         {
>>   702 |             var stream = new MediaStream
     703 |             {
     704 |                 Codec = streamInfo.CodecName,
     705 |                 Profile = streamInfo.Profile,
     706 |                 Width = streamInfo.Width,
     707 |                 Height = streamInfo.Height,
```

**Kanit**

- Olusturulan tip: `MediaStream`
- Kural bu tipi neden yakaladi: `Stream` ekiyle bitiyor
- Satirda `using` var mi: hayir
- Satir `return` ile mi donuyor: hayir
- Bir alana/degiskene atama: goremedim
- Ayni metotta `Dispose` gecen satir: hayir
- Ayni metotta `using` gecen satir: evet

## SV005.4 `IsFileIdenticalAsync`
`src/Jellyfin.Extensions/StreamExtensions.cs:96` · [GitHub](https://github.com/jellyfin/jellyfin/blob/1d7b6d97844c8cc848ed3fb5c4b48bb9cdd5b139/src/Jellyfin.Extensions/StreamExtensions.cs#L96)

**Tipin bildirimi** (satir 16)

```csharp
public static class StreamExtensions
```

- Tip turu: `class` · partial mi: hayir
- Taban listesi: yok

**Metodun imzasi** (satir 81)

```csharp
public static async Task<bool> IsFileIdenticalAsync(this Stream stream, string path, CancellationToken cancellationToken = default)
```

**Bulgu satiri ve etrafi**

```csharp
      91 |             var originalPosition = stream.Position;
      92 |             try
      93 |             {
      94 |                 stream.Position = 0;
      95 | 
>>    96 |                 var existingFileStream = new FileStream(
      97 |                     path,
      98 |                     FileMode.Open,
      99 |                     FileAccess.Read,
     100 |                     FileShare.Read,
     101 |                     bufferSize: StreamComparisonBufferSize,
```

**Kanit**

- Olusturulan tip: `FileStream`
- Kural bu tipi neden yakaladi: `Stream` ekiyle bitiyor
- Satirda `using` var mi: hayir
- Satir `return` ile mi donuyor: hayir
- Bir alana/degiskene atama: goremedim
- Ayni metotta `Dispose` gecen satir: hayir
- Ayni metotta `using` gecen satir: evet

## SV005.5 `GetSubtitleProfile_RespectsExtractionSetting`
`tests/Jellyfin.Model.Tests/Dlna/StreamBuilderTests.cs:689` · [GitHub](https://github.com/jellyfin/jellyfin/blob/1d7b6d97844c8cc848ed3fb5c4b48bb9cdd5b139/tests/Jellyfin.Model.Tests/Dlna/StreamBuilderTests.cs#L689)

**Tipin bildirimi** (satir 20)

```csharp
public class StreamBuilderTests
```

- Tip turu: `class` · partial mi: hayir
- Taban listesi: yok

**Metodun imzasi** (satir 680)

```csharp
public void GetSubtitleProfile_RespectsExtractionSetting( string codec, string profileFormat, bool enableSubtitleExtraction, bool isExternal, PlayMethod playMethod, SubtitleDeliveryMethod expectedMethod)
```

**Bulgu satiri ve etrafi**

```csharp
     684 |             bool isExternal,
     685 |             PlayMethod playMethod,
     686 |             SubtitleDeliveryMethod expectedMethod)
     687 |         {
     688 |             var mediaSource = new MediaSourceInfo();
>>   689 |             var subtitleStream = new MediaStream
     690 |             {
     691 |                 Type = MediaStreamType.Subtitle,
     692 |                 Index = 0,
     693 |                 IsExternal = isExternal,
     694 |                 Path = isExternal ? "/media/sub." + codec : null,
```

**Kanit**

- Olusturulan tip: `MediaStream`
- Kural bu tipi neden yakaladi: `Stream` ekiyle bitiyor
- Satirda `using` var mi: hayir
- Satir `return` ile mi donuyor: hayir
- Bir alana/degiskene atama: goremedim
- Ayni metotta `Dispose` gecen satir: hayir
- Ayni metotta `using` gecen satir: hayir


---

# SV006 - iptal edilemeyen islem

## SV006.1 `GetItems`
`Jellyfin.Api/Controllers/ItemsController.cs:173` · [GitHub](https://github.com/jellyfin/jellyfin/blob/1d7b6d97844c8cc848ed3fb5c4b48bb9cdd5b139/Jellyfin.Api/Controllers/ItemsController.cs#L173)

**Tipin bildirimi** (satir 37)

```csharp
public class ItemsController : BaseJellyfinApiController
```

- Tip turu: `class` · partial mi: hayir
- Taban listesi: `BaseJellyfinApiController`

**Metodun imzasi** (satir 173)

```csharp
public async Task<ActionResult<QueryResult<BaseItemDto>>> GetItems( [FromQuery] Guid? userId, [FromQuery] string? maxOfficialRating, [FromQuery] bool? hasThemeSong, [FromQuery] bool? hasThemeVideo, [FromQuery] bool? hasSubtitles, [FromQuery] bool? hasSpecialFeature, [FromQuery] bool? hasTrailer, [FromQuery] Guid? adjacentTo, [FromQuery] int? indexNumber, [FromQuery] int? parentIndexNumber, ... (toplam 17 parametre, imza kirpildi)
```

- Imza 17 parametre iceriyor, yukarida kirpildi. Tamami GitHub baglantisinda.

**Bulgu satiri ve etrafi**

```csharp
     168 |     /// <param name="enableTotalRecordCount">Optional. Enable the total record count.</param>
     169 |     /// <param name="enableImages">Optional, include image information in output.</param>
     170 |     /// <returns>A <see cref="QueryResult{BaseItemDto}"/> with the items.</returns>
     171 |     [HttpGet("Items")]
     172 |     [ProducesResponseType(StatusCodes.Status200OK)]
>>   173 |     public async Task<ActionResult<QueryResult<BaseItemDto>>> GetItems(
     174 |         [FromQuery] Guid? userId,
     175 |         [FromQuery] string? maxOfficialRating,
     176 |         [FromQuery] bool? hasThemeSong,
     177 |         [FromQuery] bool? hasThemeVideo,
     178 |         [FromQuery] bool? hasSubtitles,
```

**Kanit**

- `public` mi: evet
- `override` mu: hayir
- Tipin taban listesinde arayuz yok
- Govdede `CancellationToken` gecen bir cagri: hayir

## SV006.2 `MarkUnplayedItem`
`Jellyfin.Api/Controllers/PlaystateController.cs:143` · [GitHub](https://github.com/jellyfin/jellyfin/blob/1d7b6d97844c8cc848ed3fb5c4b48bb9cdd5b139/Jellyfin.Api/Controllers/PlaystateController.cs#L143)

**Tipin bildirimi** (satir 28)

```csharp
public class PlaystateController : BaseJellyfinApiController
```

- Tip turu: `class` · partial mi: hayir
- Taban listesi: `BaseJellyfinApiController`

**Metodun imzasi** (satir 143)

```csharp
public async Task<ActionResult<UserItemDataDto?>> MarkUnplayedItem( [FromQuery] Guid? userId, [FromRoute, Required] Guid itemId)
```

**Bulgu satiri ve etrafi**

```csharp
     138 |     /// <returns>A <see cref="OkResult"/> containing the <see cref="UserItemDataDto"/>, or a <see cref="NotFoundResult"/> if item was not found.</returns>
     139 |     [HttpDelete("UserPlayedItems/{itemId}")]
     140 |     [ProducesResponseType(StatusCodes.Status200OK)]
     141 |     [ProducesResponseType(StatusCodes.Status404NotFound)]
     142 |     [Tags("UserData")]
>>   143 |     public async Task<ActionResult<UserItemDataDto?>> MarkUnplayedItem(
     144 |         [FromQuery] Guid? userId,
     145 |         [FromRoute, Required] Guid itemId)
     146 |     {
     147 |         userId = RequestHelpers.GetUserId(User, userId);
     148 |         var user = _userManager.GetUserById(userId.Value);
```

**Kanit**

- `public` mi: evet
- `override` mu: hayir
- Tipin taban listesinde arayuz yok
- Govdede `CancellationToken` gecen bir cagri: hayir

## SV006.3 `ForgotPassword`
`Jellyfin.Api/Controllers/UserController.cs:544` · [GitHub](https://github.com/jellyfin/jellyfin/blob/1d7b6d97844c8cc848ed3fb5c4b48bb9cdd5b139/Jellyfin.Api/Controllers/UserController.cs#L544)

**Tipin bildirimi** (satir 38)

```csharp
public class UserController : BaseJellyfinApiController
```

- Tip turu: `class` · partial mi: hayir
- Taban listesi: `BaseJellyfinApiController`

**Metodun imzasi** (satir 544)

```csharp
public async Task<ActionResult<ForgotPasswordResult>> ForgotPassword([FromBody, Required] ForgotPasswordDto forgotPasswordRequest)
```

**Bulgu satiri ve etrafi**

```csharp
     539 |     /// <response code="200">Password reset process started.</response>
     540 |     /// <returns>A <see cref="Task"/> containing a <see cref="ForgotPasswordResult"/>.</returns>
     541 |     [HttpPost("ForgotPassword")]
     542 |     [ProducesResponseType(StatusCodes.Status200OK)]
     543 |     [Tags("Authentication")]
>>   544 |     public async Task<ActionResult<ForgotPasswordResult>> ForgotPassword([FromBody, Required] ForgotPasswordDto forgotPasswordRequest)
     545 |     {
     546 |         var ip = HttpContext.GetNormalizedRemoteIP();
     547 |         var isLocal = HttpContext.IsLocal()
     548 |                       || _networkManager.IsInLocalNetwork(ip);
     549 | 
```

**Kanit**

- `public` mi: evet
- `override` mu: hayir
- Tipin taban listesinde arayuz yok
- Govdede `CancellationToken` gecen bir cagri: hayir

## SV006.4 `Invoke`
`Jellyfin.Api/Middleware/QueryStringDecodingMiddleware.cs:28` · [GitHub](https://github.com/jellyfin/jellyfin/blob/1d7b6d97844c8cc848ed3fb5c4b48bb9cdd5b139/Jellyfin.Api/Middleware/QueryStringDecodingMiddleware.cs#L28)

**Tipin bildirimi** (satir 10)

```csharp
public class QueryStringDecodingMiddleware
```

- Tip turu: `class` · partial mi: hayir
- Taban listesi: yok

**Metodun imzasi** (satir 28)

```csharp
public async Task Invoke(HttpContext httpContext)
```

**Bulgu satiri ve etrafi**

```csharp
      23 |     /// <summary>
      24 |     /// Executes the middleware action.
      25 |     /// </summary>
      26 |     /// <param name="httpContext">The current HTTP context.</param>
      27 |     /// <returns>The async task.</returns>
>>    28 |     public async Task Invoke(HttpContext httpContext)
      29 |     {
      30 |         var feature = httpContext.Features.Get<IQueryFeature>();
      31 |         if (feature is not null)
      32 |         {
      33 |             httpContext.Features.Set<IQueryFeature>(new UrlDecodeQueryFeature(feature));
```

**Kanit**

- `public` mi: evet
- `override` mu: hayir
- Tipin taban listesinde arayuz yok
- Govdede `CancellationToken` gecen bir cagri: hayir

## SV006.5 `KillTranscodingJobs`
`MediaBrowser.Controller/MediaEncoding/ITranscodeManager.cs:43` · [GitHub](https://github.com/jellyfin/jellyfin/blob/1d7b6d97844c8cc848ed3fb5c4b48bb9cdd5b139/MediaBrowser.Controller/MediaEncoding/ITranscodeManager.cs#L43)

**Tipin bildirimi** (satir 11)

```csharp
public interface ITranscodeManager
```

- Tip turu: `interface` · partial mi: hayir
- Taban listesi: yok

**Metodun imzasi** (satir 43)

```csharp
public Task KillTranscodingJobs(string deviceId, string? playSessionId, Func<string, bool> deleteFiles);
```

**Bulgu satiri ve etrafi**

```csharp
      38 |     /// </summary>
      39 |     /// <param name="deviceId">The device id.</param>
      40 |     /// <param name="playSessionId">The play session identifier.</param>
      41 |     /// <param name="deleteFiles">The delete files.</param>
      42 |     /// <returns>Task.</returns>
>>    43 |     public Task KillTranscodingJobs(string deviceId, string? playSessionId, Func<string, bool> deleteFiles);
      44 | 
      45 |     /// <summary>
      46 |     /// Report the transcoding progress to the session manager.
      47 |     /// </summary>
      48 |     /// <param name="job">The <see cref="TranscodingJob"/> of which the progress will be reported.</param>
```

**Kanit**

- `public` mi: evet
- `override` mu: hayir
- Tipin taban listesinde arayuz yok
- Govdede `CancellationToken` gecen bir cagri: hayir

---

# SV004: LINQ disi olabilecek cagrilar

Adim 6 raporunda iki ornek gormustum, burada 277 SV004 bulgusunun tamamini taradim.
Cagrinin uzerindeki ifadeyi metinden cozebildigim 216 bulgu var; kalan 61'inde ifade
cok satira yayildigi ya da ic ice zincir oldugu icin cozemedim.

Cozebildiklerim icinde, uzerinde cagri yapilan sey bir LINQ kaynagi degil de .NET'in
statik yardimci tiplerinden biri gibi duranlar:

| Ifade | Bulgu sayisi |
|---|---|
| `Math` | 41 |
| `Directory.EnumerateFileSystemEntries(location)` | 1 |
| **Toplam** | **42** |

41'i `Math.Max` ve `Math.Min`. Kuralin sorgu bitirici listesinde `Max` ve `Min` var ve
bunlar `Math` uzerinde de ayni adla cagriliyor.

Cagri adlarinin dagilimi (cozebildigim 216 bulgu icinde):

| Cagri | Sayi |
|---|---|
| `Any` | 58 |
| `ToList` | 30 |
| `ToArray` | 30 |
| `Max` | 24 |
| `FirstOrDefault` | 23 |
| `Min` | 22 |
| `Count` | 13 |
| `First` | 7 |
| `ToDictionary` | 6 |
| `Sum` | 1 |

Bu bolumdeki 30 satirlik orneklemde `Math` uzerinden gelen iki bulgu var (SV004.3 ve
SV004.5); ikisinin de kanit bolumunde "LINQ disi olabilir" etiketi duruyor.

# Dogrulama notu

Bu dosyayi urettikten sonra 30 bolumun tamamini klondaki kaynak dosyalara karsi
programatik olarak kontrol ettim. Kontrol edilenler:

- Bulgu satirinin dosyada var olmasi
- Tip bildiriminin, yazdigim satir numarasinda gercekten bulunmasi
- Metot imzasinin, yazdigim satir numarasinda bulunmasi ve o satirin metot adini icermesi
- Kod parcasindaki her satirin kaynaktaki ayni numarali satirla **birebir** ayni olmasi
- Her parcada bulgu satirinin `>>` ile isaretlenmis olmasi

**30 bolumun 30'u dogrulandi, sapma cikmadi.**

Uretim sirasinda iki kusur yakalandi ve duzeltildi (bunlar dogrulama adiminda degil,
ciktiyi gozden gecirirken cikti):

- `Start(` gibi kisa metot adlarini ararken duz metin aramasi kullaniyordum ve
  `TrimStart(`, `OnRefreshStart(` gibi alakasiz satirlar eslesiyordu. Kelime siniri
  olan bir aramaya cevirdim.
- `new MediaStream` gibi nesne baslatici kullanan satirlarda `{` bir sonraki satirda
  oldugu icin tip adini cozemiyordum ve "goremedim" yaziyordum. Duzeltildi; bes SV005
  bulgusunun besinde de tip adi artik cozuluyor.

# Goremedigim seyler

Bunlari tahmin etmek yerine acikca birakiyorum:

- **Bir ifadenin gercek tipi.** SV002'de `.Result` alinan seyin `Task` olup olmadigini
  ancak ayni dosyada bir bildirim varsa gorebiliyorum; yoksa "goremedim" yaziyor.
- **Ortuk arayuz uygulamalari.** SV006'da tipin taban listesindeki arayuzu klonda
  aradim ve o arayuzde ayni adda bir uye olup olmadigina baktim. Arayuz baska bir
  pakette ise ya da ad birden fazla yerde geciyorsa sonuc kesin degil.
- **Dolayli cagri zincirleri.** SV001'de metot adiyla duz arama yapiyorum. Cikan
  sayilar ham eslesme; kacinin gercekten o metodu cagirdigini sembol cozumlemesi
  olmadan bilemem.
- **Bir nesnenin sonradan atilip atilmadigi.** SV005'te ayni metotta `Dispose` ya da
  `using` gecip gecmedigine bakiyorum. Nesne baska bir metoda verilip orada atiliyorsa
  goremiyorum.
