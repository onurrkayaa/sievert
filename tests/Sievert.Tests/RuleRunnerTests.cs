using Sievert.Analysis;
using Sievert.Analysis.Rules;
using Sievert.Core.Rules;

namespace Sievert.Tests;

public class RuleRunnerTests
{
    private static readonly string SampleDirectory = Path.Combine(AppContext.BaseDirectory, "Patients");

    [Fact]
    public void Runner_CollectsFindingsFromEveryFile()
    {
        IReadOnlyList<Finding> findings = Run(new AsyncVoidRule());

        Assert.Equal(["Save", "Tick"], findings.Select(finding => finding.MethodName).Order(StringComparer.Ordinal).ToArray());
        Assert.All(findings, finding => Assert.Equal(Path.Combine("Patients", "AsyncVoid.cs"), finding.FilePath));
    }

    [Fact]
    public void EmptyRuleList_ProducesNoFindings()
    {
        Assert.Empty(Run());
    }

    private static IReadOnlyList<Finding> Run(params IRule[] rules) =>
        new RuleRunner(rules).Run(SourceFileFinder.Find(SampleDirectory), AppContext.BaseDirectory);
}
