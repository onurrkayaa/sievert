using Sievert.Modeling;

namespace Sievert.Tests;

/// <summary>
/// Sayisal tolerans sozlesmesinin testleri: docs/urun/model-aciklama-sayisal-tolerans.md
/// surum 2.0.
///
/// Iki yonu birden sinaniyor. Toleransin fazla sert olmadigi (float olcegi kaynakli
/// farklar geciyor) ve fazla gevsek olmadigi (bilerek sokulmus bozukluk reddediliyor).
/// Ikincisi olmadan birincisi anlamsiz olurdu: her seyi kabul eden bir tolerans da
/// "gecti" der.
/// </summary>
public sealed class ExplanationToleranceTests
{
    private static readonly string ResultsPath = ProjectRoot.Combine("data", "asama5", "model-results.json");

    private static readonly string ModelDirectory = ProjectRoot.Combine("data", "asama5", "models");

    private static readonly Lazy<IReadOnlyList<SnapshotRow>> Rows =
        new(() => SnapshotReader.Read(ProjectRoot.Combine("data", "asama5", "commit-metrics.csv")));

    [Fact]
    public void TheUnitRoundoffIsExactlyTwoToTheMinusTwentyThree()
    {
        // Ikisi de calisma zamaninda hesaplaniyor: sabiti kendi tanimiyla karsilastirmak
        // derleyicinin ayni ifadeyi iki yere yazmasindan ibaret olurdu.
        double halving = 1.0;

        for (int step = 0; step < 23; step++)
        {
            halving /= 2.0;
        }

        Assert.Equal(ModelExplainer.FloatUnitRoundoff, halving);
        Assert.Equal(ModelExplainer.FloatUnitRoundoff, Math.Pow(2, -23));
    }

    /// <summary>
    /// <c>float.Epsilon</c> makine epsilonu DEGIL, temsil edilebilir en kucuk pozitif
    /// subnormal sayi. Buraya konursa tolerans pratikte sifirlanir; bu yaygin bir
    /// karistirma oldugu icin ayri bir test.
    /// </summary>
    [Fact]
    public void TheUnitRoundoffIsNotFloatEpsilon()
    {
        Assert.NotEqual(float.Epsilon, ModelExplainer.FloatUnitRoundoff);
        Assert.True(ModelExplainer.FloatUnitRoundoff > float.Epsilon * 1e30);
    }

    [Fact]
    public void TheBaseToleranceIsStillTheV1Value()
    {
        Assert.Equal(1e-6, ModelExplainer.AbsoluteToleranceV1);
    }

    [Fact]
    public void TheScaleGrowsWithContributionsThatCancelEachOther()
    {
        // Logit kucuk (0,0) ama terimler buyuk: olcek terimlerden gelmeli.
        ModelExplanation cancelling = Explanation(intercept: 0.0, [50.0, -50.0]);

        Assert.Equal(100.0, cancelling.Scale);
        Assert.True(cancelling.AllowedError > ModelExplainer.AbsoluteToleranceV1);

        ModelExplanation small = Explanation(intercept: 0.0, [0.1, -0.1]);

        Assert.Equal(1.0, small.Scale);
        Assert.True(cancelling.AllowedError > small.AllowedError);
    }

    [Fact]
    public void TheScaleNeverFallsBelowOne()
    {
        Assert.Equal(1.0, Explanation(intercept: 0.0, [0.0]).Scale);
    }

    [Fact]
    public void TheAllowedErrorFollowsTheContract()
    {
        ModelExplanation explanation = Explanation(intercept: -2.0, [10.0, -5.0]);

        double expectedScale = 2.0 + 10.0 + 5.0;
        double expected = 1e-6 + (ModelExplainer.FloatUnitRoundoff * expectedScale);

        Assert.Equal(expectedScale, explanation.Scale);
        Assert.Equal(expected, explanation.AllowedError);
    }

    [Fact]
    public void TheNormalizedErrorIsTheRatioAndOneIsTheBoundary()
    {
        ModelExplanation explanation = Explanation(intercept: 0.0, [10.0], driftFromModel: 0.0);

        Assert.Equal(0.0, explanation.NormalizedError);
        Assert.True(explanation.IsWithinTolerance);

        ModelExplanation exactlyAtTheEdge = Explanation(
            intercept: 0.0,
            [10.0],
            driftFromModel: 1e-6 + (ModelExplainer.FloatUnitRoundoff * 10.0));

        Assert.Equal(1.0, exactlyAtTheEdge.NormalizedError, 9);
        Assert.True(exactlyAtTheEdge.IsWithinTolerance);
    }

    /// <summary>
    /// Adim 2'de v1'de kalan satirlarin hepsi float olcegi kaynakliydi. Burada uc reponun
    /// gercek satirlariyla v2'nin onlari gecirdigi sinaniyor.
    /// </summary>
    [Theory]
    [InlineData("polly", "github.com/app-vnext/polly")]
    [InlineData("sharex", "github.com/sharex/sharex")]
    [InlineData("jellyfin", "github.com/jellyfin/jellyfin")]
    public void RealRowsPassTheScaledTolerance(string code, string identity)
    {
        (ModelProfile profile, FeatureScaler scaler, LoadedModel model) = Load(code);

        List<SnapshotRow> rows = [.. Rows.Value.Where(row => row.RepositoryIdentity == identity)];
        int step = Math.Max(1, rows.Count / 300);
        double worst = 0.0;

        for (int index = 0; index < rows.Count; index += step)
        {
            ModelExplanation explanation = ModelExplainer.Explain(profile, scaler, model, rows[index]);

            worst = Math.Max(worst, explanation.NormalizedError);

            Assert.True(explanation.IsWithinTolerance, $"{code}: normalize hata {explanation.NormalizedError:R}");
        }

        // Tolerans tam sinirda degil: gercek satirlarda pay kaliyor.
        Assert.True(worst < 1.0, $"{code}: en buyuk normalize hata {worst:R}");
    }

    /// <summary>
    /// Mutasyon testi. Tek bir katsayi, en az 1e-4 mutlak logit farki uretecek kadar
    /// bozuluyor ve aciklama gercek yoldan yeniden uretiliyor. v2 bunu reddetmeli.
    /// </summary>
    [Theory]
    [InlineData("polly", "github.com/app-vnext/polly")]
    [InlineData("sharex", "github.com/sharex/sharex")]
    [InlineData("jellyfin", "github.com/jellyfin/jellyfin")]
    public void ACorruptedCoefficientIsRejected(string code, string identity)
    {
        (ModelProfile profile, FeatureScaler scaler, LoadedModel model) = Load(code);

        SnapshotRow row = Rows.Value.First(item => item.RepositoryIdentity == identity);

        ModelExplanation honest = ModelExplainer.Explain(profile, scaler, model, row);

        Assert.True(honest.IsWithinTolerance);

        // Degeri sifirdan yeterince uzak bir oznitelik sec; bozulma logit'e gecsin.
        FeatureEffect target = honest.Effects
            .Where(effect => Math.Abs(effect.TransformedValue) > 1e-3)
            .MaxBy(effect => Math.Abs(effect.TransformedValue))!;

        int position = honest.Effects.ToList().FindIndex(effect => effect.Name == target.Name);

        const double LogitDrift = 1e-4;
        double[] weights = [.. profile.Coefficients.Weights];
        weights[position] += LogitDrift / target.TransformedValue;

        ModelProfile corrupted = profile with
        {
            Coefficients = new Coefficients(profile.Coefficients.Intercept, weights),
        };

        ModelExplanation mutated = ModelExplainer.Explain(corrupted, scaler, model, row);

        Assert.True(mutated.AbsoluteError >= LogitDrift * 0.9, $"bozulma logit'e gecmedi: {mutated.AbsoluteError:R}");
        Assert.False(mutated.IsWithinTolerance, $"{code}: bozuk aciklama kabul edildi, normalize hata {mutated.NormalizedError:R}");
        Assert.True(mutated.NormalizedError > 10, $"{code}: normalize hata yalnizca {mutated.NormalizedError:R}");
    }

    [Fact]
    public void ACorruptedInterceptIsRejected()
    {
        (ModelProfile profile, FeatureScaler scaler, LoadedModel model) = Load("polly");
        SnapshotRow row = Rows.Value.First(item => item.RepositoryIdentity == "github.com/app-vnext/polly");

        ModelProfile corrupted = profile with
        {
            Coefficients = new Coefficients(
                profile.Coefficients.Intercept + 1e-4,
                profile.Coefficients.Weights),
        };

        Assert.False(ModelExplainer.Explain(corrupted, scaler, model, row).IsWithinTolerance);
    }

    private static (ModelProfile Profile, FeatureScaler Scaler, LoadedModel Model) Load(string code)
    {
        ModelRegistry registry = ModelRegistry.Create(ResultsPath, ModelDirectory);
        ModelProfile profile = registry.Find(code)!;

        return (profile, registry.ScalerFor(profile), registry.Load(code));
    }

    /// <summary>Gercek model olmadan, yalniz sayisal kurali sinamak icin kurulmus aciklama.</summary>
    private static ModelExplanation Explanation(
        double intercept,
        double[] contributions,
        double driftFromModel = 0.0)
    {
        List<FeatureEffect> effects =
        [
            .. contributions.Select((value, index) =>
                new FeatureEffect($"F{index}", value, value, 1.0, value, false, null))
        ];

        double explained = intercept + contributions.Sum();

        return new ModelExplanation(intercept, explained - driftFromModel, explained, 0.5, effects);
    }
}
