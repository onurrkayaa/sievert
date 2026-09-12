using Sievert.Modeling;

namespace Sievert.Tests;

/// <summary>
/// Lojistik regresyon modeli ve olasilik esikleri. Kucuk, elle kurulmus bolmelerle
/// calisiyor; dondurulmus veri kumesine dokunmuyor.
/// </summary>
public class ModelTests
{
    // --- Esik ---

    [Fact]
    public void Threshold_FixedOneIsHalf()
    {
        Assert.Equal(0.5, ProbabilityThreshold.Fixed);

        IReadOnlyList<Scored> scored = ProbabilityThreshold.Apply(
            [new(0.49, true), new(0.5, false), new(0.51, true)],
            ProbabilityThreshold.Fixed);

        Assert.Equal([false, true, true], scored.Select(row => row.Predicted));
    }

    [Fact]
    public void Threshold_ComesOnlyFromTrainLabels()
    {
        IReadOnlyList<ScoredProbability> train =
        [
            new(0.90, true),
            new(0.60, true),
            new(0.40, false),
            new(0.10, false),
        ];

        Assert.Equal(0.60, ProbabilityThreshold.Choose(train).Threshold, 12);
    }

    [Fact]
    public void Threshold_DoesNotMoveWhenTestLabelsAreFlipped()
    {
        IReadOnlyList<ScoredProbability> train = [new(0.9, true), new(0.6, true), new(0.4, false), new(0.1, false)];

        double before = ProbabilityThreshold.Choose(train).Threshold;

        // Test tarafi tamamen degisti; egitim ayni kaldi.
        _ = ProbabilityThreshold.Apply([new(0.9, false), new(0.1, true)], before);

        Assert.Equal(before, ProbabilityThreshold.Choose(train).Threshold, 12);
    }

    [Fact]
    public void Threshold_ScoresStayRawSoPrAucMeasuresRanking()
    {
        IReadOnlyList<Scored> scored = ProbabilityThreshold.Apply(
            [new(0.9, true), new(0.8, false), new(0.7, true), new(0.6, false)],
            threshold: 0.75);

        // Skorlar ham olasilik olarak kaldi: sozlesmedeki 19/24 ornegiyle ayni egri.
        Assert.Equal(19.0 / 24.0, Evaluation.Of(scored).PrAuc!.Value, 12);
        Assert.Equal(2, scored.Count(row => row.Predicted));
    }

    // --- Model ---

    [Fact]
    public void Model_TheSameSeedGivesTheSameProbabilities()
    {
        RepositorySplit split = Split();

        RepositoryModel first = LogisticRegressionModel.Train(split);
        RepositoryModel second = LogisticRegressionModel.Train(split);

        Assert.Equal(first.TestProbabilities, second.TestProbabilities);
        Assert.Equal(first.Coefficients.Weights, second.Coefficients.Weights);
        Assert.Equal(first.Coefficients.Intercept, second.Coefficients.Intercept, 12);
    }

    [Fact]
    public void Model_ProbabilitiesAreFiniteAndInsideTheUnitInterval()
    {
        RepositoryModel model = LogisticRegressionModel.Train(Split());

        Assert.All(model.TestProbabilities, probability =>
        {
            Assert.True(double.IsFinite(probability));
            Assert.InRange(probability, 0.0, 1.0);
        });
    }

    [Fact]
    public void Model_SeesExactlyFifteenColumns()
    {
        RepositorySplit split = Split();
        FeatureScaler scaler = FeatureScaler.Fit(split.Identity, split.Train);

        IReadOnlyList<ModelInput> rows = LogisticRegressionModel.Rows(split.Train, scaler);

        Assert.All(rows, row => Assert.Equal(15, row.Features.Length));
        Assert.Equal(15, LogisticRegressionModel.Train(split).Coefficients.Weights.Count);
    }

    [Fact]
    public void Model_KeepsItsPredictionsAfterBeingSavedAndLoaded()
    {
        RepositorySplit split = Split();
        RepositoryModel model = LogisticRegressionModel.Train(split);

        string path = Path.Combine(Path.GetTempPath(), "sievert-model-" + Guid.NewGuid().ToString("n") + ".zip");
        LogisticRegressionModel.Save(split, path);

        IReadOnlyList<double> loaded = LogisticRegressionModel.Load(
            path,
            LogisticRegressionModel.Rows(split.Test, model.Scaler));

        Assert.Equal(model.TestProbabilities, loaded);

        File.Delete(path);
    }

    [Fact]
    public void Model_RefusesAForbiddenFieldAsAPredictor()
    {
        Assert.Throws<InvalidOperationException>(() => ModelFeatures.Value(Row(1, 1, false), "LabelSource"));
        Assert.Throws<InvalidOperationException>(() => ModelFeatures.Value(Row(1, 1, false), "BotMu"));
    }

    [Fact]
    public void Model_RecordsHowManyTestValuesLieOutsideTheTrainRange()
    {
        RepositoryModel model = LogisticRegressionModel.Train(Split());

        Assert.True(model.TestValuesOutsideTrainRange >= 0);
        Assert.True(model.TestRowsOutsideTrainRange <= model.TestProbabilities.Count);
    }

    /// <summary>
    /// Kucuk ama ogrenilebilir bir bolme: buyuk commit'ler pozitif, kucukler negatif,
    /// aralarinda gurultu var.
    /// </summary>
    private static RepositorySplit Split()
    {
        List<SnapshotRow> train = [];
        List<SnapshotRow> test = [];

        for (int index = 0; index < 120; index++)
        {
            bool positive = index % 3 == 0;
            train.Add(Row(index + 1, index % 7, positive, "t" + index));
        }

        for (int index = 0; index < 40; index++)
        {
            bool positive = index % 3 == 0;
            test.Add(Row(index + 5, index % 5, positive, "s" + index));
        }

        return new RepositorySplit("a/b", train, test);
    }

    private static SnapshotRow Row(int size, int history, bool positive, string sha = "x") =>
        new("klasor", "a/b", sha, DateTimeOffset.UnixEpoch,
            size * (positive ? 40 : 1), size, size % 5 + 1, size % 3, size % 4 * 0.5,
            size % 6 + 1, size % 2 + 1, size * 3, size, history * 2 + 1, history,
            history + 1, size * 2, history * 3, positive, positive, positive ? "szz" : null, false);
}
