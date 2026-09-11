namespace Sievert.Core.Rules;

/// <summary>Bir check calistirmasinin sonundaki toplu sayilar.</summary>
/// <param name="FileCount">Taranan dosya sayisi.</param>
/// <param name="FindingCount">Bulunan toplam bulgu sayisi.</param>
/// <param name="ByRuleCode">Kural koduna gore bulgu sayilari. Hic bulgu yoksa bos.</param>
public sealed record CheckSummary(int FileCount, int FindingCount, IReadOnlyDictionary<string, int> ByRuleCode)
{
    /// <summary>Bulgulari sayip ozeti cikarir. Kural kodlari her calistirmada ayni sirada gelsin diye siralanir.</summary>
    public static CheckSummary Of(int fileCount, IReadOnlyList<Finding> findings) =>
        new(
            fileCount,
            findings.Count,
            findings
                .GroupBy(finding => finding.RuleCode, StringComparer.Ordinal)
                .OrderBy(group => group.Key, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal));
}
