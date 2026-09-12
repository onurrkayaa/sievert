using System.Globalization;

using Sievert.Modeling;

namespace Sievert.Measure;

/// <summary>
/// Kor dogrulama listesini anahtar dosyasiyla birlestirip sayar.
///
/// Model-pozitif ve model-negatif ornekler TEK SAYIDA BIRLESTIRILMIYOR; orneklem
/// siniflara esit dagitildigi icin populasyon accuracy, genel precision, genel recall ya
/// da genel hata orani hesaplanmiyor (olcut dosyasi).
/// </summary>
public static class TallyCommand
{
    public static int Run(string[] args)
    {
        string keyPath = args[1];
        string materialPath = args[2];

        IReadOnlyDictionary<string, string> decisions = ValidationTally.ReadDecisions(materialPath);
        List<ValidationDecision> rows = [];
        List<string> missing = [];

        foreach (string line in File.ReadLines(keyPath).Skip(1))
        {
            string[] fields = line.Split(',');
            string sample = fields[0];

            if (!decisions.TryGetValue(sample, out string? decision) || decision.Length == 0)
            {
                missing.Add(sample);
                continue;
            }

            rows.Add(new ValidationDecision(sample, fields[1], fields[3] == "1", fields[6] == "1", decision));
        }

        if (missing.Count > 0)
        {
            Console.WriteLine($"Eksik karar: {missing.Count}");

            return 0;
        }

        Console.WriteLine(
            "Otuz commit, onceden ilan edilmis olcutlerle yazar tarafindan commit diff'i ve sonraki");
        Console.WriteLine(
            "ilgili degisiklikler incelenerek siniflandirildi; model tahmini ve otomatik etiket");
        Console.WriteLine(
            "degerlendiriciye gosterilmedi, bagimsiz ikinci degerlendirici kullanilmadi.");
        Console.WriteLine();

        TallyCounts positives = ValidationTally.Count(
            "model-pozitif", rows.Where(row => row.ModelPrediction));
        TallyCounts negatives = ValidationTally.Count(
            "model-negatif", rows.Where(row => !row.ModelPrediction));

        Print(positives, "insan dogrulama precision isareti");
        Print(negatives, "insan dogrulama kacirma isareti");

        Console.WriteLine("== Repo bazinda ==");

        foreach (string identity in rows.Select(row => row.Identity).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal))
        {
            Console.WriteLine($"  {identity}");

            // sievert:disable SV004 rows bellekte bir liste, veritabani sorgusu degil
            List<ValidationDecision> forRepository = [.. rows.Where(row => row.Identity == identity)];

            // sievert:disable SV004 forRepository bellekte bir liste, veritabani sorgusu degil
            TallyCounts repositoryPositives = ValidationTally.Count("  model-pozitif", forRepository.Where(row => row.ModelPrediction));
            // sievert:disable SV004 ayni bellekteki liste uzerinde suzme
            TallyCounts repositoryNegatives = ValidationTally.Count("  model-negatif", forRepository.Where(row => !row.ModelPrediction));

            Print(repositoryPositives, "precision isareti");
            Print(repositoryNegatives, "kacirma isareti");
        }

        (int positiveIntroduced, int positiveNot, int negativeIntroduced, int negativeNot, int unresolved) =
            ValidationTally.AgainstSzz(rows);

        Console.WriteLine("== SZZ etiketi ile insan karari ==");
        Console.WriteLine($"  SZZ pozitif + KUSUR-GETIRDI   : {positiveIntroduced}");
        Console.WriteLine($"  SZZ pozitif + KUSUR-GETIRMEDI : {positiveNot}");
        Console.WriteLine($"  SZZ negatif + KUSUR-GETIRDI   : {negativeIntroduced}");
        Console.WriteLine($"  SZZ negatif + KUSUR-GETIRMEDI : {negativeNot}");
        Console.WriteLine($"  VERI-YETMEDI / BAKILMADI      : {unresolved}");

        return 0;
    }

    private static void Print(TallyCounts counts, string label)
    {
        Console.WriteLine($"{counts.Name}:");
        Console.WriteLine($"  incelenen        : {counts.Reviewed}");
        Console.WriteLine($"  KUSUR-GETIRDI    : {counts.Introduced}");
        Console.WriteLine($"  KUSUR-GETIRMEDI  : {counts.NotIntroduced}");
        Console.WriteLine($"  VERI-YETMEDI     : {counts.NotEnoughData}");
        Console.WriteLine($"  BAKILMADI        : {counts.NotReviewed}");
        Console.WriteLine(counts.Signal is double signal
            ? $"  {label}: {counts.Introduced} / {counts.Denominator} = {signal.ToString("F4", CultureInfo.InvariantCulture)}"
            : $"  {label}: N/A ({counts.Introduced} / {counts.Denominator})");
        Console.WriteLine();
    }
}
