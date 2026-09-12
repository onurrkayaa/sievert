using Sievert.Api;
using Sievert.Api.Endpoints;

namespace Sievert.Tests;

/// <summary>
/// Urun dili sozlesmesinin testleri: docs/urun/risk-sozlesmesi.md surum 1.0.
///
/// Sozlesme bir belge ama burada calisan bir kontrol. Yasak ifadeler listesi belgede
/// kalirsa bir gun bir metne sizar ve kimse fark etmez.
/// </summary>
public sealed class RiskContractTests
{
    /// <summary>Sozlesmedeki yasak ifadeler. Olumsuzlamayla bile kullanilmiyor.</summary>
    private static readonly string[] Forbidden =
    [
        "hata olasilig",
        "ihtimalle hata",
        "gercek hatayi tahmin",
        "kalibre edilmis olasilik",
        "tum c# repolarinda genellenir",
        "birlesik skor",
    ];

    public static TheoryData<string> AllApiText()
    {
        TheoryData<string> texts = [];

        foreach (string code in (string[])
        [
            RiskWarning.UncalibratedScore,
            RiskWarning.SzzTarget,
            RiskWarning.StaticAnalysisNotIncluded,
            RiskWarning.HumanValidationLimited,
            RiskWarning.CsLabelCoverageLimit,
            RiskWarning.OutsideTrainRange,
            RiskWarning.ExternalModelProfile,
            RiskWarning.UnknownRepositoryModel,
        ])
        {
            texts.Add(RiskWarning.Text(code));
        }

        foreach (string limitation in ModelEndpoints.Limitations)
        {
            texts.Add(limitation);
        }

        return texts;
    }

    [Theory]
    [MemberData(nameof(AllApiText))]
    public void NoApiTextUsesAForbiddenPhrase(string text)
    {
        foreach (string phrase in Forbidden)
        {
            Assert.DoesNotContain(phrase, text, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void EveryWarningCodeHasAnExplanation()
    {
        foreach (string code in (string[])
        [
            RiskWarning.UncalibratedScore,
            RiskWarning.SzzTarget,
            RiskWarning.StaticAnalysisNotIncluded,
            RiskWarning.HumanValidationLimited,
            RiskWarning.CsLabelCoverageLimit,
            RiskWarning.OutsideTrainRange,
            RiskWarning.ExternalModelProfile,
            RiskWarning.UnknownRepositoryModel,
        ])
        {
            Assert.False(string.IsNullOrWhiteSpace(RiskWarning.Text(code)));
        }
    }

    [Fact]
    public void AnUnknownWarningCode_StopsInsteadOfReturningAnEmptyString()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => RiskWarning.Text("BOYLE_BIR_KOD_YOK"));
    }

    [Fact]
    public void TheThreeMandatoryWarningsAreAlwaysInTheAlwaysList()
    {
        Assert.Contains(RiskWarning.UncalibratedScore, RiskWarning.Always);
        Assert.Contains(RiskWarning.SzzTarget, RiskWarning.Always);
        Assert.Contains(RiskWarning.StaticAnalysisNotIncluded, RiskWarning.Always);
    }

    /// <summary>
    /// Cevap sozlesmesinde <c>probability</c> ya da birlesik skor diye bir alan yok.
    /// Alan adini yansima ile kontrol ediyorum: bir gun biri eklerse test duser.
    /// </summary>
    [Fact]
    public void TheAssessmentRecordHasNoProbabilityOrCombinedField()
    {
        foreach (System.Reflection.PropertyInfo property in typeof(CommitRiskAssessment).GetProperties())
        {
            Assert.DoesNotContain("probability", property.Name, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("combined", property.Name, StringComparison.OrdinalIgnoreCase);
        }

        foreach (System.Reflection.PropertyInfo property in typeof(FeatureContribution).GetProperties())
        {
            Assert.DoesNotContain("probability", property.Name, StringComparison.OrdinalIgnoreCase);
        }
    }
}
