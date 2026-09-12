namespace Sievert.Tests;

/// <summary>
/// Testler dondurulmus kanit dosyalarini okuyor; bunlar repo kokune gore duruyor.
/// Calisma dizini kosucuya gore degistigi icin kok, cozum dosyasi bulunana kadar
/// yukari cikilarak bulunuyor.
/// </summary>
public static class ProjectRoot
{
    public static string Path { get; } = Find();

    public static string Combine(params string[] parts) =>
        System.IO.Path.Combine([Path, .. parts]);

    private static string Find()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(System.IO.Path.Combine(directory.FullName, "Sievert.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Sievert.slnx bulunamadi; repo koku cozulemedi.");
    }
}
