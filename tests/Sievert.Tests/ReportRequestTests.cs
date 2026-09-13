using Sievert.Api.Reports;
using Sievert.Contracts;

namespace Sievert.Tests;

/// <summary>
/// Rapor istegi dogrulamasinin testleri.
///
/// Sinirlar sozlesmede yaziyor; buradaki testler sozlesmenin kodla ayni seyi soyledigini
/// kontrol ediyor. Sessizce kirpma yok: sinirin disindaki istek reddediliyor.
/// </summary>
public sealed class ReportRequestTests
{
    [Fact]
    public void DefaultsMatchTheContract()
    {
        ResolvedReportRequest request = Valid(new ReportRequest(Guid.NewGuid()));

        Assert.Equal(ReportLimits.DefaultCommitWindow, request.CommitWindow);
        Assert.Equal(ReportLimits.DefaultFileLimit, request.FileLimit);
        Assert.Equal(ReportLimits.DefaultTimelineCount, request.TimelineCount);
        Assert.Equal(ReportLimits.DefaultTopCommitCount, request.TopCommitCount);
        Assert.Equal(ReportLimits.DefaultFindingLimit, request.FindingLimit);
        Assert.Equal(ReportCulture.Turkish, request.Culture);
        Assert.False(request.IncludePartial);
    }

    [Theory]
    [InlineData(49)]
    [InlineData(1001)]
    public void ACommitWindowOutsideTheRangeIsRefused(int window)
    {
        ReportValidation result = ReportRequestValidator.Validate(
            new ReportRequest(Guid.NewGuid(), CommitWindow: window));

        Assert.Null(result.Request);
        Assert.Equal(ApiError.ReportParameterInvalid, result.ErrorCode);
    }

    [Theory]
    [InlineData(9)]
    [InlineData(101)]
    public void AFileLimitOutsideTheRangeIsRefused(int limit)
    {
        Assert.Equal(
            ApiError.ReportParameterInvalid,
            ReportRequestValidator.Validate(new ReportRequest(Guid.NewGuid(), FileLimit: limit)).ErrorCode);
    }

    [Theory]
    [InlineData(19)]
    [InlineData(501)]
    public void ATimelineCountOutsideTheRangeIsRefused(int count)
    {
        Assert.Equal(
            ApiError.ReportParameterInvalid,
            ReportRequestValidator.Validate(new ReportRequest(Guid.NewGuid(), TimelineCount: count)).ErrorCode);
    }

    [Theory]
    [InlineData(4)]
    [InlineData(101)]
    public void ATopCommitCountOutsideTheRangeIsRefused(int count)
    {
        Assert.Equal(
            ApiError.ReportParameterInvalid,
            ReportRequestValidator.Validate(new ReportRequest(Guid.NewGuid(), TopCommitCount: count)).ErrorCode);
    }

    [Fact]
    public void AFindingLimitOfZeroIsAllowed()
    {
        // Sifir gecerli bir secim: "bulgulari sayiyla ozetle, listeleme".
        Assert.Equal(0, Valid(new ReportRequest(Guid.NewGuid(), FindingLimit: 0)).FindingLimit);
    }

    [Fact]
    public void AnUnsupportedCultureIsRefused()
    {
        Assert.Equal(
            ApiError.ReportCultureNotSupported,
            ReportRequestValidator.Validate(new ReportRequest(Guid.NewGuid(), Culture: "de-DE")).ErrorCode);
    }

    [Theory]
    [InlineData("tr-tr")]
    [InlineData("EN-US")]
    public void CultureMatchingIsCaseInsensitiveButNormalised(string culture)
    {
        string resolved = Valid(new ReportRequest(Guid.NewGuid(), Culture: culture)).Culture;

        Assert.Contains(resolved, ReportCulture.Supported);
        Assert.Equal(resolved, ReportCulture.Normalize(culture));
    }

    [Fact]
    public void ATooLongTitleIsRefusedRatherThanTruncated()
    {
        ReportValidation result = ReportRequestValidator.Validate(
            new ReportRequest(Guid.NewGuid(), Title: new string('a', ReportLimits.MaximumTitleLength + 1)));

        Assert.Null(result.Request);
        Assert.Equal(ApiError.ReportParameterInvalid, result.ErrorCode);
    }

    [Fact]
    public void ATooLongNoteIsRefusedRatherThanTruncated()
    {
        ReportValidation result = ReportRequestValidator.Validate(
            new ReportRequest(Guid.NewGuid(), Notes: new string('a', ReportLimits.MaximumNotesLength + 1)));

        Assert.Null(result.Request);
        Assert.Equal(ApiError.ReportParameterInvalid, result.ErrorCode);
    }

    [Fact]
    public void ControlCharactersAreStrippedFromTheTitle()
    {
        string raw = "iki" + (char)13 + (char)10 + "satir" + (char)9 + "ve" + (char)0 + "bos";

        ResolvedReportRequest request = Valid(new ReportRequest(Guid.NewGuid(), Title: raw));

        Assert.NotNull(request.Title);
        Assert.All(request.Title, character => Assert.False(char.IsControl(character)));
    }

    [Fact]
    public void MarkupInTheTitleStaysPlainText()
    {
        // PDF isaretleme dili yorumlamiyor; metin oldugu gibi kaliyor.
        ResolvedReportRequest request = Valid(new ReportRequest(
            Guid.NewGuid(), Title: "<b>kalin</b> **yildiz**"));

        Assert.Equal("<b>kalin</b> **yildiz**", request.Title);
    }

    [Fact]
    public void AnEmptyTitleBecomesNull()
    {
        Assert.Null(Valid(new ReportRequest(Guid.NewGuid(), Title: "   ")).Title);
    }

    [Fact]
    public void AMissingJobIdIsRefused()
    {
        Assert.Equal(
            ApiError.ReportRequestInvalid,
            ReportRequestValidator.Validate(new ReportRequest(Guid.Empty)).ErrorCode);

        Assert.Equal(
            ApiError.ReportRequestInvalid,
            ReportRequestValidator.Validate(null).ErrorCode);
    }

    [Fact]
    public void TheFingerprintChangesWithEveryField()
    {
        ResolvedReportRequest baseline = Valid(new ReportRequest(Guid.Parse(
            "11111111-1111-1111-1111-111111111111")));

        string first = baseline.Fingerprint();

        Assert.Equal(first, baseline.Fingerprint());
        Assert.NotEqual(first, (baseline with { CommitWindow = 500 }).Fingerprint());
        Assert.NotEqual(first, (baseline with { Culture = "en-US" }).Fingerprint());
        Assert.NotEqual(first, (baseline with { Title = "x" }).Fingerprint());
        Assert.NotEqual(first, (baseline with { IncludePartial = true }).Fingerprint());
    }

    private static ResolvedReportRequest Valid(ReportRequest request)
    {
        ReportValidation result = ReportRequestValidator.Validate(request);

        Assert.Null(result.ErrorCode);
        Assert.NotNull(result.Request);

        return result.Request;
    }
}
