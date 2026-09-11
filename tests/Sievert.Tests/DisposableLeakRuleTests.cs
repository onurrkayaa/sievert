using Sievert.Analysis.Rules;
using Sievert.Core.Rules;

namespace Sievert.Tests;

public class DisposableLeakRuleTests
{
    private static readonly DisposableLeakRule Rule = new();

    [Fact]
    public void AStreamThatIsNeverDisposed_ProducesAFinding()
    {
        Finding finding = Findings().Single(found => found.MethodName == "Save");

        Assert.Equal("SV005", finding.RuleCode);
        Assert.Equal(Severity.Warning, finding.Severity);
        Assert.Equal("Patients/Leak.cs", finding.FilePath);
        Assert.False(string.IsNullOrWhiteSpace(finding.Rationale));
    }

    [Fact]
    public void AStreamReader_ProducesAFinding() =>
        Assert.Contains(Findings(), found => found.MethodName == "Read");

    [Fact]
    public void HttpClient_ProducesAFindingThatMentionsItsLongLife()
    {
        Finding finding = Findings().Single(found => found.MethodName == "Fetch");

        Assert.Contains("uzun omurlu", finding.Description, StringComparison.Ordinal);
    }

    [Fact]
    public void AUsingDeclaration_IsExempt() =>
        AssertExempt("SaveWithUsing", ExemptionReason.UsingScope);

    [Fact]
    public void AUsingStatement_IsExempt() =>
        AssertExempt("ReadWithUsing", ExemptionReason.UsingScope);

    [Fact]
    public void AssignedToAField_IsExempt() =>
        AssertExempt("OpenShared", ExemptionReason.OwnedByType);

    [Fact]
    public void Returned_IsExempt() =>
        AssertExempt("Open", ExemptionReason.CallerOwns);

    [Fact]
    public void ATypeThatIsNotOnTheList_ProducesNoFinding() =>
        Assert.DoesNotContain(Findings(), found => found.MethodName == "Plain");

    [Fact]
    public void AFieldInitialiser_IsExempt() =>
        // _client = new HttpClient() alan baslangic degeri; sinif sahibi.
        Assert.Contains(Run().Exemptions, skipped => skipped.Reason == ExemptionReason.OwnedByType && skipped.Line > 60);

    private static void AssertExempt(string methodName, ExemptionReason reason)
    {
        Assert.DoesNotContain(Findings(), found => found.MethodName == methodName);
        Assert.Equal(reason, Run().Exemptions.Single(skipped => skipped.MethodName == methodName).Reason);
    }

    private static IReadOnlyList<Finding> Findings() => Run().Findings;

    private static RuleResult Run() => RuleTestHelper.InspectSample(Rule, "Leak.cs");

    [Fact]
    public void ATypeThatOnlyEndsWithStream_IsNotAssumedDisposable() =>
        // MediaStream bir veri sinifi. Olcumde SV005'in bes yanlis pozitifinin ucu buydu.
        Assert.Empty(RuleTestHelper.InspectSource(
            Rule,
            "src/Akis.cs",
            """
            public class Akis
            {
                public void Ekle(List<MediaStream> akislar)
                {
                    akislar.Add(new MediaStream());
                }
            }
            """).Findings);

    [Fact]
    public void ATwoStepAwaitUsing_IsExempt()
    {
        // var x = new ...; await using (x...) kalibi. Olcumde iki yanlis pozitif buydu.
        RuleResult result = RuleTestHelper.InspectSource(
            Rule,
            "src/Kopya.cs",
            """
            public class Kopya
            {
                public async Task YazAsync()
                {
                    var memoryStream = new MemoryStream();
                    await using (memoryStream.ConfigureAwait(false))
                    {
                        Kullan(memoryStream);
                    }
                }
            }
            """);

        Assert.Empty(result.Findings);
        Assert.Equal(ExemptionReason.UsingScope, result.Exemptions.Single().Reason);
    }
}
