namespace Sievert.Core.Analysis;

/// <summary>Metot uzunluklarının dağılımı. Ortalama tek başına yanıltıcı olduğu için hepsi bir arada.</summary>
/// <param name="Average">Metot başına ortalama satır sayısı.</param>
/// <param name="Median">Metotların ortadaki değeri. Çift sayıda metotta iki ortancanın ortalaması.</param>
/// <param name="P90">Metotların yüzde 90'ı bu satır sayısının altında.</param>
/// <param name="P95">Metotların yüzde 95'i bu satır sayısının altında.</param>
/// <param name="Longest">En uzun metodun satır sayısı.</param>
public sealed record MethodLength(double Average, double Median, int P90, int P95, int Longest);

/// <summary>Kapalı <c>#if</c> dalları yüzünden hiç bakamadığımız kod.</summary>
/// <param name="FileCount">Koşullu derleme içeren dosya sayısı.</param>
/// <param name="LineCount">Kapalı dallarda kalan toplam satır sayısı.</param>
/// <param name="Ratio">Bu satırların taranan toplam satıra oranı, 0 ile 1 arası.</param>
public sealed record BlindSpot(int FileCount, int LineCount, double Ratio);

/// <summary>Üretim ve test kodunun ayrı sayıları.</summary>
/// <param name="ProductionFileCount">Test gibi görünmeyen dosya sayısı.</param>
/// <param name="TestFileCount">Test gibi görünen dosya sayısı.</param>
/// <param name="ProductionMethodCount">Üretim dosyalarındaki metot sayısı.</param>
/// <param name="TestMethodCount">Test dosyalarındaki metot sayısı.</param>
public sealed record CodeSplit(int ProductionFileCount, int TestFileCount, int ProductionMethodCount, int TestMethodCount);

/// <summary>Bir tarama sonundaki toplu sayilar.</summary>
/// <param name="FileCount">Taranan dosya sayisi.</param>
/// <param name="TypeCount">Bulunan tip sayisi.</param>
/// <param name="MethodCount">Bulunan metot sayisi.</param>
/// <param name="AsyncMethodCount">Bunlarin kaci async.</param>
/// <param name="AsyncRatio">Async metotlarin tum metotlara orani, 0 ile 1 arasi.</param>
/// <param name="MethodLength">Metot uzunluklarinin dagilimi.</param>
/// <param name="FilesWithParseErrors">En az bir sozdizimi hatasi olan dosya sayisi.</param>
/// <param name="FilesWithoutTypes">Okundu ama icinde hic tip cikmayan dosya sayisi.</param>
/// <param name="BlindSpot">Kapali #if dallarinda kalan kod.</param>
/// <param name="CodeSplit">Uretim ve test kodunun ayri sayilari.</param>
/// <param name="ExcludedFileCount">
/// --exclude kaliplariyla elenen dosya sayisi. bin, obj, .git ve node_modules klasorlerinin
/// icine zaten hic girilmiyor, o dosyalar bu sayiya dahil degil.
/// </param>
/// <param name="SkippedDirectories">
/// Icine hic girilmeyen klasorler (bin, obj, .git, node_modules), tarama kokune gore goreli.
/// Bunlarin icindeki dosyalar ExcludedFileCount'a girmiyor; saymak icin klasoru gezmek
/// gerekirdi. Yollari raporlamak bedelsiz ve "hicbir sey sessizce atlanmiyor" demeye yetiyor.
/// </param>
public sealed record ScanSummary(
    int FileCount,
    int TypeCount,
    int MethodCount,
    int AsyncMethodCount,
    double AsyncRatio,
    MethodLength MethodLength,
    int FilesWithParseErrors,
    int FilesWithoutTypes,
    BlindSpot BlindSpot,
    CodeSplit CodeSplit,
    int ExcludedFileCount,
    IReadOnlyList<string> SkippedDirectories);

/// <summary>Bir metodun hangi dosyada ve hangi tipin icinde oldugu.</summary>
public sealed record MethodLocation(string FilePath, string TypeName, SievertMethod Method);
