namespace Sievert.Api;

/// <summary>
/// Risk cevabindaki uyari kodlari ve karsilik gelen aciklamalar.
/// Kodlar ve ne zaman cikacaklari docs/urun/risk-sozlesmesi.md icinde sabit.
/// </summary>
public static class RiskWarning
{
    public const string UncalibratedScore = "UNCALIBRATED_SCORE";

    public const string SzzTarget = "SZZ_TARGET";

    public const string StaticAnalysisNotIncluded = "STATIC_ANALYSIS_NOT_INCLUDED";

    public const string HumanValidationLimited = "HUMAN_VALIDATION_LIMITED";

    public const string CsLabelCoverageLimit = "CS_LABEL_COVERAGE_LIMIT";

    public const string OutsideTrainRange = "OUTSIDE_TRAIN_RANGE";

    public const string ExternalModelProfile = "EXTERNAL_MODEL_PROFILE";

    public const string UnknownRepositoryModel = "UNKNOWN_REPOSITORY_MODEL";

    /// <summary>Her risk cevabinda bulunmasi zorunlu olan uyarilar.</summary>
    public static readonly IReadOnlyList<string> Always =
    [
        UncalibratedScore,
        SzzTarget,
        StaticAnalysisNotIncluded,
        HumanValidationLimited,
    ];

    private static readonly Dictionary<string, string> Texts = new(StringComparer.Ordinal)
    {
        // Metinler yasak ifadeleri olumsuzlayarak bile ICERMIYOR. Sebep pratik: yasagi
        // sinayan test metnin icinde geciyor mu diye bakiyor, cumlenin anlamina degil.
        [UncalibratedScore] =
            "Ham model skoru kalibre edilmedi. Sayiyi yuzde gibi okuma: ayni modelin commit'leri "
            + "kendi arasinda siralamasi icin uretildi, gercek hata oranina karsi dogrulanmadi.",
        [SzzTarget] =
            "Model, SZZ ile uretilmis bir hedefe gore egitildi. Hedef, gercek hatalarin kendisi "
            + "degil, duzeltme commit'lerinden geriye dogru isaretlenmis satirlardir.",
        [StaticAnalysisNotIncluded] =
            "Statik analiz bulgulari model skoruna girmiyor. Bulgu bolumu ayri durur ve "
            + "skoru degistirmez.",
        [HumanValidationLimited] =
            "Kor insan ornekleminde model-pozitif isareti 14 ornekte 1 idi. Bu bir populasyon "
            + "precision'i degil, ama skorun gercek hata orani gibi okunmasini engelleyen bir sinirliliktir.",
        [CsLabelCoverageLimit] =
            "Bu commit hic C# dosyasi degistirmiyor. Egitim verisinde bu grupta 8607 satirin "
            + "hicbiri pozitif etiketli degil; skor bu grup icin dogrulanmadi.",
        [OutsideTrainRange] =
            "En az bir oznitelik degeri egitimde gorulen araligin disinda. Deger kirpilmadi, "
            + "skor oldugu gibi verildi; bu bolgede modelin davranisi olculmedi.",
        [ExternalModelProfile] =
            "Kullanilan model profili bu commit'in reposundan farkli bir repoda egitildi. "
            + "Repo-arasi aktarim deneylerinde sonuc tutarli cikmadi.",
        [UnknownRepositoryModel] =
            "Bu depo icin egitilmis bir model profili yok ve varsayilan profil secilmiyor.",
    };

    public static string Text(string code) => Texts.TryGetValue(code, out string? text)
        ? text
        : throw new ArgumentOutOfRangeException(nameof(code), code, "Tanimsiz uyari kodu.");

    /// <summary>Verilen kodlarin aciklama metinleri, kod sirasinda.</summary>
    public static IReadOnlyList<string> Describe(IEnumerable<string> codes) => [.. codes.Select(Text)];
}
