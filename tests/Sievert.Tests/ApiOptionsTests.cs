using Sievert.Api;

namespace Sievert.Tests;

/// <summary>
/// Kanit dosyalarinin nerede arandiginin testleri.
///
/// Sebep somut: <c>dotnet run --project src/Sievert.Api</c> icerik kokunu proje klasoru,
/// <c>dotnet ...dll</c> ise bin klasoru yapiyor. Ikisinde de model dosyalari birkac ust
/// klasorde kaliyor ve API ilk istekte patliyordu.
/// </summary>
public sealed class ApiOptionsTests
{
    [Fact]
    public void PathsResolveToTheRepositoryRoot_EvenFromAProjectSubdirectory()
    {
        ApiOptions resolved = new ApiOptions().Resolve(ProjectRoot.Combine("src", "Sievert.Api"));

        Assert.Equal(ProjectRoot.Path, resolved.ArtifactRoot);
        Assert.True(File.Exists(resolved.ModelResultsPath), resolved.ModelResultsPath);
        Assert.True(File.Exists(resolved.ScoreReferencePath), resolved.ScoreReferencePath);
        Assert.True(Directory.Exists(resolved.ModelDirectory), resolved.ModelDirectory);
    }

    [Fact]
    public void PathsResolveToTheRepositoryRoot_EvenFromTheBuildOutput()
    {
        ApiOptions resolved = new ApiOptions().Resolve(AppContext.BaseDirectory);

        Assert.Equal(ProjectRoot.Path, resolved.ArtifactRoot);
        Assert.True(File.Exists(resolved.ModelResultsPath), resolved.ModelResultsPath);
    }

    [Fact]
    public void AnExplicitArtifactRoot_WinsOverTheSearch()
    {
        ApiOptions resolved = new ApiOptions { ArtifactRoot = Path.GetTempPath() }
            .Resolve(ProjectRoot.Path);

        Assert.Equal(Path.GetFullPath(Path.GetTempPath()), resolved.ArtifactRoot);
        Assert.StartsWith(Path.GetFullPath(Path.GetTempPath()), resolved.ModelResultsPath, StringComparison.Ordinal);
    }

    [Fact]
    public void WithNoRepositoryAbove_ItFallsBackToTheContentRoot()
    {
        string directory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(directory);

        try
        {
            // Gecici klasorun ustunde Sievert.slnx yok; arama bos donuyor.
            Assert.Null(ApiOptions.FindRepositoryRoot(directory));

            ApiOptions resolved = new ApiOptions().Resolve(directory);

            Assert.StartsWith(Path.GetFullPath(directory), resolved.ModelResultsPath, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void TheDefaultPageSizeIsTwentyFiveAndTheMaximumIsHundred()
    {
        ApiOptions options = new();

        Assert.Equal(25, options.DefaultPageSize);
        Assert.Equal(100, options.MaximumPageSize);
    }
}
