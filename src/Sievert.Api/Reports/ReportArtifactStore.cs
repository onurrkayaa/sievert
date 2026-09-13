using System.Globalization;
using System.Security.Cryptography;
using System.Text;

using Sievert.Contracts;

namespace Sievert.Api.Reports;

/// <summary>Diske yazilmis bir artefaktin olculen ozellikleri.</summary>
/// <param name="ByteLength">Yeniden adlandirmadan sonra okunan uzunluk.</param>
/// <param name="Sha256">Dosyanin ozeti, kucuk harf onaltilik.</param>
public sealed record StoredArtifact(long ByteLength, string Sha256);

/// <summary>Artefakt dogrulamasinin sonucu.</summary>
/// <param name="Ok">Dosya var, boyutu ve ozeti kayitla ayni.</param>
/// <param name="ErrorCode">Uymuyorsa hangi kodla reddedildigi.</param>
public sealed record ArtifactCheck(bool Ok, string? ErrorCode);

/// <summary>
/// Uretilen rapor dosyalarinin durdugu yer.
///
/// Arayuz olmasinin sebebi ileride S3 benzeri bir depo eklemek degil; **testin gercek
/// diske yazmadan kosabilmesi** ve depolama kurallarinin tek yerde toplanmasi.
/// </summary>
public interface IReportArtifactStore
{
    /// <summary>Yeni bir depolama anahtari uretir. Kullanici girdisinden turetmez.</summary>
    string NewStorageKey(Guid reportId);

    /// <summary>
    /// Icerigi once gecici dosyaya yazar, ozetini hesaplar, sonra atomik olarak
    /// yerine tasir. Yari yazilmis bir dosya hicbir zaman nihai adiyla gorunmez.
    /// </summary>
    Task<StoredArtifact> WriteAsync(string storageKey, byte[] content, CancellationToken token);

    /// <summary>Dosya var mi, boyutu ve ozeti bekleneni tutuyor mu.</summary>
    Task<ArtifactCheck> VerifyAsync(string storageKey, long expectedLength, string expectedSha256, CancellationToken token);

    /// <summary>Dogrulanmis dosyanin icerigi. Dogrulamadan cagirilmamali.</summary>
    Task<byte[]> ReadAsync(string storageKey, CancellationToken token);

    /// <summary>Yarida kalmis gecici dosyalari siler ve kac tane sildigini doner.</summary>
    int CleanTemporaryFiles();

    /// <summary>Bir artefakti siler. Yalniz basarisiz uretim temizliginde kullaniliyor.</summary>
    void Delete(string storageKey);
}

/// <summary>Yerel dosya sistemine yazan uygulama.</summary>
public sealed class LocalReportArtifactStore : IReportArtifactStore
{
    private const string TemporarySuffix = ".sievert-tmp";

    private readonly string root;

    /// <param name="root">
    /// Artefakt koku. Acilista mutlaklastiriliyor: goreli bir kok, surecin calisma
    /// dizinine gore kayardi ve iki farkli yerde iki ayri rapor klasoru olusurdu.
    /// </param>
    public LocalReportArtifactStore(string root)
    {
        this.root = Path.GetFullPath(root);
        Directory.CreateDirectory(this.root);
    }

    /// <summary>
    /// Depolama anahtari: rapor kimliginden turetilmis sabit bir ad.
    ///
    /// Kullanicinin verdigi baslik ya da dosya adi buraya **girmiyor**. Girseydi
    /// "../../" iceren bir baslik kok disina yazdirabilirdi; bu yuzden anahtar
    /// tamamen uygulamanin urettigi bir kimlik.
    /// </summary>
    public string NewStorageKey(Guid reportId) =>
        "report-" + reportId.ToString("n", CultureInfo.InvariantCulture) + ".pdf";

    public async Task<StoredArtifact> WriteAsync(string storageKey, byte[] content, CancellationToken token)
    {
        string target = Resolve(storageKey);
        string temporary = target + TemporarySuffix;

        try
        {
            await using (FileStream stream = new(
                temporary, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await stream.WriteAsync(content, token);
                await stream.FlushAsync(token);
            }

            string checksum = Convert.ToHexStringLower(SHA256.HashData(content));

            // Atomik yer degistirme: bu satirdan once nihai ad hic var olmuyor, o yuzden
            // yarim bir dosyayi indirmek mumkun degil.
            File.Move(temporary, target, overwrite: true);

            FileInfo written = new(target);

            return new StoredArtifact(written.Length, checksum);
        }
        catch
        {
            Remove(temporary);

            throw;
        }
    }

    public Task<ArtifactCheck> VerifyAsync(
        string storageKey, long expectedLength, string expectedSha256, CancellationToken token)
    {
        string path = Resolve(storageKey);

        if (!File.Exists(path))
        {
            return Task.FromResult(new ArtifactCheck(false, ApiError.ReportArtifactCorrupted));
        }

        FileInfo file = new(path);

        if (file.Length != expectedLength)
        {
            return Task.FromResult(new ArtifactCheck(false, ApiError.ReportArtifactCorrupted));
        }

        if (file.Length > ReportLimits.MaximumArtifactBytes)
        {
            return Task.FromResult(new ArtifactCheck(false, ApiError.ReportArtifactTooLarge));
        }

        byte[] content = File.ReadAllBytes(path);
        string checksum = Convert.ToHexStringLower(SHA256.HashData(content));

        return Task.FromResult(string.Equals(checksum, expectedSha256, StringComparison.Ordinal)
            ? new ArtifactCheck(true, null)
            : new ArtifactCheck(false, ApiError.ReportArtifactCorrupted));
    }

    public async Task<byte[]> ReadAsync(string storageKey, CancellationToken token) =>
        await File.ReadAllBytesAsync(Resolve(storageKey), token);

    /// <summary>
    /// Yalniz bu deponun kokundeki, bu uygulamanin gecici desenine uyan dosyalari siler.
    /// Baska hicbir dosyaya dokunmuyor: bir temizlik rutini yanlis yerde kosarsa
    /// kullanicinin dosyalarini silmis olur.
    /// </summary>
    public int CleanTemporaryFiles()
    {
        int removed = 0;

        foreach (string path in Directory.EnumerateFiles(root, "*" + TemporarySuffix, SearchOption.TopDirectoryOnly))
        {
            if (Remove(path))
            {
                removed++;
            }
        }

        return removed;
    }

    public void Delete(string storageKey) => Remove(Resolve(storageKey));

    /// <summary>
    /// Anahtari kok icindeki bir yola cevirir ve kok disina cikmadigini dogrular.
    ///
    /// Iki katmanli kontrol var: once anahtarin bicimi (yalniz beklenen karakterler),
    /// sonra cozulen yolun gercekten kokun altinda kalmasi. Ilki yeterli gorunuyor ama
    /// tek basina birakmak, bicim kontrolunun ileride gevsemesi halinde sessizce kok
    /// disina yazmak demek olurdu.
    /// </summary>
    private string Resolve(string storageKey)
    {
        if (string.IsNullOrWhiteSpace(storageKey)
            || storageKey.Any(character => !(char.IsAsciiLetterOrDigit(character) || character is '-' or '.')))
        {
            throw new InvalidOperationException("Gecersiz depolama anahtari.");
        }

        if (storageKey.Contains("..", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Gecersiz depolama anahtari.");
        }

        string path = Path.GetFullPath(Path.Combine(root, storageKey));

        if (!path.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Depolama anahtari kok disina cikiyor.");
        }

        return path;
    }

    private static bool Remove(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);

                return true;
            }
        }
        catch (IOException)
        {
            // Baska bir surec tutuyorsa birakiliyor; bir sonraki acilista denenir.
        }
        catch (UnauthorizedAccessException)
        {
        }

        return false;
    }
}

/// <summary>Dosya adini guvenli hale getiren yardimci.</summary>
public static class ReportFileName
{
    /// <summary>
    /// Depo adindan indirme adi uretir.
    ///
    /// Yol ayraci, kontrol karakteri, tirnak ve satir sonu cikariliyor. Sebep yalniz
    /// dosya sistemi degil: bu ad <c>Content-Disposition</c> basligina giriyor ve
    /// icinde satir sonu olan bir ad baslik enjeksiyonu demek.
    /// </summary>
    public static string For(string repositoryName, DateTimeOffset stamp, bool partial)
    {
        StringBuilder cleaned = new();

        foreach (char character in repositoryName.ToLowerInvariant())
        {
            if (char.IsAsciiLetterOrDigit(character))
            {
                cleaned.Append(character);
            }
            else if (character is '-' or '_' or ' ' or '.')
            {
                cleaned.Append('-');
            }
        }

        string name = cleaned.ToString().Trim('-');

        while (name.Contains("--", StringComparison.Ordinal))
        {
            name = name.Replace("--", "-", StringComparison.Ordinal);
        }

        if (name.Length == 0)
        {
            name = "repo";
        }

        if (name.Length > 60)
        {
            name = name[..60].Trim('-');
        }

        string suffix = partial ? "-partial" : string.Empty;

        return $"sievert-{name}{suffix}-{stamp.UtcDateTime:yyyyMMdd}.pdf";
    }
}
