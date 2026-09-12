namespace Sievert.Modeling;

/// <summary>
/// Modele hangi alanin girip hangisinin girmedigi. Liste burada duruyor ve
/// <see cref="EnsureNoExcluded"/> ile siniliyor; gerekcesi ADR 0016'da.
///
/// Disarida birakilan alanlarin her birinin ayri bir sebebi var ve hepsi ayni sonuca
/// cikiyor: girerlerse model olculen basariyi gercekte olmayan bir yerden alir.
/// </summary>
public static class ModelFeatures
{
    /// <summary>ADR 0013'teki 15 commit olcusu. Bu adimda eleme ya da donusum yok.</summary>
    public static readonly IReadOnlyList<string> Candidates =
    [
        "LinesAdded",
        "LinesDeleted",
        "FilesChanged",
        "CsFilesChanged",
        "Entropy",
        "DirectoryCount",
        "SubsystemCount",
        "MaxFileAgeDays",
        "MinFileAgeDays",
        "PriorChanges",
        "PriorFixes",
        "DistinctAuthorsOnFiles",
        "AuthorCommitCount",
        "AuthorFileExperience",
        "IsFix",
    ];

    /// <summary>
    /// Modele girmesi yasak alanlar ve sebepleri. <c>BotMu</c> ile <c>IsBot</c> ayni
    /// alanin iki adi (CSV basligi / C# ozelligi); ikisi de yaziliyor ki hangi adla
    /// gelirse gelsin yakalansin.
    /// </summary>
    public static readonly Dictionary<string, string> Excluded = new(StringComparer.Ordinal)
    {
        ["IsBugIntroducing"] = "hedef degisken, oznitelik degil",
        ["LabelSource"] = "denetim alani; dolu olup olmamasi hedefin kendisi",
        ["Sha"] = "kimlik alani",
        ["Repository"] = "kimlik alani, ustelik makineye ozel klasor adi",
        ["RepositoryIdentity"] = "gruplama alani; ayni-repo deneyinde sabit sutun",
        ["AuthorDateUtc"] = "bolme icin; takvim tarihi sag sansuru ogretir",
        ["BotMu"] = "bot duyarlilik deneyi Adim 4'te, ana modelde yok",
        ["IsBot"] = "bot duyarlilik deneyi Adim 4'te, ana modelde yok",
    };

    /// <summary>
    /// Verilen ad listesinde yasak bir alan varsa durur. Model kuran her yol buradan
    /// gecmeli; liste elle yazilmis bir belge degil, calisan bir kontrol olsun diye var.
    /// </summary>
    public static void EnsureNoExcluded(IEnumerable<string> names)
    {
        List<string> found = [.. names.Where(Excluded.ContainsKey)];

        if (found.Count == 0)
        {
            return;
        }

        throw new InvalidOperationException(
            "Modele girmesi yasak alan(lar) oznitelik listesinde: "
            + string.Join(", ", found.Select(name => $"{name} ({Excluded[name]})")));
    }

    /// <summary>
    /// Tek bir ozniteligin degeri, adiyla. Yasak bir ad verilirse durur; tahmin ureten
    /// her yol buradan gectigi icin hedef ya da kimlik alani yanlislikla tahmin
    /// girdisi olamiyor.
    /// </summary>
    public static double Value(SnapshotRow row, string name)
    {
        EnsureNoExcluded([name]);

        for (int index = 0; index < Candidates.Count; index++)
        {
            if (string.Equals(Candidates[index], name, StringComparison.Ordinal))
            {
                return Values(row)[index];
            }
        }

        throw new InvalidOperationException($"Boyle bir aday oznitelik yok: {name}");
    }

    /// <summary>Aday ozniteliklerin degerleri, <see cref="Candidates"/> ile ayni sirada.</summary>
    public static IReadOnlyList<double> Values(SnapshotRow row) =>
    [
        row.LinesAdded,
        row.LinesDeleted,
        row.FilesChanged,
        row.CsFilesChanged,
        row.Entropy,
        row.DirectoryCount,
        row.SubsystemCount,
        row.MaxFileAgeDays,
        row.MinFileAgeDays,
        row.PriorChanges,
        row.PriorFixes,
        row.DistinctAuthorsOnFiles,
        row.AuthorCommitCount,
        row.AuthorFileExperience,
        row.IsFix ? 1 : 0,
    ];
}
