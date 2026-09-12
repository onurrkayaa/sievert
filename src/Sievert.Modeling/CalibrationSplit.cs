namespace Sievert.Modeling;

/// <summary>
/// Kalibrasyon deneyinin uc bolumu. Ana train/test manifesti DEGISMIYOR: bu bolme
/// mevcut train bolumunun kendi icinde zaman sirali ikiye ayrilmasi.
///
/// model-fit: train'in ilk %80'i, modeli egitiyor.
/// calibration: train'in kalani, kalibrasyonu ogreniyor.
/// test: mevcut test, aynen; hicbir fit islemine girmiyor.
/// </summary>
public sealed record CalibrationSplit(
    string Identity,
    IReadOnlyList<SnapshotRow> ModelFit,
    IReadOnlyList<SnapshotRow> Calibration,
    IReadOnlyList<SnapshotRow> Test)
{
    public const double ModelFitShare = 0.80;

    public const string ModelFitName = "model-fit";

    public const string CalibrationName = "calibration";

    public const string TestName = "test";

    public int ModelFitPositives => Count(ModelFit);

    public int CalibrationPositives => Count(Calibration);

    public int TestPositives => Count(Test);

    /// <summary>
    /// Kalibrasyon bolumunde hem pozitif hem negatif var mi. Biri yoksa kalibrasyon
    /// ogrenilemez ve o repo icin calistirilmaz.
    /// </summary>
    public bool CalibrationHasBothClasses =>
        CalibrationPositives > 0 && CalibrationPositives < Calibration.Count;

    /// <summary>
    /// Siralama ana manifestteki ile ayni kural: tarih artan, esitlikte sha ordinal.
    /// Manifest zaten bu sirada geliyor, yine de burada tekrar uygulaniyor ki bolme
    /// girdinin sirasina bagli kalmasin.
    /// </summary>
    public static CalibrationSplit From(RepositorySplit repository)
    {
        List<SnapshotRow> ordered =
        [
            .. repository.Train
                .OrderBy(row => row.AuthorDateUtc.UtcDateTime)
                .ThenBy(row => row.Sha, StringComparer.Ordinal)
        ];

        int fit = (int)Math.Floor(ordered.Count * ModelFitShare);

        return new CalibrationSplit(
            repository.Identity,
            [.. ordered.Take(fit)],
            [.. ordered.Skip(fit)],
            repository.Test);
    }

    private static int Count(IReadOnlyList<SnapshotRow> rows)
    {
        int positives = 0;

        foreach (SnapshotRow row in rows)
        {
            if (row.IsBugIntroducing)
            {
                positives++;
            }
        }

        return positives;
    }
}
