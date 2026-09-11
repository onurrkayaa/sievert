using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Sievert.Analysis.Rules;

/// <summary>
/// Taranacak dosyalari bir kez okuyup ayristirir ve her biri icin kural baglamini kurar.
/// RuleRunner bunu kullaniyor; testler de ayni yoldan gecsin diye disari acik.
/// </summary>
public static class ScannedFileSet
{
    /// <summary>Her dosyayi bir kez okuyup ayristirir. Agaca diskteki tam yol da yaziliyor.</summary>
    public static IReadOnlyList<ScannedFile> Parse(IReadOnlyList<string> filePaths, string scanRoot) =>
        filePaths
            .Select(filePath => new ScannedFile(
                Path.GetFullPath(filePath),
                ScanRoot.RelativePath(filePath, scanRoot),
                CSharpSyntaxTree.ParseText(File.ReadAllText(filePath), path: filePath)))
            .ToList();

    /// <summary>
    /// Her dosya icin baglami kurar. Klasor gruplamasi bir kez yapiliyor, her kural icin
    /// tekrar hesaplanmiyor.
    /// </summary>
    public static IReadOnlyList<RuleContext> Contexts(IReadOnlyList<ScannedFile> files)
    {
        Dictionary<string, List<ScannedFile>> byFolder = new(StringComparer.Ordinal);

        foreach (ScannedFile file in files)
        {
            string folder = Path.GetDirectoryName(file.AbsolutePath) ?? string.Empty;

            if (!byFolder.TryGetValue(folder, out List<ScannedFile>? group))
            {
                group = [];
                byFolder[folder] = group;
            }

            group.Add(file);
        }

        return files
            .Select(file => new RuleContext(
                file,
                byFolder[Path.GetDirectoryName(file.AbsolutePath) ?? string.Empty]
                    .Where(other => !ReferenceEquals(other, file))
                    .ToList()))
            .ToList();
    }
}
