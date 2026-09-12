using Sievert.Modeling;

using static Sievert.Tests.EvaluationMetricTests;

namespace Sievert.Tests;

/// <summary>
/// Oznitelik donusumu: log1p, standartlastirma ve bunlarin YALNIZCA egitim bolumunden
/// ogrenilmesi. Sozlesme Adim 3 promptunda sonuc gorulmeden sabitlendi.
/// </summary>
public class FeatureTransformTests
{
    [Fact]
    public void Log_OfZeroIsZero()
    {
        Assert.Equal(0.0, FeatureTransform.Log1P(0.0), 12);
    }

    [Fact]
    public void Log_MatchesTheHandComputedValue()
    {
        Assert.Equal(Math.Log(1 + 9), FeatureTransform.Log1P(9.0), 12);
    }

    [Fact]
    public void Log_RefusesANegativeValue()
    {
        Assert.Throws<InvalidDataException>(() => FeatureTransform.Log1P(-1.0));
    }

    [Fact]
    public void Fit_RefusesARepositoryWithANegativeCountFeature()
    {
        // Anlik goruntude olmamali; olursa model egitilmeden durulmali.
        IReadOnlyList<SnapshotRow> train = [Row(-5, sha: "a"), Row(10, sha: "b")];

        Assert.Throws<InvalidDataException>(() => FeatureScaler.Fit("a/b", train));
    }

    [Fact]
    public void Fit_RefusesAFeatureWithoutVariance()
    {
        // Butun satirlarda ayni deger: standart sapma 0, sessizce cikarilmiyor.
        IReadOnlyList<SnapshotRow> train = [Row(7, sha: "a"), Row(7, sha: "b")];

        InvalidDataException error = Assert.Throws<InvalidDataException>(() => FeatureScaler.Fit("a/b", train));

        Assert.Contains("a/b", error.Message, StringComparison.Ordinal);
        Assert.Contains("LinesAdded", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Fit_LearnsTheStatisticsOnTheLogScale()
    {
        IReadOnlyList<SnapshotRow> train = [Spread(0, 0.0), Spread(3, 1.0)];

        FeatureScaler scaler = FeatureScaler.Fit("a/b", train);
        FeatureStatistics statistics = scaler.Statistics.Single(entry => entry.Name == "LinesAdded");

        // log1p(0) = 0, log1p(3) = 1,3863 -> ortalama 0,6931
        Assert.Equal(0.0, statistics.Min, 12);
        Assert.Equal(Math.Log(4), statistics.Max, 12);
        Assert.Equal(Math.Log(4) / 2, statistics.Mean, 12);
    }

    [Fact]
    public void Apply_StandardisesWithTheTrainMeanAndDeviation()
    {
        IReadOnlyList<SnapshotRow> train = [Spread(0, 0.0), Spread(3, 1.0)];
        FeatureScaler scaler = FeatureScaler.Fit("a/b", train);

        int index = ModelFeatures.Candidates.ToList().IndexOf("LinesAdded");

        // Iki noktali bir kumede degerler tam olarak -1 ve +1'e oturuyor.
        Assert.Equal(-1.0, scaler.Apply(train[0])[index], 6);
        Assert.Equal(1.0, scaler.Apply(train[1])[index], 6);
    }

    [Fact]
    public void Apply_DoesNotClipAValueOutsideTheTrainRange()
    {
        IReadOnlyList<SnapshotRow> train = [Spread(0, 0.0), Spread(3, 1.0)];
        FeatureScaler scaler = FeatureScaler.Fit("a/b", train);

        int index = ModelFeatures.Candidates.ToList().IndexOf("LinesAdded");
        float far = scaler.Apply(Spread(1000, 0.5))[index];

        Assert.True(far > 1.0, "train araligi disindaki deger kirpilmamali");

        // Sayim DEGER bazinda: iki satirin 13 sayim ozniteligi de aralik disinda,
        // Entropy (0,5) araligin icinde. 2 x 13 = 26 deger, 2 satir.
        IReadOnlyList<SnapshotRow> far2 = [Spread(1000, 0.5), Spread(2000, 0.5)];

        Assert.Equal(26, scaler.OutsideTrainRange(far2));
        Assert.Equal(2, scaler.OutsideTrainRangeRows(far2));
        Assert.Equal(0, scaler.OutsideTrainRange(train));
        Assert.Equal(0, scaler.OutsideTrainRangeRows(train));
    }

    [Fact]
    public void Apply_LeavesIsFixAsZeroOrOne()
    {
        IReadOnlyList<SnapshotRow> train =
        [
            Spread(0, 0.0) with { IsFix = false },
            Spread(3, 1.0) with { IsFix = true },
        ];

        FeatureScaler scaler = FeatureScaler.Fit("a/b", train);
        int index = ModelFeatures.Candidates.ToList().IndexOf("IsFix");

        Assert.Equal(0.0f, scaler.Apply(train[0])[index]);
        Assert.Equal(1.0f, scaler.Apply(train[1])[index]);
        Assert.DoesNotContain(scaler.Statistics, entry => entry.Name == "IsFix");
    }

    [Fact]
    public void Apply_NormalisesEntropyWithoutLogging()
    {
        IReadOnlyList<SnapshotRow> train = [Spread(1, 0.0), Spread(2, 4.0)];
        FeatureScaler scaler = FeatureScaler.Fit("a/b", train);

        FeatureStatistics entropy = scaler.Statistics.Single(entry => entry.Name == "Entropy");

        // Log uygulansaydi maksimum log(5) = 1,609 olurdu; ham deger 4 kalmali.
        Assert.Equal(4.0, entropy.Max, 12);
        Assert.Equal(2.0, entropy.Mean, 12);
    }

    [Fact]
    public void Contract_CoversAllFifteenCandidatesExactlyOnce()
    {
        List<string> covered =
        [
            .. FeatureTransform.LogFeatures,
            FeatureTransform.RawContinuousFeature,
            FeatureTransform.FlagFeature,
        ];

        Assert.Equal(15, covered.Count);
        Assert.Equal(15, covered.Distinct(StringComparer.Ordinal).Count());
        Assert.Equal([.. ModelFeatures.Candidates.Order(StringComparer.Ordinal)], [.. covered.Order(StringComparer.Ordinal)]);
    }

    [Theory]
    [InlineData("LabelSource")]
    [InlineData("IsBugIntroducing")]
    [InlineData("BotMu")]
    [InlineData("Sha")]
    [InlineData("AuthorDateUtc")]
    public void Contract_ForbiddenFieldsAreNotPartOfTheTransform(string name)
    {
        Assert.DoesNotContain(name, FeatureTransform.LogFeatures);
        Assert.NotEqual(FeatureTransform.RawContinuousFeature, name);
        Assert.NotEqual(FeatureTransform.FlagFeature, name);
    }

    /// <summary>Butun sayim alanlari ayni degeri, Entropy ayri bir deger aliyor.</summary>
    private static SnapshotRow Spread(int count, double entropy) =>
        new("klasor", "a/b", "s" + count + "-" + entropy, DateTimeOffset.UnixEpoch,
            count, count, count, count, entropy, count, count, count, count,
            count, count, count, count, count, false, false, null, false);
}
