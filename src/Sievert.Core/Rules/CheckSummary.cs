namespace Sievert.Core.Rules;

/// <summary>
/// Bir calistirmada hangi kurallarin etkin oldugu. Yapilandirmayla kapatilmis bir kural
/// ozette gorunsun istedim: sessizce kapali duran bir kural, calisip hicbir sey bulmamis
/// gibi gorunur ve bu fark edilmezdi.
/// </summary>
/// <param name="ActiveCodes">Calistirilan kural kodlari, sirali.</param>
/// <param name="DisabledCodes">Yapilandirmayla kapatilmis kural kodlari, sirali.</param>
public sealed record RuleUsage(IReadOnlyList<string> ActiveCodes, IReadOnlyList<string> DisabledCodes)
{
    /// <summary>Kural calistirmayan komutlar icin.</summary>
    public static readonly RuleUsage None = new([], []);
}

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
/// <param name="ExcludedFileCount">
/// --exclude kaliplariyla elenen dosya sayisi. bin, obj, .git ve node_modules klasorlerinin
/// icine zaten hic girilmiyor, o dosyalar bu sayiya dahil degil.
/// </param>
/// <param name="SkippedDirectories">
/// Icine hic girilmeyen klasorler (bin, obj, .git, node_modules), tarama kokune gore goreli.
/// Bunlarin icindeki dosyalar ExcludedFileCount'a girmiyor.
/// </param>
/// <param name="Rules">Hangi kurallarin calistigi ve hangilerinin kapatildigi.</param>
public sealed record CheckSummary(
    int FileCount,
    int FindingCount,
    IReadOnlyList<RuleCodeCount> ByRuleCode,
    int ExcludedFileCount,
    IReadOnlyList<string> SkippedDirectories,
    RuleUsage Rules)
{
    /// <summary>Bulgulari sayip ozeti cikarir. Cikti her calistirmada ayni olsun diye kural kodlari siralanir.</summary>
    public static CheckSummary Of(
        int fileCount,
        IReadOnlyList<Finding> findings,
        int excludedFileCount = 0,
        IReadOnlyList<string>? skippedDirectories = null,
        RuleUsage? rules = null) =>
        new(
            fileCount,
            findings.Count,
            findings
                .GroupBy(finding => finding.RuleCode, StringComparer.Ordinal)
                .OrderBy(group => group.Key, StringComparer.Ordinal)
                .Select(group => new RuleCodeCount(group.Key, group.Count()))
                .ToList(),
            excludedFileCount,
            skippedDirectories ?? [],
            rules ?? RuleUsage.None);
}
