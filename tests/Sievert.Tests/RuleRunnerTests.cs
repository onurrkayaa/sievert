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

        Assert.Equal(
            ["OnDropped", "OnProgress", "Refresh", "Save", "Tick"],
            findings.Select(finding => finding.MethodName).Order(StringComparer.Ordinal).ToArray());
    }

    [Fact]
    public void EmptyRuleList_ProducesNoFindings()
    {
        Assert.Empty(Run());
    }

    private static IReadOnlyList<Finding> Run(params IRule[] rules) =>
        new RuleRunner(rules).Run(SourceFileFinder.Find(SampleDirectory), AppContext.BaseDirectory).Findings;
}
