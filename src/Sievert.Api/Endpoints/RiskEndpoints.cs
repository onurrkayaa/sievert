using Microsoft.EntityFrameworkCore;

using Sievert.Data;
using Sievert.Data.Entities;
using Sievert.Modeling;

namespace Sievert.Api.Endpoints;

/// <summary>
/// Tek bir commit icin model degerlendirmesi.
///
/// Cevapta dort ayri sayi var ve hicbiri digerinin yerine gecmiyor: ham model skoru,
/// 0,5 esigine gore karar, egitim esigine gore karar, goreli endeks. Tek bir "risk
/// yuzdesi" uretmemek bilincli: skor kalibre edilmedi.
/// </summary>
public static class RiskEndpoints
{
    /// <summary>Kabul edilen en kisa sha oneki. Daha kisasi cok fazla commit'e uyuyor.</summary>
    private const int MinimumShaLength = 7;

    public static void MapRisk(this RouteGroupBuilder api) =>
        api.MapGet("/repositories/{repositoryId:int}/commits/{sha}/risk", async (
            int repositoryId,
            string sha,
            HttpContext context,
            DatabaseSettings settings,
            ModelRegistry registry,
            ScoreReference reference,
            CancellationToken cancellation) =>
        {
            if (Database.NotReady(context, settings) is IResult unavailable)
            {
                return unavailable;
            }

            if (!IsSha(sha))
            {
                return Problems.BadRequest(
                    context,
                    $"Commit kimligi en az {MinimumShaLength}, en fazla 40 onaltilik karakter olmali.",
                    ApiError.InvalidSha);
            }

            SievertContext database = Database.Open(context);

            RepositoryRow? repository = await database.Repositories
                .FirstOrDefaultAsync(row => row.Id == repositoryId, cancellation);

            if (repository is null)
            {
                return Problems.NotFound(context, $"{repositoryId} numarali depo yok.", ApiError.RepositoryNotFound);
            }

            List<CommitRow> matches = await database.Commits
                .Where(row => row.RepositoryId == repositoryId && row.Sha.StartsWith(sha))
                .Take(2)
                .ToListAsync(cancellation);

            if (matches.Count == 0)
            {
                return Problems.NotFound(
                    context,
                    $"{sha} ile baslayan bir commit bu depoda yok.",
                    ApiError.CommitNotFound);
            }

            if (matches.Count > 1)
            {
                return Problems.Conflict(
                    context,
                    $"{sha} oneki birden fazla commit'e uyuyor; daha uzun bir onek ver.",
                    ApiError.AmbiguousSha);
            }

            CommitRow commit = matches[0];

            CommitMetricRow? metric = await database.CommitMetrics
                .FirstOrDefaultAsync(row => row.CommitId == commit.Id, cancellation);

            if (metric is null)
            {
                return Problems.Unprocessable(
                    context,
                    "Bu commit icin turetilmis olcu yok; once metrik hesabi calistirilmali.",
                    ApiError.CommitMetricsMissing);
            }

            if (context.Request.Query.ContainsKey("profile"))
            {
                // Adim 2'de vardi, Adim 3'te kaldirildi (risk sozlesmesi surum 1.1).
                // Sessizce yok saymiyorum: istegin sahibi sectigi profille skorlandigini
                // sanardi.
                return Problems.BadRequest(
                    context,
                    "profile parametresi kaldirildi. Model profilini depo kimligi belirliyor; "
                    + "bilinen depo kendi profilini kullanir, bilinmeyen depo skorlanmaz.",
                    ApiError.ProfileSelectionNotSupported);
            }

            if (registry.ForRepository(repository.Identity) is not ModelProfile profile)
            {
                // Sessiz varsayilan yok: bilinmeyen depo icin bir profil secmek, olculmemis
                // bir aktarimi olculmus gibi gostermek olurdu.
                return Problems.Unprocessable(
                    context,
                    RiskWarning.Text(RiskWarning.UnknownRepositoryModel)
                    + $" Egitilmis profiller: {string.Join(", ", registry.Profiles.Select(item => item.ProfileCode))}.",
                    ApiError.UnknownRepositoryModel);
            }

            if (registry.StatusOf(profile.ProfileCode) == ModelStatus.ChecksumMismatch)
            {
                return Problems.Conflict(
                    context,
                    $"{profile.ProfileCode} modelinin ozeti kayitli degerle ayni degil; model kullanilmadi.",
                    ApiError.ModelChecksumMismatch);
            }

            if (reference.For(profile.ProfileCode) is not ScoreDistribution distribution)
            {
                return Problems.Unavailable(
                    context,
                    $"{profile.ProfileCode} icin egitim skor dagilimi yok; goreli endeks uretilemedi.",
                    ApiError.ScoreReferenceNotReady);
            }

            SnapshotRow row = CommitFeatures.ToSnapshotRow(repository, commit, metric);

            ModelExplanation explanation = ModelExplainer.Explain(
                profile,
                registry.ScalerFor(profile),
                registry.Load(profile.ProfileCode),
                row);

            if (!explanation.IsWithinTolerance)
            {
                // Aciklama modelle tutmuyorsa cevap DONMUYOR. Yanlis bir aciklama,
                // aciklama olmamasindan kotu. Tolerans sozlesmesi surum 2.0:
                // docs/urun/model-aciklama-sayisal-tolerans.md.
                //
                // Ayrintida sayisal ic durum, katsayi ya da oznitelik degeri YOK; onlar
                // gunluge gidiyor, cevaba degil.
                return Problems.Create(
                    context,
                    StatusCodes.Status500InternalServerError,
                    "Aciklama modelle tutmadi",
                    "Model aciklamasi dogrulanamadi, bu yuzden aciklama uretilmedi.",
                    ApiError.ModelExplanationMismatch);
            }

            return Results.Ok(Assess(repository, commit, metric, profile, explanation, distribution));
        })
        .WithName("CommitRisk")
        .WithSummary("Tek bir commit icin ham model skoru, kararlar, goreli endeks ve katkilar")
        .Produces<CommitRiskAssessment>()
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

    private static CommitRiskAssessment Assess(
        RepositoryRow repository,
        CommitRow commit,
        CommitMetricRow metric,
        ModelProfile profile,
        ModelExplanation explanation,
        ScoreDistribution distribution)
    {
        List<string> warnings = [.. RiskWarning.Always];

        if (metric.CsFilesChanged == 0)
        {
            warnings.Add(RiskWarning.CsLabelCoverageLimit);
        }

        if (explanation.AnyOutsideTrainRange)
        {
            warnings.Add(RiskWarning.OutsideTrainRange);
        }

        List<FeatureContribution> positive = Rank(explanation.Effects, above: true);
        List<FeatureContribution> negative = Rank(explanation.Effects, above: false);

        return new CommitRiskAssessment(
            repository.Id,
            repository.Identity,
            commit.Sha,
            profile.ProfileCode,
            profile.ModelCodeCommit,
            profile.ShortChecksum,
            explanation.RawModelScore,
            profile.IsCalibrated,
            distribution.RiskIndex(explanation.RawModelScore),
            $"{profile.ProfileCode} profilinin egitim bolumundeki {distribution.TrainCount} skorun "
            + "ampirik yuzdeligi; esitlikte orta sira",
            explanation.RawModelScore >= 0.5,
            explanation.RawModelScore >= profile.TrainThreshold,
            profile.TrainThreshold,
            "SZZ ile isaretlenmis hata getiren commit",
            [.. explanation.Effects.Select(effect =>
                new FeatureValue(effect.Name, effect.RawValue, effect.TransformedValue))],
            positive,
            negative,
            warnings,
            RiskWarning.Describe(warnings),
            new StaticAnalysisSection("not-run", [], IncludedInModelScore: false),
            new AuditSection(commit.IsBugIntroducing, commit.LabelSource));
    }

    /// <summary>
    /// Bir yondeki en guclu bes katki. Katkisi tam sifir olan oznitelik listeye
    /// girmiyor: sifir bir yon degil.
    /// </summary>
    private static List<FeatureContribution> Rank(IReadOnlyList<FeatureEffect> effects, bool above)
    {
        List<FeatureEffect> selected =
        [
            .. effects
                .Where(effect => above ? effect.Contribution > 0 : effect.Contribution < 0)
                .OrderByDescending(effect => Math.Abs(effect.Contribution))
                .ThenBy(effect => effect.Name, StringComparer.Ordinal)
                .Take(5)
        ];

        List<FeatureContribution> ranked = [];

        for (int index = 0; index < selected.Count; index++)
        {
            FeatureEffect effect = selected[index];
            string direction = above ? "up" : "down";

            ranked.Add(new FeatureContribution(
                effect.Name,
                effect.RawValue,
                effect.TransformedValue,
                effect.Coefficient,
                effect.Contribution,
                direction,
                index + 1,
                "contribution." + direction,
                above
                    ? "Model bu satirda skoru yukari tasidi."
                    : "Model bu satirda skoru asagi tasidi.",
                effect.OutsideTrainRange,
                effect.OutsideDirection));
        }

        return ranked;
    }

    private static bool IsSha(string value)
    {
        if (value.Length is < MinimumShaLength or > 40)
        {
            return false;
        }

        foreach (char character in value)
        {
            if (!Uri.IsHexDigit(character))
            {
                return false;
            }
        }

        return true;
    }
}
