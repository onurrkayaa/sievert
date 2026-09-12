namespace Sievert.Modeling;

/// <summary>Bir genelleme deneyinin sonucu.</summary>
public sealed record TransferResult(
    string Experiment,
    IReadOnlyList<string> Sources,
    string Target,
    int SourceRows,
    int SourcePositives,
    int TargetRows,
    int TargetPositives,
    double SourceThreshold,
    Confusion AtSourceThreshold,
    Confusion AtHalf,
    double? PrAuc,
    double Brier,
    double Ece,
    double MeanPrediction,
    Coefficients Coefficients,
    IReadOnlyList<double> Probabilities);

/// <summary>
/// Repo-arasi genelleme. Donusum, model ve esik YALNIZCA kaynak reponun train
/// bolumunden; hedef test hicbir fit islemine girmiyor.
///
/// Repo kimligi oznitelik olarak girmiyor (ADR 0016). Kaynak repolar dogal satir
/// sayilariyla birlestiriliyor; agirlik esitleme yok, yani buyuk repo daha agir basiyor
/// ve bu raporda yaziyor.
/// </summary>
public static class Generalization
{
    public static TransferResult Transfer(
        string experiment,
        IReadOnlyList<RepositorySplit> sources,
        RepositorySplit target)
    {
        List<SnapshotRow> train = [];

        foreach (RepositorySplit source in sources)
        {
            train.AddRange(source.Train);
        }

        string identity = string.Join("+", sources.Select(source => source.Identity));
        RepositorySplit combined = new(identity, train, target.Test);
        RepositoryModel model = LogisticRegressionModel.Train(combined);

        List<ScoredProbability> sourceRows = new(train.Count);

        for (int index = 0; index < train.Count; index++)
        {
            sourceRows.Add(new ScoredProbability(model.TrainProbabilities[index], train[index].IsBugIntroducing));
        }

        // Esik YALNIZCA kaynak train'de seciliyor; hedef test etiketleri girmiyor.
        double threshold = ProbabilityThreshold.Choose(sourceRows).Threshold;

        List<Scored> atThreshold = new(target.Test.Count);
        List<Scored> atHalf = new(target.Test.Count);
        List<ScoredProbability> probabilities = new(target.Test.Count);
        double total = 0.0;

        for (int index = 0; index < target.Test.Count; index++)
        {
            double probability = model.TestProbabilities[index];
            bool actual = target.Test[index].IsBugIntroducing;

            atThreshold.Add(new Scored(probability >= threshold, probability, actual));
            atHalf.Add(new Scored(probability >= ProbabilityThreshold.Fixed, probability, actual));
            probabilities.Add(new ScoredProbability(probability, actual));
            total += probability;
        }

        Outcome outcome = Evaluation.Of(atThreshold);
        CalibrationResult calibration = Calibration.Measure(probabilities);

        int sourcePositives = 0;

        foreach (SnapshotRow row in train)
        {
            if (row.IsBugIntroducing)
            {
                sourcePositives++;
            }
        }

        return new TransferResult(
            experiment,
            [.. sources.Select(source => source.Identity)],
            target.Identity,
            train.Count,
            sourcePositives,
            target.Test.Count,
            target.TestPositives,
            threshold,
            outcome.Counts,
            Evaluation.Of(atHalf).Counts,
            outcome.PrAuc,
            calibration.Brier,
            calibration.Ece,
            total / target.Test.Count,
            model.Coefficients,
            [.. model.TestProbabilities]);
    }

    /// <summary>
    /// Korunan performans orani. Payda sifirsa N/A; "korunan" bir ad, nedensellik
    /// iddiasi degil.
    /// </summary>
    public static double? Retained(double? crossRepo, double? sameRepo) =>
        crossRepo is double one && sameRepo is double other && other != 0
            ? one / other
            : null;

    /// <summary>Katsayilari mutlak buyukluge gore siralar.</summary>
    public static IReadOnlyList<(string Feature, double Weight)> Ranked(Coefficients coefficients)
    {
        List<(string Feature, double Weight)> ranked = [];

        for (int index = 0; index < coefficients.Weights.Count; index++)
        {
            ranked.Add((ModelFeatures.Candidates[index], coefficients.Weights[index]));
        }

        ranked.Sort((left, right) => Math.Abs(right.Weight).CompareTo(Math.Abs(left.Weight)));

        return ranked;
    }
}
