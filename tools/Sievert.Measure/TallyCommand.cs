using System.Globalization;
using System.Text;
using System.Text.Json;

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
        string? outputPath = args.Length > 3 ? args[3] : null;
        string? codeCommit = args.Length > 4 ? args[4] : null;

        IReadOnlyDictionary<string, string> decisions = ValidationTally.ReadDecisions(materialPath);
        List<ValidationDecision> rows = [];
        List<string> missing = [];
        Dictionary<string, string> confusion = new(StringComparer.Ordinal);

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
            confusion[sample] = fields[7].Trim();
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

        if (outputPath is not null)
        {
            Write(outputPath, codeCommit ?? "bilinmiyor", keyPath, rows, confusion);
        }

        return 0;
    }

    /// <summary>
    /// Sonuc dosyasi. Oranlar YALNIZCA karar verilmis ornekler uzerinden; BAKILMADI ve
    /// VERI-YETMEDI paydadan cikiyor ve ayri sayiliyor. Model-pozitif ve model-negatif
    /// kumeler birlestirilmiyor; populasyon accuracy / precision / recall hesaplanmiyor
    /// (olcut dosyasi).
    /// </summary>
    private static void Write(
        string path,
        string codeCommit,
        string keyPath,
        IReadOnlyList<ValidationDecision> rows,
        IReadOnlyDictionary<string, string> confusion)
    {
        using MemoryStream stream = new();

        using (Utf8JsonWriter writer = new(stream, new JsonWriterOptions { Indented = true }))
        {
            writer.WriteStartObject();

            writer.WriteStartObject("provenance");
            writer.WriteString("humanDecisionCommit", "6806406");
            writer.WriteString("unreviewedRecordedCommit", "b364f1a");
            writer.WriteString("criteriaFile", "docs/olcumler/asama5-tahmin-dogrulama-olcut.md");
            writer.WriteString("criteriaCommit", "a3088a0");
            writer.WriteString("sampleKeyChecksum", FileChecksum.Sha256(keyPath));
            writer.WriteString("snapshotChecksum", "1b8e5a5c64ff9ccc6f8495f90711a05b95b5340b2e778dce59a9c6cea4d46e95");
            writer.WriteString("splitManifestChecksum", "01b5cafa2c82cde3dffc98afc0ed571918ebbc648bb8c74fad2bc071b601b8cb");
            writer.WriteString("modelResultsChecksum", "cd3b3e3d7575859b7d7bc446024df1ee140aec7f5fb710a4f4b265578249a0b5");
            writer.WriteNumber("validationSampleSize", 30);
            writer.WriteString("eligibility", "CsFilesChanged > 0");
            writer.WriteString("sampling", "her repo icin 5 model-pozitif + 5 model-negatif");
            writer.WriteNumber("seed", 20260912);
            writer.WriteString("codeCommit", codeCommit);
            writer.WriteString(
                "method",
                "Otuz commitlik kor orneklemin 25'i yazar tarafindan commit diff'i ve sonraki "
                + "ilgili degisiklikler incelenerek siniflandirildi; bes ornek incelenmedi ve "
                + "paydalardan cikarildi. Model tahmini ve otomatik etiket kararlar tamamlanana "
                + "kadar degerlendiriciye gosterilmedi; bagimsiz ikinci degerlendirici kullanilmadi.");
            writer.WriteEndObject();

            WriteCounts(writer, "overall", ValidationTally.Count("toplam", rows));

            writer.WriteStartArray("byPredictionClass");
            WriteGroup(writer, "model-pozitif", "precisionSignal", rows.Where(row => row.ModelPrediction));
            WriteGroup(writer, "model-negatif", "missSignal", rows.Where(row => !row.ModelPrediction));
            writer.WriteEndArray();

            writer.WriteStartArray("byRepository");

            foreach (string identity in Identities(rows))
            {
                // sievert:disable SV004 rows bellekte bir liste, veritabani sorgusu degil
                List<ValidationDecision> forRepository = [.. rows.Where(row => row.Identity == identity)];

                writer.WriteStartObject();
                writer.WriteString("repository", identity);
                WriteCounts(writer, "all", ValidationTally.Count(identity, forRepository));
                writer.WriteStartArray("byPredictionClass");
                WriteGroup(writer, "model-pozitif", "precisionSignal", forRepository.Where(row => row.ModelPrediction));
                WriteGroup(writer, "model-negatif", "missSignal", forRepository.Where(row => !row.ModelPrediction));
                writer.WriteEndArray();
                WriteSzz(writer, forRepository);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();

            WriteSzz(writer, rows);
            WriteConfusionGroups(writer, rows, confusion);

            writer.WriteEndObject();
        }

        string json = Encoding.UTF8.GetString(stream.ToArray()) + "\n";
        File.WriteAllText(path, json, new UTF8Encoding(false));
        File.WriteAllText(Path.ChangeExtension(path, ".sha256"), FileChecksum.Line(path));

        Console.WriteLine();
        Console.WriteLine($"{Path.GetFileName(path)}: {FileChecksum.Sha256(path)}");
    }

    private static string[] Identities(IReadOnlyList<ValidationDecision> rows) =>
        [.. rows.Select(row => row.Identity).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)];

    private static void WriteGroup(
        Utf8JsonWriter writer,
        string name,
        string signalName,
        IEnumerable<ValidationDecision> rows)
    {
        TallyCounts counts = ValidationTally.Count(name, rows);

        writer.WriteStartObject();
        writer.WriteString("group", name);
        WriteCounts(writer, "counts", counts);
        writer.WriteStartObject(signalName);
        writer.WriteNumber("numerator", counts.Introduced);
        writer.WriteNumber("denominator", counts.Denominator);
        writer.WriteNumber("excludedNotEnoughData", counts.NotEnoughData);
        writer.WriteNumber("excludedNotReviewed", counts.NotReviewed);

        if (counts.Signal is double signal)
        {
            writer.WriteNumber("ratio", signal);
        }
        else
        {
            writer.WriteNull("ratio");
        }

        writer.WriteEndObject();
        writer.WriteEndObject();
    }

    private static void WriteCounts(Utf8JsonWriter writer, string name, TallyCounts counts)
    {
        writer.WriteStartObject(name);
        writer.WriteNumber("reviewed", counts.Reviewed);
        writer.WriteNumber("introduced", counts.Introduced);
        writer.WriteNumber("notIntroduced", counts.NotIntroduced);
        writer.WriteNumber("notEnoughData", counts.NotEnoughData);
        writer.WriteNumber("notReviewed", counts.NotReviewed);
        writer.WriteNumber("decidedDenominator", counts.Denominator);
        writer.WriteEndObject();
    }

    private static void WriteSzz(Utf8JsonWriter writer, IReadOnlyList<ValidationDecision> rows)
    {
        (int positiveIntroduced, int positiveNot, int negativeIntroduced, int negativeNot, int unresolved) =
            ValidationTally.AgainstSzz(rows);

        writer.WriteStartObject("szzCrossTable");
        writer.WriteNumber("szzPositiveIntroduced", positiveIntroduced);
        writer.WriteNumber("szzPositiveNotIntroduced", positiveNot);
        writer.WriteNumber("szzNegativeIntroduced", negativeIntroduced);
        writer.WriteNumber("szzNegativeNotIntroduced", negativeNot);
        writer.WriteNumber("unresolved", unresolved);

        WriteRatio(writer, "szzPositiveConfirmationSignal", positiveIntroduced, positiveIntroduced + positiveNot);
        WriteRatio(writer, "szzNegativeMissSignal", negativeIntroduced, negativeIntroduced + negativeNot);

        writer.WriteEndObject();
    }

    private static void WriteRatio(Utf8JsonWriter writer, string name, int numerator, int denominator)
    {
        writer.WriteStartObject(name);
        writer.WriteNumber("numerator", numerator);
        writer.WriteNumber("denominator", denominator);

        if (denominator > 0)
        {
            writer.WriteNumber("ratio", (double)numerator / denominator);
        }
        else
        {
            writer.WriteNull("ratio");
        }

        writer.WriteEndObject();
    }

    /// <summary>Model-SZZ anlasmazliklarinda insan karari. Orneklem ici olgular.</summary>
    private static void WriteConfusionGroups(
        Utf8JsonWriter writer,
        IReadOnlyList<ValidationDecision> rows,
        IReadOnlyDictionary<string, string> confusion)
    {
        writer.WriteStartArray("byConfusionGroup");

        foreach (string cell in (string[])["TP", "FP", "FN", "TN"])
        {
            // sievert:disable SV004 rows bellekte bir liste, veritabani sorgusu degil
            List<ValidationDecision> group = [.. rows.Where(row => confusion[row.SampleId] == cell)];

            writer.WriteStartObject();
            writer.WriteString("cell", cell);
            WriteCounts(writer, "counts", ValidationTally.Count(cell, group));
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
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
