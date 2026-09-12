using System.Collections.Concurrent;

namespace Sievert.Api.Analysis;

/// <summary>
/// Bu surecte kosan islerin iptal jetonlari.
///
/// Iptalin **kaynagi burasi degil**: kaynak veritabanindaki
/// <c>CancellationRequestedAtUtc</c> sutunu. Buradaki jeton yalnizca hizlandirici, cunku
/// istegi alan surec ile isi kosan surec ayni olmayabilir. Kayit bulunamazsa iptal yine
/// gerceklesiyor, sadece bir sonraki obek sinirinda.
/// </summary>
public sealed class JobCancellationRegistry
{
    private readonly ConcurrentDictionary<Guid, CancellationTokenSource> running = new();

    /// <summary>Is icin jeton acar; is bitince <see cref="Release"/> cagrilmali.</summary>
    public CancellationTokenSource Register(Guid jobId, CancellationToken hostToken)
    {
        CancellationTokenSource source = CancellationTokenSource.CreateLinkedTokenSource(hostToken);

        running[jobId] = source;

        return source;
    }

    public void Release(Guid jobId)
    {
        if (running.TryRemove(jobId, out CancellationTokenSource? source))
        {
            source.Dispose();
        }
    }

    /// <summary>Is bu surecte kosuyorsa jetonu iptal eder. Kosmuyorsa false.</summary>
    public bool Cancel(Guid jobId)
    {
        if (!running.TryGetValue(jobId, out CancellationTokenSource? source))
        {
            return false;
        }

        source.Cancel();

        return true;
    }
}
