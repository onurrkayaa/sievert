using Sievert.Modeling;

namespace Sievert.Tests;

/// <summary>Eslesmis dairesel hareketli blok bootstrap.</summary>
public class BootstrapTests
{
    [Theory]
    [InlineData(828, 29)]
    [InlineData(2547, 51)]
    [InlineData(6876, 83)]
    [InlineData(9, 3)]
    [InlineData(10, 4)]
    public void BlockLength_IsTheCeilingOfTheSquareRoot(int count, int expected)
    {
        Assert.Equal(expected, BlockBootstrap.BlockLength(count));
    }

    [Fact]
    public void Indices_ReturnExactlyEnoughRows()
    {
        int[] indices = BlockBootstrap.Indices(new Random(20260912), 100, 10);

        Assert.Equal(100, indices.Length);
        Assert.All(indices, index => Assert.InRange(index, 0, 99));
    }

    [Fact]
    public void Indices_WrapAroundTheEndOfTheRepository()
    {
        // Tek blok, uzunluk kume boyutu kadar: baslangic nerede olursa olsun
        // butun satirlar bir kez geciyor, yani sarma calisiyor.
        int[] indices = BlockBootstrap.Indices(new Random(1), 10, 10);

        Assert.Equal(10, indices.Length);
        Assert.Equal(10, indices.Distinct().Count());
    }

    [Fact]
    public void Indices_AreTheSameForTheSameSeed()
    {
        Assert.Equal(
            BlockBootstrap.Indices(new Random(20260912), 200, 15),
            BlockBootstrap.Indices(new Random(20260912), 200, 15));
    }

    [Fact]
    public void Indices_DifferForADifferentSeed()
    {
        Assert.NotEqual(
            BlockBootstrap.Indices(new Random(20260912), 200, 15),
            BlockBootstrap.Indices(new Random(20260913), 200, 15));
    }

    [Fact]
    public void Indices_KeepBlocksContiguous()
    {
        int[] indices = BlockBootstrap.Indices(new Random(7), 100, 10);

        // Her blogun icinde ardisik indeksler var (dairesel mod ile).
        for (int block = 0; block < 10; block++)
        {
            for (int offset = 1; offset < 10; offset++)
            {
                int previous = indices[(block * 10) + offset - 1];
                int current = indices[(block * 10) + offset];

                Assert.Equal((previous + 1) % 100, current);
            }
        }
    }

    [Fact]
    public void Run_IsDeterministicForTheSameSeed()
    {
        BootstrapReport first = BlockBootstrap.Run(Repositories(), seed: 20260912, repeats: 50);
        BootstrapReport second = BlockBootstrap.Run(Repositories(), seed: 20260912, repeats: 50);

        Assert.Equal(first.Micro.DeltaF1.Values, second.Micro.DeltaF1.Values);
        Assert.Equal(first.Macro.DeltaPrAuc.Values, second.Macro.DeltaPrAuc.Values);
    }

    [Fact]
    public void Run_MicroAndMacroAreNotTheSameNumber()
    {
        BootstrapReport report = BlockBootstrap.Run(Repositories(), seed: 20260912, repeats: 100);

        Assert.NotEqual(report.Micro.DeltaF1.Mean, report.Macro.DeltaF1.Mean, 6);
    }

    [Fact]
    public void Run_CountsRepeatsWhereTheDeltaWasNotAvailable()
    {
        // Hic pozitifi olmayan tek bir repo: her tekrarda F1 ve PR-AUC N/A.
        List<PairedRow> rows = [];

        for (int index = 0; index < 40; index++)
        {
            rows.Add(new PairedRow(index % 2 == 0, 0.4, index % 3 == 0, 5, false));
        }

        BootstrapReport report = BlockBootstrap.Run([("a/b", rows)], seed: 20260912, repeats: 20);

        Assert.Equal(20, report.Repositories[0].NotAvailableF1);
        Assert.Equal(20, report.Repositories[0].NotAvailablePrAuc);
        Assert.Equal(20, report.Repositories[0].ValidRepeats);
    }

    [Fact]
    public void Run_UsesTheSameRowsForBothMethods()
    {
        // Model ile taban ayni tahminleri veriyorsa fark her tekrarda tam olarak 0 olmali.
        List<PairedRow> rows = [];

        for (int index = 0; index < 60; index++)
        {
            bool predicted = index % 4 == 0;
            rows.Add(new PairedRow(predicted, 0.3 + (index % 5 * 0.1), predicted, 0.3 + (index % 5 * 0.1), index % 3 == 0));
        }

        BootstrapReport report = BlockBootstrap.Run([("a/b", rows)], seed: 20260912, repeats: 30);

        Assert.All(report.Repositories[0].DeltaF1.Values, value => Assert.Equal(0.0, value, 12));
        Assert.All(report.Repositories[0].DeltaPrAuc.Values, value => Assert.Equal(0.0, value, 12));
    }

    [Fact]
    public void Run_KeepsTheDeclaredRepeatCount()
    {
        BootstrapReport report = BlockBootstrap.Run(Repositories(), seed: 20260912, repeats: 2000);

        Assert.Equal(2000, report.Repeats);
        Assert.Equal(2000, report.Micro.ValidRepeats);
    }

    private static IReadOnlyList<(string, IReadOnlyList<PairedRow>)> Repositories()
    {
        List<PairedRow> small = [];
        List<PairedRow> large = [];

        for (int index = 0; index < 80; index++)
        {
            small.Add(new PairedRow(index % 3 == 0, (index % 10) / 10.0, index % 4 == 0, index % 7, index % 5 == 0));
        }

        for (int index = 0; index < 400; index++)
        {
            large.Add(new PairedRow(index % 2 == 0, (index % 20) / 20.0, index % 5 == 0, index % 11, index % 3 == 0));
        }

        return [("a/small", small), ("b/large", large)];
    }
}
