using System.Threading.Channels;

namespace Sievert.Api.Analysis;

/// <summary>
/// Worker'i uyandiran kuyruk.
///
/// Isin kendisi burada DEGIL, veritabaninda. Kuyruk yalnizca "bakilacak bir is var"
/// haberini tasiyor; surec kapanip acildiginda kuyruk bosaliyor ama isler duruyor ve
/// yeniden kuyruga aliniyorlar (ADR 0024).
/// </summary>
public interface IAnalysisJobQueue
{
    /// <summary>Kuyrugun kapasitesi; saglik ucunda gorunuyor.</summary>
    int Capacity { get; }

    /// <summary>Su an kuyrukta bekleyen oge sayisi.</summary>
    int Count { get; }

    /// <summary>Isi kuyruga ekler. Kuyruk doluysa yer acilana kadar bekler.</summary>
    ValueTask EnqueueAsync(Guid jobId, CancellationToken cancellation = default);

    /// <summary>Sirasi gelen isi alir.</summary>
    ValueTask<Guid> DequeueAsync(CancellationToken cancellation);
}

/// <summary>
/// Sinirli kapasiteli kanal.
///
/// Dolu oldugunda <c>Wait</c>: en eskiyi ya da en yeniyi ATMAK, veritabaninda
/// <c>queued</c> duran bir isin hic calistirilmamasi demek olurdu ve kimse bunu fark
/// etmezdi. Beklemek geri basinc uretiyor; ustelik kuyruk dolu kalsa bile isler
/// kaybolmuyor, yeniden baslatmada kurtarma onlari geri aliyor.
/// </summary>
public sealed class AnalysisJobQueue : IAnalysisJobQueue
{
    private readonly Channel<Guid> channel;

    public AnalysisJobQueue(AnalysisOptions options)
    {
        Capacity = options.QueueCapacity;

        channel = Channel.CreateBounded<Guid>(new BoundedChannelOptions(options.QueueCapacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = false,
            SingleWriter = false,
        });
    }

    public int Capacity { get; }

    public int Count => channel.Reader.Count;

    public ValueTask EnqueueAsync(Guid jobId, CancellationToken cancellation = default) =>
        channel.Writer.WriteAsync(jobId, cancellation);

    public ValueTask<Guid> DequeueAsync(CancellationToken cancellation) =>
        channel.Reader.ReadAsync(cancellation);
}
