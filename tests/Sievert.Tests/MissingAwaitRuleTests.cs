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

    [Fact]
    public void TwoCallsInOneLambda_AreTwoRecordsOnDifferentLines()
    {
        // Muafiyet kaydinda metot adi cevreleyen metot olarak kaliyor; iki kaydi
        // birbirinden ayiran sey satir numarasi.
        IReadOnlyList<Exemption> exemptions = RuleTestHelper.InspectSource(
            Rule,
            "src/Arkaplan.cs",
            """
            public class Arkaplan
            {
                public void Basla()
                {
                    Task.Run(() =>
                    {
                        BirAsync();
                        IkiAsync();
                    });
                }
            }
            """)
            .Exemptions;

        Assert.Equal(2, exemptions.Count);
        Assert.All(exemptions, exemption => Assert.Equal("Basla", exemption.MethodName));
        Assert.Equal(2, exemptions.Select(exemption => exemption.Line).Distinct().Count());
    }

    [Fact]
    public void AFluentSetupCallBehindALambda_IsExempt()
    {
        // Olcumde SV003'un bes yanlis pozitifinin besi de bu bicimdeydi: isaretlenen cagri
        // lambdanin icindeki degil, zincirin sonundaki ReturnsAsync.
        RuleResult result = RuleTestHelper.InspectSource(
            Rule,
            "tests/Kurulum.cs",
            """
            public class Kurulum
            {
                public void Hazirla()
                {
                    factory.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>())).ReturnsAsync(context);
                }
            }
            """);

        Assert.Empty(result.Findings);
        Assert.Equal(ExemptionReason.ExpressionTree, result.Exemptions.Single().Reason);
    }

    [Fact]
    public void ALambdaInTheCallsOwnArgument_IsStillAFinding() =>
        // Muafiyet yalnizca zincirin alicisina bakiyor. Cagrinin kendi argumanindaki lambda
        // onu beklenmemis bir gorev olmaktan cikarmaz.
        Assert.Single(RuleTestHelper.InspectSource(
            Rule,
            "src/Toplu.cs",
            """
            public class Toplu
            {
                public void Basla()
                {
                    IsleAsync(kayitlar.Select(kayit => kayit.Id));
                }
            }
            """).Findings);
}
