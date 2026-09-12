namespace Sievert.Modeling;

/// <summary>Bir alt kumede olculen sonuc.</summary>
public sealed record SubsetOutcome(
    string Name,
    int Rows,
    int Positives,
    double Threshold,
    Confusion Counts,
    double? PrAuc,
    double Brier,
    double Ece,
    double MeanPrediction);

/// <summary>Bir oznitelik icin testte aralik disi sayimlar.</summary>
public sealed record RangeCount(string Feature, int BelowMinimum, int AboveMaximum);

/// <summary>
/// Duyarlilik deneyleri. Hicbiri ana modeli, ana bolmeyi ya da dondurulmus dosyalari
/// degistirmiyor; her biri ayri bir "ya soyle olsaydi" hesabi.
/// </summary>
public static class Sensitivity
{
    /// <summary>Onceden ilan edilen olgunluk esigi.</summary>
    public const int MaturityDays = 90;

    /// <summary>Ana modelin tahminleriyle, verilen satir alt kumesinde olcum.</summary>
    public static SubsetOutcome Evaluate(
        string name,
        IReadOnlyList<(SnapshotRow Row, double Probability)> rows,
        double threshold)
    {
        List<Scored> scored = new(rows.Count);
        List<ScoredProbability> probabilities = new(rows.Count);
        int positives = 0;
        double total = 0.0;

        foreach ((SnapshotRow row, double probability) in rows)
        {
            scored.Add(new Scored(probability >= threshold, probability, row.IsBugIntroducing));
            probabilities.Add(new ScoredProbability(probability, row.IsBugIntroducing));
            total += probability;

            if (row.IsBugIntroducing)
            {
                positives++;
            }
        }

        Outcome outcome = Evaluation.Of(scored);
        CalibrationResult calibration = Calibration.Measure(probabilities);

        return new SubsetOutcome(
            name,
            rows.Count,
            positives,
            threshold,
            outcome.Counts,
            outcome.PrAuc,
            calibration.Brier,
            calibration.Ece,
            rows.Count == 0 ? double.NaN : total / rows.Count);
    }

    /// <summary>Verilen oznitelik kumesiyle yeniden egitip hedef alt kumede olcer.</summary>
    public static SubsetOutcome Retrain(
        string name,
        string identity,
        IReadOnlyList<SnapshotRow> train,
        IReadOnlyList<SnapshotRow> test,
        IReadOnlyList<string> features)
    {
        RepositoryModel model = LogisticRegressionModel.Train(new RepositorySplit(identity, train, test), features);

        List<ScoredProbability> trainRows = new(train.Count);

        for (int index = 0; index < train.Count; index++)
        {
            trainRows.Add(new ScoredProbability(model.TrainProbabilities[index], train[index].IsBugIntroducing));
        }

        // Esik YALNIZCA bu deneyin kendi egitim alt kumesinde seciliyor.
        double threshold = ProbabilityThreshold.Choose(trainRows).Threshold;

        List<(SnapshotRow, double)> testRows = new(test.Count);

        for (int index = 0; index < test.Count; index++)
        {
            testRows.Add((test[index], model.TestProbabilities[index]));
        }

        return Evaluate(name, testRows, threshold);
    }

    /// <summary>
    /// Sag sansur olcusu: reponun en son commit tarihi eksi bu commit'in tarihi.
    /// Adim 1'deki tanimin aynisi.
    /// </summary>
    public static int Maturity(DateTimeOffset last, DateTimeOffset commit) => (int)(last - commit).TotalDays;

    /// <summary>Testte her surekli ozniteligin egitim araliginin altinda ve ustunde kalan sayisi.</summary>
    public static IReadOnlyList<RangeCount> Ranges(FeatureScaler scaler, IReadOnlyList<SnapshotRow> test)
    {
        List<RangeCount> counts = [];

        foreach (FeatureStatistics statistics in scaler.Statistics.OrderBy(entry => entry.Name, StringComparer.Ordinal))
        {
            int below = 0;
            int above = 0;

            foreach (SnapshotRow row in test)
            {
                double value = ModelFeatures.Value(row, statistics.Name);

                if (FeatureTransform.LogFeatures.Contains(statistics.Name, StringComparer.Ordinal))
                {
                    value = FeatureTransform.Log1P(value);
                }

                if (value < statistics.Min)
                {
                    below++;
                }
                else if (value > statistics.Max)
                {
                    above++;
                }
            }

            counts.Add(new RangeCount(statistics.Name, below, above));
        }

        return counts;
    }

    /// <summary>
    /// Sentetik tek yonlu etiket gurultusu: aday gizli pozitifler yalnizca
    /// <c>IsBugIntroducing = false</c> VE <c>CsFilesChanged &gt; 0</c> olan satirlar.
    ///
    /// Orijinal anlik goruntu degismiyor; bu islem yeni bir liste uretiyor.
    /// </summary>
    public static IReadOnlyList<SnapshotRow> Flip(IReadOnlyList<SnapshotRow> rows, double rate, Random random)
    {
        List<SnapshotRow> flipped = new(rows.Count);

        foreach (SnapshotRow row in rows)
        {
            bool candidate = !row.IsBugIntroducing && row.CsFilesChanged > 0;

            flipped.Add(candidate && random.NextDouble() < rate
                ? row with { IsBugIntroducing = true, LabelSource = "sentetik" }
                : row);
        }

        return flipped;
    }

    public static int Candidates(IReadOnlyList<SnapshotRow> rows)
    {
        int candidates = 0;

        foreach (SnapshotRow row in rows)
        {
            if (!row.IsBugIntroducing && row.CsFilesChanged > 0)
            {
                candidates++;
            }
        }

        return candidates;
    }
}
