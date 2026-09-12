namespace Sievert.Api;

/// <summary>
/// API ayarlari. Baglanti dizesi burada YOK: o yalnizca <c>SIEVERT_DB</c> ortam
/// degiskeninden okunuyor (ADR 0012) ve hicbir ayar dosyasina yazilmiyor.
/// </summary>
public sealed class ApiOptions
{
    public const string Section = "Sievert";

    /// <summary>
    /// Model artefaktlarinin bulundugu klasor. Icerik koku uzerinden cozuluyor;
    /// calisma dizinine guvenilmiyor, cunku API baska bir klasorden baslatilabilir.
    /// </summary>
    public string ModelDirectory { get; set; } = Path.Combine("data", "asama5", "models");

    /// <summary>Model metadata'sinin okundugu dondurulmus kanit dosyasi.</summary>
    public string ModelResultsPath { get; set; } = Path.Combine("data", "asama5", "model-results.json");

    /// <summary>RiskIndex icin egitim skor referansi.</summary>
    public string ScoreReferencePath { get; set; } = Path.Combine("data", "asama6", "model-score-reference.json");

    /// <summary>Sayfalama varsayilani ve ust siniri.</summary>
    public int DefaultPageSize { get; set; } = 25;

    public int MaximumPageSize { get; set; } = 100;

    /// <summary>
    /// Kanit dosyalarinin arandigi kok. Bos birakilirsa icerik kokunden yukari dogru
    /// <c>Sievert.slnx</c> aranir.
    /// </summary>
    public string? ArtifactRoot { get; set; }

    /// <summary>
    /// Goreli yollari kanit kokune gore cozer.
    ///
    /// Neden icerik koku tek basina yetmiyor: model dosyalari repo kokunde duruyor ama
    /// icerik koku nasil baslatildigina gore degisiyor. <c>dotnet run --project
    /// src/Sievert.Api</c> icerik kokunu proje klasoru yapiyor, <c>dotnet ...dll</c> ise
    /// bin klasoru. Ikisinde de repo koku birkac ust klasorde. Bunu elle vermek zorunda
    /// birakmak yerine <c>Sievert.slnx</c> aranarak bulunuyor; bulunamazsa icerik kokune
    /// donuluyor ve acilista acik hata veriliyor.
    /// </summary>
    public ApiOptions Resolve(string contentRoot)
    {
        string root = ArtifactRoot is { Length: > 0 } given
            ? Path.GetFullPath(Path.Combine(contentRoot, given))
            : FindRepositoryRoot(contentRoot) ?? contentRoot;

        return new ApiOptions
        {
            ArtifactRoot = root,
            ModelDirectory = Absolute(root, ModelDirectory),
            ModelResultsPath = Absolute(root, ModelResultsPath),
            ScoreReferencePath = Absolute(root, ScoreReferencePath),
            DefaultPageSize = DefaultPageSize,
            MaximumPageSize = MaximumPageSize,
        };
    }

    /// <summary>Cozum dosyasi bulunana kadar yukari cikar. Bulamazsa null.</summary>
    public static string? FindRepositoryRoot(string start)
    {
        DirectoryInfo? directory = new(Path.GetFullPath(start));

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Sievert.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        return null;
    }

    private static string Absolute(string root, string path) =>
        Path.IsPathRooted(path) ? path : Path.GetFullPath(Path.Combine(root, path));
}
