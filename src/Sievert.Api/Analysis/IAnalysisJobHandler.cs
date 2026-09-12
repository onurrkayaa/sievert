using Sievert.Data.Entities;

namespace Sievert.Api.Analysis;

/// <summary>Bir isin nasil bittigi.</summary>
/// <param name="Status">Terminal durum.</param>
/// <param name="ResultCount">Kaydedilen sonuc satiri.</param>
/// <param name="ProcessedItems">Islenen oge sayisi.</param>
/// <param name="ErrorCode">Makine okunabilir hata kodu; basarida null.</param>
/// <param name="ErrorMessage">Temizlenmis hata metni; yol ve yigin izi ICERMEZ.</param>
/// <param name="ResultSummary">Is turune ozel sayilar, JSON.</param>
public sealed record JobOutcome(
    AnalysisJobStatus Status,
    int ResultCount,
    int ProcessedItems,
    string? ErrorCode = null,
    string? ErrorMessage = null,
    string? ResultSummary = null)
{
    public static JobOutcome Canceled(int resultCount, int processed) => new(
        AnalysisJobStatus.Canceled,
        resultCount,
        processed,
        "ANALYSIS_CANCELED",
        "Is iptal edildi. Kaydedilen sonuc kismi.");

    public static JobOutcome Failed(string code, string message, int resultCount = 0, int processed = 0) =>
        new(AnalysisJobStatus.Failed, resultCount, processed, code, message);
}

/// <summary>Bir is turunu kosan bilesen.</summary>
public interface IAnalysisJobHandler
{
    AnalysisJobKind Kind { get; }

    /// <summary>
    /// Isi kosar. Istisna firlatmak yerine <see cref="JobOutcome"/> dondurmek tercih
    /// ediliyor; worker'in istisna metnini kullaniciya gosterilebilir hale getirmesi
    /// gerekmesin diye. Iptal jetonu yine istisna atabilir, onu worker ele aliyor.
    /// </summary>
    Task<JobOutcome> RunAsync(AnalysisJobRun run, CancellationToken cancellation);
}

/// <summary>Bir kosunun baglami.</summary>
/// <param name="JobId">Isin kimligi.</param>
/// <param name="RepositoryId">Uzerinde calisilan depo.</param>
/// <param name="Progress">Ilerleme yazici ve iptal sorgusu.</param>
public sealed record AnalysisJobRun(Guid JobId, int RepositoryId, JobProgress Progress);
