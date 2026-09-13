using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

using Sievert.Contracts;

namespace Sievert.Api.Reports;

/// <summary>
/// Raporun canonical girdi manifesti.
///
/// Manifest raporun **neyden uretildigini** tek bir metinde topluyor: hangi depo, hangi
/// is, hangi model, hangi parametreler, hangi kanit dosyalari. Iki kisi ayni PDF'i
/// elinde tutuyorsa ayni manifesti de uretebilmeli.
///
/// <b>Uretim zamani manifeste dahil degil.</b> Olsaydi ayni girdiyle iki kez uretilen
/// rapor iki farkli ozet verirdi ve "ayni girdi ayni cikti" iddiasi olculemezdi. Uretim
/// zamani artefakt kaydinda, manifestin disinda duruyor.
/// </summary>
public static class ReportManifest
{
    /// <summary>Manifest semasinin surumu. Alan eklenirse artiyor.</summary>
    public const string SchemaVersion = "1.0";

    /// <summary>Raporu ureten kod surumu; PDF'de ve manifestte ayni deger.</summary>
    public const string GeneratorVersion = "sievert-report/1.0";

    /// <summary>
    /// Canonical JSON: UTF-8, BOM yok, LF, sabit alan sirasi, girintisiz.
    ///
    /// Girintisiz olmasinin sebebi bicimsel degil: girinti karakterleri de ozete
    /// giriyor ve bir gun bicimlendirici degisirse ozet sessizce degisirdi.
    /// </summary>
    public static string Canonical(ReportManifestInput input)
    {
        JsonObject root = new()
        {
            ["schemaVersion"] = SchemaVersion,
            ["culture"] = input.Culture,
            ["repository"] = new JsonObject
            {
                ["id"] = input.RepositoryId,
                ["identity"] = input.RepositoryIdentity,
                ["displayName"] = input.RepositoryDisplayName,
            },
            ["reportParameters"] = new JsonObject
            {
                ["includePartial"] = input.IncludePartial,
                ["commitWindow"] = input.CommitWindow,
                ["fileLimit"] = input.FileLimit,
                ["timelineCount"] = input.TimelineCount,
                ["topCommitCount"] = input.TopCommitCount,
                ["findingLimit"] = input.FindingLimit,
                ["title"] = input.Title,
                ["notes"] = input.Notes,
            },
            ["riskAnalysis"] = new JsonObject
            {
                ["jobId"] = input.RiskJobId.ToString(),
                ["status"] = input.RiskJobStatus,
                ["isResultComplete"] = input.RiskJobComplete,
                ["rankingScope"] = input.RankingScope,
                ["requestedAtUtc"] = Moment(input.RiskRequestedAtUtc),
                ["startedAtUtc"] = Moment(input.RiskStartedAtUtc),
                ["completedAtUtc"] = Moment(input.RiskCompletedAtUtc),
                ["resultCount"] = input.RiskResultCount,
            },
            ["model"] = new JsonObject
            {
                ["profile"] = input.ModelProfile,
                ["codeCommit"] = input.ModelCodeCommit,
                ["checksum"] = input.ModelChecksum,
                ["isCalibrated"] = false,
                ["trainThreshold"] = Number(input.TrainThreshold),
                ["targetDefinition"] = input.TargetDefinition,
            },
            ["staticAnalysis"] = input.StaticJobId is null
                ? null
                : new JsonObject
                {
                    ["jobId"] = input.StaticJobId.Value.ToString(),
                    ["sourceHeadSha"] = input.StaticSourceHeadSha,
                    ["completedAtUtc"] = Moment(input.StaticCompletedAtUtc),
                    ["findingCount"] = input.StaticFindingCount,
                    ["suppressedCount"] = input.StaticSuppressedCount,
                    ["exemptionCount"] = input.StaticExemptionCount,
                },
            ["sourceData"] = new JsonObject
            {
                ["scoreReferenceChecksum"] = input.ScoreReferenceChecksum,
                ["modelResultsChecksum"] = input.ModelResultsChecksum,
            },
            ["limitations"] = Codes(input.LimitationCodes),
            ["generatedWith"] = new JsonObject
            {
                ["sievert"] = input.SievertVersion,
                ["reportSchema"] = SchemaVersion,
                ["generator"] = GeneratorVersion,
            },
        };

        return root.ToJsonString(new JsonSerializerOptions { WriteIndented = false });
    }

    /// <summary>Canonical metnin UTF-8 baytlari. BOM yok.</summary>
    public static byte[] Bytes(string canonical) => new UTF8Encoding(false).GetBytes(canonical);

    /// <summary>Manifestin ozeti: kucuk harf onaltilik SHA-256.</summary>
    public static string Checksum(string canonical) =>
        Convert.ToHexStringLower(SHA256.HashData(Bytes(canonical)));

    /// <summary>
    /// Zaman damgasi: her zaman UTC ve sabit bicim.
    ///
    /// Yerel saat dilimi yazilsaydi ayni veri iki makinede iki farkli manifest verirdi.
    /// </summary>
    private static JsonNode? Moment(DateTimeOffset? value) =>
        value is null ? null : JsonValue.Create(
            value.Value.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ", CultureInfo.InvariantCulture));

    /// <summary>Ondalik sayilar degismez kulturle; virgullu ondalik manifeste girmiyor.</summary>
    private static JsonNode Number(double value) =>
        JsonValue.Create(value.ToString("R", CultureInfo.InvariantCulture))!;

    /// <summary>Kod listesi siralanip tekillestiriliyor; sira girdinin sirasina bagli kalmasin.</summary>
    private static JsonArray Codes(IEnumerable<string> codes)
    {
        JsonArray array = [];

        foreach (string code in codes.Distinct(StringComparer.Ordinal).OrderBy(code => code, StringComparer.Ordinal))
        {
            array.Add(code);
        }

        return array;
    }
}

/// <summary>Manifesti kuran degerler. Hepsi veriden okunuyor, hicbiri uretim aninda uretilmiyor.</summary>
public sealed record ReportManifestInput
{
    public required int RepositoryId { get; init; }

    public required string RepositoryIdentity { get; init; }

    public required string RepositoryDisplayName { get; init; }

    public required string Culture { get; init; }

    public required bool IncludePartial { get; init; }

    public required int CommitWindow { get; init; }

    public required int FileLimit { get; init; }

    public required int TimelineCount { get; init; }

    public required int TopCommitCount { get; init; }

    public required int FindingLimit { get; init; }

    public string? Title { get; init; }

    public string? Notes { get; init; }

    public required Guid RiskJobId { get; init; }

    public required string RiskJobStatus { get; init; }

    public required bool RiskJobComplete { get; init; }

    public required string RankingScope { get; init; }

    public required DateTimeOffset RiskRequestedAtUtc { get; init; }

    public DateTimeOffset? RiskStartedAtUtc { get; init; }

    public DateTimeOffset? RiskCompletedAtUtc { get; init; }

    public required int RiskResultCount { get; init; }

    public required string ModelProfile { get; init; }

    public required string ModelCodeCommit { get; init; }

    public required string ModelChecksum { get; init; }

    public required double TrainThreshold { get; init; }

    /// <summary>Hedefin ne oldugu: SZZ tabanli otomatik etiket.</summary>
    public string TargetDefinition { get; init; } = "szz-automatic-label";

    public Guid? StaticJobId { get; init; }

    public string? StaticSourceHeadSha { get; init; }

    public DateTimeOffset? StaticCompletedAtUtc { get; init; }

    public int? StaticFindingCount { get; init; }

    public int? StaticSuppressedCount { get; init; }

    public int? StaticExemptionCount { get; init; }

    public required string ScoreReferenceChecksum { get; init; }

    public required string ModelResultsChecksum { get; init; }

    public required IReadOnlyList<string> LimitationCodes { get; init; }

    public required string SievertVersion { get; init; }
}

/// <summary>
/// Raporda her zaman duran sinirlilik kodlari.
///
/// Risk sozlesmesindeki uyari kodlariyla ayni ad alanini paylasiyorlar: bir rapor tek bir
/// commit'in cevabindan daha az sey soylememeli.
/// </summary>
public static class ReportLimitation
{
    public const string Uncalibrated = RiskWarning.UncalibratedScore;

    public const string SzzTarget = RiskWarning.SzzTarget;

    public const string StaticNotIncluded = RiskWarning.StaticAnalysisNotIncluded;

    public const string HumanValidationLimited = RiskWarning.HumanValidationLimited;

    public const string CsLabelCoverage = RiskWarning.CsLabelCoverageLimit;

    /// <summary>Model repo-ozel; baska bir depoya tasinmasi olculmedi.</summary>
    public const string RepositorySpecificModel = "REPOSITORY_SPECIFIC_MODEL";

    /// <summary>Repo-arasi aktarim tutarsiz cikti.</summary>
    public const string CrossRepositoryTransfer = "CROSS_REPOSITORY_TRANSFER_LIMIT";

    /// <summary>En yeni commit'ler henuz duzeltilmemis olabilir (sag sansurleme).</summary>
    public const string RightCensoring = "RIGHT_CENSORING";

    /// <summary>Bot commit'lerinin etkisi ayristirilmadi.</summary>
    public const string BotSensitivity = "BOT_SENSITIVITY";

    /// <summary>Dosya haritasindaki renk ortalama endeks; dosyanin kendi skoru degil.</summary>
    public const string FileMeanIndexDefinition = "FILE_MEAN_INDEX_DEFINITION";

    /// <summary>Rapor kismi bir sonuctan uretildi.</summary>
    public const string PartialResult = "PARTIAL_ANALYSIS_RESULT";

    /// <summary>Her raporda duran kodlar.</summary>
    public static readonly IReadOnlyList<string> Always =
    [
        Uncalibrated,
        SzzTarget,
        StaticNotIncluded,
        HumanValidationLimited,
        CsLabelCoverage,
        RepositorySpecificModel,
        CrossRepositoryTransfer,
        RightCensoring,
        BotSensitivity,
        FileMeanIndexDefinition,
    ];
}
