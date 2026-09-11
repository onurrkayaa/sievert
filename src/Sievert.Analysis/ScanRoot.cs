using Sievert.Core.Analysis;

namespace Sievert.Analysis;

/// <summary>
/// Taramanin kok klasoru. Cikti nereye giderse gitsin dosya yollari hep bu koke gore
/// goreli yazilsin diye burada tek yerden hesaplaniyor.
/// </summary>
public static class ScanRoot
{
    /// <summary>Klasor verildiyse klasorun kendisi, dosya verildiyse dosyanin bulundugu klasor.</summary>
    public static string Find(string path)
    {
        if (Directory.Exists(path))
        {
            return Path.GetFullPath(path);
        }

        string directory = Path.GetDirectoryName(Path.GetFullPath(path)) ?? Path.GetFullPath(".");
        return directory.Length == 0 ? Path.GetFullPath(".") : directory;
    }

    /// <summary>Analizlerdeki dosya yollarini koke gore goreli hale getirir.</summary>
    public static IReadOnlyList<FileAnalysis> MakePathsRelative(IReadOnlyList<FileAnalysis> analyses, string root) =>
        analyses
            .Select(analysis => analysis with { FilePath = Relative(analysis.FilePath, root) })
            .ToList();

    private static string Relative(string filePath, string root) =>
        Path.GetRelativePath(root, Path.GetFullPath(filePath));
}
