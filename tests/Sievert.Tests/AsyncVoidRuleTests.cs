using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using Sievert.Analysis.Rules;
using Sievert.Core.Rules;

namespace Sievert.Tests;

public class AsyncVoidRuleTests
{
    private static readonly AsyncVoidRule Rule = new();

    [Fact]
    public void PlainAsyncVoid_ProducesAFinding()
    {
        Finding finding = Inspect(SampleFile()).Single(found => found.MethodName == "Save");

        Assert.Equal("SV001", finding.RuleCode);
        Assert.Equal(Severity.Error, finding.Severity);
        Assert.Equal("Patients/AsyncVoid.cs", finding.FilePath);
    }

    [Fact]
    public void EventHandler_IsLeftAlone()
    {
        // Ikinci parametresinin tip adi EventArgs ile bitiyor, bulgu uretmiyoruz.
        Assert.DoesNotContain(Inspect(SampleFile()), found => found.MethodName == "OnSaved");
    }

    [Fact]
    public void AsyncTask_ProducesNoFinding()
    {
        Assert.DoesNotContain(Inspect(SampleFile()), found => found.MethodName == "SaveAsync");
    }

    [Fact]
    public void AsyncVoidWithoutParameters_IsAlsoFound()
    {
        // Parametresi olmadigi icin event handler istisnasina giremez.
        Finding finding = Inspect(SampleFile()).Single(found => found.MethodName == "Tick");

        Assert.Equal(28, finding.Line);
    }

    private static IReadOnlyList<Finding> Inspect(string filePath) =>
        Rule.InspectFile(
            CSharpSyntaxTree.ParseText(File.ReadAllText(filePath), path: filePath),
            "Patients/AsyncVoid.cs");

    private static string SampleFile() =>
        Path.Combine(AppContext.BaseDirectory, "Patients", "AsyncVoid.cs");
}
