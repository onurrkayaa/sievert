using System.Globalization;
using System.Text;

using Sievert.Modeling;

namespace Sievert.Measure;

/// <summary>
/// Asama 6 Adim 3: aciklama toleransinin v1 ve v2 kurallariyla yeniden olcumu.
///
/// Girdi dondurulmus anlik goruntu, veritabani degil. Sebep: bu olcumun iki kosuda ayni
/// baytlari vermesi gerekiyor, veritabani ise yeniden madencilik gorebilir.
///
/// Eski v1 sonucu bu komutun ciktisinda da duruyor; yeni kural eskisini silmiyor,
/// yanina yaziliyor.
/// </summary>
public static class ExplanationToleranceCommand
{
    /// <summary>Mutasyon testinin urettigi mutlak logit farki.</summary>
    private const double MutationLogitDrift = 1e-4;

    public static int Run(string[] args)
    {
        string repositoryRoot = Path.GetFullPath(args[1]);
        string codeCommit = args[2];
        string outputPath = args[3];

        string dataDirectory = Path.Combine(repositoryRoot, "data", "asama5");
        string snapshot = Path.Combine(dataDirectory, "commit-metrics.csv");
        string modelResults = Path.Combine(dataDirectory, "model-results.json");

        foreach (string file in (string[])[snapshot, modelResults])
        {
            FileChecksum.Verify(file, Path.ChangeExtension(file, ".sha256"));
        }

        Console.WriteLine("Dondurulmus dosyalarin ozetleri dogrulandi.");

        ModelRegistry registry = ModelRegistry.Create(modelResults, Path.Combine(dataDirectory, "models"));
        IReadOnlyList<SnapshotRow> rows = SnapshotReader.Read(snapshot);

        List<ProfileResult> results = [];

        foreach (ModelProfile profile in registry.Profiles)
        {
            results.Add(Measure(registry, profile, rows));
        }

        Print(results);

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        File.WriteAllText(outputPath, Render(codeCommit, results), new UTF8Encoding(false));

        Console.WriteLine();
        Console.WriteLine($"{outputPath} yazildi.");

        // v2'de kalan satir ya da reddedilmeyen mutasyon varsa cikis kodu 1.
        return results.All(result => result.V2Failures.Count == 0 && result.Mutation.Rejected) ? 0 : 1;
    }

    private static ProfileResult Measure(ModelRegistry registry, ModelProfile profile, IReadOnlyList<SnapshotRow> allRows)
    {
        FeatureScaler scaler = registry.ScalerFor(profile);
        LoadedModel model = registry.Load(profile.ProfileCode);

        List<SnapshotRow> rows =
        [
            .. allRows.Where(row => string.Equals(
                row.RepositoryIdentity,
                profile.RepositoryIdentity,
                StringComparison.Ordinal))
        ];

        List<double> normalized = [];
        List<Failure> v1Failures = [];
        List<Failure> v2Failures = [];

        double worstAbsolute = 0.0;
        double worstAllowed = 0.0;

        foreach (SnapshotRow row in rows)
        {
            ModelExplanation explanation = ModelExplainer.Explain(profile, scaler, model, row);

            normalized.Add(explanation.NormalizedError);
            worstAbsolute = Math.Max(worstAbsolute, explanation.AbsoluteError);
            worstAllowed = Math.Max(worstAllowed, explanation.AllowedError);

            if (explanation.AbsoluteError > ModelExplainer.AbsoluteToleranceV1)
            {
                v1Failures.Add(Failure.Of(row, explanation));
            }

            if (!explanation.IsWithinTolerance)
            {
                v2Failures.Add(Failure.Of(row, explanation));
            }
        }

        normalized.Sort();

        return new ProfileResult(
            profile.ProfileCode,
            profile.RepositoryIdentity,
            rows.Count,
            v1Failures,
            v2Failures,
            worstAbsolute,
            worstAllowed,
            normalized.Count == 0 ? 0.0 : normalized[^1],
            Percentile(normalized, 0.50),
            Percentile(normalized, 0.95),
            Percentile(normalized, 0.99),
            Mutate(profile, scaler, model, rows[0]));
    }

    /// <summary>
    /// Tek bir katsayiyi, en az <see cref="MutationLogitDrift"/> kadar logit farki
    /// uretecek sekilde bozar ve v2'nin reddedip reddetmedigine bakar.
    ///
    /// Bu, toleransin fazla genis olup olmadiginin kontrolu. Her seyi kabul eden bir
    /// tolerans da "gecti" derdi.
    /// </summary>
    private static Mutation Mutate(ModelProfile profile, FeatureScaler scaler, LoadedModel model, SnapshotRow row)
    {
        ModelExplanation honest = ModelExplainer.Explain(profile, scaler, model, row);

        FeatureEffect target = honest.Effects
            .Where(effect => Math.Abs(effect.TransformedValue) > 1e-3)
            .MaxBy(effect => Math.Abs(effect.TransformedValue))!;

        int position = honest.Effects.ToList().FindIndex(effect => effect.Name == target.Name);

        double[] weights = [.. profile.Coefficients.Weights];
        weights[position] += MutationLogitDrift / target.TransformedValue;

        ModelProfile corrupted = profile with
        {
            Coefficients = new Coefficients(profile.Coefficients.Intercept, weights),
        };

        ModelExplanation mutated = ModelExplainer.Explain(corrupted, scaler, model, row);

        return new Mutation(
            target.Name,
            mutated.AbsoluteError,
            mutated.AllowedError,
            mutated.NormalizedError,
            !mutated.IsWithinTolerance);
    }

    private static double Percentile(List<double> sorted, double share) => sorted.Count == 0
        ? 0.0
        : sorted[Math.Min(sorted.Count - 1, (int)Math.Ceiling(share * sorted.Count) - 1)];

    private static void Print(List<ProfileResult> results)
    {
        Console.WriteLine();
        Console.WriteLine($"{"profil",-10}{"satir",8}{"v1 kalan",10}{"v2 kalan",10}{"p50",12}{"p95",12}{"p99",12}{"max",12}");

        foreach (ProfileResult result in results)
        {
            Console.WriteLine(
                $"{result.ProfileCode,-10}{result.Rows,8}{result.V1Failures.Count,10}{result.V2Failures.Count,10}"
                + $"{result.MedianNormalized,12:F4}{result.P95Normalized,12:F4}"
                + $"{result.P99Normalized,12:F4}{result.MaxNormalized,12:F4}");
        }

        Console.WriteLine();
        Console.WriteLine($"toplam satir      : {results.Sum(result => result.Rows)}");
        Console.WriteLine($"v1'de kalan       : {results.Sum(result => result.V1Failures.Count)}");
        Console.WriteLine($"v2'de kalan       : {results.Sum(result => result.V2Failures.Count)}");
        Console.WriteLine($"en buyuk mutlak   : {results.Max(result => result.WorstAbsolute):E3}");
        Console.WriteLine($"en buyuk izin     : {results.Max(result => result.WorstAllowed):E3}");
        Console.WriteLine($"en buyuk normalize: {results.Max(result => result.MaxNormalized):F4}");

        Console.WriteLine();
        Console.WriteLine("v1'de kalan satirlarin v2 sonucu:");

        foreach (ProfileResult result in results)
        {
            foreach (Failure failure in result.V1Failures)
            {
                Console.WriteLine(
                    $"  {result.ProfileCode,-9} {failure.Sha[..12]}  mutlak {failure.AbsoluteError:E3}  "
                    + $"olcek {failure.Scale,8:F2}  izin {failure.AllowedError:E3}  normalize {failure.NormalizedError:F4}  "
                    + (failure.NormalizedError <= 1.0 ? "v2 GECTI" : "v2 KALDI"));
            }
        }

        Console.WriteLine();
        Console.WriteLine("mutasyon testi (tek katsayi, en az 1e-4 logit farki):");

        foreach (ProfileResult result in results)
        {
            Console.WriteLine(
                $"  {result.ProfileCode,-9} {result.Mutation.FeatureName,-22} mutlak {result.Mutation.AbsoluteError:E3}  "
                + $"izin {result.Mutation.AllowedError:E3}  normalize {result.Mutation.NormalizedError:F1}  "
                + (result.Mutation.Rejected ? "REDDEDILDI" : "KABUL EDILDI - TOLERANS COK GENIS"));
        }
    }

    private static string Render(string codeCommit, List<ProfileResult> results)
    {
        StringBuilder text = new();
        text.Append("{\n");
        text.Append("  \"codeCommit\": \"").Append(codeCommit).Append("\",\n");
        text.Append("  \"contractVersion\": \"2.0\",\n");
        text.Append("  \"absoluteToleranceV1\": ").Append(Number(ModelExplainer.AbsoluteToleranceV1)).Append(",\n");
        text.Append("  \"floatUnitRoundoff\": ").Append(Number(ModelExplainer.FloatUnitRoundoff)).Append(",\n");
        text.Append("  \"mutationLogitDrift\": ").Append(Number(MutationLogitDrift)).Append(",\n");
        text.Append("  \"profiles\": [\n");

        for (int index = 0; index < results.Count; index++)
        {
            ProfileResult result = results[index];

            text.Append("    {\n");
            text.Append("      \"profile\": \"").Append(result.ProfileCode).Append("\",\n");
            text.Append("      \"repository\": \"").Append(result.RepositoryIdentity).Append("\",\n");
            text.Append("      \"rows\": ").Append(result.Rows).Append(",\n");
            text.Append("      \"v1Failures\": ").Append(result.V1Failures.Count).Append(",\n");
            text.Append("      \"v2Failures\": ").Append(result.V2Failures.Count).Append(",\n");
            text.Append("      \"worstAbsoluteError\": ").Append(Number(result.WorstAbsolute)).Append(",\n");
            text.Append("      \"worstAllowedError\": ").Append(Number(result.WorstAllowed)).Append(",\n");
            text.Append("      \"normalizedError\": {\n");
            text.Append("        \"p50\": ").Append(Number(result.MedianNormalized)).Append(",\n");
            text.Append("        \"p95\": ").Append(Number(result.P95Normalized)).Append(",\n");
            text.Append("        \"p99\": ").Append(Number(result.P99Normalized)).Append(",\n");
            text.Append("        \"max\": ").Append(Number(result.MaxNormalized)).Append('\n');
            text.Append("      },\n");
            text.Append("      \"mutation\": {\n");
            text.Append("        \"feature\": \"").Append(result.Mutation.FeatureName).Append("\",\n");
            text.Append("        \"absoluteError\": ").Append(Number(result.Mutation.AbsoluteError)).Append(",\n");
            text.Append("        \"allowedError\": ").Append(Number(result.Mutation.AllowedError)).Append(",\n");
            text.Append("        \"normalizedError\": ").Append(Number(result.Mutation.NormalizedError)).Append(",\n");
            text.Append("        \"rejected\": ").Append(result.Mutation.Rejected ? "true" : "false").Append('\n');
            text.Append("      },\n");
            text.Append("      \"v1FailingRows\": [");

            for (int failure = 0; failure < result.V1Failures.Count; failure++)
            {
                Failure item = result.V1Failures[failure];

                text.Append(failure == 0 ? "\n" : ",\n");
                text.Append("        { \"sha\": \"").Append(item.Sha).Append("\", ");
                text.Append("\"absoluteError\": ").Append(Number(item.AbsoluteError)).Append(", ");
                text.Append("\"scale\": ").Append(Number(item.Scale)).Append(", ");
                text.Append("\"allowedError\": ").Append(Number(item.AllowedError)).Append(", ");
                text.Append("\"normalizedError\": ").Append(Number(item.NormalizedError)).Append(", ");
                text.Append("\"passesV2\": ").Append(item.NormalizedError <= 1.0 ? "true" : "false").Append(" }");
            }

            text.Append(result.V1Failures.Count == 0 ? "]\n" : "\n      ]\n");
            text.Append(index == results.Count - 1 ? "    }\n" : "    },\n");
        }

        text.Append("  ]\n");
        text.Append("}\n");

        return text.ToString();
    }

    private static string Number(double value) => value.ToString("R", CultureInfo.InvariantCulture);

    private sealed record Failure(string Sha, double AbsoluteError, double Scale, double AllowedError, double NormalizedError)
    {
        public static Failure Of(SnapshotRow row, ModelExplanation explanation) => new(
            row.Sha,
            explanation.AbsoluteError,
            explanation.Scale,
            explanation.AllowedError,
            explanation.NormalizedError);
    }

    private sealed record Mutation(
        string FeatureName,
        double AbsoluteError,
        double AllowedError,
        double NormalizedError,
        bool Rejected);

    private sealed record ProfileResult(
        string ProfileCode,
        string RepositoryIdentity,
        int Rows,
        List<Failure> V1Failures,
        List<Failure> V2Failures,
        double WorstAbsolute,
        double WorstAllowed,
        double MaxNormalized,
        double MedianNormalized,
        double P95Normalized,
        double P99Normalized,
        Mutation Mutation);
}
