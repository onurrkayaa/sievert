using Microsoft.ML;

namespace Sievert.Modeling;

/// <summary>
/// Diskten bir kez yuklenmis ML.NET modeli. <see cref="ITransformer"/> durumsuz oldugu
/// icin ayni ornek birden fazla istek tarafindan kullanilabiliyor; her istekte modeli
/// yeniden yuklemek hem yavas hem gereksiz.
/// </summary>
public sealed class LoadedModel
{
    private readonly MLContext context;

    private readonly ITransformer transformer;

    private LoadedModel(MLContext context, ITransformer transformer)
    {
        this.context = context;
        this.transformer = transformer;
    }

    public static LoadedModel FromFile(string path)
    {
        MLContext context = new(seed: LogisticRegressionModel.Seed);

        return new LoadedModel(context, context.Model.Load(path, out _));
    }

    /// <summary>Tek bir satirin ham skoru.</summary>
    public double Score(float[] features) => Score([features])[0];

    /// <summary>Tek bir satirin logit'i ve ham skoru birlikte.</summary>
    public ModelScore Evaluate(float[] features) =>
        LogisticRegressionModel.Outputs(context, transformer, [new ModelInput { Features = features }])[0];

    /// <summary>Verilen satirlarin ham skorlari, giris sirasinda.</summary>
    public IReadOnlyList<double> Score(IReadOnlyList<float[]> rows)
    {
        List<ModelInput> inputs = new(rows.Count);

        foreach (float[] features in rows)
        {
            inputs.Add(new ModelInput { Features = features });
        }

        return LogisticRegressionModel.Probabilities(context, transformer, inputs);
    }
}
