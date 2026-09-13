using System.Text.Json;

using Microsoft.EntityFrameworkCore;

using Sievert.Api.Analysis;
using Sievert.Contracts;
using Sievert.Data;
using Sievert.Data.Entities;
using Sievert.Modeling;

namespace Sievert.Api.Reports;

/// <summary>Rapor istegi sonucunun ozeti.</summary>
/// <param name="Artifact">Olusturulan ya da tekrar anahtariyla bulunan kayit.</param>
/// <param name="Created">Yeni is acildi mi; false ise ayni istek zaten vardi.</param>
public sealed record ReportRequestResult(
    ReportArtifactRow? Artifact,
    bool Created,
    string? ErrorCode,
    string? Detail)
{
    public static ReportRequestResult Ok(ReportArtifactRow artifact, bool created) =>
        new(artifact, created, null, null);

    public static ReportRequestResult Error(string code, string detail) =>
        new(null, false, code, detail);
}

/// <summary>
/// Rapor istegini dogrular, manifesti uretir ve uretim isini kuyruga koyar.
///
/// PDF burada uretilmiyor: istek bir arka plan isi aciyor ve hemen donuyor. Sebep
/// olculdu degil, tasarim - bir HTTP istegini saniyelerce acik tutmak, kuyrugu ve iptali
/// zaten kurulmus bir sistemde gereksiz.
/// </summary>
public sealed class ReportService(
    SievertContext context,
    ModelRegistry registry,
    ScoreReference reference,
    IAnalysisJobQueue queue,
    IReportArtifactStore store,
    TimeProvider clock)
{
    public async Task<ReportRequestResult> RequestAsync(
        int repositoryId,
        ReportRequest? request,
        string? idempotencyKey,
        CancellationToken cancellation)
    {
        ReportValidation validation = ReportRequestValidator.Validate(request);

        if (validation.Request is not ResolvedReportRequest resolved)
        {
            return ReportRequestResult.Error(validation.ErrorCode!, validation.Detail!);
        }

        if (idempotencyKey is not null && !AnalysisJobStore.IsValidKey(idempotencyKey))
        {
            return ReportRequestResult.Error(
                ApiError.IdempotencyKeyInvalid, "Idempotency-Key bicimi gecersiz.");
        }

        if (await context.Repositories.AsNoTracking()
            .FirstOrDefaultAsync(row => row.Id == repositoryId, cancellation) is not RepositoryRow repository)
        {
            return ReportRequestResult.Error(ApiError.RepositoryNotFound, "Depo bulunamadi.");
        }

        ReportPlanResult plan = await PlanAsync(repository, resolved, cancellation);

        if (plan.Plan is not ReportPlan ready)
        {
            return ReportRequestResult.Error(plan.ErrorCode!, plan.Detail!);
        }

        string fingerprint = resolved.Fingerprint();

        // Ayni tekrar anahtari: govde de ayniysa ayni rapor doner, degistiyse reddedilir.
        // Ikincisi sessizce yeni bir rapor uretseydi istemci hangi raporu aldigini
        // bilemezdi.
        if (idempotencyKey is not null)
        {
            ReportArtifactRow? existing = await context.ReportArtifacts
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    row => row.RepositoryId == repositoryId && row.IdempotencyKey == idempotencyKey,
                    cancellation);

            if (existing is not null)
            {
                return string.Equals(existing.RequestFingerprint, fingerprint, StringComparison.Ordinal)
                    ? ReportRequestResult.Ok(existing, false)
                    : ReportRequestResult.Error(
                        ApiError.IdempotencyKeyReused,
                        "Ayni Idempotency-Key farkli bir istekle kullanildi.");
            }
        }

        DateTimeOffset now = clock.GetUtcNow();
        Guid reportId = Guid.CreateVersion7();

        string manifest = ReportManifest.Canonical(ManifestInput(ready, reference, registry));
        string manifestChecksum = ReportManifest.Checksum(manifest);

        AnalysisJobRow job = new()
        {
            Id = Guid.CreateVersion7(),
            RepositoryId = repositoryId,
            Kind = AnalysisJobKind.ReportGenerate,
            Status = AnalysisJobStatus.Queued,
            RequestedAtUtc = now,
            CurrentPhase = "queued",
            IdempotencyKey = null,

            // Aktif tekillik anahtari rapor kimligini iceriyor: ayni depo icin iki farkli
            // rapor istegi birbirini engellemiyor. Sinirsiz paralel uretimi engelleyen sey
            // worker es zamanliligi (varsayilan 1), kuyruk anahtari degil.
            ActiveDeduplicationKey = $"{repositoryId}:report-generate:{reportId:n}",
        };

        ReportArtifactRow artifact = new()
        {
            Id = reportId,
            AnalysisJobId = job.Id,
            RepositoryId = repositoryId,
            RiskAnalysisJobId = ready.RiskJob.Id,
            StaticAnalysisJobId = ready.StaticJob?.Id,
            Status = ReportArtifactStatus.Pending,
            Format = "pdf",
            Culture = resolved.Culture,
            SafeFileName = ReportFileName.For(repository.Name, now, ready.IsPartial),
            StorageKey = store.NewStorageKey(reportId),
            ContentType = "application/pdf",
            ManifestJson = manifest,
            ManifestSha256 = manifestChecksum,
            RequestedAtUtc = now,
            IdempotencyKey = idempotencyKey,
            RequestFingerprint = fingerprint,
            IsPartial = ready.IsPartial,
            SchemaVersion = ReportManifest.SchemaVersion,
            GeneratorVersion = ReportManifest.GeneratorVersion,
        };

        context.AnalysisJobs.Add(job);
        context.ReportArtifacts.Add(artifact);

        await context.SaveChangesAsync(cancellation);

        context.Entry(job).State = EntityState.Detached;
        context.Entry(artifact).State = EntityState.Detached;

        await queue.EnqueueAsync(job.Id, cancellation);

        return ReportRequestResult.Ok(artifact, true);
    }

    /// <summary>
    /// Istegi cozer: is kayitlari, model profili ve sinirlilik kodlari.
    ///
    /// Uretim isi de ayni yoldan geciyor, o yuzden bir raporun kabul edildigi kosullar
    /// ile uretildigi kosullar ayni koddan cikiyor.
    /// </summary>
    public async Task<ReportPlanResult> PlanAsync(
        RepositoryRow repository,
        ResolvedReportRequest request,
        CancellationToken cancellation)
    {
        AnalysisJobRow? risk = await context.AnalysisJobs
            .AsNoTracking()
            .FirstOrDefaultAsync(job => job.Id == request.RiskAnalysisJobId, cancellation);

        if (risk is null)
        {
            return ReportPlanResult.Error(ApiError.ReportRiskJobNotFound, "Risk analizi isi bulunamadi.");
        }

        if (risk.RepositoryId != repository.Id)
        {
            return ReportPlanResult.Error(
                ApiError.ReportRiskJobRepositoryMismatch, "Secilen is baska bir depoya ait.");
        }

        if (risk.Kind != AnalysisJobKind.RiskScoreAll)
        {
            return ReportPlanResult.Error(
                ApiError.ReportRiskJobKindMismatch, "Secilen is risk skorlamasi degil.");
        }

        bool partial = !risk.IsResultComplete;

        if (partial && !request.IncludePartial)
        {
            return ReportPlanResult.Error(
                ApiError.ReportPartialResultNotAllowed,
                "Is tamamlanmadi. Kismi sonuctan rapor icin includePartial=true gerekiyor.");
        }

        if (!await context.CommitRiskSnapshots
            .AsNoTracking()
            .AnyAsync(row => row.AnalysisJobId == risk.Id, cancellation))
        {
            return ReportPlanResult.Error(ApiError.ReportNoResults, "Iste kaydedilmis sonuc yok.");
        }

        AnalysisJobRow? staticJob = null;

        if (request.StaticAnalysisJobId is Guid staticId)
        {
            staticJob = await context.AnalysisJobs
                .AsNoTracking()
                .FirstOrDefaultAsync(job => job.Id == staticId, cancellation);

            if (staticJob is null
                || staticJob.RepositoryId != repository.Id
                || staticJob.Kind != AnalysisJobKind.StaticScan
                || staticJob.Status != AnalysisJobStatus.Succeeded
                || !staticJob.IsResultComplete)
            {
                return ReportPlanResult.Error(
                    ApiError.ReportStaticAnalysisIncompatible,
                    "Statik analiz isi bu rapora uygun degil: ayni depoda tamamlanmis bir tarama olmali.");
            }
        }

        if (registry.ForRepository(repository.Identity) is not ModelProfile profile)
        {
            return ReportPlanResult.Error(
                ApiError.UnknownRepositoryModel,
                "Bu depo icin egitilmis bir model profili yok.");
        }

        List<string> limitations = [.. ReportLimitation.Always];

        if (partial)
        {
            limitations.Add(ReportLimitation.PartialResult);
        }

        return ReportPlanResult.Ok(new ReportPlan(
            repository,
            risk,
            staticJob,
            profile,
            request,
            partial,
            partial ? RankingScope.WrittenResultsOnly : RankingScope.CompleteAnalysis,
            limitations));
    }

    /// <summary>Kaydedilmis manifestten parametreleri geri okur.</summary>
    public static ResolvedReportRequest Parameters(ReportArtifactRow artifact)
    {
        using JsonDocument document = JsonDocument.Parse(artifact.ManifestJson);
        JsonElement parameters = document.RootElement.GetProperty("reportParameters");

        return new ResolvedReportRequest(
            artifact.RiskAnalysisJobId,
            artifact.StaticAnalysisJobId,
            parameters.GetProperty("includePartial").GetBoolean(),
            parameters.GetProperty("commitWindow").GetInt32(),
            parameters.GetProperty("fileLimit").GetInt32(),
            parameters.GetProperty("timelineCount").GetInt32(),
            parameters.GetProperty("topCommitCount").GetInt32(),
            parameters.GetProperty("findingLimit").GetInt32(),
            artifact.Culture,
            Optional(parameters, "title"),
            Optional(parameters, "notes"));
    }

    /// <summary>Static tarama ozetinden bir sayi; ozet okunamazsa null.</summary>
    private static int? Summary(string? resultSummary, string name)
    {
        if (resultSummary is null or "")
        {
            return null;
        }

        try
        {
            using JsonDocument document = JsonDocument.Parse(resultSummary);

            return document.RootElement.TryGetProperty(name, out JsonElement value)
                && value.TryGetInt32(out int number)
                    ? number
                    : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? Optional(JsonElement parameters, string name) =>
        parameters.TryGetProperty(name, out JsonElement value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static ReportManifestInput ManifestInput(
        ReportPlan plan, ScoreReference reference, ModelRegistry registry) => new()
        {
            RepositoryId = plan.Repository.Id,
            RepositoryIdentity = plan.Repository.Identity,
            RepositoryDisplayName = plan.Repository.Name,
            Culture = plan.Request.Culture,
            IncludePartial = plan.Request.IncludePartial,
            CommitWindow = plan.Request.CommitWindow,
            FileLimit = plan.Request.FileLimit,
            TimelineCount = plan.Request.TimelineCount,
            TopCommitCount = plan.Request.TopCommitCount,
            FindingLimit = plan.Request.FindingLimit,
            Title = plan.Request.Title,
            Notes = plan.Request.Notes,
            RiskJobId = plan.RiskJob.Id,
            RiskJobStatus = AnalysisJobRow.Name(plan.RiskJob.Status),
            RiskJobComplete = plan.RiskJob.IsResultComplete,
            RankingScope = plan.RankingScope,
            RiskRequestedAtUtc = plan.RiskJob.RequestedAtUtc,
            RiskStartedAtUtc = plan.RiskJob.StartedAtUtc,
            RiskCompletedAtUtc = plan.RiskJob.CompletedAtUtc,
            RiskResultCount = plan.RiskJob.ResultCount,
            ModelProfile = plan.Profile.ProfileCode,
            ModelCodeCommit = plan.Profile.ModelCodeCommit,
            ModelChecksum = plan.Profile.ModelChecksum,
            TrainThreshold = plan.Profile.TrainThreshold,
            StaticJobId = plan.StaticJob?.Id,
            StaticSourceHeadSha = plan.StaticJob?.SourceHeadSha,
            StaticCompletedAtUtc = plan.StaticJob?.CompletedAtUtc,
            StaticFindingCount = plan.StaticJob?.ResultCount,
            StaticSuppressedCount = Summary(plan.StaticJob?.ResultSummary, "suppressedCount"),
            StaticExemptionCount = Summary(plan.StaticJob?.ResultSummary, "exemptionCount"),
            ScoreReferenceChecksum = reference.Checksum,
            ModelResultsChecksum = registry.ModelResultsChecksum,
            LimitationCodes = plan.Limitations,
            SievertVersion = ReportManifest.GeneratorVersion,
        };
}

/// <summary>Plan cozumunun sonucu.</summary>
public sealed record ReportPlanResult(ReportPlan? Plan, string? ErrorCode, string? Detail)
{
    public static ReportPlanResult Ok(ReportPlan plan) => new(plan, null, null);

    public static ReportPlanResult Error(string code, string detail) => new(null, code, detail);
}
