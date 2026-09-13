using System.Globalization;
using System.Text;

using Sievert.Api.Reports;

namespace Sievert.Tests;

/// <summary>
/// Canonical manifestin testleri.
///
/// Manifest raporun kimligi: ayni girdi ayni baytlari vermezse "bu PDF su veriden
/// uretildi" cumlesi dogrulanamaz hale gelir. O yuzden testlerin cogu esitlik degil,
/// **bayt esitligi** kontrol ediyor.
/// </summary>
public sealed class ReportManifestTests
{
    [Fact]
    public void TheSameInputProducesTheSameBytes()
    {
        string first = ReportManifest.Canonical(Input());
        string second = ReportManifest.Canonical(Input());

        Assert.Equal(first, second, StringComparer.Ordinal);
        Assert.Equal(ReportManifest.Bytes(first), ReportManifest.Bytes(second));
        Assert.Equal(ReportManifest.Checksum(first), ReportManifest.Checksum(second));
    }

    [Fact]
    public void TheGenerationTimeIsNotPartOfTheChecksum()
    {
        // Manifestte uretim zamani alani yok; olsaydi ayni girdi iki farkli ozet verirdi.
        string canonical = ReportManifest.Canonical(Input());

        Assert.DoesNotContain("generatedAt", canonical, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("reportId", canonical, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("title")]
    [InlineData("notes")]
    [InlineData("culture")]
    [InlineData("window")]
    [InlineData("fileLimit")]
    public void ADifferentParameterProducesADifferentChecksum(string change)
    {
        string baseline = ReportManifest.Checksum(ReportManifest.Canonical(Input()));

        ReportManifestInput changed = change switch
        {
            "title" => Input() with { Title = "baska baslik" },
            "notes" => Input() with { Notes = "baska not" },
            "culture" => Input() with { Culture = "en-US" },
            "window" => Input() with { CommitWindow = 500 },
            _ => Input() with { FileLimit = 100 },
        };

        Assert.NotEqual(baseline, ReportManifest.Checksum(ReportManifest.Canonical(changed)));
    }

    [Fact]
    public void TheManifestHasNoByteOrderMarkAndNoCarriageReturn()
    {
        byte[] bytes = ReportManifest.Bytes(ReportManifest.Canonical(Input()));

        Assert.False(bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF);
        Assert.DoesNotContain((byte)'\r', bytes);
    }

    [Fact]
    public void TimesAreWrittenInUtcRegardlessOfTheOffset()
    {
        ReportManifestInput shifted = Input() with
        {
            RiskCompletedAtUtc = new DateTimeOffset(2026, 1, 2, 15, 0, 0, TimeSpan.FromHours(3)),
        };

        Assert.Contains("2026-01-02T12:00:00.0000000Z", ReportManifest.Canonical(shifted), StringComparison.Ordinal);
    }

    [Fact]
    public void DecimalsUseTheInvariantSeparatorEvenInATurkishCulture()
    {
        CultureInfo original = CultureInfo.CurrentCulture;

        try
        {
            // Sunucunun kulturu virgullu ondalik kullandiginda manifestin degismemesi
            // gerekiyor: ayni veri iki makinede ayni ozeti vermeli.
            CultureInfo.CurrentCulture = new CultureInfo("tr-TR");

            Assert.Contains("0.2381", ReportManifest.Canonical(Input()), StringComparison.Ordinal);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Fact]
    public void LimitationCodesAreSortedSoTheirOrderCannotChangeTheChecksum()
    {
        string first = ReportManifest.Canonical(Input() with
        {
            LimitationCodes = ["SZZ_TARGET", "UNCALIBRATED_SCORE"],
        });

        string second = ReportManifest.Canonical(Input() with
        {
            LimitationCodes = ["UNCALIBRATED_SCORE", "SZZ_TARGET", "SZZ_TARGET"],
        });

        Assert.Equal(first, second, StringComparer.Ordinal);
    }

    [Fact]
    public void TheManifestCarriesNoPathOrConnectionString()
    {
        string canonical = ReportManifest.Canonical(Input());

        Assert.DoesNotContain("/Users/", canonical, StringComparison.Ordinal);
        Assert.DoesNotContain("Password=", canonical, StringComparison.Ordinal);
        Assert.DoesNotContain("Host=", canonical, StringComparison.Ordinal);
        Assert.DoesNotContain("localPath", canonical, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AReportWithoutStaticAnalysisWritesNullNotZero()
    {
        // "Statik analiz calistirilmadi" ile "sifir bulgu bulundu" ayni sey degil.
        Assert.Contains("\"staticAnalysis\":null", ReportManifest.Canonical(Input()), StringComparison.Ordinal);
    }

    [Fact]
    public void TheChecksumIsLowercaseHexOf64Characters()
    {
        string checksum = ReportManifest.Checksum(ReportManifest.Canonical(Input()));

        Assert.Equal(64, checksum.Length);
        Assert.Equal(checksum.ToLowerInvariant(), checksum, StringComparer.Ordinal);
        Assert.All(checksum, character => Assert.True(Uri.IsHexDigit(character)));
    }

    [Fact]
    public void TheCanonicalTextIsValidUtf8Json()
    {
        string canonical = ReportManifest.Canonical(Input() with { Title = "Turkce baslik: ölçüm ığ" });

        using System.Text.Json.JsonDocument document = System.Text.Json.JsonDocument.Parse(canonical);

        Assert.Equal(
            "Turkce baslik: ölçüm ığ",
            document.RootElement.GetProperty("reportParameters").GetProperty("title").GetString());

        Assert.Equal(canonical, Encoding.UTF8.GetString(ReportManifest.Bytes(canonical)), StringComparer.Ordinal);
    }

    private static ReportManifestInput Input() => new()
    {
        RepositoryId = 7,
        RepositoryIdentity = "github.com/app-vnext/polly",
        RepositoryDisplayName = "polly-full",
        Culture = "tr-TR",
        IncludePartial = false,
        CommitWindow = 200,
        FileLimit = 50,
        TimelineCount = 100,
        TopCommitCount = 20,
        FindingLimit = 50,
        Title = null,
        Notes = null,
        RiskJobId = Guid.Parse("11111111-2222-3333-4444-555555555555"),
        RiskJobStatus = "succeeded",
        RiskJobComplete = true,
        RankingScope = "complete-analysis",
        RiskRequestedAtUtc = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
        RiskStartedAtUtc = new DateTimeOffset(2026, 1, 1, 0, 0, 1, TimeSpan.Zero),
        RiskCompletedAtUtc = new DateTimeOffset(2026, 1, 1, 0, 0, 2, TimeSpan.Zero),
        RiskResultCount = 2759,
        ModelProfile = "polly",
        ModelCodeCommit = "3a4c2ce",
        ModelChecksum = new string('a', 64),
        TrainThreshold = 0.2381,
        ScoreReferenceChecksum = new string('b', 64),
        ModelResultsChecksum = new string('c', 64),
        LimitationCodes = ReportLimitation.Always,
        SievertVersion = "sievert-report/1.0",
    };
}
