namespace Sievert.Analysis;

/// <summary>Verilen yolun altindaki taranacak .cs dosyalarini bulur.</summary>
public static class KaynakDosyaBulucu
{
    /// <summary>Icine hic girilmeyen klasorler.</summary>
    private static readonly string[] AtlanacakKlasorler = ["bin", "obj", ".git", "node_modules"];

    /// <summary>
    /// Yol bir dosyaysa (ve .cs ise) onu, klasorse altindaki butun .cs dosyalarini dondurur.
    /// Sonuc her calistirmada ayni sirada gelsin diye siralanir.
    /// </summary>
    public static IReadOnlyList<string> Bul(string yol)
    {
        if (File.Exists(yol))
        {
            return CsDosyasiMi(yol) ? [yol] : [];
        }

        if (!Directory.Exists(yol))
        {
            return [];
        }

        List<string> bulunanlar = [];
        KlasoruGez(yol, bulunanlar);
        bulunanlar.Sort(StringComparer.Ordinal);
        return bulunanlar;
    }

    /// <summary>Klasor adi atlanacaklar listesinde mi.</summary>
    public static bool AtlanacakKlasorMu(string klasorAdi) =>
        AtlanacakKlasorler.Contains(klasorAdi, StringComparer.OrdinalIgnoreCase);

    private static void KlasoruGez(string klasor, List<string> bulunanlar)
    {
        bulunanlar.AddRange(Directory.EnumerateFiles(klasor, "*.cs").Where(CsDosyasiMi));

        foreach (string altKlasor in Directory.EnumerateDirectories(klasor))
        {
            if (!AtlanacakKlasorMu(Path.GetFileName(altKlasor)))
            {
                KlasoruGez(altKlasor, bulunanlar);
            }
        }
    }

    private static bool CsDosyasiMi(string yol) =>
        Path.GetExtension(yol).Equals(".cs", StringComparison.OrdinalIgnoreCase);
}
