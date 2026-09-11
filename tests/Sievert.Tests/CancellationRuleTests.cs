using Sievert.Analysis.Rules;
using Sievert.Core.Rules;

namespace Sievert.Tests;

public class CancellationRuleTests
{
    private static readonly CancellationRule Rule = new();

    [Fact]
    public void APublicTaskMethodWithoutAToken_ProducesAnInfoFinding()
    {
        Finding finding = Findings().Single(found => found.MethodName == "SaveAsync" && found.Line < 30);

        Assert.Equal("SV006", finding.RuleCode);
        Assert.Equal(Severity.Info, finding.Severity);
        Assert.Equal("Patients/Cancellation.cs", finding.FilePath);
        Assert.False(string.IsNullOrWhiteSpace(finding.Rationale));
    }

    [Fact]
    public void ValueTask_CountsToo() =>
        Assert.Contains(Findings(), found => found.MethodName == "CountAsync");

    [Fact]
    public void AMethodThatAlreadyTakesAToken_ProducesNoFinding() =>
        Assert.DoesNotContain(Findings(), found => found.MethodName == "DeleteAsync");

    [Fact]
    public void APrivateMethod_IsExempt()
    {
        Assert.DoesNotContain(Findings(), found => found.MethodName == "CacheAsync");
        Assert.Equal(
            ExemptionReason.NotPublic,
            Run().Exemptions.Single(skipped => skipped.MethodName == "CacheAsync").Reason);
    }

    [Fact]
    public void AnExplicitInterfaceImplementation_IsExempt() =>
        Assert.Equal(
            ExemptionReason.InheritedSignature,
            Run().Exemptions.Single(skipped => skipped.MethodName == "LoadAsync").Reason);

    [Fact]
    public void AnOverride_IsExempt() =>
        Assert.Contains(
            Run().Exemptions,
            skipped => skipped.MethodName == "SaveAsync" && skipped.Reason == ExemptionReason.InheritedSignature);

    [Fact]
    public void AMethodThatDoesNotReturnATask_IsNotACandidate()
    {
        Assert.DoesNotContain(Findings(), found => found.MethodName == "Describe");
        Assert.DoesNotContain(Run().Exemptions, skipped => skipped.MethodName == "Describe");
    }

    [Fact]
    public void InTestCode_TheMethodIsExempt()
    {
        RuleResult result = RuleTestHelper.InspectSource(
            Rule,
            "tests/ServiceTests.cs",
            "public class ServiceTests { public Task RunAsync() { return Task.CompletedTask; } }");

        Assert.Empty(result.Findings);
        Assert.Equal(ExemptionReason.TestCode, result.Exemptions.Single().Reason);
    }

    private static IReadOnlyList<Finding> Findings() => Run().Findings;

    private static RuleResult Run() => RuleTestHelper.InspectSample(Rule, "Cancellation.cs");
}
