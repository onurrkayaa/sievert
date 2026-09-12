# Model tahminlerinin elle dogrulanmasi: malzeme

**Olcut dosyasi:** `docs/olcumler/asama5-tahmin-dogrulama-olcut.md`

**Orneklem tohumu:** 20260912 (secim), 20260913 (karistirma)

**Ureten kod commit'i:** `a09078b`

**Repo dagilimi:** github.com/app-vnext/polly 10, github.com/jellyfin/jellyfin 10, github.com/sharex/sharex 10

**Uygunluk kurali:** yalnizca `CsFilesChanged > 0` olan test commit'leri. Sebep: degerlendirici kaynak kod degisikligine bakacak. Baska boyut, churn ya da dosya sayisi filtresi uygulanmadi; botlar dislanmadi.

**Korlenen alanlar (bu dosyada YOK):** modelin tahmini, modelin olasiligi, egitimde secilen esik, otomatik uretilmis hata etiketi, etiketin kaynagi ve modelin bu etikete gore dogru/yanlis hucresi.

> **Uyari:** kararlar tamamlanmadan `data/asama5/prediction-validation-key.csv` acilmaz.

Her ornek icin yalnizca kronolojik olgular veriliyor. Sonraki commit'lerin hicbiri "ilgili duzeltme" diye isaretlenmedi.

---

### Ornek SAMPLE-01 — github.com/app-vnext/polly

Commit:

- Kisa SHA: `99883e91edd5`
- Yazar tarihi: 2025-02-16
- Mesaj basligi: Improve mutation score (#2506)
- Baglanti: https://github.com/app-vnext/polly/commit/99883e91edd5b7e33bfd08af52fc4453cb204bfb

Degisiklik ozeti:

- Degisen toplam dosya: 57
- Degisen `.cs` dosyasi: 55
- Eklenen satir: 1868, silinen satir: 264
- Degisen `.cs` dosyalari:
  - `src/Polly/Bulkhead/BulkheadSemaphoreFactory.cs`
  - `src/Polly/Caching/AsyncCacheTResultSyntax.cs`
  - `src/Polly/Caching/AsyncGenericCacheProvider.cs`
  - `src/Polly/Caching/GenericCacheProvider.cs`
  - `src/Polly/Caching/NonSlidingTtl.cs`
  - `src/Polly/CircuitBreaker/AdvancedCircuitController.cs`
  - `src/Polly/CircuitBreaker/CircuitStateController.cs`
  - `src/Polly/CircuitBreaker/RollingHealthMetrics.cs`
  - `src/Polly/Policy.HandleSyntax.cs`
  - `src/Polly/Policy.SyncNonGenericImplementation.cs`
  - `src/Polly/PolicyBuilder.OrSyntax.cs`
  - `src/Polly/RateLimit/AsyncRateLimitSyntax.cs`
  - `src/Polly/RateLimit/AsyncRateLimitTResultSyntax.cs`
  - `src/Polly/RateLimit/LockFreeTokenBucketRateLimiter.cs`
  - `src/Polly/RateLimit/RateLimitSyntax.cs`
  - `src/Polly/RateLimit/RateLimitTResultSyntax.cs`
  - `src/Polly/RateLimit/RateLimiterFactory.cs`
  - `src/Polly/Registry/PolicyRegistry.cs`
  - `src/Polly/Retry/AsyncRetrySyntax.cs`
  - `src/Polly/Retry/AsyncRetryTResultSyntax.cs`
  - `src/Polly/Retry/RetryTResultSyntax.cs`
  - `src/Polly/Timeout/AsyncTimeoutEngine.cs`
  - `src/Polly/Timeout/AsyncTimeoutSyntax.cs`
  - `src/Polly/Timeout/TimeoutSyntax.cs`
  - `test/Polly.Specs/Caching/AbsoluteTtlSpecs.cs`
  - `test/Polly.Specs/Caching/AsyncSerializingCacheProviderSpecs.cs`
  - `test/Polly.Specs/Caching/CacheAsyncSpecs.cs`
  - `test/Polly.Specs/Caching/CacheTResultAsyncSpecs.cs`
  - `test/Polly.Specs/CircuitBreaker/AdvancedCircuitBreakerAsyncSpecs.cs`
  - `test/Polly.Specs/CircuitBreaker/AdvancedCircuitBreakerSpecs.cs`
  - `test/Polly.Specs/CircuitBreaker/RollingHealthMetricsTests.cs`
  - `test/Polly.Specs/Fallback/FallbackAsyncSpecs.cs`
  - `test/Polly.Specs/Fallback/FallbackSpecs.cs`
  - `test/Polly.Specs/Fallback/FallbackTResultAsyncSpecs.cs`
  - `test/Polly.Specs/Fallback/FallbackTResultSpecs.cs`
  - `test/Polly.Specs/RateLimit/AsyncRateLimitPolicySpecs.cs`
  - `test/Polly.Specs/RateLimit/AsyncRateLimitPolicyTResultSpecs.cs`
  - `test/Polly.Specs/RateLimit/RateLimitPolicySpecs.cs`
  - `test/Polly.Specs/RateLimit/RateLimitPolicyTResultSpecs.cs`
  - `test/Polly.Specs/RateLimit/RateLimitRejectedExceptionTests.cs`
  - `test/Polly.Specs/Registry/PolicyRegistrySpecs.cs`
  - `test/Polly.Specs/ResiliencePipelineConversionExtensionsTests.cs`
  - `test/Polly.Specs/Retry/RetryForeverAsyncSpecs.cs`
  - `test/Polly.Specs/Retry/RetryForeverSpecs.cs`
  - `test/Polly.Specs/Retry/RetryTResultMixedResultExceptionSpecs.cs`
  - `test/Polly.Specs/Retry/WaitAndRetryAsyncSpecs.cs`
  - `test/Polly.Specs/Retry/WaitAndRetryForeverAsyncSpecs.cs`
  - `test/Polly.Specs/Retry/WaitAndRetryForeverSpecs.cs`
  - `test/Polly.Specs/Retry/WaitAndRetryForeverTResultSpecs.cs`
  - `test/Polly.Specs/Retry/WaitAndRetrySpecs.cs`
  - `test/Polly.Specs/Retry/WaitAndRetryTResultAsyncSpecs.cs`
  - `test/Polly.Specs/Retry/WaitAndRetryTResultSpecs.cs`
  - `test/Polly.Specs/Timeout/TimeoutAsyncSpecs.cs`
  - `test/Polly.Specs/Timeout/TimeoutSpecs.cs`
  - `test/Polly.Specs/Timeout/TimeoutTResultAsyncSpecs.cs`

Commit'in ilgili C# diff'i:

```diff
--- src/Polly/Bulkhead/BulkheadSemaphoreFactory.cs
--- a/src/Polly/Bulkhead/BulkheadSemaphoreFactory.cs
+++ b/src/Polly/Bulkhead/BulkheadSemaphoreFactory.cs
@@ -7,9 +7,7 @@ internal static class BulkheadSemaphoreFactory
     {
         var maxParallelizationSemaphore = new SemaphoreSlim(maxParallelization, maxParallelization);
 
-        var maxQueuingCompounded = maxQueueingActions <= int.MaxValue - maxParallelization
-            ? maxQueueingActions + maxParallelization
-            : int.MaxValue;
+        var maxQueuingCompounded = Math.Min(maxQueueingActions + maxParallelization, int.MaxValue);
         var maxQueuedActionsSemaphore = new SemaphoreSlim(maxQueuingCompounded, maxQueuingCompounded);
 
         return (maxParallelizationSemaphore, maxQueuedActionsSemaphore);
--- src/Polly/Caching/AsyncCacheTResultSyntax.cs
--- a/src/Polly/Caching/AsyncCacheTResultSyntax.cs
+++ b/src/Polly/Caching/AsyncCacheTResultSyntax.cs
@@ -22,7 +22,7 @@ public partial class Policy
             throw new ArgumentNullException(nameof(cacheProvider));
         }
 
-        return CacheAsync<TResult>(cacheProvider.AsyncFor<TResult>(), new RelativeTtl(ttl), DefaultCacheKeyStrategy.Instance.GetCacheKey, onCacheError);
+        return CacheAsync(cacheProvider.AsyncFor<TResult>(), new RelativeTtl(ttl), DefaultCacheKeyStrategy.Instance.GetCacheKey, onCacheError);
     }
 
     /// <summary>
@@ -45,7 +45,7 @@ public partial class Policy
             throw new ArgumentNullException(nameof(cacheProvider));
         }
 
-        return CacheAsync<TResult>(cacheProvider.AsyncFor<TResult>(), ttlStrategy, DefaultCacheKeyStrategy.Instance.GetCacheKey, onCacheError);
+        return CacheAsync(cacheProvider.AsyncFor<TResult>(), ttlStrategy, DefaultCacheKeyStrategy.Instance.GetCacheKey, onCacheError);
     }
 
     /// <summary>
@@ -78,7 +78,7 @@ public partial class Policy
             throw new ArgumentNullException(nameof(cacheKeyStrategy));
         }
 
-        return CacheAsync<TResult>(cacheProvider.AsyncFor<TResult>(), new RelativeTtl(ttl), cacheKeyStrategy.GetCacheKey, onCacheError);
+        return CacheAsync(cacheProvider.AsyncFor<TResult>(), new RelativeTtl(ttl), cacheKeyStrategy.GetCacheKey, onCacheError);
     }
 
     /// <summary>
@@ -112,7 +112,7 @@ public partial class Policy
             throw new ArgumentNullException(nameof(cacheKeyStrategy));
         }
 
-        return CacheAsync<TResult>(cacheProvider.AsyncFor<TResult>(), ttlStrategy, cacheKeyStrategy.GetCacheKey, onCacheError);
+        return CacheAsync(cacheProvider.AsyncFor<TResult>(), ttlStrategy, cacheKeyStrategy.GetCacheKey, onCacheError);
     }
 
     /// <summary>
@@ -136,7 +136,7 @@ public partial class Policy
             throw new ArgumentNullException(nameof(cacheProvider));
         }
 
-        return CacheAsync<TResult>(cacheProvider.AsyncFor<TResult>(), new RelativeTtl(ttl), cacheKeyStrategy, onCacheError);
+        return CacheAsync(cacheProvider.AsyncFor<TResult>(), new RelativeTtl(ttl), cacheKeyStrategy, onCacheError);
     }
 
     /// <summary>
@@ -161,7 +161,7 @@ public partial class Policy
             throw new ArgumentNullException(nameof(cacheProvider));
         }
 
-        return CacheAsync<TResult>(cacheProvider.AsyncFor<TResult>(), ttlStrategy, cacheKeyStrategy, onCacheError);
+        return CacheAsync(cacheProvider.AsyncFor<TResult>(), ttlStrategy, cacheKeyStrategy, onCacheError);
     }
 
     /// <summary>
@@ -197,7 +197,7 @@ public partial class Policy
             throw new ArgumentNullException(nameof(cacheProvider));
         }
 
-        return CacheAsync<TResult>(cacheProvider.AsyncFor<TResult>(), new RelativeTtl(ttl), DefaultCacheKeyStrategy.Instance.GetCacheKey, onCacheGet, onCacheMiss, onCachePut, onCacheGetError, onCachePutError);
+        return CacheAsync(cacheProvider.AsyncFor<TResult>(), new RelativeTtl(ttl), DefaultCacheKeyStrategy.Instance.GetCacheKey, onCacheGet, onCacheMiss, onCachePut, onCacheGetError, onCachePutError);
     }
 
     /// <summary>
@@ -234,7 +234,7 @@ public partial class Policy
             throw new ArgumentNullException(nameof(cacheProvider));
         }
 
-        return CacheAsync<TResult>(cacheProvider.AsyncFor<TResult>(), ttlStrategy, DefaultCacheKeyStrategy.Instance.GetCacheKey, onCacheGet, onCacheMiss, onCachePut, onCacheGetError, onCachePutError);
+        return CacheAsync(cacheProvider.AsyncFor<TResult>(), ttlStrategy, DefaultCacheKeyStrategy.Instance.GetCacheKey, onCacheGet, onCacheMiss, onCachePut, onCacheGetError, onCachePutError);
     }
 
     /// <summary>
@@ -278,7 +278,7 @@ public partial class Policy
             throw new ArgumentNullException(nameof(cacheKeyStrategy));
         }
 
-        return CacheAsync<TResult>(cacheProvider.AsyncFor<TResult>(), new RelativeTtl(ttl), cacheKeyStrategy.GetCacheKey, onCacheGet, onCacheMiss, onCachePut, onCacheGetError, onCachePutError);
+        return CacheAsync(cacheProvider.AsyncFor<TResult>(), new RelativeTtl(ttl), cacheKeyStrategy.GetCacheKey, onCacheGet, onCacheMiss, onCachePut, onCacheGetError, onCachePutError);
     }
 
     /// <summary>
@@ -323,7 +323,7 @@ public partial class Policy
             throw new ArgumentNullException(nameof(cacheKeyStrategy));
         }
 
-        return CacheAsync<TResult>(cacheProvider.AsyncFor<TResult>(), ttlStrategy, cacheKeyStrategy.GetCacheKey, onCacheGet, onCacheMiss, onCachePut, onCacheGetError, onCachePutError);
+        return CacheAsync(cacheProvider.AsyncFor<TResult>(), ttlStrategy, cacheKeyStrategy.GetCacheKey, onCacheGet, onCacheMiss, onCachePut, onCacheGetError, onCachePutError);
     }
 
     /// <summary>
@@ -362,7 +362,7 @@ public partial class Policy
             throw new ArgumentNullException(nameof(cacheProvider));
         }
 
-        return CacheAsync<TResult>(cacheProvider.AsyncFor<TResult>(), new RelativeTtl(ttl), cacheKeyStrategy, onCacheGet, onCacheMiss, onCachePut, onCacheGetError, onCachePutError);
+        return CacheAsync(cacheProvider.AsyncFor<TResult>(), new RelativeTtl(ttl), cacheKeyStrategy, onCacheGet, onCacheMiss, onCachePut, onCacheGetError, onCachePutError);
     }
 
     /// <summary>
@@ -402,7 +402,7 @@ public partial class Policy
             throw new ArgumentNullException(nameof(cacheProvider));
         }
 
-        return CacheAsync<TResult>(cacheProvider.AsyncFor<TResult>(), ttlStrategy, cacheKeyStrategy, onCacheGet, onCacheMiss, onCachePut, onCacheGetError, onCachePutError);
+        return CacheAsync(cacheProvider.AsyncFor<TResult>(), ttlStrategy, cacheKeyStrategy, onCacheGet, onCacheMiss, onCachePut, onCacheGetError, onCachePutError);
     }
 
     /// <summary>
@@ -418,7 +418,7 @@ public partial class Policy
     /// <returns>The policy instance.</returns>
     /// <exception cref="ArgumentNullException">Thrown when <paramref name="cacheProvider"/> is <see langword="null"/>.</exception>
     public static AsyncCachePolicy<TResult> CacheAsync<TResult>(IAsyncCacheProvider<TResult> cacheProvider, TimeSpan ttl, Action<Context, string, Exception>? onCacheError = null) =>
-        CacheAsync<TResult>(cacheProvider, new RelativeTtl(ttl), DefaultCacheKeyStrategy.Instance.GetCacheKey, onCacheError);
+        CacheAsync(cacheProvider, new RelativeTtl(ttl), DefaultCacheKeyStrategy.Instance.GetCacheKey, onCacheError);
 
     /// <summary>
     /// <para>Builds an <see cref="AsyncPolicy{TResult}"/> that will function like a result cache for delegate executions returning a <typeparamref name="TResult"/>.</para>
@@ -434,7 +434,7 @@ public partial class Policy
     /// <exception cref="ArgumentNullException">Thrown when <paramref name="cacheProvider"/> is <see langword="null"/>.</exception>
     /// <exception cref="ArgumentNullException">Thrown when <paramref name="ttlStrategy"/> is <see langword="null"/>.</exception>
     public static AsyncCachePolicy<TResult> CacheAsync<TResult>(IAsyncCacheProvider<TResult> cacheProvider, ITtlStrategy ttlStrategy, Action<Context, string, Exception>? onCacheError = null) =>
-        CacheAsync<TResult>(cacheProvider, ttlStrategy, DefaultCacheKeyStrategy.Instance.GetCacheKey, onCacheError);
+        CacheAsync(cacheProvider, ttlStrategy, DefaultCacheKeyStrategy.Instance.GetCacheKey, onCacheError);
 
     /// <summary>
     /// <para>Builds an <see cref="AsyncPolicy{TResult}"/> that will function like a result cache for delegate executions returning a <typeparamref name="TResult"/>.</para>
@@ -450,7 +450,7 @@ public partial class Policy
     /// <exception cref="ArgumentNullException">Thrown when <paramref name="cacheProvider"/> is <see langword="null"/>.</exception>
     /// <exception cref="ArgumentNullException">Thrown when <paramref name="ttlStrategy"/> is <see langword="null"/>.</exception>
     public static AsyncCachePolicy<TResult> CacheAsync<TResult>(IAsyncCacheProvider<TResult> cacheProvider, ITtlStrategy<TResult> ttlStrategy, Action<Context, string, Exception>? onCacheError = null) =>
-        CacheAsync<TResult>(cacheProvider, ttlStrategy, DefaultCacheKeyStrategy.Instance.GetCacheKey, onCacheError);
+        CacheAsync(cacheProvider, ttlStrategy, DefaultCacheKeyStrategy.Instance.GetCacheKey, onCacheError);
 
     /// <summary>
     /// <para>Builds an <see cref="AsyncPolicy{TResult}" /> that will function like a result cache for delegate executions returning a <typeparamref name="TResult"/>.</para>
@@ -477,7 +477,7 @@ public partial class Policy
             throw new ArgumentNullException(nameof(cacheKeyStrategy));
         }
 
-        return CacheAsync<TResult>(cacheProvider, new RelativeTtl(ttl), cacheKeyStrategy.GetCacheKey, onCacheError);
+        return CacheAsync(cacheProvider, new RelativeTtl(ttl), cacheKeyStrategy.GetCacheKey, onCacheError);
     }
 
```

Kirpildi; tam diff: https://github.com/app-vnext/polly/commit/99883e91edd5b7e33bfd08af52fc4453cb204bfb

Sonraki tarihsel olgular:

- `src/Polly/Bulkhead/BulkheadSemaphoreFactory.cs`
  - `bbe8807723c4` 2025-06-10 Fix overflow in BulkheadSemaphoreFactory (#2638)
    https://github.com/app-vnext/polly/commit/bbe8807723c4a3405831112a739312535c4d3caa
- `src/Polly/Caching/AsyncCacheTResultSyntax.cs`
  - Gozlem araliginda sonraki degisiklik yok
- `src/Polly/Caching/AsyncGenericCacheProvider.cs`
  - Gozlem araliginda sonraki degisiklik yok
- `src/Polly/Caching/GenericCacheProvider.cs`
  - Gozlem araliginda sonraki degisiklik yok
- `src/Polly/Caching/NonSlidingTtl.cs`
  - Gozlem araliginda sonraki degisiklik yok
- `src/Polly/CircuitBreaker/AdvancedCircuitController.cs`
  - Gozlem araliginda sonraki degisiklik yok
- `src/Polly/CircuitBreaker/CircuitStateController.cs`
  - Gozlem araliginda sonraki degisiklik yok
- `src/Polly/CircuitBreaker/RollingHealthMetrics.cs`
  - Gozlem araliginda sonraki degisiklik yok
- `src/Polly/Policy.HandleSyntax.cs`
  - Gozlem araliginda sonraki degisiklik yok
- `src/Polly/Policy.SyncNonGenericImplementation.cs`
  - Gozlem araliginda sonraki degisiklik yok
- `src/Polly/PolicyBuilder.OrSyntax.cs`
  - Gozlem araliginda sonraki degisiklik yok
- `src/Polly/RateLimit/AsyncRateLimitSyntax.cs`
  - Gozlem araliginda sonraki degisiklik yok
- `src/Polly/RateLimit/AsyncRateLimitTResultSyntax.cs`
  - Gozlem araliginda sonraki degisiklik yok
- `src/Polly/RateLimit/LockFreeTokenBucketRateLimiter.cs`
  - Gozlem araliginda sonraki degisiklik yok
- `src/Polly/RateLimit/RateLimitSyntax.cs`
  - Gozlem araliginda sonraki degisiklik yok
- `src/Polly/RateLimit/RateLimitTResultSyntax.cs`
  - Gozlem araliginda sonraki degisiklik yok
- `src/Polly/RateLimit/RateLimiterFactory.cs`
  - Gozlem araliginda sonraki degisiklik yok
- `src/Polly/Registry/PolicyRegistry.cs`
  - `dc50439e7462` 2025-02-19 Add missing coverage
    https://github.com/app-vnext/polly/commit/dc50439e746241470df78eb4babc549d94c10e08
- `src/Polly/Retry/AsyncRetrySyntax.cs`
  - `3fb089717fb6` 2025-02-21 Improve coverage (#2526)
    https://github.com/app-vnext/polly/commit/3fb089717fb63c52b51d66a378cdad08b3f6335f
- `src/Polly/Retry/AsyncRetryTResultSyntax.cs`
  - `4469d8392076` 2025-02-17 Add missing mutations (#2508)
    https://github.com/app-vnext/polly/commit/4469d8392076e4025157f437dc5a9f7d8634bc7c
- `src/Polly/Retry/RetryTResultSyntax.cs`
  - Gozlem araliginda sonraki degisiklik yok
- `src/Polly/Timeout/AsyncTimeoutEngine.cs`
  - `74bf31fff0e0` 2025-11-21 Pass timeout to TimeoutRejectedException
    https://github.com/app-vnext/polly/commit/74bf31fff0e0907034994be4df22424aa71818f1
  - `3987bb8c0a87` 2026-08-08 Bump SonarAnalyzer.CSharp from 10.29.0.143774 to 10.31.0.145097 (#3190)
    https://github.com/app-vnext/polly/commit/3987bb8c0a876314ea0e29d200c333f0fdef5e53
- `src/Polly/Timeout/AsyncTimeoutSyntax.cs`
  - `3fb089717fb6` 2025-02-21 Improve coverage (#2526)
    https://github.com/app-vnext/polly/commit/3fb089717fb63c52b51d66a378cdad08b3f6335f
- `src/Polly/Timeout/TimeoutSyntax.cs`
  - Gozlem araliginda sonraki degisiklik yok
- `test/Polly.Specs/Caching/AbsoluteTtlSpecs.cs`
  - Gozlem araliginda sonraki degisiklik yok
- `test/Polly.Specs/Caching/AsyncSerializingCacheProviderSpecs.cs`
  - `6d5fcd6160f5` 2025-08-23 xunit v3 preparation (#2723)
    https://github.com/app-vnext/polly/commit/6d5fcd6160f5b4a2291075131ef4e485d1dacdec
- `test/Polly.Specs/Caching/CacheAsyncSpecs.cs`
  - `6d5fcd6160f5` 2025-08-23 xunit v3 preparation (#2723)
    https://github.com/app-vnext/polly/commit/6d5fcd6160f5b4a2291075131ef4e485d1dacdec
- `test/Polly.Specs/Caching/CacheTResultAsyncSpecs.cs`
  - `6d5fcd6160f5` 2025-08-23 xunit v3 preparation (#2723)
    https://github.com/app-vnext/polly/commit/6d5fcd6160f5b4a2291075131ef4e485d1dacdec
- `test/Polly.Specs/CircuitBreaker/AdvancedCircuitBreakerAsyncSpecs.cs`
  - `3fb089717fb6` 2025-02-21 Improve coverage (#2526)
    https://github.com/app-vnext/polly/commit/3fb089717fb63c52b51d66a378cdad08b3f6335f
  - `6d5fcd6160f5` 2025-08-23 xunit v3 preparation (#2723)
    https://github.com/app-vnext/polly/commit/6d5fcd6160f5b4a2291075131ef4e485d1dacdec
- `test/Polly.Specs/CircuitBreaker/AdvancedCircuitBreakerSpecs.cs`
  - `3fb089717fb6` 2025-02-21 Improve coverage (#2526)
    https://github.com/app-vnext/polly/commit/3fb089717fb63c52b51d66a378cdad08b3f6335f
  - `6d5fcd6160f5` 2025-08-23 xunit v3 preparation (#2723)
    https://github.com/app-vnext/polly/commit/6d5fcd6160f5b4a2291075131ef4e485d1dacdec
- `test/Polly.Specs/CircuitBreaker/RollingHealthMetricsTests.cs`
  - Gozlem araliginda sonraki degisiklik yok
- `test/Polly.Specs/Fallback/FallbackAsyncSpecs.cs`
  - `4469d8392076` 2025-02-17 Add missing mutations (#2508)
    https://github.com/app-vnext/polly/commit/4469d8392076e4025157f437dc5a9f7d8634bc7c
- `test/Polly.Specs/Fallback/FallbackSpecs.cs`
  - `4469d8392076` 2025-02-17 Add missing mutations (#2508)
    https://github.com/app-vnext/polly/commit/4469d8392076e4025157f437dc5a9f7d8634bc7c
- `test/Polly.Specs/Fallback/FallbackTResultAsyncSpecs.cs`
  - `4469d8392076` 2025-02-17 Add missing mutations (#2508)
    https://github.com/app-vnext/polly/commit/4469d8392076e4025157f437dc5a9f7d8634bc7c
  - `6d5fcd6160f5` 2025-08-23 xunit v3 preparation (#2723)
    https://github.com/app-vnext/polly/commit/6d5fcd6160f5b4a2291075131ef4e485d1dacdec
- `test/Polly.Specs/Fallback/FallbackTResultSpecs.cs`
  - `4469d8392076` 2025-02-17 Add missing mutations (#2508)
    https://github.com/app-vnext/polly/commit/4469d8392076e4025157f437dc5a9f7d8634bc7c
  - `3fb089717fb6` 2025-02-21 Improve coverage (#2526)
    https://github.com/app-vnext/polly/commit/3fb089717fb63c52b51d66a378cdad08b3f6335f
  - `6d5fcd6160f5` 2025-08-23 xunit v3 preparation (#2723)
    https://github.com/app-vnext/polly/commit/6d5fcd6160f5b4a2291075131ef4e485d1dacdec
- `test/Polly.Specs/RateLimit/AsyncRateLimitPolicySpecs.cs`
  - Gozlem araliginda sonraki degisiklik yok
- `test/Polly.Specs/RateLimit/AsyncRateLimitPolicyTResultSpecs.cs`
  - Gozlem araliginda sonraki degisiklik yok
- `test/Polly.Specs/RateLimit/RateLimitPolicySpecs.cs`
  - Gozlem araliginda sonraki degisiklik yok
- `test/Polly.Specs/RateLimit/RateLimitPolicyTResultSpecs.cs`
  - `6d5fcd6160f5` 2025-08-23 xunit v3 preparation (#2723)
    https://github.com/app-vnext/polly/commit/6d5fcd6160f5b4a2291075131ef4e485d1dacdec
- `test/Polly.Specs/RateLimit/RateLimitRejectedExceptionTests.cs`
  - Gozlem araliginda sonraki degisiklik yok
- `test/Polly.Specs/Registry/PolicyRegistrySpecs.cs`
  - Gozlem araliginda sonraki degisiklik yok
- `test/Polly.Specs/ResiliencePipelineConversionExtensionsTests.cs`
  - `d36bdd5c9317` 2026-05-22 Bump SonarAnalyzer.CSharp from 10.25.0.139117 to 10.26.0.140279 (#3085)
    https://github.com/app-vnext/polly/commit/d36bdd5c931784ebf5c99ff63601cace46a1d5e8
- `test/Polly.Specs/Retry/RetryForeverAsyncSpecs.cs`
  - `4469d8392076` 2025-02-17 Add missing mutations (#2508)
    https://github.com/app-vnext/polly/commit/4469d8392076e4025157f437dc5a9f7d8634bc7c
  - `3fb089717fb6` 2025-02-21 Improve coverage (#2526)
    https://github.com/app-vnext/polly/commit/3fb089717fb63c52b51d66a378cdad08b3f6335f
  - `6d5fcd6160f5` 2025-08-23 xunit v3 preparation (#2723)
    https://github.com/app-vnext/polly/commit/6d5fcd6160f5b4a2291075131ef4e485d1dacdec
- `test/Polly.Specs/Retry/RetryForeverSpecs.cs`
  - `4469d8392076` 2025-02-17 Add missing mutations (#2508)
    https://github.com/app-vnext/polly/commit/4469d8392076e4025157f437dc5a9f7d8634bc7c
  - `3fb089717fb6` 2025-02-21 Improve coverage (#2526)
    https://github.com/app-vnext/polly/commit/3fb089717fb63c52b51d66a378cdad08b3f6335f
- `test/Polly.Specs/Retry/RetryTResultMixedResultExceptionSpecs.cs`
  - Gozlem araliginda sonraki degisiklik yok
- `test/Polly.Specs/Retry/WaitAndRetryAsyncSpecs.cs`
  - `4469d8392076` 2025-02-17 Add missing mutations (#2508)
    https://github.com/app-vnext/polly/commit/4469d8392076e4025157f437dc5a9f7d8634bc7c
  - `3fb089717fb6` 2025-02-21 Improve coverage (#2526)
    https://github.com/app-vnext/polly/commit/3fb089717fb63c52b51d66a378cdad08b3f6335f
  - `6d5fcd6160f5` 2025-08-23 xunit v3 preparation (#2723)
    https://github.com/app-vnext/polly/commit/6d5fcd6160f5b4a2291075131ef4e485d1dacdec
- `test/Polly.Specs/Retry/WaitAndRetryForeverAsyncSpecs.cs`
  - `3fb089717fb6` 2025-02-21 Improve coverage (#2526)
    https://github.com/app-vnext/polly/commit/3fb089717fb63c52b51d66a378cdad08b3f6335f
  - `6d5fcd6160f5` 2025-08-23 xunit v3 preparation (#2723)
    https://github.com/app-vnext/polly/commit/6d5fcd6160f5b4a2291075131ef4e485d1dacdec
- `test/Polly.Specs/Retry/WaitAndRetryForeverSpecs.cs`
  - `4469d8392076` 2025-02-17 Add missing mutations (#2508)
    https://github.com/app-vnext/polly/commit/4469d8392076e4025157f437dc5a9f7d8634bc7c
- `test/Polly.Specs/Retry/WaitAndRetryForeverTResultSpecs.cs`
  - `3fb089717fb6` 2025-02-21 Improve coverage (#2526)
    https://github.com/app-vnext/polly/commit/3fb089717fb63c52b51d66a378cdad08b3f6335f
- `test/Polly.Specs/Retry/WaitAndRetrySpecs.cs`
  - `4469d8392076` 2025-02-17 Add missing mutations (#2508)
    https://github.com/app-vnext/polly/commit/4469d8392076e4025157f437dc5a9f7d8634bc7c
  - `796be73c98c5` 2026-06-27 Bump dotnet-stryker from 4.14.2 to 4.15.0 (#3127)
    https://github.com/app-vnext/polly/commit/796be73c98c5bad3488b8966b5224f33adc9ed18
- `test/Polly.Specs/Retry/WaitAndRetryTResultAsyncSpecs.cs`
  - `4469d8392076` 2025-02-17 Add missing mutations (#2508)
    https://github.com/app-vnext/polly/commit/4469d8392076e4025157f437dc5a9f7d8634bc7c
- `test/Polly.Specs/Retry/WaitAndRetryTResultSpecs.cs`
  - `3fb089717fb6` 2025-02-21 Improve coverage (#2526)
    https://github.com/app-vnext/polly/commit/3fb089717fb63c52b51d66a378cdad08b3f6335f
- `test/Polly.Specs/Timeout/TimeoutAsyncSpecs.cs`
  - `3fb089717fb6` 2025-02-21 Improve coverage (#2526)
    https://github.com/app-vnext/polly/commit/3fb089717fb63c52b51d66a378cdad08b3f6335f
  - `74bf31fff0e0` 2025-11-21 Pass timeout to TimeoutRejectedException
    https://github.com/app-vnext/polly/commit/74bf31fff0e0907034994be4df22424aa71818f1
- `test/Polly.Specs/Timeout/TimeoutSpecs.cs`
  - `74bf31fff0e0` 2025-11-21 Pass timeout to TimeoutRejectedException
    https://github.com/app-vnext/polly/commit/74bf31fff0e0907034994be4df22424aa71818f1
- `test/Polly.Specs/Timeout/TimeoutTResultAsyncSpecs.cs`
  - `3fb089717fb6` 2025-02-21 Improve coverage (#2526)
    https://github.com/app-vnext/polly/commit/3fb089717fb63c52b51d66a378cdad08b3f6335f

Karar:

[ BAKILMADI ]

Not:

[ ]

---

### Ornek SAMPLE-02 — github.com/app-vnext/polly

Commit:

- Kisa SHA: `32cab5f6a2a4`
- Yazar tarihi: 2025-08-22
- Mesaj basligi: Make delegates static
- Baglanti: https://github.com/app-vnext/polly/commit/32cab5f6a2a47ab0e2713106cc916947dbbff53f

Degisiklik ozeti:

- Degisen toplam dosya: 2
- Degisen `.cs` dosyasi: 2
- Eklenen satir: 2, silinen satir: 2
- Degisen `.cs` dosyalari:
  - `bench/Polly.Core.Benchmarks/Utils/Helper.StrategyPipeline.cs`
  - `src/Polly.Testing/ResiliencePipelineExtensions.cs`

Commit'in ilgili C# diff'i:

```diff
--- bench/Polly.Core.Benchmarks/Utils/Helper.StrategyPipeline.cs
--- a/bench/Polly.Core.Benchmarks/Utils/Helper.StrategyPipeline.cs
+++ b/bench/Polly.Core.Benchmarks/Utils/Helper.StrategyPipeline.cs
@@ -10,7 +10,7 @@ internal static partial class Helper
         {
             for (var i = 0; i < count; i++)
             {
-                builder.AddStrategy(_ => new EmptyResilienceStrategy(), new EmptyResilienceOptions());
+                builder.AddStrategy(static _ => new EmptyResilienceStrategy(), new EmptyResilienceOptions());
             }
         }),
         _ => throw new NotSupportedException()
--- src/Polly.Testing/ResiliencePipelineExtensions.cs
--- a/src/Polly.Testing/ResiliencePipelineExtensions.cs
+++ b/src/Polly.Testing/ResiliencePipelineExtensions.cs
@@ -48,7 +48,7 @@ public static class ResiliencePipelineExtensions
 
         return new ResiliencePipelineDescriptor(
             descriptors,
-            isReloadable: components.Exists(s => s is ReloadableComponent));
+            isReloadable: components.Exists(static s => s is ReloadableComponent));
     }
 
     private static object GetStrategyInstance<T>(PipelineComponent component)
```

Sonraki tarihsel olgular:

- `bench/Polly.Core.Benchmarks/Utils/Helper.StrategyPipeline.cs`
  - Gozlem araliginda sonraki degisiklik yok
- `src/Polly.Testing/ResiliencePipelineExtensions.cs`
  - Gozlem araliginda sonraki degisiklik yok

Karar:

[ BAKILMADI ]

Not:

[ ]

---

### Ornek SAMPLE-03 — github.com/app-vnext/polly

Commit:

- Kisa SHA: `ed1c79be1c0c`
- Yazar tarihi: 2024-07-22
- Mesaj basligi: Fix CA1062 warnings (#2223)
- Baglanti: https://github.com/app-vnext/polly/commit/ed1c79be1c0ce774072c123b52ac51e21be798fb

Degisiklik ozeti:

- Degisen toplam dosya: 5
- Degisen `.cs` dosyasi: 5
- Eklenen satir: 94, silinen satir: 9
- Degisen `.cs` dosyalari:
  - `src/Polly/RateLimit/AsyncRateLimitPolicy.cs`
  - `test/Polly.Specs/NoOp/NoOpSpecs.cs`
  - `test/Polly.Specs/NoOp/NoOpTResultAsyncSpecs.cs`
  - `test/Polly.Specs/RateLimit/AsyncRateLimitPolicySpecs.cs`
  - `test/Polly.Specs/RateLimit/AsyncRateLimitPolicyTResultSpecs.cs`

Commit'in ilgili C# diff'i:

```diff
--- src/Polly/RateLimit/AsyncRateLimitPolicy.cs
--- a/src/Polly/RateLimit/AsyncRateLimitPolicy.cs
+++ b/src/Polly/RateLimit/AsyncRateLimitPolicy.cs
@@ -4,7 +4,6 @@ namespace Polly.RateLimit;
 /// <summary>
 /// A rate-limit policy that can be applied to asynchronous delegates.
 /// </summary>
-#pragma warning disable CA1062 // Validate arguments of public methods
 public class AsyncRateLimitPolicy : AsyncPolicy, IRateLimitPolicy
 {
     private readonly IRateLimiter _rateLimiter;
@@ -14,9 +13,25 @@ public class AsyncRateLimitPolicy : AsyncPolicy, IRateLimitPolicy
 
     /// <inheritdoc/>
     [DebuggerStepThrough]
-    protected override Task<TResult> ImplementationAsync<TResult>(Func<Context, CancellationToken, Task<TResult>> action, Context context, CancellationToken cancellationToken,
-        bool continueOnCapturedContext) =>
-        AsyncRateLimitEngine.ImplementationAsync(_rateLimiter, null, action, context, continueOnCapturedContext, cancellationToken);
+    protected override Task<TResult> ImplementationAsync<TResult>(
+        Func<Context, CancellationToken, Task<TResult>> action,
+        Context context,
+        CancellationToken cancellationToken,
+        bool continueOnCapturedContext)
+    {
+        if (action is null)
+        {
+            throw new ArgumentNullException(nameof(action));
+        }
+
+        return AsyncRateLimitEngine.ImplementationAsync(
+            _rateLimiter,
+            null,
+            action,
+            context,
+            continueOnCapturedContext,
+            cancellationToken);
+    }
 }
 
 /// <summary>
@@ -38,7 +53,23 @@ public class AsyncRateLimitPolicy<TResult> : AsyncPolicy<TResult>, IRateLimitPol
 
     /// <inheritdoc/>
     [DebuggerStepThrough]
-    protected override Task<TResult> ImplementationAsync(Func<Context, CancellationToken, Task<TResult>> action, Context context, CancellationToken cancellationToken,
-        bool continueOnCapturedContext) =>
-        AsyncRateLimitEngine.ImplementationAsync(_rateLimiter, _retryAfterFactory, action, context, continueOnCapturedContext, cancellationToken);
+    protected override Task<TResult> ImplementationAsync(
+        Func<Context, CancellationToken, Task<TResult>> action,
+        Context context,
+        CancellationToken cancellationToken,
+        bool continueOnCapturedContext)
+    {
+        if (action is null)
+        {
+            throw new ArgumentNullException(nameof(action));
+        }
+
+        return AsyncRateLimitEngine.ImplementationAsync(
+            _rateLimiter,
+            _retryAfterFactory,
+            action,
+            context,
+            continueOnCapturedContext,
+            cancellationToken);
+    }
 }
--- test/Polly.Specs/NoOp/NoOpSpecs.cs
--- a/test/Polly.Specs/NoOp/NoOpSpecs.cs
+++ b/test/Polly.Specs/NoOp/NoOpSpecs.cs
@@ -18,7 +18,8 @@ public class NoOpSpecs
 
         var exceptionAssertions = func.Should().Throw<TargetInvocationException>();
         exceptionAssertions.And.Message.Should().Be("Exception has been thrown by the target of an invocation.");
-        exceptionAssertions.WithInnerException<ArgumentNullException>("action");
+        exceptionAssertions.And.InnerException.Should().BeOfType<ArgumentNullException>()
+            .Which.ParamName.Should().Be("action");
     }
 
     [Fact]
--- test/Polly.Specs/NoOp/NoOpTResultAsyncSpecs.cs
--- a/test/Polly.Specs/NoOp/NoOpTResultAsyncSpecs.cs
+++ b/test/Polly.Specs/NoOp/NoOpTResultAsyncSpecs.cs
@@ -17,7 +17,8 @@ public class NoOpTResultAsyncSpecs
 
         var exceptionAssertions = func.Should().Throw<TargetInvocationException>();
         exceptionAssertions.And.Message.Should().Be("Exception has been thrown by the target of an invocation.");
-        exceptionAssertions.WithInnerException<ArgumentNullException>("action");
+        exceptionAssertions.And.InnerException.Should().BeOfType<ArgumentNullException>()
+            .Which.ParamName.Should().Be("action");
     }
 
     [Fact]
--- test/Polly.Specs/RateLimit/AsyncRateLimitPolicySpecs.cs
--- a/test/Polly.Specs/RateLimit/AsyncRateLimitPolicySpecs.cs
+++ b/test/Polly.Specs/RateLimit/AsyncRateLimitPolicySpecs.cs
@@ -31,4 +31,30 @@ public class AsyncRateLimitPolicySpecs : RateLimitPolicySpecsBase, IDisposable
             throw new InvalidOperationException("Unexpected policy type in test construction.");
         }
     }
+
+    [Fact]
+    public void Should_throw_when_action_is_null()
+    {
+        var flags = BindingFlags.NonPublic | BindingFlags.Instance;
+        Func<Context, CancellationToken, Task<EmptyStruct>> action = null!;
+        IRateLimiter rateLimiter = RateLimiterFactory.Create(TimeSpan.FromSeconds(1), 1);
+
+        var instance = Activator.CreateInstance(
+            typeof(AsyncRateLimitPolicy),
+            flags,
+            null,
+            [rateLimiter],
+            null)!;
+        var instanceType = instance.GetType();
+        var methods = instanceType.GetMethods(flags);
+        var methodInfo = methods.First(method => method is { Name: "ImplementationAsync", ReturnType.Name: "Task`1" });
+        var generic = methodInfo.MakeGenericMethod(typeof(EmptyStruct));
+
+        var func = () => generic.Invoke(instance, [action, new Context(), CancellationToken.None, false]);
+
+        var exceptionAssertions = func.Should().Throw<TargetInvocationException>();
+        exceptionAssertions.And.Message.Should().Be("Exception has been thrown by the target of an invocation.");
+        exceptionAssertions.And.InnerException.Should().BeOfType<ArgumentNullException>()
+            .Which.ParamName.Should().Be("action");
+    }
 }
--- test/Polly.Specs/RateLimit/AsyncRateLimitPolicyTResultSpecs.cs
--- a/test/Polly.Specs/RateLimit/AsyncRateLimitPolicyTResultSpecs.cs
+++ b/test/Polly.Specs/RateLimit/AsyncRateLimitPolicyTResultSpecs.cs
@@ -47,4 +47,30 @@ public class AsyncRateLimitPolicyTResultSpecs : RateLimitPolicyTResultSpecsBase,
             throw new InvalidOperationException("Unexpected policy type in test construction.");
         }
     }
+
+    [Fact]
+    public void Should_throw_when_action_is_null()
+    {
+        var flags = BindingFlags.NonPublic | BindingFlags.Instance;
+        Func<Context, CancellationToken, Task<EmptyStruct>> action = null!;
+        IRateLimiter rateLimiter = RateLimiterFactory.Create(TimeSpan.FromSeconds(1), 1);
+        Func<TimeSpan, Context, EmptyStruct>? retryAfterFactory = null!;
+
+        var instance = Activator.CreateInstance(
+            typeof(AsyncRateLimitPolicy<EmptyStruct>),
+            flags,
+            null,
+            [rateLimiter, retryAfterFactory],
+            null)!;
+        var instanceType = instance.GetType();
+        var methods = instanceType.GetMethods(flags);
+        var methodInfo = methods.First(method => method is { Name: "ImplementationAsync", ReturnType.Name: "Task`1" });
+
+        var func = () => methodInfo.Invoke(instance, [action, new Context(), CancellationToken.None, false]);
+
+        var exceptionAssertions = func.Should().Throw<TargetInvocationException>();
+        exceptionAssertions.And.Message.Should().Be("Exception has been thrown by the target of an invocation.");
+        exceptionAssertions.And.InnerException.Should().BeOfType<ArgumentNullException>()
+            .Which.ParamName.Should().Be("action");
+    }
```

Kirpildi; tam diff: https://github.com/app-vnext/polly/commit/ed1c79be1c0ce774072c123b52ac51e21be798fb

Sonraki tarihsel olgular:

- `src/Polly/RateLimit/AsyncRateLimitPolicy.cs`
  - `1f232caf9672` 2025-02-15 Improve Polly coverage (#2505)
    https://github.com/app-vnext/polly/commit/1f232caf9672e515751ba2df76d82bfbbb62e016
- `test/Polly.Specs/NoOp/NoOpSpecs.cs`
  - `e3f1a3ad68cc` 2025-01-19 Remove FluentAssertions (#2459)
    https://github.com/app-vnext/polly/commit/e3f1a3ad68cc1c1faf29b4e95c16d9acbb3b54fb
  - `3fb089717fb6` 2025-02-21 Improve coverage (#2526)
    https://github.com/app-vnext/polly/commit/3fb089717fb63c52b51d66a378cdad08b3f6335f
  - `6d5fcd6160f5` 2025-08-23 xunit v3 preparation (#2723)
    https://github.com/app-vnext/polly/commit/6d5fcd6160f5b4a2291075131ef4e485d1dacdec
- `test/Polly.Specs/NoOp/NoOpTResultAsyncSpecs.cs`
  - `e3f1a3ad68cc` 2025-01-19 Remove FluentAssertions (#2459)
    https://github.com/app-vnext/polly/commit/e3f1a3ad68cc1c1faf29b4e95c16d9acbb3b54fb
  - `6d5fcd6160f5` 2025-08-23 xunit v3 preparation (#2723)
    https://github.com/app-vnext/polly/commit/6d5fcd6160f5b4a2291075131ef4e485d1dacdec
- `test/Polly.Specs/RateLimit/AsyncRateLimitPolicySpecs.cs`
  - `f5cbe89c0e42` 2025-01-10 xunit v3 preparation
    https://github.com/app-vnext/polly/commit/f5cbe89c0e42d0ec7e34f8f39d20e059d81fac19
  - `e3f1a3ad68cc` 2025-01-19 Remove FluentAssertions (#2459)
    https://github.com/app-vnext/polly/commit/e3f1a3ad68cc1c1faf29b4e95c16d9acbb3b54fb
  - `1f232caf9672` 2025-02-15 Improve Polly coverage (#2505)
    https://github.com/app-vnext/polly/commit/1f232caf9672e515751ba2df76d82bfbbb62e016
- `test/Polly.Specs/RateLimit/AsyncRateLimitPolicyTResultSpecs.cs`
  - `f5cbe89c0e42` 2025-01-10 xunit v3 preparation
    https://github.com/app-vnext/polly/commit/f5cbe89c0e42d0ec7e34f8f39d20e059d81fac19
  - `e3f1a3ad68cc` 2025-01-19 Remove FluentAssertions (#2459)
    https://github.com/app-vnext/polly/commit/e3f1a3ad68cc1c1faf29b4e95c16d9acbb3b54fb
  - `781c5ccf1893` 2025-01-27 Apply IDE suggestions
    https://github.com/app-vnext/polly/commit/781c5ccf1893f6f0430fdcfdebdb8215fc940574

Karar:

[ KUSUR-GETIRDI ]

Not:

[ ]

---

### Ornek SAMPLE-04 — github.com/jellyfin/jellyfin

Commit:

- Kisa SHA: `63c4fc297a91`
- Yazar tarihi: 2026-03-01
- Mesaj basligi: Update naming
- Baglanti: https://github.com/jellyfin/jellyfin/commit/63c4fc297a918cf4407ba16dda5e98d34173dcad

Degisiklik ozeti:

- Degisen toplam dosya: 2
- Degisen `.cs` dosyasi: 2
- Eklenen satir: 4, silinen satir: 4
- Degisen `.cs` dosyalari:
  - `Jellyfin.Server/Configuration/StartupMode.cs`
  - `Jellyfin.Server/Migrations/JellyfinMigrationService.cs`

Commit'in ilgili C# diff'i:

```diff
--- Jellyfin.Server/Configuration/StartupMode.cs
--- a/Jellyfin.Server/Configuration/StartupMode.cs
+++ b/Jellyfin.Server/Configuration/StartupMode.cs
@@ -13,12 +13,12 @@ public enum StartupMode
     MediaServer = 0,
 
     /// <summary>
-    /// Attempts to Migrate the selected database only then shuts down.
+    /// Attempts to Migrate the system only then shuts down.
     /// </summary>
-    MigrateDatabase = 1,
+    MigrateSystem = 1,
 
     /// <summary>
     /// Runs the Database seed function regardless of <see cref="BaseApplicationConfiguration.IsStartupWizardCompleted"/> state.
     /// </summary>
-    SeedDatabase = 2
+    SeedSystem = 2
 }
--- Jellyfin.Server/Migrations/JellyfinMigrationService.cs
--- a/Jellyfin.Server/Migrations/JellyfinMigrationService.cs
+++ b/Jellyfin.Server/Migrations/JellyfinMigrationService.cs
@@ -98,7 +98,7 @@ internal class JellyfinMigrationService
         var serverConfig = File.Exists(appPaths.SystemConfigurationFilePath)
             ? (ServerConfiguration)xmlSerializer.DeserializeFromFile(typeof(ServerConfiguration), appPaths.SystemConfigurationFilePath)!
             : new ServerConfiguration();
-        if (!serverConfig.IsStartupWizardCompleted || startupOptions.StartupMode is Configuration.StartupMode.SeedDatabase)
+        if (!serverConfig.IsStartupWizardCompleted || startupOptions.StartupMode is Configuration.StartupMode.SeedSystem)
         {
             logger.LogInformation("System initialization detected. Seed data. Startup mode is: {StartupMode}", startupOptions.StartupMode ?? Configuration.StartupMode.MediaServer);
             var flatApplyMigrations = Migrations.SelectMany(e => e.Where(f => !f.Metadata.RunMigrationOnSetup)).ToArray();
```

Sonraki tarihsel olgular:

- `Jellyfin.Server/Configuration/StartupMode.cs`
  - Gozlem araliginda sonraki degisiklik yok
- `Jellyfin.Server/Migrations/JellyfinMigrationService.cs`
  - `11130030d251` 2026-05-26 Backport: Fix/user manager collation (#16919)
    https://github.com/jellyfin/jellyfin/commit/11130030d25101e4ca42e2215d8343155a529b79
  - `0046adda29b4` 2026-06-22 Restyle the startup UI and add a generic startup activity line
    https://github.com/jellyfin/jellyfin/commit/0046adda29b4d99cbdf6b215d14539c08e96ab3e
  - `f571cd5a6acb` 2026-07-27 Reevaluate pending migrations after each one instead of per stage
    https://github.com/jellyfin/jellyfin/commit/f571cd5a6acbf1e942fb64c5723b15f1f1e745b9

Karar:

[ KUSUR-GETIRMEDI ]

Not:

[ ]

---

### Ornek SAMPLE-05 — github.com/jellyfin/jellyfin

Commit:

- Kisa SHA: `5677566a4163`
- Yazar tarihi: 2023-07-29
- Mesaj basligi: Enable nullable for more files
- Baglanti: https://github.com/jellyfin/jellyfin/commit/5677566a41638f4c62f107f3540363457c099019

Degisiklik ozeti:

- Degisen toplam dosya: 10
- Degisen `.cs` dosyasi: 10
- Eklenen satir: 161, silinen satir: 190
- Degisen `.cs` dosyalari:
  - `Emby.Dlna/Didl/DidlBuilder.cs`
  - `Emby.Dlna/PlayTo/Device.cs`
  - `Emby.Dlna/PlayTo/PlayToController.cs`
  - `Emby.Dlna/PlayTo/PlayToManager.cs`
  - `Emby.Server.Implementations/Plugins/PluginManager.cs`
  - `MediaBrowser.Controller/Drawing/ImageProcessorExtensions.cs`
  - `MediaBrowser.Model/Dlna/DeviceProfile.cs`
  - `MediaBrowser.Model/Dlna/StreamBuilder.cs`
  - `MediaBrowser.Model/Dlna/StreamInfo.cs`
  - `tests/Jellyfin.Model.Tests/Dlna/StreamBuilderTests.cs`

Commit'in ilgili C# diff'i:

```diff
--- Emby.Dlna/Didl/DidlBuilder.cs
--- a/Emby.Dlna/Didl/DidlBuilder.cs
+++ b/Emby.Dlna/Didl/DidlBuilder.cs
@@ -1,5 +1,3 @@
-#nullable disable
-
 #pragma warning disable CS1591
 
 using System;
@@ -45,8 +43,8 @@ namespace Emby.Dlna.Didl
         private readonly DeviceProfile _profile;
         private readonly IImageProcessor _imageProcessor;
         private readonly string _serverAddress;
-        private readonly string _accessToken;
-        private readonly User _user;
+        private readonly string? _accessToken;
+        private readonly User? _user;
         private readonly IUserDataManager _userDataManager;
         private readonly ILocalizationManager _localization;
         private readonly IMediaSourceManager _mediaSourceManager;
@@ -56,10 +54,10 @@ namespace Emby.Dlna.Didl
 
         public DidlBuilder(
             DeviceProfile profile,
-            User user,
+            User? user,
             IImageProcessor imageProcessor,
             string serverAddress,
-            string accessToken,
+            string? accessToken,
             IUserDataManager userDataManager,
             ILocalizationManager localization,
             IMediaSourceManager mediaSourceManager,
@@ -85,7 +83,7 @@ namespace Emby.Dlna.Didl
             return url + "&dlnaheaders=true";
         }
 
-        public string GetItemDidl(BaseItem item, User user, BaseItem context, string deviceId, Filter filter, StreamInfo streamInfo)
+        public string GetItemDidl(BaseItem item, User? user, BaseItem? context, string deviceId, Filter filter, StreamInfo streamInfo)
         {
             var settings = new XmlWriterSettings
             {
@@ -140,12 +138,12 @@ namespace Emby.Dlna.Didl
         public void WriteItemElement(
             XmlWriter writer,
             BaseItem item,
-            User user,
-            BaseItem context,
+            User? user,
+            BaseItem? context,
             StubType? contextStubType,
             string deviceId,
             Filter filter,
-            StreamInfo streamInfo = null)
+            StreamInfo? streamInfo = null)
         {
             var clientId = GetClientId(item, null);
 
@@ -190,7 +188,7 @@ namespace Emby.Dlna.Didl
             writer.WriteFullEndElement();
         }
 
-        private void AddVideoResource(XmlWriter writer, BaseItem video, string deviceId, Filter filter, StreamInfo streamInfo = null)
+        private void AddVideoResource(XmlWriter writer, BaseItem video, string deviceId, Filter filter, StreamInfo? streamInfo = null)
         {
             if (streamInfo is null)
             {
@@ -203,7 +201,7 @@ namespace Emby.Dlna.Didl
                     Profile = _profile,
                     DeviceId = deviceId,
                     MaxBitrate = _profile.MaxStreamingBitrate
-                });
+                }) ?? throw new InvalidOperationException("No optimal video stream found");
             }
 
             var targetWidth = streamInfo.TargetWidth;
@@ -315,7 +313,7 @@ namespace Emby.Dlna.Didl
 
             var mediaSource = streamInfo.MediaSource;
 
-            if (mediaSource.RunTimeTicks.HasValue)
+            if (mediaSource?.RunTimeTicks.HasValue == true)
             {
                 writer.WriteAttributeString("duration", TimeSpan.FromTicks(mediaSource.RunTimeTicks.Value).ToString("c", CultureInfo.InvariantCulture));
             }
@@ -410,7 +408,7 @@ namespace Emby.Dlna.Didl
             writer.WriteFullEndElement();
         }
 
-        private string GetDisplayName(BaseItem item, StubType? itemStubType, BaseItem context)
+        private string GetDisplayName(BaseItem item, StubType? itemStubType, BaseItem? context)
         {
             if (itemStubType.HasValue)
             {
@@ -452,7 +450,7 @@ namespace Emby.Dlna.Didl
         /// <param name="episode">The episode.</param>
         /// <param name="context">Current context.</param>
         /// <returns>Formatted name of the episode.</returns>
-        private string GetEpisodeDisplayName(Episode episode, BaseItem context)
+        private string GetEpisodeDisplayName(Episode episode, BaseItem? context)
         {
             string[] components;
 
@@ -530,7 +528,7 @@ namespace Emby.Dlna.Didl
 
         private bool NotNullOrWhiteSpace(string s) => !string.IsNullOrWhiteSpace(s);
 
-        private void AddAudioResource(XmlWriter writer, BaseItem audio, string deviceId, Filter filter, StreamInfo streamInfo = null)
+        private void AddAudioResource(XmlWriter writer, BaseItem audio, string deviceId, Filter filter, StreamInfo? streamInfo = null)
         {
             writer.WriteStartElement(string.Empty, "res", NsDidl);
 
@@ -544,14 +542,14 @@ namespace Emby.Dlna.Didl
                     MediaSources = sources.ToArray(),
                     Profile = _profile,
                     DeviceId = deviceId
-                });
+                }) ?? throw new InvalidOperationException("No optimal audio stream found");
             }
 
             var url = NormalizeDlnaMediaUrl(streamInfo.ToUrl(_serverAddress, _accessToken));
 
             var mediaSource = streamInfo.MediaSource;
 
-            if (mediaSource.RunTimeTicks.HasValue)
+            if (mediaSource?.RunTimeTicks is not null)
             {
                 writer.WriteAttributeString("duration", TimeSpan.FromTicks(mediaSource.RunTimeTicks.Value).ToString("c", CultureInfo.InvariantCulture));
             }
@@ -634,7 +632,7 @@ namespace Emby.Dlna.Didl
                 // Samsung sometimes uses 1 as root
                 || string.Equals(id, "1", StringComparison.OrdinalIgnoreCase);
 
-        public void WriteFolderElement(XmlWriter writer, BaseItem folder, StubType? stubType, BaseItem context, int childCount, Filter filter, string requestedId = null)
+        public void WriteFolderElement(XmlWriter writer, BaseItem folder, StubType? stubType, BaseItem context, int childCount, Filter filter, string? requestedId = null)
         {
             writer.WriteStartElement(string.Empty, "container", NsDidl);
 
@@ -678,14 +676,14 @@ namespace Emby.Dlna.Didl
             writer.WriteFullEndElement();
         }
 
-        private void AddSamsungBookmarkInfo(BaseItem item, User user, XmlWriter writer, StreamInfo streamInfo)
+        private void AddSamsungBookmarkInfo(BaseItem item, User? user, XmlWriter writer, StreamInfo? streamInfo)
         {
             if (!item.SupportsPositionTicksResume || item is Folder)
             {
                 return;
             }
 
-            XmlAttribute secAttribute = null;
+            XmlAttribute? secAttribute = null;
             foreach (var attribute in _profile.XmlRootAttributes)
             {
                 if (string.Equals(attribute.Name, "xmlns:sec", StringComparison.OrdinalIgnoreCase))
@@ -695,8 +693,8 @@ namespace Emby.Dlna.Didl
                 }
             }
 
-            // Not a samsung device
```

Kirpildi; tam diff: https://github.com/jellyfin/jellyfin/commit/5677566a41638f4c62f107f3540363457c099019

Sonraki tarihsel olgular:

- `Emby.Dlna/Didl/DidlBuilder.cs`
  - `f1aba6b95230` 2023-11-09 Remove Emby.Dlna
    https://github.com/jellyfin/jellyfin/commit/f1aba6b95230474d47c580071370c7dbd00eba13
- `Emby.Dlna/PlayTo/Device.cs`
  - `fdef9356b9ba` 2023-10-07 Use null propagation
    https://github.com/jellyfin/jellyfin/commit/fdef9356b9ba483e437fbc3a2bc0b6aaf3c05c29
  - `96c3bde3463a` 2023-10-07 Remove redundant nullable directive
    https://github.com/jellyfin/jellyfin/commit/96c3bde3463ad0457d894ed532093ed28e868ba8
  - `a9ef103c95a7` 2023-11-05 Add IDisposableAnalyzers to more projects
    https://github.com/jellyfin/jellyfin/commit/a9ef103c95a7460031879726f4afda3013ca6619
- `Emby.Dlna/PlayTo/PlayToController.cs`
  - `a9ef103c95a7` 2023-11-05 Add IDisposableAnalyzers to more projects
    https://github.com/jellyfin/jellyfin/commit/a9ef103c95a7460031879726f4afda3013ca6619
  - `f1aba6b95230` 2023-11-09 Remove Emby.Dlna
    https://github.com/jellyfin/jellyfin/commit/f1aba6b95230474d47c580071370c7dbd00eba13
- `Emby.Dlna/PlayTo/PlayToManager.cs`
  - `526f9a825c82` 2023-10-07 Make files readonly
    https://github.com/jellyfin/jellyfin/commit/526f9a825c8205942155759afc5fc1e7a8f6fc6a
  - `f1aba6b95230` 2023-11-09 Remove Emby.Dlna
    https://github.com/jellyfin/jellyfin/commit/f1aba6b95230474d47c580071370c7dbd00eba13
- `Emby.Server.Implementations/Plugins/PluginManager.cs`
  - `493de3297a41` 2023-09-23 Use IHostLifetime to handle restarting and shutting down
    https://github.com/jellyfin/jellyfin/commit/493de3297a415061f8d6a69ff9f62261c3159a2a
  - `d7748cfa0476` 2023-10-11 Multiple Stream changes
    https://github.com/jellyfin/jellyfin/commit/d7748cfa0476280cce9dba34b4512cc58760c8bb
  - `1e1e1560a474` 2023-11-09 Add IServerApplicationHost parameter to IPluginServiceRegistrator
    https://github.com/jellyfin/jellyfin/commit/1e1e1560a47439c02931e67736bcd87b606cf35c
- `MediaBrowser.Controller/Drawing/ImageProcessorExtensions.cs`
  - Gozlem araliginda sonraki degisiklik yok
- `MediaBrowser.Model/Dlna/DeviceProfile.cs`
  - `68fd9c469f01` 2024-01-12 Remove DLNA-specific DeviceProfile code (#10850)
    https://github.com/jellyfin/jellyfin/commit/68fd9c469f013acef3e661d15adb61c4eb965ec7
  - `2351eeba5619` 2024-09-17 Rework PR 6203
    https://github.com/jellyfin/jellyfin/commit/2351eeba561905bafae48a948f3126797c284766
  - `bcc818f39778` 2024-09-22 Fix DeviceProfile.Id should be nullable (#12679)
    https://github.com/jellyfin/jellyfin/commit/bcc818f397789be7d487e58726a6dd415546f194
- `MediaBrowser.Model/Dlna/StreamBuilder.cs`
  - `18a311d32fd6` 2023-08-22 == null -> is null
    https://github.com/jellyfin/jellyfin/commit/18a311d32fd6e3cdfed466017bda46566a013106
  - `47254d6a2236` 2023-10-07 Remove conditional access when it is known to be not null
    https://github.com/jellyfin/jellyfin/commit/47254d6a2236e079af3cc8c2e37c77d9d1479235
  - `b62b0ec2b581` 2023-10-23 Fix warnings
    https://github.com/jellyfin/jellyfin/commit/b62b0ec2b581369de42c69305773f0edb9d701b4
- `MediaBrowser.Model/Dlna/StreamInfo.cs`
  - `407cf5d0bf9d` 2024-03-04 Add MediaStreamProtocol enum (#10153)
    https://github.com/jellyfin/jellyfin/commit/407cf5d0bf9d3563ae77fd34ce29ffae5af4339f
  - `e731250342a0` 2024-03-08 Lowercase MediaStreamProtocol for backwards compatibility
    https://github.com/jellyfin/jellyfin/commit/e731250342a0d4d6ac971b310a4b42c675060750
  - `0381c5a288bc` 2024-05-06 Add EnableAudioVbrEncoding to TranscodingProfile
    https://github.com/jellyfin/jellyfin/commit/0381c5a288bc56e20aa5def05e3d41bacf3519a7
- `tests/Jellyfin.Model.Tests/Dlna/StreamBuilderTests.cs`
  - `bc959270b762` 2023-09-11 Removed nesting levels through block-scoped 'using' statement (#10025)
    https://github.com/jellyfin/jellyfin/commit/bc959270b762d1a6f68fc4f46a7c42138b39710c
  - `d92f2ac31cad` 2023-12-19 test: fix remux tests
    https://github.com/jellyfin/jellyfin/commit/d92f2ac31cad5b667125a055708a080605ee53f6
  - `8c29fa422a65` 2023-12-20 test: fix tizen profile
    https://github.com/jellyfin/jellyfin/commit/8c29fa422a65b9c9abe239cf4536804e5c08aa2c

Karar:

[ KUSUR-GETIRMEDI ]

Not:

[ ]

---

### Ornek SAMPLE-06 — github.com/sharex/sharex

Commit:

- Kisa SHA: `738082745ce1`
- Yazar tarihi: 2022-01-31
- Mesaj basligi: Parser improvements
- Baglanti: https://github.com/sharex/sharex/commit/738082745ce147876d93a510b4015562c46a2647

Degisiklik ozeti:

- Degisen toplam dosya: 3
- Degisen `.cs` dosyasi: 3
- Eklenen satir: 61, silinen satir: 37
- Degisen `.cs` dosyalari:
  - `ShareX.UploadersLib/CustomUploader/CustomUploaderItem.cs`
  - `ShareX.UploadersLib/CustomUploader/CustomUploaderParser2.cs`
  - `ShareX.UploadersLib/CustomUploader/Functions/CustomUploaderFunctionPrompt.cs`

Commit'in ilgili C# diff'i:

```diff
--- ShareX.UploadersLib/CustomUploader/CustomUploaderItem.cs
--- a/ShareX.UploadersLib/CustomUploader/CustomUploaderItem.cs
+++ b/ShareX.UploadersLib/CustomUploader/CustomUploaderItem.cs
@@ -265,9 +265,13 @@ public void ParseResponse(UploadResult result, ResponseInfo responseInfo, Custom
                     responseInfo.ResponseText = "";
                 }
 
-                CustomUploaderParser parser = new CustomUploaderParser(responseInfo, RegexList);
-                parser.FileName = input.FileName;
-                parser.URLEncode = true;
+                CustomUploaderParser2 parser = new CustomUploaderParser2()
+                {
+                    FileName = input.FileName,
+                    ResponseInfo = responseInfo,
+                    RegexList = RegexList,
+                    URLEncode = true
+                };
 
                 if (responseInfo.IsSuccess)
                 {
--- ShareX.UploadersLib/CustomUploader/CustomUploaderParser2.cs
--- a/ShareX.UploadersLib/CustomUploader/CustomUploaderParser2.cs
+++ b/ShareX.UploadersLib/CustomUploader/CustomUploaderParser2.cs
@@ -48,36 +48,33 @@ public class CustomUploaderParser2
 
         public string Parse(string text)
         {
-            return ParseSyntax(text, 0, out _);
-        }
-
-        private string ParseSyntax(string text, int startPosition, out int endPosition)
-        {
-            endPosition = startPosition;
-
             if (string.IsNullOrEmpty(text))
             {
                 return "";
             }
 
+            return ParseSyntax(text, 0, out _);
+        }
+
+        private string ParseSyntax(string text, int startPosition, out int endPosition)
+        {
             StringBuilder sbResult = new StringBuilder();
             bool escape = false;
+            int i;
 
-            for (int i = startPosition; i < text.Length; i++)
+            for (i = startPosition; i < text.Length; i++)
             {
                 if (!escape)
                 {
                     if (text[i] == SyntaxStart)
                     {
-                        string parsed = ParseSyntax(text, i + 1, out i);
-                        parsed = ParseFunction(parsed);
+                        string parsed = ParseFunction(text, i + 1, out i);
                         sbResult.Append(parsed);
                         continue;
                     }
-                    else if (text[i] == SyntaxEnd)
+                    else if (text[i] == SyntaxEnd || text[i] == SyntaxParameterDelimiter)
                     {
-                        endPosition = i;
-                        return sbResult.ToString();
+                        break;
                     }
                     else if (text[i] == SyntaxEscape)
                     {
@@ -85,44 +82,66 @@ private string ParseSyntax(string text, int startPosition, out int endPosition)
                         continue;
                     }
                 }
-                else
-                {
-                    escape = false;
-                }
 
+                escape = false;
                 sbResult.Append(text[i]);
             }
 
+            endPosition = i;
             return sbResult.ToString();
         }
 
-        private string ParseFunction(string text)
+        private string ParseFunction(string text, int startPosition, out int endPosition)
         {
-            string functionName;
-            string[] parameterArray = null;
-
-            int parameterPosition = text.IndexOf(SyntaxParameterStart);
+            StringBuilder sbFunctionName = new StringBuilder();
+            bool parsingFunctionName = true;
+            List<string> parameters = new List<string>();
+            bool escape = false;
+            int i;
 
-            if (parameterPosition >= 0)
-            {
-                functionName = text.Remove(parameterPosition);
-                string parameters = text.Substring(parameterPosition + 1);
-                // TODO: Support delimiter escape
-                parameterArray = parameters.Split(SyntaxParameterDelimiter);
-            }
-            else
+            for (i = startPosition; i < text.Length; i++)
             {
-                functionName = text;
+                if (!escape)
+                {
+                    if (text[i] == SyntaxEnd)
+                    {
+                        break;
+                    }
+                    else if (text[i] == SyntaxEscape)
+                    {
+                        escape = true;
+                        continue;
+                    }
+
+                    if (parsingFunctionName)
+                    {
+                        if (text[i] == SyntaxParameterStart)
+                        {
+                            parsingFunctionName = false;
+                            continue;
+                        }
+
+                        sbFunctionName.Append(text[i]);
+                    }
+                    else
+                    {
+                        string parsed = ParseSyntax(text, i, out i);
+                        parameters.Add(parsed);
+                    }
+                }
+
+                escape = false;
             }
 
-            return CallFunction(functionName, parameterArray);
+            endPosition = i;
+            return CallFunction(sbFunctionName.ToString(), parameters.ToArray());
         }
 
         private string CallFunction(string functionName, string[] parameters)
         {
             foreach (CustomUploaderFunction function in Functions)
             {
-                if (function.Name.Equals(functionName, StringComparison.InvariantCultureIgnoreCase))
+                if (function.Name.Equals(functionName, StringComparison.OrdinalIgnoreCase))
                 {
                     return function.Call(this, parameters);
                 }
--- ShareX.UploadersLib/CustomUploader/Functions/CustomUploaderFunctionPrompt.cs
--- a/ShareX.UploadersLib/CustomUploader/Functions/CustomUploaderFunctionPrompt.cs
+++ b/ShareX.UploadersLib/CustomUploader/Functions/CustomUploaderFunctionPrompt.cs
```

Kirpildi; tam diff: https://github.com/sharex/sharex/commit/738082745ce147876d93a510b4015562c46a2647

Sonraki tarihsel olgular:

- `ShareX.UploadersLib/CustomUploader/CustomUploaderItem.cs`
  - `130600fc93d8` 2022-01-31 Added ShareXSyntaxParser
    https://github.com/sharex/sharex/commit/130600fc93d8ed3739e143b7c8715f06c6d7b3f8
  - `57972d07cb77` 2022-01-31 Removed regex list instead using first parameter for regex pattern
    https://github.com/sharex/sharex/commit/57972d07cb773844d05671d935aa19e1e4ec9d75
  - `f60e4461c923` 2022-01-31 Use CustomUploaderSyntaxParser
    https://github.com/sharex/sharex/commit/f60e4461c9237d1a66208fdb05156c3be5b6b8e1
- `ShareX.UploadersLib/CustomUploader/CustomUploaderParser2.cs`
  - `0fffb7eb9c90` 2022-01-31 Parser improvements
    https://github.com/sharex/sharex/commit/0fffb7eb9c90e94168a984f73d193f2dcc5157b4
  - `130600fc93d8` 2022-01-31 Added ShareXSyntaxParser
    https://github.com/sharex/sharex/commit/130600fc93d8ed3739e143b7c8715f06c6d7b3f8
    yol: `ShareX.UploadersLib/CustomUploader/CustomUploaderParser2.cs` -> `ShareX.UploadersLib/CustomUploader/ShareXSyntaxParser.cs`
  - `1b0262ce4f91` 2022-01-31 Parser improvements
    https://github.com/sharex/sharex/commit/1b0262ce4f919353cb558dad844a10366e3bb0f7
- `ShareX.UploadersLib/CustomUploader/Functions/CustomUploaderFunctionPrompt.cs`
  - `130600fc93d8` 2022-01-31 Added ShareXSyntaxParser
    https://github.com/sharex/sharex/commit/130600fc93d8ed3739e143b7c8715f06c6d7b3f8
  - `54d0c0a53324` 2022-01-31 Added syntax examples
    https://github.com/sharex/sharex/commit/54d0c0a53324f00bc7297ad7d42e79c19027beb6
  - `91828231883b` 2022-04-13 Rename class
    https://github.com/sharex/sharex/commit/91828231883b3526ddcfab233161b359c187e373

Karar:

[ KUSUR-GETIRMEDI ]

Not:

[ ]

---

### Ornek SAMPLE-07 — github.com/sharex/sharex

Commit:

- Kisa SHA: `602ca0d5824f`
- Yazar tarihi: 2026-05-30
- Mesaj basligi: Add BorderStyle support to FreehandAnnotation and update related rendering logic
- Baglanti: https://github.com/sharex/sharex/commit/602ca0d5824f1a555a66a4cd2eba04c43060b27e

Degisiklik ozeti:

- Degisen toplam dosya: 7
- Degisen `.cs` dosyasi: 7
- Eklenen satir: 23, silinen satir: 7
- Degisen `.cs` dosyalari:
  - `ShareX.ImageEditor/Core/Annotations/Shapes/FreehandAnnotation.cs`
  - `ShareX.ImageEditor/Presentation/Controllers/EditorInputController.cs`
  - `ShareX.ImageEditor/Presentation/Rendering/AnnotationVisuals/AnnotationVisualFactory.cs`
  - `ShareX.ImageEditor/Presentation/Rendering/AnnotationVisuals/FreehandAnnotation.Visual.cs`
  - `ShareX.ImageEditor/Presentation/ViewModels/MainViewModel.ToolOptions.cs`
  - `ShareX.ImageEditor/Presentation/Views/EditorView.ToolbarHandlers.cs`
  - `ShareX.ImageEditor/Presentation/Views/EditorView.axaml.cs`

Commit'in ilgili C# diff'i:

```diff
--- ShareX.ImageEditor/Core/Annotations/Shapes/FreehandAnnotation.cs
--- a/ShareX.ImageEditor/Core/Annotations/Shapes/FreehandAnnotation.cs
+++ b/ShareX.ImageEditor/Core/Annotations/Shapes/FreehandAnnotation.cs
@@ -35,6 +35,7 @@ public partial class FreehandAnnotation : Annotation, IPointBasedAnnotation
 {
     public override AnnotationCategory Category => AnnotationCategory.Shapes;
     public List<SKPoint> Points { get; set; } = new List<SKPoint>();
+    public BorderStyle BorderStyle { get; set; } = BorderStyle.Solid;
 
     /// <summary>
     /// Simplification tolerance for smoothing
--- ShareX.ImageEditor/Presentation/Controllers/EditorInputController.cs
--- a/ShareX.ImageEditor/Presentation/Controllers/EditorInputController.cs
+++ b/ShareX.ImageEditor/Presentation/Controllers/EditorInputController.cs
@@ -464,7 +464,8 @@ private static Rect GetCropOverlayCanvasRect(global::Avalonia.Controls.Shapes.Re
                 {
                     Stroke = brush,
                     StrokeThickness = vm.StrokeWidth,
-                    StrokeLineCap = PenLineCap.Round,
+                    StrokeDashArray = BorderStyleDashHelper.CreateStrokeDashArray(vm.SelectedBorderStyle),
+                    StrokeLineCap = PenLineCap.Flat,
                     StrokeJoin = PenLineJoin.Round,
                     UseLayoutRounding = false,
                     IsHitTestVisible = false
@@ -484,7 +485,7 @@ private static Rect GetCropOverlayCanvasRect(global::Avalonia.Controls.Shapes.Re
 
                 path.SetValue(Panel.ZIndexProperty, 1);
 
-                var freehand = new FreehandAnnotation { StrokeColor = vm.SelectedColor, StrokeWidth = vm.StrokeWidth, ShadowEnabled = vm.ShadowEnabled, Points = new List<SKPoint> { ToSKPoint(_startPoint) } };
+                var freehand = new FreehandAnnotation { StrokeColor = vm.SelectedColor, StrokeWidth = vm.StrokeWidth, BorderStyle = vm.SelectedBorderStyle, ShadowEnabled = vm.ShadowEnabled, Points = new List<SKPoint> { ToSKPoint(_startPoint) } };
                 path.Tag = freehand;
                 path.Data = freehand.CreateSmoothedGeometry();
                 _currentShape = path;
--- ShareX.ImageEditor/Presentation/Rendering/AnnotationVisuals/AnnotationVisualFactory.cs
--- a/ShareX.ImageEditor/Presentation/Rendering/AnnotationVisuals/AnnotationVisualFactory.cs
+++ b/ShareX.ImageEditor/Presentation/Rendering/AnnotationVisuals/AnnotationVisualFactory.cs
@@ -129,6 +129,8 @@ public static class AnnotationVisualFactory
                 break;
 
             case FreehandAnnotation freehand when control is Avalonia.Controls.Shapes.Path freehandPath:
+                freehandPath.StrokeDashArray = BorderStyleDashHelper.CreateStrokeDashArray(freehand.BorderStyle);
+                freehandPath.StrokeLineCap = PenLineCap.Flat;
                 freehandPath.Data = freehand.CreateSmoothedGeometry();
                 break;
 
--- ShareX.ImageEditor/Presentation/Rendering/AnnotationVisuals/FreehandAnnotation.Visual.cs
--- a/ShareX.ImageEditor/Presentation/Rendering/AnnotationVisuals/FreehandAnnotation.Visual.cs
+++ b/ShareX.ImageEditor/Presentation/Rendering/AnnotationVisuals/FreehandAnnotation.Visual.cs
@@ -26,6 +26,7 @@
 using Avalonia;
 using Avalonia.Controls;
 using Avalonia.Media;
+using ShareX.ImageEditor.Presentation.Helpers;
 
 namespace ShareX.ImageEditor.Core.Annotations;
 
@@ -41,7 +42,8 @@ public Control CreateVisual()
         {
             Stroke = brush,
             StrokeThickness = StrokeWidth,
-            StrokeLineCap = PenLineCap.Round,
+            StrokeDashArray = BorderStyleDashHelper.CreateStrokeDashArray(BorderStyle),
+            StrokeLineCap = PenLineCap.Flat,
             StrokeJoin = PenLineJoin.Round,
             Data = CreateSmoothedGeometry(),
             Tag = this
--- ShareX.ImageEditor/Presentation/ViewModels/MainViewModel.ToolOptions.cs
--- a/ShareX.ImageEditor/Presentation/ViewModels/MainViewModel.ToolOptions.cs
+++ b/ShareX.ImageEditor/Presentation/ViewModels/MainViewModel.ToolOptions.cs
@@ -436,11 +436,11 @@ private void UpdateOptionsFromTextColor()
                 return;
             }
 
-            bool supportsBorderStyle = ActiveTool is EditorTool.Rectangle or EditorTool.Ellipse or EditorTool.Line;
+            bool supportsBorderStyle = ActiveTool is EditorTool.Rectangle or EditorTool.Ellipse or EditorTool.Line or EditorTool.Freehand;
 
             if (ActiveTool == EditorTool.Select && SelectedAnnotation != null)
             {
-                supportsBorderStyle = SelectedAnnotation.ToolType is EditorTool.Rectangle or EditorTool.Ellipse or EditorTool.Line;
+                supportsBorderStyle = SelectedAnnotation.ToolType is EditorTool.Rectangle or EditorTool.Ellipse or EditorTool.Line or EditorTool.Freehand;
             }
 
             if (supportsBorderStyle)
@@ -784,10 +784,10 @@ private void SetSpotlightBlurOption(float value)
 
         public bool ShowBorderStyle => ActiveTool switch
         {
-            EditorTool.Rectangle or EditorTool.Ellipse or EditorTool.Line => true,
+            EditorTool.Rectangle or EditorTool.Ellipse or EditorTool.Line or EditorTool.Freehand => true,
             EditorTool.Select => _selectedAnnotation != null && _selectedAnnotation.ToolType switch
             {
-                EditorTool.Rectangle or EditorTool.Ellipse or EditorTool.Line => true,
+                EditorTool.Rectangle or EditorTool.Ellipse or EditorTool.Line or EditorTool.Freehand => true,
                 _ => false
             },
             _ => false
@@ -1061,6 +1061,7 @@ private void LoadOptionsForTool(EditorTool tool)
                     StrokeWidth = Options.Thickness;
                     CornerRadius = Options.CornerRadius;
                     ShadowEnabled = Options.Shadow;
+                    SelectedBorderStyle = NormalizeBorderStyle(Options.BorderStyle);
                     FontSize = Options.TextFontSize;
                     break;
                 case EditorTool.Arrow:
--- ShareX.ImageEditor/Presentation/Views/EditorView.ToolbarHandlers.cs
--- a/ShareX.ImageEditor/Presentation/Views/EditorView.ToolbarHandlers.cs
+++ b/ShareX.ImageEditor/Presentation/Views/EditorView.ToolbarHandlers.cs
@@ -632,6 +632,11 @@ private void ApplySelectedBorderStyle(BorderStyle borderStyle)
                     linePath.StrokeDashArray = BorderStyleDashHelper.CreateStrokeDashArray(borderStyle);
                     linePath.StrokeLineCap = PenLineCap.Flat;
                     break;
+                case FreehandAnnotation freehandAnnotation when selected is global::Avalonia.Controls.Shapes.Path freehandPath:
+                    freehandAnnotation.BorderStyle = borderStyle;
+                    freehandPath.StrokeDashArray = BorderStyleDashHelper.CreateStrokeDashArray(borderStyle);
+                    freehandPath.StrokeLineCap = PenLineCap.Flat;
+                    break;
             }
         }
 
--- ShareX.ImageEditor/Presentation/Views/EditorView.axaml.cs
--- a/ShareX.ImageEditor/Presentation/Views/EditorView.axaml.cs
+++ b/ShareX.ImageEditor/Presentation/Views/EditorView.axaml.cs
@@ -389,6 +389,10 @@ private void OnSelectionChanged(bool hasSelection)
                     {
                         vm.SelectedBorderStyle = line.BorderStyle;
                     }
+                    else if (vm.SelectedAnnotation is FreehandAnnotation freehand)
+                    {
+                        vm.SelectedBorderStyle = freehand.BorderStyle;
+                    }
                     else if (vm.SelectedAnnotation is CursorAnnotation cursor)
                     {
                         vm.SelectedCursorType = cursor.CursorType;
```

Sonraki tarihsel olgular:

- `ShareX.ImageEditor/Core/Annotations/Shapes/FreehandAnnotation.cs`
  - `b3a7f3f8dafc` 2026-09-08 Refactor application components and remove obsolete code
    https://github.com/sharex/sharex/commit/b3a7f3f8dafcd01d4f1c82432db921a6d20a5147
  - `3efb2acd6926` 2026-09-08 Centralize annotation movement and tail geometry handling
    https://github.com/sharex/sharex/commit/3efb2acd6926bb7aa4e20f26edbfb0107d6e34bb
- `ShareX.ImageEditor/Presentation/Controllers/EditorInputController.cs`
  - `98a5432b7056` 2026-05-30 Refactor stroke line cap handling to use BorderStyleDashHelper for consistent border styling across annotations
    https://github.com/sharex/sharex/commit/98a5432b705617239c9a6f665c9599dac8f1b11d
  - `4f05a52f5541` 2026-05-31 Add shadow color support to annotations and refactor shadow effect handling
    https://github.com/sharex/sharex/commit/4f05a52f55418ce1aec26f057cb4b466134e0898
  - `03c6c8af86cf` 2026-05-31 Add support for text bold and italic styles in annotations and update related UI components
    https://github.com/sharex/sharex/commit/03c6c8af86cfd04bc693007e0acec6e12ab8cf11
- `ShareX.ImageEditor/Presentation/Rendering/AnnotationVisuals/AnnotationVisualFactory.cs`
  - `98a5432b7056` 2026-05-30 Refactor stroke line cap handling to use BorderStyleDashHelper for consistent border styling across annotations
    https://github.com/sharex/sharex/commit/98a5432b705617239c9a6f665c9599dac8f1b11d
  - `5f47631abdc4` 2026-05-31 Add rotation support for Ellipse and Rectangle annotations and update related rendering logic
    https://github.com/sharex/sharex/commit/5f47631abdc4808c6216cb73330182043a9040d5
  - `49e83c4790b8` 2026-05-31 Add rotation support for SpeechBalloonAnnotation and update related handling in EditorSelectionController and AnnotationVisualFactory
    https://github.com/sharex/sharex/commit/49e83c4790b88917652221bbc1b8cada9ee5084c
- `ShareX.ImageEditor/Presentation/Rendering/AnnotationVisuals/FreehandAnnotation.Visual.cs`
  - `98a5432b7056` 2026-05-30 Refactor stroke line cap handling to use BorderStyleDashHelper for consistent border styling across annotations
    https://github.com/sharex/sharex/commit/98a5432b705617239c9a6f665c9599dac8f1b11d
  - `4f05a52f5541` 2026-05-31 Add shadow color support to annotations and refactor shadow effect handling
    https://github.com/sharex/sharex/commit/4f05a52f55418ce1aec26f057cb4b466134e0898
  - `dfbc23a44a47` 2026-06-21 Add configurable shadow options to the annotation toolbar
    https://github.com/sharex/sharex/commit/dfbc23a44a47ee7b3fb590e742f9b20bfd25bbdd
- `ShareX.ImageEditor/Presentation/ViewModels/MainViewModel.ToolOptions.cs`
  - `03c6c8af86cf` 2026-05-31 Add support for text bold and italic styles in annotations and update related UI components
    https://github.com/sharex/sharex/commit/03c6c8af86cfd04bc693007e0acec6e12ab8cf11
  - `a241c7e5a16b` 2026-06-16 Code cleanup
    https://github.com/sharex/sharex/commit/a241c7e5a16b4a40163bf9954a1c06c7b4f3e640
  - `fe370a4ec47d` 2026-06-18 Add tail toggles for step annotations
    https://github.com/sharex/sharex/commit/fe370a4ec47dfad702bbcfbe0bd678707443f430
- `ShareX.ImageEditor/Presentation/Views/EditorView.ToolbarHandlers.cs`
  - `98a5432b7056` 2026-05-30 Refactor stroke line cap handling to use BorderStyleDashHelper for consistent border styling across annotations
    https://github.com/sharex/sharex/commit/98a5432b705617239c9a6f665c9599dac8f1b11d
  - `4f05a52f5541` 2026-05-31 Add shadow color support to annotations and refactor shadow effect handling
    https://github.com/sharex/sharex/commit/4f05a52f55418ce1aec26f057cb4b466134e0898
  - `03c6c8af86cf` 2026-05-31 Add support for text bold and italic styles in annotations and update related UI components
    https://github.com/sharex/sharex/commit/03c6c8af86cfd04bc693007e0acec6e12ab8cf11
- `ShareX.ImageEditor/Presentation/Views/EditorView.axaml.cs`
  - `03c6c8af86cf` 2026-05-31 Add support for text bold and italic styles in annotations and update related UI components
    https://github.com/sharex/sharex/commit/03c6c8af86cfd04bc693007e0acec6e12ab8cf11
  - `65e9cff98ddc` 2026-05-31 Add hover feedback handling in EditorSelectionController for Control key interactions
    https://github.com/sharex/sharex/commit/65e9cff98ddccec9dda888910fb1b73da2c4f44f
  - `031f21bce0d2` 2026-05-31 Implement shape movement during creation with Control key support in EditorInputController
    https://github.com/sharex/sharex/commit/031f21bce0d295ab03bc57d0c4137b758214cb07

Karar:

[ KUSUR-GETIRMEDI ]

Not:

[ ]

---

### Ornek SAMPLE-08 — github.com/sharex/sharex

Commit:

- Kisa SHA: `d27199c22070`
- Yazar tarihi: 2026-07-28
- Mesaj basligi: Remove obsolete code and configuration
- Baglanti: https://github.com/sharex/sharex/commit/d27199c22070855a18cdd474a3770b8da4356b8a

Degisiklik ozeti:

- Degisen toplam dosya: 5
- Degisen `.cs` dosyasi: 4
- Eklenen satir: 1087, silinen satir: 1418
- Degisen `.cs` dosyalari:
  - `ShareX.ScreenCaptureLib/Forms/FFmpegOptionsForm.Designer.cs`
  - `ShareX.ScreenCaptureLib/Forms/FFmpegOptionsForm.cs`
  - `ShareX.ScreenCaptureLib/Presentation/FFmpegOptions/FFmpegOptionsWindow.axaml.cs`
  - `ShareX/Presentation/TaskSettings/TaskSettingsPageBuilder.cs`

Commit'in ilgili C# diff'i:

```diff
--- ShareX.ScreenCaptureLib/Forms/FFmpegOptionsForm.Designer.cs
--- a/ShareX.ScreenCaptureLib/Forms/FFmpegOptionsForm.Designer.cs
+++ /dev/null
@@ -1,858 +0,0 @@
-﻿namespace ShareX.ScreenCaptureLib
-{
-    partial class FFmpegOptionsForm
-    {
-        /// <summary>
-        /// Required designer variable.
-        /// </summary>
-        private System.ComponentModel.IContainer components = null;
-
-        /// <summary>
-        /// Clean up any resources being used.
-        /// </summary>
-        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
-        protected override void Dispose(bool disposing)
-        {
-            if (disposing && (components != null))
-            {
-                components.Dispose();
-            }
-            base.Dispose(disposing);
-        }
-
-        #region Windows Form Designer generated code
-
-        /// <summary>
-        /// Required method for Designer support - do not modify
-        /// the contents of this method with the code editor.
-        /// </summary>
-        private void InitializeComponent()
-        {
-            components = new System.ComponentModel.Container();
-            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FFmpegOptionsForm));
-            ttHelpTip = new System.Windows.Forms.ToolTip(components);
-            pbx264PresetWarning = new System.Windows.Forms.PictureBox();
-            cbx264Preset = new System.Windows.Forms.ComboBox();
-            nudx264CRF = new System.Windows.Forms.NumericUpDown();
-            nudXvidQscale = new System.Windows.Forms.NumericUpDown();
-            nudGIFBayerScale = new System.Windows.Forms.NumericUpDown();
-            cbGIFDither = new System.Windows.Forms.ComboBox();
-            cbGIFStatsMode = new System.Windows.Forms.ComboBox();
-            cbVideoCodec = new System.Windows.Forms.ComboBox();
-            btnFFmpegBrowse = new System.Windows.Forms.Button();
-            txtFFmpegPath = new System.Windows.Forms.TextBox();
-            cbCustomCommands = new System.Windows.Forms.CheckBox();
-            txtCommandLinePreview = new System.Windows.Forms.TextBox();
-            txtUserArgs = new System.Windows.Forms.TextBox();
-            cbVideoSource = new System.Windows.Forms.ComboBox();
-            lblVideoSource = new System.Windows.Forms.Label();
-            cbAudioSource = new System.Windows.Forms.ComboBox();
-            lblAudioSource = new System.Windows.Forms.Label();
-            cbAudioCodec = new System.Windows.Forms.ComboBox();
-            lblCommandLineArgs = new System.Windows.Forms.Label();
-            cbUseCustomFFmpegPath = new System.Windows.Forms.CheckBox();
-            lblVideoEncoder = new System.Windows.Forms.Label();
-            lblAudioEncoder = new System.Windows.Forms.Label();
-            tcFFmpegAudioCodecs = new ShareX.HelpersLib.TablessControl();
-            tpAAC = new System.Windows.Forms.TabPage();
-            lblAACBitrateK = new System.Windows.Forms.Label();
-            cbAACBitrate = new System.Windows.Forms.ComboBox();
-            lblAACBitrate = new System.Windows.Forms.Label();
-            tpOpus = new System.Windows.Forms.TabPage();
-            lblOpusBitrateK = new System.Windows.Forms.Label();
-            cbOpusBitrate = new System.Windows.Forms.ComboBox();
-            lblOpusBitrate = new System.Windows.Forms.Label();
-            tpVorbis = new System.Windows.Forms.TabPage();
-            cbVorbisQuality = new System.Windows.Forms.ComboBox();
-            lblVorbisQuality = new System.Windows.Forms.Label();
-            tpMP3 = new System.Windows.Forms.TabPage();
-            cbMP3Quality = new System.Windows.Forms.ComboBox();
-            lblMP3Quality = new System.Windows.Forms.Label();
-            tcFFmpegVideoCodecs = new ShareX.HelpersLib.TablessControl();
-            tpX264 = new System.Windows.Forms.TabPage();
-            lblx264BitrateK = new System.Windows.Forms.Label();
-            cbx264UseBitrate = new System.Windows.Forms.CheckBox();
-            lblx264CRF = new System.Windows.Forms.Label();
-            lblx264Preset = new System.Windows.Forms.Label();
-            nudx264Bitrate = new System.Windows.Forms.NumericUpDown();
-            tpVpx = new System.Windows.Forms.TabPage();
-            lblVP8BitrateK = new System.Windows.Forms.Label();
-            nudVP8Bitrate = new System.Windows.Forms.NumericUpDown();
-            lblVP8Bitrate = new System.Windows.Forms.Label();
-            tpXvid = new System.Windows.Forms.TabPage();
-            lblXvidQscale = new System.Windows.Forms.Label();
-            tpNVENC = new System.Windows.Forms.TabPage();
-            cbNVENCTune = new System.Windows.Forms.ComboBox();
-            lblNVENCTune = new System.Windows.Forms.Label();
-            lblNVENCBitrateK = new System.Windows.Forms.Label();
-            cbNVENCPreset = new System.Windows.Forms.ComboBox();
-            lblNVENCPreset = new System.Windows.Forms.Label();
-            nudNVENCBitrate = new System.Windows.Forms.NumericUpDown();
-            lblNVENCBitrate = new System.Windows.Forms.Label();
-            tpGIF = new System.Windows.Forms.TabPage();
-            lblGIFDither = new System.Windows.Forms.Label();
-            lblGIFStatsMode = new System.Windows.Forms.Label();
-            tpAMF = new System.Windows.Forms.TabPage();
-            lblAMFBitrateK = new System.Windows.Forms.Label();
-            nudAMFBitrate = new System.Windows.Forms.NumericUpDown();
-            lblAMFBitrate = new System.Windows.Forms.Label();
-            cbAMFQuality = new System.Windows.Forms.ComboBox();
-            lblAMFQuality = new System.Windows.Forms.Label();
-            cbAMFUsage = new System.Windows.Forms.ComboBox();
-            lblAMFUsage = new System.Windows.Forms.Label();
-            tpQSV = new System.Windows.Forms.TabPage();
-            lblQSVBitrateK = new System.Windows.Forms.Label();
-            cbQSVPreset = new System.Windows.Forms.ComboBox();
-            lblQSVPreset = new System.Windows.Forms.Label();
-            nudQSVBitrate = new System.Windows.Forms.NumericUpDown();
-            lblQSVBitrate = new System.Windows.Forms.Label();
-            btnResetOptions = new System.Windows.Forms.Button();
-            btnDownloadRecorderDevices = new System.Windows.Forms.Button();
-            ((System.ComponentModel.ISupportInitialize)pbx264PresetWarning).BeginInit();
-            ((System.ComponentModel.ISupportInitialize)nudx264CRF).BeginInit();
-            ((System.ComponentModel.ISupportInitialize)nudXvidQscale).BeginInit();
-            ((System.ComponentModel.ISupportInitialize)nudGIFBayerScale).BeginInit();
-            tcFFmpegAudioCodecs.SuspendLayout();
-            tpAAC.SuspendLayout();
-            tpOpus.SuspendLayout();
-            tpVorbis.SuspendLayout();
-            tpMP3.SuspendLayout();
-            tcFFmpegVideoCodecs.SuspendLayout();
-            tpX264.SuspendLayout();
-            ((System.ComponentModel.ISupportInitialize)nudx264Bitrate).BeginInit();
-            tpVpx.SuspendLayout();
-            ((System.ComponentModel.ISupportInitialize)nudVP8Bitrate).BeginInit();
-            tpXvid.SuspendLayout();
-            tpNVENC.SuspendLayout();
-            ((System.ComponentModel.ISupportInitialize)nudNVENCBitrate).BeginInit();
-            tpGIF.SuspendLayout();
-            tpAMF.SuspendLayout();
-            ((System.ComponentModel.ISupportInitialize)nudAMFBitrate).BeginInit();
-            tpQSV.SuspendLayout();
-            ((System.ComponentModel.ISupportInitialize)nudQSVBitrate).BeginInit();
-            SuspendLayout();
-            // 
-            // ttHelpTip
-            // 
-            ttHelpTip.AutomaticDelay = 0;
-            ttHelpTip.AutoPopDelay = 30000;
-            ttHelpTip.BackColor = System.Drawing.SystemColors.Window;
-            ttHelpTip.InitialDelay = 500;
-            ttHelpTip.ReshowDelay = 100;
-            ttHelpTip.UseAnimation = false;
-            ttHelpTip.UseFading = false;
-            // 
-            // pbx264PresetWarning
-            // 
-            pbx264PresetWarning.Image = Properties.Resources.exclamation_button;
-            resources.ApplyResources(pbx264PresetWarning, "pbx264PresetWarning");
-            pbx264PresetWarning.Name = "pbx264PresetWarning";
-            pbx264PresetWarning.TabStop = false;
-            ttHelpTip.SetToolTip(pbx264PresetWarning, resources.GetString("pbx264PresetWarning.ToolTip"));
-            // 
-            // cbx264Preset
-            // 
-            cbx264Preset.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
-            cbx264Preset.FormattingEnabled = true;
```

Kirpildi; tam diff: https://github.com/sharex/sharex/commit/d27199c22070855a18cdd474a3770b8da4356b8a

Sonraki tarihsel olgular:

- `ShareX.ScreenCaptureLib/Forms/FFmpegOptionsForm.Designer.cs`
  - Gozlem araliginda sonraki degisiklik yok
- `ShareX.ScreenCaptureLib/Forms/FFmpegOptionsForm.cs`
  - Gozlem araliginda sonraki degisiklik yok
- `ShareX.ScreenCaptureLib/Presentation/FFmpegOptions/FFmpegOptionsWindow.axaml.cs`
  - `3ca088301318` 2026-07-28 Remove obsolete code and simplify project structure
    https://github.com/sharex/sharex/commit/3ca088301318297ca90d3fb5e928359f463e08fa
  - `fd314b9cd663` 2026-08-02 Localize FFmpeg options window strings
    https://github.com/sharex/sharex/commit/fd314b9cd663ccaaf1d0396f25b3e94fc0f9e111
  - `a423f3d639c1` 2026-08-03 Update ShareX application functionality
    https://github.com/sharex/sharex/commit/a423f3d639c12ba2eb62888fb33213e3fedfcd31
- `ShareX/Presentation/TaskSettings/TaskSettingsPageBuilder.cs`
  - `e8044e3b08a9` 2026-07-31 Add notification button editor to task settings
    https://github.com/sharex/sharex/commit/e8044e3b08a9f7333a79bdb980e6c26e2632cee7
  - `b6f50f18465b` 2026-07-31 Add configurable notification action button size
    https://github.com/sharex/sharex/commit/b6f50f18465bda8783a0ffc650fe9be2b44c21a6
  - `358fe784592f` 2026-08-01 Update ShareX functionality and related components
    https://github.com/sharex/sharex/commit/358fe784592f9dfa4d3c918cd052b5eb667e2670

Karar:

[ KUSUR-GETIRMEDI ]

Not:

[ ]

---

### Ornek SAMPLE-09 — github.com/app-vnext/polly

Commit:

- Kisa SHA: `7b673c5bf57a`
- Yazar tarihi: 2025-01-28
- Mesaj basligi: Enable CA1724
- Baglanti: https://github.com/app-vnext/polly/commit/7b673c5bf57af808592e3f1851823f1e356f4229

Degisiklik ozeti:

- Degisen toplam dosya: 3
- Degisen `.cs` dosyasi: 2
- Eklenen satir: 5, silinen satir: 1
- Degisen `.cs` dosyalari:
  - `src/Polly/Bulkhead/AsyncBulkheadSyntax.cs`
  - `src/Polly/Policy.ContextAndKeys.cs`

Commit'in ilgili C# diff'i:

```diff
--- src/Polly/Bulkhead/AsyncBulkheadSyntax.cs
--- a/src/Polly/Bulkhead/AsyncBulkheadSyntax.cs
+++ b/src/Polly/Bulkhead/AsyncBulkheadSyntax.cs
@@ -1,7 +1,9 @@
 #nullable enable
 namespace Polly;
 
+#pragma warning disable CA1724
 public partial class Policy
+#pragma warning restore CA1724
 {
     /// <summary>
     /// <para>Builds a bulkhead isolation <see cref="Policy"/>, which limits the maximum concurrency of actions executed through the policy.  Imposing a maximum concurrency limits the potential of governed actions, when faulting, to bring down the system.</para>
--- src/Polly/Policy.ContextAndKeys.cs
--- a/src/Polly/Policy.ContextAndKeys.cs
+++ b/src/Polly/Policy.ContextAndKeys.cs
@@ -37,7 +37,9 @@ public abstract partial class Policy
     }
 }
 
+#pragma warning disable CA1724
 public abstract partial class Policy<TResult>
+#pragma warning restore CA1724
 {
     /// <summary>
     /// Sets the PolicyKey for this <see cref="Policy{TResult}"/> instance.
```

Sonraki tarihsel olgular:

- `src/Polly/Bulkhead/AsyncBulkheadSyntax.cs`
  - Gozlem araliginda sonraki degisiklik yok
- `src/Polly/Policy.ContextAndKeys.cs`
  - `dc50439e7462` 2025-02-19 Add missing coverage
    https://github.com/app-vnext/polly/commit/dc50439e746241470df78eb4babc549d94c10e08

Karar:

[ BAKILMADI ]

Not:

[ ]

---

### Ornek SAMPLE-10 — github.com/app-vnext/polly

Commit:

- Kisa SHA: `e984839b8324`
- Yazar tarihi: 2026-06-04
- Mesaj basligi: Add caller CancellationToken propagation for hedging and timeout (#3094)
- Baglanti: https://github.com/app-vnext/polly/commit/e984839b8324a163270f0bc686929a1066de69e0

Degisiklik ozeti:

- Degisen toplam dosya: 7
- Degisen `.cs` dosyasi: 7
- Eklenen satir: 368, silinen satir: 3
- Degisen `.cs` dosyalari:
  - `src/Polly.Core/Hedging/HedgingResilienceStrategy.cs`
  - `src/Polly.Core/Timeout/TimeoutResilienceStrategy.cs`
  - `src/Polly.Core/Utils/OutcomeUtilities.cs`
  - `test/Polly.Core.Tests/Hedging/HedgingResilienceStrategyTests.cs`
  - `test/Polly.Core.Tests/Issues/IssuesTests.CancellationTokenPropagation_3086.cs`
  - `test/Polly.Core.Tests/Timeout/TimeoutResilienceStrategyTests.cs`
  - `test/Polly.Core.Tests/Utils/OutcomeUtilitiesTests.cs`

Commit'in ilgili C# diff'i:

```diff
--- src/Polly.Core/Hedging/HedgingResilienceStrategy.cs
--- a/src/Polly.Core/Hedging/HedgingResilienceStrategy.cs
+++ b/src/Polly.Core/Hedging/HedgingResilienceStrategy.cs
@@ -1,5 +1,6 @@
 using Polly.Hedging.Utils;
 using Polly.Telemetry;
+using Polly.Utils;
 
 namespace Polly.Hedging;
 
@@ -57,7 +58,7 @@ internal sealed class HedgingResilienceStrategy<T> : ResilienceStrategy<T>
 
                 if (loadedExecution.Outcome is Outcome<T> outcome)
                 {
-                    return outcome;
+                    return outcome.WithCallerCancellationToken(cancellationToken);
                 }
 
                 var delay = await GetHedgingDelayAsync(context, hedgingContext.LoadedTasks).ConfigureAwait(continueOnCapturedContext);
@@ -72,7 +73,7 @@ internal sealed class HedgingResilienceStrategy<T> : ResilienceStrategy<T>
                 if (!execution.IsHandled)
                 {
                     execution.AcceptOutcome();
-                    return outcome;
+                    return outcome.WithCallerCancellationToken(cancellationToken);
                 }
             }
         }
--- src/Polly.Core/Timeout/TimeoutResilienceStrategy.cs
--- a/src/Polly.Core/Timeout/TimeoutResilienceStrategy.cs
+++ b/src/Polly.Core/Timeout/TimeoutResilienceStrategy.cs
@@ -1,4 +1,5 @@
 using Polly.Telemetry;
+using Polly.Utils;
 
 namespace Polly.Timeout;
 
@@ -89,7 +90,7 @@ internal sealed class TimeoutResilienceStrategy : ResilienceStrategy
             return Outcome.FromException<TResult>(timeoutException.TrySetStackTrace());
         }
 
-        return outcome;
+        return outcome.WithCallerCancellationToken(previousToken);
     }
 
     private static CancellationTokenRegistration CreateRegistration(CancellationTokenSource cancellationSource, CancellationToken previousToken)
--- src/Polly.Core/Utils/OutcomeUtilities.cs
--- /dev/null
+++ b/src/Polly.Core/Utils/OutcomeUtilities.cs
@@ -0,0 +1,32 @@
+namespace Polly.Utils;
+
+internal static class OutcomeUtilities
+{
+    /// <summary>
+    /// Ensures that an <see cref="OperationCanceledException"/> escaping a strategy that substituted the
+    /// execution <see cref="CancellationToken"/> (e.g. timeout or hedging) carries the caller's token when
+    /// the cancellation was caused by that token.
+    /// </summary>
+    /// <typeparam name="T">The result type of the outcome.</typeparam>
+    /// <param name="outcome">The outcome produced by the strategy.</param>
+    /// <param name="callerToken">The cancellation token that was associated with the execution before the strategy substituted it.</param>
+    /// <returns>
+    /// An outcome whose <see cref="OperationCanceledException"/> carries <paramref name="callerToken"/> when the caller
+    /// requested cancellation, preserving the original exception as its <see cref="Exception.InnerException"/>;
+    /// otherwise the original <paramref name="outcome"/> unchanged.
+    /// </returns>
+    /// <remarks>
+    /// The rewrite happens only when <paramref name="callerToken"/> actually requested cancellation. This preserves
+    /// the contract that a real timeout, or an unrelated <see cref="OperationCanceledException"/> thrown while the
+    /// caller's token was not cancelled, is left untouched.
+    /// </remarks>
+    public static Outcome<T> WithCallerCancellationToken<T>(this Outcome<T> outcome, CancellationToken callerToken)
+    {
+        if (callerToken.IsCancellationRequested && outcome.Exception is OperationCanceledException oce && oce.CancellationToken != callerToken)
+        {
+            return Outcome.FromException<T>(new OperationCanceledException(oce.Message, oce, callerToken).TrySetStackTrace());
+        }
+
+        return outcome;
+    }
+}
--- test/Polly.Core.Tests/Hedging/HedgingResilienceStrategyTests.cs
--- a/test/Polly.Core.Tests/Hedging/HedgingResilienceStrategyTests.cs
+++ b/test/Polly.Core.Tests/Hedging/HedgingResilienceStrategyTests.cs
@@ -935,6 +935,36 @@ public class HedgingResilienceStrategyTests : IDisposable
         _events.Select(v => v.Event.EventName).Distinct().Count().ShouldBe(2);
     }
 
+    [Fact]
+    public async Task ExecuteCore_CallerCancellation_EnsureExceptionCarriesCallerToken()
+    {
+        // arrange
+        using var cancellationSource = new CancellationTokenSource();
+        _options.MaxHedgedAttempts = 1;
+        _options.Delay = LongDelay; // ensure no secondary attempt starts before the primary completes
+        ConfigureHedging(); // ShouldHandle returns false, so the primary's outcome is accepted as-is
+
+        var strategy = (HedgingResilienceStrategy<string>)Create().GetPipelineDescriptor().FirstStrategy.StrategyInstance;
+
+        var context = ResilienceContextPool.Shared.Get(CancellationToken);
+        context.CancellationToken = cancellationSource.Token;
+
+        // act
+        var outcome = await strategy.ExecuteCore(
+            (ctx, _) =>
+            {
+                cancellationSource.Cancel();
+                ctx.CancellationToken.ThrowIfCancellationRequested();
+                return Outcome.FromResultAsValueTask("unreachable");
+            },
+            context,
+            "state");
+
+        // assert
+        var exception = outcome.Exception.ShouldBeOfType<OperationCanceledException>();
+        exception.CancellationToken.ShouldBe(cancellationSource.Token);
+    }
+
     private void ConfigureHedging() =>
         ConfigureHedging(_ => false, _actions.Generator);
 
--- test/Polly.Core.Tests/Issues/IssuesTests.CancellationTokenPropagation_3086.cs
--- /dev/null
+++ b/test/Polly.Core.Tests/Issues/IssuesTests.CancellationTokenPropagation_3086.cs
@@ -0,0 +1,203 @@
+using Polly.Hedging;
+using Polly.Timeout;
+
+namespace Polly.Core.Tests.Issues;
+
+public partial class IssuesTests
+{
+    // https://github.com/App-vNext/Polly/issues/3086
+    // When a strategy substitutes the caller's CancellationToken with an internal one (timeout, hedging),
+    // a caller-initiated cancellation must still surface an OperationCanceledException carrying the
+    // caller's token, so that callers can distinguish caller cancellation from other failures.
+    [Fact]
+    public async Task Timeout_CallerCancellation_ExceptionCarriesCallerToken_3086()
+    {
+        var pipeline = new ResiliencePipelineBuilder()
+            .AddTimeout(TimeSpan.FromMinutes(1))
+            .Build();
+
+        using var cts = new CancellationTokenSource();
+
+        var exception = await Should.ThrowAsync<OperationCanceledException>(() =>
+            pipeline.ExecuteAsync(
+                async token =>
+                {
+                    cts.Cancel(); // simulate cancellation request from upstream caller
+                    token.ThrowIfCancellationRequested(); // simulate cancellation response from downstream code
+                },
+                cts.Token).AsTask());
+
+        exception.CancellationToken.ShouldBe(cts.Token);
+    }
+
+    [Fact]
+    public async Task TimeoutThenRetry_CallerCancellation_ExceptionCarriesCallerToken_3086()
```

Kirpildi; tam diff: https://github.com/app-vnext/polly/commit/e984839b8324a163270f0bc686929a1066de69e0

Sonraki tarihsel olgular:

- `src/Polly.Core/Hedging/HedgingResilienceStrategy.cs`
  - Gozlem araliginda sonraki degisiklik yok
- `src/Polly.Core/Timeout/TimeoutResilienceStrategy.cs`
  - `3987bb8c0a87` 2026-08-08 Bump SonarAnalyzer.CSharp from 10.29.0.143774 to 10.31.0.145097 (#3190)
    https://github.com/app-vnext/polly/commit/3987bb8c0a876314ea0e29d200c333f0fdef5e53
- `src/Polly.Core/Utils/OutcomeUtilities.cs`
  - Gozlem araliginda sonraki degisiklik yok
- `test/Polly.Core.Tests/Hedging/HedgingResilienceStrategyTests.cs`
  - Gozlem araliginda sonraki degisiklik yok
- `test/Polly.Core.Tests/Issues/IssuesTests.CancellationTokenPropagation_3086.cs`
  - Gozlem araliginda sonraki degisiklik yok
- `test/Polly.Core.Tests/Timeout/TimeoutResilienceStrategyTests.cs`
  - `6cc79edccae5` 2026-07-17 Bump NSubstitute from 5.3.0 to 6.0.0 (#3165)
    https://github.com/app-vnext/polly/commit/6cc79edccae5d6f236fd011a69fb12e8f913be80
- `test/Polly.Core.Tests/Utils/OutcomeUtilitiesTests.cs`
  - Gozlem araliginda sonraki degisiklik yok

Karar:

[ KUSUR-GETIRMEDI ]

Not:

[ ]

---

### Ornek SAMPLE-11 — github.com/jellyfin/jellyfin

Commit:

- Kisa SHA: `9e480f6efb4b`
- Yazar tarihi: 2025-11-11
- Mesaj basligi: Update to .NET 10.0
- Baglanti: https://github.com/jellyfin/jellyfin/commit/9e480f6efb4bc0e1f0d1323ed7ed5a7208fded99

Degisiklik ozeti:

- Degisen toplam dosya: 48
- Degisen `.cs` dosyasi: 10
- Eklenen satir: 99, silinen satir: 140
- Degisen `.cs` dosyalari:
  - `Jellyfin.Api/Helpers/HlsHelpers.cs`
  - `Jellyfin.Server.Implementations/FullSystemBackup/BackupService.cs`
  - `Jellyfin.Server/Extensions/ApiServiceCollectionExtensions.cs`
  - `MediaBrowser.Common/Net/NetworkConstants.cs`
  - `MediaBrowser.Common/Net/NetworkUtils.cs`
  - `MediaBrowser.Model/Net/IPData.cs`
  - `src/Jellyfin.Database/Jellyfin.Database.Implementations/Locking/OptimisticLockBehavior.cs`
  - `src/Jellyfin.Database/Jellyfin.Database.Implementations/Locking/PessimisticLockBehavior.cs`
  - `src/Jellyfin.Networking/Manager/NetworkManager.cs`
  - `tests/Jellyfin.Server.Tests/ParseNetworkTests.cs`

Commit'in ilgili C# diff'i:

```diff
--- Jellyfin.Api/Helpers/HlsHelpers.cs
--- a/Jellyfin.Api/Helpers/HlsHelpers.cs
+++ b/Jellyfin.Api/Helpers/HlsHelpers.cs
@@ -45,15 +45,9 @@ public static class HlsHelpers
                     using var reader = new StreamReader(fileStream);
                     var count = 0;
 
-                    while (!reader.EndOfStream)
+                    string? line;
+                    while ((line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false)) is not null)
                     {
-                        var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
-                        if (line is null)
-                        {
-                            // Nothing currently in buffer.
-                            break;
-                        }
-
                         if (line.Contains("#EXTINF:", StringComparison.OrdinalIgnoreCase))
                         {
                             count++;
--- Jellyfin.Server.Implementations/FullSystemBackup/BackupService.cs
--- a/Jellyfin.Server.Implementations/FullSystemBackup/BackupService.cs
+++ b/Jellyfin.Server.Implementations/FullSystemBackup/BackupService.cs
@@ -102,7 +102,7 @@ public class BackupService : IBackupService
             }
 
             BackupManifest? manifest;
-            var manifestStream = zipArchiveEntry.Open();
+            var manifestStream = await zipArchiveEntry.OpenAsync().ConfigureAwait(false);
             await using (manifestStream.ConfigureAwait(false))
             {
                 manifest = await JsonSerializer.DeserializeAsync<BackupManifest>(manifestStream, _serializerSettings).ConfigureAwait(false);
@@ -160,7 +160,7 @@ public class BackupService : IBackupService
                     }
 
                     HistoryRow[] historyEntries;
-                    var historyArchive = historyEntry.Open();
+                    var historyArchive = await historyEntry.OpenAsync().ConfigureAwait(false);
                     await using (historyArchive.ConfigureAwait(false))
                     {
                         historyEntries = await JsonSerializer.DeserializeAsync<HistoryRow[]>(historyArchive).ConfigureAwait(false) ??
@@ -204,7 +204,7 @@ public class BackupService : IBackupService
                             continue;
                         }
 
-                        var zipEntryStream = zipEntry.Open();
+                        var zipEntryStream = await zipEntry.OpenAsync().ConfigureAwait(false);
                         await using (zipEntryStream.ConfigureAwait(false))
                         {
                             _logger.LogInformation("Restore backup of {Table}", entityType.Type.Name);
@@ -329,7 +329,7 @@ public class BackupService : IBackupService
                             _logger.LogInformation("Begin backup of entity {Table}", entityType.SourceName);
                             var zipEntry = zipArchive.CreateEntry(NormalizePathSeparator(Path.Combine("Database", $"{entityType.SourceName}.json")));
                             var entities = 0;
-                            var zipEntryStream = zipEntry.Open();
+                            var zipEntryStream = await zipEntry.OpenAsync().ConfigureAwait(false);
                             await using (zipEntryStream.ConfigureAwait(false))
                             {
                                 var jsonSerializer = new Utf8JsonWriter(zipEntryStream);
@@ -366,7 +366,7 @@ public class BackupService : IBackupService
                 foreach (var item in Directory.EnumerateFiles(_applicationPaths.ConfigurationDirectoryPath, "*.xml", SearchOption.TopDirectoryOnly)
                              .Union(Directory.EnumerateFiles(_applicationPaths.ConfigurationDirectoryPath, "*.json", SearchOption.TopDirectoryOnly)))
                 {
-                    zipArchive.CreateEntryFromFile(item, NormalizePathSeparator(Path.Combine("Config", Path.GetFileName(item))));
+                    await zipArchive.CreateEntryFromFileAsync(item, NormalizePathSeparator(Path.Combine("Config", Path.GetFileName(item)))).ConfigureAwait(false);
                 }
 
                 void CopyDirectory(string source, string target, string filter = "*")
@@ -380,6 +380,7 @@ public class BackupService : IBackupService
 
                     foreach (var item in Directory.EnumerateFiles(source, filter, SearchOption.AllDirectories))
                     {
+                        // TODO: @bond make async
                         zipArchive.CreateEntryFromFile(item, NormalizePathSeparator(Path.Combine(target, Path.GetRelativePath(source, item))));
                     }
                 }
@@ -405,7 +406,7 @@ public class BackupService : IBackupService
                     CopyDirectory(Path.Combine(_applicationPaths.InternalMetadataPath), Path.Combine("Data", "metadata"));
                 }
 
-                var manifestStream = zipArchive.CreateEntry(ManifestEntryName).Open();
+                var manifestStream = await zipArchive.CreateEntry(ManifestEntryName).OpenAsync().ConfigureAwait(false);
                 await using (manifestStream.ConfigureAwait(false))
                 {
                     await JsonSerializer.SerializeAsync(manifestStream, manifest).ConfigureAwait(false);
@@ -505,7 +506,7 @@ public class BackupService : IBackupService
                 return null;
             }
 
-            var manifestStream = manifestEntry.Open();
+            var manifestStream = await manifestEntry.OpenAsync().ConfigureAwait(false);
             await using (manifestStream.ConfigureAwait(false))
             {
                 return await JsonSerializer.DeserializeAsync<BackupManifest>(manifestStream, _serializerSettings).ConfigureAwait(false);
--- Jellyfin.Server/Extensions/ApiServiceCollectionExtensions.cs
--- a/Jellyfin.Server/Extensions/ApiServiceCollectionExtensions.cs
+++ b/Jellyfin.Server/Extensions/ApiServiceCollectionExtensions.cs
@@ -174,7 +174,7 @@ namespace Jellyfin.Server.Extensions
             if (config.KnownProxies.Length == 0)
             {
                 options.ForwardedHeaders = ForwardedHeaders.None;
-                options.KnownNetworks.Clear();
+                options.KnownIPNetworks.Clear();
                 options.KnownProxies.Clear();
             }
             else
@@ -184,7 +184,7 @@ namespace Jellyfin.Server.Extensions
             }
 
             // Only set forward limit if we have some known proxies or some known networks.
-            if (options.KnownProxies.Count != 0 || options.KnownNetworks.Count != 0)
+            if (options.KnownProxies.Count != 0 || options.KnownIPNetworks.Count != 0)
             {
                 options.ForwardLimit = null;
             }
@@ -290,10 +290,7 @@ namespace Jellyfin.Server.Extensions
                 }
                 else if (NetworkUtils.TryParseToSubnet(allowedProxies[i], out var subnet))
                 {
-                    if (subnet is not null)
-                    {
-                        AddIPAddress(config, options, subnet.Prefix, subnet.PrefixLength);
-                    }
+                    AddIPAddress(config, options, subnet.BaseAddress, subnet.PrefixLength);
                 }
                 else if (NetworkUtils.TryParseHost(allowedProxies[i], out var addresses, config.EnableIPv4, config.EnableIPv6))
                 {
@@ -323,7 +320,7 @@ namespace Jellyfin.Server.Extensions
             }
             else
             {
-                options.KnownNetworks.Add(new Microsoft.AspNetCore.HttpOverrides.IPNetwork(addr, prefixLength));
+                options.KnownIPNetworks.Add(new System.Net.IPNetwork(addr, prefixLength));
             }
         }
 
--- MediaBrowser.Common/Net/NetworkConstants.cs
--- a/MediaBrowser.Common/Net/NetworkConstants.cs
+++ b/MediaBrowser.Common/Net/NetworkConstants.cs
@@ -1,5 +1,4 @@
 using System.Net;
-using IPNetwork = Microsoft.AspNetCore.HttpOverrides.IPNetwork;
 
 namespace MediaBrowser.Common.Net;
 
--- MediaBrowser.Common/Net/NetworkUtils.cs
--- a/MediaBrowser.Common/Net/NetworkUtils.cs
+++ b/MediaBrowser.Common/Net/NetworkUtils.cs
@@ -6,7 +6,6 @@ using System.Net;
 using System.Net.Sockets;
 using System.Text.RegularExpressions;
 using Jellyfin.Extensions;
-using IPNetwork = Microsoft.AspNetCore.HttpOverrides.IPNetwork;
 
 namespace MediaBrowser.Common.Net;
 
@@ -196,7 +195,7 @@ public static partial class NetworkUtils
     /// <param name="result">An <see cref="IPNetwork"/>.</param>
     /// <param name="negated">Boolean signaling if negated or not negated values should be parsed.</param>
```

Kirpildi; tam diff: https://github.com/jellyfin/jellyfin/commit/9e480f6efb4bc0e1f0d1323ed7ed5a7208fded99

Sonraki tarihsel olgular:

- `Jellyfin.Api/Helpers/HlsHelpers.cs`
  - Gozlem araliginda sonraki degisiklik yok
- `Jellyfin.Server.Implementations/FullSystemBackup/BackupService.cs`
  - `7825fa4e43f1` 2026-03-30 Backport pull request #16425 from jellyfin/release-10.11.z
    https://github.com/jellyfin/jellyfin/commit/7825fa4e43f1970aa46d5ee0e986dee019bf4dd2
  - `fa07a3abe89b` 2026-06-26 Skip backups whens can is running
    https://github.com/jellyfin/jellyfin/commit/fa07a3abe89b6e0eb96a9f8d8a3eb57dea20ca2a
  - `d7727224c2f5` 2026-07-17 Skip corrupt KeyframeData rows during full system backup
    https://github.com/jellyfin/jellyfin/commit/d7727224c2f5024c9981bc70a274826beb7f1cdf
- `Jellyfin.Server/Extensions/ApiServiceCollectionExtensions.cs`
  - `1ba8e2c93c29` 2025-11-16 Fix tests
    https://github.com/jellyfin/jellyfin/commit/1ba8e2c93c2906682050c95957649c20e1b557d9
  - `6e74be0d46f4` 2025-12-03 Backport pull request #15672 from jellyfin/release-10.11.z
    https://github.com/jellyfin/jellyfin/commit/6e74be0d46f409b7b63f02a29cbbbd572f40bd32
  - `23b48a0d0f92` 2026-01-02 Upgrade Swashbuckle and fix OpenAPI spec (#15886)
    https://github.com/jellyfin/jellyfin/commit/23b48a0d0f92706bc4f533cfa78077796ce8da61
- `MediaBrowser.Common/Net/NetworkConstants.cs`
  - Gozlem araliginda sonraki degisiklik yok
- `MediaBrowser.Common/Net/NetworkUtils.cs`
  - `1ba8e2c93c29` 2025-11-16 Fix tests
    https://github.com/jellyfin/jellyfin/commit/1ba8e2c93c2906682050c95957649c20e1b557d9
  - `f24709f11c82` 2026-05-10 Print warning on invalid Subnets in Network/Proxy configuration (#16793)
    https://github.com/jellyfin/jellyfin/commit/f24709f11c82bb85b70f073c89d3d21c30b1cde5
  - `e811cd7caf43` 2026-05-13 Prevent unecessary log spam in NetworkUtils
    https://github.com/jellyfin/jellyfin/commit/e811cd7caf434acd84645af8bd05248b54c65c39
- `MediaBrowser.Model/Net/IPData.cs`
  - Gozlem araliginda sonraki degisiklik yok
- `src/Jellyfin.Database/Jellyfin.Database.Implementations/Locking/OptimisticLockBehavior.cs`
  - `868ad089f814` 2026-08-06 Fix captured (and discarded) Execute exceptions
    https://github.com/jellyfin/jellyfin/commit/868ad089f814740e70939be7ce821a8220c3b4ff
- `src/Jellyfin.Database/Jellyfin.Database.Implementations/Locking/PessimisticLockBehavior.cs`
  - `dd81e52f8182` 2026-08-06 Add warning that PessimisticLockBehavior is unsafe
    https://github.com/jellyfin/jellyfin/commit/dd81e52f818295a3ef12880f2be8f24c8c1269c0
- `src/Jellyfin.Networking/Manager/NetworkManager.cs`
  - `1ba8e2c93c29` 2025-11-16 Fix tests
    https://github.com/jellyfin/jellyfin/commit/1ba8e2c93c2906682050c95957649c20e1b557d9
  - `93902fc610a9` 2025-12-20 fix crashes on devices that don't support ipv6
    https://github.com/jellyfin/jellyfin/commit/93902fc610a9d8b52780d88f7bb986e668567c9d
  - `146681f0ba92` 2025-12-21 Warn server administrator when IPv6 is enabled but unsupported by OS
    https://github.com/jellyfin/jellyfin/commit/146681f0ba927b6c2d1e392a2b157a28c36e1a6b
- `tests/Jellyfin.Server.Tests/ParseNetworkTests.cs`
  - `9962fbbe2ed2` 2026-04-21 fix: IPv6 prefixes not recognized as proxy https://github.com/jellyfin/jellyfin/issues/15710
    https://github.com/jellyfin/jellyfin/commit/9962fbbe2ed20a95dd1e9533fbc684070347f031

Karar:

[ KUSUR-GETIRMEDI ]

Not:

[ ]

---

### Ornek SAMPLE-12 — github.com/jellyfin/jellyfin

Commit:

- Kisa SHA: `4be3f5f1f9ff`
- Yazar tarihi: 2026-05-04
- Mesaj basligi: Add Accept-Language header support for per-request localization
- Baglanti: https://github.com/jellyfin/jellyfin/commit/4be3f5f1f9ff8bd0333033d6ad9c99711da03f96

Degisiklik ozeti:

- Degisen toplam dosya: 28
- Degisen `.cs` dosyasi: 27
- Eklenen satir: 571, silinen satir: 149
- Degisen `.cs` dosyalari:
  - `Emby.Server.Implementations/HttpServer/WebSocketConnection.cs`
  - `Emby.Server.Implementations/HttpServer/WebSocketManager.cs`
  - `Emby.Server.Implementations/Localization/LocalizationManager.cs`
  - `Jellyfin.Api/Controllers/LibraryController.cs`
  - `Jellyfin.Server.Implementations/Events/Consumers/Library/LyricDownloadFailureLogger.cs`
  - `Jellyfin.Server.Implementations/Events/Consumers/Library/SubtitleDownloadFailureLogger.cs`
  - `Jellyfin.Server.Implementations/Events/Consumers/Security/AuthenticationFailedLogger.cs`
  - `Jellyfin.Server.Implementations/Events/Consumers/Security/AuthenticationSucceededLogger.cs`
  - `Jellyfin.Server.Implementations/Events/Consumers/Session/PlaybackStartLogger.cs`
  - `Jellyfin.Server.Implementations/Events/Consumers/Session/PlaybackStopLogger.cs`
  - `Jellyfin.Server.Implementations/Events/Consumers/Session/SessionEndedLogger.cs`
  - `Jellyfin.Server.Implementations/Events/Consumers/Session/SessionStartedLogger.cs`
  - `Jellyfin.Server.Implementations/Events/Consumers/System/TaskCompletedLogger.cs`
  - `Jellyfin.Server.Implementations/Events/Consumers/Updates/PluginInstallationFailedLogger.cs`
  - `Jellyfin.Server.Implementations/Events/Consumers/Updates/PluginInstalledLogger.cs`
  - `Jellyfin.Server.Implementations/Events/Consumers/Updates/PluginUninstalledLogger.cs`
  - `Jellyfin.Server.Implementations/Events/Consumers/Updates/PluginUpdatedLogger.cs`
  - `Jellyfin.Server.Implementations/Events/Consumers/Users/UserCreatedLogger.cs`
  - `Jellyfin.Server.Implementations/Events/Consumers/Users/UserDeletedLogger.cs`
  - `Jellyfin.Server.Implementations/Events/Consumers/Users/UserLockedOutLogger.cs`
  - `Jellyfin.Server.Implementations/Events/Consumers/Users/UserPasswordChangedLogger.cs`
  - `Jellyfin.Server/Middleware/AcceptLanguageMiddleware.cs`
  - `Jellyfin.Server/Startup.cs`
  - `MediaBrowser.Controller/Net/BasePeriodicWebSocketListener.cs`
  - `MediaBrowser.Controller/Net/IWebSocketConnection.cs`
  - `MediaBrowser.Model/Globalization/ILocalizationManager.cs`
  - `tests/Jellyfin.Server.Implementations.Tests/Localization/LocalizationManagerTests.cs`

Commit'in ilgili C# diff'i:

```diff
--- Emby.Server.Implementations/HttpServer/WebSocketConnection.cs
--- a/Emby.Server.Implementations/HttpServer/WebSocketConnection.cs
+++ b/Emby.Server.Implementations/HttpServer/WebSocketConnection.cs
@@ -1,5 +1,7 @@
 using System;
 using System.Buffers;
+using System.Collections.Generic;
+using System.Globalization;
 using System.IO.Pipelines;
 using System.Net;
 using System.Net.WebSockets;
@@ -7,6 +9,7 @@ using System.Text;
 using System.Text.Json;
 using System.Threading;
 using System.Threading.Tasks;
+using Emby.Server.Implementations.Localization;
 using Jellyfin.Extensions.Json;
 using MediaBrowser.Controller.Net;
 using MediaBrowser.Controller.Net.WebSocketMessages;
@@ -69,6 +72,17 @@ namespace Emby.Server.Implementations.HttpServer
         /// <inheritdoc />
         public IPAddress? RemoteEndPoint { get; }
 
+        /// <summary>
+        /// Gets or initializes the culture fallback chain captured from the
+        /// <c>Accept-Language</c> header of the upgrade request.
+        /// </summary>
+        public IReadOnlyList<string>? RequestCultureFallback { get; init; }
+
+        /// <summary>
+        /// Gets or initializes the UI culture name captured from the upgrade request.
+        /// </summary>
+        public string? RequestUICulture { get; init; }
+
         /// <inheritdoc />
         public Func<WebSocketMessageInfo, Task>? OnReceive { get; set; }
 
@@ -82,6 +96,28 @@ namespace Emby.Server.Implementations.HttpServer
         public WebSocketState State => _socket.State;
 
         /// <inheritdoc />
+        public void ApplyRequestCulture()
+        {
+            if (RequestCultureFallback is not null)
+            {
+                LocalizationManager.RequestCultureFallback = RequestCultureFallback;
+            }
+
+            if (!string.IsNullOrEmpty(RequestUICulture))
+            {
+                try
+                {
+                    CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(RequestUICulture);
+                }
+                catch (CultureNotFoundException)
+                {
+                    // Jellyfin culture codes (e.g. "es_419") aren't always valid .NET cultures —
+                    // skip setting CurrentUICulture; RequestCultureFallback above carries the chain.
+                }
+            }
+        }
+
+        /// <inheritdoc />
         public async Task SendAsync(OutboundWebSocketMessage message, CancellationToken cancellationToken)
         {
             var json = JsonSerializer.SerializeToUtf8Bytes(message, _jsonOptions);
--- Emby.Server.Implementations/HttpServer/WebSocketManager.cs
--- a/Emby.Server.Implementations/HttpServer/WebSocketManager.cs
+++ b/Emby.Server.Implementations/HttpServer/WebSocketManager.cs
@@ -4,9 +4,11 @@
 
 using System;
 using System.Collections.Generic;
+using System.Globalization;
 using System.Linq;
 using System.Net.WebSockets;
 using System.Threading.Tasks;
+using Emby.Server.Implementations.Localization;
 using MediaBrowser.Common.Extensions;
 using MediaBrowser.Controller.Net;
 using Microsoft.AspNetCore.Http;
@@ -48,13 +50,22 @@ namespace Emby.Server.Implementations.HttpServer
 
                 WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync().ConfigureAwait(false);
 
+                // Capture the culture context set by AcceptLanguageMiddleware so it can be
+                // restored both when processing incoming messages and when periodic
+                // listeners produce server-initiated payloads on background tasks.
                 var connection = new WebSocketConnection(
                     _loggerFactory.CreateLogger<WebSocketConnection>(),
                     webSocket,
                     authorizationInfo,
                     context.GetNormalizedRemoteIP())
                 {
-                    OnReceive = ProcessWebSocketMessageReceived
+                    RequestCultureFallback = LocalizationManager.RequestCultureFallback,
+                    RequestUICulture = CultureInfo.CurrentUICulture.Name
+                };
+                connection.OnReceive = result =>
+                {
+                    connection.ApplyRequestCulture();
+                    return ProcessWebSocketMessageReceived(result);
                 };
                 await using (connection.ConfigureAwait(false))
                 {
--- Emby.Server.Implementations/Localization/LocalizationManager.cs
--- a/Emby.Server.Implementations/Localization/LocalizationManager.cs
+++ b/Emby.Server.Implementations/Localization/LocalizationManager.cs
@@ -3,10 +3,12 @@ using System.Collections.Concurrent;
 using System.Collections.Frozen;
 using System.Collections.Generic;
 using System.Diagnostics.CodeAnalysis;
+using System.Globalization;
 using System.IO;
 using System.Linq;
 using System.Reflection;
 using System.Text.Json;
+using System.Threading;
 using System.Threading.Tasks;
 using Jellyfin.Extensions;
 using Jellyfin.Extensions.Json;
@@ -26,21 +28,36 @@ namespace Emby.Server.Implementations.Localization
         private const string RatingsPath = "Emby.Server.Implementations.Localization.Ratings.";
         private const string CulturesPath = "Emby.Server.Implementations.Localization.iso6392.txt";
         private const string CountriesPath = "Emby.Server.Implementations.Localization.countries.json";
+        private const string CoreResourcePrefix = "Emby.Server.Implementations.Localization.Core.";
         private static readonly Assembly _assembly = typeof(LocalizationManager).Assembly;
         private static readonly string[] _unratedValues = ["n/a", "unrated", "not rated", "nr"];
 
+        /// <summary>
+        /// Gets the mapping from BCP-47 hyphenated culture codes to Jellyfin's underscore-based codes.
+        /// </summary>
+        public static readonly FrozenDictionary<string, string> Bcp47ToJellyfinMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
+        {
+            ["es-419"] = "es_419",
+            ["es-DO"] = "es_DO",
+            ["ur-PK"] = "ur_PK"
+        }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
+
         private readonly IServerConfigurationManager _configurationManager;
         private readonly ILogger<LocalizationManager> _logger;
 
         private readonly Dictionary<string, Dictionary<string, ParentalRatingScore?>> _allParentalRatings = new(StringComparer.OrdinalIgnoreCase);
 
-        private readonly ConcurrentDictionary<string, Dictionary<string, string>> _dictionaries = new(StringComparer.OrdinalIgnoreCase);
+        private static readonly AsyncLocal<IReadOnlyList<string>?> _requestCultureFallback = new();
+
+        private readonly ConcurrentDictionary<string, Dictionary<string, string>> _cultureOnlyDictionaries = new(StringComparer.OrdinalIgnoreCase);
 
         private readonly JsonSerializerOptions _jsonOptions = JsonDefaults.Options;
 
         private readonly ConcurrentDictionary<string, CultureDto?> _cultureCache = new(StringComparer.OrdinalIgnoreCase);
         private List<CultureDto> _cultures = [];
 
+        private static readonly IReadOnlyList<LocalizationOption> _localizationOptions = BuildLocalizationOptions();
+
         private FrozenDictionary<string, string> _iso6392BtoT = null!;
 
         /// <summary>
@@ -54,6 +71,41 @@ namespace Emby.Server.Implementations.Localization
```

Kirpildi; tam diff: https://github.com/jellyfin/jellyfin/commit/4be3f5f1f9ff8bd0333033d6ad9c99711da03f96

Sonraki tarihsel olgular:

- `Emby.Server.Implementations/HttpServer/WebSocketConnection.cs`
  - `5cfb379aa636` 2026-05-04 Use native middleware
    https://github.com/jellyfin/jellyfin/commit/5cfb379aa63689435077c8f1ebc10c98f625238c
  - `93d7a1cf2044` 2026-05-04 Cleanup
    https://github.com/jellyfin/jellyfin/commit/93d7a1cf20442e4a82b2819f8a5bdbe4f6c0ae97
  - `8f3eb3205d61` 2026-07-02 Close sessions for lost WebSockets to prevent zombie SyncPlay groups (#17079)
    https://github.com/jellyfin/jellyfin/commit/8f3eb3205d61d71638a1c695372cef273e76d2b3
- `Emby.Server.Implementations/HttpServer/WebSocketManager.cs`
  - `5cfb379aa636` 2026-05-04 Use native middleware
    https://github.com/jellyfin/jellyfin/commit/5cfb379aa63689435077c8f1ebc10c98f625238c
  - `93d7a1cf2044` 2026-05-04 Cleanup
    https://github.com/jellyfin/jellyfin/commit/93d7a1cf20442e4a82b2819f8a5bdbe4f6c0ae97
- `Emby.Server.Implementations/Localization/LocalizationManager.cs`
  - `5cfb379aa636` 2026-05-04 Use native middleware
    https://github.com/jellyfin/jellyfin/commit/5cfb379aa63689435077c8f1ebc10c98f625238c
  - `b8c0017b7466` 2026-05-13 Build BCP47 map reflexively
    https://github.com/jellyfin/jellyfin/commit/b8c0017b7466e5b50a8a32476469b8b6d2215b8c
  - `7a5181c3fd3a` 2026-05-14 Address review comments
    https://github.com/jellyfin/jellyfin/commit/7a5181c3fd3aea8a9913fe07086970c39c9bc1c4
- `Jellyfin.Api/Controllers/LibraryController.cs`
  - `c9f71d8531d9` 2026-05-29 Add a collection API for 'Included In' feature (#15516)
    https://github.com/jellyfin/jellyfin/commit/c9f71d8531d946f04d5395bd885f08128253559c
  - `c7111b757089` 2026-06-01 Only resolve symlinks on playback (#16965)
    https://github.com/jellyfin/jellyfin/commit/c7111b7570895cd999b8ca6abde9f8d558b99200
- `Jellyfin.Server.Implementations/Events/Consumers/Library/LyricDownloadFailureLogger.cs`
  - Gozlem araliginda sonraki degisiklik yok
- `Jellyfin.Server.Implementations/Events/Consumers/Library/SubtitleDownloadFailureLogger.cs`
  - Gozlem araliginda sonraki degisiklik yok
- `Jellyfin.Server.Implementations/Events/Consumers/Security/AuthenticationFailedLogger.cs`
  - Gozlem araliginda sonraki degisiklik yok
- `Jellyfin.Server.Implementations/Events/Consumers/Security/AuthenticationSucceededLogger.cs`
  - Gozlem araliginda sonraki degisiklik yok
- `Jellyfin.Server.Implementations/Events/Consumers/Session/PlaybackStartLogger.cs`
  - Gozlem araliginda sonraki degisiklik yok
- `Jellyfin.Server.Implementations/Events/Consumers/Session/PlaybackStopLogger.cs`
  - Gozlem araliginda sonraki degisiklik yok
- `Jellyfin.Server.Implementations/Events/Consumers/Session/SessionEndedLogger.cs`
  - Gozlem araliginda sonraki degisiklik yok
- `Jellyfin.Server.Implementations/Events/Consumers/Session/SessionStartedLogger.cs`
  - Gozlem araliginda sonraki degisiklik yok
- `Jellyfin.Server.Implementations/Events/Consumers/System/TaskCompletedLogger.cs`
  - Gozlem araliginda sonraki degisiklik yok
- `Jellyfin.Server.Implementations/Events/Consumers/Updates/PluginInstallationFailedLogger.cs`
  - Gozlem araliginda sonraki degisiklik yok
- `Jellyfin.Server.Implementations/Events/Consumers/Updates/PluginInstalledLogger.cs`
  - Gozlem araliginda sonraki degisiklik yok
- `Jellyfin.Server.Implementations/Events/Consumers/Updates/PluginUninstalledLogger.cs`
  - Gozlem araliginda sonraki degisiklik yok
- `Jellyfin.Server.Implementations/Events/Consumers/Updates/PluginUpdatedLogger.cs`
  - Gozlem araliginda sonraki degisiklik yok
- `Jellyfin.Server.Implementations/Events/Consumers/Users/UserCreatedLogger.cs`
  - Gozlem araliginda sonraki degisiklik yok
- `Jellyfin.Server.Implementations/Events/Consumers/Users/UserDeletedLogger.cs`
  - Gozlem araliginda sonraki degisiklik yok
- `Jellyfin.Server.Implementations/Events/Consumers/Users/UserLockedOutLogger.cs`
  - Gozlem araliginda sonraki degisiklik yok
- `Jellyfin.Server.Implementations/Events/Consumers/Users/UserPasswordChangedLogger.cs`
  - Gozlem araliginda sonraki degisiklik yok
- `Jellyfin.Server/Middleware/AcceptLanguageMiddleware.cs`
  - `5cfb379aa636` 2026-05-04 Use native middleware
    https://github.com/jellyfin/jellyfin/commit/5cfb379aa63689435077c8f1ebc10c98f625238c
- `Jellyfin.Server/Startup.cs`
  - `5cfb379aa636` 2026-05-04 Use native middleware
    https://github.com/jellyfin/jellyfin/commit/5cfb379aa63689435077c8f1ebc10c98f625238c
  - `95164883c1ac` 2026-05-05 Add Fallback
    https://github.com/jellyfin/jellyfin/commit/95164883c1acb9a48002ae2c8182a7ddb05fb4cb
- `MediaBrowser.Controller/Net/BasePeriodicWebSocketListener.cs`
  - Gozlem araliginda sonraki degisiklik yok
- `MediaBrowser.Controller/Net/IWebSocketConnection.cs`
  - Gozlem araliginda sonraki degisiklik yok
- `MediaBrowser.Model/Globalization/ILocalizationManager.cs`
  - `2cd2f36fe491` 2026-07-20 Extract truncation logic to helper and add tests
    https://github.com/jellyfin/jellyfin/commit/2cd2f36fe4912cb465cf5e78a055463f8a5c44a9
- `tests/Jellyfin.Server.Implementations.Tests/Localization/LocalizationManagerTests.cs`
  - `5cfb379aa636` 2026-05-04 Use native middleware
    https://github.com/jellyfin/jellyfin/commit/5cfb379aa63689435077c8f1ebc10c98f625238c
  - `f398b6d08b46` 2026-06-26 Fix localization lookup
    https://github.com/jellyfin/jellyfin/commit/f398b6d08b46544f61523c6871624201a2b54dfc
  - `2cd2f36fe491` 2026-07-20 Extract truncation logic to helper and add tests
    https://github.com/jellyfin/jellyfin/commit/2cd2f36fe4912cb465cf5e78a055463f8a5c44a9

Karar:

[ KUSUR-GETIRMEDI ]

Not:

[ ]

---

### Ornek SAMPLE-13 — github.com/jellyfin/jellyfin

Commit:

- Kisa SHA: `98e41d5a14a5`
- Yazar tarihi: 2023-05-18
- Mesaj basligi: Styling, format, minor code changes (crobibero)
- Baglanti: https://github.com/jellyfin/jellyfin/commit/98e41d5a14a579113f354ae3cb32a9ff6bc41958

Degisiklik ozeti:

- Degisen toplam dosya: 4
- Degisen `.cs` dosyasi: 4
- Eklenen satir: 26, silinen satir: 41
- Degisen `.cs` dosyalari:
  - `Jellyfin.Api/Controllers/TrickplayController.cs`
  - `MediaBrowser.Providers/Trickplay/TrickplayImagesTask.cs`
  - `MediaBrowser.Providers/Trickplay/TrickplayManager.cs`
  - `MediaBrowser.Providers/Trickplay/TrickplayProvider.cs`

Commit'in ilgili C# diff'i:

```diff
--- Jellyfin.Api/Controllers/TrickplayController.cs
--- a/Jellyfin.Api/Controllers/TrickplayController.cs
+++ b/Jellyfin.Api/Controllers/TrickplayController.cs
@@ -6,18 +6,14 @@ using System.IO;
 using System.Linq;
 using System.Net.Mime;
 using System.Text;
-using System.Threading.Tasks;
 using Jellyfin.Api.Attributes;
 using Jellyfin.Api.Extensions;
-using Jellyfin.Api.Helpers;
-using MediaBrowser.Common.Extensions;
 using MediaBrowser.Controller.Library;
 using MediaBrowser.Controller.Trickplay;
 using MediaBrowser.Model;
 using Microsoft.AspNetCore.Authorization;
 using Microsoft.AspNetCore.Http;
 using Microsoft.AspNetCore.Mvc;
-using Microsoft.Extensions.Logging;
 
 namespace Jellyfin.Api.Controllers;
 
@@ -28,26 +24,18 @@ namespace Jellyfin.Api.Controllers;
 [Authorize]
 public class TrickplayController : BaseJellyfinApiController
 {
-    private readonly ILogger<TrickplayController> _logger;
-    private readonly IHttpContextAccessor _httpContextAccessor;
     private readonly ILibraryManager _libraryManager;
     private readonly ITrickplayManager _trickplayManager;
 
     /// <summary>
     /// Initializes a new instance of the <see cref="TrickplayController"/> class.
     /// </summary>
-    /// <param name="logger">Instance of the <see cref="ILogger{TrickplayController}"/> interface.</param>
-    /// <param name="httpContextAccessor">Instance of the <see cref="IHttpContextAccessor"/> interface.</param>
     /// <param name="libraryManager">Instance of <see cref="ILibraryManager"/>.</param>
     /// <param name="trickplayManager">Instance of <see cref="ITrickplayManager"/>.</param>
     public TrickplayController(
-        ILogger<TrickplayController> logger,
-        IHttpContextAccessor httpContextAccessor,
         ILibraryManager libraryManager,
         ITrickplayManager trickplayManager)
     {
-        _logger = logger;
-        _httpContextAccessor = httpContextAccessor;
         _libraryManager = libraryManager;
         _trickplayManager = trickplayManager;
     }
@@ -66,9 +54,9 @@ public class TrickplayController : BaseJellyfinApiController
     public ActionResult GetTrickplayHlsPlaylist(
         [FromRoute, Required] Guid itemId,
         [FromRoute, Required] int width,
-        [FromQuery] string? mediaSourceId)
+        [FromQuery] Guid? mediaSourceId)
     {
-        return GetTrickplayPlaylistInternal(width, mediaSourceId ?? itemId.ToString("N"));
+        return GetTrickplayPlaylistInternal(width, mediaSourceId ?? itemId);
     }
 
     /// <summary>
@@ -89,9 +77,9 @@ public class TrickplayController : BaseJellyfinApiController
         [FromRoute, Required] Guid itemId,
         [FromRoute, Required] int width,
         [FromRoute, Required] int index,
-        [FromQuery] string? mediaSourceId)
+        [FromQuery] Guid? mediaSourceId)
     {
-        var item = _libraryManager.GetItemById(mediaSourceId ?? itemId.ToString("N"));
+        var item = _libraryManager.GetItemById(mediaSourceId ?? itemId);
         if (item is null)
         {
             return NotFound();
@@ -106,28 +94,22 @@ public class TrickplayController : BaseJellyfinApiController
         return NotFound();
     }
 
-    private ActionResult GetTrickplayPlaylistInternal(int width, string mediaSourceId)
+    private ActionResult GetTrickplayPlaylistInternal(int width, Guid mediaSourceId)
     {
-        if (_httpContextAccessor.HttpContext is null)
-        {
-            throw new ResourceNotFoundException(nameof(_httpContextAccessor.HttpContext));
-        }
-
-        var tilesResolutions = _trickplayManager.GetTilesResolutions(Guid.Parse(mediaSourceId));
-        if (tilesResolutions is not null && tilesResolutions.ContainsKey(width))
+        var tilesResolutions = _trickplayManager.GetTilesResolutions(mediaSourceId);
+        if (tilesResolutions is not null && tilesResolutions.TryGetValue(width, out var tilesInfo))
         {
             var builder = new StringBuilder(128);
-            var tilesInfo = tilesResolutions[width];
 
             if (tilesInfo.TileCount > 0)
             {
                 const string urlFormat = "Trickplay/{0}/{1}.jpg?MediaSourceId={2}&api_key={3}";
                 const string decimalFormat = "{0:0.###}";
 
-                var resolution = tilesInfo.Width.ToString(CultureInfo.InvariantCulture) + "x" + tilesInfo.Height.ToString(CultureInfo.InvariantCulture);
-                var layout = tilesInfo.TileWidth.ToString(CultureInfo.InvariantCulture) + "x" + tilesInfo.TileHeight.ToString(CultureInfo.InvariantCulture);
+                var resolution = $"{tilesInfo.Width}x{tilesInfo.Height}";
+                var layout = $"{tilesInfo.TileWidth}x{tilesInfo.TileHeight}";
                 var tilesPerGrid = tilesInfo.TileWidth * tilesInfo.TileHeight;
-                var tileDuration = (decimal)tilesInfo.Interval / 1000;
+                var tileDuration = tilesInfo.Interval / 1000m;
                 var infDuration = tileDuration * tilesPerGrid;
                 var tileGridCount = (int)Math.Ceiling((decimal)tilesInfo.TileCount / tilesPerGrid);
 
@@ -153,15 +135,22 @@ public class TrickplayController : BaseJellyfinApiController
                         urlFormat,
                         width.ToString(CultureInfo.InvariantCulture),
                         i.ToString(CultureInfo.InvariantCulture),
-                        mediaSourceId,
-                        _httpContextAccessor.HttpContext.User.GetToken());
+                        mediaSourceId.ToString("N"),
+                        User.GetToken());
 
                     // EXTINF
-                    builder.Append("#EXTINF:").Append(string.Format(CultureInfo.InvariantCulture, decimalFormat, infDuration))
+                    builder
+                        .Append("#EXTINF:")
+                        .Append(string.Format(CultureInfo.InvariantCulture, decimalFormat, infDuration))
                         .AppendLine(",");
 
                     // EXT-X-TILES
-                    builder.Append("#EXT-X-TILES:RESOLUTION=").Append(resolution).Append(",LAYOUT=").Append(layout).Append(",DURATION=")
+                    builder
+                        .Append("#EXT-X-TILES:RESOLUTION=")
+                        .Append(resolution)
+                        .Append(",LAYOUT=")
+                        .Append(layout)
+                        .Append(",DURATION=")
                         .AppendLine(string.Format(CultureInfo.InvariantCulture, decimalFormat, tileDuration));
 
                     // URL
--- MediaBrowser.Providers/Trickplay/TrickplayImagesTask.cs
--- a/MediaBrowser.Providers/Trickplay/TrickplayImagesTask.cs
+++ b/MediaBrowser.Providers/Trickplay/TrickplayImagesTask.cs
@@ -94,7 +94,7 @@ public class TrickplayImagesTask : IScheduledTask
             }
             catch (Exception ex)
             {
-                _logger.LogError("Error creating trickplay files for {ItemName}: {Msg}", item.Name, ex);
+                _logger.LogError(ex, "Error creating trickplay files for {ItemName}", item.Name);
             }
 
             numComplete++;
--- MediaBrowser.Providers/Trickplay/TrickplayManager.cs
--- a/MediaBrowser.Providers/Trickplay/TrickplayManager.cs
+++ b/MediaBrowser.Providers/Trickplay/TrickplayManager.cs
@@ -33,6 +33,7 @@ public class TrickplayManager : ITrickplayManager
     private readonly IServerConfigurationManager _config;
 
     private static readonly SemaphoreSlim _resourcePool = new(1, 1);
+    private static readonly string[] _trickplayImgExtensions = { ".jpg" };
 
     /// <summary>
     /// Initializes a new instance of the <see cref="TrickplayManager"/> class.
@@ -95,10 +96,10 @@ public class TrickplayManager : ITrickplayManager
         var imgTempDir = string.Empty;
```

Kirpildi; tam diff: https://github.com/jellyfin/jellyfin/commit/98e41d5a14a579113f354ae3cb32a9ff6bc41958

Sonraki tarihsel olgular:

- `Jellyfin.Api/Controllers/TrickplayController.cs`
  - `d338253242f3` 2023-05-18 Fix styling for string builder
    https://github.com/jellyfin/jellyfin/commit/d338253242f3b7996e735b94eacfa9d67ed0913a
  - `049361b66cef` 2023-05-24 TrickplayController return 404 if playlist doesn't exist. Minor code style/format changes (crobibero)
    https://github.com/jellyfin/jellyfin/commit/049361b66cefe7cb26364f9c39ac57abc7826752
  - `619d1d47f27e` 2023-06-23 Move GetHlsPlaylist to ITrickplayManager
    https://github.com/jellyfin/jellyfin/commit/619d1d47f27e3ca2f2f249fa81fe23f8019ec0e7
- `MediaBrowser.Providers/Trickplay/TrickplayImagesTask.cs`
  - `f82af0478122` 2023-05-18 Trickplay task pagination
    https://github.com/jellyfin/jellyfin/commit/f82af0478122d668294affecd21f89fcec69a61d
  - `a2a144869d38` 2023-06-23 Minor code fixes (cvium)
    https://github.com/jellyfin/jellyfin/commit/a2a144869d38b295fac0c3ab5bf0ee66b84e4eb0
  - `453c65d6193f` 2023-11-10 Fix build after merge
    https://github.com/jellyfin/jellyfin/commit/453c65d6193ff9745d03e043725fd67712aaec62
- `MediaBrowser.Providers/Trickplay/TrickplayManager.cs`
  - `a9594cd8b47b` 2023-05-18 Minor code change
    https://github.com/jellyfin/jellyfin/commit/a9594cd8b47bece2e9732cc7addb6b118dd130a4
  - `0e2c362078c5` 2023-05-30 Move SkiaSharp related code to Jellyfin.Drawing and IImageEncoder
    https://github.com/jellyfin/jellyfin/commit/0e2c362078c5b0babaa0fd254106452e6d67ebe8
  - `619d1d47f27e` 2023-06-23 Move GetHlsPlaylist to ITrickplayManager
    https://github.com/jellyfin/jellyfin/commit/619d1d47f27e3ca2f2f249fa81fe23f8019ec0e7
- `MediaBrowser.Providers/Trickplay/TrickplayProvider.cs`
  - `049361b66cef` 2023-05-24 TrickplayController return 404 if playlist doesn't exist. Minor code style/format changes (crobibero)
    https://github.com/jellyfin/jellyfin/commit/049361b66cefe7cb26364f9c39ac57abc7826752
  - `c3091b75a3ea` 2024-05-25 Backport pull request #11739 from jellyfin/release-10.9.z
    https://github.com/jellyfin/jellyfin/commit/c3091b75a3ea70df06bbfbec8b0b90a2c74f881c
  - `c56dbc1c4410` 2024-09-07 Enhance Trickplay (#11883)
    https://github.com/jellyfin/jellyfin/commit/c56dbc1c4410e1b0ec31ca901809b6f627bbb6ed

Karar:

[ KUSUR-GETIRMEDI ]

Not:

[ ]

---

### Ornek SAMPLE-14 — github.com/sharex/sharex

Commit:

- Kisa SHA: `69efe9969226`
- Yazar tarihi: 2026-08-09
- Mesaj basligi: Add Danish language support
- Baglanti: https://github.com/sharex/sharex/commit/69efe9969226c59713bffad76d8bf86efe93dd46

Degisiklik ozeti:

- Degisen toplam dosya: 29
- Degisen `.cs` dosyasi: 2
- Eklenen satir: 13209, silinen satir: 8
- Degisen `.cs` dosyalari:
  - `ShareX/Enums.cs`
  - `ShareX/LanguageHelper.cs`

Commit'in ilgili C# diff'i:

```diff
--- ShareX/Enums.cs
--- a/ShareX/Enums.cs
+++ b/ShareX/Enums.cs
@@ -56,6 +56,8 @@ public enum SupportedLanguage
         Arabic,
         [Description("Čeština (Czech)")]
         Czech,
+        [Description("Dansk (Danish)")]
+        Danish,
         [Description("Nederlands (Dutch)")]
         Dutch,
         [Description("English")]
--- ShareX/LanguageHelper.cs
--- a/ShareX/LanguageHelper.cs
+++ b/ShareX/LanguageHelper.cs
@@ -77,6 +77,9 @@ public static string GetCultureName(SupportedLanguage language)
                 case SupportedLanguage.Czech:
                     cultureName = "cs-CZ";
                     break;
+                case SupportedLanguage.Danish:
+                    cultureName = "da-DK";
+                    break;
                 case SupportedLanguage.Dutch:
                     cultureName = "nl-NL";
                     break;
```

Sonraki tarihsel olgular:

- `ShareX/Enums.cs`
  - `351e25a06c95` 2026-08-20 Refactor project files and remove obsolete code
    https://github.com/sharex/sharex/commit/351e25a06c95568f52b98abf720b3af5f0271d0d
  - `b86281f50daf` 2026-08-20 Replace recording tray bitmap with Lucide icon
    https://github.com/sharex/sharex/commit/b86281f50daf9ffc546d8209dc9f6a3d6c694def
  - `ec0d3f615d52` 2026-09-05 Add video trimmer tool and hotkey integration
    https://github.com/sharex/sharex/commit/ec0d3f615d52cf6554df7fd567d936301072b8d7
- `ShareX/LanguageHelper.cs`
  - Gozlem araliginda sonraki degisiklik yok

Karar:

[ KUSUR-GETIRMEDI ]

Not:

[ ]

---

### Ornek SAMPLE-15 — github.com/jellyfin/jellyfin

Commit:

- Kisa SHA: `7186b343bd21`
- Yazar tarihi: 2023-01-14
- Mesaj basligi: Move Formatters to Jellyfin.Api
- Baglanti: https://github.com/jellyfin/jellyfin/commit/7186b343bd21fe6b4e771b8530bd780a98a84472

Degisiklik ozeti:

- Degisen toplam dosya: 5
- Degisen `.cs` dosyasi: 5
- Eklenen satir: 5, silinen satir: 5
- Degisen `.cs` dosyalari:
  - `Jellyfin.Api/Formatters/CamelCaseJsonProfileFormatter.cs`
  - `Jellyfin.Api/Formatters/CssOutputFormatter.cs`
  - `Jellyfin.Api/Formatters/PascalCaseJsonProfileFormatter.cs`
  - `Jellyfin.Api/Formatters/XmlOutputFormatter.cs`
  - `Jellyfin.Server/Extensions/ApiServiceCollectionExtensions.cs`

Commit'in ilgili C# diff'i:

```diff
--- Jellyfin.Api/Formatters/CamelCaseJsonProfileFormatter.cs
--- a/Jellyfin.Server/Formatters/CamelCaseJsonProfileFormatter.cs
+++ b/Jellyfin.Api/Formatters/CamelCaseJsonProfileFormatter.cs
@@ -2,7 +2,7 @@ using Jellyfin.Extensions.Json;
 using Microsoft.AspNetCore.Mvc.Formatters;
 using Microsoft.Net.Http.Headers;
 
-namespace Jellyfin.Server.Formatters
+namespace Jellyfin.Api.Formatters
 {
     /// <summary>
     /// Camel Case Json Profile Formatter.
--- Jellyfin.Api/Formatters/CssOutputFormatter.cs
--- a/Jellyfin.Server/Formatters/CssOutputFormatter.cs
+++ b/Jellyfin.Api/Formatters/CssOutputFormatter.cs
@@ -3,7 +3,7 @@ using System.Threading.Tasks;
 using Microsoft.AspNetCore.Http;
 using Microsoft.AspNetCore.Mvc.Formatters;
 
-namespace Jellyfin.Server.Formatters
+namespace Jellyfin.Api.Formatters
 {
     /// <summary>
     /// Css output formatter.
--- Jellyfin.Api/Formatters/PascalCaseJsonProfileFormatter.cs
--- a/Jellyfin.Server/Formatters/PascalCaseJsonProfileFormatter.cs
+++ b/Jellyfin.Api/Formatters/PascalCaseJsonProfileFormatter.cs
@@ -3,7 +3,7 @@ using Jellyfin.Extensions.Json;
 using Microsoft.AspNetCore.Mvc.Formatters;
 using Microsoft.Net.Http.Headers;
 
-namespace Jellyfin.Server.Formatters
+namespace Jellyfin.Api.Formatters
 {
     /// <summary>
     /// Pascal Case Json Profile Formatter.
--- Jellyfin.Api/Formatters/XmlOutputFormatter.cs
--- a/Jellyfin.Server/Formatters/XmlOutputFormatter.cs
+++ b/Jellyfin.Api/Formatters/XmlOutputFormatter.cs
@@ -4,7 +4,7 @@ using System.Threading.Tasks;
 using Microsoft.AspNetCore.Http;
 using Microsoft.AspNetCore.Mvc.Formatters;
 
-namespace Jellyfin.Server.Formatters
+namespace Jellyfin.Api.Formatters
 {
     /// <summary>
     /// Xml output formatter.
--- Jellyfin.Server/Extensions/ApiServiceCollectionExtensions.cs
--- a/Jellyfin.Server/Extensions/ApiServiceCollectionExtensions.cs
+++ b/Jellyfin.Server/Extensions/ApiServiceCollectionExtensions.cs
@@ -20,13 +20,13 @@ using Jellyfin.Api.Auth.RequiresElevationPolicy;
 using Jellyfin.Api.Auth.SyncPlayAccessPolicy;
 using Jellyfin.Api.Constants;
 using Jellyfin.Api.Controllers;
+using Jellyfin.Api.Formatters;
 using Jellyfin.Api.ModelBinders;
 using Jellyfin.Data.Enums;
 using Jellyfin.Extensions.Json;
 using Jellyfin.Networking.Configuration;
 using Jellyfin.Server.Configuration;
 using Jellyfin.Server.Filters;
-using Jellyfin.Server.Formatters;
 using MediaBrowser.Common.Net;
 using MediaBrowser.Model.Entities;
 using MediaBrowser.Model.Session;
```

Sonraki tarihsel olgular:

- `Jellyfin.Api/Formatters/CamelCaseJsonProfileFormatter.cs`
  - `f5f890e68562` 2023-01-31 Migrate to file-scoped namespaces
    https://github.com/jellyfin/jellyfin/commit/f5f890e68562e55d4bed16c454c4b4305152b296
- `Jellyfin.Api/Formatters/CssOutputFormatter.cs`
  - `f5f890e68562` 2023-01-31 Migrate to file-scoped namespaces
    https://github.com/jellyfin/jellyfin/commit/f5f890e68562e55d4bed16c454c4b4305152b296
  - `97a02f580398` 2024-08-30 Remove BOM from UTF-8 files
    https://github.com/jellyfin/jellyfin/commit/97a02f58039855eb1e3e23686d4fe5bee1fbd15e
  - `d2db7004024c` 2024-10-31 Always await instead of directly returning Task
    https://github.com/jellyfin/jellyfin/commit/d2db7004024c6bbdd541a381c673f1e0b0aebfcb
- `Jellyfin.Api/Formatters/PascalCaseJsonProfileFormatter.cs`
  - `f5f890e68562` 2023-01-31 Migrate to file-scoped namespaces
    https://github.com/jellyfin/jellyfin/commit/f5f890e68562e55d4bed16c454c4b4305152b296
- `Jellyfin.Api/Formatters/XmlOutputFormatter.cs`
  - `f5f890e68562` 2023-01-31 Migrate to file-scoped namespaces
    https://github.com/jellyfin/jellyfin/commit/f5f890e68562e55d4bed16c454c4b4305152b296
  - `97a02f580398` 2024-08-30 Remove BOM from UTF-8 files
    https://github.com/jellyfin/jellyfin/commit/97a02f58039855eb1e3e23686d4fe5bee1fbd15e
  - `d2db7004024c` 2024-10-31 Always await instead of directly returning Task
    https://github.com/jellyfin/jellyfin/commit/d2db7004024c6bbdd541a381c673f1e0b0aebfcb
- `Jellyfin.Server/Extensions/ApiServiceCollectionExtensions.cs`
  - `209edd38a416` 2023-02-08 refactor: simplify authz
    https://github.com/jellyfin/jellyfin/commit/209edd38a4163a8cf4abd5e47bfe0ea1a100f351
  - `956c89dc2f5d` 2023-02-09 fix default policy
    https://github.com/jellyfin/jellyfin/commit/956c89dc2f5d6c3b8a3f362564b174fe303a9401
  - `c9aef96dba26` 2023-02-09 fix firsttimesetup
    https://github.com/jellyfin/jellyfin/commit/c9aef96dba26d29df3db9e3a1ce243be23bea772

Karar:

[ KUSUR-GETIRMEDI ]

Not:

[ ]

---

### Ornek SAMPLE-16 — github.com/app-vnext/polly

Commit:

- Kisa SHA: `bbe8807723c4`
- Yazar tarihi: 2025-06-10
- Mesaj basligi: Fix overflow in BulkheadSemaphoreFactory (#2638)
- Baglanti: https://github.com/app-vnext/polly/commit/bbe8807723c4a3405831112a739312535c4d3caa

Degisiklik ozeti:

- Degisen toplam dosya: 4
- Degisen `.cs` dosyasi: 4
- Eklenen satir: 28, silinen satir: 1
- Degisen `.cs` dosyalari:
  - `src/Polly/Bulkhead/BulkheadSemaphoreFactory.cs`
  - `test/Polly.Specs/Bulkhead/BulkheadAsyncSpecs.cs`
  - `test/Polly.Specs/Bulkhead/BulkheadSpecs.cs`
  - `test/Polly.Specs/Bulkhead/BulkheadTResultSpecs.cs`

Commit'in ilgili C# diff'i:

```diff
--- src/Polly/Bulkhead/BulkheadSemaphoreFactory.cs
--- a/src/Polly/Bulkhead/BulkheadSemaphoreFactory.cs
+++ b/src/Polly/Bulkhead/BulkheadSemaphoreFactory.cs
@@ -7,7 +7,7 @@ internal static class BulkheadSemaphoreFactory
     {
         var maxParallelizationSemaphore = new SemaphoreSlim(maxParallelization, maxParallelization);
 
-        var maxQueuingCompounded = Math.Min(maxQueueingActions + maxParallelization, int.MaxValue);
+        var maxQueuingCompounded = (int)Math.Min((long)maxQueueingActions + maxParallelization, int.MaxValue);
         var maxQueuedActionsSemaphore = new SemaphoreSlim(maxQueuingCompounded, maxQueuingCompounded);
 
         return (maxParallelizationSemaphore, maxQueuedActionsSemaphore);
--- test/Polly.Specs/Bulkhead/BulkheadAsyncSpecs.cs
--- a/test/Polly.Specs/Bulkhead/BulkheadAsyncSpecs.cs
+++ b/test/Polly.Specs/Bulkhead/BulkheadAsyncSpecs.cs
@@ -64,6 +64,15 @@ public class BulkheadAsyncSpecs(ITestOutputHelper testOutputHelper) : BulkheadSp
     }
 
     [Fact]
+    public void Should_not_throw_when_maxQueuingActions_is_int_MaxValue()
+    {
+        Action policy = () => Policy
+            .BulkheadAsync(1, int.MaxValue);
+
+        policy.ShouldNotThrow();
+    }
+
+    [Fact]
     public void Should_throw_when_onBulkheadRejected_is_null()
     {
         Action policy = () => Policy
--- test/Polly.Specs/Bulkhead/BulkheadSpecs.cs
--- a/test/Polly.Specs/Bulkhead/BulkheadSpecs.cs
+++ b/test/Polly.Specs/Bulkhead/BulkheadSpecs.cs
@@ -54,6 +54,15 @@ public class BulkheadSpecs(ITestOutputHelper testOutputHelper) : BulkheadSpecsBa
     }
 
     [Fact]
+    public void Should_not_throw_when_maxQueuingActions_is_int_MaxValue()
+    {
+        Action policy = () => Policy
+            .Bulkhead(1, int.MaxValue);
+
+        policy.ShouldNotThrow();
+    }
+
+    [Fact]
     public void Should_throw_when_maxQueuedActions_less_than_zero()
     {
         Action policy = () => Policy
--- test/Polly.Specs/Bulkhead/BulkheadTResultSpecs.cs
--- a/test/Polly.Specs/Bulkhead/BulkheadTResultSpecs.cs
+++ b/test/Polly.Specs/Bulkhead/BulkheadTResultSpecs.cs
@@ -53,6 +53,15 @@ public class BulkheadTResultSpecs(ITestOutputHelper testOutputHelper) : Bulkhead
     }
 
     [Fact]
+    public void Should_not_throw_when_maxQueuingActions_is_int_MaxValue()
+    {
+        Action policy = () => Policy
+            .Bulkhead<int>(1, int.MaxValue);
+
+        policy.ShouldNotThrow();
+    }
+
+    [Fact]
     public void Should_throw_when_maxQueuingActions_less_than_zero()
     {
         Action policy = () => Policy
```

Sonraki tarihsel olgular:

- `src/Polly/Bulkhead/BulkheadSemaphoreFactory.cs`
  - Gozlem araliginda sonraki degisiklik yok
- `test/Polly.Specs/Bulkhead/BulkheadAsyncSpecs.cs`
  - `6d5fcd6160f5` 2025-08-23 xunit v3 preparation (#2723)
    https://github.com/app-vnext/polly/commit/6d5fcd6160f5b4a2291075131ef4e485d1dacdec
- `test/Polly.Specs/Bulkhead/BulkheadSpecs.cs`
  - `6d5fcd6160f5` 2025-08-23 xunit v3 preparation (#2723)
    https://github.com/app-vnext/polly/commit/6d5fcd6160f5b4a2291075131ef4e485d1dacdec
- `test/Polly.Specs/Bulkhead/BulkheadTResultSpecs.cs`
  - `6d5fcd6160f5` 2025-08-23 xunit v3 preparation (#2723)
    https://github.com/app-vnext/polly/commit/6d5fcd6160f5b4a2291075131ef4e485d1dacdec

Karar:

[ KUSUR-GETIRMEDI ]

Not:

[ ]

---

### Ornek SAMPLE-17 — github.com/sharex/sharex

Commit:

- Kisa SHA: `7b4786cfb1cb`
- Yazar tarihi: 2026-08-19
- Mesaj basligi: Fix image effect preset submenu toggle handling
- Baglanti: https://github.com/sharex/sharex/commit/7b4786cfb1cb1e179cd2640ec99dde5f07c6acc1

Degisiklik ozeti:

- Degisen toplam dosya: 2
- Degisen `.cs` dosyasi: 2
- Eklenen satir: 27, silinen satir: 12
- Degisen `.cs` dosyalari:
  - `ShareX/Presentation/MainWindow/MainMenuBuilder.cs`
  - `ShareX/Presentation/MainWindow/MainWindow.axaml.cs`

Commit'in ilgili C# diff'i:

```diff
--- ShareX/Presentation/MainWindow/MainMenuBuilder.cs
--- a/ShareX/Presentation/MainWindow/MainMenuBuilder.cs
+++ b/ShareX/Presentation/MainWindow/MainMenuBuilder.cs
@@ -325,16 +325,7 @@ internal static IReadOnlyList<(AfterCaptureTasks Task, string Header, string Ico
 
     private IReadOnlyList<MainMenuEntry> BuildImageEffectPresetMenu()
     {
-        List<MainMenuEntry> items = new()
-        {
-            new MainMenuEntry(Strings.MainMenuBuilder_EnableAddImageEffects, LucideIcons.wand_sparkles,
-                () => Program.DefaultTaskSettings.AfterCaptureJob =
-                    Program.DefaultTaskSettings.AfterCaptureJob.Swap(AfterCaptureTasks.AddImageEffects),
-                isChecked: Program.DefaultTaskSettings.AfterCaptureJob.HasFlag(AfterCaptureTasks.AddImageEffects),
-                toggleType: MainMenuToggleType.CheckBox,
-                staysOpenOnClick: true),
-            MainMenuEntry.Separator()
-        };
+        List<MainMenuEntry> items = new();
         List<ImageEffectsLib.ImageEffectPreset>? presets = Program.DefaultTaskSettings.ImageSettings.ImageEffectPresets;
 
         if (presets != null)
@@ -353,7 +344,7 @@ private IReadOnlyList<MainMenuEntry> BuildImageEffectPresetMenu()
             }
         }
 
-        if (items.Count == 2)
+        if (items.Count == 0)
         {
             items.Add(new MainMenuEntry(Strings.MainMenuBuilder_NoImageEffectPresets, LucideIcons.wand_sparkles, isEnabled: false));
         }
--- ShareX/Presentation/MainWindow/MainWindow.axaml.cs
--- a/ShareX/Presentation/MainWindow/MainWindow.axaml.cs
+++ b/ShareX/Presentation/MainWindow/MainWindow.axaml.cs
@@ -472,12 +472,36 @@ private IEnumerable<Control> BuildMenuControls(IEnumerable<MainMenuEntry> entrie
             if (entry.CreateChildren != null)
             {
                 AddLazySubmenu(item, entry.CreateChildren, menu);
+
+                if (entry.ExecuteAsync != null)
+                {
+                    item.AddHandler(PointerPressedEvent, (_, e) =>
+                    {
+                        PointerPoint point = e.GetCurrentPoint(item);
+                        if (point.Properties.PointerUpdateKind == PointerUpdateKind.LeftButtonPressed &&
+                            new Rect(item.Bounds.Size).Contains(point.Position))
+                        {
+                            item.IsChecked = item.ToggleType switch
+                            {
+                                MenuItemToggleType.CheckBox => !item.IsChecked,
+                                MenuItemToggleType.Radio => true,
+                                _ => item.IsChecked
+                            };
+                            item.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent, item));
+                        }
+                    }, RoutingStrategies.Tunnel, handledEventsToo: true);
+                }
             }
 
             if (entry.ExecuteAsync != null)
             {
-                item.Click += (_, _) =>
+                item.Click += (_, e) =>
                 {
+                    if (!ReferenceEquals(e.Source, item))
+                    {
+                        return;
+                    }
+
                     if (!entry.StaysOpenOnClick)
                     {
                         // Modal WinForms dialogs and capture overlays cannot be opened while
```

Sonraki tarihsel olgular:

- `ShareX/Presentation/MainWindow/MainMenuBuilder.cs`
  - `af34a382391c` 2026-08-19 Remove icons from image effect preset menu items
    https://github.com/sharex/sharex/commit/af34a382391cdc6980987f78708e377b88f7be5d
  - `351e25a06c95` 2026-08-20 Refactor project files and remove obsolete code
    https://github.com/sharex/sharex/commit/351e25a06c95568f52b98abf720b3af5f0271d0d
  - `1f7c16c45ce3` 2026-08-27 Keep destination menus open after selection
    https://github.com/sharex/sharex/commit/1f7c16c45ce320332d2797b8268e4f8db2ae5ad4
- `ShareX/Presentation/MainWindow/MainWindow.axaml.cs`
  - `d672c1e6619f` 2026-08-27 Refresh destination menu headers after settings changes
    https://github.com/sharex/sharex/commit/d672c1e6619f8562bbd4062e4c548fff5be32876
  - `57828e616c79` 2026-08-28 Highlight destination values in menus
    https://github.com/sharex/sharex/commit/57828e616c79c195e2d6529bee6fd8e6aa3af381
  - `0e9c8e86ea01` 2026-08-29 Emphasize checked persistent menu items
    https://github.com/sharex/sharex/commit/0e9c8e86ea013c530c98a75c0f1a8d904ed51ef0

Karar:

[ KUSUR-GETIRMEDI ]

Not:

[ ]

---

### Ornek SAMPLE-18 — github.com/sharex/sharex

Commit:

- Kisa SHA: `f15c42028858`
- Yazar tarihi: 2026-05-12
- Mesaj basligi: Update ShowFileMenu logic to always display the file menu in the image editor
- Baglanti: https://github.com/sharex/sharex/commit/f15c42028858356605379fa1fbad3e559e1715b7

Degisiklik ozeti:

- Degisen toplam dosya: 1
- Degisen `.cs` dosyasi: 1
- Eklenen satir: 2, silinen satir: 2
- Degisen `.cs` dosyalari:
  - `ShareX.ImageEditor/Hosting/AvaloniaIntegration.cs`

Commit'in ilgili C# diff'i:

```diff
--- ShareX.ImageEditor/Hosting/AvaloniaIntegration.cs
--- a/ShareX.ImageEditor/Hosting/AvaloniaIntegration.cs
+++ b/ShareX.ImageEditor/Hosting/AvaloniaIntegration.cs
@@ -25,13 +25,13 @@
 
 using Avalonia;
 using Avalonia.Controls;
-using SkiaSharp;
 using Avalonia.Controls.ApplicationLifetimes;
 using Avalonia.Themes.Fluent;
 using Avalonia.Threading;
 using ShareX.ImageEditor.Hosting.Diagnostics;
 using ShareX.ImageEditor.Presentation.ViewModels;
 using ShareX.ImageEditor.Presentation.Views;
+using SkiaSharp;
 
 namespace ShareX.ImageEditor.Hosting
 {
@@ -238,7 +238,7 @@ public static void ShowEditor(Stream imageStream)
                     vm.ImageFilePath = filePath;
                 }
 
-                vm.ShowFileMenu = !taskMode;
+                vm.ShowFileMenu = true;
                 vm.ShowTaskButtons = true;
                 vm.UseContinueWorkflow = taskMode;
                 vm.ShowBottomToolbar = true;
```

Sonraki tarihsel olgular:

- `ShareX.ImageEditor/Hosting/AvaloniaIntegration.cs`
  - `d96ee5513c57` 2026-05-18 fixed #8639: Add Print functionality with UI integration and command binding
    https://github.com/sharex/sharex/commit/d96ee5513c578b4f5a4cf0e1dd3e6540e44571ea
  - `2729b695481c` 2026-05-20 Add ShowOptionsButton property and update UI bindings in EditorView
    https://github.com/sharex/sharex/commit/2729b695481cdf33625af09afe3d0d3a406f276d
  - `4f2aa8c0947d` 2026-05-27 Enhance notifications: implement notification system with customizable messages and icons
    https://github.com/sharex/sharex/commit/4f2aa8c0947da7b0a0cd6eb1b083dbac7d00bf3f

Karar:

[ KUSUR-GETIRMEDI ]

Not:

[ ]

---

### Ornek SAMPLE-19 — github.com/sharex/sharex

Commit:

- Kisa SHA: `9de1953e6d68`
- Yazar tarihi: 2022-08-17
- Mesaj basligi: Refactor CutOutBitmapMiddle
- Baglanti: https://github.com/sharex/sharex/commit/9de1953e6d686f4a8a36e3c58ca9e39b0a0c64dd

Degisiklik ozeti:

- Degisen toplam dosya: 2
- Degisen `.cs` dosyasi: 2
- Eklenen satir: 46, silinen satir: 98
- Degisen `.cs` dosyalari:
  - `ShareX.HelpersLib/Helpers/ImageHelpers.cs`
  - `ShareX.ScreenCaptureLib/Shapes/ShapeManager.cs`

Commit'in ilgili C# diff'i:

```diff
--- ShareX.HelpersLib/Helpers/ImageHelpers.cs
--- a/ShareX.HelpersLib/Helpers/ImageHelpers.cs
+++ b/ShareX.HelpersLib/Helpers/ImageHelpers.cs
@@ -223,124 +223,72 @@ public static Bitmap CropBitmap(Bitmap bmp, Rectangle rect)
             return null;
         }
 
-        public static Bitmap CutOutBitmapMiddleHorizontal(Bitmap bmp, int x, int width, CutOutEffectType effectType, int effectSize)
+        private static Bitmap ApplyCutOutEffect(Bitmap bmp, AnchorStyles effectEdge, CutOutEffectType effectType, int effectSize)
         {
-            if (bmp != null && width > 0)
+            switch (effectType)
             {
-                Bitmap leftPart = null, rightPart = null;
-                if (x > 0)
-                {
-                    leftPart = CropBitmap(bmp, new Rectangle(0, 0, Math.Min(x, bmp.Width), bmp.Height));
-                    switch (effectType)
-                    {
-                        case CutOutEffectType.None:
-                            break;
-                        case CutOutEffectType.ZigZag:
-                            break;
-                        case CutOutEffectType.TornEdge:
-                            leftPart = TornEdges(leftPart, effectSize, effectSize * 2, AnchorStyles.Right, false);
-                            break;
-                        case CutOutEffectType.Wave:
-                            break;
-                        case CutOutEffectType.Gradient:
-                            break;
-                    }
-                }
-                if (x + width < bmp.Width)
-                {
-                    int x2 = Math.Max(x + width, 0);
-                    rightPart = CropBitmap(bmp, new Rectangle(x2, 0, bmp.Width - x2, bmp.Height));
-                    switch (effectType)
-                    {
-                        case CutOutEffectType.None:
-                            break;
-                        case CutOutEffectType.ZigZag:
-                            break;
-                        case CutOutEffectType.TornEdge:
-                            rightPart = TornEdges(rightPart, effectSize, effectSize * 2, AnchorStyles.Left, false);
-                            break;
-                        case CutOutEffectType.Wave:
-                            break;
-                        case CutOutEffectType.Gradient:
-                            break;
-                    }
-                }
+                case CutOutEffectType.None:
+                    return bmp;
 
-                if (leftPart != null && rightPart != null)
-                {
-                    return CombineImages(new List<Bitmap> { leftPart, rightPart }, Orientation.Horizontal);
-                }
-                else if (leftPart != null)
-                {
-                    return leftPart;
-                }
-                else if (rightPart != null)
-                {
-                    return rightPart;
-                }
+                case CutOutEffectType.ZigZag:
+                    return bmp;
+
+                case CutOutEffectType.TornEdge:
+                    return TornEdges(bmp, effectSize, effectSize * 2, effectEdge, false);
+
+                case CutOutEffectType.Wave:
+                    return bmp;
+
+                case CutOutEffectType.Gradient:
+                    return bmp;
             }
 
-            return null;
+            throw new NotImplementedException(); // should not be reachable
         }
 
-        public static Bitmap CutOutBitmapMiddleVertical(Bitmap bmp, int y, int height, CutOutEffectType effectType, int effectSize)
+        public static Bitmap CutOutBitmapMiddle(Bitmap bmp, Orientation orientation, int start, int size, CutOutEffectType effectType, int effectSize)
         {
-            if (bmp != null && height > 0)
+            if (bmp != null && size > 0)
             {
-                Bitmap topPart = null, bottomPart = null;
-                if (y > 0)
+                Bitmap firstPart = null, secondPart = null;
+
+                if (start > 0)
                 {
-                    topPart = CropBitmap(bmp, new Rectangle(0, 0, bmp.Width, Math.Min(y, bmp.Height)));
-                    switch (effectType)
-                    {
-                        case CutOutEffectType.None:
-                            break;
-                        case CutOutEffectType.ZigZag:
-                            break;
-                        case CutOutEffectType.TornEdge:
-                            topPart = TornEdges(topPart, effectSize, effectSize * 2, AnchorStyles.Bottom, false);
-                            break;
-                        case CutOutEffectType.Wave:
-                            break;
-                        case CutOutEffectType.Gradient:
-                            break;
-                    }
+                    Rectangle r = orientation == Orientation.Horizontal
+                        ? new Rectangle(0, 0, Math.Min(start, bmp.Width), bmp.Height)
+                        : new Rectangle(0, 0, bmp.Width, Math.Min(start, bmp.Height));
+                    firstPart = CropBitmap(bmp, r);
+                    AnchorStyles effectEdge = orientation == Orientation.Horizontal ? AnchorStyles.Right : AnchorStyles.Bottom;
+                    firstPart = ApplyCutOutEffect(firstPart, effectEdge, effectType, effectSize);
                 }
-                if (y + height < bmp.Height)
+
+                int cutDimension = orientation == Orientation.Horizontal ? bmp.Width : bmp.Height;
+                if (start + size < cutDimension)
                 {
-                    int y2 = Math.Max(y + height, 0);
-                    bottomPart = CropBitmap(bmp, new Rectangle(0, y2, bmp.Width, bmp.Height - y2));
-                    switch (effectType)
-                    {
-                        case CutOutEffectType.None:
-                            break;
-                        case CutOutEffectType.ZigZag:
-                            break;
-                        case CutOutEffectType.TornEdge:
-                            bottomPart = TornEdges(bottomPart, effectSize, effectSize * 2, AnchorStyles.Top, false);
-                            break;
-                        case CutOutEffectType.Wave:
-                            break;
-                        case CutOutEffectType.Gradient:
-                            break;
-                    }
+                    int end = Math.Max(start + size, 0);
+                    Rectangle r = orientation == Orientation.Horizontal
+                        ? new Rectangle(end, 0, bmp.Width - end, bmp.Height)
+                        : new Rectangle(0, end, bmp.Width, bmp.Height - end);
+                    secondPart = CropBitmap(bmp, r);
+                    AnchorStyles effectEdge = orientation == Orientation.Horizontal ? AnchorStyles.Left : AnchorStyles.Top;
+                    secondPart = ApplyCutOutEffect(secondPart, effectEdge, effectType, effectSize);
                 }
 
-                if (topPart != null && bottomPart != null)
+                if (firstPart != null && secondPart != null)
                 {
-                    return CombineImages(new List<Bitmap> { topPart, bottomPart }, Orientation.Vertical);
+                    return CombineImages(new List<Bitmap> { firstPart, secondPart }, orientation);
                 }
-                else if (topPart != null)
+                else if (firstPart != null)
                 {
-                    return topPart;
+                    return firstPart;
                 }
-                else if (bottomPart != null)
```

Kirpildi; tam diff: https://github.com/sharex/sharex/commit/9de1953e6d686f4a8a36e3c58ca9e39b0a0c64dd

Sonraki tarihsel olgular:

- `ShareX.HelpersLib/Helpers/ImageHelpers.cs`
  - `a342f8d2b6c4` 2022-08-17 Add Wave cut effect
    https://github.com/sharex/sharex/commit/a342f8d2b6c4437e4c6c3d7a9d55df4cf2f02213
  - `e0423cf7c000` 2022-08-17 Implement ZigZag effect via a flag for TornEdges function
    https://github.com/sharex/sharex/commit/e0423cf7c00069990d15d207e17525bfdb341bd8
  - `8ca64af5961a` 2022-08-17 Remove the Gradient effect type for now
    https://github.com/sharex/sharex/commit/8ca64af5961a051f001b6dafe86c468fb9c2c599
- `ShareX.ScreenCaptureLib/Shapes/ShapeManager.cs`
  - `a342f8d2b6c4` 2022-08-17 Add Wave cut effect
    https://github.com/sharex/sharex/commit/a342f8d2b6c4437e4c6c3d7a9d55df4cf2f02213
  - `e0423cf7c000` 2022-08-17 Implement ZigZag effect via a flag for TornEdges function
    https://github.com/sharex/sharex/commit/e0423cf7c00069990d15d207e17525bfdb341bd8
  - `b784ebe0c7fa` 2022-08-17 Add GUI configuration for cut out tool
    https://github.com/sharex/sharex/commit/b784ebe0c7fafec0349cbaf7d4fabc83f92748bd

Karar:

[ KUSUR-GETIRMEDI ]

Not:

[ ]

---

### Ornek SAMPLE-20 — github.com/app-vnext/polly

Commit:

- Kisa SHA: `651508bfced1`
- Yazar tarihi: 2025-08-22
- Mesaj basligi: Fix flaky mutants
- Baglanti: https://github.com/app-vnext/polly/commit/651508bfced1c365b8dd047e62657aac7e64df20

Degisiklik ozeti:

- Degisen toplam dosya: 1
- Degisen `.cs` dosyasi: 1
- Eklenen satir: 3, silinen satir: 0
- Degisen `.cs` dosyalari:
  - `src/Polly.Core/CircuitBreaker/Controller/CircuitStateController.cs`

Commit'in ilgili C# diff'i:

```diff
--- src/Polly.Core/CircuitBreaker/Controller/CircuitStateController.cs
--- a/src/Polly.Core/CircuitBreaker/Controller/CircuitStateController.cs
+++ b/src/Polly.Core/CircuitBreaker/Controller/CircuitStateController.cs
@@ -162,6 +162,8 @@ internal sealed class CircuitStateController<T> : IDisposable
         }
 
         task = ExecuteScheduledTaskAsync(task, context);
+
+        // stryker disable once all : no means to test this
         if (!task.IsCompleted)
         {
             return WaitHalfOpenTask(task, context.ContinueOnCapturedContext);
@@ -243,6 +245,7 @@ internal sealed class CircuitStateController<T> : IDisposable
 
     internal static Task ExecuteScheduledTaskAsync(Task task, ResilienceContext context)
     {
+        // stryker disable once all : no means to test this
         if (context.IsSynchronous && !task.IsCompleted)
         {
 #pragma warning disable CA1849 // Call async methods when in an async method
```

Sonraki tarihsel olgular:

- `src/Polly.Core/CircuitBreaker/Controller/CircuitStateController.cs`
  - Gozlem araliginda sonraki degisiklik yok

Karar:

[ BAKILMADI ]

Not:

[ ]

---

### Ornek SAMPLE-21 — github.com/app-vnext/polly

Commit:

- Kisa SHA: `b4d43e27c5d2`
- Yazar tarihi: 2025-01-18
- Mesaj basligi: Use Shouldly
- Baglanti: https://github.com/app-vnext/polly/commit/b4d43e27c5d265c5b6704e63a4e126390e8f4f9d

Degisiklik ozeti:

- Degisen toplam dosya: 135
- Degisen `.cs` dosyasi: 134
- Eklenen satir: 1592, silinen satir: 1762
- Degisen `.cs` dosyalari:
  - `test/Polly.Core.Tests/CircuitBreaker/BreakDurationGeneratorArgumentsTests.cs`
  - `test/Polly.Core.Tests/CircuitBreaker/BrokenCircuitExceptionTests.cs`
  - `test/Polly.Core.Tests/CircuitBreaker/CircuitBreakerManualControlTests.cs`
  - `test/Polly.Core.Tests/CircuitBreaker/CircuitBreakerOptionsTests.cs`
  - `test/Polly.Core.Tests/CircuitBreaker/CircuitBreakerPredicateArgumentsTests.cs`
  - `test/Polly.Core.Tests/CircuitBreaker/CircuitBreakerResiliencePipelineBuilderTests.cs`
  - `test/Polly.Core.Tests/CircuitBreaker/CircuitBreakerResilienceStrategyTests.cs`
  - `test/Polly.Core.Tests/CircuitBreaker/CircuitBreakerStateProviderTests.cs`
  - `test/Polly.Core.Tests/CircuitBreaker/Controller/AdvancedCircuitBehaviorTests.cs`
  - `test/Polly.Core.Tests/CircuitBreaker/Controller/CircuitStateControllerTests.cs`
  - `test/Polly.Core.Tests/CircuitBreaker/Controller/ScheduledTaskExecutorTests.cs`
  - `test/Polly.Core.Tests/CircuitBreaker/Health/HealthMetricsTests.cs`
  - `test/Polly.Core.Tests/CircuitBreaker/Health/RollingHealthMetricsTests.cs`
  - `test/Polly.Core.Tests/CircuitBreaker/Health/SingleHealthMetricsTests.cs`
  - `test/Polly.Core.Tests/CircuitBreaker/IsolatedCircuitExceptionTests.cs`
  - `test/Polly.Core.Tests/CircuitBreaker/OnCircuitClosedArgumentsTests.cs`
  - `test/Polly.Core.Tests/CircuitBreaker/OnCircuitHalfOpenedArgumentsTests.cs`
  - `test/Polly.Core.Tests/CircuitBreaker/OnCircuitOpenedArgumentsTests.cs`
  - `test/Polly.Core.Tests/ExecutionRejectedExceptionTests.cs`
  - `test/Polly.Core.Tests/Fallback/FallbackHandlerTests.cs`
  - `test/Polly.Core.Tests/Fallback/FallbackResiliencePipelineBuilderExtensionsTests.cs`
  - `test/Polly.Core.Tests/Fallback/FallbackResilienceStrategyTests.cs`
  - `test/Polly.Core.Tests/Fallback/FallbackStrategyOptionsTests.cs`
  - `test/Polly.Core.Tests/GenericResiliencePipelineBuilderTests.cs`
  - `test/Polly.Core.Tests/Hedging/Controller/HedgingControllerTests.cs`
  - `test/Polly.Core.Tests/Hedging/Controller/HedgingExecutionContextTests.cs`
  - `test/Polly.Core.Tests/Hedging/Controller/TaskExecutionTests.cs`
  - `test/Polly.Core.Tests/Hedging/HedgingActionGeneratorArgumentsTests.cs`
  - `test/Polly.Core.Tests/Hedging/HedgingDelayGeneratorArgumentsTests.cs`
  - `test/Polly.Core.Tests/Hedging/HedgingHandlerTests.cs`
  - `test/Polly.Core.Tests/Hedging/HedgingPredicateArgumentsTests.cs`
  - `test/Polly.Core.Tests/Hedging/HedgingResiliencePipelineBuilderExtensionsTests.cs`
  - `test/Polly.Core.Tests/Hedging/HedgingResilienceStrategyTests.cs`
  - `test/Polly.Core.Tests/Hedging/HedgingStrategyOptionsTests.cs`
  - `test/Polly.Core.Tests/Hedging/OnHedgingArgumentsTests.cs`
  - `test/Polly.Core.Tests/Issues/IssuesTests.CircuitBreakerStateRegistry_1828.cs`
  - `test/Polly.Core.Tests/Issues/IssuesTests.CircuitBreakerStateSharing_959.cs`
  - `test/Polly.Core.Tests/Issues/IssuesTests.FlowingContext_849.cs`
  - `test/Polly.Core.Tests/Issues/IssuesTests.HandleMultipleResults_898.cs`
  - `test/Polly.Core.Tests/Issues/IssuesTests.InfiniteRetry_2163.cs`
  - `test/Polly.Core.Tests/OutcomeTests.cs`
  - `test/Polly.Core.Tests/PredicateBuilderTests.cs`
  - `test/Polly.Core.Tests/PredicateResultTests.cs`
  - `test/Polly.Core.Tests/Registry/ConfigureBuilderContextTests.cs`
  - `test/Polly.Core.Tests/Registry/ResiliencePipelineProviderTests.cs`
  - `test/Polly.Core.Tests/Registry/ResiliencePipelineRegistryOptionsTests.cs`
  - `test/Polly.Core.Tests/Registry/ResiliencePipelineRegistryTests.cs`
  - `test/Polly.Core.Tests/ResilienceContextPoolTests.cs`
  - `test/Polly.Core.Tests/ResilienceContextTests.cs`
  - `test/Polly.Core.Tests/ResiliencePipelineBuilderTests.cs`
  - `test/Polly.Core.Tests/ResiliencePipelineTTests.Async.cs`
  - `test/Polly.Core.Tests/ResiliencePipelineTTests.Sync.cs`
  - `test/Polly.Core.Tests/ResiliencePipelineTests.Async.cs`
  - `test/Polly.Core.Tests/ResiliencePipelineTests.AsyncT.cs`
  - `test/Polly.Core.Tests/ResiliencePipelineTests.Sync.cs`
  - `test/Polly.Core.Tests/ResiliencePipelineTests.SyncT.cs`
  - `test/Polly.Core.Tests/ResiliencePipelineTests.cs`
  - `test/Polly.Core.Tests/ResiliencePropertiesTests.cs`
  - `test/Polly.Core.Tests/ResiliencePropertyKeyTests.cs`
  - `test/Polly.Core.Tests/ResilienceStrategyOptionsTests.cs`
  - `test/Polly.Core.Tests/Retry/OnRetryArgumentsTests.cs`
  - `test/Polly.Core.Tests/Retry/RetryConstantsTests.cs`
  - `test/Polly.Core.Tests/Retry/RetryDelayGeneratorArgumentsTests.cs`
  - `test/Polly.Core.Tests/Retry/RetryHelperTests.cs`
  - `test/Polly.Core.Tests/Retry/RetryResiliencePipelineBuilderExtensionsTests.cs`
  - `test/Polly.Core.Tests/Retry/RetryResilienceStrategyTests.cs`
  - `test/Polly.Core.Tests/Retry/RetryStrategyOptionsTests.cs`
  - `test/Polly.Core.Tests/Retry/ShouldRetryArgumentsTests.cs`
  - `test/Polly.Core.Tests/Simmy/Behavior/BehaviorGeneratorArgumentsTests.cs`
  - `test/Polly.Core.Tests/Simmy/Behavior/ChaosBehaviorConstantsTests.cs`
  - `test/Polly.Core.Tests/Simmy/Behavior/ChaosBehaviorPipelineBuilderExtensionsTests.cs`
  - `test/Polly.Core.Tests/Simmy/Behavior/ChaosBehaviorStrategyOptionsTests.cs`
  - `test/Polly.Core.Tests/Simmy/Behavior/ChaosBehaviorStrategyTests.cs`
  - `test/Polly.Core.Tests/Simmy/Behavior/OnBehaviorInjectedArgumentsTests.cs`
  - `test/Polly.Core.Tests/Simmy/ChaosStrategyConstantsTests.cs`
  - `test/Polly.Core.Tests/Simmy/ChaosStrategyOptionsTTests.cs`
  - `test/Polly.Core.Tests/Simmy/ChaosStrategyOptionsTests.cs`
  - `test/Polly.Core.Tests/Simmy/ChaosStrategyTTests.cs`
  - `test/Polly.Core.Tests/Simmy/ChaosStrategyTests.cs`
  - `test/Polly.Core.Tests/Simmy/EnabledGeneratorArgumentsTests.cs`
  - `test/Polly.Core.Tests/Simmy/Fault/ChaosFaultConstantsTests.cs`
  - `test/Polly.Core.Tests/Simmy/Fault/ChaosFaultPipelineBuilderExtensionsTests.cs`
  - `test/Polly.Core.Tests/Simmy/Fault/ChaosFaultStrategyOptionsTests.cs`
  - `test/Polly.Core.Tests/Simmy/Fault/ChaosFaultStrategyTests.cs`
  - `test/Polly.Core.Tests/Simmy/Fault/FaultGeneratorArgumentsTests.cs`
  - `test/Polly.Core.Tests/Simmy/Fault/FaultGeneratorTests.cs`
  - `test/Polly.Core.Tests/Simmy/Fault/OnFaultInjectedArgumentsTests.cs`
  - `test/Polly.Core.Tests/Simmy/InjectionRateGeneratorArgumentsTests.cs`
  - `test/Polly.Core.Tests/Simmy/Latency/ChaosLatencyConstantsTests.cs`
  - `test/Polly.Core.Tests/Simmy/Latency/ChaosLatencyPipelineBuilderExtensionsTests.cs`
  - `test/Polly.Core.Tests/Simmy/Latency/ChaosLatencyStrategyOptionsTests.cs`
  - `test/Polly.Core.Tests/Simmy/Latency/ChaosLatencyStrategyTests.cs`
  - `test/Polly.Core.Tests/Simmy/Latency/LatencyGeneratorArgumentsTests.cs`
  - `test/Polly.Core.Tests/Simmy/Latency/OnLatencyInjectedArgumentsTests.cs`
  - `test/Polly.Core.Tests/Simmy/Outcomes/ChaosOutcomeConstantsTests.cs`
  - `test/Polly.Core.Tests/Simmy/Outcomes/ChaosOutcomePipelineBuilderExtensionsTests.cs`
  - `test/Polly.Core.Tests/Simmy/Outcomes/ChaosOutcomeStrategyOptionsTests.cs`
  - `test/Polly.Core.Tests/Simmy/Outcomes/ChaosOutcomeStrategyTests.cs`
  - `test/Polly.Core.Tests/Simmy/Outcomes/OnOutcomeInjectedArgumentsTests.cs`
  - `test/Polly.Core.Tests/Simmy/Outcomes/OutcomeGeneratorArgumentsTests.cs`
  - `test/Polly.Core.Tests/Simmy/Outcomes/OutcomeGeneratorTests.cs`
  - `test/Polly.Core.Tests/Simmy/TestChaosStrategyOptions.TResult.cs`
  - `test/Polly.Core.Tests/Simmy/TestChaosStrategyOptions.cs`
  - `test/Polly.Core.Tests/Simmy/Utils/GeneratorHelperTests.cs`
  - `test/Polly.Core.Tests/StrategyBuilderContextTests.cs`
  - `test/Polly.Core.Tests/Telemetry/ExecutionAttemptArgumentsTests.cs`
  - `test/Polly.Core.Tests/Telemetry/PipelineExecutedArgumentsTests.cs`
  - `test/Polly.Core.Tests/Telemetry/ResilienceEventTests.cs`
  - `test/Polly.Core.Tests/Telemetry/ResilienceStrategyTelemetryTests.cs`
  - `test/Polly.Core.Tests/Telemetry/TelemetryEventArgumentsTests.cs`
  - `test/Polly.Core.Tests/Telemetry/TelemetryUtilTests.cs`
  - `test/Polly.Core.Tests/Timeout/TimeoutConstantsTests.cs`
  - `test/Polly.Core.Tests/Timeout/TimeoutRejectedExceptionTests.cs`
  - `test/Polly.Core.Tests/Timeout/TimeoutResiliencePipelineBuilderExtensionsTests.cs`
  - `test/Polly.Core.Tests/Timeout/TimeoutResilienceStrategyTests.cs`
  - `test/Polly.Core.Tests/Timeout/TimeoutStrategyOptionsTests.cs`
  - `test/Polly.Core.Tests/Timeout/TimeoutUtilTests.cs`
  - `test/Polly.Core.Tests/Utils/CancellationTokenSourcePoolTests.cs`
  - `test/Polly.Core.Tests/Utils/DisposeHelperTests.cs`
  - `test/Polly.Core.Tests/Utils/LegacySupportTests.cs`
  - `test/Polly.Core.Tests/Utils/Pipeline/BridgePipelineComponentTests.cs`
  - `test/Polly.Core.Tests/Utils/Pipeline/ComponentWithDisposeCallbacksTests.cs`
  - `test/Polly.Core.Tests/Utils/Pipeline/CompositePipelineComponentTests.cs`
  - `test/Polly.Core.Tests/Utils/Pipeline/DelegatingComponentTests.cs`
  - `test/Polly.Core.Tests/Utils/Pipeline/ExecutionTrackingComponentTests.cs`
  - `test/Polly.Core.Tests/Utils/Pipeline/PipelineComponentFactoryTests.cs`
  - `test/Polly.Core.Tests/Utils/Pipeline/PipelineComponentTests.cs`
  - `test/Polly.Core.Tests/Utils/Pipeline/ReloadablePipelineComponentTests.cs`
  - `test/Polly.Core.Tests/Utils/RandomUtilTests.cs`
  - `test/Polly.Core.Tests/Utils/StrategyHelperTests.cs`
  - `test/Polly.Core.Tests/Utils/TaskHelperTests.cs`
  - `test/Polly.Core.Tests/Utils/TimeProviderExtensionsTests.cs`
  - `test/Polly.Core.Tests/Utils/TypeNameFormatterTests.cs`
  - `test/Polly.Core.Tests/Utils/ValidationHelperTests.cs`

Commit'in ilgili C# diff'i:

```diff
--- test/Polly.Core.Tests/CircuitBreaker/BreakDurationGeneratorArgumentsTests.cs
--- a/test/Polly.Core.Tests/CircuitBreaker/BreakDurationGeneratorArgumentsTests.cs
+++ b/test/Polly.Core.Tests/CircuitBreaker/BreakDurationGeneratorArgumentsTests.cs
@@ -14,9 +14,9 @@ public class BreakDurationGeneratorArgumentsTests
 
         var args = new BreakDurationGeneratorArguments(expectedFailureRate, failureCount, context);
 
-        args.FailureRate.Should().Be(expectedFailureRate);
-        args.FailureCount.Should().Be(failureCount);
-        args.Context.Should().Be(context);
+        args.FailureRate.ShouldBe(expectedFailureRate);
+        args.FailureCount.ShouldBe(failureCount);
+        args.Context.ShouldBe(context);
     }
 
     [Fact]
@@ -28,9 +28,9 @@ public class BreakDurationGeneratorArgumentsTests
 
         var args = new BreakDurationGeneratorArguments(expectedFailureRate, failureCount, context, 99);
 
-        args.FailureRate.Should().Be(expectedFailureRate);
-        args.FailureCount.Should().Be(failureCount);
-        args.Context.Should().Be(context);
-        args.HalfOpenAttempts.Should().Be(99);
+        args.FailureRate.ShouldBe(expectedFailureRate);
+        args.FailureCount.ShouldBe(failureCount);
+        args.Context.ShouldBe(context);
+        args.HalfOpenAttempts.ShouldBe(99);
     }
 }
--- test/Polly.Core.Tests/CircuitBreaker/BrokenCircuitExceptionTests.cs
--- a/test/Polly.Core.Tests/CircuitBreaker/BrokenCircuitExceptionTests.cs
+++ b/test/Polly.Core.Tests/CircuitBreaker/BrokenCircuitExceptionTests.cs
@@ -8,50 +8,50 @@ public class BrokenCircuitExceptionTests
     public void Ctor_Default_Ok()
     {
         var exception = new BrokenCircuitException();
-        exception.Message.Should().Be("The circuit is now open and is not allowing calls.");
-        exception.RetryAfter.Should().BeNull();
+        exception.Message.ShouldBe("The circuit is now open and is not allowing calls.");
+        exception.RetryAfter.ShouldBeNull();
     }
 
     [Fact]
     public void Ctor_Message_Ok()
     {
         var exception = new BrokenCircuitException(TestMessage);
-        exception.Message.Should().Be(TestMessage);
-        exception.RetryAfter.Should().BeNull();
+        exception.Message.ShouldBe(TestMessage);
+        exception.RetryAfter.ShouldBeNull();
     }
 
     [Fact]
     public void Ctor_RetryAfter_Ok()
     {
         var exception = new BrokenCircuitException(TestRetryAfter);
-        exception.Message.Should().Be($"The circuit is now open and is not allowing calls. It can be retried after '{TestRetryAfter}'.");
-        exception.RetryAfter.Should().Be(TestRetryAfter);
+        exception.Message.ShouldBe($"The circuit is now open and is not allowing calls. It can be retried after '{TestRetryAfter}'.");
+        exception.RetryAfter.ShouldBe(TestRetryAfter);
     }
 
     [Fact]
     public void Ctor_Message_RetryAfter_Ok()
     {
         var exception = new BrokenCircuitException(TestMessage, TestRetryAfter);
-        exception.Message.Should().Be(TestMessage);
-        exception.RetryAfter.Should().Be(TestRetryAfter);
+        exception.Message.ShouldBe(TestMessage);
+        exception.RetryAfter.ShouldBe(TestRetryAfter);
     }
 
     [Fact]
     public void Ctor_Message_InnerException_Ok()
     {
         var exception = new BrokenCircuitException(TestMessage, new InvalidOperationException());
-        exception.Message.Should().Be(TestMessage);
-        exception.InnerException.Should().BeOfType<InvalidOperationException>();
-        exception.RetryAfter.Should().BeNull();
+        exception.Message.ShouldBe(TestMessage);
+        exception.InnerException.ShouldBeOfType<InvalidOperationException>();
+        exception.RetryAfter.ShouldBeNull();
     }
 
     [Fact]
     public void Ctor_Message_RetryAfter_InnerException_Ok()
     {
         var exception = new BrokenCircuitException(TestMessage, TestRetryAfter, new InvalidOperationException());
-        exception.Message.Should().Be(TestMessage);
-        exception.InnerException.Should().BeOfType<InvalidOperationException>();
-        exception.RetryAfter.Should().Be(TestRetryAfter);
+        exception.Message.ShouldBe(TestMessage);
+        exception.InnerException.ShouldBeOfType<InvalidOperationException>();
+        exception.RetryAfter.ShouldBe(TestRetryAfter);
     }
 
 #if NETFRAMEWORK
@@ -60,10 +60,10 @@ public class BrokenCircuitExceptionTests
     {
         var exception = new BrokenCircuitException(TestMessage, TestRetryAfter, new InvalidOperationException());
         BrokenCircuitException roundtripResult = BinarySerializationUtil.SerializeAndDeserializeException(exception);
-        roundtripResult.Should().NotBeNull();
-        roundtripResult.Message.Should().Be(TestMessage);
-        roundtripResult.InnerException.Should().BeOfType<InvalidOperationException>();
-        roundtripResult.RetryAfter.Should().Be(TestRetryAfter);
+        roundtripResult.ShouldNotBeNull();
+        roundtripResult.Message.ShouldBe(TestMessage);
+        roundtripResult.InnerException.ShouldBeOfType<InvalidOperationException>();
+        roundtripResult.RetryAfter.ShouldBe(TestRetryAfter);
     }
 
     [Fact]
@@ -71,10 +71,10 @@ public class BrokenCircuitExceptionTests
     {
         var exception = new BrokenCircuitException(TestMessage, new InvalidOperationException());
         BrokenCircuitException roundtripResult = BinarySerializationUtil.SerializeAndDeserializeException(exception);
-        roundtripResult.Should().NotBeNull();
-        roundtripResult.Message.Should().Be(TestMessage);
-        roundtripResult.InnerException.Should().BeOfType<InvalidOperationException>();
-        roundtripResult.RetryAfter.Should().BeNull();
+        roundtripResult.ShouldNotBeNull();
+        roundtripResult.Message.ShouldBe(TestMessage);
+        roundtripResult.InnerException.ShouldBeOfType<InvalidOperationException>();
+        roundtripResult.RetryAfter.ShouldBeNull();
     }
 #endif
 
--- test/Polly.Core.Tests/CircuitBreaker/CircuitBreakerManualControlTests.cs
--- a/test/Polly.Core.Tests/CircuitBreaker/CircuitBreakerManualControlTests.cs
+++ b/test/Polly.Core.Tests/CircuitBreaker/CircuitBreakerManualControlTests.cs
@@ -15,13 +15,13 @@ public class CircuitBreakerManualControlTests
         using var reg = control.Initialize(
             c =>
             {
-                c.IsSynchronous.Should().BeTrue();
+                c.IsSynchronous.ShouldBeTrue();
                 isolateCalled = true;
                 return Task.CompletedTask;
             },
             _ => Task.CompletedTask);
 
-        isolateCalled.Should().Be(isolated);
+        isolateCalled.ShouldBe(isolated);
     }
 
     [InlineData(true)]
@@ -42,13 +42,13 @@ public class CircuitBreakerManualControlTests
         using var reg = control.Initialize(
             c =>
             {
-                c.IsSynchronous.Should().BeTrue();
+                c.IsSynchronous.ShouldBeTrue();
                 isolated = true;
                 return Task.CompletedTask;
             },
             _ => Task.CompletedTask);
 
-        isolated.Should().Be(!closedAfter);
+        isolated.ShouldBe(!closedAfter);
```

Kirpildi; tam diff: https://github.com/app-vnext/polly/commit/b4d43e27c5d265c5b6704e63a4e126390e8f4f9d

Sonraki tarihsel olgular:

- `test/Polly.Core.Tests/CircuitBreaker/BreakDurationGeneratorArgumentsTests.cs`
  - `d36bdd5c9317` 2026-05-22 Bump SonarAnalyzer.CSharp from 10.25.0.139117 to 10.26.0.140279 (#3085)
    https://github.com/app-vnext/polly/commit/d36bdd5c931784ebf5c99ff63601cace46a1d5e8
- `test/Polly.Core.Tests/CircuitBreaker/BrokenCircuitExceptionTests.cs`
  - Gozlem araliginda sonraki degisiklik yok
- `test/Polly.Core.Tests/CircuitBreaker/CircuitBreakerManualControlTests.cs`
  - `6d5fcd6160f5` 2025-08-23 xunit v3 preparation (#2723)
    https://github.com/app-vnext/polly/commit/6d5fcd6160f5b4a2291075131ef4e485d1dacdec
- `test/Polly.Core.Tests/CircuitBreaker/CircuitBreakerOptionsTests.cs`
  - `6d5fcd6160f5` 2025-08-23 xunit v3 preparation (#2723)
    https://github.com/app-vnext/polly/commit/6d5fcd6160f5b4a2291075131ef4e485d1dacdec
- `test/Polly.Core.Tests/CircuitBreaker/CircuitBreakerPredicateArgumentsTests.cs`
  - `6d5fcd6160f5` 2025-08-23 xunit v3 preparation (#2723)
    https://github.com/app-vnext/polly/commit/6d5fcd6160f5b4a2291075131ef4e485d1dacdec
- `test/Polly.Core.Tests/CircuitBreaker/CircuitBreakerResiliencePipelineBuilderTests.cs`
  - `6d5fcd6160f5` 2025-08-23 xunit v3 preparation (#2723)
    https://github.com/app-vnext/polly/commit/6d5fcd6160f5b4a2291075131ef4e485d1dacdec
- `test/Polly.Core.Tests/CircuitBreaker/CircuitBreakerResilienceStrategyTests.cs`
  - `a14508b9d283` 2025-08-22 Reduce async overhead (#2664)
    https://github.com/app-vnext/polly/commit/a14508b9d2839efbd9141305bb3e327ca261dbeb
  - `6d5fcd6160f5` 2025-08-23 xunit v3 preparation (#2723)
    https://github.com/app-vnext/polly/commit/6d5fcd6160f5b4a2291075131ef4e485d1dacdec
- `test/Polly.Core.Tests/CircuitBreaker/CircuitBreakerStateProviderTests.cs`
  - Gozlem araliginda sonraki degisiklik yok
- `test/Polly.Core.Tests/CircuitBreaker/Controller/AdvancedCircuitBehaviorTests.cs`
  - Gozlem araliginda sonraki degisiklik yok
- `test/Polly.Core.Tests/CircuitBreaker/Controller/CircuitStateControllerTests.cs`
  - `a14508b9d283` 2025-08-22 Reduce async overhead (#2664)
    https://github.com/app-vnext/polly/commit/a14508b9d2839efbd9141305bb3e327ca261dbeb
  - `6d5fcd6160f5` 2025-08-23 xunit v3 preparation (#2723)
    https://github.com/app-vnext/polly/commit/6d5fcd6160f5b4a2291075131ef4e485d1dacdec
  - `53bf3885fb62` 2026-04-14 Assert nullable has result (#3028)
    https://github.com/app-vnext/polly/commit/53bf3885fb62f20296280867b42c5ac1c1c13fb8
- `test/Polly.Core.Tests/CircuitBreaker/Controller/ScheduledTaskExecutorTests.cs`
  - `a14508b9d283` 2025-08-22 Reduce async overhead (#2664)
    https://github.com/app-vnext/polly/commit/a14508b9d2839efbd9141305bb3e327ca261dbeb
  - `6d5fcd6160f5` 2025-08-23 xunit v3 preparation (#2723)
    https://github.com/app-vnext/polly/commit/6d5fcd6160f5b4a2291075131ef4e485d1dacdec
  - `016dd909db88` 2026-03-03 Fix 'ScheduledTaskExecutor' deadlock when 'TrySetResult' runs continuations inline (#2953)
    https://github.com/app-vnext/polly/commit/016dd909db880b9d8d07954ff5f4ea3cc1e11c67
- `test/Polly.Core.Tests/CircuitBreaker/Health/HealthMetricsTests.cs`
  - Gozlem araliginda sonraki degisiklik yok
- `test/Polly.Core.Tests/CircuitBreaker/Health/RollingHealthMetricsTests.cs`
  - Gozlem araliginda sonraki degisiklik yok
- `test/Polly.Core.Tests/CircuitBreaker/Health/SingleHealthMetricsTests.cs`
  - Gozlem araliginda sonraki degisiklik yok
- `test/Polly.Core.Tests/CircuitBreaker/IsolatedCircuitExceptionTests.cs`
  - Gozlem araliginda sonraki degisiklik yok
- `test/Polly.Core.Tests/CircuitBreaker/OnCircuitClosedArgumentsTests.cs`
  - `6d5fcd6160f5` 2025-08-23 xunit v3 preparation (#2723)
    https://github.com/app-vnext/polly/commit/6d5fcd6160f5b4a2291075131ef4e485d1dacdec
- `test/Polly.Core.Tests/CircuitBreaker/OnCircuitHalfOpenedArgumentsTests.cs`
  - `6d5fcd6160f5` 2025-08-23 xunit v3 preparation (#2723)
    https://github.com/app-vnext/polly/commit/6d5fcd6160f5b4a2291075131ef4e485d1dacdec
- `test/Polly.Core.Tests/CircuitBreaker/OnCircuitOpenedArgumentsTests.cs`
  - `6d5fcd6160f5` 2025-08-23 xunit v3 preparation (#2723)
    https://github.com/app-vnext/polly/commit/6d5fcd6160f5b4a2291075131ef4e485d1dacdec
- `test/Polly.Core.Tests/ExecutionRejectedExceptionTests.cs`
  - Gozlem araliginda sonraki degisiklik yok
- `test/Polly.Core.Tests/Fallback/FallbackHandlerTests.cs`
  - `a14508b9d283` 2025-08-22 Reduce async overhead (#2664)
    https://github.com/app-vnext/polly/commit/a14508b9d2839efbd9141305bb3e327ca261dbeb
  - `6d5fcd6160f5` 2025-08-23 xunit v3 preparation (#2723)
    https://github.com/app-vnext/polly/commit/6d5fcd6160f5b4a2291075131ef4e485d1dacdec
- `test/Polly.Core.Tests/Fallback/FallbackResiliencePipelineBuilderExtensionsTests.cs`
  - Gozlem araliginda sonraki degisiklik yok
- `test/Polly.Core.Tests/Fallback/FallbackResilienceStrategyTests.cs`
  - `a14508b9d283` 2025-08-22 Reduce async overhead (#2664)
    https://github.com/app-vnext/polly/commit/a14508b9d2839efbd9141305bb3e327ca261dbeb
  - `6d5fcd6160f5` 2025-08-23 xunit v3 preparation (#2723)
    https://github.com/app-vnext/polly/commit/6d5fcd6160f5b4a2291075131ef4e485d1dacdec
- `test/Polly.Core.Tests/Fallback/FallbackStrategyOptionsTests.cs`
  - `6d5fcd6160f5` 2025-08-23 xunit v3 preparation (#2723)
    https://github.com/app-vnext/polly/commit/6d5fcd6160f5b4a2291075131ef4e485d1dacdec
- `test/Polly.Core.Tests/GenericResiliencePipelineBuilderTests.cs`
  - Gozlem araliginda sonraki degisiklik yok
- `test/Polly.Core.Tests/Hedging/Controller/HedgingControllerTests.cs`
  - `6d5fcd6160f5` 2025-08-23 xunit v3 preparation (#2723)
    https://github.com/app-vnext/polly/commit/6d5fcd6160f5b4a2291075131ef4e485d1dacdec
- `test/Polly.Core.Tests/Hedging/Controller/HedgingExecutionContextTests.cs`
  - `acd304721c50` 2025-01-18 Use ShouldBeEmpty
    https://github.com/app-vnext/polly/commit/acd304721c5078a70593dd50b6eaa78939b21a57
  - `a14508b9d283` 2025-08-22 Reduce async overhead (#2664)
    https://github.com/app-vnext/polly/commit/a14508b9d2839efbd9141305bb3e327ca261dbeb
  - `6d5fcd6160f5` 2025-08-23 xunit v3 preparation (#2723)
    https://github.com/app-vnext/polly/commit/6d5fcd6160f5b4a2291075131ef4e485d1dacdec
- `test/Polly.Core.Tests/Hedging/Controller/TaskExecutionTests.cs`
  - `a14508b9d283` 2025-08-22 Reduce async overhead (#2664)
    https://github.com/app-vnext/polly/commit/a14508b9d2839efbd9141305bb3e327ca261dbeb
  - `65983195e7b2` 2025-09-06 Simplify code (#2740)
    https://github.com/app-vnext/polly/commit/65983195e7b26d182f4be8a3ad5ee3c29fbea28a
  - `8fd8fc08736b` 2026-04-03 Formatting tweaks (#3017)
    https://github.com/app-vnext/polly/commit/8fd8fc08736b3623443f9c87433dfc120ab18a57
- `test/Polly.Core.Tests/Hedging/HedgingActionGeneratorArgumentsTests.cs`
  - `6d5fcd6160f5` 2025-08-23 xunit v3 preparation (#2723)
    https://github.com/app-vnext/polly/commit/6d5fcd6160f5b4a2291075131ef4e485d1dacdec
- `test/Polly.Core.Tests/Hedging/HedgingDelayGeneratorArgumentsTests.cs`
  - `6d5fcd6160f5` 2025-08-23 xunit v3 preparation (#2723)
    https://github.com/app-vnext/polly/commit/6d5fcd6160f5b4a2291075131ef4e485d1dacdec
- `test/Polly.Core.Tests/Hedging/HedgingHandlerTests.cs`
  - `a14508b9d283` 2025-08-22 Reduce async overhead (#2664)
    https://github.com/app-vnext/polly/commit/a14508b9d2839efbd9141305bb3e327ca261dbeb
- `test/Polly.Core.Tests/Hedging/HedgingPredicateArgumentsTests.cs`
  - `198b42a16a75` 2025-05-04 Add AttemptNumber to HedgingPredicateArguments (#2603)
    https://github.com/app-vnext/polly/commit/198b42a16a75753be49d9820aa89c6b3518b91ea
  - `6d5fcd6160f5` 2025-08-23 xunit v3 preparation (#2723)
    https://github.com/app-vnext/polly/commit/6d5fcd6160f5b4a2291075131ef4e485d1dacdec
- `test/Polly.Core.Tests/Hedging/HedgingResiliencePipelineBuilderExtensionsTests.cs`
  - `198b42a16a75` 2025-05-04 Add AttemptNumber to HedgingPredicateArguments (#2603)
    https://github.com/app-vnext/polly/commit/198b42a16a75753be49d9820aa89c6b3518b91ea
  - `6d5fcd6160f5` 2025-08-23 xunit v3 preparation (#2723)
    https://github.com/app-vnext/polly/commit/6d5fcd6160f5b4a2291075131ef4e485d1dacdec
- `test/Polly.Core.Tests/Hedging/HedgingResilienceStrategyTests.cs`
  - `bdbfc74bbe0e` 2025-01-18 Use ShouldBeInOrder
    https://github.com/app-vnext/polly/commit/bdbfc74bbe0efe5c14203146ecd70b63d27b9177
  - `6d5fcd6160f5` 2025-08-23 xunit v3 preparation (#2723)
    https://github.com/app-vnext/polly/commit/6d5fcd6160f5b4a2291075131ef4e485d1dacdec
  - `e984839b8324` 2026-06-04 Add caller CancellationToken propagation for hedging and timeout (#3094)
    https://github.com/app-vnext/polly/commit/e984839b8324a163270f0bc686929a1066de69e0
- `test/Polly.Core.Tests/Hedging/HedgingStrategyOptionsTests.cs`
  - `198b42a16a75` 2025-05-04 Add AttemptNumber to HedgingPredicateArguments (#2603)
    https://github.com/app-vnext/polly/commit/198b42a16a75753be49d9820aa89c6b3518b91ea
  - `6d5fcd6160f5` 2025-08-23 xunit v3 preparation (#2723)
    https://github.com/app-vnext/polly/commit/6d5fcd6160f5b4a2291075131ef4e485d1dacdec
- `test/Polly.Core.Tests/Hedging/OnHedgingArgumentsTests.cs`
  - `6d5fcd6160f5` 2025-08-23 xunit v3 preparation (#2723)
    https://github.com/app-vnext/polly/commit/6d5fcd6160f5b4a2291075131ef4e485d1dacdec
- `test/Polly.Core.Tests/Issues/IssuesTests.CircuitBreakerStateRegistry_1828.cs`
  - Gozlem araliginda sonraki degisiklik yok
- `test/Polly.Core.Tests/Issues/IssuesTests.CircuitBreakerStateSharing_959.cs`
  - `6d5fcd6160f5` 2025-08-23 xunit v3 preparation (#2723)
    https://github.com/app-vnext/polly/commit/6d5fcd6160f5b4a2291075131ef4e485d1dacdec
  - `ed3d603c7bc1` 2026-04-03 Formatting tweaks (#3018)
    https://github.com/app-vnext/polly/commit/ed3d603c7bc17e8b4eafdcb8e982916cc6928a2f
- `test/Polly.Core.Tests/Issues/IssuesTests.FlowingContext_849.cs`
  - `6d5fcd6160f5` 2025-08-23 xunit v3 preparation (#2723)
    https://github.com/app-vnext/polly/commit/6d5fcd6160f5b4a2291075131ef4e485d1dacdec
- `test/Polly.Core.Tests/Issues/IssuesTests.HandleMultipleResults_898.cs`
  - `6d5fcd6160f5` 2025-08-23 xunit v3 preparation (#2723)
    https://github.com/app-vnext/polly/commit/6d5fcd6160f5b4a2291075131ef4e485d1dacdec
  - `8fd8fc08736b` 2026-04-03 Formatting tweaks (#3017)
    https://github.com/app-vnext/polly/commit/8fd8fc08736b3623443f9c87433dfc120ab18a57
- `test/Polly.Core.Tests/Issues/IssuesTests.InfiniteRetry_2163.cs`
  - Gozlem araliginda sonraki degisiklik yok
- `test/Polly.Core.Tests/OutcomeTests.cs`
  - `3fb089717fb6` 2025-02-21 Improve coverage (#2526)
    https://github.com/app-vnext/polly/commit/3fb089717fb63c52b51d66a378cdad08b3f6335f
  - `f56e106c1cb7` 2025-05-16 .NET 10 preparation (#2620)
    https://github.com/app-vnext/polly/commit/f56e106c1cb74ebab1fc9178d866df1c05df5db0
- `test/Polly.Core.Tests/PredicateBuilderTests.cs`
  - `3fb089717fb6` 2025-02-21 Improve coverage (#2526)
    https://github.com/app-vnext/polly/commit/3fb089717fb63c52b51d66a378cdad08b3f6335f
  - `198b42a16a75` 2025-05-04 Add AttemptNumber to HedgingPredicateArguments (#2603)
    https://github.com/app-vnext/polly/commit/198b42a16a75753be49d9820aa89c6b3518b91ea
  - `6d5fcd6160f5` 2025-08-23 xunit v3 preparation (#2723)
    https://github.com/app-vnext/polly/commit/6d5fcd6160f5b4a2291075131ef4e485d1dacdec
- `test/Polly.Core.Tests/PredicateResultTests.cs`
  - Gozlem araliginda sonraki degisiklik yok
- `test/Polly.Core.Tests/Registry/ConfigureBuilderContextTests.cs`
  - Gozlem araliginda sonraki degisiklik yok
- `test/Polly.Core.Tests/Registry/ResiliencePipelineProviderTests.cs`
  - Gozlem araliginda sonraki degisiklik yok
- `test/Polly.Core.Tests/Registry/ResiliencePipelineRegistryOptionsTests.cs`
  - `f56e106c1cb7` 2025-05-16 .NET 10 preparation (#2620)
    https://github.com/app-vnext/polly/commit/f56e106c1cb74ebab1fc9178d866df1c05df5db0
- `test/Polly.Core.Tests/Registry/ResiliencePipelineRegistryTests.cs`
  - Gozlem araliginda sonraki degisiklik yok
- `test/Polly.Core.Tests/ResilienceContextPoolTests.cs`
  - `6d5fcd6160f5` 2025-08-23 xunit v3 preparation (#2723)
    https://github.com/app-vnext/polly/commit/6d5fcd6160f5b4a2291075131ef4e485d1dacdec
- `test/Polly.Core.Tests/ResilienceContextTests.cs`
  - `6d5fcd6160f5` 2025-08-23 xunit v3 preparation (#2723)
    https://github.com/app-vnext/polly/commit/6d5fcd6160f5b4a2291075131ef4e485d1dacdec
- `test/Polly.Core.Tests/ResiliencePipelineBuilderTests.cs`
  - `11bd31651890` 2025-01-18 Swap assertion order
    https://github.com/app-vnext/polly/commit/11bd31651890c361d3064d7a9ef082b8289e61fa
  - `bdbfc74bbe0e` 2025-01-18 Use ShouldBeInOrder
    https://github.com/app-vnext/polly/commit/bdbfc74bbe0efe5c14203146ecd70b63d27b9177
  - `6d5fcd6160f5` 2025-08-23 xunit v3 preparation (#2723)
    https://github.com/app-vnext/polly/commit/6d5fcd6160f5b4a2291075131ef4e485d1dacdec
- `test/Polly.Core.Tests/ResiliencePipelineTTests.Async.cs`
  - Gozlem araliginda sonraki degisiklik yok
- `test/Polly.Core.Tests/ResiliencePipelineTTests.Sync.cs`
  - `3fb089717fb6` 2025-02-21 Improve coverage (#2526)
    https://github.com/app-vnext/polly/commit/3fb089717fb63c52b51d66a378cdad08b3f6335f
  - `6d5fcd6160f5` 2025-08-23 xunit v3 preparation (#2723)
    https://github.com/app-vnext/polly/commit/6d5fcd6160f5b4a2291075131ef4e485d1dacdec
- `test/Polly.Core.Tests/ResiliencePipelineTests.Async.cs`
  - `6d5fcd6160f5` 2025-08-23 xunit v3 preparation (#2723)
    https://github.com/app-vnext/polly/commit/6d5fcd6160f5b4a2291075131ef4e485d1dacdec
- `test/Polly.Core.Tests/ResiliencePipelineTests.AsyncT.cs`
  - `6d5fcd6160f5` 2025-08-23 xunit v3 preparation (#2723)
    https://github.com/app-vnext/polly/commit/6d5fcd6160f5b4a2291075131ef4e485d1dacdec
- `test/Polly.Core.Tests/ResiliencePipelineTests.Sync.cs`
  - Gozlem araliginda sonraki degisiklik yok
- `test/Polly.Core.Tests/ResiliencePipelineTests.SyncT.cs`
  - Gozlem araliginda sonraki degisiklik yok
- `test/Polly.Core.Tests/ResiliencePipelineTests.cs`
  - `c276803bb3f0` 2025-08-22 Use collection expressions
    https://github.com/app-vnext/polly/commit/c276803bb3f03b3ac8d776fa7ab321695dc0cd8a
- `test/Polly.Core.Tests/ResiliencePropertiesTests.cs`
  - Gozlem araliginda sonraki degisiklik yok
- `test/Polly.Core.Tests/ResiliencePropertyKeyTests.cs`
  - Gozlem araliginda sonraki degisiklik yok
- `test/Polly.Core.Tests/ResilienceStrategyOptionsTests.cs`
  - Gozlem araliginda sonraki degisiklik yok
- `test/Polly.Core.Tests/Retry/OnRetryArgumentsTests.cs`
  - `6d5fcd6160f5` 2025-08-23 xunit v3 preparation (#2723)
    https://github.com/app-vnext/polly/commit/6d5fcd6160f5b4a2291075131ef4e485d1dacdec
- `test/Polly.Core.Tests/Retry/RetryConstantsTests.cs`
  - Gozlem araliginda sonraki degisiklik yok
- `test/Polly.Core.Tests/Retry/RetryDelayGeneratorArgumentsTests.cs`
  - `6d5fcd6160f5` 2025-08-23 xunit v3 preparation (#2723)
    https://github.com/app-vnext/polly/commit/6d5fcd6160f5b4a2291075131ef4e485d1dacdec
- `test/Polly.Core.Tests/Retry/RetryHelperTests.cs`
  - `a14508b9d283` 2025-08-22 Reduce async overhead (#2664)
    https://github.com/app-vnext/polly/commit/a14508b9d2839efbd9141305bb3e327ca261dbeb
  - `c276803bb3f0` 2025-08-22 Use collection expressions
    https://github.com/app-vnext/polly/commit/c276803bb3f03b3ac8d776fa7ab321695dc0cd8a
  - `156a9bbee143` 2025-11-24 Add specification tests for jitter
    https://github.com/app-vnext/polly/commit/156a9bbee14397989490cc280f09de324ca85191
- `test/Polly.Core.Tests/Retry/RetryResiliencePipelineBuilderExtensionsTests.cs`
  - Gozlem araliginda sonraki degisiklik yok
- `test/Polly.Core.Tests/Retry/RetryResilienceStrategyTests.cs`
  - `a953ba811907` 2025-02-04 Fix for retry cancellation (#2456)
    https://github.com/app-vnext/polly/commit/a953ba8119074d7f63cc69643d4ef1d1de52ee3f
  - `8eee77abe2a4` 2025-08-12 Refactor project dependencies
    https://github.com/app-vnext/polly/commit/8eee77abe2a42da768cf9f05cdd998595df26c1c
  - `a14508b9d283` 2025-08-22 Reduce async overhead (#2664)
    https://github.com/app-vnext/polly/commit/a14508b9d2839efbd9141305bb3e327ca261dbeb
- `test/Polly.Core.Tests/Retry/RetryStrategyOptionsTests.cs`
  - `6d5fcd6160f5` 2025-08-23 xunit v3 preparation (#2723)
    https://github.com/app-vnext/polly/commit/6d5fcd6160f5b4a2291075131ef4e485d1dacdec
- `test/Polly.Core.Tests/Retry/ShouldRetryArgumentsTests.cs`
  - `6d5fcd6160f5` 2025-08-23 xunit v3 preparation (#2723)
    https://github.com/app-vnext/polly/commit/6d5fcd6160f5b4a2291075131ef4e485d1dacdec
- `test/Polly.Core.Tests/Simmy/Behavior/BehaviorGeneratorArgumentsTests.cs`
  - `6d5fcd6160f5` 2025-08-23 xunit v3 preparation (#2723)
    https://github.com/app-vnext/polly/commit/6d5fcd6160f5b4a2291075131ef4e485d1dacdec
- `test/Polly.Core.Tests/Simmy/Behavior/ChaosBehaviorConstantsTests.cs`
  - Gozlem araliginda sonraki degisiklik yok
- `test/Polly.Core.Tests/Simmy/Behavior/ChaosBehaviorPipelineBuilderExtensionsTests.cs`
  - `6d5fcd6160f5` 2025-08-23 xunit v3 preparation (#2723)
    https://github.com/app-vnext/polly/commit/6d5fcd6160f5b4a2291075131ef4e485d1dacdec
- `test/Polly.Core.Tests/Simmy/Behavior/ChaosBehaviorStrategyOptionsTests.cs`
  - Gozlem araliginda sonraki degisiklik yok
- `test/Polly.Core.Tests/Simmy/Behavior/ChaosBehaviorStrategyTests.cs`
  - `6d5fcd6160f5` 2025-08-23 xunit v3 preparation (#2723)
    https://github.com/app-vnext/polly/commit/6d5fcd6160f5b4a2291075131ef4e485d1dacdec
- `test/Polly.Core.Tests/Simmy/Behavior/OnBehaviorInjectedArgumentsTests.cs`
  - `6d5fcd6160f5` 2025-08-23 xunit v3 preparation (#2723)
    https://github.com/app-vnext/polly/commit/6d5fcd6160f5b4a2291075131ef4e485d1dacdec
- `test/Polly.Core.Tests/Simmy/ChaosStrategyConstantsTests.cs`
  - Gozlem araliginda sonraki degisiklik yok
- `test/Polly.Core.Tests/Simmy/ChaosStrategyOptionsTTests.cs`
  - Gozlem araliginda sonraki degisiklik yok
- `test/Polly.Core.Tests/Simmy/ChaosStrategyOptionsTests.cs`
  - `f56e106c1cb7` 2025-05-16 .NET 10 preparation (#2620)
    https://github.com/app-vnext/polly/commit/f56e106c1cb74ebab1fc9178d866df1c05df5db0
- `test/Polly.Core.Tests/Simmy/ChaosStrategyTTests.cs`
  - `6d5fcd6160f5` 2025-08-23 xunit v3 preparation (#2723)
    https://github.com/app-vnext/polly/commit/6d5fcd6160f5b4a2291075131ef4e485d1dacdec
- `test/Polly.Core.Tests/Simmy/ChaosStrategyTests.cs`
  - `6d5fcd6160f5` 2025-08-23 xunit v3 preparation (#2723)
    https://github.com/app-vnext/polly/commit/6d5fcd6160f5b4a2291075131ef4e485d1dacdec
- `test/Polly.Core.Tests/Simmy/EnabledGeneratorArgumentsTests.cs`
  - `6d5fcd6160f5` 2025-08-23 xunit v3 preparation (#2723)
    https://github.com/app-vnext/polly/commit/6d5fcd6160f5b4a2291075131ef4e485d1dacdec
- `test/Polly.Core.Tests/Simmy/Fault/ChaosFaultConstantsTests.cs`
  - Gozlem araliginda sonraki degisiklik yok
- `test/Polly.Core.Tests/Simmy/Fault/ChaosFaultPipelineBuilderExtensionsTests.cs`
  - `6d5fcd6160f5` 2025-08-23 xunit v3 preparation (#2723)
    https://github.com/app-vnext/polly/commit/6d5fcd6160f5b4a2291075131ef4e485d1dacdec
- `test/Polly.Core.Tests/Simmy/Fault/ChaosFaultStrategyOptionsTests.cs`
  - Gozlem araliginda sonraki degisiklik yok
- `test/Polly.Core.Tests/Simmy/Fault/ChaosFaultStrategyTests.cs`
  - `6d5fcd6160f5` 2025-08-23 xunit v3 preparation (#2723)
    https://github.com/app-vnext/polly/commit/6d5fcd6160f5b4a2291075131ef4e485d1dacdec
- `test/Polly.Core.Tests/Simmy/Fault/FaultGeneratorArgumentsTests.cs`
  - `6d5fcd6160f5` 2025-08-23 xunit v3 preparation (#2723)
    https://github.com/app-vnext/polly/commit/6d5fcd6160f5b4a2291075131ef4e485d1dacdec
- `test/Polly.Core.Tests/Simmy/Fault/FaultGeneratorTests.cs`
  - `6d5fcd6160f5` 2025-08-23 xunit v3 preparation (#2723)
    https://github.com/app-vnext/polly/commit/6d5fcd6160f5b4a2291075131ef4e485d1dacdec
  - `482bdf824fba` 2026-09-06 Return null from FaultGenerator when no fault is generated (#3220)
    https://github.com/app-vnext/polly/commit/482bdf824fba19c1188655156e512570c3d7f7af
- `test/Polly.Core.Tests/Simmy/Fault/OnFaultInjectedArgumentsTests.cs`
  - `6d5fcd6160f5` 2025-08-23 xunit v3 preparation (#2723)
    https://github.com/app-vnext/polly/commit/6d5fcd6160f5b4a2291075131ef4e485d1dacdec
- `test/Polly.Core.Tests/Simmy/InjectionRateGeneratorArgumentsTests.cs`
  - `6d5fcd6160f5` 2025-08-23 xunit v3 preparation (#2723)
    https://github.com/app-vnext/polly/commit/6d5fcd6160f5b4a2291075131ef4e485d1dacdec
- `test/Polly.Core.Tests/Simmy/Latency/ChaosLatencyConstantsTests.cs`
  - Gozlem araliginda sonraki degisiklik yok
- `test/Polly.Core.Tests/Simmy/Latency/ChaosLatencyPipelineBuilderExtensionsTests.cs`
  - `6d5fcd6160f5` 2025-08-23 xunit v3 preparation (#2723)
    https://github.com/app-vnext/polly/commit/6d5fcd6160f5b4a2291075131ef4e485d1dacdec
- `test/Polly.Core.Tests/Simmy/Latency/ChaosLatencyStrategyOptionsTests.cs`
  - Gozlem araliginda sonraki degisiklik yok
- `test/Polly.Core.Tests/Simmy/Latency/ChaosLatencyStrategyTests.cs`
  - `6d5fcd6160f5` 2025-08-23 xunit v3 preparation (#2723)
    https://github.com/app-vnext/polly/commit/6d5fcd6160f5b4a2291075131ef4e485d1dacdec
- `test/Polly.Core.Tests/Simmy/Latency/LatencyGeneratorArgumentsTests.cs`
  - `6d5fcd6160f5` 2025-08-23 xunit v3 preparation (#2723)
    https://github.com/app-vnext/polly/commit/6d5fcd6160f5b4a2291075131ef4e485d1dacdec
- `test/Polly.Core.Tests/Simmy/Latency/OnLatencyInjectedArgumentsTests.cs`
  - `6d5fcd6160f5` 2025-08-23 xunit v3 preparation (#2723)
    https://github.com/app-vnext/polly/commit/6d5fcd6160f5b4a2291075131ef4e485d1dacdec
- `test/Polly.Core.Tests/Simmy/Outcomes/ChaosOutcomeConstantsTests.cs`
  - Gozlem araliginda sonraki degisiklik yok
- `test/Polly.Core.Tests/Simmy/Outcomes/ChaosOutcomePipelineBuilderExtensionsTests.cs`
  - `6d5fcd6160f5` 2025-08-23 xunit v3 preparation (#2723)
    https://github.com/app-vnext/polly/commit/6d5fcd6160f5b4a2291075131ef4e485d1dacdec
- `test/Polly.Core.Tests/Simmy/Outcomes/ChaosOutcomeStrategyOptionsTests.cs`
  - Gozlem araliginda sonraki degisiklik yok
- `test/Polly.Core.Tests/Simmy/Outcomes/ChaosOutcomeStrategyTests.cs`
  - `6d5fcd6160f5` 2025-08-23 xunit v3 preparation (#2723)
    https://github.com/app-vnext/polly/commit/6d5fcd6160f5b4a2291075131ef4e485d1dacdec
  - `65983195e7b2` 2025-09-06 Simplify code (#2740)
    https://github.com/app-vnext/polly/commit/65983195e7b26d182f4be8a3ad5ee3c29fbea28a
- `test/Polly.Core.Tests/Simmy/Outcomes/OnOutcomeInjectedArgumentsTests.cs`
  - `6d5fcd6160f5` 2025-08-23 xunit v3 preparation (#2723)
    https://github.com/app-vnext/polly/commit/6d5fcd6160f5b4a2291075131ef4e485d1dacdec
- `test/Polly.Core.Tests/Simmy/Outcomes/OutcomeGeneratorArgumentsTests.cs`
  - `6d5fcd6160f5` 2025-08-23 xunit v3 preparation (#2723)
    https://github.com/app-vnext/polly/commit/6d5fcd6160f5b4a2291075131ef4e485d1dacdec
- `test/Polly.Core.Tests/Simmy/Outcomes/OutcomeGeneratorTests.cs`
  - `6d5fcd6160f5` 2025-08-23 xunit v3 preparation (#2723)
    https://github.com/app-vnext/polly/commit/6d5fcd6160f5b4a2291075131ef4e485d1dacdec
- `test/Polly.Core.Tests/Simmy/TestChaosStrategyOptions.TResult.cs`
  - Gozlem araliginda sonraki degisiklik yok
- `test/Polly.Core.Tests/Simmy/TestChaosStrategyOptions.cs`
  - Gozlem araliginda sonraki degisiklik yok
- `test/Polly.Core.Tests/Simmy/Utils/GeneratorHelperTests.cs`
  - `6d5fcd6160f5` 2025-08-23 xunit v3 preparation (#2723)
    https://github.com/app-vnext/polly/commit/6d5fcd6160f5b4a2291075131ef4e485d1dacdec
- `test/Polly.Core.Tests/StrategyBuilderContextTests.cs`
  - `b87f20347af5` 2026-03-14 Telemetry refactoring (#2985)
    https://github.com/app-vnext/polly/commit/b87f20347af528b6f7c6ff1f9e6c1bc02f97856c
- `test/Polly.Core.Tests/Telemetry/ExecutionAttemptArgumentsTests.cs`
  - Gozlem araliginda sonraki degisiklik yok
- `test/Polly.Core.Tests/Telemetry/PipelineExecutedArgumentsTests.cs`
  - Gozlem araliginda sonraki degisiklik yok
- `test/Polly.Core.Tests/Telemetry/ResilienceEventTests.cs`
  - Gozlem araliginda sonraki degisiklik yok
- `test/Polly.Core.Tests/Telemetry/ResilienceStrategyTelemetryTests.cs`
  - `3fb089717fb6` 2025-02-21 Improve coverage (#2526)
    https://github.com/app-vnext/polly/commit/3fb089717fb63c52b51d66a378cdad08b3f6335f
  - `6d5fcd6160f5` 2025-08-23 xunit v3 preparation (#2723)
    https://github.com/app-vnext/polly/commit/6d5fcd6160f5b4a2291075131ef4e485d1dacdec
  - `b87f20347af5` 2026-03-14 Telemetry refactoring (#2985)
    https://github.com/app-vnext/polly/commit/b87f20347af528b6f7c6ff1f9e6c1bc02f97856c
- `test/Polly.Core.Tests/Telemetry/TelemetryEventArgumentsTests.cs`
  - `6d5fcd6160f5` 2025-08-23 xunit v3 preparation (#2723)
    https://github.com/app-vnext/polly/commit/6d5fcd6160f5b4a2291075131ef4e485d1dacdec
- `test/Polly.Core.Tests/Telemetry/TelemetryUtilTests.cs`
  - `6d5fcd6160f5` 2025-08-23 xunit v3 preparation (#2723)
    https://github.com/app-vnext/polly/commit/6d5fcd6160f5b4a2291075131ef4e485d1dacdec
- `test/Polly.Core.Tests/Timeout/TimeoutConstantsTests.cs`
  - Gozlem araliginda sonraki degisiklik yok
- `test/Polly.Core.Tests/Timeout/TimeoutRejectedExceptionTests.cs`
  - Gozlem araliginda sonraki degisiklik yok
- `test/Polly.Core.Tests/Timeout/TimeoutResiliencePipelineBuilderExtensionsTests.cs`
  - `6d5fcd6160f5` 2025-08-23 xunit v3 preparation (#2723)
    https://github.com/app-vnext/polly/commit/6d5fcd6160f5b4a2291075131ef4e485d1dacdec
- `test/Polly.Core.Tests/Timeout/TimeoutResilienceStrategyTests.cs`
  - `acd304721c50` 2025-01-18 Use ShouldBeEmpty
    https://github.com/app-vnext/polly/commit/acd304721c5078a70593dd50b6eaa78939b21a57
  - `6d5fcd6160f5` 2025-08-23 xunit v3 preparation (#2723)
    https://github.com/app-vnext/polly/commit/6d5fcd6160f5b4a2291075131ef4e485d1dacdec
  - `e984839b8324` 2026-06-04 Add caller CancellationToken propagation for hedging and timeout (#3094)
    https://github.com/app-vnext/polly/commit/e984839b8324a163270f0bc686929a1066de69e0
- `test/Polly.Core.Tests/Timeout/TimeoutStrategyOptionsTests.cs`
  - Gozlem araliginda sonraki degisiklik yok
- `test/Polly.Core.Tests/Timeout/TimeoutUtilTests.cs`
  - `f56e106c1cb7` 2025-05-16 .NET 10 preparation (#2620)
    https://github.com/app-vnext/polly/commit/f56e106c1cb74ebab1fc9178d866df1c05df5db0
- `test/Polly.Core.Tests/Utils/CancellationTokenSourcePoolTests.cs`
  - `6d5fcd6160f5` 2025-08-23 xunit v3 preparation (#2723)
    https://github.com/app-vnext/polly/commit/6d5fcd6160f5b4a2291075131ef4e485d1dacdec
- `test/Polly.Core.Tests/Utils/DisposeHelperTests.cs`
  - Gozlem araliginda sonraki degisiklik yok
- `test/Polly.Core.Tests/Utils/LegacySupportTests.cs`
  - `3fb089717fb6` 2025-02-21 Improve coverage (#2526)
    https://github.com/app-vnext/polly/commit/3fb089717fb63c52b51d66a378cdad08b3f6335f
- `test/Polly.Core.Tests/Utils/Pipeline/BridgePipelineComponentTests.cs`
  - `6d5fcd6160f5` 2025-08-23 xunit v3 preparation (#2723)
    https://github.com/app-vnext/polly/commit/6d5fcd6160f5b4a2291075131ef4e485d1dacdec
- `test/Polly.Core.Tests/Utils/Pipeline/ComponentWithDisposeCallbacksTests.cs`
  - Gozlem araliginda sonraki degisiklik yok
- `test/Polly.Core.Tests/Utils/Pipeline/CompositePipelineComponentTests.cs`
  - `6d5fcd6160f5` 2025-08-23 xunit v3 preparation (#2723)
    https://github.com/app-vnext/polly/commit/6d5fcd6160f5b4a2291075131ef4e485d1dacdec
- `test/Polly.Core.Tests/Utils/Pipeline/DelegatingComponentTests.cs`
  - `6d5fcd6160f5` 2025-08-23 xunit v3 preparation (#2723)
    https://github.com/app-vnext/polly/commit/6d5fcd6160f5b4a2291075131ef4e485d1dacdec
- `test/Polly.Core.Tests/Utils/Pipeline/ExecutionTrackingComponentTests.cs`
  - `6d5fcd6160f5` 2025-08-23 xunit v3 preparation (#2723)
    https://github.com/app-vnext/polly/commit/6d5fcd6160f5b4a2291075131ef4e485d1dacdec
- `test/Polly.Core.Tests/Utils/Pipeline/PipelineComponentFactoryTests.cs`
  - `a14508b9d283` 2025-08-22 Reduce async overhead (#2664)
    https://github.com/app-vnext/polly/commit/a14508b9d2839efbd9141305bb3e327ca261dbeb
- `test/Polly.Core.Tests/Utils/Pipeline/PipelineComponentTests.cs`
  - Gozlem araliginda sonraki degisiklik yok
- `test/Polly.Core.Tests/Utils/Pipeline/ReloadablePipelineComponentTests.cs`
  - Gozlem araliginda sonraki degisiklik yok
- `test/Polly.Core.Tests/Utils/RandomUtilTests.cs`
  - `a14508b9d283` 2025-08-22 Reduce async overhead (#2664)
    https://github.com/app-vnext/polly/commit/a14508b9d2839efbd9141305bb3e327ca261dbeb
- `test/Polly.Core.Tests/Utils/StrategyHelperTests.cs`
  - `a14508b9d283` 2025-08-22 Reduce async overhead (#2664)
    https://github.com/app-vnext/polly/commit/a14508b9d2839efbd9141305bb3e327ca261dbeb
- `test/Polly.Core.Tests/Utils/TaskHelperTests.cs`
  - `a14508b9d283` 2025-08-22 Reduce async overhead (#2664)
    https://github.com/app-vnext/polly/commit/a14508b9d2839efbd9141305bb3e327ca261dbeb
- `test/Polly.Core.Tests/Utils/TimeProviderExtensionsTests.cs`
  - `6d5fcd6160f5` 2025-08-23 xunit v3 preparation (#2723)
    https://github.com/app-vnext/polly/commit/6d5fcd6160f5b4a2291075131ef4e485d1dacdec
- `test/Polly.Core.Tests/Utils/TypeNameFormatterTests.cs`
  - Gozlem araliginda sonraki degisiklik yok
- `test/Polly.Core.Tests/Utils/ValidationHelperTests.cs`
  - Gozlem araliginda sonraki degisiklik yok

Karar:

[ KUSUR-GETIRMEDI ]

Not:

[ ]

---

### Ornek SAMPLE-22 — github.com/app-vnext/polly

Commit:

- Kisa SHA: `8eee77abe2a4`
- Yazar tarihi: 2025-08-12
- Mesaj basligi: Refactor project dependencies
- Baglanti: https://github.com/app-vnext/polly/commit/8eee77abe2a42da768cf9f05cdd998595df26c1c

Degisiklik ozeti:

- Degisen toplam dosya: 14
- Degisen `.cs` dosyasi: 1
- Eklenen satir: 45, silinen satir: 60
- Degisen `.cs` dosyalari:
  - `test/Polly.Core.Tests/Retry/RetryResilienceStrategyTests.cs`

Commit'in ilgili C# diff'i:

```diff
--- test/Polly.Core.Tests/Retry/RetryResilienceStrategyTests.cs
--- a/test/Polly.Core.Tests/Retry/RetryResilienceStrategyTests.cs
+++ b/test/Polly.Core.Tests/Retry/RetryResilienceStrategyTests.cs
@@ -2,7 +2,6 @@ using Microsoft.Extensions.Time.Testing;
 using Polly.Retry;
 using Polly.Telemetry;
 using Polly.Testing;
-using Xunit.Sdk;
 
 namespace Polly.Core.Tests.Retry;
 
@@ -256,7 +255,7 @@ public class RetryResilienceStrategyTests
         int generatedValues = 0;
 
         var delay = TimeSpan.Zero;
-        var provider = new ThrowingFakeTimeProvider();
+        var provider = new FakeTimeProvider();
 
         _options.ShouldHandle = _ => PredicateResult.True();
         _options.MaxRetryAttempts = 3;
@@ -289,14 +288,6 @@ public class RetryResilienceStrategyTests
         increment.ShouldBeFalse();
     }
 
-    private sealed class ThrowingFakeTimeProvider : FakeTimeProvider
-    {
-        public override DateTimeOffset GetUtcNow() => throw new XunitException("TimeProvider should not be used.");
-
-        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
-            => throw new XunitException("TimeProvider should not be used.");
-    }
-
     [Fact]
     public async Task OnRetry_EnsureCorrectArguments()
     {
```

Sonraki tarihsel olgular:

- `test/Polly.Core.Tests/Retry/RetryResilienceStrategyTests.cs`
  - `a14508b9d283` 2025-08-22 Reduce async overhead (#2664)
    https://github.com/app-vnext/polly/commit/a14508b9d2839efbd9141305bb3e327ca261dbeb
  - `6d5fcd6160f5` 2025-08-23 xunit v3 preparation (#2723)
    https://github.com/app-vnext/polly/commit/6d5fcd6160f5b4a2291075131ef4e485d1dacdec

Karar:

[ KUSUR-GETIRMEDI ]

Not:

[ ]

---

### Ornek SAMPLE-23 — github.com/jellyfin/jellyfin

Commit:

- Kisa SHA: `dc27d8f9cd3b`
- Yazar tarihi: 2023-10-10
- Mesaj basligi: Refactor URI overrides (#10051)
- Baglanti: https://github.com/jellyfin/jellyfin/commit/dc27d8f9cd3b6cf280ba8e5e2520db387f651fb4

Degisiklik ozeti:

- Degisen toplam dosya: 18
- Degisen `.cs` dosyasi: 18
- Eklenen satir: 431, silinen satir: 276
- Degisen `.cs` dosyalari:
  - `Emby.Dlna/Main/DlnaEntryPoint.cs`
  - `Emby.Server.Implementations/ApplicationHost.cs`
  - `Emby.Server.Implementations/EntryPoints/UdpServerEntryPoint.cs`
  - `Emby.Server.Implementations/Net/SocketFactory.cs`
  - `Emby.Server.Implementations/Udp/UdpServer.cs`
  - `Jellyfin.Networking/Extensions/NetworkExtensions.cs`
  - `Jellyfin.Networking/Manager/NetworkManager.cs`
  - `Jellyfin.Server/Extensions/ApiServiceCollectionExtensions.cs`
  - `Jellyfin.Server/Extensions/WebHostBuilderExtensions.cs`
  - `MediaBrowser.Model/Net/IPData.cs`
  - `MediaBrowser.Model/Net/PublishedServerUriOverride.cs`
  - `RSSDP/SsdpCommunicationsServer.cs`
  - `RSSDP/SsdpDeviceLocator.cs`
  - `RSSDP/SsdpDevicePublisher.cs`
  - `tests/Jellyfin.Networking.Tests/NetworkExtensionsTests.cs`
  - `tests/Jellyfin.Networking.Tests/NetworkManagerTests.cs`
  - `tests/Jellyfin.Networking.Tests/NetworkParseTests.cs`
  - `tests/Jellyfin.Server.Tests/ParseNetworkTests.cs`

Commit'in ilgili C# diff'i:

```diff
--- Emby.Dlna/Main/DlnaEntryPoint.cs
--- a/Emby.Dlna/Main/DlnaEntryPoint.cs
+++ b/Emby.Dlna/Main/DlnaEntryPoint.cs
@@ -201,9 +201,10 @@ namespace Emby.Dlna.Main
             {
                 if (_communicationsServer is null)
                 {
-                    var enableMultiSocketBinding = OperatingSystem.IsWindows() || OperatingSystem.IsLinux();
-
-                    _communicationsServer = new SsdpCommunicationsServer(_socketFactory, _networkManager, _logger, enableMultiSocketBinding)
+                    _communicationsServer = new SsdpCommunicationsServer(
+                        _socketFactory,
+                        _networkManager,
+                        _logger)
                     {
                         IsShared = true
                     };
@@ -213,7 +214,7 @@ namespace Emby.Dlna.Main
             }
             catch (Exception ex)
             {
-                _logger.LogError(ex, "Error starting ssdp handlers");
+                _logger.LogError(ex, "Error starting SSDP handlers");
             }
         }
 
--- Emby.Server.Implementations/ApplicationHost.cs
--- a/Emby.Server.Implementations/ApplicationHost.cs
+++ b/Emby.Server.Implementations/ApplicationHost.cs
@@ -450,7 +450,7 @@ namespace Emby.Server.Implementations
 
             ConfigurationManager.AddParts(GetExports<IConfigurationFactory>());
 
-            NetManager = new NetworkManager(ConfigurationManager, LoggerFactory.CreateLogger<NetworkManager>());
+            NetManager = new NetworkManager(ConfigurationManager, _startupConfig, LoggerFactory.CreateLogger<NetworkManager>());
 
             // Initialize runtime stat collection
             if (ConfigurationManager.Configuration.EnableMetrics)
@@ -913,7 +913,7 @@ namespace Emby.Server.Implementations
         /// <inheritdoc/>
         public string GetSmartApiUrl(HttpRequest request)
         {
-            // Return the host in the HTTP request as the API url
+            // Return the host in the HTTP request as the API URL if not configured otherwise
             if (ConfigurationManager.GetNetworkConfiguration().EnablePublishedServerUriByRequest)
             {
                 int? requestPort = request.Host.Port;
@@ -948,7 +948,7 @@ namespace Emby.Server.Implementations
         public string GetApiUrlForLocalAccess(IPAddress ipAddress = null, bool allowHttps = true)
         {
             // With an empty source, the port will be null
-            var smart = NetManager.GetBindAddress(ipAddress, out _, true);
+            var smart = NetManager.GetBindAddress(ipAddress, out _, false);
             var scheme = !allowHttps ? Uri.UriSchemeHttp : null;
             int? port = !allowHttps ? HttpPort : null;
             return GetLocalApiUrl(smart, scheme, port);
--- Emby.Server.Implementations/EntryPoints/UdpServerEntryPoint.cs
--- a/Emby.Server.Implementations/EntryPoints/UdpServerEntryPoint.cs
+++ b/Emby.Server.Implementations/EntryPoints/UdpServerEntryPoint.cs
@@ -18,7 +18,7 @@ using Microsoft.Extensions.Logging;
 namespace Emby.Server.Implementations.EntryPoints
 {
     /// <summary>
-    /// Class UdpServerEntryPoint.
+    /// Class responsible for registering all UDP broadcast endpoints and their handlers.
     /// </summary>
     public sealed class UdpServerEntryPoint : IServerEntryPoint
     {
@@ -35,7 +35,6 @@ namespace Emby.Server.Implementations.EntryPoints
         private readonly IConfiguration _config;
         private readonly IConfigurationManager _configurationManager;
         private readonly INetworkManager _networkManager;
-        private readonly bool _enableMultiSocketBinding;
 
         /// <summary>
         /// The UDP server.
@@ -65,7 +64,6 @@ namespace Emby.Server.Implementations.EntryPoints
             _configurationManager = configurationManager;
             _networkManager = networkManager;
             _udpServers = new List<UdpServer>();
-            _enableMultiSocketBinding = OperatingSystem.IsWindows() || OperatingSystem.IsLinux();
         }
 
         /// <inheritdoc />
@@ -80,14 +78,16 @@ namespace Emby.Server.Implementations.EntryPoints
 
             try
             {
-                if (_enableMultiSocketBinding)
+                // Linux needs to bind to the broadcast addresses to get broadcast traffic
+                // Windows receives broadcast fine when binding to just the interface, it is unable to bind to broadcast addresses
+                if (OperatingSystem.IsLinux())
                 {
-                    // Add global broadcast socket
+                    // Add global broadcast listener
                     var server = new UdpServer(_logger, _appHost, _config, IPAddress.Broadcast, PortNumber);
                     server.Start(_cancellationTokenSource.Token);
                     _udpServers.Add(server);
 
-                    // Add bind address specific broadcast sockets
+                    // Add bind address specific broadcast listeners
                     // IPv6 is currently unsupported
                     var validInterfaces = _networkManager.GetInternalBindAddresses().Where(i => i.AddressFamily == AddressFamily.InterNetwork);
                     foreach (var intf in validInterfaces)
@@ -102,9 +102,18 @@ namespace Emby.Server.Implementations.EntryPoints
                 }
                 else
                 {
-                    var server = new UdpServer(_logger, _appHost, _config, IPAddress.Any, PortNumber);
-                    server.Start(_cancellationTokenSource.Token);
-                    _udpServers.Add(server);
+                    // Add bind address specific broadcast listeners
+                    // IPv6 is currently unsupported
+                    var validInterfaces = _networkManager.GetInternalBindAddresses().Where(i => i.AddressFamily == AddressFamily.InterNetwork);
+                    foreach (var intf in validInterfaces)
+                    {
+                        var intfAddress = intf.Address;
+                        _logger.LogDebug("Binding UDP server to {Address} on port {PortNumber}", intfAddress, PortNumber);
+
+                        var server = new UdpServer(_logger, _appHost, _config, intfAddress, PortNumber);
+                        server.Start(_cancellationTokenSource.Token);
+                        _udpServers.Add(server);
+                    }
                 }
             }
             catch (SocketException ex)
@@ -119,7 +128,7 @@ namespace Emby.Server.Implementations.EntryPoints
         {
             if (_disposed)
             {
-                throw new ObjectDisposedException(this.GetType().Name);
+                throw new ObjectDisposedException(GetType().Name);
             }
         }
 
--- Emby.Server.Implementations/Net/SocketFactory.cs
--- a/Emby.Server.Implementations/Net/SocketFactory.cs
+++ b/Emby.Server.Implementations/Net/SocketFactory.cs
@@ -1,12 +1,15 @@
-#pragma warning disable CS1591
-
 using System;
+using System.Linq;
 using System.Net;
+using System.Net.NetworkInformation;
 using System.Net.Sockets;
 using MediaBrowser.Model.Net;
 
 namespace Emby.Server.Implementations.Net
 {
+    /// <summary>
+    /// Factory class to create different kinds of sockets.
+    /// </summary>
     public class SocketFactory : ISocketFactory
     {
         /// <inheritdoc />
@@ -38,7 +41,8 @@ namespace Emby.Server.Implementations.Net
         /// <inheritdoc />
         public Socket CreateSsdpUdpSocket(IPData bindInterface, int localPort)
         {
```

Kirpildi; tam diff: https://github.com/jellyfin/jellyfin/commit/dc27d8f9cd3b6cf280ba8e5e2520db387f651fb4

Sonraki tarihsel olgular:

- `Emby.Dlna/Main/DlnaEntryPoint.cs`
  - `effc3d488c2e` 2023-10-11 Use DI for ContentDirectoryService
    https://github.com/jellyfin/jellyfin/commit/effc3d488c2e0df04189b18b8e47905b1f641f24
  - `e0b089a37573` 2023-10-11 Use DI for ConnectionManagerService
    https://github.com/jellyfin/jellyfin/commit/e0b089a37573a02898e179e738d44039df9432bf
  - `010cf2340aca` 2023-10-11 Use DI for MediaReceiverRegistrarService
    https://github.com/jellyfin/jellyfin/commit/010cf2340aca8f21d90fd9c8c8653b9b8d7208b2
- `Emby.Server.Implementations/ApplicationHost.cs`
  - `2b1454530b2a` 2023-10-11 Add DLNA service collection extensions
    https://github.com/jellyfin/jellyfin/commit/2b1454530b2a29db2769c3de4c025d457200303f
  - `b62b0ec2b581` 2023-10-23 Fix warnings
    https://github.com/jellyfin/jellyfin/commit/b62b0ec2b581369de42c69305773f0edb9d701b4
  - `2b742be38e08` 2023-10-26 Use IHostedService for DLNA
    https://github.com/jellyfin/jellyfin/commit/2b742be38e08afea1a1a417c258ac9469dd48a3b
- `Emby.Server.Implementations/EntryPoints/UdpServerEntryPoint.cs`
  - `b62b0ec2b581` 2023-10-23 Fix warnings
    https://github.com/jellyfin/jellyfin/commit/b62b0ec2b581369de42c69305773f0edb9d701b4
  - `9595636d6105` 2023-11-10 Move network utilities to MediaBrowser.Common
    https://github.com/jellyfin/jellyfin/commit/9595636d6105bbe77292d87c7016c21f9df8d4c7
  - `e463dbda47cc` 2023-11-10 Move network configuration to MediaBrowser.Common
    https://github.com/jellyfin/jellyfin/commit/e463dbda47cc51d9e774e867140921f001a3a52a
- `Emby.Server.Implementations/Net/SocketFactory.cs`
  - `e46e3be667c7` 2023-11-09 Remove DLNA socket code
    https://github.com/jellyfin/jellyfin/commit/e46e3be667c76ff9a242d7499aff83d2d10881ed
  - `fc1e27b75490` 2023-11-30 Move SocketFactory to Jellyfin.Networking
    https://github.com/jellyfin/jellyfin/commit/fc1e27b7549014dbf1de16f2805c65f8a624fb2b
    yol: `Emby.Server.Implementations/Net/SocketFactory.cs` -> `src/Jellyfin.Networking/Udp/SocketFactory.cs`
  - `eea676429b60` 2023-11-30 Use file-scoped namespaces in Jellyfin.Networking
    https://github.com/jellyfin/jellyfin/commit/eea676429b603c9a19e098b1a99c6c024af95ec7
- `Emby.Server.Implementations/Udp/UdpServer.cs`
  - `f1ca1dd7cc14` 2023-11-30 Move UdpServerEntryPoint to Jellyfin.Networking
    https://github.com/jellyfin/jellyfin/commit/f1ca1dd7cc14e59938a73a34c2561856d706312b
    yol: `Emby.Server.Implementations/Udp/UdpServer.cs` -> `src/Jellyfin.Networking/Udp/UdpServer.cs`
  - `eea676429b60` 2023-11-30 Use file-scoped namespaces in Jellyfin.Networking
    https://github.com/jellyfin/jellyfin/commit/eea676429b603c9a19e098b1a99c6c024af95ec7
  - `43b32b0d94d4` 2024-01-06 Auto Discovery Cleanup (#10793)
    https://github.com/jellyfin/jellyfin/commit/43b32b0d94d4deef49bbca735dc50447acdb9250
    yol: `src/Jellyfin.Networking/Udp/UdpServer.cs` -> `src/Jellyfin.Networking/AutoDiscoveryHost.cs`
- `Jellyfin.Networking/Extensions/NetworkExtensions.cs`
  - `223b15627029` 2023-11-10 Move network constants to MediaBrowser.Common
    https://github.com/jellyfin/jellyfin/commit/223b15627029054c4d319e113bb0d2a39af890e0
  - `9595636d6105` 2023-11-10 Move network utilities to MediaBrowser.Common
    https://github.com/jellyfin/jellyfin/commit/9595636d6105bbe77292d87c7016c21f9df8d4c7
    yol: `Jellyfin.Networking/Extensions/NetworkExtensions.cs` -> `MediaBrowser.Common/Net/NetworkUtils.cs`
  - `635d67d458e0` 2023-11-14 Revert "Use System.Net.IPNetwork"
    https://github.com/jellyfin/jellyfin/commit/635d67d458e02df53a1b08998ccd3cff16e76ac3
- `Jellyfin.Networking/Manager/NetworkManager.cs`
  - `99e0d46ad93c` 2023-10-23 Use System.Net.IPNetwork
    https://github.com/jellyfin/jellyfin/commit/99e0d46ad93c1f2e62aed67c26b92f256610f1a6
  - `b62b0ec2b581` 2023-10-23 Fix warnings
    https://github.com/jellyfin/jellyfin/commit/b62b0ec2b581369de42c69305773f0edb9d701b4
  - `223b15627029` 2023-11-10 Move network constants to MediaBrowser.Common
    https://github.com/jellyfin/jellyfin/commit/223b15627029054c4d319e113bb0d2a39af890e0
- `Jellyfin.Server/Extensions/ApiServiceCollectionExtensions.cs`
  - `8ada8dbbac6f` 2023-10-16 add policy to the subtitle controller
    https://github.com/jellyfin/jellyfin/commit/8ada8dbbac6fc4f55ad5834f301f5c42139b4171
  - `99e0d46ad93c` 2023-10-23 Use System.Net.IPNetwork
    https://github.com/jellyfin/jellyfin/commit/99e0d46ad93c1f2e62aed67c26b92f256610f1a6
  - `b62b0ec2b581` 2023-10-23 Fix warnings
    https://github.com/jellyfin/jellyfin/commit/b62b0ec2b581369de42c69305773f0edb9d701b4
- `Jellyfin.Server/Extensions/WebHostBuilderExtensions.cs`
  - `a7b2b92f2b22` 2024-05-17 Backport pull request #11671 from jellyfin/release-10.9.z
    https://github.com/jellyfin/jellyfin/commit/a7b2b92f2b22c670356139f44008deee70519e19
  - `9563e4f85ea6` 2024-06-01 Backport pull request #11823 from jellyfin/release-10.9.z
    https://github.com/jellyfin/jellyfin/commit/9563e4f85ea6bdd3410dae2a7d48ea0664fe606c
  - `22d8528d904e` 2024-08-05 Backport pull request #11901 from jellyfin/release-10.9.z
    https://github.com/jellyfin/jellyfin/commit/22d8528d904e69a8e22ba0e6d43dcb58a54bdcf5
- `MediaBrowser.Model/Net/IPData.cs`
  - `99e0d46ad93c` 2023-10-23 Use System.Net.IPNetwork
    https://github.com/jellyfin/jellyfin/commit/99e0d46ad93c1f2e62aed67c26b92f256610f1a6
  - `635d67d458e0` 2023-11-14 Revert "Use System.Net.IPNetwork"
    https://github.com/jellyfin/jellyfin/commit/635d67d458e02df53a1b08998ccd3cff16e76ac3
  - `9e480f6efb4b` 2025-11-11 Update to .NET 10.0
    https://github.com/jellyfin/jellyfin/commit/9e480f6efb4bc0e1f0d1323ed7ed5a7208fded99
- `MediaBrowser.Model/Net/PublishedServerUriOverride.cs`
  - Gozlem araliginda sonraki degisiklik yok
- `RSSDP/SsdpCommunicationsServer.cs`
  - `a8c55ae8fec5` 2023-11-09 Remove RSSDP
    https://github.com/jellyfin/jellyfin/commit/a8c55ae8fec58b3aacd1cf311a8ac9c2dda503ff
- `RSSDP/SsdpDeviceLocator.cs`
  - `a8c55ae8fec5` 2023-11-09 Remove RSSDP
    https://github.com/jellyfin/jellyfin/commit/a8c55ae8fec58b3aacd1cf311a8ac9c2dda503ff
- `RSSDP/SsdpDevicePublisher.cs`
  - `a8c55ae8fec5` 2023-11-09 Remove RSSDP
    https://github.com/jellyfin/jellyfin/commit/a8c55ae8fec58b3aacd1cf311a8ac9c2dda503ff
- `tests/Jellyfin.Networking.Tests/NetworkExtensionsTests.cs`
  - `9595636d6105` 2023-11-10 Move network utilities to MediaBrowser.Common
    https://github.com/jellyfin/jellyfin/commit/9595636d6105bbe77292d87c7016c21f9df8d4c7
  - `fb5da641f418` 2025-01-11 Update dependency FsCheck.Xunit to v3 (#13333)
    https://github.com/jellyfin/jellyfin/commit/fb5da641f418f5e696d3d021fa3638bde456e981
- `tests/Jellyfin.Networking.Tests/NetworkManagerTests.cs`
  - `e463dbda47cc` 2023-11-10 Move network configuration to MediaBrowser.Common
    https://github.com/jellyfin/jellyfin/commit/e463dbda47cc51d9e774e867140921f001a3a52a
- `tests/Jellyfin.Networking.Tests/NetworkParseTests.cs`
  - `9595636d6105` 2023-11-10 Move network utilities to MediaBrowser.Common
    https://github.com/jellyfin/jellyfin/commit/9595636d6105bbe77292d87c7016c21f9df8d4c7
  - `e463dbda47cc` 2023-11-10 Move network configuration to MediaBrowser.Common
    https://github.com/jellyfin/jellyfin/commit/e463dbda47cc51d9e774e867140921f001a3a52a
  - `0fd36a5bf1fc` 2023-11-14 Fix warnings in test projects
    https://github.com/jellyfin/jellyfin/commit/0fd36a5bf1fc87ebbfd5b74585f0c080995c1688
- `tests/Jellyfin.Server.Tests/ParseNetworkTests.cs`
  - `99e0d46ad93c` 2023-10-23 Use System.Net.IPNetwork
    https://github.com/jellyfin/jellyfin/commit/99e0d46ad93c1f2e62aed67c26b92f256610f1a6
  - `e463dbda47cc` 2023-11-10 Move network configuration to MediaBrowser.Common
    https://github.com/jellyfin/jellyfin/commit/e463dbda47cc51d9e774e867140921f001a3a52a
  - `0fd36a5bf1fc` 2023-11-14 Fix warnings in test projects
    https://github.com/jellyfin/jellyfin/commit/0fd36a5bf1fc87ebbfd5b74585f0c080995c1688

Karar:

[ KUSUR-GETIRMEDI ]

Not:

[ ]

---

### Ornek SAMPLE-24 — github.com/sharex/sharex

Commit:

- Kisa SHA: `bfe154eafe1a`
- Yazar tarihi: 2026-07-11
- Mesaj basligi: Remove obsolete code paths and simplify the affected feature
- Baglanti: https://github.com/sharex/sharex/commit/bfe154eafe1a5c44c5684e8a24bde0d1d29dedf6

Degisiklik ozeti:

- Degisen toplam dosya: 22
- Degisen `.cs` dosyasi: 15
- Eklenen satir: 42, silinen satir: 42
- Degisen `.cs` dosyalari:
  - `ShareX.Tools/Features/HashChecker/HashCheckerContracts.cs`
  - `ShareX.Tools/Features/HashChecker/HashCheckerViewModel.cs`
  - `ShareX.Tools/Features/HashChecker/HashCheckerWindow.axaml.cs`
  - `ShareX.Tools/Features/ImageCombiner/ImageCombinerContracts.cs`
  - `ShareX.Tools/Features/ImageCombiner/ImageCombinerViewModel.cs`
  - `ShareX.Tools/Features/ImageCombiner/ImageCombinerWindow.axaml.cs`
  - `ShareX.Tools/Features/QrCode/QrCodeContracts.cs`
  - `ShareX.Tools/Features/QrCode/QrCodeViewModel.cs`
  - `ShareX.Tools/Features/QrCode/QrCodeWindow.axaml.cs`
  - `ShareX.Tools/Features/VideoConverter/VideoConverterContracts.cs`
  - `ShareX.Tools/Features/VideoConverter/VideoConverterViewModel.cs`
  - `ShareX.Tools/Features/VideoConverter/VideoConverterWindow.axaml.cs`
  - `ShareX.Tools/Infrastructure/ViewModelBase.cs`
  - `ShareX.Tools/Integration/ToolsIntegration.cs`
  - `ShareX/TaskHelpers.cs`

Commit'in ilgili C# diff'i:

```diff
--- ShareX.Tools/Features/HashChecker/HashCheckerContracts.cs
--- a/ShareX.Avalonia.Tools/Features/HashChecker/HashCheckerContracts.cs
+++ b/ShareX.Tools/Features/HashChecker/HashCheckerContracts.cs
@@ -23,7 +23,7 @@
 
 #endregion License Information (GPL v3)
 
-namespace ShareX.AvaloniaUI.Tools.Features.HashChecker;
+namespace ShareX.Tools.Features.HashChecker;
 
 public enum HashCheckerAlgorithm
 {
--- ShareX.Tools/Features/HashChecker/HashCheckerViewModel.cs
--- a/ShareX.Avalonia.Tools/Features/HashChecker/HashCheckerViewModel.cs
+++ b/ShareX.Tools/Features/HashChecker/HashCheckerViewModel.cs
@@ -25,9 +25,9 @@
 
 using CommunityToolkit.Mvvm.ComponentModel;
 using CommunityToolkit.Mvvm.Input;
-using ShareX.AvaloniaUI.Tools.Infrastructure;
+using ShareX.Tools.Infrastructure;
 
-namespace ShareX.AvaloniaUI.Tools.Features.HashChecker;
+namespace ShareX.Tools.Features.HashChecker;
 
 public sealed record HashAlgorithmItem(HashCheckerAlgorithm Algorithm, string DisplayName);
 
--- ShareX.Tools/Features/HashChecker/HashCheckerWindow.axaml.cs
--- a/ShareX.Avalonia.Tools/Features/HashChecker/HashCheckerWindow.axaml.cs
+++ b/ShareX.Tools/Features/HashChecker/HashCheckerWindow.axaml.cs
@@ -27,10 +27,10 @@
 using Avalonia.Input;
 using Avalonia.Markup.Xaml;
 using Avalonia.Platform.Storage;
-using ShareX.AvaloniaUI.Tools.Features.HashChecker;
+using ShareX.Tools.Features.HashChecker;
 using ShareX.AvaloniaUI.Theming;
 
-namespace ShareX.AvaloniaUI.Tools.Features.HashChecker;
+namespace ShareX.Tools.Features.HashChecker;
 
 public partial class HashCheckerWindow : Window
 {
--- ShareX.Tools/Features/ImageCombiner/ImageCombinerContracts.cs
--- a/ShareX.Avalonia.Tools/Features/ImageCombiner/ImageCombinerContracts.cs
+++ b/ShareX.Tools/Features/ImageCombiner/ImageCombinerContracts.cs
@@ -23,7 +23,7 @@
 
 #endregion License Information (GPL v3)
 
-namespace ShareX.AvaloniaUI.Tools.Features.ImageCombiner;
+namespace ShareX.Tools.Features.ImageCombiner;
 
 public enum ImageCombinerOrientation
 {
--- ShareX.Tools/Features/ImageCombiner/ImageCombinerViewModel.cs
--- a/ShareX.Avalonia.Tools/Features/ImageCombiner/ImageCombinerViewModel.cs
+++ b/ShareX.Tools/Features/ImageCombiner/ImageCombinerViewModel.cs
@@ -26,10 +26,10 @@
 using Avalonia.Media.Imaging;
 using CommunityToolkit.Mvvm.ComponentModel;
 using CommunityToolkit.Mvvm.Input;
-using ShareX.AvaloniaUI.Tools.Infrastructure;
+using ShareX.Tools.Infrastructure;
 using System.Collections.ObjectModel;
 
-namespace ShareX.AvaloniaUI.Tools.Features.ImageCombiner;
+namespace ShareX.Tools.Features.ImageCombiner;
 
 public sealed partial class ImageCombinerViewModel : ViewModelBase, IDisposable
 {
--- ShareX.Tools/Features/ImageCombiner/ImageCombinerWindow.axaml.cs
--- a/ShareX.Avalonia.Tools/Features/ImageCombiner/ImageCombinerWindow.axaml.cs
+++ b/ShareX.Tools/Features/ImageCombiner/ImageCombinerWindow.axaml.cs
@@ -28,10 +28,10 @@
 using Avalonia.Interactivity;
 using Avalonia.Markup.Xaml;
 using Avalonia.Platform.Storage;
-using ShareX.AvaloniaUI.Tools.Features.ImageCombiner;
+using ShareX.Tools.Features.ImageCombiner;
 using ShareX.AvaloniaUI.Theming;
 
-namespace ShareX.AvaloniaUI.Tools.Features.ImageCombiner;
+namespace ShareX.Tools.Features.ImageCombiner;
 
 public partial class ImageCombinerWindow : Window
 {
--- ShareX.Tools/Features/QrCode/QrCodeContracts.cs
--- a/ShareX.Avalonia.Tools/Features/QrCode/QrCodeContracts.cs
+++ b/ShareX.Tools/Features/QrCode/QrCodeContracts.cs
@@ -23,7 +23,7 @@
 
 #endregion License Information (GPL v3)
 
-namespace ShareX.AvaloniaUI.Tools.Features.QrCode;
+namespace ShareX.Tools.Features.QrCode;
 
 public enum QrCodeScanMode
 {
--- ShareX.Tools/Features/QrCode/QrCodeViewModel.cs
--- a/ShareX.Avalonia.Tools/Features/QrCode/QrCodeViewModel.cs
+++ b/ShareX.Tools/Features/QrCode/QrCodeViewModel.cs
@@ -26,10 +26,10 @@
 using Avalonia.Media.Imaging;
 using CommunityToolkit.Mvvm.ComponentModel;
 using CommunityToolkit.Mvvm.Input;
-using ShareX.AvaloniaUI.Tools.Infrastructure;
+using ShareX.Tools.Infrastructure;
 using System.Text;
 
-namespace ShareX.AvaloniaUI.Tools.Features.QrCode;
+namespace ShareX.Tools.Features.QrCode;
 
 public sealed partial class QrCodeViewModel : ViewModelBase, IDisposable
 {
--- ShareX.Tools/Features/QrCode/QrCodeWindow.axaml.cs
--- a/ShareX.Avalonia.Tools/Features/QrCode/QrCodeWindow.axaml.cs
+++ b/ShareX.Tools/Features/QrCode/QrCodeWindow.axaml.cs
@@ -29,10 +29,10 @@
 using Avalonia.Media;
 using Avalonia.Media.Imaging;
 using Avalonia.Platform.Storage;
-using ShareX.AvaloniaUI.Tools.Features.QrCode;
+using ShareX.Tools.Features.QrCode;
 using ShareX.AvaloniaUI.Theming;
 
-namespace ShareX.AvaloniaUI.Tools.Features.QrCode;
+namespace ShareX.Tools.Features.QrCode;
 
 public partial class QrCodeWindow : Window
 {
--- ShareX.Tools/Features/VideoConverter/VideoConverterContracts.cs
--- a/ShareX.Avalonia.Tools/Features/VideoConverter/VideoConverterContracts.cs
+++ b/ShareX.Tools/Features/VideoConverter/VideoConverterContracts.cs
@@ -23,7 +23,7 @@
 
 #endregion License Information (GPL v3)
 
-namespace ShareX.AvaloniaUI.Tools.Features.VideoConverter;
+namespace ShareX.Tools.Features.VideoConverter;
 
 public enum VideoConverterCodec
 {
--- ShareX.Tools/Features/VideoConverter/VideoConverterViewModel.cs
--- a/ShareX.Avalonia.Tools/Features/VideoConverter/VideoConverterViewModel.cs
+++ b/ShareX.Tools/Features/VideoConverter/VideoConverterViewModel.cs
@@ -25,10 +25,10 @@
 
 using CommunityToolkit.Mvvm.ComponentModel;
 using CommunityToolkit.Mvvm.Input;
-using ShareX.AvaloniaUI.Tools.Infrastructure;
+using ShareX.Tools.Infrastructure;
 using System.Text;
 
-namespace ShareX.AvaloniaUI.Tools.Features.VideoConverter;
+namespace ShareX.Tools.Features.VideoConverter;
 
 public sealed record VideoConverterCodecItem(VideoConverterCodec Codec, string DisplayName);
 
--- ShareX.Tools/Features/VideoConverter/VideoConverterWindow.axaml.cs
```

Kirpildi; tam diff: https://github.com/sharex/sharex/commit/bfe154eafe1a5c44c5684e8a24bde0d1d29dedf6

Sonraki tarihsel olgular:

- `ShareX.Tools/Features/HashChecker/HashCheckerContracts.cs`
  - `0fe0617d62ad` 2026-07-11 Remove obsolete code paths and dead implementations
    https://github.com/sharex/sharex/commit/0fe0617d62ad9a5537685b50f8ce1087d367b57d
    yol: `ShareX.Tools/Features/HashChecker/HashCheckerContracts.cs` -> `ShareX.Tools/HashChecker/HashCheckerContracts.cs`
  - `240e16c3a8f0` 2026-07-11 Flatten ShareX.Tools namespaces for tool windows and view models
    https://github.com/sharex/sharex/commit/240e16c3a8f08d1c5e3695dd77f2656141e6b317
  - `866b31930292` 2026-07-12 Move ShareX.Tools files into Tools subfolder
    https://github.com/sharex/sharex/commit/866b319302929907b026a8633f8afb07cc7361cc
    yol: `ShareX.Tools/HashChecker/HashCheckerContracts.cs` -> `ShareX.Tools/Tools/HashChecker/HashCheckerContracts.cs`
- `ShareX.Tools/Features/HashChecker/HashCheckerViewModel.cs`
  - `0fe0617d62ad` 2026-07-11 Remove obsolete code paths and dead implementations
    https://github.com/sharex/sharex/commit/0fe0617d62ad9a5537685b50f8ce1087d367b57d
    yol: `ShareX.Tools/Features/HashChecker/HashCheckerViewModel.cs` -> `ShareX.Tools/HashChecker/HashCheckerViewModel.cs`
  - `240e16c3a8f0` 2026-07-11 Flatten ShareX.Tools namespaces for tool windows and view models
    https://github.com/sharex/sharex/commit/240e16c3a8f08d1c5e3695dd77f2656141e6b317
  - `866b31930292` 2026-07-12 Move ShareX.Tools files into Tools subfolder
    https://github.com/sharex/sharex/commit/866b319302929907b026a8633f8afb07cc7361cc
    yol: `ShareX.Tools/HashChecker/HashCheckerViewModel.cs` -> `ShareX.Tools/Tools/HashChecker/HashCheckerViewModel.cs`
- `ShareX.Tools/Features/HashChecker/HashCheckerWindow.axaml.cs`
  - `0fe0617d62ad` 2026-07-11 Remove obsolete code paths and dead implementations
    https://github.com/sharex/sharex/commit/0fe0617d62ad9a5537685b50f8ce1087d367b57d
    yol: `ShareX.Tools/Features/HashChecker/HashCheckerWindow.axaml.cs` -> `ShareX.Tools/HashChecker/HashCheckerWindow.axaml.cs`
  - `240e16c3a8f0` 2026-07-11 Flatten ShareX.Tools namespaces for tool windows and view models
    https://github.com/sharex/sharex/commit/240e16c3a8f08d1c5e3695dd77f2656141e6b317
  - `866b31930292` 2026-07-12 Move ShareX.Tools files into Tools subfolder
    https://github.com/sharex/sharex/commit/866b319302929907b026a8633f8afb07cc7361cc
    yol: `ShareX.Tools/HashChecker/HashCheckerWindow.axaml.cs` -> `ShareX.Tools/Tools/HashChecker/HashCheckerWindow.axaml.cs`
- `ShareX.Tools/Features/ImageCombiner/ImageCombinerContracts.cs`
  - `0fe0617d62ad` 2026-07-11 Remove obsolete code paths and dead implementations
    https://github.com/sharex/sharex/commit/0fe0617d62ad9a5537685b50f8ce1087d367b57d
    yol: `ShareX.Tools/Features/ImageCombiner/ImageCombinerContracts.cs` -> `ShareX.Tools/ImageCombiner/ImageCombinerContracts.cs`
  - `240e16c3a8f0` 2026-07-11 Flatten ShareX.Tools namespaces for tool windows and view models
    https://github.com/sharex/sharex/commit/240e16c3a8f08d1c5e3695dd77f2656141e6b317
  - `e38a9e592c72` 2026-07-12 Remove obsolete files and cleanup legacy code
    https://github.com/sharex/sharex/commit/e38a9e592c7257818a0dca7ca3b8503340ab2460
- `ShareX.Tools/Features/ImageCombiner/ImageCombinerViewModel.cs`
  - `0fe0617d62ad` 2026-07-11 Remove obsolete code paths and dead implementations
    https://github.com/sharex/sharex/commit/0fe0617d62ad9a5537685b50f8ce1087d367b57d
    yol: `ShareX.Tools/Features/ImageCombiner/ImageCombinerViewModel.cs` -> `ShareX.Tools/ImageCombiner/ImageCombinerViewModel.cs`
  - `240e16c3a8f0` 2026-07-11 Flatten ShareX.Tools namespaces for tool windows and view models
    https://github.com/sharex/sharex/commit/240e16c3a8f08d1c5e3695dd77f2656141e6b317
  - `e38a9e592c72` 2026-07-12 Remove obsolete files and cleanup legacy code
    https://github.com/sharex/sharex/commit/e38a9e592c7257818a0dca7ca3b8503340ab2460
- `ShareX.Tools/Features/ImageCombiner/ImageCombinerWindow.axaml.cs`
  - `0fe0617d62ad` 2026-07-11 Remove obsolete code paths and dead implementations
    https://github.com/sharex/sharex/commit/0fe0617d62ad9a5537685b50f8ce1087d367b57d
    yol: `ShareX.Tools/Features/ImageCombiner/ImageCombinerWindow.axaml.cs` -> `ShareX.Tools/ImageCombiner/ImageCombinerWindow.axaml.cs`
  - `240e16c3a8f0` 2026-07-11 Flatten ShareX.Tools namespaces for tool windows and view models
    https://github.com/sharex/sharex/commit/240e16c3a8f08d1c5e3695dd77f2656141e6b317
  - `e38a9e592c72` 2026-07-12 Remove obsolete files and cleanup legacy code
    https://github.com/sharex/sharex/commit/e38a9e592c7257818a0dca7ca3b8503340ab2460
- `ShareX.Tools/Features/QrCode/QrCodeContracts.cs`
  - `0fe0617d62ad` 2026-07-11 Remove obsolete code paths and dead implementations
    https://github.com/sharex/sharex/commit/0fe0617d62ad9a5537685b50f8ce1087d367b57d
    yol: `ShareX.Tools/Features/QrCode/QrCodeContracts.cs` -> `ShareX.Tools/QrCode/QrCodeContracts.cs`
  - `87eb418d0ec3` 2026-07-11 Rename QR code tool types and window to QRCode
    https://github.com/sharex/sharex/commit/87eb418d0ec38589eb0e0bcdf4676fe0c2322a27
    yol: `ShareX.Tools/QrCode/QrCodeContracts.cs` -> `ShareX.Tools/QRCode/QRCodeContracts.cs`
  - `240e16c3a8f0` 2026-07-11 Flatten ShareX.Tools namespaces for tool windows and view models
    https://github.com/sharex/sharex/commit/240e16c3a8f08d1c5e3695dd77f2656141e6b317
- `ShareX.Tools/Features/QrCode/QrCodeViewModel.cs`
  - `0fe0617d62ad` 2026-07-11 Remove obsolete code paths and dead implementations
    https://github.com/sharex/sharex/commit/0fe0617d62ad9a5537685b50f8ce1087d367b57d
    yol: `ShareX.Tools/Features/QrCode/QrCodeViewModel.cs` -> `ShareX.Tools/QrCode/QrCodeViewModel.cs`
  - `87eb418d0ec3` 2026-07-11 Rename QR code tool types and window to QRCode
    https://github.com/sharex/sharex/commit/87eb418d0ec38589eb0e0bcdf4676fe0c2322a27
    yol: `ShareX.Tools/QrCode/QrCodeViewModel.cs` -> `ShareX.Tools/QRCode/QRCodeViewModel.cs`
  - `240e16c3a8f0` 2026-07-11 Flatten ShareX.Tools namespaces for tool windows and view models
    https://github.com/sharex/sharex/commit/240e16c3a8f08d1c5e3695dd77f2656141e6b317
- `ShareX.Tools/Features/QrCode/QrCodeWindow.axaml.cs`
  - `0fe0617d62ad` 2026-07-11 Remove obsolete code paths and dead implementations
    https://github.com/sharex/sharex/commit/0fe0617d62ad9a5537685b50f8ce1087d367b57d
    yol: `ShareX.Tools/Features/QrCode/QrCodeWindow.axaml.cs` -> `ShareX.Tools/QrCode/QrCodeWindow.axaml.cs`
  - `87eb418d0ec3` 2026-07-11 Rename QR code tool types and window to QRCode
    https://github.com/sharex/sharex/commit/87eb418d0ec38589eb0e0bcdf4676fe0c2322a27
    yol: `ShareX.Tools/QrCode/QrCodeWindow.axaml.cs` -> `ShareX.Tools/QRCode/QRCodeWindow.axaml.cs`
  - `240e16c3a8f0` 2026-07-11 Flatten ShareX.Tools namespaces for tool windows and view models
    https://github.com/sharex/sharex/commit/240e16c3a8f08d1c5e3695dd77f2656141e6b317
- `ShareX.Tools/Features/VideoConverter/VideoConverterContracts.cs`
  - `0fe0617d62ad` 2026-07-11 Remove obsolete code paths and dead implementations
    https://github.com/sharex/sharex/commit/0fe0617d62ad9a5537685b50f8ce1087d367b57d
    yol: `ShareX.Tools/Features/VideoConverter/VideoConverterContracts.cs` -> `ShareX.Tools/VideoConverter/VideoConverterContracts.cs`
  - `240e16c3a8f0` 2026-07-11 Flatten ShareX.Tools namespaces for tool windows and view models
    https://github.com/sharex/sharex/commit/240e16c3a8f08d1c5e3695dd77f2656141e6b317
  - `e38a9e592c72` 2026-07-12 Remove obsolete files and cleanup legacy code
    https://github.com/sharex/sharex/commit/e38a9e592c7257818a0dca7ca3b8503340ab2460
- `ShareX.Tools/Features/VideoConverter/VideoConverterViewModel.cs`
  - `0fe0617d62ad` 2026-07-11 Remove obsolete code paths and dead implementations
    https://github.com/sharex/sharex/commit/0fe0617d62ad9a5537685b50f8ce1087d367b57d
    yol: `ShareX.Tools/Features/VideoConverter/VideoConverterViewModel.cs` -> `ShareX.Tools/VideoConverter/VideoConverterViewModel.cs`
  - `240e16c3a8f0` 2026-07-11 Flatten ShareX.Tools namespaces for tool windows and view models
    https://github.com/sharex/sharex/commit/240e16c3a8f08d1c5e3695dd77f2656141e6b317
  - `e38a9e592c72` 2026-07-12 Remove obsolete files and cleanup legacy code
    https://github.com/sharex/sharex/commit/e38a9e592c7257818a0dca7ca3b8503340ab2460
- `ShareX.Tools/Features/VideoConverter/VideoConverterWindow.axaml.cs`
  - `0fe0617d62ad` 2026-07-11 Remove obsolete code paths and dead implementations
    https://github.com/sharex/sharex/commit/0fe0617d62ad9a5537685b50f8ce1087d367b57d
    yol: `ShareX.Tools/Features/VideoConverter/VideoConverterWindow.axaml.cs` -> `ShareX.Tools/VideoConverter/VideoConverterWindow.axaml.cs`
  - `240e16c3a8f0` 2026-07-11 Flatten ShareX.Tools namespaces for tool windows and view models
    https://github.com/sharex/sharex/commit/240e16c3a8f08d1c5e3695dd77f2656141e6b317
  - `e38a9e592c72` 2026-07-12 Remove obsolete files and cleanup legacy code
    https://github.com/sharex/sharex/commit/e38a9e592c7257818a0dca7ca3b8503340ab2460
- `ShareX.Tools/Infrastructure/ViewModelBase.cs`
  - Gozlem araliginda sonraki degisiklik yok
- `ShareX.Tools/Integration/ToolsIntegration.cs`
  - `0fe0617d62ad` 2026-07-11 Remove obsolete code paths and dead implementations
    https://github.com/sharex/sharex/commit/0fe0617d62ad9a5537685b50f8ce1087d367b57d
  - `87eb418d0ec3` 2026-07-11 Rename QR code tool types and window to QRCode
    https://github.com/sharex/sharex/commit/87eb418d0ec38589eb0e0bcdf4676fe0c2322a27
  - `da71d286e050` 2026-07-11 Remove obsolete code and simplify ShareX internals
    https://github.com/sharex/sharex/commit/da71d286e0500b6a0c8d1a5a9f91d52e358d9a08
- `ShareX/TaskHelpers.cs`
  - `0fe0617d62ad` 2026-07-11 Remove obsolete code paths and dead implementations
    https://github.com/sharex/sharex/commit/0fe0617d62ad9a5537685b50f8ce1087d367b57d
  - `87eb418d0ec3` 2026-07-11 Rename QR code tool types and window to QRCode
    https://github.com/sharex/sharex/commit/87eb418d0ec38589eb0e0bcdf4676fe0c2322a27
  - `da71d286e050` 2026-07-11 Remove obsolete code and simplify ShareX internals
    https://github.com/sharex/sharex/commit/da71d286e0500b6a0c8d1a5a9f91d52e358d9a08

Karar:

[ KUSUR-GETIRMEDI ]

Not:

[ ]

---

### Ornek SAMPLE-25 — github.com/sharex/sharex

Commit:

- Kisa SHA: `ca4951996860`
- Yazar tarihi: 2023-06-04
- Mesaj basligi: Code refactoring
- Baglanti: https://github.com/sharex/sharex/commit/ca4951996860b0b7bb44a4ad73b530ac4bc379c2

Degisiklik ozeti:

- Degisen toplam dosya: 74
- Degisen `.cs` dosyasi: 55
- Eklenen satir: 299, silinen satir: 276
- Degisen `.cs` dosyalari:
  - `ShareX.HelpersLib/Extensions/Extensions.cs`
  - `ShareX.HelpersLib/Forms/ColorPickerForm.Designer.cs`
  - `ShareX.HelpersLib/Forms/DNSChangerForm.Designer.cs`
  - `ShareX.HelpersLib/Forms/DNSChangerForm.cs`
  - `ShareX.HelpersLib/Forms/ErrorForm.Designer.cs`
  - `ShareX.HelpersLib/Forms/ErrorForm.cs`
  - `ShareX.HelpersLib/Forms/GradientPickerForm.Designer.cs`
  - `ShareX.HelpersLib/Forms/GradientPickerForm.cs`
  - `ShareX.HelpersLib/Forms/PrintForm.Designer.cs`
  - `ShareX.HelpersLib/Forms/PrintForm.cs`
  - `ShareX.ImageEffectsLib/Forms/ImageEffectsForm.Designer.cs`
  - `ShareX.ImageEffectsLib/Forms/ImageEffectsForm.cs`
  - `ShareX.ScreenCaptureLib/Forms/CanvasSizeForm.Designer.cs`
  - `ShareX.ScreenCaptureLib/Forms/CanvasSizeForm.cs`
  - `ShareX.ScreenCaptureLib/Forms/EditorStartupForm.Designer.cs`
  - `ShareX.ScreenCaptureLib/Forms/EditorStartupForm.cs`
  - `ShareX.ScreenCaptureLib/Forms/ImageSizeForm.Designer.cs`
  - `ShareX.ScreenCaptureLib/Forms/ImageSizeForm.cs`
  - `ShareX.ScreenCaptureLib/Forms/NewImageForm.Designer.cs`
  - `ShareX.ScreenCaptureLib/Forms/NewImageForm.cs`
  - `ShareX.ScreenCaptureLib/Forms/ScrollingCaptureOptionsForm.Designer.cs`
  - `ShareX.ScreenCaptureLib/Forms/ScrollingCaptureOptionsForm.cs`
  - `ShareX.ScreenCaptureLib/Forms/TextDrawingInputBox.Designer.cs`
  - `ShareX.ScreenCaptureLib/Forms/TextDrawingInputBox.cs`
  - `ShareX.UploadersLib/Forms/EmailForm.Designer.cs`
  - `ShareX.UploadersLib/Forms/EmailForm.cs`
  - `ShareX.UploadersLib/Forms/JiraUpload.Designer.cs`
  - `ShareX.UploadersLib/Forms/JiraUpload.cs`
  - `ShareX.UploadersLib/Forms/TextUploadForm.Designer.cs`
  - `ShareX.UploadersLib/Forms/TwitterTweetForm.Designer.cs`
  - `ShareX.UploadersLib/Forms/TwitterTweetForm.cs`
  - `ShareX/Forms/AboutForm.Designer.cs`
  - `ShareX/Forms/AboutForm.cs`
  - `ShareX/Forms/ActionsForm.Designer.cs`
  - `ShareX/Forms/ActionsForm.cs`
  - `ShareX/Forms/AfterCaptureForm.Designer.cs`
  - `ShareX/Forms/AfterCaptureForm.cs`
  - `ShareX/Forms/AfterUploadForm.cs`
  - `ShareX/Forms/AfterUploadForm.designer.cs`
  - `ShareX/Forms/BeforeUploadForm.Designer.cs`
  - `ShareX/Forms/BeforeUploadForm.cs`
  - `ShareX/Forms/ClipboardFormatForm.Designer.cs`
  - `ShareX/Forms/ClipboardFormatForm.cs`
  - `ShareX/Forms/ClipboardUploadForm.Designer.cs`
  - `ShareX/Forms/ClipboardUploadForm.cs`
  - `ShareX/Forms/FirstTimeUploadForm.Designer.cs`
  - `ShareX/Forms/FirstTimeUploadForm.cs`
  - `ShareX/Forms/QuickTaskInfoEditForm.Designer.cs`
  - `ShareX/Forms/QuickTaskInfoEditForm.cs`
  - `ShareX/Forms/QuickTaskMenuEditorForm.Designer.cs`
  - `ShareX/Forms/QuickTaskMenuEditorForm.cs`
  - `ShareX/Forms/WatchFolderForm.Designer.cs`
  - `ShareX/Forms/WatchFolderForm.cs`
  - `ShareX/Tools/PinToScreen/PinToScreenStartupForm.Designer.cs`
  - `ShareX/Tools/PinToScreen/PinToScreenStartupForm.cs`

Commit'in ilgili C# diff'i:

```diff
--- ShareX.HelpersLib/Extensions/Extensions.cs
--- a/ShareX.HelpersLib/Extensions/Extensions.cs
+++ b/ShareX.HelpersLib/Extensions/Extensions.cs
@@ -940,6 +940,7 @@ public static void CloseOnEscape(this Form form)
             {
                 if (e.KeyCode == Keys.Escape)
                 {
+                    form.DialogResult = DialogResult.Cancel;
                     form.Close();
                 }
             };
--- ShareX.HelpersLib/Forms/ColorPickerForm.Designer.cs
--- a/ShareX.HelpersLib/Forms/ColorPickerForm.Designer.cs
+++ b/ShareX.HelpersLib/Forms/ColorPickerForm.Designer.cs
@@ -70,6 +70,7 @@ private void InitializeComponent()
             this.ttMain = new System.Windows.Forms.ToolTip(this.components);
             this.btnScreenColorPicker = new System.Windows.Forms.Button();
             this.btnClipboardColorPicker = new System.Windows.Forms.Button();
+            this.cbTransparent = new ShareX.HelpersLib.ColorButton();
             this.cmsCopy = new System.Windows.Forms.ContextMenuStrip(this.components);
             this.tsmiCopyAll = new System.Windows.Forms.ToolStripMenuItem();
             this.tsmiCopyRGB = new System.Windows.Forms.ToolStripMenuItem();
@@ -93,7 +94,6 @@ private void InitializeComponent()
             this.lblNameValue = new System.Windows.Forms.Label();
             this.btnClipboardStatus = new System.Windows.Forms.Button();
             this.mbCopy = new ShareX.HelpersLib.MenuButton();
-            this.cbTransparent = new ShareX.HelpersLib.ColorButton();
             this.pbColorPreview = new ShareX.HelpersLib.MyPictureBox();
             this.colorPicker = new ShareX.HelpersLib.ColorPicker();
             ((System.ComponentModel.ISupportInitialize)(this.nudKey)).BeginInit();
@@ -114,7 +114,6 @@ private void InitializeComponent()
             // 
             // btnCancel
             // 
-            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
             resources.ApplyResources(this.btnCancel, "btnCancel");
             this.btnCancel.Name = "btnCancel";
             this.btnCancel.UseVisualStyleBackColor = true;
@@ -437,6 +436,17 @@ private void InitializeComponent()
             this.btnClipboardColorPicker.UseVisualStyleBackColor = true;
             this.btnClipboardColorPicker.Click += new System.EventHandler(this.btnClipboardColorPicker_Click);
             // 
+            // cbTransparent
+            // 
+            this.cbTransparent.Color = System.Drawing.Color.Transparent;
+            this.cbTransparent.ColorPickerOptions = null;
+            resources.ApplyResources(this.cbTransparent, "cbTransparent");
+            this.cbTransparent.ManualButtonClick = true;
+            this.cbTransparent.Name = "cbTransparent";
+            this.ttMain.SetToolTip(this.cbTransparent, resources.GetString("cbTransparent.ToolTip"));
+            this.cbTransparent.UseVisualStyleBackColor = true;
+            this.cbTransparent.Click += new System.EventHandler(this.cbTransparent_Click);
+            // 
             // cmsCopy
             // 
             this.cmsCopy.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
@@ -588,16 +598,6 @@ private void InitializeComponent()
             this.mbCopy.Name = "mbCopy";
             this.mbCopy.UseVisualStyleBackColor = true;
             // 
-            // cbTransparent
-            // 
-            this.cbTransparent.Color = System.Drawing.Color.Transparent;
-            resources.ApplyResources(this.cbTransparent, "cbTransparent");
-            this.cbTransparent.ManualButtonClick = true;
-            this.cbTransparent.Name = "cbTransparent";
-            this.ttMain.SetToolTip(this.cbTransparent, resources.GetString("cbTransparent.ToolTip"));
-            this.cbTransparent.UseVisualStyleBackColor = true;
-            this.cbTransparent.Click += new System.EventHandler(this.cbTransparent_Click);
-            // 
             // pbColorPreview
             // 
             this.pbColorPreview.BackColor = System.Drawing.SystemColors.Window;
@@ -620,7 +620,6 @@ private void InitializeComponent()
             resources.ApplyResources(this, "$this");
             this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
             this.BackColor = System.Drawing.SystemColors.Window;
-            this.CancelButton = this.btnCancel;
             this.Controls.Add(this.btnClipboardStatus);
             this.Controls.Add(this.btnClipboardColorPicker);
             this.Controls.Add(this.lblName);
--- ShareX.HelpersLib/Forms/DNSChangerForm.Designer.cs
--- a/ShareX.HelpersLib/Forms/DNSChangerForm.Designer.cs
+++ b/ShareX.HelpersLib/Forms/DNSChangerForm.Designer.cs
@@ -88,7 +88,6 @@ private void InitializeComponent()
             // 
             // btnCancel
             // 
-            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
             resources.ApplyResources(this.btnCancel, "btnCancel");
             this.btnCancel.Name = "btnCancel";
             this.btnCancel.UseVisualStyleBackColor = true;
--- ShareX.HelpersLib/Forms/DNSChangerForm.cs
--- a/ShareX.HelpersLib/Forms/DNSChangerForm.cs
+++ b/ShareX.HelpersLib/Forms/DNSChangerForm.cs
@@ -145,6 +145,16 @@ private void UpdateControls()
             }
         }
 
+        private async void btnPingPrimary_Click(object sender, EventArgs e)
+        {
+            await SendPing(txtPreferredDNS.Text);
+        }
+
+        private async void btnPingSecondary_Click(object sender, EventArgs e)
+        {
+            await SendPing(txtAlternateDNS.Text);
+        }
+
         private void btnSave_Click(object sender, EventArgs e)
         {
             if (cbAdapters.SelectedItem is AdapterInfo adapter)
@@ -195,17 +205,8 @@ private void btnSave_Click(object sender, EventArgs e)
 
         private void btnCancel_Click(object sender, EventArgs e)
         {
+            DialogResult = DialogResult.Cancel;
             Close();
         }
-
-        private async void btnPingPrimary_Click(object sender, EventArgs e)
-        {
-            await SendPing(txtPreferredDNS.Text);
-        }
-
-        private async void btnPingSecondary_Click(object sender, EventArgs e)
-        {
-            await SendPing(txtAlternateDNS.Text);
-        }
     }
 }
--- ShareX.HelpersLib/Forms/ErrorForm.Designer.cs
--- a/ShareX.HelpersLib/Forms/ErrorForm.Designer.cs
+++ b/ShareX.HelpersLib/Forms/ErrorForm.Designer.cs
@@ -105,10 +105,8 @@ private void InitializeComponent()
             // 
             this.AcceptButton = this.btnContinue;
             resources.ApplyResources(this, "$this");
-            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
             this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
             this.BackColor = System.Drawing.SystemColors.Window;
-            this.CancelButton = this.btnClose;
             this.Controls.Add(this.lblErrorMessage);
             this.Controls.Add(this.flpMenu);
             this.Controls.Add(this.txtException);
--- ShareX.HelpersLib/Forms/ErrorForm.cs
--- a/ShareX.HelpersLib/Forms/ErrorForm.cs
+++ b/ShareX.HelpersLib/Forms/ErrorForm.cs
@@ -42,7 +42,7 @@ public ErrorForm(Exception error, string logFilePath, string bugReportPath) : th
         public ErrorForm(string errorTitle, string errorMessage, string logFilePath, string bugReportPath, bool unhandledException = true)
         {
             InitializeComponent();
-            ShareXResources.ApplyTheme(this);
+            ShareXResources.ApplyTheme(this, true);
 
             IsUnhandledException = unhandledException;
             LogFilePath = logFilePath;
--- ShareX.HelpersLib/Forms/GradientPickerForm.Designer.cs
--- a/ShareX.HelpersLib/Forms/GradientPickerForm.Designer.cs
+++ b/ShareX.HelpersLib/Forms/GradientPickerForm.Designer.cs
```

Kirpildi; tam diff: https://github.com/sharex/sharex/commit/ca4951996860b0b7bb44a4ad73b530ac4bc379c2

Sonraki tarihsel olgular:

- `ShareX.HelpersLib/Extensions/Extensions.cs`
  - `af260ba6f1d9` 2023-07-22 Added SelectTabWithoutFocus extension
    https://github.com/sharex/sharex/commit/af260ba6f1d95fb78a373e4e072b161c7091eece
  - `63ae0947acc3` 2023-07-22 Added FormExtensions
    https://github.com/sharex/sharex/commit/63ae0947acc360fcd1bd62f5735f9263125b6bb1
  - `077e74a2d417` 2024-01-02 Update year
    https://github.com/sharex/sharex/commit/077e74a2d4173d6315977e57e20fb6203d3b7078
- `ShareX.HelpersLib/Forms/ColorPickerForm.Designer.cs`
  - `eff518c13eb0` 2026-07-30 Remove obsolete code and files
    https://github.com/sharex/sharex/commit/eff518c13eb03743a1ef2d33becb9a7a16fbdfc6
- `ShareX.HelpersLib/Forms/DNSChangerForm.Designer.cs`
  - `251cd02c8ec8` 2025-03-17 Removed DNS changer tool
    https://github.com/sharex/sharex/commit/251cd02c8ec85c1a87e6987991205b83a11034b3
- `ShareX.HelpersLib/Forms/DNSChangerForm.cs`
  - `077e74a2d417` 2024-01-02 Update year
    https://github.com/sharex/sharex/commit/077e74a2d4173d6315977e57e20fb6203d3b7078
  - `9db8d8608b65` 2025-01-08 Update year
    https://github.com/sharex/sharex/commit/9db8d8608b65bb42b91f2e73f1946b674c60fd96
  - `251cd02c8ec8` 2025-03-17 Removed DNS changer tool
    https://github.com/sharex/sharex/commit/251cd02c8ec85c1a87e6987991205b83a11034b3
- `ShareX.HelpersLib/Forms/ErrorForm.Designer.cs`
  - `a92bafb72524` 2024-03-28 Code refactoring
    https://github.com/sharex/sharex/commit/a92bafb7252425921df0922aba1b3e555630dd0f
  - `021e6834ae38` 2026-07-23 Remove obsolete code and files
    https://github.com/sharex/sharex/commit/021e6834ae38682db348e7379f9becebbf2b2c15
- `ShareX.HelpersLib/Forms/ErrorForm.cs`
  - `077e74a2d417` 2024-01-02 Update year
    https://github.com/sharex/sharex/commit/077e74a2d4173d6315977e57e20fb6203d3b7078
  - `9db8d8608b65` 2025-01-08 Update year
    https://github.com/sharex/sharex/commit/9db8d8608b65bb42b91f2e73f1946b674c60fd96
  - `cfc08a16d9f0` 2026-01-19 Update year
    https://github.com/sharex/sharex/commit/cfc08a16d9f094d9f079fcaeb46911fd44ed0d19
- `ShareX.HelpersLib/Forms/GradientPickerForm.Designer.cs`
  - `d095da0e09ac` 2026-07-29 Remove obsolete files and code
    https://github.com/sharex/sharex/commit/d095da0e09ac7e48a2d6c62acf8c564f97c01ac7
- `ShareX.HelpersLib/Forms/GradientPickerForm.cs`
  - `8e08f2862033` 2023-06-07 Added gradient live preview to ImageBeautifierForm
    https://github.com/sharex/sharex/commit/8e08f286203311ec5bb9d5b4aa658025b58e8710
  - `077e74a2d417` 2024-01-02 Update year
    https://github.com/sharex/sharex/commit/077e74a2d4173d6315977e57e20fb6203d3b7078
  - `9db8d8608b65` 2025-01-08 Update year
    https://github.com/sharex/sharex/commit/9db8d8608b65bb42b91f2e73f1946b674c60fd96
- `ShareX.HelpersLib/Forms/PrintForm.Designer.cs`
  - `902c0c8d66fe` 2026-07-24 Remove obsolete project files
    https://github.com/sharex/sharex/commit/902c0c8d66fe1072591e7f57838b627175192e2d
- `ShareX.HelpersLib/Forms/PrintForm.cs`
  - `077e74a2d417` 2024-01-02 Update year
    https://github.com/sharex/sharex/commit/077e74a2d4173d6315977e57e20fb6203d3b7078
  - `9db8d8608b65` 2025-01-08 Update year
    https://github.com/sharex/sharex/commit/9db8d8608b65bb42b91f2e73f1946b674c60fd96
  - `cfc08a16d9f0` 2026-01-19 Update year
    https://github.com/sharex/sharex/commit/cfc08a16d9f094d9f079fcaeb46911fd44ed0d19
- `ShareX.ImageEffectsLib/Forms/ImageEffectsForm.Designer.cs`
  - `a42f608eba4a` 2026-07-13 Clean up obsolete files and reduce project clutter
    https://github.com/sharex/sharex/commit/a42f608eba4a35e29c31fb81a15bfb91187c1dc7
- `ShareX.ImageEffectsLib/Forms/ImageEffectsForm.cs`
  - `077e74a2d417` 2024-01-02 Update year
    https://github.com/sharex/sharex/commit/077e74a2d4173d6315977e57e20fb6203d3b7078
  - `9db8d8608b65` 2025-01-08 Update year
    https://github.com/sharex/sharex/commit/9db8d8608b65bb42b91f2e73f1946b674c60fd96
  - `262970d344a9` 2025-05-22 Removed "Use custom theme" checkbox
    https://github.com/sharex/sharex/commit/262970d344a9ff6d411aae1872afb2f67746d2dc
- `ShareX.ScreenCaptureLib/Forms/CanvasSizeForm.Designer.cs`
  - `351e25a06c95` 2026-08-20 Refactor project files and remove obsolete code
    https://github.com/sharex/sharex/commit/351e25a06c95568f52b98abf720b3af5f0271d0d
- `ShareX.ScreenCaptureLib/Forms/CanvasSizeForm.cs`
  - `077e74a2d417` 2024-01-02 Update year
    https://github.com/sharex/sharex/commit/077e74a2d4173d6315977e57e20fb6203d3b7078
  - `9db8d8608b65` 2025-01-08 Update year
    https://github.com/sharex/sharex/commit/9db8d8608b65bb42b91f2e73f1946b674c60fd96
  - `cfc08a16d9f0` 2026-01-19 Update year
    https://github.com/sharex/sharex/commit/cfc08a16d9f094d9f079fcaeb46911fd44ed0d19
- `ShareX.ScreenCaptureLib/Forms/EditorStartupForm.Designer.cs`
  - `dd7e1bb0ad65` 2023-11-08 Added "Load image from URL" button to EditorStartupForm
    https://github.com/sharex/sharex/commit/dd7e1bb0ad6508e41a4ce571da80070aff29c7e1
  - `5a855c00d217` 2026-04-18 Update EditorStartupForm
    https://github.com/sharex/sharex/commit/5a855c00d217d046a343873c1ed695c65156793f
  - `351e25a06c95` 2026-08-20 Refactor project files and remove obsolete code
    https://github.com/sharex/sharex/commit/351e25a06c95568f52b98abf720b3af5f0271d0d
- `ShareX.ScreenCaptureLib/Forms/EditorStartupForm.cs`
  - `dd7e1bb0ad65` 2023-11-08 Added "Load image from URL" button to EditorStartupForm
    https://github.com/sharex/sharex/commit/dd7e1bb0ad6508e41a4ce571da80070aff29c7e1
  - `077e74a2d417` 2024-01-02 Update year
    https://github.com/sharex/sharex/commit/077e74a2d4173d6315977e57e20fb6203d3b7078
  - `9db8d8608b65` 2025-01-08 Update year
    https://github.com/sharex/sharex/commit/9db8d8608b65bb42b91f2e73f1946b674c60fd96
- `ShareX.ScreenCaptureLib/Forms/ImageSizeForm.Designer.cs`
  - `351e25a06c95` 2026-08-20 Refactor project files and remove obsolete code
    https://github.com/sharex/sharex/commit/351e25a06c95568f52b98abf720b3af5f0271d0d
- `ShareX.ScreenCaptureLib/Forms/ImageSizeForm.cs`
  - `077e74a2d417` 2024-01-02 Update year
    https://github.com/sharex/sharex/commit/077e74a2d4173d6315977e57e20fb6203d3b7078
  - `bd93e8b4b096` 2024-11-22 fixed #7686: Use SetValue
    https://github.com/sharex/sharex/commit/bd93e8b4b0963f644e139946d478ceff93ca9f8c
  - `9db8d8608b65` 2025-01-08 Update year
    https://github.com/sharex/sharex/commit/9db8d8608b65bb42b91f2e73f1946b674c60fd96
- `ShareX.ScreenCaptureLib/Forms/NewImageForm.Designer.cs`
  - `351e25a06c95` 2026-08-20 Refactor project files and remove obsolete code
    https://github.com/sharex/sharex/commit/351e25a06c95568f52b98abf720b3af5f0271d0d
- `ShareX.ScreenCaptureLib/Forms/NewImageForm.cs`
  - `077e74a2d417` 2024-01-02 Update year
    https://github.com/sharex/sharex/commit/077e74a2d4173d6315977e57e20fb6203d3b7078
  - `bd93e8b4b096` 2024-11-22 fixed #7686: Use SetValue
    https://github.com/sharex/sharex/commit/bd93e8b4b0963f644e139946d478ceff93ca9f8c
  - `9db8d8608b65` 2025-01-08 Update year
    https://github.com/sharex/sharex/commit/9db8d8608b65bb42b91f2e73f1946b674c60fd96
- `ShareX.ScreenCaptureLib/Forms/ScrollingCaptureOptionsForm.Designer.cs`
  - `6ffac8a2cf07` 2024-05-24 Added "Auto ignore bottom edge" option
    https://github.com/sharex/sharex/commit/6ffac8a2cf077b390293d1c8afa2505dc2c3f251
  - `40fc9daeccc5` 2024-08-12 Make forms localizable
    https://github.com/sharex/sharex/commit/40fc9daeccc5c06d50f0a7b482bc9cc825cf71b5
  - `01617b3c8d61` 2025-06-08 Added "Scroll method" option to scrolling capture
    https://github.com/sharex/sharex/commit/01617b3c8d61eee8d6b7f466d20ad0f2c2f8a8f6
- `ShareX.ScreenCaptureLib/Forms/ScrollingCaptureOptionsForm.cs`
  - `077e74a2d417` 2024-01-02 Update year
    https://github.com/sharex/sharex/commit/077e74a2d4173d6315977e57e20fb6203d3b7078
  - `10c6f5e8a791` 2024-01-17 Support closing windows with "Escape" key
    https://github.com/sharex/sharex/commit/10c6f5e8a791dcc04d2964b736b524f9967416e1
  - `6ffac8a2cf07` 2024-05-24 Added "Auto ignore bottom edge" option
    https://github.com/sharex/sharex/commit/6ffac8a2cf077b390293d1c8afa2505dc2c3f251
- `ShareX.ScreenCaptureLib/Forms/TextDrawingInputBox.Designer.cs`
  - `351e25a06c95` 2026-08-20 Refactor project files and remove obsolete code
    https://github.com/sharex/sharex/commit/351e25a06c95568f52b98abf720b3af5f0271d0d
- `ShareX.ScreenCaptureLib/Forms/TextDrawingInputBox.cs`
  - `077e74a2d417` 2024-01-02 Update year
    https://github.com/sharex/sharex/commit/077e74a2d4173d6315977e57e20fb6203d3b7078
  - `9db8d8608b65` 2025-01-08 Update year
    https://github.com/sharex/sharex/commit/9db8d8608b65bb42b91f2e73f1946b674c60fd96
  - `cfc08a16d9f0` 2026-01-19 Update year
    https://github.com/sharex/sharex/commit/cfc08a16d9f094d9f079fcaeb46911fd44ed0d19
- `ShareX.UploadersLib/Forms/EmailForm.Designer.cs`
  - `0d5c1ee8c8c2` 2026-07-28 Remove obsolete code and simplify project structure
    https://github.com/sharex/sharex/commit/0d5c1ee8c8c29bddffca55bab074a21b802abe2c
- `ShareX.UploadersLib/Forms/EmailForm.cs`
  - `077e74a2d417` 2024-01-02 Update year
    https://github.com/sharex/sharex/commit/077e74a2d4173d6315977e57e20fb6203d3b7078
  - `9db8d8608b65` 2025-01-08 Update year
    https://github.com/sharex/sharex/commit/9db8d8608b65bb42b91f2e73f1946b674c60fd96
  - `cfc08a16d9f0` 2026-01-19 Update year
    https://github.com/sharex/sharex/commit/cfc08a16d9f094d9f079fcaeb46911fd44ed0d19
- `ShareX.UploadersLib/Forms/JiraUpload.Designer.cs`
  - `5f00f67c0abe` 2026-01-31 Removed MEGA file uploader due to unmaintained library
    https://github.com/sharex/sharex/commit/5f00f67c0abe26cec2b579d0efd11df7fb0b4557
- `ShareX.UploadersLib/Forms/JiraUpload.cs`
  - `077e74a2d417` 2024-01-02 Update year
    https://github.com/sharex/sharex/commit/077e74a2d4173d6315977e57e20fb6203d3b7078
  - `9db8d8608b65` 2025-01-08 Update year
    https://github.com/sharex/sharex/commit/9db8d8608b65bb42b91f2e73f1946b674c60fd96
  - `cfc08a16d9f0` 2026-01-19 Update year
    https://github.com/sharex/sharex/commit/cfc08a16d9f094d9f079fcaeb46911fd44ed0d19
- `ShareX.UploadersLib/Forms/TextUploadForm.Designer.cs`
  - `452f758ce82c` 2026-07-21 Remove obsolete code and simplify project structure
    https://github.com/sharex/sharex/commit/452f758ce82c64b6fd01c3220ba923e4d348ae98
- `ShareX.UploadersLib/Forms/TwitterTweetForm.Designer.cs`
  - `13d03979bf21` 2025-07-02 fixed #7998: Removed Twitter/X
    https://github.com/sharex/sharex/commit/13d03979bf210eb168ba731ae66885a6602c1219
- `ShareX.UploadersLib/Forms/TwitterTweetForm.cs`
  - `077e74a2d417` 2024-01-02 Update year
    https://github.com/sharex/sharex/commit/077e74a2d4173d6315977e57e20fb6203d3b7078
  - `9db8d8608b65` 2025-01-08 Update year
    https://github.com/sharex/sharex/commit/9db8d8608b65bb42b91f2e73f1946b674c60fd96
  - `13d03979bf21` 2025-07-02 fixed #7998: Removed Twitter/X
    https://github.com/sharex/sharex/commit/13d03979bf210eb168ba731ae66885a6602c1219
- `ShareX/Forms/AboutForm.Designer.cs`
  - `fd9618bec04b` 2025-06-29 About form changes
    https://github.com/sharex/sharex/commit/fd9618bec04b4410dc8828af8a4291d236eb370c
  - `f65ba7b2404a` 2026-07-15 Refactor AboutForm to AboutWindow with Avalonia UI integration
    https://github.com/sharex/sharex/commit/f65ba7b2404a99e65bdafb650e696b888094bce1
- `ShareX/Forms/AboutForm.cs`
  - `ae07657826d1` 2023-12-28 Removed Steamworks.NET dependency
    https://github.com/sharex/sharex/commit/ae07657826d13a269042bc1f62de3d3fe73a921b
  - `077e74a2d417` 2024-01-02 Update year
    https://github.com/sharex/sharex/commit/077e74a2d4173d6315977e57e20fb6203d3b7078
  - `dca8fa2b9ee7` 2024-03-05 Translation: Hebrew missing strings
    https://github.com/sharex/sharex/commit/dca8fa2b9ee7e2bcb6c2274f3be18a34c0db8686
- `ShareX/Forms/ActionsForm.Designer.cs`
  - `eab103b3cd63` 2026-07-23 Remove obsolete code and simplify implementation
    https://github.com/sharex/sharex/commit/eab103b3cd63921e56a1a164156e62b90b27a2d2
- `ShareX/Forms/ActionsForm.cs`
  - `077e74a2d417` 2024-01-02 Update year
    https://github.com/sharex/sharex/commit/077e74a2d4173d6315977e57e20fb6203d3b7078
  - `9db8d8608b65` 2025-01-08 Update year
    https://github.com/sharex/sharex/commit/9db8d8608b65bb42b91f2e73f1946b674c60fd96
  - `cfc08a16d9f0` 2026-01-19 Update year
    https://github.com/sharex/sharex/commit/cfc08a16d9f094d9f079fcaeb46911fd44ed0d19
- `ShareX/Forms/AfterCaptureForm.Designer.cs`
  - `8150d8fc2d77` 2025-06-20 Work in progress
    https://github.com/sharex/sharex/commit/8150d8fc2d77132185f464d4e1625c39dac2110d
  - `8f139014d386` 2026-07-22 Remove obsolete code and simplify project structure
    https://github.com/sharex/sharex/commit/8f139014d386bc5e6ca295447964e3dd184eef23
- `ShareX/Forms/AfterCaptureForm.cs`
  - `077e74a2d417` 2024-01-02 Update year
    https://github.com/sharex/sharex/commit/077e74a2d4173d6315977e57e20fb6203d3b7078
  - `9db8d8608b65` 2025-01-08 Update year
    https://github.com/sharex/sharex/commit/9db8d8608b65bb42b91f2e73f1946b674c60fd96
  - `cfc08a16d9f0` 2026-01-19 Update year
    https://github.com/sharex/sharex/commit/cfc08a16d9f094d9f079fcaeb46911fd44ed0d19
- `ShareX/Forms/AfterUploadForm.cs`
  - `077e74a2d417` 2024-01-02 Update year
    https://github.com/sharex/sharex/commit/077e74a2d4173d6315977e57e20fb6203d3b7078
  - `9db8d8608b65` 2025-01-08 Update year
    https://github.com/sharex/sharex/commit/9db8d8608b65bb42b91f2e73f1946b674c60fd96
  - `cfc08a16d9f0` 2026-01-19 Update year
    https://github.com/sharex/sharex/commit/cfc08a16d9f094d9f079fcaeb46911fd44ed0d19
- `ShareX/Forms/AfterUploadForm.designer.cs`
  - `eadbe9bf4368` 2026-07-22 Remove obsolete code and files
    https://github.com/sharex/sharex/commit/eadbe9bf4368160687e109012a5abf0773fa2c8b
- `ShareX/Forms/BeforeUploadForm.Designer.cs`
  - `6cc8c9429f39` 2026-07-22 Remove obsolete files and code
    https://github.com/sharex/sharex/commit/6cc8c9429f39cbf8b15c885b61b72afa5776c1de
- `ShareX/Forms/BeforeUploadForm.cs`
  - `077e74a2d417` 2024-01-02 Update year
    https://github.com/sharex/sharex/commit/077e74a2d4173d6315977e57e20fb6203d3b7078
  - `9db8d8608b65` 2025-01-08 Update year
    https://github.com/sharex/sharex/commit/9db8d8608b65bb42b91f2e73f1946b674c60fd96
  - `cfc08a16d9f0` 2026-01-19 Update year
    https://github.com/sharex/sharex/commit/cfc08a16d9f094d9f079fcaeb46911fd44ed0d19
- `ShareX/Forms/ClipboardFormatForm.Designer.cs`
  - `2fea2310f219` 2026-07-23 Refactor codebase and remove obsolete implementation
    https://github.com/sharex/sharex/commit/2fea2310f219ae64ba4b97a77c81b228c5d6e1f8
- `ShareX/Forms/ClipboardFormatForm.cs`
  - `077e74a2d417` 2024-01-02 Update year
    https://github.com/sharex/sharex/commit/077e74a2d4173d6315977e57e20fb6203d3b7078
  - `9db8d8608b65` 2025-01-08 Update year
    https://github.com/sharex/sharex/commit/9db8d8608b65bb42b91f2e73f1946b674c60fd96
  - `cfc08a16d9f0` 2026-01-19 Update year
    https://github.com/sharex/sharex/commit/cfc08a16d9f094d9f079fcaeb46911fd44ed0d19
- `ShareX/Forms/ClipboardUploadForm.Designer.cs`
  - `83dc6fa220ef` 2026-07-21 Remove obsolete files and simplify project structure
    https://github.com/sharex/sharex/commit/83dc6fa220ef3c1658aacf4d5ff7a64617810e74
- `ShareX/Forms/ClipboardUploadForm.cs`
  - `077e74a2d417` 2024-01-02 Update year
    https://github.com/sharex/sharex/commit/077e74a2d4173d6315977e57e20fb6203d3b7078
  - `9db8d8608b65` 2025-01-08 Update year
    https://github.com/sharex/sharex/commit/9db8d8608b65bb42b91f2e73f1946b674c60fd96
  - `262970d344a9` 2025-05-22 Removed "Use custom theme" checkbox
    https://github.com/sharex/sharex/commit/262970d344a9ff6d411aae1872afb2f67746d2dc
- `ShareX/Forms/FirstTimeUploadForm.Designer.cs`
  - `8361e971e23f` 2025-10-28 Automatic upload is now disabled by default for new installations
    https://github.com/sharex/sharex/commit/8361e971e23f25d9157a51ab34296e10877a613f
- `ShareX/Forms/FirstTimeUploadForm.cs`
  - `077e74a2d417` 2024-01-02 Update year
    https://github.com/sharex/sharex/commit/077e74a2d4173d6315977e57e20fb6203d3b7078
  - `9db8d8608b65` 2025-01-08 Update year
    https://github.com/sharex/sharex/commit/9db8d8608b65bb42b91f2e73f1946b674c60fd96
  - `8361e971e23f` 2025-10-28 Automatic upload is now disabled by default for new installations
    https://github.com/sharex/sharex/commit/8361e971e23f25d9157a51ab34296e10877a613f
- `ShareX/Forms/QuickTaskInfoEditForm.Designer.cs`
  - `be682581cf02` 2026-07-23 Remove obsolete code and files
    https://github.com/sharex/sharex/commit/be682581cf02639560329d9e75c7a72c8bffb74f
- `ShareX/Forms/QuickTaskInfoEditForm.cs`
  - `077e74a2d417` 2024-01-02 Update year
    https://github.com/sharex/sharex/commit/077e74a2d4173d6315977e57e20fb6203d3b7078
  - `9db8d8608b65` 2025-01-08 Update year
    https://github.com/sharex/sharex/commit/9db8d8608b65bb42b91f2e73f1946b674c60fd96
  - `cfc08a16d9f0` 2026-01-19 Update year
    https://github.com/sharex/sharex/commit/cfc08a16d9f094d9f079fcaeb46911fd44ed0d19
- `ShareX/Forms/QuickTaskMenuEditorForm.Designer.cs`
  - `5e317dc5b2dc` 2024-04-03 fixed #7305: Improve icon detection in quick task menu
    https://github.com/sharex/sharex/commit/5e317dc5b2dcdd0fb8f41d9fb7524988ec262149
  - `be682581cf02` 2026-07-23 Remove obsolete code and files
    https://github.com/sharex/sharex/commit/be682581cf02639560329d9e75c7a72c8bffb74f
- `ShareX/Forms/QuickTaskMenuEditorForm.cs`
  - `077e74a2d417` 2024-01-02 Update year
    https://github.com/sharex/sharex/commit/077e74a2d4173d6315977e57e20fb6203d3b7078
  - `9db8d8608b65` 2025-01-08 Update year
    https://github.com/sharex/sharex/commit/9db8d8608b65bb42b91f2e73f1946b674c60fd96
  - `cfc08a16d9f0` 2026-01-19 Update year
    https://github.com/sharex/sharex/commit/cfc08a16d9f094d9f079fcaeb46911fd44ed0d19
- `ShareX/Forms/WatchFolderForm.Designer.cs`
  - `9f41f2652a25` 2026-07-23 Remove obsolete code and simplify implementation
    https://github.com/sharex/sharex/commit/9f41f2652a25d072e26ec0b3627ab936a2c2f157
- `ShareX/Forms/WatchFolderForm.cs`
  - `077e74a2d417` 2024-01-02 Update year
    https://github.com/sharex/sharex/commit/077e74a2d4173d6315977e57e20fb6203d3b7078
  - `9db8d8608b65` 2025-01-08 Update year
    https://github.com/sharex/sharex/commit/9db8d8608b65bb42b91f2e73f1946b674c60fd96
  - `cfc08a16d9f0` 2026-01-19 Update year
    https://github.com/sharex/sharex/commit/cfc08a16d9f094d9f079fcaeb46911fd44ed0d19
- `ShareX/Tools/PinToScreen/PinToScreenStartupForm.Designer.cs`
  - `f67683f89dcf` 2026-07-12 Prune obsolete ShareX implementation files
    https://github.com/sharex/sharex/commit/f67683f89dcfcfb914fa8945c5bfd587d76572a1
- `ShareX/Tools/PinToScreen/PinToScreenStartupForm.cs`
  - `d78720253b71` 2023-06-19 fixed #6706: Support pinning context menus/popups using "Pin to screen" tool
    https://github.com/sharex/sharex/commit/d78720253b71c72426f64ae81edace0f9f9b37a3
  - `077e74a2d417` 2024-01-02 Update year
    https://github.com/sharex/sharex/commit/077e74a2d4173d6315977e57e20fb6203d3b7078
  - `9db8d8608b65` 2025-01-08 Update year
    https://github.com/sharex/sharex/commit/9db8d8608b65bb42b91f2e73f1946b674c60fd96

Karar:

[ KUSUR-GETIRMEDI ]

Not:

[ ]

---

### Ornek SAMPLE-26 — github.com/jellyfin/jellyfin

Commit:

- Kisa SHA: `286dabdc4bcf`
- Yazar tarihi: 2021-09-02
- Mesaj basligi: Add SqliteItemRepository.ItemImageInfoFromValueString as a fuzzing
- Baglanti: https://github.com/jellyfin/jellyfin/commit/286dabdc4bcff65430f0abe78fbeaaed28635e18

Degisiklik ozeti:

- Degisen toplam dosya: 6
- Degisen `.cs` dosyasi: 4
- Eklenen satir: 53, silinen satir: 1
- Degisen `.cs` dosyalari:
  - `Emby.Server.Implementations/Data/SqliteItemRepository.cs`
  - `Emby.Server.Implementations/Properties/AssemblyInfo.cs`
  - `fuzz/Emby.Server.Implementations.Fuzz/Program.cs`
  - `tests/Jellyfin.Server.Implementations.Tests/Data/SqliteItemRepositoryTests.cs`

Commit'in ilgili C# diff'i:

```diff
--- Emby.Server.Implementations/Data/SqliteItemRepository.cs
--- a/Emby.Server.Implementations/Data/SqliteItemRepository.cs
+++ b/Emby.Server.Implementations/Data/SqliteItemRepository.cs
@@ -1135,15 +1135,25 @@ namespace Emby.Server.Implementations.Data
                 Path = RestorePath(path.ToString())
             };
 
-            if (long.TryParse(dateModified, NumberStyles.Any, CultureInfo.InvariantCulture, out var ticks))
+            if (long.TryParse(dateModified, NumberStyles.Any, CultureInfo.InvariantCulture, out var ticks)
+                && ticks >= DateTime.MinValue.Ticks
+                && ticks <= DateTime.MaxValue.Ticks)
             {
                 image.DateModified = new DateTime(ticks, DateTimeKind.Utc);
             }
+            else
+            {
+                return null;
+            }
 
             if (Enum.TryParse(imageType.ToString(), true, out ImageType type))
             {
                 image.Type = type;
             }
+            else
+            {
+                return null;
+            }
 
             // Optional parameters: width*height*blurhash
             if (nextSegment + 1 < value.Length - 1)
--- Emby.Server.Implementations/Properties/AssemblyInfo.cs
--- a/Emby.Server.Implementations/Properties/AssemblyInfo.cs
+++ b/Emby.Server.Implementations/Properties/AssemblyInfo.cs
@@ -16,6 +16,7 @@ using System.Runtime.InteropServices;
 [assembly: AssemblyCulture("")]
 [assembly: NeutralResourcesLanguage("en")]
 [assembly: InternalsVisibleTo("Jellyfin.Server.Implementations.Tests")]
+[assembly: InternalsVisibleTo("Emby.Server.Implementations.Fuzz")]
 
 // Setting ComVisible to false makes the types in this assembly not visible
 // to COM components.  If you need to access a type in this assembly from
--- fuzz/Emby.Server.Implementations.Fuzz/Program.cs
--- a/fuzz/Emby.Server.Implementations.Fuzz/Program.cs
+++ b/fuzz/Emby.Server.Implementations.Fuzz/Program.cs
@@ -1,5 +1,12 @@
 ﻿using System;
+using AutoFixture;
+using AutoFixture.AutoMoq;
+using Emby.Server.Implementations.Data;
 using Emby.Server.Implementations.Library;
+using MediaBrowser.Controller;
+using MediaBrowser.Controller.Entities;
+using MediaBrowser.Model.Entities;
+using Moq;
 using SharpFuzz;
 
 namespace Emby.Server.Implementations.Fuzz
@@ -11,6 +18,7 @@ namespace Emby.Server.Implementations.Fuzz
             switch (args[0])
             {
                 case "PathExtensions.TryReplaceSubPath": Run(PathExtensions_TryReplaceSubPath); return;
+                case "SqliteItemRepository.ItemImageInfoFromValueString": Run(SqliteItemRepository_ItemImageInfoFromValueString); return;
                 default: throw new ArgumentException($"Unknown fuzzing function: {args[0]}");
             }
         }
@@ -28,5 +36,27 @@ namespace Emby.Server.Implementations.Fuzz
 
             _ = PathExtensions.TryReplaceSubPath(parts[0], parts[1], parts[2], out _);
         }
+
+        private static void SqliteItemRepository_ItemImageInfoFromValueString(string data)
+        {
+            var sqliteItemRepository = MockSqliteItemRepository();
+            sqliteItemRepository.ItemImageInfoFromValueString(data);
+        }
+
+        private static SqliteItemRepository MockSqliteItemRepository()
+        {
+            const string VirtualMetaDataPath = "%MetadataPath%";
+            const string MetaDataPath = "/meta/data/path";
+
+            var appHost = new Mock<IServerApplicationHost>();
+            appHost.Setup(x => x.ExpandVirtualPath(It.IsAny<string>()))
+                .Returns((string x) => x.Replace(VirtualMetaDataPath, MetaDataPath, StringComparison.Ordinal));
+            appHost.Setup(x => x.ReverseVirtualPath(It.IsAny<string>()))
+                .Returns((string x) => x.Replace(MetaDataPath, VirtualMetaDataPath, StringComparison.Ordinal));
+
+            IFixture fixture = new Fixture().Customize(new AutoMoqCustomization { ConfigureMembers = true });
+            fixture.Inject(appHost);
+            return fixture.Create<SqliteItemRepository>();
+        }
     }
 }
--- tests/Jellyfin.Server.Implementations.Tests/Data/SqliteItemRepositoryTests.cs
--- a/tests/Jellyfin.Server.Implementations.Tests/Data/SqliteItemRepositoryTests.cs
+++ b/tests/Jellyfin.Server.Implementations.Tests/Data/SqliteItemRepositoryTests.cs
@@ -109,6 +109,9 @@ namespace Jellyfin.Server.Implementations.Tests.Data
         [InlineData("")]
         [InlineData("*")]
         [InlineData("https://image.tmdb.org/t/p/original/zhB5CHEgqqh4wnEqDNJLfWXJlcL.jpg*0")]
+        [InlineData("/mnt/series/Family Guy/Season 1/Family Guy - S01E01-thumb.jpg*6374520964785129080*WjQbtJtSO8nhNZ%L_Io#R/oaS<o}-;adXAoIn7j[%hW9s:WGw[nN")] // Invalid modified date
+        [InlineData("/mnt/series/Family Guy/Season 1/Family Guy - S01E01-thumb.jpg*-637452096478512963*WjQbtJtSO8nhNZ%L_Io#R/oaS<o}-;adXAoIn7j[%hW9s:WGw[nN")] // Negative modified date
+        [InlineData("/mnt/series/Family Guy/Season 1/Family Guy - S01E01-thumb.jpg*637452096478512963*Invalid*1920*1080*WjQbtJtSO8nhNZ%L_Io#R/oaS6o}-;adXAoIn7j[%hW9s:WGw[nN")] // Invalid type
         public void ItemImageInfoFromValueString_Invalid_Null(string value)
         {
             Assert.Null(_sqliteItemRepository.ItemImageInfoFromValueString(value));
```

Sonraki tarihsel olgular:

- `Emby.Server.Implementations/Data/SqliteItemRepository.cs`
  - `637e86478f5c` 2021-09-03 Fix some warnings
    https://github.com/jellyfin/jellyfin/commit/637e86478f5cca7c8ac5e17cf541dc4c6adac14e
  - `3d0b1ccae661` 2021-09-06 Remove all unused usings
    https://github.com/jellyfin/jellyfin/commit/3d0b1ccae661704371041aadafc9816a223b1ea0
  - `5fd315b17c7c` 2021-09-24 Address comments
    https://github.com/jellyfin/jellyfin/commit/5fd315b17c7c95c05eaba0713b27f6a95d31e164
- `Emby.Server.Implementations/Properties/AssemblyInfo.cs`
  - `7eba162879f6` 2023-12-28 Move LiveTv tests to separate project
    https://github.com/jellyfin/jellyfin/commit/7eba162879f6d1ff04539cac5c0d6372a955d82b
  - `c1a3084312fa` 2023-12-28 Move LiveTv to separate project
    https://github.com/jellyfin/jellyfin/commit/c1a3084312fa4fb7796b83640bfe9ad2b5044afa
- `fuzz/Emby.Server.Implementations.Fuzz/Program.cs`
  - `b16033df03db` 2023-10-22 Fix fuzz projects (#10416)
    https://github.com/jellyfin/jellyfin/commit/b16033df03db7a6c3e3b3636c9eac4dad8e49f9d
  - `97a02f580398` 2024-08-30 Remove BOM from UTF-8 files
    https://github.com/jellyfin/jellyfin/commit/97a02f58039855eb1e3e23686d4fe5bee1fbd15e
- `tests/Jellyfin.Server.Implementations.Tests/Data/SqliteItemRepositoryTests.cs`
  - `19b8bcaec438` 2021-09-11 Use TheoryData instead of MemberData and ClassData
    https://github.com/jellyfin/jellyfin/commit/19b8bcaec43835c698a35975a748c2129c1413aa
  - `cbfa355e31ec` 2021-12-24 Update StyleCop
    https://github.com/jellyfin/jellyfin/commit/cbfa355e31ec7a78ef73bbde5566fb2b3424363e
  - `3462676a8f28` 2022-12-14 Fix debug builds (#8909)
    https://github.com/jellyfin/jellyfin/commit/3462676a8f288358d65484ce2022b66ef9da5ee9

Karar:

[ KUSUR-GETIRMEDI ]

Not:

[ ]

---

### Ornek SAMPLE-27 — github.com/app-vnext/polly

Commit:

- Kisa SHA: `8ba491d7da22`
- Yazar tarihi: 2026-07-10
- Mesaj basligi: Bump SonarAnalyzer.CSharp from 10.27.0.140913 to 10.28.0.143324 (#3154)
- Baglanti: https://github.com/app-vnext/polly/commit/8ba491d7da22a0364c9eb4bbef3a09d0c97c52a9

Degisiklik ozeti:

- Degisen toplam dosya: 2
- Degisen `.cs` dosyasi: 1
- Eklenen satir: 3, silinen satir: 1
- Degisen `.cs` dosyalari:
  - `src/Polly.Core/Utils/RandomUtil.cs`

Commit'in ilgili C# diff'i:

```diff
--- src/Polly.Core/Utils/RandomUtil.cs
--- a/src/Polly.Core/Utils/RandomUtil.cs
+++ b/src/Polly.Core/Utils/RandomUtil.cs
@@ -8,7 +8,9 @@ internal static class RandomUtil
     public static double NextDouble() => Random.Shared.NextDouble();
     public static int Next(int maxValue) => Random.Shared.Next(maxValue);
 #else
+#pragma warning disable S2245
     private static readonly ThreadLocal<Random> Instance = new(() => new Random());
+#pragma warning restore S2245
 
     public static double NextDouble() => Instance.Value.NextDouble();
     public static int Next(int maxValue) => Instance.Value.Next(maxValue);
```

Sonraki tarihsel olgular:

- `src/Polly.Core/Utils/RandomUtil.cs`
  - Gozlem araliginda sonraki degisiklik yok

Karar:

[ BAKILMADI ]

Not:

[ ]

---

### Ornek SAMPLE-28 — github.com/jellyfin/jellyfin

Commit:

- Kisa SHA: `faf1cea63e04`
- Yazar tarihi: 2025-11-17
- Mesaj basligi: Backport pull request #15514 from jellyfin/release-10.11.z
- Baglanti: https://github.com/jellyfin/jellyfin/commit/faf1cea63e042e3b2bf4ab87f38d86d5a2de0b07

Degisiklik ozeti:

- Degisen toplam dosya: 1
- Degisen `.cs` dosyasi: 1
- Eklenen satir: 5, silinen satir: 2
- Degisen `.cs` dosyalari:
  - `MediaBrowser.XbmcMetadata/Providers/BaseNfoProvider.cs`

Commit'in ilgili C# diff'i:

```diff
--- MediaBrowser.XbmcMetadata/Providers/BaseNfoProvider.cs
--- a/MediaBrowser.XbmcMetadata/Providers/BaseNfoProvider.cs
+++ b/MediaBrowser.XbmcMetadata/Providers/BaseNfoProvider.cs
@@ -68,12 +68,15 @@ namespace MediaBrowser.XbmcMetadata.Providers
         {
             var file = GetXmlFile(new ItemInfo(item), directoryService);
 
-            if (file is null)
+            if (file?.Exists is not true)
             {
                 return false;
             }
 
-            return file.Exists && _fileSystem.GetLastWriteTimeUtc(file) > item.DateLastSaved;
+            var fileTime = _fileSystem.GetLastWriteTimeUtc(file);
+
+            // 1 minute tolerance to avoid detecting our own file writes
+            return (fileTime - item.DateLastSaved) > TimeSpan.FromMinutes(1);
         }
 
         protected abstract void Fetch(MetadataResult<T> result, string path, CancellationToken cancellationToken);
```

Sonraki tarihsel olgular:

- `MediaBrowser.XbmcMetadata/Providers/BaseNfoProvider.cs`
  - Gozlem araliginda sonraki degisiklik yok

Karar:

[ KUSUR-GETIRMEDI ]

Not:

[ ]

---

### Ornek SAMPLE-29 — github.com/sharex/sharex

Commit:

- Kisa SHA: `5587f4f74965`
- Yazar tarihi: 2022-05-15
- Mesaj basligi: Add GetPrefix helper
- Baglanti: https://github.com/sharex/sharex/commit/5587f4f749654459dcfe13f0a0d28a6c37761eb5

Degisiklik ozeti:

- Degisen toplam dosya: 1
- Degisen `.cs` dosyasi: 1
- Eklenen satir: 6, silinen satir: 1
- Degisen `.cs` dosyalari:
  - `ShareX.HelpersLib/Helpers/URLHelpers.cs`

Commit'in ilgili C# diff'i:

```diff
--- ShareX.HelpersLib/Helpers/URLHelpers.cs
--- a/ShareX.HelpersLib/Helpers/URLHelpers.cs
+++ b/ShareX.HelpersLib/Helpers/URLHelpers.cs
@@ -420,6 +420,11 @@ public static bool HasPrefix(string url)
         {
             return URLPrefixes.Any(x => url.StartsWith(x, StringComparison.InvariantCultureIgnoreCase));
         }
+        
+        public static string GetPrefix(string url)
+        {
+            return URLPrefixes.Find(x => url.StartsWith(x, StringComparison.InvariantCultureIgnoreCase));
+        }
 
         public static string FixPrefix(string url, string prefix = "http://")
         {
@@ -572,4 +577,4 @@ public static string BuildUri(string root, string path, string query = null)
             return builder.Uri.AbsoluteUri;
         }
     }
-}
+}
```

Sonraki tarihsel olgular:

- `ShareX.HelpersLib/Helpers/URLHelpers.cs`
  - `3e7c861a1c4e` 2022-05-15 #6257: Fixed the issue
    https://github.com/sharex/sharex/commit/3e7c861a1c4ec0066f4c205ee454beaaf9e527a2
  - `fa76a96f8858` 2022-05-16 Remember selected service link
    https://github.com/sharex/sharex/commit/fa76a96f88583712f091cb76ecbfcd0420c5e292
  - `6939ef2b5370` 2022-07-24 Change parameter default value to "https://"
    https://github.com/sharex/sharex/commit/6939ef2b53703a104f1060594a2aaf92f0808472

Karar:

[ KUSUR-GETIRMEDI ]

Not:

[ ]

---

### Ornek SAMPLE-30 — github.com/jellyfin/jellyfin

Commit:

- Kisa SHA: `eba24d188d3f`
- Yazar tarihi: 2023-03-18
- Mesaj basligi: Update Emby.Server.Implementations/Playlists/PlaylistManager.cs
- Baglanti: https://github.com/jellyfin/jellyfin/commit/eba24d188d3ff9747e2c39281e3f64d5fdd53899

Degisiklik ozeti:

- Degisen toplam dosya: 1
- Degisen `.cs` dosyasi: 1
- Eklenen satir: 1, silinen satir: 1
- Degisen `.cs` dosyalari:
  - `Emby.Server.Implementations/Playlists/PlaylistManager.cs`

Commit'in ilgili C# diff'i:

```diff
--- Emby.Server.Implementations/Playlists/PlaylistManager.cs
--- a/Emby.Server.Implementations/Playlists/PlaylistManager.cs
+++ b/Emby.Server.Implementations/Playlists/PlaylistManager.cs
@@ -136,7 +136,7 @@ namespace Emby.Server.Implementations.Playlists
                     Name = name,
                     Path = path,
                     OwnerUserId = options.UserId,
-                    Shares = options.Shares
+                    Shares = options.Shares ?? Array.Empty<Share>()
                 };
 
                 playlist.SetMediaType(options.MediaType);
```

Sonraki tarihsel olgular:

- `Emby.Server.Implementations/Playlists/PlaylistManager.cs`
  - `9211a73e4011` 2023-03-25 Apply suggestions from code review
    https://github.com/jellyfin/jellyfin/commit/9211a73e4011c0c610fdbcf24e0723a3552f22fa
  - `a8cdf4434b10` 2023-05-12 Fix access to playlists not created by a user (#9746)
    https://github.com/jellyfin/jellyfin/commit/a8cdf4434b10044dbb9ba540d6d137906aa67b54
  - `2920611ffc20` 2023-05-13 Convert string MediaType to enum MediaType
    https://github.com/jellyfin/jellyfin/commit/2920611ffc206d845563637c4a865bf3f02d1374

Karar:

[ KUSUR-GETIRDI ]

Not:

[ ]

---

