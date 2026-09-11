using System.Diagnostics;
using System.Text;

using Sievert.Core.Mining;
using Sievert.Mining;

namespace Sievert.Cli;

/// <summary>Bir madencilik kosusunun sonucu: ozet ve ne kadar surdugu.</summary>
/// <param name="Summary">Kosunun ozeti.</param>
/// <param name="Elapsed">Gecen sure.</param>
public sealed record MineResult(MiningSummary Summary, TimeSpan Elapsed);

/// <summary>
/// mine komutunun isi. Commit'ler miner'dan tek tek geliyor, her biri yazilip
/// birakiliyor; hicbir noktada butun tarih bellekte durmuyor.
/// </summary>
public static class MineCommand
{
    public static MineResult Run(string repositoryPath, MiningOptions options, string? outputPath)
    {
        Stopwatch clock = Stopwatch.StartNew();

        RepositoryMiner miner = new();
        MiningTally tally = new(RepositoryMiner.RenameSimilarityThreshold);

        // Dosya yoksa yazici da yok; --out verilmediginde sadece ozet hesaplaniyor.
        using StreamWriter? writer = outputPath is null
            ? null
            : new StreamWriter(outputPath, append: false, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

        foreach (CommitRecord commit in miner.Read(repositoryPath, options))
        {
            tally.Add(commit);
            writer?.WriteLine(MineJsonFormatter.Line(commit));
        }

        clock.Stop();

        return new MineResult(tally.Build(miner.SkippedMergeCount), clock.Elapsed);
    }
}
