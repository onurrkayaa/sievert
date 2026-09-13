using Microsoft.EntityFrameworkCore;

using Sievert.Api.Analysis;
using Sievert.Api.Visualizations;
using Sievert.Contracts;
using Sievert.Data;
using Sievert.Data.Entities;
using Sievert.Modeling;

namespace Sievert.Api.Reports;

/// <summary>
/// Rapor modelini veritabanindan kurar.
///
/// Gorsellestirme ucleriyle **ayni** kurucular kullaniliyor (<see cref="FileActivityBuilder"/>,
/// <see cref="TimelineBuilder"/>). Rapor kendi hesabini yapsaydi ekranla PDF'in ayni
/// sayiyi gostermesi umuda kalirdi; boyle olunca ayni olmak zorunda.
/// </summary>
public sealed class ReportDataBuilder(
    SievertContext context,
    ModelRegistry registry,
    ScoreReference reference)
{
    /// <summary>Ozette gosterilen "en cok dokunulan dosya" sayisi.</summary>
    private const int MostTouchedCount = 5;

    /// <summary>Ozette gosterilen "en yuksek endeksli commit" sayisi.</summary>
    private const int HighestCommitCount = 5;

    public async Task<ReportModel> BuildAsync(
        ReportPlan plan,
        Guid reportId,
        DateTimeOffset generatedAtUtc,
        string manifestSha256,
        CancellationToken cancellation)
    {
        VisualizationQueries queries = new(context);

        List<WindowCommit> window = await queries.WindowAsync(
            plan.RiskJob.Id, plan.Request.CommitWindow, cancellation);

        List<WindowFile> files = await queries.FilesAsync(
            plan.RiskJob.Id, plan.Request.CommitWindow, cancellation);

        Dictionary<string, int>? staticCounts = plan.StaticJob is null
            ? null
            : await queries.StaticFindingCountsAsync(plan.StaticJob.Id, cancellation);

        List<FileActivityItem> activity = FileActivityBuilder.Build(
            window,
            files,
            plan.Request.FileLimit,
            FileActivitySort.MeanRiskDescending,
            staticCounts,
            plan.StaticJob?.Id,
            out int fileCountBeforeLimit);

        // Zaman cizelgesi kendi sayisini kullaniyor: pencere ile ayni olmak zorunda degil.
        List<WindowCommit> timelineWindow = plan.Request.TimelineCount == plan.Request.CommitWindow
            ? window
            : await queries.WindowAsync(plan.RiskJob.Id, plan.Request.TimelineCount, cancellation);

        List<RiskTimelinePoint> timeline = TimelineBuilder.Build(timelineWindow);

        ModelProfile profile = plan.Profile;
        ScoreDistribution? distribution = reference.For(profile.ProfileCode);

        double thresholdAt05 = distribution?.RiskIndex(0.5) ?? 0;
        double thresholdAtTrain = distribution?.RiskIndex(profile.TrainThreshold) ?? 0;

        List<ReportCommitRow> ranked = [.. window
            .Select(Row)
            .OrderByDescending(row => row.RiskIndex)
            .ThenByDescending(row => row.AuthorDateUtc)
            .ThenBy(row => row.Sha, StringComparer.Ordinal)];

        List<ReportCommitRow> top = [.. ranked.Take(plan.Request.TopCommitCount)];

        ReportSummary summary = Summarise(plan, window, activity, ranked, staticCounts);

        ReportStaticSection staticSection = await StaticAsync(plan, cancellation);

        List<ReportExplanation> explanations = await ExplainAsync(plan, ranked, cancellation);

        return new ReportModel(
            new ReportCover(
                plan.Repository.Name,
                plan.Repository.Identity,
                reportId,
                generatedAtUtc,
                plan.RiskJob.Id,
                plan.StaticJob?.Id,
                profile.ProfileCode,
                profile.ShortChecksum,
                profile.IsCalibrated,
                plan.IsPartial,
                plan.Request.Title,
                plan.Request.Notes),
            summary,
            timeline,
            thresholdAt05,
            thresholdAtTrain,
            activity,
            fileCountBeforeLimit,
            FileActivitySort.MeanRiskDescending,
            top,
            staticSection,
            explanations,
            plan.Limitations,
            new ReportProvenance(
                manifestSha256,
                profile.ModelChecksum,
                reference.Checksum,
                registry.ModelResultsChecksum,
                plan.RiskJob.Id,
                plan.StaticJob?.Id,
                ReportManifest.SchemaVersion,
                ReportManifest.GeneratorVersion));
    }

    private static ReportCommitRow Row(WindowCommit commit) => new(
        commit.Sha,
        commit.ShortSha,
        commit.AuthorDateUtc,
        Text.Plain(commit.MessageSubject, 160),
        commit.RiskIndex,
        commit.RawModelScore,
        commit.DecisionAt05,
        commit.DecisionAtTrainThreshold,
        commit.LinesAdded + commit.LinesDeleted,
        commit.CsFilesChanged,
        Codes(commit.WarningCodes));

    /// <summary>
    /// Uyari kodlari veritabaninda JSON dizi metni olarak duruyor.
    ///
    /// Ilk surumde virgulle ayrilmis metin sanilmisti ve raporda kodlar tirnak ve
    /// koseli parantezle birlikte basildi. Gorsel kontrolde yakalandi; zaman cizelgesi
    /// ayni isi zaten dogru yapiyordu.
    /// </summary>
    private static IReadOnlyList<string> Codes(string raw)
    {
        if (raw.Length == 0)
        {
            return [];
        }

        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<List<string>>(raw) ?? [];
        }
        catch (System.Text.Json.JsonException)
        {
            return [];
        }
    }

    private static ReportSummary Summarise(
        ReportPlan plan,
        IReadOnlyList<WindowCommit> window,
        IReadOnlyList<FileActivityItem> activity,
        IReadOnlyList<ReportCommitRow> ranked,
        Dictionary<string, int>? staticCounts)
    {
        double[] indices = [.. window.Select(commit => commit.RiskIndex).Order()];

        double mean = indices.Length == 0 ? 0 : indices.Average();
        double median = indices.Length == 0
            ? 0
            : indices.Length % 2 == 1
                ? indices[indices.Length / 2]
                : (indices[(indices.Length / 2) - 1] + indices[indices.Length / 2]) / 2;

        List<ReportTouchedFile> touched = [.. activity
            .OrderByDescending(item => item.TouchCount)
            .ThenBy(item => item.RelativePath, StringComparer.Ordinal)
            .Take(MostTouchedCount)
            .Select(item => new ReportTouchedFile(item.RelativePath, item.TouchCount, item.MeanRiskIndex))];

        return new ReportSummary(
            window.Count,
            window.Count == 0 ? null : window[0].AuthorDateUtc,
            window.Count == 0 ? null : window[^1].AuthorDateUtc,
            plan.Request.CommitWindow,
            Round(mean),
            Round(median),
            indices.Length == 0 ? 0 : indices[^1],
            window.Count(commit => commit.DecisionAt05),
            window.Count(commit => commit.DecisionAtTrainThreshold),
            plan.Profile.TrainThreshold,
            staticCounts?.Values.Sum(),
            touched,
            [.. ranked.Take(HighestCommitCount)],
            plan.RankingScope);
    }

    private async Task<ReportStaticSection> StaticAsync(ReportPlan plan, CancellationToken cancellation)
    {
        if (plan.StaticJob is not AnalysisJobRow job)
        {
            return new ReportStaticSection(
                false, null, null, null, false, null, 0, 0, 0, [], [], []);
        }

        List<StaticAnalysisFindingRow> findings = await context.StaticAnalysisFindings
            .AsNoTracking()
            .Where(finding => finding.AnalysisJobId == job.Id)
            .OrderBy(finding => finding.RuleCode)
            .ThenBy(finding => finding.RelativePath)
            .ThenBy(finding => finding.Line)
            .ThenBy(finding => finding.Id)
            .ToListAsync(cancellation);

        List<ReportRuleCount> rules = [.. findings
            .GroupBy(finding => finding.RuleCode, StringComparer.Ordinal)
            .Select(group => new ReportRuleCount(group.Key, group.Count()))
            .OrderBy(row => row.RuleCode, StringComparer.Ordinal)];

        List<ReportSeverityCount> severities = [.. findings
            .GroupBy(finding => finding.Severity, StringComparer.Ordinal)
            .Select(group => new ReportSeverityCount(group.Key, group.Count()))
            .OrderBy(row => row.Severity, StringComparer.Ordinal)];

        (int suppressed, int exemptions) = Summary(job.ResultSummary);

        return new ReportStaticSection(
            true,
            job.Id,
            job.SourceHeadSha,
            job.SourceTreeState,
            job.SourceStateVerifiedAtUtc is not null && !job.SourceCommitChangedDuringAnalysis,
            job.CompletedAtUtc,
            findings.Count,
            suppressed,
            exemptions,
            rules,
            severities,
            [.. findings.Take(plan.Request.FindingLimit).Select(Finding)]);
    }

    private static StaticFindingResponse Finding(StaticAnalysisFindingRow row) => new(
        row.RuleCode,
        row.Severity,
        row.RelativePath,
        row.Line,
        row.Column,
        row.MemberName,
        Text.Plain(row.Message, 300),
        Text.Plain(row.Rationale, 300),
        row.IsTestCode);

    /// <summary>Static tarama ozetindeki susturma ve muafiyet sayilari.</summary>
    private static (int Suppressed, int Exemptions) Summary(string? resultSummary)
    {
        if (resultSummary is null or "")
        {
            return (0, 0);
        }

        try
        {
            using System.Text.Json.JsonDocument document = System.Text.Json.JsonDocument.Parse(resultSummary);

            return (
                Read(document, "suppressedCount"),
                Read(document, "exemptionCount"));
        }
        catch (System.Text.Json.JsonException)
        {
            // Ozet okunamiyorsa sifir yazmak yerine sifir donuluyor ve bolum zaten
            // bulgu sayisini ayrica gosteriyor.
            return (0, 0);
        }

        static int Read(System.Text.Json.JsonDocument document, string name) =>
            document.RootElement.TryGetProperty(name, out System.Text.Json.JsonElement value)
            && value.TryGetInt32(out int number)
                ? number
                : 0;
    }

    /// <summary>
    /// En yuksek endeksli birkac commit icin model aciklamasi.
    ///
    /// Aciklama yeniden hesaplaniyor, kaydedilmis skorla karsilastirilmiyor: katkilarin
    /// toplami modelin logit'ini tutuyor mu sorusu her raporda yeniden soruluyor.
    /// </summary>
    private async Task<List<ReportExplanation>> ExplainAsync(
        ReportPlan plan,
        IReadOnlyList<ReportCommitRow> ranked,
        CancellationToken cancellation)
    {
        List<ReportExplanation> explanations = [];

        if (reference.For(plan.Profile.ProfileCode) is not ScoreDistribution distribution)
        {
            return explanations;
        }

        string[] shas = [.. ranked.Take(ReportLimits.ExplainedCommitCount).Select(row => row.Sha)];

        if (shas.Length == 0)
        {
            return explanations;
        }

        List<CommitRow> commits = await context.Commits
            .AsNoTracking()
            .Where(commit => commit.RepositoryId == plan.Repository.Id && shas.Contains(commit.Sha))
            .ToListAsync(cancellation);

        Dictionary<int, CommitMetricRow> metrics = await context.CommitMetrics
            .AsNoTracking()
            .Where(metric => commits.Select(commit => commit.Id).Contains(metric.CommitId))
            .ToDictionaryAsync(metric => metric.CommitId, cancellation);

        FeatureScaler scaler = registry.ScalerFor(plan.Profile);
        LoadedModel model = registry.Load(plan.Profile.ProfileCode);

        foreach (string sha in shas)
        {
            if (commits.Find(commit => commit.Sha == sha) is not CommitRow commit
                || !metrics.TryGetValue(commit.Id, out CommitMetricRow? metric))
            {
                continue;
            }

            CommitRiskResult result = CommitRiskCalculator.Compute(
                plan.Profile, scaler, model, distribution, plan.Repository, commit, metric);

            List<ReportContribution> effects = [.. result.Explanation.Effects
                .Select(effect => new ReportContribution(
                    effect.Name,
                    effect.RawValue,
                    effect.TransformedValue,
                    effect.Coefficient,
                    effect.Contribution))];

            explanations.Add(new ReportExplanation(
                commit.Sha.Length >= 12 ? commit.Sha[..12] : commit.Sha,
                result.Explanation.RawModelScore,
                result.RiskIndex,
                result.Explanation.IsWithinTolerance,
                [.. effects.Where(effect => effect.Contribution > 0)
                    .OrderByDescending(effect => effect.Contribution)
                    .Take(ReportLimits.ContributionsPerSide)],
                [.. effects.Where(effect => effect.Contribution < 0)
                    .OrderBy(effect => effect.Contribution)
                    .Take(ReportLimits.ContributionsPerSide)]));
        }

        return explanations;
    }

    private static double Round(double value) => Math.Round(value, 1, MidpointRounding.AwayFromZero);
}
