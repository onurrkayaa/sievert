using Sievert.Modeling;

namespace Sievert.Tests;

/// <summary>Kor dogrulama listesinin sayimi. Sahte veriyle bagimsiz dogrulama.</summary>
public class ValidationTallyTests
{
    [Fact]
    public void Counts_SeparateTheFourCategories()
    {
        TallyCounts counts = ValidationTally.Count("model-pozitif",
        [
            Decision("SAMPLE-01", true, true, ValidationTally.Introduced),
            Decision("SAMPLE-02", true, false, ValidationTally.Introduced),
            Decision("SAMPLE-03", true, true, ValidationTally.NotIntroduced),
            Decision("SAMPLE-04", true, false, ValidationTally.NotEnoughData),
            Decision("SAMPLE-05", true, true, ValidationTally.NotReviewed),
        ]);

        Assert.Equal(5, counts.Reviewed);
        Assert.Equal(2, counts.Introduced);
        Assert.Equal(1, counts.NotIntroduced);
        Assert.Equal(1, counts.NotEnoughData);
        Assert.Equal(1, counts.NotReviewed);
    }

    [Fact]
    public void Denominator_ExcludesBothUnresolvedCategories()
    {
        TallyCounts counts = ValidationTally.Count("k",
        [
            Decision("SAMPLE-01", true, true, ValidationTally.Introduced),
            Decision("SAMPLE-02", true, true, ValidationTally.NotIntroduced),
            Decision("SAMPLE-03", true, true, ValidationTally.NotEnoughData),
            Decision("SAMPLE-04", true, true, ValidationTally.NotReviewed),
        ]);

        // Payda 2, pay 1 -> isaret 0,5. VERI-YETMEDI ve BAKILMADI paydadan cikti.
        Assert.Equal(2, counts.Denominator);
        Assert.Equal(0.5, counts.Signal!.Value, 12);
    }

    [Fact]
    public void Signal_IsNotAvailableWhenNoDecisionWasMade()
    {
        TallyCounts counts = ValidationTally.Count("k",
        [
            Decision("SAMPLE-01", true, true, ValidationTally.NotEnoughData),
            Decision("SAMPLE-02", true, true, ValidationTally.NotReviewed),
        ]);

        Assert.Equal(0, counts.Denominator);
        Assert.Null(counts.Signal);
    }

    [Fact]
    public void Count_RefusesAnUnknownCategory()
    {
        InvalidDataException error = Assert.Throws<InvalidDataException>(
            () => ValidationTally.Count("k", [Decision("SAMPLE-01", true, true, "BELKI")]));

        Assert.Contains("SAMPLE-01", error.Message, StringComparison.Ordinal);
        Assert.Contains("BELKI", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Count_RefusesAnEmptyDecision()
    {
        Assert.Throws<InvalidDataException>(
            () => ValidationTally.Count("k", [Decision("SAMPLE-01", true, true, string.Empty)]));
    }

    [Fact]
    public void PositiveAndNegativeGroups_AreCountedSeparately()
    {
        ValidationDecision[] all =
        [
            Decision("SAMPLE-01", true, true, ValidationTally.Introduced),
            Decision("SAMPLE-02", true, false, ValidationTally.NotIntroduced),
            Decision("SAMPLE-03", false, false, ValidationTally.NotIntroduced),
            Decision("SAMPLE-04", false, true, ValidationTally.Introduced),
        ];

        TallyCounts positives = ValidationTally.Count("model-pozitif", all.Where(row => row.ModelPrediction));
        TallyCounts negatives = ValidationTally.Count("model-negatif", all.Where(row => !row.ModelPrediction));

        Assert.Equal(0.5, positives.Signal!.Value, 12);
        Assert.Equal(0.5, negatives.Signal!.Value, 12);

        // Iki kumenin sayilari ayri; tek bir toplamda birlestirilmiyor.
        Assert.Equal(2, positives.Reviewed);
        Assert.Equal(2, negatives.Reviewed);
    }

    [Fact]
    public void AgainstSzz_FillsTheFourCellsAndCountsUnresolvedSeparately()
    {
        (int positiveIntroduced, int positiveNot, int negativeIntroduced, int negativeNot, int unresolved) =
            ValidationTally.AgainstSzz(
            [
                Decision("SAMPLE-01", true, true, ValidationTally.Introduced),
                Decision("SAMPLE-02", true, true, ValidationTally.NotIntroduced),
                Decision("SAMPLE-03", true, false, ValidationTally.Introduced),
                Decision("SAMPLE-04", true, false, ValidationTally.NotIntroduced),
                Decision("SAMPLE-05", true, false, ValidationTally.NotEnoughData),
                Decision("SAMPLE-06", true, true, ValidationTally.NotReviewed),
            ]);

        Assert.Equal(1, positiveIntroduced);
        Assert.Equal(1, positiveNot);
        Assert.Equal(1, negativeIntroduced);
        Assert.Equal(1, negativeNot);
        Assert.Equal(2, unresolved);
    }

    [Fact]
    public void ReadDecisions_ReadsFilledAndEmptyBrackets()
    {
        string path = Path.Combine(Path.GetTempPath(), "sievert-tally-" + Guid.NewGuid().ToString("n") + ".md");

        File.WriteAllText(path,
            "### Ornek SAMPLE-01 — a/b\n\nKarar:\n\n[ KUSUR-GETIRDI ]\n\nNot:\n\n[ ]\n\n---\n\n"
            + "### Ornek SAMPLE-02 — a/b\n\nKarar:\n\n[ ]\n\nNot:\n\n[ ]\n\n---\n");

        IReadOnlyDictionary<string, string> decisions = ValidationTally.ReadDecisions(path);

        Assert.Equal("KUSUR-GETIRDI", decisions["SAMPLE-01"]);
        Assert.Equal(string.Empty, decisions["SAMPLE-02"]);

        File.Delete(path);
    }

    private static ValidationDecision Decision(string sample, bool prediction, bool szz, string decision) =>
        new(sample, "a/b", prediction, szz, decision);
}
