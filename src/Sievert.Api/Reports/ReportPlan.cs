using System.Globalization;
using System.Security.Cryptography;
using System.Text;

using Sievert.Contracts;
using Sievert.Data.Entities;
using Sievert.Modeling;

namespace Sievert.Api.Reports;

/// <summary>Dogrulanmis rapor parametreleri. Hepsi dolu; null kontrolu burada bitiyor.</summary>
public sealed record ResolvedReportRequest(
    Guid RiskAnalysisJobId,
    Guid? StaticAnalysisJobId,
    bool IncludePartial,
    int CommitWindow,
    int FileLimit,
    int TimelineCount,
    int TopCommitCount,
    int FindingLimit,
    string Culture,
    string? Title,
    string? Notes)
{
    /// <summary>
    /// Istek govdesinin parmak izi.
    ///
    /// Ayni tekrar anahtarinin farkli bir govdeyle kullanilmasini yakalamak icin var.
    /// Alanlar sabit sirada ve degismez kulturle yaziliyor; yoksa ayni istek iki
    /// makinede iki parmak izi verirdi.
    /// </summary>
    public string Fingerprint()
    {
        StringBuilder text = new();

        text.Append(RiskAnalysisJobId.ToString("n", CultureInfo.InvariantCulture)).Append('\n')
            .Append(StaticAnalysisJobId?.ToString("n", CultureInfo.InvariantCulture) ?? "-").Append('\n')
            .Append(IncludePartial ? "1" : "0").Append('\n')
            .Append(CommitWindow.ToString(CultureInfo.InvariantCulture)).Append('\n')
            .Append(FileLimit.ToString(CultureInfo.InvariantCulture)).Append('\n')
            .Append(TimelineCount.ToString(CultureInfo.InvariantCulture)).Append('\n')
            .Append(TopCommitCount.ToString(CultureInfo.InvariantCulture)).Append('\n')
            .Append(FindingLimit.ToString(CultureInfo.InvariantCulture)).Append('\n')
            .Append(Culture).Append('\n')
            .Append(Title ?? "-").Append('\n')
            .Append(Notes ?? "-");

        return Convert.ToHexStringLower(SHA256.HashData(new UTF8Encoding(false).GetBytes(text.ToString())));
    }
}

/// <summary>Rapor uretimi icin cozulmus butun girdiler.</summary>
public sealed record ReportPlan(
    RepositoryRow Repository,
    AnalysisJobRow RiskJob,
    AnalysisJobRow? StaticJob,
    ModelProfile Profile,
    ResolvedReportRequest Request,
    bool IsPartial,
    string RankingScope,
    IReadOnlyList<string> Limitations);

/// <summary>Dogrulama sonucu: ya plan ya hata kodu.</summary>
/// <param name="ErrorCode">Basarisizsa hangi kod.</param>
/// <param name="Detail">Kullaniciya gosterilecek aciklama; yol ve kimlik bilgisi icermez.</param>
public sealed record ReportValidation(
    ResolvedReportRequest? Request,
    string? ErrorCode,
    string? Detail)
{
    public static ReportValidation Ok(ResolvedReportRequest request) => new(request, null, null);

    public static ReportValidation Error(string code, string detail) => new(null, code, detail);
}

/// <summary>Istek govdesini sozlesmedeki sinirlara gore dogrular.</summary>
public static class ReportRequestValidator
{
    public static ReportValidation Validate(ReportRequest? request)
    {
        if (request is null || request.RiskAnalysisJobId == Guid.Empty)
        {
            return ReportValidation.Error(
                ApiError.ReportRequestInvalid, "riskAnalysisJobId alani zorunlu.");
        }

        string? culture = request.Culture is null
            ? ReportCulture.Turkish
            : ReportCulture.Normalize(request.Culture);

        if (culture is null)
        {
            return ReportValidation.Error(
                ApiError.ReportCultureNotSupported,
                $"culture yalniz {string.Join(" ya da ", ReportCulture.Supported)} olabilir.");
        }

        int window = request.CommitWindow ?? ReportLimits.DefaultCommitWindow;
        int files = request.FileLimit ?? ReportLimits.DefaultFileLimit;
        int timeline = request.TimelineCount ?? ReportLimits.DefaultTimelineCount;
        int commits = request.TopCommitCount ?? ReportLimits.DefaultTopCommitCount;
        int findings = request.FindingLimit ?? ReportLimits.DefaultFindingLimit;

        if (!ReportLimits.IsCommitWindow(window))
        {
            return Range("commitWindow", ReportLimits.MinimumCommitWindow, ReportLimits.MaximumCommitWindow);
        }

        if (!ReportLimits.IsFileLimit(files))
        {
            return Range("fileLimit", ReportLimits.MinimumFileLimit, ReportLimits.MaximumFileLimit);
        }

        if (!ReportLimits.IsTimelineCount(timeline))
        {
            return Range("timelineCount", ReportLimits.MinimumTimelineCount, ReportLimits.MaximumTimelineCount);
        }

        if (!ReportLimits.IsTopCommitCount(commits))
        {
            return Range("topCommitCount", ReportLimits.MinimumTopCommitCount, ReportLimits.MaximumTopCommitCount);
        }

        if (!ReportLimits.IsFindingLimit(findings))
        {
            return Range("findingLimit", ReportLimits.MinimumFindingLimit, ReportLimits.MaximumFindingLimit);
        }

        // Uzunluk sinirini asan metin sessizce kirpilmiyor: kullanici yazdigi seyin
        // raporda oldugunu sanmamali.
        if (request.Title is { Length: > ReportLimits.MaximumTitleLength })
        {
            return ReportValidation.Error(
                ApiError.ReportParameterInvalid,
                $"title en fazla {ReportLimits.MaximumTitleLength} karakter olabilir.");
        }

        if (request.Notes is { Length: > ReportLimits.MaximumNotesLength })
        {
            return ReportValidation.Error(
                ApiError.ReportParameterInvalid,
                $"notes en fazla {ReportLimits.MaximumNotesLength} karakter olabilir.");
        }

        return ReportValidation.Ok(new ResolvedReportRequest(
            request.RiskAnalysisJobId,
            request.StaticAnalysisJobId,
            request.IncludePartial,
            window,
            files,
            timeline,
            commits,
            findings,
            culture,
            Text.Optional(request.Title, ReportLimits.MaximumTitleLength),
            Text.Optional(request.Notes, ReportLimits.MaximumNotesLength)));
    }

    private static ReportValidation Range(string name, int minimum, int maximum) =>
        ReportValidation.Error(
            ApiError.ReportParameterInvalid,
            $"{name} {minimum} ile {maximum} arasinda olmali.");
}

/// <summary>Kullanicidan gelen metni duz metne cevirir.</summary>
public static class Text
{
    /// <summary>
    /// Kontrol karakterlerini temizler ve uzunlugu kirpar.
    ///
    /// PDF'te isaretleme dili calismiyor, yani HTML ya da Markdown zaten yorumlanmiyor.
    /// Temizlenen sey satir sonlari ve kontrol karakterleri: ikisi de tabloyu bozuyor ve
    /// ayni metin dosya adina ya da HTTP basligina giderse oradaki anlami tehlikeli.
    /// </summary>
    public static string Plain(string? value, int maximumLength)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        StringBuilder cleaned = new(value.Length);

        foreach (char character in value)
        {
            cleaned.Append(char.IsControl(character) ? ' ' : character);
        }

        string text = cleaned.ToString().Trim();

        while (text.Contains("  ", StringComparison.Ordinal))
        {
            text = text.Replace("  ", " ", StringComparison.Ordinal);
        }

        return text.Length <= maximumLength ? text : text[..maximumLength];
    }

    /// <summary>Bos metni null'a cevirir; rapor "baslik yok" ile "bos baslik" ayirmasin.</summary>
    public static string? Optional(string? value, int maximumLength)
    {
        string text = Plain(value, maximumLength);

        return text.Length == 0 ? null : text;
    }
}
