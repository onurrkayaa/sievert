# Etiket siniflandirma malzemesi

**Tarih:** 2026-09-11
**Liste surumu:** `asama4-etiketleme-dogrulama.md` **v2** (aday suzme kusuru
giderildikten sonraki liste; v1 ayri dosyada duruyor)
**Hangi kodla uretildi:** `1e835c7` - satir kaymasi duzeltilmis, `tools/Sievert.Measure`
`malzeme` modu
**Olcutler:** `asama4-etiketleme-olcut.md` (siniflandirma baslamadan once ilan edildi)

Bu dosya 30 satirin her biri icin siniflandirmaya yetecek malzemeyi bir araya getiriyor:
duzeltmenin ilgili hunk'i ve suclanan satirin ebeveyn surumundeki baglami. **Karar ve Not
satirlari bos**; ornek karar da konmadi, cunku konulan ornek siniflandirmayi yonlendirir.

## Dagilim

| Repo | Etiketli satir | Etiketsiz satir | Toplam |
|---|---|---|---|
| Polly | 5 | 5 | 10 |
| ShareX | 5 | 5 | 10 |
| Jellyfin | 5 | 5 | 10 |
| **Toplam** | **15** | **15** | **30** |

Satir numaralari 1-10 Polly, 11-20 ShareX, 21-30 Jellyfin.

## Satirlar nasil okunacak

**Etiketli satirlar:** SZZ bu commit'i sucladi. Soru, suclamanin dogru olup olmadigi.

**Etiketsiz satirlar:** SZZ bu commit'i suclamadi ama ayni dosyanin blame'inde goruyordu.
Soru ters yonden: suclanmali miydi. Bu satirlarda "Suclanan commit" yerine "Aday commit"
yaziyor ve altinda SZZ'nin neden suclamadigi tek cumleyle veriliyor - o cumle bir karar
degil, olculen bir mesafe.

**Hunk secimi:** satir araligina denk gelen hunk yaziliyor. Denk gelen hunk yoksa (etiketsiz
satirlarda olabiliyor) dosyanin ilk hunk'i yaziliyor ve bu belirtiliyor. Hunk 40 satiri
asarsa kirpiliyor ve kirpildigi yaziliyor.

**Baglam:** suclanan ya da aday satirin ebeveyn surumundeki hâli, ustunde ve altinda
sekizer satirla. Ilgili satirlarin basinda `>>>` isareti var.

**"Merge" basligi tasiyan bir commit her zaman birlestirme degil.** Satir 21'in suclanan
commit'inin basligi `Merge pull request #7941 from jellyfin/fix-overflow` ama commit'in
tek ebeveyni var, yani GitHub'in squash-merge'i. SZZ zaten gercek birlestirme
commit'lerini suclamiyor (ADR 0014), o yuzden listedeki hicbir satir gercek bir
birlestirmeden gelmiyor. Olcut 6'yi mesaj basligina bakarak uygulamamak gerekiyor;
kontrol edilecek sey ebeveyn sayisi.

### Satir 1 / 30  [Polly]  [etiketli: EVET]

Duzeltme commit'i: `3b8ba0e4f4db6d8260053336a7e95584ea9ac0d8` - Fix mutant
Suclanan commit:   `f301f78c09594b865f74dd5946b2326d015f05fd` - Use Shoudly (2025-01-18)
Dosya: `test/Polly.Core.Tests/Utils/ObjectPoolTests.cs`:38

Duzeltmenin ilgili hunk'i:

```diff
@@ -35,7 +35,7 @@ public class ObjectPoolTests
         var items2 = GetStoreReturn(pool);
 
         // Assert
-        items1.ShouldBeEquivalentTo(items2);
+        items1.ShouldBe(items2);
     }
 
     [Fact]

```

Suclanan satirin baglami:

```csharp
        30          // Arrange
        31          var pool = new ObjectPool<object>(() => new object(), _ => true);
        32          var items1 = GetStoreReturn(pool);
        33  
        34          // Act
        35          var items2 = GetStoreReturn(pool);
        36  
        37          // Assert
>>>     38          items1.ShouldBeEquivalentTo(items2);
        39      }
        40  
        41      [Fact]
        42      public void MaxCapacityOverflow_Respected()
        43      {
        44          // Arrange
        45          var count = ObjectPool<object>.MaxCapacity + 10;
        46          var pool = new ObjectPool<object>(() => new object(), _ => true);
```

Karar:E
Not:

### Satir 2 / 30  [Polly]  [etiketli: EVET]

Duzeltme commit'i: `f55880c5c3c36087222ece6f765713d1267121e7` - Fix Retry strategy example code (#2527)
Suclanan commit:   `d2f9750589ce2aa2260264fc775a630ba32ef66d` - [Docs] Polish the docs (#1619) (2023-09-22)
Dosya: `src/Snippets/Docs/Retry.cs`:183

Duzeltmenin ilgili hunk'i:

```diff
@@ -174,18 +174,18 @@ internal static class Retry
     {
         #region retry-pattern-overusing-builder
 
-        ImmutableArray<Type> networkExceptions = new[]
-        {
+        ImmutableArray<Type> networkExceptions =
+        [
             typeof(SocketException),
             typeof(HttpRequestException),
-        }.ToImmutableArray();
+        ];
 
-        ImmutableArray<Type> strategyExceptions = new[]
-        {
+        ImmutableArray<Type> strategyExceptions =
+        [
             typeof(TimeoutRejectedException),
             typeof(BrokenCircuitException),
             typeof(RateLimitRejectedException),
-        }.ToImmutableArray();
+        ];
 
         ImmutableArray<Type> retryableExceptions = networkExceptions
             .Union(strategyExceptions)
```

Suclanan satirin baglami:

```csharp
       175          #region retry-pattern-overusing-builder
       176  
       177          ImmutableArray<Type> networkExceptions = new[]
       178          {
       179              typeof(SocketException),
       180              typeof(HttpRequestException),
       181          }.ToImmutableArray();
       182  
>>>    183          ImmutableArray<Type> strategyExceptions = new[]
       184          {
       185              typeof(TimeoutRejectedException),
       186              typeof(BrokenCircuitException),
       187              typeof(RateLimitRejectedException),
       188          }.ToImmutableArray();
       189  
       190          ImmutableArray<Type> retryableExceptions = networkExceptions
       191              .Union(strategyExceptions)
```

Karar:E
Not:

### Satir 3 / 30  [Polly]  [etiketli: EVET]

Duzeltme commit'i: `a7588cba6b7b76bb89b6957ad879910eba11c58d` - Fix 758; race condition causing NullReferenceException for BrokenCircuitException
Suclanan commit:   `c6519b24b7305028184f439babb1de1c5eadfa21` - Replaced sinle line methods with expression-bodied members. Removed empty useless line. (2019-01-27)
Dosya: `src/Polly/CircuitBreaker/CircuitStateController.cs`:140

Duzeltmenin ilgili hunk'i:

```diff
@@ -137,9 +137,22 @@ namespace Polly.CircuitBreaker
         }
 
         private BrokenCircuitException GetBreakingException()
-            => _lastOutcome.Exception != null
-                ? new BrokenCircuitException("The circuit is now open and is not allowing calls.", _lastOutcome.Exception)
-                : new BrokenCircuitException<TResult>("The circuit is now open and is not allowing calls.", _lastOutcome.Result);
+        {
+            const string BrokenCircuitMessage = "The circuit is now open and is not allowing calls.";
+
+            var lastOutcome = _lastOutcome;
+            if (lastOutcome == null)
+            {
+                return new BrokenCircuitException(BrokenCircuitMessage);
+            }
+
+            if (lastOutcome.Exception != null)
+            {
+                return new BrokenCircuitException(BrokenCircuitMessage, lastOutcome.Exception);
+            }
+
+            return new BrokenCircuitException<TResult>(BrokenCircuitMessage, lastOutcome.Result);
+        }
 
         public void OnActionPreExecute()
         {

```

Suclanan satirin baglami:

```csharp
       132                  // It's time to permit a / another trial call in the half-open state ...
       133                  // ... but to prevent race conditions/multiple calls, we have to ensure only _one_ thread wins the race to own this next call.
       134                  return Interlocked.CompareExchange(ref _blockedTill, SystemClock.UtcNow().Ticks + _durationOfBreak.Ticks, currentlyBlockedUntil) == currentlyBlockedUntil;
       135              }
       136              return false;
       137          }
       138  
       139          private BrokenCircuitException GetBreakingException()
>>>    140              => _lastOutcome.Exception != null
       141                  ? new BrokenCircuitException("The circuit is now open and is not allowing calls.", _lastOutcome.Exception)
       142                  : new BrokenCircuitException<TResult>("The circuit is now open and is not allowing calls.", _lastOutcome.Result);
       143  
       144          public void OnActionPreExecute()
       145          {
       146              switch (CircuitState)
       147              {
       148                  case CircuitState.Closed:
```

Karar:E
Not:

### Satir 4 / 30  [Polly]  [etiketli: EVET]

Duzeltme commit'i: `33344166be5338ae57aa456953b3e5c2bb119bc4` - Fix Minor typo in Bulkhead intellisence (#246)
Suclanan commit:   `a85c7901c790289ec988fa88298b37aa8cd19526` - Add Bulkhead policy and all specs (#160) (2016-10-06)
Dosya: `src/Polly.Shared/Bulkhead/BulkheadSyntaxAsync.cs`:52-67

Duzeltmenin ilgili hunk'i:

```diff
@@ -49,7 +49,7 @@ namespace Polly
         /// <para>When an execution would cause the number of actions executing concurrently through the policy to exceed <paramref name="maxParallelization" />, the policy allows a further <paramref name="maxQueuingActions" /> executions to queue, waiting for a concurrent execution slot.  When an execution would cause the number of queuing actions to exceed <paramref name="maxQueuingActions" />, a <see cref="BulkheadRejectedException" /> is thrown.</para>
         /// </summary>
         /// <param name="maxParallelization">The maximum number of concurrent actions that may be executing through the policy.</param>
-        /// <param name="maxQueuingActions">The maxmimum number of actions that may be queuing, waiting for an execution slot.</param>
+        /// <param name="maxQueuingActions">The maximum number of actions that may be queuing, waiting for an execution slot.</param>
         /// <returns>The policy instance.</returns>
         /// <exception cref="System.ArgumentOutOfRangeException">maxParallelization;Value must be greater than zero.</exception>
         /// <exception cref="System.ArgumentOutOfRangeException">maxQueuingActions;Value must be greater than or equal to zero.</exception>
```

Suclanan satirin baglami:

```csharp
        44              return BulkheadAsync(maxParallelization, 0, onBulkheadRejectedAsync);
        45          }
        46  
        47          /// <summary>
        48          /// Builds a bulkhead isolation <see cref="Policy" />, which limits the maximum concurrency of actions executed through the policy.  Imposing a maximum concurrency limits the potential of governed actions, when faulting, to bring down the system.
        49          /// <para>When an execution would cause the number of actions executing concurrently through the policy to exceed <paramref name="maxParallelization" />, the policy allows a further <paramref name="maxQueuingActions" /> executions to queue, waiting for a concurrent execution slot.  When an execution would cause the number of queuing actions to exceed <paramref name="maxQueuingActions" />, a <see cref="BulkheadRejectedException" /> is thrown.</para>
        50          /// </summary>
        51          /// <param name="maxParallelization">The maximum number of concurrent actions that may be executing through the policy.</param>
>>>     52          /// <param name="maxQueuingActions">The maxmimum number of actions that may be queuing, waiting for an execution slot.</param>
>>>     53          /// <returns>The policy instance.</returns>
>>>     54          /// <exception cref="System.ArgumentOutOfRangeException">maxParallelization;Value must be greater than zero.</exception>
>>>     55          /// <exception cref="System.ArgumentOutOfRangeException">maxQueuingActions;Value must be greater than or equal to zero.</exception>
>>>     56          public static BulkheadPolicy BulkheadAsync(int maxParallelization, int maxQueuingActions)
>>>     57          {
>>>     58              Func<Context, Task> doNothingAsync = _ => TaskHelper.EmptyTask;
>>>     59              return BulkheadAsync(maxParallelization, maxQueuingActions, doNothingAsync);
>>>     60          }
>>>     61  
>>>     62          /// <summary>
>>>     63          /// Builds a bulkhead isolation <see cref="Policy" />, which limits the maximum concurrency of actions executed through the policy.  Imposing a maximum concurrency limits the potential of governed actions, when faulting, to bring down the system.
>>>     64          /// <para>When an execution would cause the number of actions executing concurrently through the policy to exceed <paramref name="maxParallelization" />, the policy allows a further <paramref name="maxQueuingActions" /> executions to queue, waiting for a concurrent execution slot.  When an execution would cause the number of queuing actions to exceed <paramref name="maxQueuingActions" />, a <see cref="BulkheadRejectedException" /> is thrown.</para>
>>>     65          /// </summary>
>>>     66          /// <param name="maxParallelization">The maximum number of concurrent actions that may be executing through the policy.</param>
>>>     67          /// <param name="maxQueuingActions">The maxmimum number of actions that may be queuing, waiting for an execution slot.</param>
        68          /// <param name="onBulkheadRejectedAsync">An action to call asynchronously, if the bulkhead rejects execution due to oversubscription.</param>
        69          /// <returns>The policy instance.</returns>
        70          /// <exception cref="System.ArgumentOutOfRangeException">maxParallelization;Value must be greater than zero.</exception>
        71          /// <exception cref="System.ArgumentOutOfRangeException">maxQueuingActions;Value must be greater than or equal to zero.</exception>
        72          /// <exception cref="System.ArgumentNullException">onBulkheadRejectedAsync</exception>
        73          public static BulkheadPolicy BulkheadAsync(int maxParallelization, int maxQueuingActions, Func<Context, Task> onBulkheadRejectedAsync)
        74          {
        75              if (maxParallelization <= 0) throw new ArgumentOutOfRangeException(nameof(maxParallelization), "Value must be greater than zero.");
```

Karar:E
Not:

### Satir 5 / 30  [Polly]  [etiketli: EVET]

Duzeltme commit'i: `18e94385d85663e84236a5be2a76de16f57f291b` - Fix samples
Suclanan commit:   `9506d058ebeb8066038a0e5bddcf66bc8f747556` - Introduce `samples` folder (#1295) (2023-06-16)
Dosya: `samples/Intro/Program.cs`:54

Duzeltmenin ilgili hunk'i:

```diff
@@ -51,7 +51,7 @@ strategy = new ResilienceStrategyBuilder()
             _ => PredicateResult.False
         },
         // Register user callback called whenever retry occurs
-        OnRetry = args => { Console.WriteLine($"Retrying...{args.Arguments.Attempt} attempt"); return default; },
+        OnRetry = args => { Console.WriteLine($"Retrying...{args.Arguments.AttemptNumber} attempt"); return default; },
         BaseDelay = TimeSpan.FromMilliseconds(400),
         BackoffType = RetryBackoffType.Constant,
         RetryCount = 3

```

Suclanan satirin baglami:

```csharp
        46          {
        47              TimeoutRejectedException => PredicateResult.True,
        48  
        49              // The "PredicateResult.False" is just shorthand for "new ValueTask<bool>(true)"
        50              // You can also use "new PredicateBuilder().Handle<TimeoutRejectedException>()"
        51              _ => PredicateResult.False
        52          },
        53          // Register user callback called whenever retry occurs
>>>     54          OnRetry = args => { Console.WriteLine($"Retrying...{args.Arguments.Attempt} attempt"); return default; },
        55          BaseDelay = TimeSpan.FromMilliseconds(400),
        56          BackoffType = RetryBackoffType.Constant,
        57          RetryCount = 3
        58      })
        59      // Add timeout using the options
        60      .AddTimeout(new TimeoutStrategyOptions
        61      {
        62          Timeout = TimeSpan.FromMilliseconds(500),
```

Karar:E
Not:

### Satir 6 / 30  [Polly]  [etiketli: HAYIR]

Duzeltme commit'i: `3b8ba0e4f4db6d8260053336a7e95584ea9ac0d8` - Fix mutant
Aday commit:       `5e19d1df68ac90a316eb9978f37406cfe2dfe3cc` - Introduce ObjectPool and use it for ResilienceContext pooling (#1111) (2023-04-12)
Dosya: `test/Polly.Core.Tests/Utils/ObjectPoolTests.cs`:26-37

SZZ neden suclamadi: Bu commit'in satirlari (26-37) duzeltmenin sildigi satirlar arasinda degil; en yakin silinen satir 38, arada 1 satir var.

Duzeltmenin ilgili hunk'i:

```diff
@@ -35,7 +35,7 @@ public class ObjectPoolTests
         var items2 = GetStoreReturn(pool);
 
         // Assert
-        items1.ShouldBeEquivalentTo(items2);
+        items1.ShouldBe(items2);
     }
 
     [Fact]

```

Suclanan satirin baglami:

```csharp
        18  
        19          // Assert
        20          Assert.Same(obj1, obj2);
        21      }
        22  
        23      [Fact]
        24      public void MaxCapacity_Ok() =>
        25          ObjectPool<object>.MaxCapacity.ShouldBe((Environment.ProcessorCount * 2) - 1);
>>>     26  
>>>     27      [Fact]
>>>     28      public void MaxCapacity_Respected()
>>>     29      {
>>>     30          // Arrange
>>>     31          var pool = new ObjectPool<object>(() => new object(), _ => true);
>>>     32          var items1 = GetStoreReturn(pool);
>>>     33  
>>>     34          // Act
>>>     35          var items2 = GetStoreReturn(pool);
>>>     36  
>>>     37          // Assert
        38          items1.ShouldBeEquivalentTo(items2);
        39      }
        40  
        41      [Fact]
        42      public void MaxCapacityOverflow_Respected()
        43      {
        44          // Arrange
        45          var count = ObjectPool<object>.MaxCapacity + 10;
```

Karar:H
Not:

### Satir 7 / 30  [Polly]  [etiketli: HAYIR]

Duzeltme commit'i: `f55880c5c3c36087222ece6f765713d1267121e7` - Fix Retry strategy example code (#2527)
Aday commit:       `2384117c816ac49d4b0541aef984d61e23c75f35` - [Docs] Minor cleanups (#1768) (2023-11-02)
Dosya: `src/Snippets/Docs/Retry.cs`:155

SZZ neden suclamadi: Bu commit'in satirlari (155) duzeltmenin sildigi satirlar arasinda degil; en yakin silinen satir 177, arada 22 satir var.

Duzeltmenin ilgili hunk'i:

```diff
(satir 155 hicbir hunk'a denk gelmiyor; dosyanin ilk hunk'i)
@@ -174,18 +174,18 @@ internal static class Retry
     {
         #region retry-pattern-overusing-builder
 
-        ImmutableArray<Type> networkExceptions = new[]
-        {
+        ImmutableArray<Type> networkExceptions =
+        [
             typeof(SocketException),
             typeof(HttpRequestException),
-        }.ToImmutableArray();
+        ];
 
-        ImmutableArray<Type> strategyExceptions = new[]
-        {
+        ImmutableArray<Type> strategyExceptions =
+        [
             typeof(TimeoutRejectedException),
             typeof(BrokenCircuitException),
             typeof(RateLimitRejectedException),
-        }.ToImmutableArray();
+        ];
 
         ImmutableArray<Type> retryableExceptions = networkExceptions
             .Union(strategyExceptions)
```

Suclanan satirin baglami:

```csharp
       147  
       148          #endregion
       149  
       150          static ValueTask SynchronizeDataAsync(CancellationToken cancellationToken) => default;
       151      }
       152  
       153      public static void AntiPattern_OverusingBuilder()
       154      {
>>>    155          #region retry-anti-pattern-overusing-builder
       156  
       157          var retry = new ResiliencePipelineBuilder()
       158              .AddRetry(new()
       159              {
       160                  ShouldHandle = new PredicateBuilder()
       161                  .Handle<HttpRequestException>()
       162                  .Handle<BrokenCircuitException>()
       163                  .Handle<TimeoutRejectedException>()
```

Karar:E
Not:

### Satir 8 / 30  [Polly]  [etiketli: HAYIR]

Duzeltme commit'i: `4379f4b8e936f465e79825760d0d919a26e24165` - Document issue 510 fix
Aday commit:       `ce32f88fe70972096903fbcde9b86616802d4fe9` - Add CLSCompliant attribute (2016-11-11)
Dosya: `src/Polly.NetStandard11/Properties/AssemblyInfo.cs`:1

SZZ neden suclamadi: Bu commit'in satirlari (1) duzeltmenin sildigi satirlar arasinda degil; en yakin silinen satir 6, arada 5 satir var.

Duzeltmenin ilgili hunk'i:

```diff
(satir 1 hicbir hunk'a denk gelmiyor; dosyanin ilk hunk'i)
@@ -3,8 +3,8 @@ using System.Reflection;
 using System.Runtime.CompilerServices;
 
 [assembly: AssemblyTitle("Polly")]
-[assembly: AssemblyInformationalVersion("6.1.0.0")]
-[assembly: AssemblyFileVersion("6.1.0.0")]
+[assembly: AssemblyInformationalVersion("6.1.1.0")]
+[assembly: AssemblyFileVersion("6.1.1.0")]
 [assembly: AssemblyVersion("6.0.0.0")]
 [assembly: CLSCompliant(true)]
 

```

Suclanan satirin baglami:

```csharp
>>>      1  using System;
         2  using System.Reflection;
         3  using System.Runtime.CompilerServices;
         4  
         5  [assembly: AssemblyTitle("Polly")]
         6  [assembly: AssemblyInformationalVersion("6.1.0.0")]
         7  [assembly: AssemblyFileVersion("6.1.0.0")]
         8  [assembly: AssemblyVersion("6.0.0.0")]
         9  [assembly: CLSCompliant(true)]
```

Karar:E
Not:

### Satir 9 / 30  [Polly]  [etiketli: HAYIR]

Duzeltme commit'i: `33344166be5338ae57aa456953b3e5c2bb119bc4` - Fix Minor typo in Bulkhead intellisence (#246)
Aday commit:       `e8fc30646e0a298271339fc3b3e8719dd470c579` - Make BulkheadPolicy truly async for .NET4.0 (#180) (2016-11-22)
Dosya: `src/Polly.Shared/Bulkhead/BulkheadSyntaxAsync.cs`:79

SZZ neden suclamadi: Bu commit'in satirlari (79) duzeltmenin sildigi satirlar arasinda degil; en yakin silinen satir 67, arada 12 satir var.

Duzeltmenin ilgili hunk'i:

```diff
(satir 79 hicbir hunk'a denk gelmiyor; dosyanin ilk hunk'i)
@@ -1,4 +1,4 @@
-﻿using Polly.Bulkhead;
+using Polly.Bulkhead;
 using Polly.Utilities;
 using System;
 using System.Threading;
```

Suclanan satirin baglami:

```csharp
        71          /// <exception cref="System.ArgumentOutOfRangeException">maxQueuingActions;Value must be greater than or equal to zero.</exception>
        72          /// <exception cref="System.ArgumentNullException">onBulkheadRejectedAsync</exception>
        73          public static BulkheadPolicy BulkheadAsync(int maxParallelization, int maxQueuingActions, Func<Context, Task> onBulkheadRejectedAsync)
        74          {
        75              if (maxParallelization <= 0) throw new ArgumentOutOfRangeException(nameof(maxParallelization), "Value must be greater than zero.");
        76              if (maxQueuingActions < 0) throw new ArgumentOutOfRangeException(nameof(maxQueuingActions), "Value must be greater than or equal to zero.");
        77              if (onBulkheadRejectedAsync == null) throw new ArgumentNullException(nameof(onBulkheadRejectedAsync));
        78  
>>>     79              SemaphoreSlim maxParallelizationSemaphore = SemaphoreSlimFactory.CreateSemaphoreSlim(maxParallelization);
        80  
        81              var maxQueuingCompounded = maxQueuingActions <= int.MaxValue - maxParallelization
        82                  ? maxQueuingActions + maxParallelization
        83                  : int.MaxValue;
        84              SemaphoreSlim maxQueuedActionsSemaphore = SemaphoreSlimFactory.CreateSemaphoreSlim(maxQueuingCompounded);
        85  
        86              return new BulkheadPolicy((action, context, cancellationToken, continueOnCapturedContext) =>
        87                  BulkheadEngine.ImplementationAsync(
```

Karar:H
Not:

### Satir 10 / 30  [Polly]  [etiketli: HAYIR]

Duzeltme commit'i: `18e94385d85663e84236a5be2a76de16f57f291b` - Fix samples
Aday commit:       `22df6409cb2cba79eeb712c7978be2c576298448` - Bump PollyVersion from 8.0.0-alpha.4 to 8.0.0-alpha.5 (#1381) (2023-07-03)
Dosya: `samples/Extensibility/Program.cs`:106-109

SZZ neden suclamadi: Bu commit'in satirlari (106-109) duzeltmenin sildigi satirlar arasinda degil; en yakin silinen satir 128, arada 19 satir var.

Duzeltmenin ilgili hunk'i:

```diff
(satir 106-109 hicbir hunk'a denk gelmiyor; dosyanin ilk hunk'i)
@@ -4,7 +4,7 @@ using Polly.Telemetry;
 // ------------------------------------------------------------------------
 // Usage of custom strategy
 // ------------------------------------------------------------------------
-var strategy = new ResilienceStrategyBuilder()
+var strategy = new CompositeStrategyBuilder()
     // This is custom extension defined in this sample
     .AddMyResilienceStrategy(new MyResilienceStrategyOptions
     {
```

Suclanan satirin baglami:

```csharp
        98  
        99          // Execute the provided callback
       100          var outcome = await callback(context, state);
       101  
       102          // Here, do something after callback execution
       103          // ...
       104  
       105          // You can then report important telemetry events
>>>    106          telemetry.Report(
>>>    107              new ResilienceEvent(ResilienceEventSeverity.Information, "MyCustomEvent"),
>>>    108              context,
>>>    109              new OnCustomEventArguments(context));
       110  
       111          // Call the delegate if provided by the user
       112          if (onCustomEvent is not null)
       113          {
       114              await onCustomEvent(new OnCustomEventArguments(context));
       115          }
       116  
       117          return outcome;
```

Karar:
Not:

### Satir 11 / 30  [ShareX]  [etiketli: EVET]

Duzeltme commit'i: `d8a803f9ce047b52eee3ab1523527113b3179506` - fixed surface prepare error
Suclanan commit:   `dba0ec79f8d70f684c48c3ed250dacb9570200f8` - Initial commit of ShareX project r748 (2013-11-03)
Dosya: `ScreenCaptureLib/Forms/RectangleRegion.cs`:47-48

Duzeltmenin ilgili hunk'i:

```diff
@@ -44,8 +44,7 @@ public class RectangleRegion : Surface
         // For screen ruler
         public bool RulerMode { get; set; }
 
-        public RectangleRegion(Image backgroundImage = null)
-            : base(backgroundImage)
+        public RectangleRegion()
         {
             AreaManager = new AreaManager(this);
             KeyDown += RectangleRegion_KeyDown;

```

Suclanan satirin baglami:

```csharp
        39  
        40          // For screen color picker
        41          public bool OneClickMode { get; set; }
        42          public Point OneClickPosition { get; set; }
        43  
        44          // For screen ruler
        45          public bool RulerMode { get; set; }
        46  
>>>     47          public RectangleRegion(Image backgroundImage = null)
>>>     48              : base(backgroundImage)
        49          {
        50              AreaManager = new AreaManager(this);
        51              KeyDown += RectangleRegion_KeyDown;
        52              MouseDown += RectangleRegion_MouseDown;
        53              MouseWheel += RectangleRegion_MouseWheel;
        54          }
        55  
        56          private void RectangleRegion_MouseDown(object sender, MouseEventArgs e)
```

Karar:E
Not:

### Satir 12 / 30  [ShareX]  [etiketli: EVET]

Duzeltme commit'i: `0460fbc7be4486d91e8b9db1b9e99935c3f93b11` - Fix arguments and headers update buttons
Suclanan commit:   `ba22454a4a3c243562f1856d7da89e93ae175ec2` - Custom uploader ui works similar to FTP ui now, changed values apply instantly without req... (2017-09-02)
Dosya: `ShareX.UploadersLib/Forms/UploadersConfigForm.cs`:3434

Duzeltmenin ilgili hunk'i:

```diff
@@ -3431,7 +3440,6 @@ private void btnCustomUploaderHeaderRemove_Click(object sender, EventArgs e)
 
         private void btnCustomUploaderHeaderUpdate_Click(object sender, EventArgs e)
         {
-            // TODO
             if (lvCustomUploaderHeaders.SelectedItems.Count > 0)
             {
                 string name = txtCustomUploaderHeaderName.Text;
```

Suclanan satirin baglami:

```csharp
      3426                  if (uploader != null) uploader.Headers.Remove(lvCustomUploaderHeaders.SelectedItems[0].Text);
      3427  
      3428                  lvCustomUploaderHeaders.SelectedItems[0].Remove();
      3429              }
      3430          }
      3431  
      3432          private void btnCustomUploaderHeaderUpdate_Click(object sender, EventArgs e)
      3433          {
>>>   3434              // TODO
      3435              if (lvCustomUploaderHeaders.SelectedItems.Count > 0)
      3436              {
      3437                  string name = txtCustomUploaderHeaderName.Text;
      3438  
      3439                  if (!string.IsNullOrEmpty(name))
      3440                  {
      3441                      string value = txtCustomUploaderHeaderValue.Text;
      3442                      lvCustomUploaderHeaders.SelectedItems[0].Text = name;
```

Karar:
Not:

### Satir 13 / 30  [ShareX]  [etiketli: EVET]

Duzeltme commit'i: `6d353451bebf1cb0456f39f3081bae563126b480` - fixed #7300: Fixed Pin to screen auto hide issue
Suclanan commit:   `5fb0493256d916b3eb19855691d3704feee0799e` - Center toolbar (2023-10-08)
Dosya: `ShareX/Tools/PinToScreen/PinToScreenForm.cs`:219

Duzeltmenin ilgili hunk'i:

```diff
@@ -214,9 +214,9 @@ protected override void Dispose(bool disposing)
 
         private void UpdateControls()
         {
-            int toolbarMargin = 10;
+            int toolbarMargin = 20;
             tsMain.Visible = ClientRectangle.Contains(PointToClient(MousePosition)) &&
-                ClientRectangle.Contains(new Rectangle(0, 0, (Options.Border ? Options.BorderSize * 2 : 0) + tsMain.Width + toolbarMargin * 2,
+                ClientRectangle.Contains(new Rectangle(0, 0, (Options.Border ? Options.BorderSize * 2 : 0) + tsMain.Width + toolbarMargin,
                 (Options.Border ? Options.BorderSize * 2 : 0) + tsMain.Height + toolbarMargin));
             tslScale.Text = ImageScale + "%";
         }
```

Suclanan satirin baglami:

```csharp
       211  
       212              base.Dispose(disposing);
       213          }
       214  
       215          private void UpdateControls()
       216          {
       217              int toolbarMargin = 10;
       218              tsMain.Visible = ClientRectangle.Contains(PointToClient(MousePosition)) &&
>>>    219                  ClientRectangle.Contains(new Rectangle(0, 0, (Options.Border ? Options.BorderSize * 2 : 0) + tsMain.Width + toolbarMargin * 2,
       220                  (Options.Border ? Options.BorderSize * 2 : 0) + tsMain.Height + toolbarMargin));
       221              tslScale.Text = ImageScale + "%";
       222          }
       223  
       224          private void AutoSizeForm()
       225          {
       226              Size previousSize = Size;
       227              Size newSize = FormSize;
```

Karar:E
Not:

### Satir 14 / 30  [ShareX]  [etiketli: EVET]

Duzeltme commit'i: `0eb0cad50c686dbc410751771da822824369f56c` - Fix Issue ShareX/ShareX#802 Do not url-encode file names for (S)FTP upload
Suclanan commit:   `75b09c29a6d8ca0836fc8b697f847dbcd6c01ada` - FTP Client button check for SFTP (2014-06-24)
Dosya: `ShareX.UploadersLib/FileUploaders/SFTP.cs`:70

Duzeltmenin ilgili hunk'i:

```diff
@@ -67,7 +67,6 @@ public override UploadResult Upload(Stream stream, string fileName)
         {
             UploadResult result = new UploadResult();
 
-            fileName = Helpers.GetValidURL(fileName);
             string subFolderPath = Account.GetSubFolderPath();
             string path = subFolderPath.CombineURL(fileName);
             bool uploadResult;

```

Suclanan satirin baglami:

```csharp
        62          }
        63  
        64          #region FileUploader methods
        65  
        66          public override UploadResult Upload(Stream stream, string fileName)
        67          {
        68              UploadResult result = new UploadResult();
        69  
>>>     70              fileName = Helpers.GetValidURL(fileName);
        71              string subFolderPath = Account.GetSubFolderPath();
        72              string path = subFolderPath.CombineURL(fileName);
        73              bool uploadResult;
        74  
        75              try
        76              {
        77                  IsUploading = true;
        78                  uploadResult = UploadStream(stream, path);
```

Karar:E
Not:

### Satir 15 / 30  [ShareX]  [etiketli: EVET]

Duzeltme commit'i: `421044930e2d7255bdc37bedf0408456165f3cbc` - fixed #358: Error window will be top most
Suclanan commit:   `df7507244e5caee926c600de91afb9bf8ef8c503` - Merging Greenshot image editor changes from https://bitbucket.org/greenshot/greenshot/comm... (2014-09-19)
Dosya: `ShareX/Properties/AssemblyInfo.cs`:14-15

Duzeltmenin ilgili hunk'i:

```diff
@@ -11,5 +11,5 @@
 [assembly: AssemblyCulture("")]
 [assembly: ComVisible(false)]
 [assembly: Guid("82E6AC09-0FEF-4390-AD9F-0DD3F5561EFC")]
-[assembly: AssemblyVersion("9.4.0")]
-[assembly: AssemblyFileVersion("9.4.0")]
\ No newline at end of file
+[assembly: AssemblyVersion("9.5.0")]
+[assembly: AssemblyFileVersion("9.5.0")]
\ No newline at end of file

```

Suclanan satirin baglami:

```csharp
         6  [assembly: AssemblyConfiguration("")]
         7  [assembly: AssemblyCompany("ShareX Developers")]
         8  [assembly: AssemblyProduct("ShareX")]
         9  [assembly: AssemblyCopyright("Copyright (C) 2007-2014 ShareX Developers")]
        10  [assembly: AssemblyTrademark("")]
        11  [assembly: AssemblyCulture("")]
        12  [assembly: ComVisible(false)]
        13  [assembly: Guid("82E6AC09-0FEF-4390-AD9F-0DD3F5561EFC")]
>>>     14  [assembly: AssemblyVersion("9.4.0")]
>>>     15  [assembly: AssemblyFileVersion("9.4.0")]
```

Karar:E
Not:

### Satir 16 / 30  [ShareX]  [etiketli: HAYIR]

Duzeltme commit'i: `d8a803f9ce047b52eee3ab1523527113b3179506` - fixed surface prepare error
Aday commit:       `794168f54ed2eb2a1964caa7d27e62fb713be90a` - Revert major TaskSettings changes (2014-05-11)
Dosya: `ShareX/TaskHelpers.cs`:257

SZZ neden suclamadi: Bu commit'in satirlari (257) duzeltmenin sildigi satirlar arasinda degil; en yakin silinen satir 351, arada 94 satir var.

Duzeltmenin ilgili hunk'i:

```diff
(satir 257 hicbir hunk'a denk gelmiyor; dosyanin ilk hunk'i)
@@ -348,8 +348,7 @@ public static PointInfo SelectPointColor(SurfaceOptions surfaceOptions = null)
                 surfaceOptions = new SurfaceOptions();
             }
 
-            using (Image fullscreen = Screenshot.CaptureFullscreen())
-            using (RectangleRegion surface = new RectangleRegion(fullscreen))
+            using (RectangleRegion surface = new RectangleRegion())
             {
                 surface.Config = surfaceOptions;
                 surface.OneClickMode = true;
```

Suclanan satirin baglami:

```csharp
       249                  {
       250                      printForm.ShowDialog();
       251                  }
       252              }
       253          }
       254  
       255          public static Image AddImageEffects(Image img, TaskSettings taskSettings)
       256          {
>>>    257              if (taskSettings.ImageSettings.ShowImageEffectsWindowAfterCapture)
       258              {
       259                  using (ImageEffectsForm imageEffectsForm = new ImageEffectsForm(img, taskSettings.ImageSettings.ImageEffects))
       260                  {
       261                      if (imageEffectsForm.ShowDialog() == DialogResult.OK)
       262                      {
       263                          taskSettings.ImageSettings.ImageEffects = imageEffectsForm.Effects;
       264                      }
       265                  }
```

Karar:
Not:

### Satir 17 / 30  [ShareX]  [etiketli: HAYIR]

Duzeltme commit'i: `ac484aa3d10d16e5a7f6adf70e0749099ef899c4` - Fix bad english, attempt 2
Aday commit:       `a991572ad31ca242e6a81ca862a66245885e6065` - Fixed SFTP directory error when using absolute path (2017-03-23)
Dosya: `ShareX.UploadersLib/FileUploaders/SFTP.cs`:186

SZZ neden suclamadi: Bu commit'in satirlari (186) duzeltmenin sildigi satirlar arasinda degil; en yakin silinen satir 107, arada 79 satir var.

Duzeltmenin ilgili hunk'i:

```diff
(satir 186 hicbir hunk'a denk gelmiyor; dosyanin ilk hunk'i)
@@ -104,7 +104,7 @@ public bool Connect()
                 {
                     if (!File.Exists(Account.Keypath))
                     {
-                        throw new FileNotFoundException("Key file does not exists.", Account.Keypath);
+                        throw new FileNotFoundException("Key file does not exist.", Account.Keypath);
                     }
 
                     PrivateKeyFile keyFile;

```

Suclanan satirin baglami:

```csharp
       178              if (Connect())
       179              {
       180                  try
       181                  {
       182                      client.CreateDirectory(path);
       183  
       184                      DebugHelper.WriteLine($"SFTP directory created: {path}");
       185                  }
>>>    186                  catch (SftpPathNotFoundException) when (createMultiDirectory)
       187                  {
       188                      CreateMultiDirectory(path);
       189                  }
       190                  catch (SftpPermissionDeniedException)
       191                  {
       192                  }
       193              }
       194          }
```

Karar:E
Not:

### Satir 18 / 30  [ShareX]  [etiketli: HAYIR]

Duzeltme commit'i: `376084ec83c55889059431c946965f8d7d7de534` - Fixed notification click action issue
Aday commit:       `417d6f3da09e83e478f3db92d2dd3108f484e11b` - Create test tasks instead of test panels (2019-05-22)
Dosya: `ShareX/TaskManager.cs`:501-513

SZZ neden suclamadi: Bu commit'in satirlari (501-513) duzeltmenin sildigi satirlar arasinda degil; en yakin silinen satir 383, arada 118 satir var.

Duzeltmenin ilgili hunk'i:

```diff
(satir 501-513 hicbir hunk'a denk gelmiyor; dosyanin ilk hunk'i)
@@ -380,7 +380,7 @@ private static void Task_TaskCompleted(WorkerTask task)
                                         Image = task.Image,
                                         Title = "ShareX - " + Resources.TaskManager_task_UploadCompleted_ShareX___Task_completed,
                                         Text = result,
-                                        URL = result
+                                        URL = info.Result.ToString()
                                     };
 
                                     NotificationForm.Show(toastConfig);

```

Suclanan satirin baglami:

```csharp
       493                      Program.MainForm.niTray.Icon = icon;
       494                      oldIcon.DisposeHandle();
       495                  }
       496  
       497                  lastIconStatus = progress;
       498              }
       499          }
       500  
>>>    501          public static void AddTestTasks(int count)
>>>    502          {
>>>    503              for (int i = 0; i < count; i++)
>>>    504              {
>>>    505                  WorkerTask task = WorkerTask.CreateHistoryTask(new RecentTask()
>>>    506                  {
>>>    507                      FilePath = @"..\..\..\ShareX.HelpersLib\Resources\ShareX_Logo.png"
>>>    508                  });
>>>    509  
>>>    510                  Start(task);
>>>    511              }
>>>    512          }
>>>    513  
       514          public static async Task TestTrayIcon()
       515          {
       516              for (int i = 0; i <= 100; i++)
       517              {
       518                  UpdateTrayIcon(i);
       519  
       520                  await Task.Delay(50);
       521              }
```

Karar:E
Not:

### Satir 19 / 30  [ShareX]  [etiketli: HAYIR]

Duzeltme commit'i: `f6f945f918028447a1b26f8d5f409727cad5438a` - Fix for rectangle capture staying top of dialogs
Aday commit:       `5554acf8b5799852fd7c318757cde25d6618c6da` - Dynamic destination changes (2014-05-12)
Dosya: `ShareX/Forms/BeforeUploadForm.Designer.cs`:68-69

SZZ neden suclamadi: Bu commit'in satirlari (68-69) duzeltmenin sildigi satirlar arasinda degil; en yakin silinen satir 91, arada 22 satir var.

Duzeltmenin ilgili hunk'i:

```diff
(satir 68-69 hicbir hunk'a denk gelmiyor; dosyanin ilk hunk'i)
@@ -88,12 +88,13 @@ private void InitializeComponent()
             this.Controls.Add(this.btnOK);
             this.Controls.Add(this.ucBeforeUpload);
             this.MaximizeBox = false;
-            this.MaximumSize = new System.Drawing.Size(420, 460);
+            this.MaximumSize = new System.Drawing.Size(420, 800);
             this.MinimizeBox = false;
             this.MinimumSize = new System.Drawing.Size(420, 420);
             this.Name = "BeforeUploadForm";
             this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
-            this.Text = "ShareX - Dynamic Destinations";
+            this.Text = "ShareX - Dynamic destinations";
+            this.TopMost = true;
             this.ResumeLayout(false);
 
         }

```

Suclanan satirin baglami:

```csharp
        60              // 
        61              this.lblTitle.Dock = System.Windows.Forms.DockStyle.Top;
        62              this.lblTitle.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F);
        63              this.lblTitle.Location = new System.Drawing.Point(0, 0);
        64              this.lblTitle.Name = "lblTitle";
        65              this.lblTitle.Padding = new System.Windows.Forms.Padding(4);
        66              this.lblTitle.Size = new System.Drawing.Size(404, 40);
        67              this.lblTitle.TabIndex = 0;
>>>     68              this.lblTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
>>>     69              // 
        70              // ucBeforeUpload
        71              // 
        72              this.ucBeforeUpload.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
        73              | System.Windows.Forms.AnchorStyles.Left) 
        74              | System.Windows.Forms.AnchorStyles.Right)));
        75              this.ucBeforeUpload.Location = new System.Drawing.Point(0, 48);
        76              this.ucBeforeUpload.Name = "ucBeforeUpload";
        77              this.ucBeforeUpload.Size = new System.Drawing.Size(400, 289);
```

Karar:E
Not:

### Satir 20 / 30  [ShareX]  [etiketli: HAYIR]

Duzeltme commit'i: `421044930e2d7255bdc37bedf0408456165f3cbc` - fixed #358: Error window will be top most
Aday commit:       `991273a9b18bfd6c5133dc690a895746b7fbf141` - Detect changes done in UploaderConfig.json (2014-04-15)
Dosya: `ShareX/Program.cs`:560

SZZ neden suclamadi: Bu commit'in satirlari (560) duzeltmenin sildigi satirlar arasinda degil; en yakin silinen satir 42, arada 518 satir var.

Duzeltmenin ilgili hunk'i:

```diff
(satir 560 hicbir hunk'a denk gelmiyor; dosyanin ilk hunk'i)
@@ -39,7 +39,7 @@ namespace ShareX
 {
     internal static class Program
     {
-        public static bool IsBeta = false;
+        public static bool IsBeta = true;
 
         public static string Title
         {

```

Suclanan satirin baglami:

```csharp
       552              TaskEx.Run(() =>
       553              {
       554                  UploadersConfig.Save(UploadersConfigFilePath);
       555              },
       556              () =>
       557              {
       558                  if (uploaderConfigWatcher != null) uploaderConfigWatcher.EnableRaisingEvents = true;
       559              });
>>>    560          }
       561  
       562          public static string GetGitHash()
       563          {
       564              using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("ShareX.GitHash.txt"))
       565              using (StreamReader reader = new StreamReader(stream))
       566              {
       567                  return reader.ReadLine();
       568              }
```

Karar:E
Not:

### Satir 21 / 30  [Jellyfin]  [etiketli: EVET]

Duzeltme commit'i: `5cfa466d8b0069e04c7d5c4e4f9b9a4bb7464034` - fix: cap GetVideoBitrateParamValue at 400 Mbps (#16467)
Suclanan commit:   `a168040cc8c0067c35801ec0469af80729c03487` - Merge pull request #7941 from jellyfin/fix-overflow (2022-06-15)
Dosya: `MediaBrowser.Controller/MediaEncoding/EncodingHelper.cs`:2618

Duzeltmenin ilgili hunk'i:

```diff
@@ -2614,8 +2614,16 @@ namespace MediaBrowser.Controller.MediaEncoding
                 }
             }
 
-            // Cap the max target bitrate to intMax/2 to satisfy the bufsize=bitrate*2.
-            return Math.Min(bitrate ?? 0, int.MaxValue / 2);
+            // Cap the max target bitrate to 400 Mbps.
+            // No consumer or professional hardware transcode target exceeds this value
+            // (Intel QSV tops out at ~300 Mbps for H.264; HEVC High Tier Level 5.x is ~240 Mbps).
+            // Without this cap, plugin-provided MPEG-TS streams with no usable bitrate metadata
+            // can produce unreasonably large -bufsize/-maxrate values for the encoder.
+            // Note: the existing FallbackMaxStreamingBitrate mechanism (default 30 Mbps) only
+            // applies when a LiveStreamId is set (M3U/HDHR sources). Plugin streams and other
+            // sources that bypass the LiveTV pipeline are not covered by it.
+            const int MaxSaneBitrate = 400_000_000; // 400 Mbps
+            return Math.Min(bitrate ?? 0, MaxSaneBitrate);
         }
 
         private int GetMinBitrate(int sourceBitrate, int requestedBitrate)

```

Suclanan satirin baglami:

```csharp
      2610                      if (request.VideoBitRate.HasValue)
      2611                      {
      2612                          bitrate = Math.Min(bitrate.Value, request.VideoBitRate.Value);
      2613                      }
      2614                  }
      2615              }
      2616  
      2617              // Cap the max target bitrate to intMax/2 to satisfy the bufsize=bitrate*2.
>>>   2618              return Math.Min(bitrate ?? 0, int.MaxValue / 2);
      2619          }
      2620  
      2621          private int GetMinBitrate(int sourceBitrate, int requestedBitrate)
      2622          {
      2623              // these values were chosen from testing to improve low bitrate streams
      2624              if (sourceBitrate <= 2000000)
      2625              {
      2626                  sourceBitrate = Convert.ToInt32(sourceBitrate * 2.5);
```

Karar:E
Not:

### Satir 22 / 30  [Jellyfin]  [etiketli: EVET]

Duzeltme commit'i: `3fc71293b4e8c30b7cdf2b7d2154a8a0c97fad79` - Fix AddProperParentChildRelationBaseItemWithCascade migration deleting all items
Suclanan commit:   `1736a566ccad07142bde86c0b450175f943e5848` - Fixes FK on unconnected base items (#14863) (2025-09-26)
Dosya: `src/Jellyfin.Database/Jellyfin.Database.Providers.Sqlite/Migrations/20250913211637_AddProperParentChildRelationBaseItemWithCascade.cs`:18-33

Duzeltmenin ilgili hunk'i:

```diff
@@ -15,22 +15,22 @@ DELETE FROM BaseItems
         WHERE
         ParentId IS NOT NULL
         AND
-        NOT EXISTS(SELECT 1 FROM BaseItems parent WHERE ParentId = parent.Id);
+        NOT EXISTS(SELECT 1 FROM BaseItems parent WHERE parent.Id = BaseItems.ParentId);
 DELETE FROM BaseItems
         WHERE
         ParentId IS NOT NULL
         AND
-        NOT EXISTS(SELECT 1 FROM BaseItems parent WHERE ParentId = parent.Id);
+        NOT EXISTS(SELECT 1 FROM BaseItems parent WHERE parent.Id = BaseItems.ParentId);
 DELETE FROM BaseItems
         WHERE
         ParentId IS NOT NULL
         AND
-        NOT EXISTS(SELECT 1 FROM BaseItems parent WHERE ParentId = parent.Id);
+        NOT EXISTS(SELECT 1 FROM BaseItems parent WHERE parent.Id = BaseItems.ParentId);
 DELETE FROM BaseItems
         WHERE
         ParentId IS NOT NULL
         AND
-        NOT EXISTS(SELECT 1 FROM BaseItems parent WHERE ParentId = parent.Id);
+        NOT EXISTS(SELECT 1 FROM BaseItems parent WHERE parent.Id = BaseItems.ParentId);
 """);
             migrationBuilder.AddForeignKey(
                 name: "FK_BaseItems_BaseItems_ParentId",

```

Suclanan satirin baglami:

```csharp
        10          /// <inheritdoc />
        11          protected override void Up(MigrationBuilder migrationBuilder)
        12          {
        13              migrationBuilder.Sql("""
        14  DELETE FROM BaseItems
        15          WHERE
        16          ParentId IS NOT NULL
        17          AND
>>>     18          NOT EXISTS(SELECT 1 FROM BaseItems parent WHERE ParentId = parent.Id);
>>>     19  DELETE FROM BaseItems
>>>     20          WHERE
>>>     21          ParentId IS NOT NULL
>>>     22          AND
>>>     23          NOT EXISTS(SELECT 1 FROM BaseItems parent WHERE ParentId = parent.Id);
>>>     24  DELETE FROM BaseItems
>>>     25          WHERE
>>>     26          ParentId IS NOT NULL
>>>     27          AND
>>>     28          NOT EXISTS(SELECT 1 FROM BaseItems parent WHERE ParentId = parent.Id);
>>>     29  DELETE FROM BaseItems
>>>     30          WHERE
>>>     31          ParentId IS NOT NULL
>>>     32          AND
>>>     33          NOT EXISTS(SELECT 1 FROM BaseItems parent WHERE ParentId = parent.Id);
        34  """);
        35              migrationBuilder.AddForeignKey(
        36                  name: "FK_BaseItems_BaseItems_ParentId",
        37                  table: "BaseItems",
        38                  column: "ParentId",
        39                  principalTable: "BaseItems",
        40                  principalColumn: "Id",
        41                  onDelete: ReferentialAction.Cascade);
```

Karar:
Not:

### Satir 23 / 30  [Jellyfin]  [etiketli: EVET]

Duzeltme commit'i: `6985a4f2558ac120e14327fc6addf656feca23a8` - Fix SortCriteria and refactor SetSorting
Suclanan commit:   `0ebd233c4148b8628326e5695dffb47dd23924c0` - update dlna music folders (2017-07-22)
Dosya: `Emby.Dlna/ContentDirectory/ControlHandler.cs`:686-688

Duzeltmenin ilgili hunk'i:

```diff
@@ -683,9 +681,9 @@ namespace Emby.Dlna.ContentDirectory
             var query = new InternalItemsQuery(user)
             {
                 StartIndex = startIndex,
-                Limit = limit
+                Limit = limit,
+                OrderBy = GetOrderBy(sort, false)
             };
-            SetSorting(query, sort, false);
 
             switch (stubType)
             {
```

Suclanan satirin baglami:

```csharp
       678          /// <param name="startIndex">The start index.</param>
       679          /// <param name="limit">The maximum number to return.</param>
       680          /// <returns>The <see cref="QueryResult{ServerItem}"/>.</returns>
       681          private QueryResult<ServerItem> GetMusicFolders(BaseItem item, User user, StubType? stubType, SortCriteria sort, int? startIndex, int? limit)
       682          {
       683              var query = new InternalItemsQuery(user)
       684              {
       685                  StartIndex = startIndex,
>>>    686                  Limit = limit
>>>    687              };
>>>    688              SetSorting(query, sort, false);
       689  
       690              switch (stubType)
       691              {
       692                  case StubType.Latest:
       693                      return GetLatest(item, query, nameof(Audio));
       694                  case StubType.Playlists:
       695                      return GetMusicPlaylists(query);
       696                  case StubType.Albums:
```

Karar:E
Not:

### Satir 24 / 30  [Jellyfin]  [etiketli: EVET]

Duzeltme commit'i: `b5f5b02787a9a05376bf23836e618030c69541e3` - Fix special features filter
Suclanan commit:   `5996c4afce11249804d24f1caa3a99b390543c4d` - Complete LinkedChildren integration and batch DTO optimizations (2026-01-17)
Dosya: `Jellyfin.Server.Implementations/Item/BaseItemRepository.cs`:3963

Duzeltmenin ilgili hunk'i:

```diff
@@ -3960,7 +3960,12 @@ public sealed class BaseItemRepository
         if (filter.HasSpecialFeature.HasValue)
         {
             var itemsWithExtras = context.BaseItems
-                .Where(extra => extra.OwnerId != null)
+                .Where(extra => extra.OwnerId != null
+                    && extra.ExtraType != null
+                    && extra.ExtraType != BaseItemExtraType.Unknown
+                    && extra.ExtraType != BaseItemExtraType.Trailer
+                    && extra.ExtraType != BaseItemExtraType.ThemeSong
+                    && extra.ExtraType != BaseItemExtraType.ThemeVideo)
                 .Select(extra => extra.OwnerId!.Value)
                 .Distinct();
 

```

Suclanan satirin baglami:

```csharp
      3955              baseQuery = filter.IsPlaceHolder.Value
      3956                  ? baseQuery.WhereItemOrDescendantMatches(context, isPlaceHolder)
      3957                  : baseQuery.WhereNeitherItemNorDescendantMatches(context, isPlaceHolder);
      3958          }
      3959  
      3960          if (filter.HasSpecialFeature.HasValue)
      3961          {
      3962              var itemsWithExtras = context.BaseItems
>>>   3963                  .Where(extra => extra.OwnerId != null)
      3964                  .Select(extra => extra.OwnerId!.Value)
      3965                  .Distinct();
      3966  
      3967              Expression<Func<BaseItemEntity, bool>> hasExtras = e => itemsWithExtras.Contains(e.Id);
      3968  
      3969              baseQuery = filter.HasSpecialFeature.Value
      3970                  ? baseQuery.WhereItemOrDescendantMatches(context, hasExtras)
      3971                  : baseQuery.WhereNeitherItemNorDescendantMatches(context, hasExtras);
```

Karar:
Not:

### Satir 25 / 30  [Jellyfin]  [etiketli: EVET]

Duzeltme commit'i: `1c78482b480034738516596248955e3e09756dd6` - Use authorization code from api-migration to fix startup wizard
Suclanan commit:   `4ad96e4ff5756ddb235a061231469b30c1ff984d` - update logging levels (2015-10-04)
Dosya: `Emby.Server.Implementations/HttpServer/Security/AuthorizationContext.cs`:72

Duzeltmenin ilgili hunk'i:

```diff
@@ -64,20 +89,20 @@ namespace Emby.Server.Implementations.HttpServer.Security
 
             if (string.IsNullOrEmpty(token))
             {
-                token = httpReq.Headers["X-Emby-Token"];
+                token = headers["X-Emby-Token"];
             }
 
             if (string.IsNullOrEmpty(token))
             {
-                token = httpReq.Headers["X-MediaBrowser-Token"];
+                token = headers["X-MediaBrowser-Token"];
             }
 
             if (string.IsNullOrEmpty(token))
             {
-                token = httpReq.QueryString["api_key"];
+                token = queryString["api_key"];
             }
 
-            var info = new AuthorizationInfo
+            var authInfo = new AuthorizationInfo
             {
                 Client = client,
                 Device = device,
```

Suclanan satirin baglami:

```csharp
        64  
        65              if (string.IsNullOrEmpty(token))
        66              {
        67                  token = httpReq.Headers["X-Emby-Token"];
        68              }
        69  
        70              if (string.IsNullOrEmpty(token))
        71              {
>>>     72                  token = httpReq.Headers["X-MediaBrowser-Token"];
        73              }
        74  
        75              if (string.IsNullOrEmpty(token))
        76              {
        77                  token = httpReq.QueryString["api_key"];
        78              }
        79  
        80              var info = new AuthorizationInfo
```

Karar:E
Not:

### Satir 26 / 30  [Jellyfin]  [etiketli: HAYIR]

Duzeltme commit'i: `5cfa466d8b0069e04c7d5c4e4f9b9a4bb7464034` - fix: cap GetVideoBitrateParamValue at 400 Mbps (#16467)
Aday commit:       `0fc288936d10afc146780d118361f2e722768ee6` - Enable VideoToolbox AV1 decode (2024-12-09)
Dosya: `MediaBrowser.Controller/MediaEncoding/EncodingHelper.cs`:6943

SZZ neden suclamadi: Bu commit'in satirlari (6943) duzeltmenin sildigi satirlar arasinda degil; en yakin silinen satir 2618, arada 4325 satir var.

Duzeltmenin ilgili hunk'i:

```diff
(satir 6943 hicbir hunk'a denk gelmiyor; dosyanin ilk hunk'i)
@@ -2614,8 +2614,16 @@ namespace MediaBrowser.Controller.MediaEncoding
                 }
             }
 
-            // Cap the max target bitrate to intMax/2 to satisfy the bufsize=bitrate*2.
-            return Math.Min(bitrate ?? 0, int.MaxValue / 2);
+            // Cap the max target bitrate to 400 Mbps.
+            // No consumer or professional hardware transcode target exceeds this value
+            // (Intel QSV tops out at ~300 Mbps for H.264; HEVC High Tier Level 5.x is ~240 Mbps).
+            // Without this cap, plugin-provided MPEG-TS streams with no usable bitrate metadata
+            // can produce unreasonably large -bufsize/-maxrate values for the encoder.
+            // Note: the existing FallbackMaxStreamingBitrate mechanism (default 30 Mbps) only
+            // applies when a LiveStreamId is set (M3U/HDHR sources). Plugin streams and other
+            // sources that bypass the LiveTV pipeline are not covered by it.
+            const int MaxSaneBitrate = 400_000_000; // 400 Mbps
+            return Math.Min(bitrate ?? 0, MaxSaneBitrate);
         }
 
         private int GetMinBitrate(int sourceBitrate, int requestedBitrate)

```

Suclanan satirin baglami:

```csharp
      6935              var is8_10_12bitSwFormatsVt = is8_10bitSwFormatsVt
      6936                  || string.Equals("yuv422p", videoStream.PixelFormat, StringComparison.OrdinalIgnoreCase)
      6937                  || string.Equals("yuv444p", videoStream.PixelFormat, StringComparison.OrdinalIgnoreCase)
      6938                  || string.Equals("yuv422p10le", videoStream.PixelFormat, StringComparison.OrdinalIgnoreCase)
      6939                  || string.Equals("yuv444p10le", videoStream.PixelFormat, StringComparison.OrdinalIgnoreCase)
      6940                  || string.Equals("yuv420p12le", videoStream.PixelFormat, StringComparison.OrdinalIgnoreCase)
      6941                  || string.Equals("yuv422p12le", videoStream.PixelFormat, StringComparison.OrdinalIgnoreCase)
      6942                  || string.Equals("yuv444p12le", videoStream.PixelFormat, StringComparison.OrdinalIgnoreCase);
>>>   6943              var isAv1SupportedSwFormatsVt = is8_10bitSwFormatsVt || string.Equals("yuv420p12le", videoStream.PixelFormat, StringComparison.OrdinalIgnoreCase);
      6944  
      6945              // The related patches make videotoolbox hardware surface working is only available in jellyfin-ffmpeg 7.0.1 at the moment.
      6946              bool useHwSurface = (_mediaEncoder.EncoderVersion >= _minFFmpegWorkingVtHwSurface) && IsVideoToolboxFullSupported();
      6947  
      6948              if (is8bitSwFormatsVt)
      6949              {
      6950                  if (string.Equals("vp8", videoStream.Codec, StringComparison.OrdinalIgnoreCase))
      6951                  {
```

Karar:E
Not:

### Satir 27 / 30  [Jellyfin]  [etiketli: HAYIR]

Duzeltme commit'i: `3fc71293b4e8c30b7cdf2b7d2154a8a0c97fad79` - Fix AddProperParentChildRelationBaseItemWithCascade migration deleting all items
Aday commit:       `a0b3e2b071509f440db10768f6f8984c7ea382d6` - Optimize internal querying of UserData, other fixes (#14795) (2025-09-16)
Dosya: `src/Jellyfin.Database/Jellyfin.Database.Providers.Sqlite/Migrations/20250913211637_AddProperParentChildRelationBaseItemWithCascade.cs`:35-52

SZZ neden suclamadi: Bu commit'in satirlari (35-52) duzeltmenin sildigi satirlar arasinda degil; en yakin silinen satir 33, arada 2 satir var.

Duzeltmenin ilgili hunk'i:

```diff
@@ -15,22 +15,22 @@ DELETE FROM BaseItems
         WHERE
         ParentId IS NOT NULL
         AND
-        NOT EXISTS(SELECT 1 FROM BaseItems parent WHERE ParentId = parent.Id);
+        NOT EXISTS(SELECT 1 FROM BaseItems parent WHERE parent.Id = BaseItems.ParentId);
 DELETE FROM BaseItems
         WHERE
         ParentId IS NOT NULL
         AND
-        NOT EXISTS(SELECT 1 FROM BaseItems parent WHERE ParentId = parent.Id);
+        NOT EXISTS(SELECT 1 FROM BaseItems parent WHERE parent.Id = BaseItems.ParentId);
 DELETE FROM BaseItems
         WHERE
         ParentId IS NOT NULL
         AND
-        NOT EXISTS(SELECT 1 FROM BaseItems parent WHERE ParentId = parent.Id);
+        NOT EXISTS(SELECT 1 FROM BaseItems parent WHERE parent.Id = BaseItems.ParentId);
 DELETE FROM BaseItems
         WHERE
         ParentId IS NOT NULL
         AND
-        NOT EXISTS(SELECT 1 FROM BaseItems parent WHERE ParentId = parent.Id);
+        NOT EXISTS(SELECT 1 FROM BaseItems parent WHERE parent.Id = BaseItems.ParentId);
 """);
             migrationBuilder.AddForeignKey(
                 name: "FK_BaseItems_BaseItems_ParentId",

```

Suclanan satirin baglami:

```csharp
        27          AND
        28          NOT EXISTS(SELECT 1 FROM BaseItems parent WHERE ParentId = parent.Id);
        29  DELETE FROM BaseItems
        30          WHERE
        31          ParentId IS NOT NULL
        32          AND
        33          NOT EXISTS(SELECT 1 FROM BaseItems parent WHERE ParentId = parent.Id);
        34  """);
>>>     35              migrationBuilder.AddForeignKey(
>>>     36                  name: "FK_BaseItems_BaseItems_ParentId",
>>>     37                  table: "BaseItems",
>>>     38                  column: "ParentId",
>>>     39                  principalTable: "BaseItems",
>>>     40                  principalColumn: "Id",
>>>     41                  onDelete: ReferentialAction.Cascade);
>>>     42          }
>>>     43  
>>>     44          /// <inheritdoc />
>>>     45          protected override void Down(MigrationBuilder migrationBuilder)
>>>     46          {
>>>     47              migrationBuilder.DropForeignKey(
>>>     48                  name: "FK_BaseItems_BaseItems_ParentId",
>>>     49                  table: "BaseItems");
>>>     50          }
>>>     51      }
>>>     52  }
        53  
```

Karar:E
Not:

### Satir 28 / 30  [Jellyfin]  [etiketli: HAYIR]

Duzeltme commit'i: `81c0451b5e578bb8a41dcb81f2766dbd1eb7f055` - Fix response code & docs
Aday commit:       `2f2bceb1104d8ea669ca21fc40200247aca956ed` - Remove default parameter values (2020-05-19)
Dosya: `Jellyfin.Api/Controllers/ScheduledTasksController.cs`:40-41

SZZ neden suclamadi: Bu commit'in satirlari (40-41) duzeltmenin sildigi satirlar arasinda degil; en yakin silinen satir 95, arada 54 satir var.

Duzeltmenin ilgili hunk'i:

```diff
(satir 40-41 hicbir hunk'a denk gelmiyor; dosyanin ilk hunk'i)
@@ -92,7 +92,7 @@ namespace Jellyfin.Api.Controllers
         /// <response code="404">Task not found.</response>
         /// <returns>An <see cref="NoContentResult"/> on success, or a <see cref="NotFoundResult"/> if the file could not be found.</returns>
         [HttpPost("Running/{taskId}")]
-        [ProducesResponseType(StatusCodes.Status200OK)]
+        [ProducesResponseType(StatusCodes.Status204NoContent)]
         [ProducesResponseType(StatusCodes.Status404NotFound)]
         public ActionResult StartTask([FromRoute] string taskId)
         {
```

Suclanan satirin baglami:

```csharp
        32          /// </summary>
        33          /// <param name="isHidden">Optional filter tasks that are hidden, or not.</param>
        34          /// <param name="isEnabled">Optional filter tasks that are enabled, or not.</param>
        35          /// <response code="200">Scheduled tasks retrieved.</response>
        36          /// <returns>The list of scheduled tasks.</returns>
        37          [HttpGet]
        38          [ProducesResponseType(StatusCodes.Status200OK)]
        39          public IEnumerable<IScheduledTaskWorker> GetTasks(
>>>     40              [FromQuery] bool? isHidden,
>>>     41              [FromQuery] bool? isEnabled)
        42          {
        43              IEnumerable<IScheduledTaskWorker> tasks = _taskManager.ScheduledTasks.OrderBy(o => o.Name);
        44  
        45              foreach (var task in tasks)
        46              {
        47                  if (task.ScheduledTask is IConfigurableScheduledTask scheduledTask)
        48                  {
        49                      if (isHidden.HasValue && isHidden.Value != scheduledTask.IsHidden)
```

Karar:E
Not:

### Satir 29 / 30  [Jellyfin]  [etiketli: HAYIR]

Duzeltme commit'i: `9849f522efa21097fcd827635ef75535bb821bf0` - fix playlist runtime display
Aday commit:       `dfe91e43b676915b840f0958e331ba2cb57966d4` - Added IDtoService (2013-09-04)
Dosya: `MediaBrowser.Server.Implementations/Dto/DtoService.cs`:1505

SZZ neden suclamadi: Bu commit'in satirlari (1505) duzeltmenin sildigi satirlar arasinda degil; en yakin silinen satir 1650, arada 145 satir var.

Duzeltmenin ilgili hunk'i:

```diff
(satir 1505 hicbir hunk'a denk gelmiyor; dosyanin ilk hunk'i)
@@ -1647,7 +1647,8 @@ namespace MediaBrowser.Server.Implementations.Dto
                 IsFolder = false,
                 Recursive = true,
                 IsVirtualUnaired = false,
-                IsMissing = false
+                IsMissing = false,
+                User = user
 
             }).Result.Items;
 

```

Suclanan satirin baglami:

```csharp
      1497  
      1498              var gameSystem = item as GameSystem;
      1499  
      1500              if (gameSystem != null)
      1501              {
      1502                  SetGameSystemProperties(dto, gameSystem);
      1503              }
      1504  
>>>   1505              var musicVideo = item as MusicVideo;
      1506              if (musicVideo != null)
      1507              {
      1508                  SetMusicVideoProperties(dto, musicVideo);
      1509              }
      1510  
      1511              var book = item as Book;
      1512              if (book != null)
      1513              {
```

Karar:E
Not:

### Satir 30 / 30  [Jellyfin]  [etiketli: HAYIR]

Duzeltme commit'i: `1c78482b480034738516596248955e3e09756dd6` - Use authorization code from api-migration to fix startup wizard
Aday commit:       `91ffff7771cb4ae9f89dbc2cb7a5cec70a3301c2` - added dlna music folders (2014-09-05)
Dosya: `Emby.Server.Implementations/HttpServer/Security/AuthorizationContext.cs`:232

SZZ neden suclamadi: Bu commit'in satirlari (232) duzeltmenin sildigi satirlar arasinda degil; en yakin silinen satir 170, arada 62 satir var.

Duzeltmenin ilgili hunk'i:

```diff
(satir 232 hicbir hunk'a denk gelmiyor; dosyanin ilk hunk'i)
@@ -8,6 +8,7 @@ using MediaBrowser.Controller.Library;
 using MediaBrowser.Controller.Net;
 using MediaBrowser.Controller.Security;
 using MediaBrowser.Model.Services;
+using Microsoft.AspNetCore.Http;
 using Microsoft.Net.Http.Headers;
 
 namespace Emby.Server.Implementations.HttpServer.Security
```

Suclanan satirin baglami:

```csharp
       224              foreach (var item in parts)
       225              {
       226                  var param = item.Trim().Split(new[] { '=' }, 2);
       227  
       228                  if (param.Length == 2)
       229                  {
       230                      var value = NormalizeValue(param[1].Trim(new[] { '"' }));
       231                      result.Add(param[0], value);
>>>    232                  }
       233              }
       234  
       235              return result;
       236          }
       237  
       238          private static string NormalizeValue(string value)
       239          {
       240              if (string.IsNullOrEmpty(value))
```

Karar:E
Not:

