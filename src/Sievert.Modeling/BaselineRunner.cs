namespace Sievert.Modeling;

/// <summary>Bir depodaki tek bir tabanin sonucu.</summary>
public sealed record RepositoryOutcome(string Identity, Confusion Counts, double? PrAuc);

/// <summary>Her seye negatif tabaninin butun sonucu.</summary>
public sealed record NegativeResult(
    IReadOnlyList<RepositoryOutcome> Repositories,
    Confusion Micro,
    double? MicroPrAuc,
    double? MacroF1);

/// <summary>Bir depoda secilen esik ve iki bolumdeki sonuclari.</summary>
public sealed record ThresholdRepositoryOutcome(
    string Identity,
    double Threshold,
    int CandidateCount,
    ThresholdOutcome Train,
    ThresholdOutcome Test);

/// <summary>LinesAdded esigi tabaninin butun sonucu.</summary>
public sealed record ThresholdResult(
    IReadOnlyList<ThresholdRepositoryOutcome> Repositories,
    Confusion Micro,
    double? MicroRawPrAuc,
    double? MicroBinaryPrAuc,
    double? MacroF1);

/// <summary>Bir deponun bolme sayilari; raporun basindaki dogrulama tablosu icin.</summary>
public sealed record SplitCounts(
    string Identity,
    int TrainRows,
    int TrainPositives,
    int TestRows,
    int TestPositives);

/// <summary>Uc tabanin bir arada sonucu. Sonuc dosyasinin tamami bundan yaziliyor.</summary>
public sealed record BaselineReport(
    string MetricContractVersion,
    string SnapshotSha256,
    string ManifestSha256,
    string CodeCommit,
    IReadOnlyList<SplitCounts> Split,
    NegativeResult Negative,
    RandomBaselineResult Random,
    ThresholdResult LinesAdded);

/// <summary>
/// Uc taban cizgisini ayni bolme uzerinde calistirir. Hepsi ayni
/// <see cref="Evaluation"/> yolundan olculuyor; farkli formullerle olculen sayilar
/// karsilastirilamaz.
/// </summary>
public static class BaselineRunner
{
    public const string MetricContractVersion = "1.0";

    public static BaselineReport Run(
        IReadOnlyList<RepositorySplit> repositories,
        string snapshotSha256,
        string manifestSha256,
        string codeCommit,
        int repeats = RandomBaseline.DefaultRepeats)
    {
        List<SplitCounts> split = [];
        List<RepositoryOutcome> negative = [];
        List<Scored> negativeMicro = [];
        List<ThresholdRepositoryOutcome> threshold = [];
        List<Scored> thresholdRawMicro = [];
        List<Scored> thresholdBinaryMicro = [];

        foreach (RepositorySplit repository in repositories)
        {
            split.Add(new SplitCounts(
                repository.Identity,
                repository.Train.Count,
                repository.TrainPositives,
                repository.Test.Count,
                repository.TestPositives));

            IReadOnlyList<Scored> scored = NegativeBaseline.Apply(repository.Test);
            Outcome outcome = Evaluation.Of(scored);
            negative.Add(new RepositoryOutcome(repository.Identity, outcome.Counts, outcome.PrAuc));
            negativeMicro.AddRange(scored);

            // Esik YALNIZCA egitim bolumunde seciliyor, sonra bir daha degistirilmiyor.
            ThresholdChoice choice = LinesAddedBaseline.Choose(repository.Train);

            threshold.Add(new ThresholdRepositoryOutcome(
                repository.Identity,
                choice.Threshold,
                choice.CandidateCount,
                LinesAddedBaseline.Apply(repository.Train, choice.Threshold),
                LinesAddedBaseline.Apply(repository.Test, choice.Threshold)));

            foreach (SnapshotRow row in repository.Test)
            {
                double value = ModelFeatures.Value(row, LinesAddedBaseline.Feature);
                bool predicted = value >= choice.Threshold;

                thresholdRawMicro.Add(new Scored(predicted, value, row.IsBugIntroducing));
                thresholdBinaryMicro.Add(new Scored(predicted, predicted ? 1.0 : 0.0, row.IsBugIntroducing));
            }
        }

        Outcome negativeTotal = Evaluation.Of(negativeMicro);
        Outcome thresholdRawTotal = Evaluation.Of(thresholdRawMicro);

        return new BaselineReport(
            MetricContractVersion,
            snapshotSha256,
            manifestSha256,
            codeCommit,
            split,
            new NegativeResult(
                negative,
                negativeTotal.Counts,
                negativeTotal.PrAuc,
                Totals.MacroF1(negative.Select(entry => entry.Counts.F1))),
            RandomBaseline.Run(repositories, repeats: repeats),
            new ThresholdResult(
                threshold,
                thresholdRawTotal.Counts,
                thresholdRawTotal.PrAuc,
                Evaluation.Of(thresholdBinaryMicro).PrAuc,
                Totals.MacroF1(threshold.Select(entry => entry.Test.Counts.F1))));
    }
}
