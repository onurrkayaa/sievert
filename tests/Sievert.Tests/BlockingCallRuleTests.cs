using Sievert.Analysis;
using Sievert.Analysis.Rules;
using Sievert.Core.Rules;

namespace Sievert.Tests;

public class BlockingCallRuleTests
{
    private static readonly BlockingCallRule Rule = new();

    [Fact]
    public void Result_OnAnAsyncCall_ProducesAFinding()
    {
        Finding finding = Findings().Single(found => found.MethodName == "ReadCount");

        Assert.Equal("SV002", finding.RuleCode);
        Assert.Equal(Severity.Warning, finding.Severity);
        Assert.Equal("Patients/Blocking.cs", finding.FilePath);
        Assert.False(string.IsNullOrWhiteSpace(finding.Rationale));
    }

    [Fact]
    public void Wait_ProducesAFinding() =>
        Assert.Contains(Findings(), found => found.MethodName == "Flush");

    [Fact]
    public void GetAwaiterGetResult_ProducesAFinding() =>
        Assert.Contains(Findings(), found => found.MethodName == "ReadName");

    [Fact]
    public void AFieldThatLooksLikeATask_ProducesAFinding() =>
        Assert.Contains(Findings(), found => found.MethodName == "WaitForPending");

    [Fact]
    public void Main_IsExempt()
    {
        // Konsol girisinde bloklamak mesru olabiliyor.
        Assert.DoesNotContain(Findings(), found => found.MethodName == "Main");
        Assert.Equal(
            ExemptionReason.EntryPoint,
            Run().Exemptions.Single(skipped => skipped.MethodName == "Main").Reason);
    }

    [Fact]
    public void ResultOnSomethingThatIsNotTaskLike_IsLeftAlone() =>
        Assert.DoesNotContain(Findings(), found => found.MethodName == "ReadLookupResult");

    [Fact]
    public void AwaitedCode_ProducesNoFinding() =>
        Assert.DoesNotContain(Findings(), found => found.MethodName == "ReadCountAsync");

    [Fact]
    public void ProductionCode_IsNotMarkedAsTestCode() =>
        Assert.All(Findings(), finding => Assert.False(finding.IsTestCode));

    [Fact]
    public void InTestCode_TheFindingIsMarkedButStillReported()
    {
        // Muafiyet yok: test kodunda da bulgu cikiyor, sadece isaretleniyor.
        Finding finding = RuleTestHelper.InspectSource(
            Rule,
            "tests/SchedulerTests.cs",
            "public class SchedulerTests { public void Check() { LoadAsync().Wait(); } }")
            .Findings
            .Single();

        Assert.True(finding.IsTestCode);
    }

    private static IReadOnlyList<Finding> Findings() => Run().Findings;

    private static RuleResult Run() => RuleTestHelper.InspectSample(Rule, "Blocking.cs");
}
