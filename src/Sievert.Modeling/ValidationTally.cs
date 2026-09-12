namespace Sievert.Modeling;

/// <summary>Bir kumedeki karar sayimlari.</summary>
public sealed record TallyCounts(
    string Name,
    int Reviewed,
    int Introduced,
    int NotIntroduced,
    int NotEnoughData,
    int NotReviewed)
{
    /// <summary>Payda yalnizca karar verilen satirlar.</summary>
    public int Denominator => Introduced + NotIntroduced;

    /// <summary>
    /// Model-pozitif kumede "precision isareti", model-negatif kumede "kacirma isareti".
    /// Ikisi de ayni hesap ama ayri anlam; birlestirilmiyor. Payda 0 ise N/A.
    /// </summary>
    public double? Signal => Denominator == 0 ? null : (double)Introduced / Denominator;
}

/// <summary>Bir kararin ham hali.</summary>
public readonly record struct ValidationDecision(string SampleId, string Identity, bool ModelPrediction, bool SzzLabel, string Decision);

/// <summary>
/// Kor dogrulama listesinin sayimi.
///
/// Model-pozitif ve model-negatif ornekler **tek sayida birlestirilmiyor**. Orneklem
/// siniflara esit dagitildigi icin populasyon accuracy, genel precision, genel recall ya
/// da genel hata orani hesaplanmiyor (olcut dosyasi).
/// </summary>
public static class ValidationTally
{
    public const string Introduced = "KUSUR-GETIRDI";

    public const string NotIntroduced = "KUSUR-GETIRMEDI";

    public const string NotEnoughData = "VERI-YETMEDI";

    public const string NotReviewed = "BAKILMADI";

    public static readonly IReadOnlyList<string> Categories =
        [Introduced, NotIntroduced, NotEnoughData, NotReviewed];

    public static TallyCounts Count(string name, IEnumerable<ValidationDecision> decisions)
    {
        int introduced = 0;
        int notIntroduced = 0;
        int notEnough = 0;
        int notReviewed = 0;

        foreach (ValidationDecision decision in decisions)
        {
            switch (decision.Decision)
            {
                case Introduced:
                    introduced++;
                    break;

                case NotIntroduced:
                    notIntroduced++;
                    break;

                case NotEnoughData:
                    notEnough++;
                    break;

                case NotReviewed:
                    notReviewed++;
                    break;

                default:
                    throw new InvalidDataException(
                        $"{decision.SampleId}: taninmayan karar '{decision.Decision}'. "
                        + $"Gecerli degerler: {string.Join(", ", Categories)}.");
            }
        }

        return new TallyCounts(
            name,
            introduced + notIntroduced + notEnough + notReviewed,
            introduced,
            notIntroduced,
            notEnough,
            notReviewed);
    }

    /// <summary>SZZ etiketiyle insan karari arasindaki dort hucre. Belirsizler ayri.</summary>
    public static (int SzzPositiveIntroduced, int SzzPositiveNot, int SzzNegativeIntroduced, int SzzNegativeNot, int Unresolved)
        AgainstSzz(IEnumerable<ValidationDecision> decisions)
    {
        int positiveIntroduced = 0;
        int positiveNot = 0;
        int negativeIntroduced = 0;
        int negativeNot = 0;
        int unresolved = 0;

        foreach (ValidationDecision decision in decisions)
        {
            switch (decision.Decision)
            {
                case Introduced when decision.SzzLabel:
                    positiveIntroduced++;
                    break;

                case NotIntroduced when decision.SzzLabel:
                    positiveNot++;
                    break;

                case Introduced:
                    negativeIntroduced++;
                    break;

                case NotIntroduced:
                    negativeNot++;
                    break;

                default:
                    unresolved++;
                    break;
            }
        }

        return (positiveIntroduced, positiveNot, negativeIntroduced, negativeNot, unresolved);
    }

    /// <summary>
    /// Malzeme dosyasindaki "Karar:" satirlarini okur. Bos koseli ayrac karar
    /// verilmemis demek; sayim yapilmadan once bildirilir.
    /// </summary>
    public static IReadOnlyDictionary<string, string> ReadDecisions(string materialPath)
    {
        Dictionary<string, string> decisions = new(StringComparer.Ordinal);
        string? sample = null;
        bool expecting = false;

        foreach (string line in File.ReadLines(materialPath))
        {
            string trimmed = line.Trim();

            if (trimmed.StartsWith("### Ornek ", StringComparison.Ordinal))
            {
                sample = trimmed["### Ornek ".Length..].Split(' ')[0].Trim();
                expecting = false;
                continue;
            }

            if (trimmed.StartsWith("Karar:", StringComparison.Ordinal))
            {
                expecting = true;
                continue;
            }

            if (!expecting || trimmed.Length == 0 || sample is null)
            {
                continue;
            }

            if (trimmed.StartsWith('[') && trimmed.EndsWith(']'))
            {
                decisions[sample] = trimmed[1..^1].Trim();
            }

            expecting = false;
        }

        return decisions;
    }
}
