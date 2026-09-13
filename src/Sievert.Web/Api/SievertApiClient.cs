using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using Sievert.Contracts;

namespace Sievert.Web.Api;

/// <summary>
/// Panelin API ile konustugu tek yer.
///
/// <c>HttpClient</c> bilesenlere dagitilmiyor. Sebebi tek bir cumle: hata cevabinin nasil
/// okunacagi, tekrar anahtarinin nasil uretilecegi ve sayfalama sinirinin ne oldugu
/// sekiz ayri sayfaya dagilirsa sekiz ayri sekilde yanlis yapilir.
///
/// Butun cagrilar iptal jetonu aliyor. Blazor'da bir kullanici sayfadan cikinca bileseni
/// birakiyor; devam eden istek de birakilmali, yoksa cevap donmeyen bir bilesene yazilir.
/// </summary>
public sealed class SievertApiClient(HttpClient http, ILogger<SievertApiClient> logger)
{
    /// <summary>API'nin kabul ettigi en buyuk sayfa boyutu. Panel bunu asmiyor.</summary>
    public const int MaximumPageSize = 100;

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public Task<ApiResult<HealthResponse>> HealthAsync(CancellationToken cancellation) =>
        GetAsync<HealthResponse>("/api/v1/health", cancellation);

    public Task<ApiResult<List<ModelResponse>>> ModelsAsync(CancellationToken cancellation) =>
        GetAsync<List<ModelResponse>>("/api/v1/models", cancellation);

    public Task<ApiResult<PagedResponse<RepositoryListItem>>> RepositoriesAsync(
        int page,
        int pageSize,
        CancellationToken cancellation) =>
        GetAsync<PagedResponse<RepositoryListItem>>(
            $"/api/v1/repositories?page={page}&pageSize={Clamp(pageSize)}",
            cancellation);

    public Task<ApiResult<RepositoryDetail>> RepositoryAsync(int id, CancellationToken cancellation) =>
        GetAsync<RepositoryDetail>($"/api/v1/repositories/{id}", cancellation);

    public Task<ApiResult<PagedResponse<CommitListItem>>> CommitsAsync(
        int repositoryId,
        int page,
        int pageSize,
        string order,
        bool? isFix,
        bool? isBugIntroducing,
        bool? isBot,
        CancellationToken cancellation)
    {
        string query = $"/api/v1/repositories/{repositoryId}/commits?page={page}&pageSize={Clamp(pageSize)}"
            + $"&order={Uri.EscapeDataString(order)}";

        query += Flag("isFix", isFix) + Flag("isBugIntroducing", isBugIntroducing) + Flag("isBot", isBot);

        return GetAsync<PagedResponse<CommitListItem>>(query, cancellation);
    }

    /// <summary>Verilmeyen bir suzgec sorguya hic yazilmiyor; "hepsi" ile "false" ayni sey degil.</summary>
    private static string Flag(string name, bool? value) =>
        value is bool set ? $"&{name}={(set ? "true" : "false")}" : string.Empty;

    public Task<ApiResult<CommitRiskAssessment>> RiskAsync(
        int repositoryId,
        string sha,
        CancellationToken cancellation) =>
        GetAsync<CommitRiskAssessment>(
            $"/api/v1/repositories/{repositoryId}/commits/{Uri.EscapeDataString(sha)}/risk",
            cancellation);

    public Task<ApiResult<AnalysisJobResponse>> AnalysisAsync(Guid jobId, CancellationToken cancellation) =>
        GetAsync<AnalysisJobResponse>($"/api/v1/analyses/{jobId}", cancellation);

    public Task<ApiResult<PagedResponse<AnalysisJobResponse>>> AnalysesAsync(
        int repositoryId,
        int page,
        int pageSize,
        string? status,
        string? kind,
        CancellationToken cancellation)
    {
        string query = $"/api/v1/repositories/{repositoryId}/analyses?page={page}&pageSize={Clamp(pageSize)}";

        if (!string.IsNullOrWhiteSpace(status))
        {
            query += $"&status={Uri.EscapeDataString(status)}";
        }

        if (!string.IsNullOrWhiteSpace(kind))
        {
            query += $"&kind={Uri.EscapeDataString(kind)}";
        }

        return GetAsync<PagedResponse<AnalysisJobResponse>>(query, cancellation);
    }

    public Task<ApiResult<AnalysisResultPage<StaticFindingResponse>>> FindingsAsync(
        Guid jobId,
        int page,
        int pageSize,
        string? ruleCode,
        string? severity,
        CancellationToken cancellation)
    {
        string query = $"/api/v1/analyses/{jobId}/findings?page={page}&pageSize={Clamp(pageSize)}";

        if (!string.IsNullOrWhiteSpace(ruleCode))
        {
            query += $"&ruleCode={Uri.EscapeDataString(ruleCode)}";
        }

        if (!string.IsNullOrWhiteSpace(severity))
        {
            query += $"&severity={Uri.EscapeDataString(severity)}";
        }

        return GetAsync<AnalysisResultPage<StaticFindingResponse>>(query, cancellation);
    }

    public Task<ApiResult<AnalysisResultPage<CommitRiskSnapshotResponse>>> RisksAsync(
        Guid jobId,
        int page,
        int pageSize,
        string order,
        CancellationToken cancellation) =>
        GetAsync<AnalysisResultPage<CommitRiskSnapshotResponse>>(
            $"/api/v1/analyses/{jobId}/risks?page={page}&pageSize={Clamp(pageSize)}"
            + $"&order={Uri.EscapeDataString(order)}",
            cancellation);

    /// <summary>
    /// Dosya etkinlik haritasi.
    ///
    /// Pencere, limit ve siralama sunucuda da dogrulaniyor; burada kirpilmiyorlar.
    /// Sessizce kirpsaydim kullanici verdigi degerle sonuc aldigini sanardi.
    /// </summary>
    public Task<ApiResult<FileActivityResponse>> FileActivityAsync(
        int repositoryId,
        Guid analysisJobId,
        int commitWindow,
        int limit,
        string sort,
        Guid? staticAnalysisJobId,
        CancellationToken cancellation)
    {
        string query = $"/api/v1/repositories/{repositoryId}/visualizations/file-activity"
            + $"?analysisJobId={analysisJobId}&commitWindow={commitWindow}&limit={limit}"
            + $"&sort={Uri.EscapeDataString(sort)}";

        if (staticAnalysisJobId is Guid overlay)
        {
            query += $"&staticAnalysisJobId={overlay}";
        }

        return GetAsync<FileActivityResponse>(query, cancellation);
    }

    public Task<ApiResult<RiskTimelineResponse>> RiskTimelineAsync(
        int repositoryId,
        Guid analysisJobId,
        int count,
        CancellationToken cancellation) =>
        GetAsync<RiskTimelineResponse>(
            $"/api/v1/repositories/{repositoryId}/visualizations/risk-timeline"
            + $"?analysisJobId={analysisJobId}&count={count}",
            cancellation);

    /// <summary>
    /// Is baslatir.
    ///
    /// Tekrar anahtari **cagiran tarafindan** veriliyor ve istek basina bir kez
    /// uretiliyor. Burada uretilseydi, kullanicinin iki kez tikladigi iki istek iki ayri
    /// anahtar alir ve tekrar anahtarinin varlik sebebi ortadan kalkardi.
    /// </summary>
    public async Task<JobStart> StartAsync(
        int repositoryId,
        string kind,
        string idempotencyKey,
        CancellationToken cancellation)
    {
        using HttpRequestMessage request = new(HttpMethod.Post, $"/api/v1/repositories/{repositoryId}/analyses")
        {
            Content = JsonContent.Create(new StartAnalysisRequest(kind), options: Json),
        };

        request.Headers.TryAddWithoutValidation("Idempotency-Key", idempotencyKey);

        try
        {
            using HttpResponseMessage response = await http.SendAsync(request, cancellation);

            if (response.IsSuccessStatusCode)
            {
                AnalysisJobResponse? job = await response.Content
                    .ReadFromJsonAsync<AnalysisJobResponse>(Json, cancellation);

                return job is null
                    ? new JobStart(JobStartOutcome.Refused, null, null, Unreadable())
                    : JobStart.From(response.StatusCode, job);
            }

            ProblemResponse problem = await ProblemAsync(response, cancellation);

            return response.StatusCode == HttpStatusCode.Conflict
                && problem.ErrorCode == ApiError.AnalysisAlreadyActive
                    ? new JobStart(JobStartOutcome.Conflict, null, problem.Extension("activeJobUrl"), problem)
                    : new JobStart(JobStartOutcome.Refused, null, null, problem);
        }
        catch (HttpRequestException error)
        {
            logger.LogWarning(error, "Is baslatilamadi; API'ye ulasilamadi.");

            return new JobStart(JobStartOutcome.Refused, null, null, Unreachable());
        }
    }

    /// <summary>
    /// Isi iptal eder.
    ///
    /// <c>200</c> ve <c>202</c> ikisi de basari; farki kullaniciya soylemek gerekiyor.
    /// Tekrar gonderilmesi zararsiz, API bu istegi idempotent isliyor.
    /// </summary>
    public async Task<JobCancel> CancelAsync(Guid jobId, CancellationToken cancellation)
    {
        try
        {
            using HttpResponseMessage response = await http.PostAsync(
                $"/api/v1/analyses/{jobId}/cancel",
                content: null,
                cancellation);

            if (!response.IsSuccessStatusCode)
            {
                return new JobCancel(false, null, await ProblemAsync(response, cancellation));
            }

            AnalysisJobResponse? job = await response.Content
                .ReadFromJsonAsync<AnalysisJobResponse>(Json, cancellation);

            return new JobCancel(response.StatusCode == HttpStatusCode.OK, job, null);
        }
        catch (HttpRequestException error)
        {
            logger.LogWarning(error, "Iptal istegi gonderilemedi.");

            return new JobCancel(false, null, Unreachable());
        }
    }

    private static int Clamp(int pageSize) => Math.Clamp(pageSize, 1, MaximumPageSize);

    private async Task<ApiResult<T>> GetAsync<T>(string path, CancellationToken cancellation)
    {
        try
        {
            using HttpResponseMessage response = await http.GetAsync(path, cancellation);

            if (!response.IsSuccessStatusCode)
            {
                return ApiResult<T>.Refused(await ProblemAsync(response, cancellation));
            }

            T? value = await response.Content.ReadFromJsonAsync<T>(Json, cancellation);

            return value is null
                ? ApiResult<T>.Refused(Unreadable())
                : ApiResult<T>.Ok(value);
        }
        catch (HttpRequestException error)
        {
            // Istisnanin kendi metni sunucu adi ve port tasiyabilir; gunluge gidiyor,
            // ekrana genel bir cumle cikiyor.
            logger.LogWarning(error, "API cagrisi basarisiz: {Path}", path);

            return ApiResult<T>.Unreachable(Unreachable().Detail!);
        }
        catch (JsonException error)
        {
            logger.LogWarning(error, "API cevabi okunamadi: {Path}", path);

            return ApiResult<T>.Unreachable("API'den gelen cevap okunamadi.");
        }
    }

    private async Task<ProblemResponse> ProblemAsync(HttpResponseMessage response, CancellationToken cancellation)
    {
        try
        {
            if (await response.Content.ReadFromJsonAsync<ProblemResponse>(Json, cancellation) is ProblemResponse
                problem)
            {
                return problem;
            }
        }
        catch (JsonException error)
        {
            logger.LogWarning(error, "Hata cevabi ayristirilamadi. Durum={Status}", (int)response.StatusCode);
        }

        return new ProblemResponse
        {
            Status = (int)response.StatusCode,
            Title = "Istek karsilanamadi",
            Detail = $"API {(int)response.StatusCode} dondu ve ayrinti okunamadi.",
        };
    }

    private static ProblemResponse Unreadable() => new()
    {
        Title = "Cevap okunamadi",
        Detail = "API cevap verdi ama icerigi beklenen bicimde degildi.",
    };

    private static ProblemResponse Unreachable() => new()
    {
        Title = "API'ye ulasilamadi",
        Detail = "API su an cevap vermiyor. Calistigini ve adresinin dogru oldugunu kontrol et.",
    };
}
