namespace Sievert.Data.Entities;

/// <summary>
/// Bir static-scan isinin urettigi tek bir bulgu.
///
/// Yollar **goreli**: mutlak yol saklamak, veritabanini o makineye baglamak ve API
/// cevabina sunucunun klasor duzenini sizdirma riski acmak olurdu.
///
/// Susturulan bulgular bu tabloya **girmiyor**; sayilari isin ozetinde duruyor. Sebep:
/// susturulmus bir bulgu "bulundu" degil "bilerek gormezden gelindi" demek, ikisini ayni
/// listede tutmak sayilari bozardi.
/// </summary>
public sealed class StaticAnalysisFindingRow
{
    public int Id { get; set; }

    public Guid AnalysisJobId { get; set; }

    public AnalysisJobRow? AnalysisJob { get; set; }

    public required string RuleCode { get; set; }

    /// <summary>error / warning / info.</summary>
    public required string Severity { get; set; }

    /// <summary>Tarama kokune gore goreli yol.</summary>
    public required string RelativePath { get; set; }

    public int Line { get; set; }

    /// <summary>Sutun. Kurallar su an sutun uretmiyor, o yuzden null.</summary>
    public int? Column { get; set; }

    public string? MemberName { get; set; }

    public required string Message { get; set; }

    public required string Rationale { get; set; }

    public bool IsTestCode { get; set; }

    /// <summary>Bu tabloda her zaman false; susturulanlar kaydedilmiyor, sayiliyor.</summary>
    public bool IsSuppressed { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
}
