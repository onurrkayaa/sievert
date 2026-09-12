using Sievert.Modeling;

namespace Sievert.Tests;

/// <summary>
/// Aciklama testleri. Sinanan sey su: katkilarin toplami modelin kendi logit'ini
/// veriyor mu. Vermiyorsa aciklama modeli anlatmiyor demektir.
/// </summary>
public sealed class ModelExplanationTests
{
    private static readonly string ResultsPath = ProjectRoot.Combine("data", "asama5", "model-results.json");

    private static readonly string ModelDirectory = ProjectRoot.Combine("data", "asama5", "models");

    private static readonly string SnapshotPath = ProjectRoot.Combine("data", "asama5", "commit-metrics.csv");

    private static readonly Lazy<IReadOnlyList<SnapshotRow>> Rows =
        new(() => SnapshotReader.Read(SnapshotPath));

    [Theory]
    [InlineData("polly", "github.com/app-vnext/polly")]
    [InlineData("sharex", "github.com/sharex/sharex")]
    [InlineData("jellyfin", "github.com/jellyfin/jellyfin")]
    public void TheContributionsAddUpToTheModelLogit(string code, string identity)
    {
        ModelRegistry registry = ModelRegistry.Create(ResultsPath, ModelDirectory);
        ModelProfile profile = registry.Find(code)!;
        FeatureScaler scaler = registry.ScalerFor(profile);
        LoadedModel model = registry.Load(code);

        // Depodan 200 satir; bastan degil, esit araliklarla, dagilimin her yerinden.
        List<SnapshotRow> rows = [.. Rows.Value.Where(row => row.RepositoryIdentity == identity)];
        int step = Math.Max(1, rows.Count / 200);

        double worst = 0.0;

        for (int index = 0; index < rows.Count; index += step)
        {
            ModelExplanation explanation = ModelExplainer.Explain(profile, scaler, model, rows[index]);

            worst = Math.Max(worst, explanation.Difference);

            Assert.Equal(15, explanation.Effects.Count);
        }

        Assert.True(
            worst <= ModelExplainer.Tolerance,
            $"{code}: en buyuk logit farki {worst:R}, tolerans {ModelExplainer.Tolerance:R}");
    }

    [Fact]
    public void AValueOutsideTheTrainRange_IsFlaggedAndTheScoreIsNotClipped()
    {
        ModelRegistry registry = ModelRegistry.Create(ResultsPath, ModelDirectory);
        ModelProfile profile = registry.Find("polly")!;
        FeatureScaler scaler = registry.ScalerFor(profile);
        LoadedModel model = registry.Load("polly");

        SnapshotRow normal = Rows.Value.First(row => row.RepositoryIdentity == "github.com/app-vnext/polly");

        // Egitimde gorulmemis buyuklukte bir commit.
        SnapshotRow huge = normal with { LinesAdded = 5_000_000 };

        ModelExplanation plain = ModelExplainer.Explain(profile, scaler, model, normal);
        ModelExplanation extreme = ModelExplainer.Explain(profile, scaler, model, huge);

        Assert.False(plain.Effects.Single(effect => effect.Name == "LinesAdded").OutsideTrainRange);

        FeatureEffect flagged = extreme.Effects.Single(effect => effect.Name == "LinesAdded");

        Assert.True(flagged.OutsideTrainRange);
        Assert.Equal("above", flagged.OutsideDirection);
        Assert.True(extreme.AnyOutsideTrainRange);

        // Kirpilmadi: deger degistigi icin skor da degisti.
        Assert.NotEqual(plain.RawModelScore, extreme.RawModelScore);
        Assert.Equal(5_000_000.0, flagged.RawValue);
        Assert.True(extreme.Difference <= ModelExplainer.Tolerance);
    }

    [Fact]
    public void TheInterceptIsPartOfTheLogitButNotAFeature()
    {
        ModelRegistry registry = ModelRegistry.Create(ResultsPath, ModelDirectory);
        ModelProfile profile = registry.Find("sharex")!;
        FeatureScaler scaler = registry.ScalerFor(profile);
        LoadedModel model = registry.Load("sharex");

        SnapshotRow row = Rows.Value.First(item => item.RepositoryIdentity == "github.com/sharex/sharex");
        ModelExplanation explanation = ModelExplainer.Explain(profile, scaler, model, row);

        double sum = explanation.Effects.Sum(effect => effect.Contribution);

        Assert.Equal(explanation.Intercept + sum, explanation.ExplainedLogit, 12);
        Assert.DoesNotContain(explanation.Effects, effect => effect.Name == "Intercept");
    }
}
