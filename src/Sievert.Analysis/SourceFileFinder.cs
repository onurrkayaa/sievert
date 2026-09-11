namespace Sievert.Analysis;

/// <summary>Verilen yolun altindaki taranacak .cs dosyalarini bulur.</summary>
public static class SourceFileFinder
{
    /// <summary>Icine hic girilmeyen klasorler.</summary>
    private static readonly string[] SkippedDirectories = ["bin", "obj", ".git", "node_modules"];

    /// <summary>
    /// Yol bir dosyaysa (ve .cs ise) onu, klasorse altindaki butun .cs dosyalarini dondurur.
    /// Sonuc her calistirmada ayni sirada gelsin diye siralanir.
    /// </summary>
    public static IReadOnlyList<string> Find(string path)
    {
        if (File.Exists(path))
        {
            return IsCSharpFile(path) ? [path] : [];
        }

        if (!Directory.Exists(path))
        {
            return [];
        }

        List<string> found = [];
        WalkDirectory(path, found);
        found.Sort(StringComparer.Ordinal);
        return found;
    }

    /// <summary>Klasor adi atlanacaklar listesinde mi.</summary>
    public static bool IsSkippedDirectory(string directoryName) =>
        SkippedDirectories.Contains(directoryName, StringComparer.OrdinalIgnoreCase);

    private static void WalkDirectory(string directory, List<string> found)
    {
        found.AddRange(Directory.EnumerateFiles(directory, "*.cs").Where(IsCSharpFile));

        foreach (string subDirectory in Directory.EnumerateDirectories(directory))
        {
            if (!IsSkippedDirectory(Path.GetFileName(subDirectory)))
            {
                WalkDirectory(subDirectory, found);
            }
        }
    }

    private static bool IsCSharpFile(string path) =>
        Path.GetExtension(path).Equals(".cs", StringComparison.OrdinalIgnoreCase);
}
