using System.Diagnostics;
using System.Text;

using Sievert.Core.Mining;
using Sievert.Data;
using Sievert.Mining;

namespace Sievert.Cli;

/// <summary>Bir madencilik kosusunun sonucu.</summary>
/// <param name="Summary">Kosunun ozeti.</param>
/// <param name="Elapsed">Gecen sure.</param>
/// <param name="Store">Veritabanina yazildiysa yazma sonucu, yazilmadiysa null.</param>
public sealed record MineResult(MiningSummary Summary, TimeSpan Elapsed, StoreResult? Store);

/// <summary>
/// mine komutunun isi. Commit'ler miner'dan tek tek geliyor; ayni akis hem dosyaya hem
/// veritabanina besleniyor, iki kez okuma ya da bellekte toplama yok.
/// </summary>
public static class MineCommand
{
    public static MineResult Run(
        string repositoryPath,
        MiningOptions options,
        string? outputPath,
        CommitStore? store = null,
        StoreOptions? storeOptions = null)
    {
        Stopwatch clock = Stopwatch.StartNew();

        RepositoryMiner miner = new();
        MiningTally tally = new(RepositoryMiner.RenameSimilarityThreshold);

        // Dosya yoksa yazici da yok; --out verilmediginde sadece ozet hesaplaniyor.
        using StreamWriter? writer = outputPath is null
            ? null
            : new StreamWriter(outputPath, append: false, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

        IEnumerable<CommitRecord> stream = Tap(
            miner.Read(repositoryPath, options),
            commit =>
            {
                tally.Add(commit);
                writer?.WriteLine(MineJsonFormatter.Line(commit));
            });

        StoreResult? stored = null;

        if (store is not null && storeOptions is not null)
        {
            stored = store.Write(stream, storeOptions);
        }
        else
        {
            // Veritabani yoksa akisi yine de tuketmek gerekiyor, yoksa hicbir sey okunmaz.
            foreach (CommitRecord _ in stream)
            {
            }
        }

        clock.Stop();

        return new MineResult(tally.Build(miner.SkippedMergeCount), clock.Elapsed, stored);
    }

    /// <summary>
    /// Akisi bozmadan her ogeye dokunur. Ozet ve JSONL yazimi buradan geciyor, boylece
    /// veritabanina yazan taraf ayni akisi bastan okumak zorunda kalmiyor.
    /// </summary>
    private static IEnumerable<CommitRecord> Tap(IEnumerable<CommitRecord> source, Action<CommitRecord> touch)
    {
        foreach (CommitRecord commit in source)
        {
            touch(commit);
            yield return commit;
        }
    }
}
