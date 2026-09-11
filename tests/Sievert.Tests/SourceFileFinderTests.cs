using Sievert.Analysis;

namespace Sievert.Tests;

public class SourceFileFinderTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "sievert-test-" + Guid.NewGuid().ToString("N"));

    public SourceFileFinderTests()
    {
        CreateFile("Bir.cs");
        CreateFile("Okuma.txt");
        CreateFile(Path.Combine("Ic", "Iki.cs"));
        CreateFile(Path.Combine("Ic", "Daha", "Uc.cs"));
        CreateFile(Path.Combine("bin", "Derlenmis.cs"));
        CreateFile(Path.Combine("obj", "Ara.cs"));
        CreateFile(Path.Combine(".git", "Kanca.cs"));
        CreateFile(Path.Combine("node_modules", "paket", "Sasirtici.cs"));
    }

    public void Dispose()
    {
        Directory.Delete(_root, recursive: true);
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void Directory_SubDirectoriesAreScannedToo()
    {
        string[] found = Names(SourceFileFinder.Find(_root));

        Assert.Equal(["Bir.cs", "Iki.cs", "Uc.cs"], found.Order(StringComparer.Ordinal).ToArray());
    }

    [Theory]
    [InlineData("bin")]
    [InlineData("obj")]
    [InlineData(".git")]
    [InlineData("node_modules")]
    public void Directory_SkippedDirectoriesAreNotEntered(string directoryName)
    {
        Assert.True(SourceFileFinder.IsSkippedDirectory(directoryName));
        Assert.DoesNotContain(SourceFileFinder.Find(_root), path => path.Contains(directoryName + Path.DirectorySeparatorChar, StringComparison.Ordinal));
    }

    [Fact]
    public void Directory_NonCSharpFilesAreLeftOut()
    {
        Assert.DoesNotContain(SourceFileFinder.Find(_root), path => path.EndsWith(".txt", StringComparison.Ordinal));
    }

    [Fact]
    public void File_GivenOnItsOwn_IsReturned()
    {
        string path = Path.Combine(_root, "Bir.cs");

        Assert.Equal([path], SourceFileFinder.Find(path));
    }

    [Fact]
    public void File_NotCSharp_ReturnsEmpty()
    {
        Assert.Empty(SourceFileFinder.Find(Path.Combine(_root, "Okuma.txt")));
    }

    [Fact]
    public void MissingPath_ReturnsEmpty()
    {
        Assert.Empty(SourceFileFinder.Find(Path.Combine(_root, "yok", "hicbir.cs")));
    }

    [Fact]
    public void Result_IsAlwaysInTheSameOrder()
    {
        Assert.Equal(SourceFileFinder.Find(_root), SourceFileFinder.Find(_root));
    }

    private void CreateFile(string relativePath)
    {
        string fullPath = Path.Combine(_root, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllText(fullPath, "// bos");
    }

    private static string[] Names(IReadOnlyList<string> paths) => paths.Select(Path.GetFileName).ToArray()!;
}
