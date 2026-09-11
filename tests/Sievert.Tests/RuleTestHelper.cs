using Microsoft.CodeAnalysis.CSharp;

using Sievert.Analysis;
using Sievert.Analysis.Rules;

namespace Sievert.Tests;

/// <summary>Kurallari ornek dosyalar ya da duz kaynak metni uzerinde calistirmak icin.</summary>
internal static class RuleTestHelper
{
    /// <summary>Ornek klasorunu RuleRunner'in yaptigi gibi ayristirip bir dosyanin baglamini verir.</summary>
    public static RuleResult InspectSample(IRule rule, string fileName) =>
        rule.Inspect(SampleContext(fileName));

    public static RuleContext SampleContext(string fileName)
    {
        string directory = Path.Combine(AppContext.BaseDirectory, "Patients");

        return ScannedFileSet
            .Contexts(ScannedFileSet.Parse(SourceFileFinder.Find(directory), AppContext.BaseDirectory))
            .Single(context => context.File.AbsolutePath == Path.Combine(directory, fileName));
    }

    /// <summary>Diskte olmayan bir kaynak icin baglam uretir.</summary>
    public static RuleContext Context(string relativePath, string source) =>
        new(
            new ScannedFile(Path.GetFullPath(relativePath), relativePath, CSharpSyntaxTree.ParseText(source)),
            []);

    /// <summary>Diskte olmayan bir kaynagi verilen goreli yolla inceler.</summary>
    public static RuleResult InspectSource(IRule rule, string relativePath, string source) =>
        rule.Inspect(new RuleContext(
            new ScannedFile(Path.GetFullPath(relativePath), relativePath, CSharpSyntaxTree.ParseText(source)),
            []));
}
