using Sievert.Analysis.Rules;
using Sievert.Core.Configuration;
using Sievert.Core.Rules;

namespace Sievert.Tests;

public class SuppressionTests
{
    [Fact]
    public void AJustifiedSuppression_RemovesTheFindingAndIsCounted()
    {
        RuleResult result = Run("""
            public class Sayac
            {
                public void Say(System.Collections.Generic.List<int> sayilar)
                {
                    foreach (int sayi in sayilar)
                    {
                        // sievert:disable SV004 sayilar bellekte, veritabani sorgusu degil
                        _ = sayilar.Any();
                    }
                }
            }
            """);

        Assert.DoesNotContain(result.Findings, finding => finding.RuleCode == "SV004");
        Assert.Single(result.Suppressions);
        Assert.Equal("SV004", result.Suppressions[0].RuleCode);
        Assert.Contains("bellekte", result.Suppressions[0].Reason, StringComparison.Ordinal);
    }

    [Fact]
    public void ASuppressionWithoutAReason_IsInvalidAndReportsItself()
    {
        RuleResult result = Run("""
            public class Sayac
            {
                public void Say(System.Collections.Generic.List<int> sayilar)
                {
                    foreach (int sayi in sayilar)
                    {
                        // sievert:disable SV004
                        _ = sayilar.Any();
                    }
                }
            }
            """);

        // Gerekcesiz susturma gecersiz: hem bulgu elenmiyor hem kendisi bulgu uretiyor.
        Assert.Contains(result.Findings, finding => finding.RuleCode == "SV004");
        Assert.Contains(result.Findings, finding => finding.RuleCode == "SV007");
        Assert.Empty(result.Suppressions);
    }

    [Fact]
    public void ASuppression_OnlyAffectsTheLineRightAfterIt()
    {
        RuleResult result = Run("""
            public class Sayac
            {
                public void Say(System.Collections.Generic.List<int> sayilar)
                {
                    foreach (int sayi in sayilar)
                    {
                        // sievert:disable SV004 sadece asagidaki satir icin
                        _ = sayilar.Any();
                        _ = sayilar.Count();
                    }
                }
            }
            """);

        // Ikinci cagri susturulmuyor: susturma tek satirlik.
        Assert.Single(result.Findings, finding => finding.RuleCode == "SV004");
        Assert.Single(result.Suppressions);
    }

    [Fact]
    public void ASuppressionForAnotherRule_DoesNotRemoveTheFinding()
    {
        RuleResult result = Run("""
            public class Sayac
            {
                public void Say(System.Collections.Generic.List<int> sayilar)
                {
                    foreach (int sayi in sayilar)
                    {
                        // sievert:disable SV002 baska bir kural
                        _ = sayilar.Any();
                    }
                }
            }
            """);

        Assert.Contains(result.Findings, finding => finding.RuleCode == "SV004");
        Assert.Empty(result.Suppressions);
    }

    private static RuleResult Run(string source) =>
        new RuleRunner(RuleCatalog.Select(SievertConfig.Default).Selection!.Enabled)
            .Run([RuleTestHelper.Context("src/Sayac.cs", source)]);
}
