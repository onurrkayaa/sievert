namespace Sievert.Data.Entities;

/// <summary>
/// Bir risk-score-all isinin tek bir commit icin kaydettigi degerlendirme.
///
/// <c>Commits</c> tablosuna yazilmiyor: ayni commit farkli kosularda ve ileride farkli
/// model surumleriyle yeniden skorlanabilir. Uzerine yazmak, hangi skorun hangi modelden
/// ve hangi kosudan geldigini kaybetmek olurdu (ADR 0024).
/// </summary>
public sealed class CommitRiskSnapshotRow
{
    public int Id { get; set; }

    public Guid AnalysisJobId { get; set; }

    public AnalysisJobRow? AnalysisJob { get; set; }

    public int CommitId { get; set; }

    public CommitRow? Commit { get; set; }

    /// <summary>Ham model skoru. Kalibre edilmis bir olasilik DEGIL.</summary>
    public double RawModelScore { get; set; }

    /// <summary>Profilin egitim dagilimindaki yuzdelik sira, 0-100.</summary>
    public double RiskIndex { get; set; }

    public bool DecisionAt05 { get; set; }

    public bool DecisionAtTrainThreshold { get; set; }

    public double TrainThreshold { get; set; }

    public required string ModelProfile { get; set; }

    public required string ModelChecksum { get; set; }

    /// <summary>Bu asamada her zaman false; sutun, gelecekte degisirse gorulsun diye var.</summary>
    public bool IsCalibrated { get; set; }

    /// <summary>Uyari kodlari, JSON dizi metni olarak.</summary>
    public required string WarningCodes { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
}
