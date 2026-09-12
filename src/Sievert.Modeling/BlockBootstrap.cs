namespace Sievert.Modeling;

/// <summary>Bir test satirinin modele ve tabana gore tahmini. Ikisi ayni satirdan geliyor.</summary>
/// <param name="ModelPredicted">Modelin train'de secilen esikle verdigi ikili tahmin.</param>
/// <param name="ModelScore">Modelin ham olasiligi.</param>
/// <param name="BaselinePredicted">LinesAdded esiginin verdigi ikili tahmin.</param>
/// <param name="BaselineScore">Ham <c>LinesAdded</c> degeri.</param>
public readonly record struct PairedRow(
    bool ModelPredicted,
    double ModelScore,
    bool BaselinePredicted,
    double BaselineScore,
    bool Actual);

/// <summary>Bir kumenin bootstrap ozeti.</summary>
public sealed record BootstrapSummary(
    string Name,
    int BlockLength,
    Distribution DeltaF1,
    Distribution DeltaPrAuc,
    int ValidRepeats,
    int NotAvailableF1,
    int NotAvailablePrAuc);

/// <summary>Butun bootstrap sonucu.</summary>
public sealed record BootstrapReport(
    int Seed,
    int Repeats,
    IReadOnlyList<BootstrapSummary> Repositories,
    BootstrapSummary Micro,
    BootstrapSummary Macro);

/// <summary>
/// Eslesmis (paired) dairesel hareketli blok bootstrap.
///
/// Model ve taban AYNI yeniden orneklenmis satirlari kullaniyor; fark her tekrarda ayni
/// satir kumesinden hesaplaniyor. Boylece iki yontemin farki, orneklemin kendisinden
/// gelen degiskenlikten arindirilmis oluyor.
///
/// Bloklar zaman sirasini kismen koruyor: test satirlari tarih sirasinda ve ardisik
/// satirlar birlikte cekiliyor. Tek tek satir cekmek, komsu commit'lerin birbirine
/// benzedigi bir veride belirsizligi oldugundan dar gosterirdi.
///
/// ONEMLI: bu bootstrap model egitim belirsizligini KAPSAMIYOR. Tahminler sabit; olculen
/// sey yalnizca zamansal test ornekleme belirsizligi.
/// </summary>
public static class BlockBootstrap
{
    public const int Repeats = 2000;

    public const int MasterSeed = 20260912;

    /// <summary>Blok uzunlugu ceil(sqrt(N)).</summary>
    public static int BlockLength(int count) => (int)Math.Ceiling(Math.Sqrt(count));

    /// <summary>
    /// Dairesel hareketli blok indeksleri: rastgele baslangiclardan bloklar alinir, repo
    /// sonunda basa sarilir, N satira ulasinca fazlasi kesilir.
    /// </summary>
    public static int[] Indices(Random random, int count, int blockLength)
    {
        int[] indices = new int[count];
        int written = 0;

        while (written < count)
        {
            int start = random.Next(count);

            for (int offset = 0; offset < blockLength && written < count; offset++)
            {
                indices[written] = (start + offset) % count;
                written++;
            }
        }

        return indices;
    }

    public static BootstrapReport Run(
        IReadOnlyList<(string Identity, IReadOnlyList<PairedRow> Rows)> repositories,
        int seed = MasterSeed,
        int repeats = Repeats)
    {
        int count = repositories.Count;
        List<double?>[] deltaF1 = Lists(count + 2);
        List<double?>[] deltaArea = Lists(count + 2);

        for (int repeat = 0; repeat < repeats; repeat++)
        {
            Random random = new(seed + repeat);
            List<PairedRow> micro = [];
            List<double?> repoF1 = [];
            List<double?> repoArea = [];

            for (int index = 0; index < count; index++)
            {
                IReadOnlyList<PairedRow> rows = repositories[index].Rows;
                int[] picked = Indices(random, rows.Count, BlockLength(rows.Count));

                List<PairedRow> sample = new(picked.Length);

                foreach (int position in picked)
                {
                    sample.Add(rows[position]);
                }

                (double? f1, double? area) = Delta(sample);
                deltaF1[index].Add(f1);
                deltaArea[index].Add(area);
                repoF1.Add(f1);
                repoArea.Add(area);
                micro.AddRange(sample);
            }

            (double? microF1, double? microArea) = Delta(micro);
            deltaF1[count].Add(microF1);
            deltaArea[count].Add(microArea);

            deltaF1[count + 1].Add(Average(repoF1));
            deltaArea[count + 1].Add(Average(repoArea));
        }

        List<BootstrapSummary> summaries = [];

        for (int index = 0; index < count; index++)
        {
            summaries.Add(Summarise(
                repositories[index].Identity,
                BlockLength(repositories[index].Rows.Count),
                deltaF1[index],
                deltaArea[index],
                repeats));
        }

        return new BootstrapReport(
            seed,
            repeats,
            summaries,
            Summarise("mikro", 0, deltaF1[count], deltaArea[count], repeats),
            Summarise("makro", 0, deltaF1[count + 1], deltaArea[count + 1], repeats));
    }

    /// <summary>
    /// Bir orneklemde modelin ve tabanin farki. Orneklemde hic pozitif yoksa recall
    /// tanimsiz, F1 ve PR-AUC N/A doner ve o tekrar sayilir.
    /// </summary>
    private static (double? DeltaF1, double? DeltaPrAuc) Delta(IReadOnlyList<PairedRow> rows)
    {
        List<Scored> model = new(rows.Count);
        List<Scored> baseline = new(rows.Count);

        foreach (PairedRow row in rows)
        {
            model.Add(new Scored(row.ModelPredicted, row.ModelScore, row.Actual));
            baseline.Add(new Scored(row.BaselinePredicted, row.BaselineScore, row.Actual));
        }

        Outcome modelOutcome = Evaluation.Of(model);
        Outcome baselineOutcome = Evaluation.Of(baseline);

        double? f1 = modelOutcome.Counts.F1 is double one && baselineOutcome.Counts.F1 is double other
            ? one - other
            : null;

        double? area = modelOutcome.PrAuc is double area1 && baselineOutcome.PrAuc is double area2
            ? area1 - area2
            : null;

        return (f1, area);
    }

    private static double? Average(IReadOnlyList<double?> values)
    {
        double total = 0.0;

        foreach (double? value in values)
        {
            if (value is not double number)
            {
                return null;
            }

            total += number;
        }

        return values.Count == 0 ? null : total / values.Count;
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

    private static BootstrapSummary Summarise(
        string name,
        int blockLength,
        List<double?> f1,
        List<double?> area,
        int repeats)
    {
        Distribution one = Distribution.Of(f1);
        Distribution other = Distribution.Of(area);

        return new BootstrapSummary(
            name,
            blockLength,
            one,
            other,
            repeats,
            one.NotAvailable,
            other.NotAvailable);
    }
}
