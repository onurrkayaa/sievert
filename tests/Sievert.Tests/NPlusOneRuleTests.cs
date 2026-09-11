using Sievert.Analysis.Rules;
using Sievert.Core.Rules;

namespace Sievert.Tests;

public class NPlusOneRuleTests
{
    private static readonly NPlusOneRule Rule = new();

    [Fact]
    public void AQueryInsideAForeach_ProducesAFinding()
    {
        Finding finding = Findings().Single(found => found.MethodName == "PerPatient");

        Assert.Equal("SV004", finding.RuleCode);
        Assert.Equal(Severity.Warning, finding.Severity);
        Assert.Equal("Patients/NPlusOne.cs", finding.FilePath);
        Assert.False(string.IsNullOrWhiteSpace(finding.Rationale));
    }

    [Fact]
    public void AQueryInsideAWhile_ProducesAFinding() =>
        Assert.Contains(Findings(), found => found.MethodName == "ScanQueue");

    [Fact]
    public void AQueryInsideAFor_ProducesAFinding() =>
        Assert.Contains(Findings(), found => found.MethodName == "CountEach");

    [Fact]
    public void AQueryOutsideTheLoop_ProducesNoFinding() =>
        Assert.DoesNotContain(Findings(), found => found.MethodName == "Prefetched");

    [Fact]
    public void AQueryInTheLoopSource_ProducesNoFinding() =>
        // foreach (var x in db.Visits.ToList()) zaten dogru yazim: tek sorgu.
        Assert.DoesNotContain(Findings(), found => found.MethodName == "IterateQueryOnce");

    [Fact]
    public void AnInMemoryListInsideALoop_IsAKnownFalsePositive() =>
        // Ad temelli tahminin bedeli. Bilerek boyle; sinirliliklar.md'de yaziyor.
        Assert.Contains(Findings(), found => found.MethodName == "InMemoryFalsePositive");

    [Fact]
    public void WhenTheReceiverLooksLikeADatabase_TheMessageSaysSo()
    {
        // Ipucu mesaja giriyor ama filtre olarak kullanilmiyor.
        Assert.Contains("_db", Findings().Single(found => found.MethodName == "PerPatient").Description, StringComparison.Ordinal);
        Assert.Contains("veritabani", Findings().Single(found => found.MethodName == "PerPatient").Description, StringComparison.Ordinal);
    }

    [Fact]
    public void WhenTheReceiverDoesNotLookLikeADatabase_TheMessageDoesNotClaimIt() =>
        Assert.DoesNotContain(
            "veritabani",
            Findings().Single(found => found.MethodName == "InMemoryFalsePositive").Description,
            StringComparison.Ordinal);

    private static IReadOnlyList<Finding> Findings() => Run().Findings;

    private static RuleResult Run() => RuleTestHelper.InspectSample(Rule, "NPlusOne.cs");
}
