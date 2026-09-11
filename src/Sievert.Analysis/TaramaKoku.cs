using Sievert.Core.Cozumleme;

namespace Sievert.Analysis;

/// <summary>
/// Taramanin kok klasoru. Cikti nereye giderse gitsin dosya yollari hep bu koke gore
/// goreli yazilsin diye burada tek yerden hesaplaniyor.
/// </summary>
public static class TaramaKoku
{
    /// <summary>Klasor verildiyse klasorun kendisi, dosya verildiyse dosyanin bulundugu klasor.</summary>
    public static string Bul(string yol)
    {
        if (Directory.Exists(yol))
        {
            return Path.GetFullPath(yol);
        }

        string klasor = Path.GetDirectoryName(Path.GetFullPath(yol)) ?? Path.GetFullPath(".");
        return klasor.Length == 0 ? Path.GetFullPath(".") : klasor;
    }

    /// <summary>Analizlerdeki dosya yollarini koke gore goreli hale getirir.</summary>
    public static IReadOnlyList<DosyaAnalizi> YollariGoreliles(IReadOnlyList<DosyaAnalizi> analizler, string kok) =>
        analizler
            .Select(analiz => analiz with { DosyaYolu = Goreli(analiz.DosyaYolu, kok) })
            .ToList();

    private static string Goreli(string dosyaYolu, string kok) =>
        Path.GetRelativePath(kok, Path.GetFullPath(dosyaYolu));
}
