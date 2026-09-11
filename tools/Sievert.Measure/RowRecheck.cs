using System.Text.RegularExpressions;

using Sievert.Mining;

namespace Sievert.Measure;

/// <summary>
/// Dogrulama listesindeki her satiri, listeyi ureten koddan BAGIMSIZ bir yoldan kontrol
/// eder: satirdaki duzeltme commit'i icin SZZ'yi bastan calistirip, satirda yazan
/// suclanan commit'in gercekten suclananlar arasinda olup olmadigina bakar.
///
/// Listeyi yeniden uretip karsilastirmak yetmiyordu: uretici ile liste ayni kodu
/// kullandigi icin o karsilastirma yalnizca "uretici deterministik mi" sorusunu
/// cevapliyor. Burasi "liste bugunku algoritmanin sonucu mu" sorusunu cevapliyor.
/// </summary>
public static partial class RowRecheck
{
    public static void Report(string repositoryPath, string listPath, string repositoryHeading)
    {
        BugIntroducerFinder finder = new();
        bool inThisRepo = false;
        bool labelledSection = true;

        int labelledRows = 0;
        int labelledConfirmed = 0;
        int notLabelledRows = 0;
        int notLabelledConfirmed = 0;

        foreach (string line in File.ReadLines(listPath))
        {
            if (line.StartsWith("## ", StringComparison.Ordinal))
            {
                inThisRepo = line.Contains(repositoryHeading, StringComparison.OrdinalIgnoreCase);
            }

            if (line.StartsWith("### ", StringComparison.Ordinal))
            {
                labelledSection = !line.Contains("Etiketlenmemis", StringComparison.Ordinal);
            }

            if (!inThisRepo || RowPattern().Match(line.Trim()) is not { Success: true } match)
            {
                continue;
            }

            string fixSha = match.Groups[1].Value;
            string culpritSha = match.Groups[2].Value;

            SzzOutcome outcome = finder.Find(
                repositoryPath,
                [new SzzFix(fixSha, DateTimeOffset.MaxValue)],
                SzzOptions.Default);

            bool blamed = outcome.BlamedShas.Any(sha => sha.StartsWith(culpritSha, StringComparison.Ordinal));

            if (labelledSection)
            {
                labelledRows++;

                if (blamed)
                {
                    labelledConfirmed++;
                }
                else
                {
                    Console.WriteLine($"  ETIKETLI satir dogrulanamadi: {fixSha} -> {culpritSha}");
                }
            }
            else
            {
                notLabelledRows++;

                if (!blamed)
                {
                    notLabelledConfirmed++;
                }
                else
                {
                    Console.WriteLine($"  ETIKETSIZ satir aslinda suclanmis: {fixSha} -> {culpritSha}");
                }
            }
        }

        Console.WriteLine(
            $"{repositoryHeading}: etiketli {labelledConfirmed}/{labelledRows} dogrulandi, "
            + $"etiketsiz {notLabelledConfirmed}/{notLabelledRows} dogrulandi");
    }

    [GeneratedRegex(@"^\|\s*\d+\s*\|\s*`([0-9a-f]+)`.*?\|\s*`([0-9a-f]+)`")]
    private static partial Regex RowPattern();
}
