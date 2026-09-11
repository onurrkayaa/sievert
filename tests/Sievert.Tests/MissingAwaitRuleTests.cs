using Sievert.Analysis.Rules;
using Sievert.Core.Rules;

namespace Sievert.Tests;

public class MissingAwaitRuleTests
{
    private static readonly MissingAwaitRule Rule = new();

    [Fact]
    public void ADroppedAsyncCall_ProducesAFinding()
    {
        Finding finding = Findings().Single(found => found.MethodName == "Send");

        Assert.Equal("SV003", finding.RuleCode);
        Assert.Equal(Severity.Warning, finding.Severity);
        Assert.Equal("Patients/MissingAwait.cs", finding.FilePath);
        Assert.False(string.IsNullOrWhiteSpace(finding.Rationale));
    }

    [Fact]
    public void AConfigureAwaitChainThatIsNotAwaited_IsStillADroppedTask() =>
        // ConfigureAwait bir ConfiguredTaskAwaitable donuyor, o da beklenmiyor.
        Assert.Contains(Findings(), found => found.MethodName == "SendConfigured");

    [Fact]
    public void ADiscardedCall_IsExempt()
    {
        Assert.DoesNotContain(Findings(), found => found.MethodName == "SendAndForget");
        Assert.Equal(
            ExemptionReason.DiscardedResult,
            Run().Exemptions.Single(skipped => skipped.MethodName == "SendAndForget").Reason);
    }

    [Fact]
    public void ACallInsideTaskRun_IsExempt()
    {
        Assert.DoesNotContain(Findings(), found => found.MethodName == "SendInBackground");
        Assert.Equal(
            ExemptionReason.FireAndForget,
            Run().Exemptions.Single(skipped => skipped.MethodName == "SendInBackground").Reason);
    }

    [Fact]
    public void AnAwaitedCall_ProducesNoFinding() =>
        Assert.DoesNotContain(Findings(), found => found.MethodName == "SendProperlyAsync");

    [Fact]
    public void ACallAssignedToAVariable_ProducesNoFinding() =>
        Assert.DoesNotContain(Findings(), found => found.MethodName == "SendAndReturn");

    [Fact]
    public void ACallThatDoesNotEndInAsync_ProducesNoFinding() =>
        Assert.DoesNotContain(Findings(), found => found.MethodName == "Log");

    private static IReadOnlyList<Finding> Findings() => Run().Findings;

    private static RuleResult Run() => RuleTestHelper.InspectSample(Rule, "MissingAwait.cs");
}
