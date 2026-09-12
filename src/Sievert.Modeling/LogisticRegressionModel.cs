using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Trainers;

namespace Sievert.Modeling;

/// <summary>ML.NET'e verilen satir. Yalnizca 15 oznitelik ve etiket var.</summary>
public sealed class ModelInput
{
    /// <summary>Oznitelik vektoru. Boyutu calisma zamaninda sema ile veriliyor.</summary>
    [VectorType(15)]
    public float[] Features { get; set; } = new float[15];

    /// <summary><c>IsBugIntroducing</c>. Yalnizca etiket; oznitelik degil.</summary>
    public bool Label { get; set; }
}

/// <summary>ML.NET'ten donen tahmin.</summary>
public sealed class ModelOutput
{
    public float Probability { get; set; }

    public float Score { get; set; }

    public bool PredictedLabel { get; set; }
}

/// <summary>Egitilmis modelin katsayilari. Olcek standartlastirilmis olcek.</summary>
public sealed record Coefficients(double Intercept, IReadOnlyList<double> Weights);

/// <summary>Bir deponun modeli ve iki bolumdeki ham olasiliklari.</summary>
public sealed record RepositoryModel(
    string Identity,
    FeatureScaler Scaler,
    Coefficients Coefficients,
    IReadOnlyList<double> TrainProbabilities,
    IReadOnlyList<double> TestProbabilities,
    int TestValuesOutsideTrainRange,
    int TestRowsOutsideTrainRange);

/// <summary>
/// Lojistik regresyon. Tek trainer var ve secildikten sonra degistirilmedi; alternatif
/// trainer'lar denenip en iyisi secilmedi (ADR 0018).
///
/// Trainer <c>LbfgsLogisticRegression</c>: dogrudan lojistik regresyon (maksimum olabilirlik),
/// toplu (batch) bir eniyileyici oldugu icin ayni veriden ayni sonucu veriyor ve katsayilari
/// dogrudan okunabiliyor.
///
/// Olasilik, skorun lojistik donusumu: ML.NET modeli <c>PlattCalibrator</c> ile sarmaliyor
/// ama parametreleri sabit (slope -1, offset 0), yani sonuc tam olarak sigmoid(skor).
/// Veriden ogrenilen bir Platt olcekleme DEGIL; bu adimda sonradan kalibrasyon yok.
/// </summary>
public static class LogisticRegressionModel
{
    public const int Seed = 20260912;

    /// <summary>
    /// L1 = 0. Sebep sonuca bakmak degil, bu adimin kendi kurali: L1 duzenlilestirme
    /// katsayilari sifirlayarak oznitelik SECIMI yapar, oysa bu adimda oznitelik eleme
    /// yasak ve 15 katsayinin hepsi raporlanacak. ML.NET varsayilani 1,0 ve o degerde
    /// butun agirliklar sifira dusuyor.
    /// </summary>
    public const float L1Regularization = 0f;

    /// <summary>L2 = 1,0. ML.NET varsayilani; degistirilmedi.</summary>
    public const float L2Regularization = 1f;

    /// <summary>Tek is parcacigi: paralel gradyan toplamasinin sirasi kosudan kosuya degisebilir.</summary>
    public const int NumberOfThreads = 1;

    public static RepositoryModel Train(RepositorySplit split) =>
        Train(split, ModelFeatures.Candidates);

    public static RepositoryModel Train(RepositorySplit split, IReadOnlyList<string> features)
    {
        FeatureScaler scaler = FeatureScaler.Fit(split.Identity, split.Train, features);
        MLContext context = new(seed: Seed);

        ITransformer transformer = Fit(context, Rows(split.Train, scaler));

        return new RepositoryModel(
            split.Identity,
            scaler,
            Read(transformer),
            Probabilities(context, transformer, Rows(split.Train, scaler)),
            Probabilities(context, transformer, Rows(split.Test, scaler)),
            scaler.OutsideTrainRange(split.Test),
            scaler.OutsideTrainRangeRows(split.Test));
    }

    /// <summary>Egitip diske yazar; yuklenen modelin ayni tahminleri verdigi sinaniyor.</summary>
    public static void Save(RepositorySplit split, string path) =>
        Save(split, path, ModelFeatures.Candidates);

    public static void Save(RepositorySplit split, string path, IReadOnlyList<string> features)
    {
        FeatureScaler scaler = FeatureScaler.Fit(split.Identity, split.Train, features);
        MLContext context = new(seed: Seed);
        IDataView data = Load(context, Rows(split.Train, scaler));
        ITransformer transformer = Fit(context, Rows(split.Train, scaler));

        context.Model.Save(transformer, data.Schema, path);
    }

    /// <summary>Diskteki modeli yukleyip verilen satirlarin olasiliklarini uretir.</summary>
    public static IReadOnlyList<double> Load(string path, IReadOnlyList<ModelInput> rows)
    {
        MLContext context = new(seed: Seed);
        ITransformer transformer = context.Model.Load(path, out _);

        return Probabilities(context, transformer, rows);
    }

    public static IReadOnlyList<ModelInput> Rows(IReadOnlyList<SnapshotRow> rows, FeatureScaler scaler)
    {
        List<ModelInput> inputs = new(rows.Count);

        foreach (SnapshotRow row in rows)
        {
            inputs.Add(new ModelInput { Features = scaler.Apply(row), Label = row.IsBugIntroducing });
        }

        return inputs;
    }

    /// <summary>
    /// Oznitelik sayisi calisma zamaninda degisebildigi icin sema elle kuruluyor;
    /// <c>VectorType</c> nitelikteki sabit boyut ablasyon deneyinde yetmiyor.
    /// </summary>
    private static IDataView Load(MLContext context, IReadOnlyList<ModelInput> rows)
    {
        SchemaDefinition schema = SchemaDefinition.Create(typeof(ModelInput));
        schema[nameof(ModelInput.Features)].ColumnType =
            new VectorDataViewType(NumberDataViewType.Single, rows[0].Features.Length);

        return context.Data.LoadFromEnumerable(rows, schema);
    }

    private static ITransformer Fit(MLContext context, IReadOnlyList<ModelInput> rows)
    {
        LbfgsLogisticRegressionBinaryTrainer.Options options = new()
        {
            LabelColumnName = nameof(ModelInput.Label),
            FeatureColumnName = nameof(ModelInput.Features),
            L1Regularization = L1Regularization,
            L2Regularization = L2Regularization,
            NumberOfThreads = NumberOfThreads,
        };

        // Karistirma yok, onbellek noktasi yok: veri zaten bellekte ve tek gecis
        // yeterli, ekleseydik yalnizca bellek kullanimi artardi.
        return context.BinaryClassification
            .Trainers
            .LbfgsLogisticRegression(options)
            .Fit(Load(context, rows));
    }

    /// <summary>Yuklenmis bir modelle puanlama; <see cref="LoadedModel"/> icin acik.</summary>
    internal static IReadOnlyList<double> Probabilities(
        MLContext context,
        ITransformer transformer,
        IReadOnlyList<ModelInput> rows)
    {
        IDataView scored = transformer.Transform(Load(context, rows));
        List<double> probabilities = new(rows.Count);

        foreach (ModelOutput output in context.Data.CreateEnumerable<ModelOutput>(scored, reuseRowObject: false))
        {
            probabilities.Add(output.Probability);
        }

        return probabilities;
    }

    private static Coefficients Read(ITransformer transformer)
    {
        LinearBinaryModelParameters parameters =
            ((BinaryPredictionTransformer<Microsoft.ML.Calibrators.CalibratedModelParametersBase<
                LinearBinaryModelParameters,
                Microsoft.ML.Calibrators.PlattCalibrator>>)transformer).Model.SubModel;

        return new Coefficients(parameters.Bias, [.. parameters.Weights.Select(weight => (double)weight)]);
    }
}
