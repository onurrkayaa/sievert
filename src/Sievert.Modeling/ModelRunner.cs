namespace Sievert.Modeling;

/// <summary>Bir esikle olculen sonuc.</summary>
public sealed record ThresholdedOutcome(double Threshold, Confusion Counts, double? PrAuc);

/// <summary>Bir deponun butun model sonuclari.</summary>
public sealed record RepositoryModelResult(
    string Identity,
    double TrainThreshold,
    int TrainThresholdCandidates,
    ThresholdedOutcome TrainAtTuned,
    ThresholdedOutcome TestAtFixed,
    ThresholdedOutcome TestAtTuned,
    CalibrationResult Calibration,
    Coefficients Coefficients,
    IReadOnlyList<FeatureStatistics> Statistics,
    IReadOnlyList<CorrelatedPair> HighCorrelations,
    int TestValuesOutsideTrainRange,
    int TestRowsOutsideTrainRange,
    IReadOnlyList<ScoredProbability> TestProbabilities);

/// <summary>Uc reponun birlesik sonucu.</summary>
public sealed record ModelReport(
    string SnapshotSha256,
    string ManifestSha256,
    string BaselineSha256,
    string CodeCommit,
    string MetricContractVersion,
    IReadOnlyList<RepositoryModelResult> Repositories,
    Confusion MicroAtFixed,
    Confusion MicroAtTuned,
    double? MicroPrAuc,
    double? MacroF1AtTuned,
    double? MacroPrAuc,
    CalibrationResult MicroCalibration,
    double MacroBrier,
    double MacroEce);

/// <summary>
/// Uc repo icin ayri model egitir ve hepsini ayni olcum yolundan gecirir. Repo-arasi
/// deney degil: her model kendi reposunun egitim bolumunde egitilip kendi test bolumunde
/// olculuyor.
/// </summary>
public static class ModelRunner
{
    public static ModelReport Run(
        IReadOnlyList<RepositorySplit> repositories,
        string snapshotSha256,
        string manifestSha256,
        string baselineSha256,
        string codeCommit)
    {
        List<RepositoryModelResult> results = [];
        List<Scored> microFixed = [];
        List<Scored> microTuned = [];
        List<ScoredProbability> microProbabilities = [];

        foreach (RepositorySplit repository in repositories)
        {
            RepositoryModel model = LogisticRegressionModel.Train(repository);

            IReadOnlyList<ScoredProbability> train = Pair(model.TrainProbabilities, repository.Train);
            IReadOnlyList<ScoredProbability> test = Pair(model.TestProbabilities, repository.Test);

            // Esik YALNIZCA egitim tahminlerinden seciliyor.
            ThresholdChoice choice = ProbabilityThreshold.Choose(train);

            IReadOnlyList<Scored> trainTuned = ProbabilityThreshold.Apply(train, choice.Threshold);
            IReadOnlyList<Scored> testFixed = ProbabilityThreshold.Apply(test, ProbabilityThreshold.Fixed);
            IReadOnlyList<Scored> testTuned = ProbabilityThreshold.Apply(test, choice.Threshold);

            results.Add(new RepositoryModelResult(
                repository.Identity,
                choice.Threshold,
                choice.CandidateCount,
                Measure(choice.Threshold, trainTuned),
                Measure(ProbabilityThreshold.Fixed, testFixed),
                Measure(choice.Threshold, testTuned),
                Calibration.Measure(test),
                model.Coefficients,
                model.Scaler.Statistics,
                SpearmanCorrelation.HighPairs(repository.Train),
                model.TestValuesOutsideTrainRange,
                model.TestRowsOutsideTrainRange,
                test));

            microFixed.AddRange(testFixed);
            microTuned.AddRange(testTuned);
            microProbabilities.AddRange(test);
        }

        Outcome tuned = Evaluation.Of(microTuned);

        return new ModelReport(
            snapshotSha256,
            manifestSha256,
            baselineSha256,
            codeCommit,
            BaselineRunner.MetricContractVersion,
            results,
            Evaluation.Of(microFixed).Counts,
            tuned.Counts,
            tuned.PrAuc,
            Totals.MacroF1(results.Select(result => result.TestAtTuned.Counts.F1)),
            Totals.MacroF1(results.Select(result => result.TestAtTuned.PrAuc)),
            Calibration.Measure(microProbabilities),
            results.Average(result => result.Calibration.Brier),
            results.Average(result => result.Calibration.Ece));
    }

    private static IReadOnlyList<ScoredProbability> Pair(
        IReadOnlyList<double> probabilities,
        IReadOnlyList<SnapshotRow> rows)
    {
        List<ScoredProbability> paired = new(rows.Count);

        for (int index = 0; index < rows.Count; index++)
        {
            paired.Add(new ScoredProbability(probabilities[index], rows[index].IsBugIntroducing));
        }

        return paired;
    }

    /// <summary>PR-AUC ham olasilikla; <see cref="Scored.Score"/> esik sonrasi degismiyor.</summary>
    private static ThresholdedOutcome Measure(double threshold, IReadOnlyList<Scored> scored)
    {
        Outcome outcome = Evaluation.Of(scored);

        return new ThresholdedOutcome(threshold, outcome.Counts, outcome.PrAuc);
    }
}
