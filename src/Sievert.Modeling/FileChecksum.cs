using System.Globalization;
using System.Security.Cryptography;

namespace Sievert.Modeling;

/// <summary>
/// Dosya ozeti. <c>shasum -a 256</c> ciktisiyla ayni bicimde yaziliyor ki kayitli deger
/// kabuktan da dogrulanabilsin.
/// </summary>
public static class FileChecksum
{
    public static string Sha256(string path)
    {
        using FileStream stream = File.OpenRead(path);

        return Convert.ToHexStringLower(SHA256.HashData(stream));
    }

    /// <summary>Dosyanin ozeti kayitli degerle ayni degilse okuma degil, hata.</summary>
    public static void Verify(string path, string checksumPath)
    {
        string recorded = Recorded(checksumPath);
        string actual = Sha256(path);

        if (!string.Equals(recorded, actual, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException(
                $"Dosyanin SHA-256 degeri kayitli degerle ayni degil: {path}{Environment.NewLine}"
                + $"  kayitli: {recorded}{Environment.NewLine}"
                + $"  okunan : {actual}");
        }
    }

    /// <summary>Satir bicimi: <c>&lt;ozet&gt;  &lt;dosya adi&gt;</c>.</summary>
    private static string Recorded(string checksumPath)
    {
        string text = File.ReadAllText(checksumPath).Trim();
        int space = text.IndexOf(' ', StringComparison.Ordinal);
        string digest = space < 0 ? text : text[..space];

        return digest.Length == 64
            ? digest
            : throw new InvalidDataException(
                $"Ozet dosyasi 64 karakterlik bir SHA-256 degeri tasimiyor: {checksumPath}");
    }

    /// <summary>Ozet dosyasinin icerigi; yazan taraf icin.</summary>
    public static string Line(string path) =>
        string.Create(CultureInfo.InvariantCulture, $"{Sha256(path)}  {Path.GetFileName(path)}{'\n'}");
}
