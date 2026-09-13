namespace Sievert.Contracts;

/// <summary>
/// Makine tarafindan okunabilir hata kodlari. Cevaptaki serbest metin degisebilir,
/// bu kodlar degismez; istemci koda gore dallanir.
///
/// Burada duran her kod bir <c>ProblemDetails</c> cevabinda cikabiliyor. Adim 3'te
/// <c>REMOTE_ACCESS_NOT_ALLOWED</c> da bu listedeydi ama o bir HTTP cevabi degil:
/// acilista, ilk istek gelmeden once verilen bir ret. Hicbir zaman
/// <c>ProblemDetails</c> olarak donemeyecek bir kodu hata katalogunda tutmak, istemciye
/// olmayan bir cevabi bekletmek olurdu; kod <see cref="RemoteAccessGuard"/> tarafina
/// tasindi.
/// </summary>
public static class ApiError
{
    public const string RepositoryNotFound = "REPOSITORY_NOT_FOUND";

    public const string CommitNotFound = "COMMIT_NOT_FOUND";

    public const string CommitMetricsMissing = "COMMIT_METRICS_MISSING";

    public const string ModelProfileRequired = "MODEL_PROFILE_REQUIRED";

    public const string ModelProfileNotFound = "MODEL_PROFILE_NOT_FOUND";

    /// <summary>
    /// Kaldirilan <c>?profile=</c> parametresi hala gonderiliyor. Sessizce yok saymak,
    /// istegin sahibine istedigi profille skorlandigini dusundururdu.
    /// </summary>
    public const string ProfileSelectionNotSupported = "PROFILE_SELECTION_NOT_SUPPORTED";

    /// <summary>Depo uc egitim reposundan biri degil. Varsayilan profil SECILMIYOR.</summary>
    public const string UnknownRepositoryModel = "UNKNOWN_REPOSITORY_MODEL";

    /// <summary>Verilen sha oneki birden fazla commit'e uyuyor.</summary>
    public const string AmbiguousSha = "AMBIGUOUS_SHA";

    /// <summary>Egitim skor dagilimi okunamadi; goreli endeks uretilemez.</summary>
    public const string ScoreReferenceNotReady = "SCORE_REFERENCE_NOT_READY";

    public const string ModelChecksumMismatch = "MODEL_CHECKSUM_MISMATCH";

    public const string ModelNotReady = "MODEL_NOT_READY";

    public const string ModelExplanationMismatch = "MODEL_EXPLANATION_MISMATCH";

    public const string DatabaseNotReady = "DATABASE_NOT_READY";

    public const string InvalidSha = "INVALID_SHA";

    public const string InvalidPagination = "INVALID_PAGINATION";

    public const string Unexpected = "UNEXPECTED_ERROR";

    // Arka plan isleri (Asama 6 Adim 3).

    /// <summary>Istekteki is turu taninmiyor.</summary>
    public const string AnalysisKindInvalid = "ANALYSIS_KIND_INVALID";

    /// <summary>Ayni repo ve tur icin zaten aktif bir is var.</summary>
    public const string AnalysisAlreadyActive = "ANALYSIS_ALREADY_ACTIVE";

    public const string AnalysisNotFound = "ANALYSIS_NOT_FOUND";

    /// <summary>Is iptal edilebilecek bir durumda degil; yaris durumunda savunma amacli.</summary>
    public const string AnalysisNotCancelable = "ANALYSIS_NOT_CANCELABLE";

    /// <summary>Bulgular risk isinden, risk satirlari tarama isinden istendi.</summary>
    public const string AnalysisResultTypeMismatch = "ANALYSIS_RESULT_TYPE_MISMATCH";

    public const string IdempotencyKeyInvalid = "IDEMPOTENCY_KEY_INVALID";

    /// <summary>Ayni tekrar anahtari baska bir repo ya da tur icin kullanilmis.</summary>
    public const string IdempotencyKeyReused = "IDEMPOTENCY_KEY_REUSED";

    /// <summary>Depo icin yerel klasor kayitli degil ya da bulunamadi.</summary>
    public const string RepositoryPathUnavailable = "REPOSITORY_PATH_UNAVAILABLE";

    public const string RepositoryNotGit = "REPOSITORY_NOT_GIT";

    /// <summary>Onceki surecte yarida kalmis is.</summary>
    public const string ProcessInterrupted = "PROCESS_INTERRUPTED";

    public const string AnalysisCanceled = "ANALYSIS_CANCELED";

    public const string AnalysisFailed = "ANALYSIS_FAILED";

    /// <summary>Yazilan satir sayisi beklenenle ayni degil; is basarili sayilmiyor.</summary>
    public const string ResultCountMismatch = "RESULT_COUNT_MISMATCH";

    // Kaynak durumu (Asama 6 Adim 3b).

    /// <summary>Calisma agacinda kaydedilmemis degisiklik var; tarama baslatilmadi.</summary>
    public const string RepositoryWorktreeDirty = "REPOSITORY_WORKTREE_DIRTY";

    /// <summary>Tarama sirasinda HEAD ya da calisma agaci degisti; sonuc tam degil.</summary>
    public const string RepositoryChangedDuringAnalysis = "REPOSITORY_CHANGED_DURING_ANALYSIS";

    /// <summary>Depo bir git deposu ama HEAD okunamadi; taranacak bir surum yok.</summary>
    public const string RepositoryHeadUnavailable = "REPOSITORY_HEAD_UNAVAILABLE";
}
