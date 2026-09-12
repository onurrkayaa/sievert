namespace Sievert.Modeling;

/// <summary>
/// Bir olcunun 1000 tekrardaki dagilimi. Yuzdelikler en yakin sira yontemiyle;
/// <c>MetricDistribution</c> ile ayni kural.
/// </summary>
/// <param name="Values">Deger uretilebilen tekrarlar, sirali degil.</param>
/// <param name="NotAvailable">Degerin N/A ciktigi tekrar sayisi.</param>
public sealed record Distribution(IReadOnlyList<double> Values, int NotAvailable)
{
    public double Mean { get; } = Values.Count == 0 ? double.NaN : Values.Average();

    public double Low { get; } = Quantile(Values, 0.025);

    public double Median { get; } = Quantile(Values, 0.50);

    public double High { get; } = Quantile(Values, 0.975);

    public double Min { get; } = Values.Count == 0 ? double.NaN : Values.Min();

    public double Max { get; } = Values.Count == 0 ? double.NaN : Values.Max();

    public static Distribution Of(IEnumerable<double?> values)
    {
        List<double> present = [];
        int missing = 0;

        foreach (double? value in values)
        {
            if (value is double number)
            {
                present.Add(number);
            }
            else
            {
                missing++;
            }
        }

        return new Distribution(present, missing);
    }

    public static Distribution Of(IEnumerable<int> values) =>
        Of(values.Select(value => (double?)value));

    private static double Quantile(IReadOnlyList<double> values, double fraction)
    {
        if (values.Count == 0)
        {
            return double.NaN;
        }

        List<double> sorted = [.. values];
        sorted.Sort();

        int index = (int)Math.Ceiling(fraction * sorted.Count) - 1;

        return sorted[Math.Clamp(index, 0, sorted.Count - 1)];
    }
}

/// <summary>Bir kumenin 1000 tekrardaki ozeti.</summary>
public sealed record RandomSummary(
    string Name,
    double Probability,
    Distribution Precision,
    Distribution Recall,
    Distribution F1,
    Distribution PrAuc,
    Distribution PredictedPositives);

/// <summary>Butun tekrarlarin sonucu.</summary>
public sealed record RandomBaselineResult(
    int Seed,
    int Repeats,
    IReadOnlyList<RandomSummary> Repositories,
    RandomSummary Micro);

/// <summary>
/// Egitim pozitif oraniyla rastgele tahmin eden taban. Her repo KENDI egitim oranini
/// kullaniyor; uc oran birlestirilip tek bir olasilik uretilmiyor, cunku taban oranlari
/// birbirinden cok uzak (%13,0 / %14,9 / %23,4) ve ortak bir oran uc repoda da yanlis
/// olurdu.
///
/// Tek kosu rastgeleligin kendisinden etkilendigi icin 1000 kez tekrarlaniyor ve
/// dagilim raporlaniyor; gerekcesi ADR 0017'de.
/// </summary>
public static class RandomBaseline
{
    /// <summary>Ana tohum. Tekrar tohumu = ana tohum + tekrar numarasi.</summary>
    public const int MasterSeed = 20260912;

    public const int DefaultRepeats = 1000;

    /// <summary>
    /// Depolar verilen sirada isleniyor ve her tekrar icin TEK bir rastgele akis
    /// kullaniliyor. Repo basina ayri ayri ayni tohumla akis acilsaydi bir reponun
    /// cizisi digerinin onekine esit olur, yani repolar birbiriyle iliskili cikardi.
    /// </summary>
    public static RandomBaselineResult Run(
        IReadOnlyList<RepositorySplit> repositories,
        int seed = MasterSeed,
        int repeats = DefaultRepeats)
    {
        List<double?>[] precision = Lists(repositories.Count + 1);
        List<double?>[] recall = Lists(repositories.Count + 1);
        List<double?>[] f1 = Lists(repositories.Count + 1);
        List<double?>[] area = Lists(repositories.Count + 1);
        List<double?>[] predicted = Lists(repositories.Count + 1);

        for (int repeat = 0; repeat < repeats; repeat++)
        {
            Random random = new(seed + repeat);
            List<Scored> micro = [];

            for (int index = 0; index < repositories.Count; index++)
            {
                RepositorySplit repository = repositories[index];
                double probability = repository.TrainPositiveRate;
                List<Scored> scored = new(repository.Test.Count);

                foreach (SnapshotRow row in repository.Test)
                {
                    double score = random.NextDouble();
                    scored.Add(new Scored(score < probability, score, row.IsBugIntroducing));
                }

                Record(Evaluation.Of(scored), index, precision, recall, f1, area, predicted);
                micro.AddRange(scored);
            }

            Record(Evaluation.Of(micro), repositories.Count, precision, recall, f1, area, predicted);
        }

        List<RandomSummary> summaries = [];

        for (int index = 0; index < repositories.Count; index++)
        {
            summaries.Add(Summarise(
                repositories[index].Identity,
                repositories[index].TrainPositiveRate,
                index,
                precision,
                recall,
                f1,
                area,
                predicted));
        }

        // Mikro toplamin tek bir olasiligi yok: her repo kendi oranini kullandi.
        RandomSummary combined = Summarise(
            "mikro",
            double.NaN,
            repositories.Count,
            precision,
            recall,
            f1,
            area,
            predicted);

        return new RandomBaselineResult(seed, repeats, summaries, combined);
    }

    private static List<double?>[] Lists(int count)
    {
        List<double?>[] lists = new List<double?>[count];

        for (int index = 0; index < count; index++)
        {
            lists[index] = [];
        }

        return lists;
    }

    private static void Record(
        Outcome outcome,
        int index,
        List<double?>[] precision,
        List<double?>[] recall,
        List<double?>[] f1,
        List<double?>[] area,
        List<double?>[] predicted)
    {
        precision[index].Add(outcome.Counts.Precision);
        recall[index].Add(outcome.Counts.Recall);
        f1[index].Add(outcome.Counts.F1);
        area[index].Add(outcome.PrAuc);
        predicted[index].Add(outcome.Counts.PredictedPositives);
    }

    private static RandomSummary Summarise(
        string name,
        double probability,
        int index,
        List<double?>[] precision,
        List<double?>[] recall,
        List<double?>[] f1,
        List<double?>[] area,
        List<double?>[] predicted) =>
        new(
            name,
            probability,
            Distribution.Of(precision[index]),
            Distribution.Of(recall[index]),
            Distribution.Of(f1[index]),
            Distribution.Of(area[index]),
            Distribution.Of(predicted[index]));
}
