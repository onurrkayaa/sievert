namespace Sievert.Modeling;

/// <summary>Bir kalibrasyon yonteminin bir test kumesindeki sonucu.</summary>
public sealed record CalibrationOutcome(
    string Method,
    CalibrationResult Calibration,
    double? PrAuc,
    Confusion CountsAtHalf,
    double MeanPrediction,
    int Positives,
    int Count);

/// <summary>Bir deponun uc yontemli kalibrasyon sonucu.</summary>
public sealed record RepositoryCalibration(
    string Identity,
    int ModelFitRows,
    int ModelFitPositives,
    int CalibrationRows,
    int CalibrationPositives,
    int TestRows,
    int TestPositives,
    double PlattSlope,
    double PlattOffset,
    int PlattIterations,
    int IsotonicBlocks,
    IReadOnlyList<CalibrationOutcome> Outcomes,
    IReadOnlyList<ScoredProbability> RawTest,
    IReadOnlyList<ScoredProbability> PlattTest,
    IReadOnlyList<ScoredProbability> IsotonicTest);

/// <summary>Uc reponun birlesik kalibrasyon sonucu.</summary>
public sealed record CalibrationReport(
    string SnapshotSha256,
    string ManifestSha256,
    string ModelResultsSha256,
    string CodeCommit,
    string ContractVersion,
    IReadOnlyList<RepositoryCalibration> Repositories,
    IReadOnlyList<CalibrationOutcome> Micro,
    IReadOnlyDictionary<string, double> MacroBrier,
    IReadOnlyDictionary<string, double> MacroEce,
    IReadOnlyDictionary<string, double> MacroPrAuc,
    IReadOnlyDictionary<string, double> MacroF1);

/// <summary>
/// Kalibrasyon deneyi. Azaltilmis egitim (model-fit) modeliyle uc sonuc yan yana
/// olculuyor: ham, Platt, isotonic.
///
/// Bu model Adim 3'un tam-train modelinin YERINE GECMIYOR; yalnizca kalibrasyonun
/// karsilastirma noktasi. Adim 3'un dosyalari bu deneyde okunmuyor bile.
/// </summary>
public static class CalibrationRunner
{
    public const string ContractVersion = "1.0";

    public const string Raw = "ham";

    public const string Platt = "platt";

    public const string Isotonic = "isotonic";

    public static CalibrationReport Run(
        IReadOnlyList<RepositorySplit> repositories,
        string snapshotSha256,
        string manifestSha256,
        string modelResultsSha256,
        string codeCommit)
    {
        List<RepositoryCalibration> results = [];
        Dictionary<string, List<ScoredProbability>> micro = new(StringComparer.Ordinal)
        {
            [Raw] = [],
            [Platt] = [],
            [Isotonic] = [],
        };

        foreach (RepositorySplit repository in repositories)
        {
            CalibrationSplit split = CalibrationSplit.From(repository);

            if (!split.CalibrationHasBothClasses)
            {
                throw new InvalidOperationException(
                    $"{repository.Identity}: kalibrasyon bolumunde iki siniftan biri yok "
                    + $"({split.CalibrationPositives} / {split.Calibration.Count}). Kalibrasyon calistirilmadi.");
            }

            // Model YALNIZCA model-fit bolumunde egitiliyor; calibration trainer'a girmiyor.
            RepositorySplit fitOnly = new(repository.Identity, split.ModelFit, split.Calibration);
            RepositoryModel model = LogisticRegressionModel.Train(fitOnly);

            IReadOnlyList<ScoredProbability> calibrationRows = Pair(model.TestProbabilities, split.Calibration);
            IReadOnlyList<double> testProbabilities = LogisticRegressionModel.Load(
                Store(fitOnly),
                LogisticRegressionModel.Rows(split.Test, model.Scaler));

            IReadOnlyList<ScoredProbability> rawTest = Pair(testProbabilities, split.Test);

            PlattCalibration platt = PlattCalibration.Fit(calibrationRows);
            IsotonicCalibration isotonic = IsotonicCalibration.Fit(calibrationRows);

            IReadOnlyList<ScoredProbability> plattTest = Map(rawTest, platt.Apply);
            IReadOnlyList<ScoredProbability> isotonicTest = Map(rawTest, isotonic.Apply);

            micro[Raw].AddRange(rawTest);
            micro[Platt].AddRange(plattTest);
            micro[Isotonic].AddRange(isotonicTest);

            results.Add(new RepositoryCalibration(
                repository.Identity,
                split.ModelFit.Count,
                split.ModelFitPositives,
                split.Calibration.Count,
                split.CalibrationPositives,
                split.Test.Count,
                split.TestPositives,
                platt.Slope,
                platt.Offset,
                platt.Iterations,
                isotonic.Blocks.Count,
                [Measure(Raw, rawTest), Measure(Platt, plattTest), Measure(Isotonic, isotonicTest)],
                rawTest,
                plattTest,
                isotonicTest));
        }

        List<CalibrationOutcome> microOutcomes =
        [
            Measure(Raw, micro[Raw]),
            Measure(Platt, micro[Platt]),
            Measure(Isotonic, micro[Isotonic]),
        ];

        return new CalibrationReport(
            snapshotSha256,
            manifestSha256,
            modelResultsSha256,
            codeCommit,
            ContractVersion,
            results,
            microOutcomes,
            Macro(results, outcome => outcome.Calibration.Brier),
            Macro(results, outcome => outcome.Calibration.Ece),
            Macro(results, outcome => outcome.PrAuc ?? double.NaN),
            Macro(results, outcome => outcome.CountsAtHalf.F1 ?? 0.0));
    }

    /// <summary>Modeli gecici bir dosyaya yazip test satirlarini o modelle puanlamak icin.</summary>
    private static string Store(RepositorySplit fitOnly)
    {
        string path = Path.Combine(
            Path.GetTempPath(),
            "sievert-kalibrasyon-" + fitOnly.Identity.Replace('/', '-') + ".zip");

        LogisticRegressionModel.Save(fitOnly, path);

        return path;
    }

    public static CalibrationOutcome Measure(string method, IReadOnlyList<ScoredProbability> rows)
    {
        List<Scored> scored = new(rows.Count);
        double total = 0.0;
        int positives = 0;

        foreach (ScoredProbability row in rows)
        {
            scored.Add(new Scored(row.Probability >= ProbabilityThreshold.Fixed, row.Probability, row.Actual));
            total += row.Probability;

            if (row.Actual)
            {
                positives++;
            }
        }

        Outcome outcome = Evaluation.Of(scored);

        return new CalibrationOutcome(
            method,
            Calibration.Measure(rows),
            outcome.PrAuc,
            outcome.Counts,
            rows.Count == 0 ? double.NaN : total / rows.Count,
            positives,
            rows.Count);
    }

    private static IReadOnlyList<ScoredProbability> Map(
        IReadOnlyList<ScoredProbability> rows,
        Func<double, double> transform)
    {
        List<ScoredProbability> mapped = new(rows.Count);

        foreach (ScoredProbability row in rows)
        {
            mapped.Add(new ScoredProbability(transform(row.Probability), row.Actual));
        }

        return mapped;
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

    private static IReadOnlyDictionary<string, double> Macro(
        IReadOnlyList<RepositoryCalibration> results,
        Func<CalibrationOutcome, double> pick)
    {
        Dictionary<string, double> macro = new(StringComparer.Ordinal);

        foreach (string method in (string[])[Raw, Platt, Isotonic])
        {
            double total = 0.0;

            foreach (RepositoryCalibration result in results)
            {
                // sievert:disable SV004 Outcomes bellekte uc elemanli bir liste, veritabani sorgusu degil
                total += pick(result.Outcomes.Single(outcome => outcome.Method == method));
            }

            macro[method] = total / results.Count;
        }

        return macro;
    }
}
