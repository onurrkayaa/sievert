namespace Sievert.Api;

/// <summary>
/// Makine tarafindan okunabilir hata kodlari. Cevaptaki serbest metin degisebilir,
/// bu kodlar degismez; istemci koda gore dallanir.
/// </summary>
public static class ApiError
{
    public const string RepositoryNotFound = "REPOSITORY_NOT_FOUND";

    public const string CommitNotFound = "COMMIT_NOT_FOUND";

    public const string CommitMetricsMissing = "COMMIT_METRICS_MISSING";

    public const string ModelProfileRequired = "MODEL_PROFILE_REQUIRED";

    public const string ModelProfileNotFound = "MODEL_PROFILE_NOT_FOUND";

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
}
