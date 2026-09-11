using Sievert.Analysis;
using Sievert.Core.Analysis;

using static Sievert.Tests.Samples;

namespace Sievert.Tests;

public class ScanRootTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "sievert-kok-" + Guid.NewGuid().ToString("N"));

    public ScanRootTests()
    {
        Directory.CreateDirectory(Path.Combine(_root, "Ic"));
        File.WriteAllText(Path.Combine(_root, "Bir.cs"), "// bos");
        File.WriteAllText(Path.Combine(_root, "Ic", "Iki.cs"), "// bos");
    }

    public void Dispose()
    {
        Directory.Delete(_root, recursive: true);
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void Directory_IsTheGivenDirectoryItself()
    {
        Assert.Equal(Path.GetFullPath(_root), ScanRoot.Find(_root));
    }

    [Fact]
    public void File_IsTheDirectoryTheFileSitsIn()
    {
        Assert.Equal(Path.GetFullPath(_root), ScanRoot.Find(Path.Combine(_root, "Bir.cs")));
    }

    [Fact]
    public void Root_IsAlwaysAnAbsolutePath()
    {
        Assert.True(Path.IsPathFullyQualified(ScanRoot.Find(".")));
    }

    [Fact]
    public void Paths_AreWrittenRelativeToTheRoot()
    {
        IReadOnlyList<FileAnalysis> relative = ScanRoot.MakePathsRelative(
            [Sample(Path.Combine(_root, "Bir.cs")), Sample(Path.Combine(_root, "Ic", "Iki.cs"))],
            ScanRoot.Find(_root));

        Assert.Equal(["Bir.cs", Path.Combine("Ic", "Iki.cs")], relative.Select(analysis => analysis.FilePath).ToArray());
    }

    [Fact]
    public void Paths_OnlyTheFileNameRemainsForASingleFileScan()
    {
        string path = Path.Combine(_root, "Bir.cs");

        IReadOnlyList<FileAnalysis> relative = ScanRoot.MakePathsRelative([Sample(path)], ScanRoot.Find(path));

        Assert.Equal("Bir.cs", relative.Single().FilePath);
    }

    [Fact]
    public void Paths_OtherFieldsOfTheAnalysisAreLeftAlone()
    {
        FileAnalysis before = Sample(Path.Combine(_root, "Bir.cs"), Type("Bir", Method("Calis")));

        FileAnalysis after = ScanRoot.MakePathsRelative([before], ScanRoot.Find(_root)).Single();

        Assert.Equal(before.Types, after.Types);
        Assert.Equal(before.TotalLineCount, after.TotalLineCount);
    }
}
