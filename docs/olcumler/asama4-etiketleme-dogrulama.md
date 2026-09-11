# SZZ etiketlerinin dogrulama listesi

**Tarih:** 2026-09-11
**Repolar:** Polly `2247db24`, ShareX `b5a397ea`, Jellyfin `1d7b6d97`
**Uretim:** `tools/Sievert.Measure`, `measure dogrulama <repo>`
**Kontrol:** `tools/liste-kontrol.py` ve `measure satir-kontrol`
**Hangi kodla uretildi:** satir kaymasi duzeltildikten (`9592508`) ve liste ureticisindeki
aday suzme kusuru giderildikten sonra. Onceki surum
`asama4-etiketleme-dogrulama-v1.md` dosyasinda duruyor.

Adim 4'te 272 etiket uretilmis ama hicbiri kaynak koda bakilarak dogrulanmamisti. Bu
liste o acigi kapatmak icin var. **E/H sutunlari bilerek bos**; isaretleme ayri bir is.

## Nasil secildi

30 satir, **uc repoya dagitilmis** (her repodan 10). Tek repodan ornekleme almadim.

Her repodan:

- **5 etiketlenmis satir:** bir duzeltmenin sildigi satirlar blame edilmis ve o satirlari
  en son yazan commit suclanmis.
- **5 etiketlenmemis satir:** ayni dosyanin blame'inde gorunen ama o duzeltmenin **hicbir
  yerinde** suclanmamis commit. Yani SZZ'nin firsati vardi, suclamadi.

Secim rastgele (sabit tohum 42), en cok suclanan commit'lerden degil. Her duzeltme
commit'inden en fazla bir satir aliniyor.

**Jellyfin satirlari nereden geliyor:** Liste uretildiginde Jellyfin'in etiketlemesi
veritabaninda henuz tamamlanmamisti. Liste satirlari veritabanindan degil, ayni SZZ
hesabinin dogrudan calistirilmasindan geliyor, o yuzden Jellyfin'den de onar satir
secilebildi ve **eksik satir yok**. Etiketleme sonradan tamamlandi (4688 etiket) ve
satirlar ayni hesaptan cikiyor.

## Kaynaga karsi kontrol

Iki ayri kontrol yapildi.

**1) Metin ve kaynak kontrolu** (`liste-kontrol.py`): iki sha da depoda var mi, yazilan
dosya duzeltmede degismis mi, satir araligi duzeltmenin sildigi satirlarla ortusuyor mu
(etiketsiz satirlarda ortusMEmeli), baglantilar dogru repoya ve dogru sha'ya gidiyor mu,
suclanan commit duzeltmeden once mi yazilmis.

**2) Bagimsiz satir kontrolu** (`measure satir-kontrol`): satirdaki duzeltme commit'i icin
SZZ bastan calistirilip, yazan suclanan commit'in gercekten suclananlar arasinda olup
olmadigina bakildi. Bu, listeyi ureten kodun aynisini kullanmayan tek kontrol ve
ilk kosusunda **gercek bir kusur yakaladi**: Polly'nin bes etiketsiz satirinin ikisi
aslinda ayni duzeltme tarafindan baska bir hunk'ta suclanmisti. Uretici duzeltildi.

Yeni listede iki kontrol de temiz: metin kontrolu uc repoda da 0 sapma, bagimsiz satir
kontrolu **30/30 dogruladi** (15 etiketli satirin 15'i suclananlar arasinda, 15 etiketsiz
satirin 15'i suclananlar arasinda degil).

## v1 ile fark

| | v1 | v2 | Ayni kalan |
|---|---|---|---|
| Etiketli satir | 15 | 15 | **15 / 15** |
| Etiketsiz satir | 15 | 15 | 9 / 15 |
| Toplam | 30 | 30 | 24 / 30 |

**Etiketli satirlarin hicbiri degismedi.** Bu beklenen sonuc: v1 zaten satir kaymasi
duzeltildikten SONRA uretilmisti, yani B070'in bu listeye etkisi yok. Degisen alti satir
tamamen liste ureticisindeki aday suzme kusurundan geliyor - etiketsiz sayilan ama aslinda
suclanmis commit'ler cikarilinca yerlerine baskalari geldi.

Yani iki listenin farki B070'in olcusu **degil**; B070 bu listeye hic yansimamis. Olculen
sey, ureticinin kendi kusurunun olcusu: etiketsiz satirlarin %40'i yanlisti.

## Polly

### Etiketlenmis

| # | Duzeltme | Duzeltme basligi | Suclanan | Suclanan basligi | Dosya | Satir | Baglantilar | Gercek mi (E/H) | Neden | Yontem nasil duzelmeli |
|---|---|---|---|---|---|---|---|---|---|---|
| 1 | `3b8ba0e4` | Fix mutant | `f301f78c` | Use Shoudly | `test/Polly.Core.Tests/Utils/ObjectPoolTests.cs` | 38 | [duzeltme](https://github.com/App-vNext/Polly/commit/3b8ba0e4f4db6d8260053336a7e95584ea9ac0d8) / [suclanan](https://github.com/App-vNext/Polly/commit/f301f78c09594b865f74dd5946b2326d015f05fd) |  |  |  |
| 2 | `f55880c5` | Fix Retry strategy example code (#2527) | `d2f97505` | [Docs] Polish the docs (#1619) | `src/Snippets/Docs/Retry.cs` | 183 | [duzeltme](https://github.com/App-vNext/Polly/commit/f55880c5c3c36087222ece6f765713d1267121e7) / [suclanan](https://github.com/App-vNext/Polly/commit/d2f9750589ce2aa2260264fc775a630ba32ef66d) |  |  |  |
| 3 | `a7588cba` | Fix 758; race condition causing NullReferenceException for BrokenCircu... | `c6519b24` | Replaced sinle line methods with expression-bodied members. Removed em... | `src/Polly/CircuitBreaker/CircuitStateController.cs` | 140 | [duzeltme](https://github.com/App-vNext/Polly/commit/a7588cba6b7b76bb89b6957ad879910eba11c58d) / [suclanan](https://github.com/App-vNext/Polly/commit/c6519b24b7305028184f439babb1de1c5eadfa21) |  |  |  |
| 4 | `33344166` | Fix Minor typo in Bulkhead intellisence (#246) | `a85c7901` | Add Bulkhead policy and all specs (#160) | `src/Polly.Shared/Bulkhead/BulkheadSyntaxAsync.cs` | 52-67 | [duzeltme](https://github.com/App-vNext/Polly/commit/33344166be5338ae57aa456953b3e5c2bb119bc4) / [suclanan](https://github.com/App-vNext/Polly/commit/a85c7901c790289ec988fa88298b37aa8cd19526) |  |  |  |
| 5 | `18e94385` | Fix samples | `9506d058` | Introduce `samples` folder (#1295) | `samples/Intro/Program.cs` | 54 | [duzeltme](https://github.com/App-vNext/Polly/commit/18e94385d85663e84236a5be2a76de16f57f291b) / [suclanan](https://github.com/App-vNext/Polly/commit/9506d058ebeb8066038a0e5bddcf66bc8f747556) |  |  |  |

### Etiketlenmemis (ayni dosyada, suclanmamis)

| # | Duzeltme | Duzeltme basligi | Suclanan | Suclanan basligi | Dosya | Satir | Baglantilar | Gercek mi (E/H) | Neden | Yontem nasil duzelmeli |
|---|---|---|---|---|---|---|---|---|---|---|
| 1 | `3b8ba0e4` | Fix mutant | `5e19d1df` | Introduce ObjectPool and use it for ResilienceContext pooling (#1111) | `test/Polly.Core.Tests/Utils/ObjectPoolTests.cs` | 26-37 | [duzeltme](https://github.com/App-vNext/Polly/commit/3b8ba0e4f4db6d8260053336a7e95584ea9ac0d8) / [suclanan](https://github.com/App-vNext/Polly/commit/5e19d1df68ac90a316eb9978f37406cfe2dfe3cc) |  |  |  |
| 2 | `f55880c5` | Fix Retry strategy example code (#2527) | `2384117c` | [Docs] Minor cleanups (#1768) | `src/Snippets/Docs/Retry.cs` | 155 | [duzeltme](https://github.com/App-vNext/Polly/commit/f55880c5c3c36087222ece6f765713d1267121e7) / [suclanan](https://github.com/App-vNext/Polly/commit/2384117c816ac49d4b0541aef984d61e23c75f35) |  |  |  |
| 3 | `4379f4b8` | Document issue 510 fix | `ce32f88f` | Add CLSCompliant attribute | `src/Polly.NetStandard11/Properties/AssemblyInfo.cs` | 1 | [duzeltme](https://github.com/App-vNext/Polly/commit/4379f4b8e936f465e79825760d0d919a26e24165) / [suclanan](https://github.com/App-vNext/Polly/commit/ce32f88fe70972096903fbcde9b86616802d4fe9) |  |  |  |
| 4 | `33344166` | Fix Minor typo in Bulkhead intellisence (#246) | `e8fc3064` | Make BulkheadPolicy truly async for .NET4.0 (#180) | `src/Polly.Shared/Bulkhead/BulkheadSyntaxAsync.cs` | 79 | [duzeltme](https://github.com/App-vNext/Polly/commit/33344166be5338ae57aa456953b3e5c2bb119bc4) / [suclanan](https://github.com/App-vNext/Polly/commit/e8fc30646e0a298271339fc3b3e8719dd470c579) |  |  |  |
| 5 | `18e94385` | Fix samples | `22df6409` | Bump PollyVersion from 8.0.0-alpha.4 to 8.0.0-alpha.5 (#1381) | `samples/Extensibility/Program.cs` | 106-109 | [duzeltme](https://github.com/App-vNext/Polly/commit/18e94385d85663e84236a5be2a76de16f57f291b) / [suclanan](https://github.com/App-vNext/Polly/commit/22df6409cb2cba79eeb712c7978be2c576298448) |  |  |  |

## ShareX

### Etiketlenmis

| # | Duzeltme | Duzeltme basligi | Suclanan | Suclanan basligi | Dosya | Satir | Baglantilar | Gercek mi (E/H) | Neden | Yontem nasil duzelmeli |
|---|---|---|---|---|---|---|---|---|---|---|
| 1 | `d8a803f9` | fixed surface prepare error | `dba0ec79` | Initial commit of ShareX project r748 | `ScreenCaptureLib/Forms/RectangleRegion.cs` | 47-48 | [duzeltme](https://github.com/ShareX/ShareX/commit/d8a803f9ce047b52eee3ab1523527113b3179506) / [suclanan](https://github.com/ShareX/ShareX/commit/dba0ec79f8d70f684c48c3ed250dacb9570200f8) |  |  |  |
| 2 | `0460fbc7` | Fix arguments and headers update buttons | `ba22454a` | Custom uploader ui works similar to FTP ui now, changed values apply i... | `ShareX.UploadersLib/Forms/UploadersConfigForm.cs` | 3434 | [duzeltme](https://github.com/ShareX/ShareX/commit/0460fbc7be4486d91e8b9db1b9e99935c3f93b11) / [suclanan](https://github.com/ShareX/ShareX/commit/ba22454a4a3c243562f1856d7da89e93ae175ec2) |  |  |  |
| 3 | `6d353451` | fixed #7300: Fixed Pin to screen auto hide issue | `5fb04932` | Center toolbar | `ShareX/Tools/PinToScreen/PinToScreenForm.cs` | 219 | [duzeltme](https://github.com/ShareX/ShareX/commit/6d353451bebf1cb0456f39f3081bae563126b480) / [suclanan](https://github.com/ShareX/ShareX/commit/5fb0493256d916b3eb19855691d3704feee0799e) |  |  |  |
| 4 | `0eb0cad5` | Fix Issue ShareX/ShareX#802 Do not url-encode file names for (S)FTP up... | `75b09c29` | FTP Client button check for SFTP | `ShareX.UploadersLib/FileUploaders/SFTP.cs` | 70 | [duzeltme](https://github.com/ShareX/ShareX/commit/0eb0cad50c686dbc410751771da822824369f56c) / [suclanan](https://github.com/ShareX/ShareX/commit/75b09c29a6d8ca0836fc8b697f847dbcd6c01ada) |  |  |  |
| 5 | `42104493` | fixed #358: Error window will be top most | `df750724` | Merging Greenshot image editor changes from https://bitbucket.org/gree... | `ShareX/Properties/AssemblyInfo.cs` | 14-15 | [duzeltme](https://github.com/ShareX/ShareX/commit/421044930e2d7255bdc37bedf0408456165f3cbc) / [suclanan](https://github.com/ShareX/ShareX/commit/df7507244e5caee926c600de91afb9bf8ef8c503) |  |  |  |

### Etiketlenmemis (ayni dosyada, suclanmamis)

| # | Duzeltme | Duzeltme basligi | Suclanan | Suclanan basligi | Dosya | Satir | Baglantilar | Gercek mi (E/H) | Neden | Yontem nasil duzelmeli |
|---|---|---|---|---|---|---|---|---|---|---|
| 1 | `d8a803f9` | fixed surface prepare error | `794168f5` | Revert major TaskSettings changes | `ShareX/TaskHelpers.cs` | 257 | [duzeltme](https://github.com/ShareX/ShareX/commit/d8a803f9ce047b52eee3ab1523527113b3179506) / [suclanan](https://github.com/ShareX/ShareX/commit/794168f54ed2eb2a1964caa7d27e62fb713be90a) |  |  |  |
| 2 | `ac484aa3` | Fix bad english, attempt 2 | `a991572a` | Fixed SFTP directory error when using absolute path | `ShareX.UploadersLib/FileUploaders/SFTP.cs` | 186 | [duzeltme](https://github.com/ShareX/ShareX/commit/ac484aa3d10d16e5a7f6adf70e0749099ef899c4) / [suclanan](https://github.com/ShareX/ShareX/commit/a991572ad31ca242e6a81ca862a66245885e6065) |  |  |  |
| 3 | `376084ec` | Fixed notification click action issue | `417d6f3d` | Create test tasks instead of test panels | `ShareX/TaskManager.cs` | 501-513 | [duzeltme](https://github.com/ShareX/ShareX/commit/376084ec83c55889059431c946965f8d7d7de534) / [suclanan](https://github.com/ShareX/ShareX/commit/417d6f3da09e83e478f3db92d2dd3108f484e11b) |  |  |  |
| 4 | `f6f945f9` | Fix for rectangle capture staying top of dialogs | `5554acf8` | Dynamic destination changes | `ShareX/Forms/BeforeUploadForm.Designer.cs` | 68-69 | [duzeltme](https://github.com/ShareX/ShareX/commit/f6f945f918028447a1b26f8d5f409727cad5438a) / [suclanan](https://github.com/ShareX/ShareX/commit/5554acf8b5799852fd7c318757cde25d6618c6da) |  |  |  |
| 5 | `42104493` | fixed #358: Error window will be top most | `991273a9` | Detect changes done in UploaderConfig.json | `ShareX/Program.cs` | 560 | [duzeltme](https://github.com/ShareX/ShareX/commit/421044930e2d7255bdc37bedf0408456165f3cbc) / [suclanan](https://github.com/ShareX/ShareX/commit/991273a9b18bfd6c5133dc690a895746b7fbf141) |  |  |  |

## Jellyfin

### Etiketlenmis

| # | Duzeltme | Duzeltme basligi | Suclanan | Suclanan basligi | Dosya | Satir | Baglantilar | Gercek mi (E/H) | Neden | Yontem nasil duzelmeli |
|---|---|---|---|---|---|---|---|---|---|---|
| 1 | `5cfa466d` | fix: cap GetVideoBitrateParamValue at 400 Mbps (#16467) | `a168040c` | Merge pull request #7941 from jellyfin/fix-overflow | `MediaBrowser.Controller/MediaEncoding/EncodingHelper.cs` | 2618 | [duzeltme](https://github.com/jellyfin/jellyfin/commit/5cfa466d8b0069e04c7d5c4e4f9b9a4bb7464034) / [suclanan](https://github.com/jellyfin/jellyfin/commit/a168040cc8c0067c35801ec0469af80729c03487) |  |  |  |
| 2 | `3fc71293` | Fix AddProperParentChildRelationBaseItemWithCascade migration deleting... | `1736a566` | Fixes FK on unconnected base items (#14863) | `src/Jellyfin.Database/Jellyfin.Database.Providers.Sqlite/Migrations/20250913211637_AddProperParentChildRelationBaseItemWithCascade.cs` | 18-33 | [duzeltme](https://github.com/jellyfin/jellyfin/commit/3fc71293b4e8c30b7cdf2b7d2154a8a0c97fad79) / [suclanan](https://github.com/jellyfin/jellyfin/commit/1736a566ccad07142bde86c0b450175f943e5848) |  |  |  |
| 3 | `6985a4f2` | Fix SortCriteria and refactor SetSorting | `0ebd233c` | update dlna music folders | `Emby.Dlna/ContentDirectory/ControlHandler.cs` | 686-688 | [duzeltme](https://github.com/jellyfin/jellyfin/commit/6985a4f2558ac120e14327fc6addf656feca23a8) / [suclanan](https://github.com/jellyfin/jellyfin/commit/0ebd233c4148b8628326e5695dffb47dd23924c0) |  |  |  |
| 4 | `b5f5b027` | Fix special features filter | `5996c4af` | Complete LinkedChildren integration and batch DTO optimizations | `Jellyfin.Server.Implementations/Item/BaseItemRepository.cs` | 3963 | [duzeltme](https://github.com/jellyfin/jellyfin/commit/b5f5b02787a9a05376bf23836e618030c69541e3) / [suclanan](https://github.com/jellyfin/jellyfin/commit/5996c4afce11249804d24f1caa3a99b390543c4d) |  |  |  |
| 5 | `1c78482b` | Use authorization code from api-migration to fix startup wizard | `4ad96e4f` | update logging levels | `Emby.Server.Implementations/HttpServer/Security/AuthorizationContext.cs` | 72 | [duzeltme](https://github.com/jellyfin/jellyfin/commit/1c78482b480034738516596248955e3e09756dd6) / [suclanan](https://github.com/jellyfin/jellyfin/commit/4ad96e4ff5756ddb235a061231469b30c1ff984d) |  |  |  |

### Etiketlenmemis (ayni dosyada, suclanmamis)

| # | Duzeltme | Duzeltme basligi | Suclanan | Suclanan basligi | Dosya | Satir | Baglantilar | Gercek mi (E/H) | Neden | Yontem nasil duzelmeli |
|---|---|---|---|---|---|---|---|---|---|---|
| 1 | `5cfa466d` | fix: cap GetVideoBitrateParamValue at 400 Mbps (#16467) | `0fc28893` | Enable VideoToolbox AV1 decode | `MediaBrowser.Controller/MediaEncoding/EncodingHelper.cs` | 6943 | [duzeltme](https://github.com/jellyfin/jellyfin/commit/5cfa466d8b0069e04c7d5c4e4f9b9a4bb7464034) / [suclanan](https://github.com/jellyfin/jellyfin/commit/0fc288936d10afc146780d118361f2e722768ee6) |  |  |  |
| 2 | `3fc71293` | Fix AddProperParentChildRelationBaseItemWithCascade migration deleting... | `a0b3e2b0` | Optimize internal querying of UserData, other fixes (#14795) | `src/Jellyfin.Database/Jellyfin.Database.Providers.Sqlite/Migrations/20250913211637_AddProperParentChildRelationBaseItemWithCascade.cs` | 35-52 | [duzeltme](https://github.com/jellyfin/jellyfin/commit/3fc71293b4e8c30b7cdf2b7d2154a8a0c97fad79) / [suclanan](https://github.com/jellyfin/jellyfin/commit/a0b3e2b071509f440db10768f6f8984c7ea382d6) |  |  |  |
| 3 | `81c0451b` | Fix response code & docs | `2f2bceb1` | Remove default parameter values | `Jellyfin.Api/Controllers/ScheduledTasksController.cs` | 40-41 | [duzeltme](https://github.com/jellyfin/jellyfin/commit/81c0451b5e578bb8a41dcb81f2766dbd1eb7f055) / [suclanan](https://github.com/jellyfin/jellyfin/commit/2f2bceb1104d8ea669ca21fc40200247aca956ed) |  |  |  |
| 4 | `9849f522` | fix playlist runtime display | `dfe91e43` | Added IDtoService | `MediaBrowser.Server.Implementations/Dto/DtoService.cs` | 1505 | [duzeltme](https://github.com/jellyfin/jellyfin/commit/9849f522efa21097fcd827635ef75535bb821bf0) / [suclanan](https://github.com/jellyfin/jellyfin/commit/dfe91e43b676915b840f0958e331ba2cb57966d4) |  |  |  |
| 5 | `1c78482b` | Use authorization code from api-migration to fix startup wizard | `91ffff77` | added dlna music folders | `Emby.Server.Implementations/HttpServer/Security/AuthorizationContext.cs` | 232 | [duzeltme](https://github.com/jellyfin/jellyfin/commit/1c78482b480034738516596248955e3e09756dd6) / [suclanan](https://github.com/jellyfin/jellyfin/commit/91ffff7771cb4ae9f89dbc2cb7a5cec70a3301c2) |  |  |  |

