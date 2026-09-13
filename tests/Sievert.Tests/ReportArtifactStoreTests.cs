using System.Security.Cryptography;
using System.Text;

using Sievert.Api.Reports;
using Sievert.Contracts;

namespace Sievert.Tests;

/// <summary>
/// Artefakt deposunun testleri.
///
/// Buradaki sorular dosya sistemi sorulari degil, guven sorulari: yari yazilmis bir dosya
/// indirilebilir mi, kullanicidan gelen bir ad kok disina cikabilir mi, diskte degismis
/// bir dosya sessizce servis edilir mi.
/// </summary>
public sealed class ReportArtifactStoreTests : IDisposable
{
    private readonly string root = Path.Combine(
        Path.GetTempPath(), "sievert-rapor-test-" + Guid.NewGuid().ToString("n"));

    private readonly LocalReportArtifactStore store;

    public ReportArtifactStoreTests() => store = new LocalReportArtifactStore(root);

    [Fact]
    public async Task WritingReturnsTheLengthAndChecksumOfTheFile()
    {
        byte[] content = Encoding.UTF8.GetBytes("ornek pdf icerigi");
        string key = store.NewStorageKey(Guid.NewGuid());

        StoredArtifact stored = await store.WriteAsync(key, content, CancellationToken.None);

        Assert.Equal(content.Length, stored.ByteLength);
        Assert.Equal(Convert.ToHexStringLower(SHA256.HashData(content)), stored.Sha256);
    }

    [Fact]
    public async Task NoTemporaryFileSurvivesASuccessfulWrite()
    {
        string key = store.NewStorageKey(Guid.NewGuid());

        await store.WriteAsync(key, [1, 2, 3], CancellationToken.None);

        Assert.Empty(Directory.GetFiles(root, "*.sievert-tmp"));
        Assert.Single(Directory.GetFiles(root));
    }

    [Fact]
    public void TemporaryFilesAreCleanedAndNothingElseIsTouched()
    {
        Directory.CreateDirectory(root);

        File.WriteAllText(Path.Combine(root, "report-abc.pdf.sievert-tmp"), "yarim");
        File.WriteAllText(Path.Combine(root, "report-abc.pdf"), "tam");
        File.WriteAllText(Path.Combine(root, "baska-dosya.txt"), "dokunulmasin");

        Assert.Equal(1, store.CleanTemporaryFiles());
        Assert.True(File.Exists(Path.Combine(root, "report-abc.pdf")));
        Assert.True(File.Exists(Path.Combine(root, "baska-dosya.txt")));
        Assert.False(File.Exists(Path.Combine(root, "report-abc.pdf.sievert-tmp")));
    }

    [Theory]
    [InlineData("../disari.pdf")]
    [InlineData("alt/klasor.pdf")]
    [InlineData("..\\windows.pdf")]
    [InlineData("")]
    [InlineData("bos luk.pdf")]
    public async Task AKeyThatCouldEscapeTheRootIsRefused(string key)
    {
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => store.WriteAsync(key, [1], CancellationToken.None));
    }

    [Fact]
    public void TheGeneratedKeyContainsNoPathSeparatorOrControlCharacter()
    {
        string key = store.NewStorageKey(Guid.NewGuid());

        Assert.DoesNotContain('/', key);
        Assert.DoesNotContain('\\', key);
        Assert.All(key, character => Assert.False(char.IsControl(character)));
    }

    [Fact]
    public async Task VerificationPassesForAnUntouchedFile()
    {
        string key = store.NewStorageKey(Guid.NewGuid());
        StoredArtifact stored = await store.WriteAsync(key, [9, 8, 7], CancellationToken.None);

        ArtifactCheck check = await store.VerifyAsync(
            key, stored.ByteLength, stored.Sha256, CancellationToken.None);

        Assert.True(check.Ok);
        Assert.Null(check.ErrorCode);
    }

    [Fact]
    public async Task AChangedFileIsReportedAsCorrupted()
    {
        string key = store.NewStorageKey(Guid.NewGuid());
        StoredArtifact stored = await store.WriteAsync(key, [1, 2, 3], CancellationToken.None);

        // Tek bir bayt degisiyor; boyut ayni kaliyor, yani yalniz ozet yakalayabilir.
        await File.WriteAllBytesAsync(
            Path.Combine(root, key), [1, 2, 4], CancellationToken.None);

        ArtifactCheck check = await store.VerifyAsync(
            key, stored.ByteLength, stored.Sha256, CancellationToken.None);

        Assert.False(check.Ok);
        Assert.Equal(ApiError.ReportArtifactCorrupted, check.ErrorCode);
    }

    [Fact]
    public async Task ADeletedFileIsReportedAsCorrupted()
    {
        string key = store.NewStorageKey(Guid.NewGuid());
        StoredArtifact stored = await store.WriteAsync(key, [1], CancellationToken.None);

        store.Delete(key);

        ArtifactCheck check = await store.VerifyAsync(
            key, stored.ByteLength, stored.Sha256, CancellationToken.None);

        Assert.False(check.Ok);
        Assert.Equal(ApiError.ReportArtifactCorrupted, check.ErrorCode);
    }

    [Fact]
    public async Task AFileWithADifferentLengthIsReportedAsCorrupted()
    {
        string key = store.NewStorageKey(Guid.NewGuid());
        StoredArtifact stored = await store.WriteAsync(key, [1, 2, 3], CancellationToken.None);

        await File.WriteAllBytesAsync(
            Path.Combine(root, key), [1, 2, 3, 4], CancellationToken.None);

        Assert.False((await store.VerifyAsync(
            key, stored.ByteLength, stored.Sha256, CancellationToken.None)).Ok);
    }

    [Fact]
    public async Task WritingTwiceWithTheSameKeyReplacesTheFileAtomically()
    {
        string key = store.NewStorageKey(Guid.NewGuid());

        await store.WriteAsync(key, [1, 1, 1], CancellationToken.None);
        StoredArtifact second = await store.WriteAsync(key, [2, 2], CancellationToken.None);

        Assert.Equal(2, second.ByteLength);
        Assert.Single(Directory.GetFiles(root));
    }

    [Theory]
    [InlineData("polly-full", false, "sievert-polly-full-20260913.pdf")]
    [InlineData("polly-full", true, "sievert-polly-full-partial-20260913.pdf")]
    [InlineData("Repo With Spaces", false, "sievert-repo-with-spaces-20260913.pdf")]
    public void TheFileNameIsSanitisedAndDated(string repository, bool partial, string expected)
    {
        string name = ReportFileName.For(
            repository, new DateTimeOffset(2026, 9, 13, 10, 0, 0, TimeSpan.Zero), partial);

        Assert.Equal(expected, name);
    }

    [Fact]
    public void AFileNameCannotCarryAHeaderInjection()
    {
        string name = ReportFileName.For(
            "repo" + (char)13 + (char)10 + "X-Injected: 1", DateTimeOffset.UnixEpoch, false);

        Assert.All(name, character => Assert.False(char.IsControl(character)));
        Assert.DoesNotContain(':', name);
        Assert.DoesNotContain('"', name);
        Assert.DoesNotContain('/', name);
    }

    [Fact]
    public void AnEmptyRepositoryNameStillProducesAUsableFileName()
    {
        string name = ReportFileName.For("///", DateTimeOffset.UnixEpoch, false);

        Assert.StartsWith("sievert-repo-", name, StringComparison.Ordinal);
        Assert.EndsWith(".pdf", name, StringComparison.Ordinal);
    }

    public void Dispose()
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
