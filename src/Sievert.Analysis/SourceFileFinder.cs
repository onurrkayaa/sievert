namespace Sievert.Analysis;

/// <summary>Bir aramanin sonucu: bulunan dosyalar ve hic girilmeyen klasorler.</summary>
/// <param name="Files">Taranacak .cs dosyalari, sirali.</param>
/// <param name="SkippedDirectories">
/// Icine hic girilmeyen klasorlerin tam yollari. Bunlarin icindeki .cs dosyalari
/// sayilmiyor: saymak icin klasoru gezmek gerekirdi ve node_modules gibi bir yerde
/// bu pahali. Onun yerine hangi klasorlerin atlandigini raporluyoruz, boylece hicbir
/// sey sessizce atlanmamis oluyor.
/// </param>
public sealed record SourceFileSearch(IReadOnlyList<string> Files, IReadOnlyList<string> SkippedDirectories);

/// <summary>Verilen yolun altindaki taranacak .cs dosyalarini bulur.</summary>
public static class SourceFileFinder
{
    /// <summary>Icine hic girilmeyen klasorler.</summary>
    private static readonly string[] SkippedDirectories = ["bin", "obj", ".git", "node_modules"];

    /// <summary>
    /// Yol bir dosyaysa (ve .cs ise) onu, klasorse altindaki butun .cs dosyalarini dondurur.
    /// Sonuc her calistirmada ayni sirada gelsin diye siralanir.
    /// </summary>
    public static IReadOnlyList<string> Find(string path) => Search(path).Files;

    /// <summary>Dosyalarin yani sira hangi klasorlere hic girilmedigini de dondurur.</summary>
    public static SourceFileSearch Search(string path)
    {
        if (File.Exists(path))
        {
            return new SourceFileSearch(IsCSharpFile(path) ? [path] : [], []);
        }

        if (!Directory.Exists(path))
        {
            return new SourceFileSearch([], []);
        }

        List<string> found = [];
        List<string> skipped = [];
        WalkDirectory(path, found, skipped);
        found.Sort(StringComparer.Ordinal);
        skipped.Sort(StringComparer.Ordinal);
        return new SourceFileSearch(found, skipped);
    }

    /// <summary>Klasor adi atlanacaklar listesinde mi.</summary>
    public static bool IsSkippedDirectory(string directoryName) =>
        SkippedDirectories.Contains(directoryName, StringComparer.OrdinalIgnoreCase);

    private static void WalkDirectory(string directory, List<string> found, List<string> skipped)
    {
        found.AddRange(Directory.EnumerateFiles(directory, "*.cs").Where(IsCSharpFile));

        foreach (string subDirectory in Directory.EnumerateDirectories(directory))
        {
            if (IsSkippedDirectory(Path.GetFileName(subDirectory)))
            {
                skipped.Add(subDirectory);
            }
            else
            {
                WalkDirectory(subDirectory, found, skipped);
            }
        }
    }

    private static bool IsCSharpFile(string path) =>
        Path.GetExtension(path).Equals(".cs", StringComparison.OrdinalIgnoreCase);
}
