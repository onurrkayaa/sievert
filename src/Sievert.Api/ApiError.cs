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

    public const string ModelChecksumMismatch = "MODEL_CHECKSUM_MISMATCH";

    public const string ModelNotReady = "MODEL_NOT_READY";

    public const string ModelExplanationMismatch = "MODEL_EXPLANATION_MISMATCH";

    public const string DatabaseNotReady = "DATABASE_NOT_READY";

    public const string InvalidSha = "INVALID_SHA";

    public const string InvalidPagination = "INVALID_PAGINATION";

    public const string Unexpected = "UNEXPECTED_ERROR";
}
