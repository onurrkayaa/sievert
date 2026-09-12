using Sievert.Data.Entities;
using Sievert.Modeling;

namespace Sievert.Api.Analysis;

/// <summary>Tek bir commit icin hesaplanan degerlendirme.</summary>
/// <param name="Explanation">Model skoru ve katkilari.</param>
/// <param name="RiskIndex">Profilin egitim dagilimindaki yuzdelik sira.</param>
/// <param name="Warnings">Uyari kodlari, sabit sirada.</param>
public sealed record CommitRiskResult(
    ModelExplanation Explanation,
    double RiskIndex,
    IReadOnlyList<string> Warnings);

/// <summary>
/// Bir commit'in degerlendirmesini uretir.
///
/// Risk ucu ile arka plan isi bu **ayni** siniftan geciyor. Iki ayri hesap olsaydi
/// "aciklama ucunun verdigi skor ile toplu skorlamanin verdigi skor ayni mi" sorusunun
/// cevabi umuda kalirdi; boyle olunca ayni olmak zorunda.
/// </summary>
public static class CommitRiskCalculator
{
    public static CommitRiskResult Compute(
        ModelProfile profile,
        FeatureScaler scaler,
        LoadedModel model,
        ScoreDistribution distribution,
        RepositoryRow repository,
        CommitRow commit,
        CommitMetricRow metric)
    {
        SnapshotRow row = CommitFeatures.ToSnapshotRow(repository, commit, metric);
        ModelExplanation explanation = ModelExplainer.Explain(profile, scaler, model, row);

        List<string> warnings = [.. RiskWarning.Always];

        if (metric.CsFilesChanged == 0)
        {
            warnings.Add(RiskWarning.CsLabelCoverageLimit);
        }

        if (explanation.AnyOutsideTrainRange)
        {
            warnings.Add(RiskWarning.OutsideTrainRange);
        }

        return new CommitRiskResult(
            explanation,
            distribution.RiskIndex(explanation.RawModelScore),
            warnings);
    }
}
