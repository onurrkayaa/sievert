using Sievert.Modeling;

namespace Sievert.Tests;

/// <summary>
/// Metrik sozlesmesinin (docs/olcumler/asama5-metrik-sozlesmesi.md, surum 1.0) testleri.
/// Sayilarin cogu elle hesaplandi ve burada sabit duruyor; formul degisirse test duser.
/// </summary>
public class EvaluationMetricTests
{
    // --- Sayim ---

    [Fact]
    public void Counts_AreTakenFromAKnownLittleExample()
    {
        // tahmin / gercek: (1,1) (1,0) (0,1) (0,0) (1,1)
        Confusion counts = Confusion.From(
        [
            new(true, true),
            new(true, false),
            new(false, true),
            new(false, false),
            new(true, true),
        ]);

        Assert.Equal(2, counts.TruePositives);
        Assert.Equal(1, counts.FalsePositives);
        Assert.Equal(1, counts.FalseNegatives);
        Assert.Equal(1, counts.TrueNegatives);
        Assert.Equal(5, counts.Total);
    }

    [Fact]
    public void Precision_IsTruePositivesOverPredictedPositives()
    {
        Confusion counts = Confusion.From([new(true, true), new(true, false), new(true, true)]);

        Assert.Equal(2.0 / 3.0, counts.Precision);
    }

    [Fact]
    public void Recall_IsTruePositivesOverActualPositives()
    {
        Confusion counts = Confusion.From([new(true, true), new(false, true), new(false, true)]);

        Assert.Equal(1.0 / 3.0, counts.Recall);
    }

    [Fact]
    public void F1_MatchesTheHandComputedValue()
    {
        // TP 2, FP 1, FN 2 -> precision 2/3, recall 1/2, F1 = 2*(2/3)*(1/2)/(2/3+1/2)
        Confusion counts = Confusion.From(
        [
            new(true, true),
            new(true, true),
            new(true, false),
            new(false, true),
            new(false, true),
        ]);

        Assert.Equal(2.0 / 3.0, counts.Precision);
        Assert.Equal(0.5, counts.Recall);
        Assert.Equal(4.0 / 7.0, counts.F1!.Value, 12);
    }

    // --- Tanimsiz paydalar ---

    [Fact]
    public void Precision_IsNotAvailableWhenNothingWasPredictedPositive()
    {
        Confusion counts = Confusion.From([new(false, true), new(false, false)]);

        Assert.Null(counts.Precision);
        Assert.Equal(0.0, counts.Recall);
    }

    [Fact]
    public void F1_IsZeroWhenPrecisionIsNotAvailable()
    {
        // Sozlesme 1.0: precision N/A ise F1 = 0, ikisi birlikte yazilir.
        Confusion counts = Confusion.From([new(false, true), new(false, false)]);

        Assert.Null(counts.Precision);
        Assert.Equal(0.0, counts.F1);
    }

    [Fact]
    public void Recall_IsNotAvailableWhenThereAreNoActualPositives()
    {
        Confusion counts = Confusion.From([new(true, false), new(false, false)]);

        Assert.Null(counts.Recall);
        Assert.Null(counts.F1);
    }

    [Fact]
    public void F1_IsZeroWhenPrecisionAndRecallAreBothZero()
    {
        Confusion counts = Confusion.From([new(true, false), new(false, true)]);

        Assert.Equal(0.0, counts.Precision);
        Assert.Equal(0.0, counts.Recall);
        Assert.Equal(0.0, counts.F1);
    }

    // --- PR-AUC ---

    [Fact]
    public void PrAuc_MatchesTheHandComputedExampleInTheContract()
    {
        double? area = PrecisionRecall.Area(
        [
            new(0.9, true),
            new(0.8, false),
            new(0.7, true),
            new(0.6, false),
        ]);

        Assert.Equal(19.0 / 24.0, area!.Value, 12);
    }

    [Fact]
    public void PrAuc_DoesNotDependOnTheOrderOfTiedScores()
    {
        ScoredRow[] one = [new(0.5, true), new(0.5, false), new(0.5, true), new(0.5, false)];
        ScoredRow[] other = [new(0.5, false), new(0.5, false), new(0.5, true), new(0.5, true)];

        Assert.Equal(PrecisionRecall.Area(one)!.Value, PrecisionRecall.Area(other)!.Value, 12);
    }

    [Fact]
    public void PrAuc_DoesNotDependOnTheInputOrderAtAll()
    {
        ScoredRow[] rows = [new(0.9, true), new(0.8, false), new(0.7, true), new(0.6, false)];
        ScoredRow[] shuffled = [new(0.6, false), new(0.9, true), new(0.7, true), new(0.8, false)];

        Assert.Equal(PrecisionRecall.Area(rows)!.Value, PrecisionRecall.Area(shuffled)!.Value, 12);
    }

    [Fact]
    public void PrAuc_OfAConstantScoreIsThePositiveRate()
    {
        // Sozlesmede yazili: tek esik grubu, alan = P / N. Basari olcusu degil.
        ScoredRow[] rows = [new(0.0, true), new(0.0, false), new(0.0, false), new(0.0, false)];

        Assert.Equal(0.25, PrecisionRecall.Area(rows)!.Value, 12);
    }

    [Fact]
    public void PrAuc_OfAPerfectRankingIsOne()
    {
        ScoredRow[] rows = [new(0.9, true), new(0.8, true), new(0.7, false), new(0.6, false)];

        Assert.Equal(1.0, PrecisionRecall.Area(rows)!.Value, 12);
    }

    [Fact]
    public void PrAuc_IsNotAvailableWithoutAnyPositive()
    {
        Assert.Null(PrecisionRecall.Area([new(0.9, false), new(0.1, false)]));
    }

    // --- Oznitelik sozlesmesi ---

    [Theory]
    [InlineData("IsBugIntroducing")]
    [InlineData("LabelSource")]
    [InlineData("Sha")]
    [InlineData("BotMu")]
    public void Feature_ForbiddenFieldsCannotBeReadAsAPredictor(string name)
    {
        Assert.Throws<InvalidOperationException>(() => ModelFeatures.Value(Row(10), name));
    }

    [Fact]
    public void Feature_CandidatesCanBeReadByName()
    {
        Assert.Equal(10.0, ModelFeatures.Value(Row(10), "LinesAdded"));
    }

    [Fact]
    public void Feature_AnUnknownNameIsRefused()
    {
        Assert.Throws<InvalidOperationException>(() => ModelFeatures.Value(Row(10), "Bilinmeyen"));
    }

    internal static SnapshotRow Row(
        int linesAdded,
        bool positive = false,
        string identity = "a/b",
        string sha = "aa") =>
        new("klasor", identity, sha, DateTimeOffset.UnixEpoch, linesAdded,
            0, 0, 0, 0.0, 0, 0, 0, 0, 0, 0, 0, 0, 0, false, positive, positive ? "szz" : null, false);
}
