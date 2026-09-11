namespace Sievert.Core.Rules;

/// <summary>Tek bir kural kodunun kac bulgu urettigi.</summary>
/// <param name="RuleCode">Kuralin kodu, ornegin "SV001".</param>
/// <param name="Count">O kuraldan cikan bulgu sayisi.</param>
public sealed record RuleCodeCount(string RuleCode, int Count);

/// <summary>Bir check calistirmasinin sonundaki toplu sayilar.</summary>
/// <param name="FileCount">Taranan dosya sayisi.</param>
/// <param name="FindingCount">Bulunan toplam bulgu sayisi.</param>
/// <param name="ByRuleCode">
/// Kural koduna gore bulgu sayilari, koda gore alfabetik sirali. Hic bulgu yoksa bos.
/// Dinamik anahtarli bir nesne yerine dizi: semasi sabit kaliyor, guclu tipli okunabiliyor.
/// </param>
public sealed record CheckSummary(int FileCount, int FindingCount, IReadOnlyList<RuleCodeCount> ByRuleCode)
{
    /// <summary>Bulgulari sayip ozeti cikarir. Cikti her calistirmada ayni olsun diye kural kodlari siralanir.</summary>
    public static CheckSummary Of(int fileCount, IReadOnlyList<Finding> findings) =>
        new(
            fileCount,
            findings.Count,
            findings
                .GroupBy(finding => finding.RuleCode, StringComparer.Ordinal)
                .OrderBy(group => group.Key, StringComparer.Ordinal)
                .Select(group => new RuleCodeCount(group.Key, group.Count()))
                .ToList());
}
