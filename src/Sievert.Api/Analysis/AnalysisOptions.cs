namespace Sievert.Api.Analysis;

/// <summary>
/// Arka plan is altyapisinin ayarlari.
///
/// Hicbiri olcum sonucuna bakilarak secilmedi; hepsi kod yazilirken sabitlendi ve
/// gerekceleri ADR 0024'te.
/// </summary>
public sealed class AnalysisOptions
{
    public const string Section = "Sievert:Analysis";

    public const int MinimumWorkerConcurrency = 1;

    public const int MaximumWorkerConcurrency = 4;

    /// <summary>
    /// Kuyrukta bekleyebilecek is sayisi. Kuyruk yalnizca uyandirma mekanizmasi; isin
    /// kendisi veritabaninda durdugu icin kuyruk dolarsa is kaybolmuyor.
    /// </summary>
    public int QueueCapacity { get; set; } = 100;

    /// <summary>
    /// Ayni anda kosan is sayisi. Varsayilan 1: bu turun olcumleri tek is uzerinden
    /// aliniyor ve paralel kosan iki isin birbirinin suresini bozmasini istemedim.
    /// </summary>
    public int WorkerConcurrency { get; set; } = 1;

    /// <summary>Risk skorlamasinda tek seferde islenen commit sayisi. Sonuc gorulmeden sabitlendi.</summary>
    public int RiskBatchSize { get; set; } = 250;

    /// <summary>Bulgular veritabanina bu buyuklukte obekler halinde yaziliyor.</summary>
    public int FindingBatchSize { get; set; } = 500;

    /// <summary>
    /// Iki rutin ilerleme yazimi arasindaki en kisa sure. Oge basina yazmak, 22 bin
    /// commit'lik bir iste 22 bin UPDATE demek olurdu.
    /// </summary>
    public TimeSpan ProgressInterval { get; set; } = TimeSpan.FromMilliseconds(500);

    /// <summary>Ayar gecerli mi; degilse acilista durduracak metin doner.</summary>
    public string? Validate()
    {
        if (WorkerConcurrency is < MinimumWorkerConcurrency or > MaximumWorkerConcurrency)
        {
            return $"{Section}:WorkerConcurrency {MinimumWorkerConcurrency} ile "
                + $"{MaximumWorkerConcurrency} arasinda olmali, verilen: {WorkerConcurrency}.";
        }

        if (QueueCapacity < 1)
        {
            return $"{Section}:QueueCapacity en az 1 olmali, verilen: {QueueCapacity}.";
        }

        if (RiskBatchSize < 1)
        {
            return $"{Section}:RiskBatchSize en az 1 olmali, verilen: {RiskBatchSize}.";
        }

        return FindingBatchSize < 1
            ? $"{Section}:FindingBatchSize en az 1 olmali, verilen: {FindingBatchSize}."
            : null;
    }
}
