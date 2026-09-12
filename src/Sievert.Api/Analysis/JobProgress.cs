using Sievert.Data;

namespace Sievert.Api.Analysis;

/// <summary>
/// Isin ilerlemesini veritabanina yazar.
///
/// Rutin yazimlar kisitli: iki yazim arasinda en az bir aralik geciyor. Oge basina
/// yazmak, 22 917 commit'lik bir iste 22 917 UPDATE demek olurdu ve olculen surenin
/// buyuk kismi ilerleme yazmak olurdu.
///
/// Asama degisiminde ve terminal geciste yazim ZORUNLU: kullanicinin gordugu son durum
/// gercek son durum olmali.
/// </summary>
public sealed class JobProgress(
    AnalysisJobStore store,
    Guid jobId,
    TimeSpan interval,
    TimeProvider clock)
{
    private string lastPhase = string.Empty;

    private DateTimeOffset lastWrite = DateTimeOffset.MinValue;

    /// <summary>Kac kez veritabanina yazildi. Olcum bu sayiyi raporluyor.</summary>
    public int WriteCount { get; private set; }

    /// <summary>Kac kez veritabanina iptal durumu soruldu.</summary>
    public int CancellationCheckCount { get; private set; }

    /// <summary>Araliga uyarak yazar; asama degistiyse aralik beklemeden yazar.</summary>
    public async Task ReportAsync(string phase, int processed, int? total, CancellationToken cancellation)
    {
        DateTimeOffset now = clock.GetUtcNow();

        if (string.Equals(phase, lastPhase, StringComparison.Ordinal) && now - lastWrite < interval)
        {
            return;
        }

        await WriteAsync(phase, processed, total, now, cancellation);
    }

    /// <summary>Aralik gozetmeden yazar. Terminal gecisten hemen once cagriliyor.</summary>
    public Task FlushAsync(string phase, int processed, int? total, CancellationToken cancellation) =>
        WriteAsync(phase, processed, total, clock.GetUtcNow(), cancellation);

    /// <summary>
    /// Iptal istenmis mi. Yerel jeton yeterli degil: istegi alan surec isi kosan surecten
    /// farkli olabilir, o yuzden kaynak veritabani.
    /// </summary>
    public async Task<bool> IsCancellationRequestedAsync(CancellationToken cancellation)
    {
        CancellationCheckCount++;

        return await store.IsCancellationRequestedAsync(jobId, cancellation);
    }

    private async Task WriteAsync(
        string phase,
        int processed,
        int? total,
        DateTimeOffset now,
        CancellationToken cancellation)
    {
        await store.UpdateProgressAsync(jobId, phase, processed, total, now, cancellation);

        WriteCount++;
        lastPhase = phase;
        lastWrite = now;
    }
}
