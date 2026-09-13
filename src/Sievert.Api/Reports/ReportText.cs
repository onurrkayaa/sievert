using System.Globalization;

using Sievert.Contracts;

namespace Sievert.Api.Reports;

/// <summary>
/// Raporun metinleri, kultur basina.
///
/// Ceviri dosyasi degil, iki elle yazilmis metin kumesi: rapor iki dilde de ayni seyi
/// soylemeli ve ozellikle sinirlilik cumleleri makine cevirisine birakilamayacak kadar
/// onemli. Uc dil gerekirse burasi bir ceviri altyapisina donusur.
/// </summary>
public sealed class ReportText
{
    private ReportText(string culture)
    {
        Culture = culture;
        Formatting = CultureInfo.GetCultureInfo(culture);
    }

    public string Culture { get; }

    /// <summary>Sayi bicimleri bu kulturden geliyor; metin secimi de ayni yerden.</summary>
    public CultureInfo Formatting { get; }

    public bool Turkish => Culture == ReportCulture.Turkish;

    public static ReportText For(string culture) => new(culture);

    public string Product => "Sievert";

    public string DocumentTitle => Turkish ? "Teknik analiz raporu" : "Technical analysis report";

    public string Repository => Turkish ? "Depo" : "Repository";

    public string RepositoryIdentity => Turkish ? "Depo kimliği" : "Repository identity";

    public string ReportId => Turkish ? "Rapor kimliği" : "Report id";

    public string GeneratedAt => Turkish ? "Üretim zamanı (UTC)" : "Generated at (UTC)";

    public string RiskJob => Turkish ? "Risk analizi işi" : "Risk analysis job";

    public string StaticJob => Turkish ? "Statik analiz işi" : "Static analysis job";

    public string NotIncluded => Turkish ? "Eklenmedi" : "Not included";

    public string ModelProfile => Turkish ? "Model profili" : "Model profile";

    public string ModelChecksum => Turkish ? "Model özeti" : "Model checksum";

    public string Calibration => Turkish ? "Kalibrasyon" : "Calibration";

    public string NotCalibrated => Turkish ? "Kalibre edilmedi" : "Not calibrated";

    /// <summary>Kapaktaki urun siniri. Raporun en onemli cumlesi.</summary>
    public string NotAVerdict => Turkish
        ? "Bu rapor kesin kusur kararı değildir."
        : "This report is not a defect verdict.";

    public string CoverIntro => Turkish
        ? "Seçilen depo, risk analizi işi ve isteğe bağlı statik analiz işi için üretilmiş, "
            + "kaynakları ve sınırları kayıtlı teknik inceleme özeti."
        : "A technical review summary for the selected repository, risk analysis job and "
            + "optional static analysis job, with its sources and limits recorded.";

    /// <summary>Kapakta duran uc zorunlu cumle; ilk sayfayi terk etmiyorlar.</summary>
    public IReadOnlyList<string> CoverContract => Turkish
        ?
        [
            "Ham model skoru kalibre edilmiş bir olasılık değildir; yüzde olarak okunamaz.",
            "Göreli risk endeksi (0-100), skorun eğitim dağılımındaki sıra yüzdeliğidir.",
            "Statik analiz bulguları model skoruna dahil değildir; ayrı raporlanır.",
        ]
        :
        [
            "The raw model score is not a calibrated probability and cannot be read as a percentage.",
            "The relative risk index (0-100) is the score's percentile within the training distribution.",
            "Static analysis findings are not part of the model score; they are reported separately.",
        ];

    public string PartialBanner => Turkish
        ? "KISMİ SONUÇ — Bu rapor yalnızca kaydedilmiş sonuçları kapsar."
        : "PARTIAL RESULT — This report covers only the results that were written.";

    public string PartialShort => Turkish
        ? "Yalnızca kaydedilmiş sonuçları kapsar."
        : "Covers only the results that were written.";

    public string Title => Turkish ? "Başlık" : "Title";

    public string Notes => Turkish ? "Not" : "Notes";

    // 2 - Yonetici ozeti

    public string SummaryHeading => Turkish ? "Yönetici özeti" : "Executive summary";

    public string CoveredCommits => Turkish ? "Kapsanan commit sayısı" : "Commits covered";

    public string DateRange => Turkish ? "Tarih aralığı" : "Date range";

    public string SelectedWindow => Turkish ? "Seçilen pencere" : "Selected window";

    public string MeanIndex => Turkish ? "Ortalama endeks" : "Mean index";

    public string MedianIndex => Turkish ? "Medyan endeks" : "Median index";

    public string MaximumIndex => Turkish ? "En yüksek endeks" : "Maximum index";

    public string DecisionAt05 => Turkish ? "0,5 eşiğine göre karar sayısı" : "Decisions at the 0.5 threshold";

    public string DecisionAtTrain => Turkish ? "Eğitim eşiğine göre karar sayısı" : "Decisions at the training threshold";

    public string StaticFindingCount => Turkish ? "Statik bulgu sayısı" : "Static finding count";

    public string MostTouched => Turkish ? "En çok dokunulan dosyalar" : "Most frequently touched files";

    public string HighestCommits => Turkish
        ? "En yüksek göreli endeksli commit'ler"
        : "Commits with the highest relative index";

    public string WithinWrittenResults => Turkish
        ? "(kaydedilmiş sonuçlar içinde)"
        : "(within the written results)";

    public string ScopeNote => Turkish
        ? "Bütün toplu sayılar seçilen commit penceresi içindir; deponun tamamı için değildir."
        : "All aggregate numbers cover the selected commit window, not the whole repository.";

    // 3 - Risk sozlesmesi

    public string ContractHeading => Turkish ? "Risk sözleşmesi" : "Risk contract";

    public IReadOnlyList<string> ContractLines => Turkish
        ?
        [
            "Ham model skoru: lojistik regresyonun 0-1 arasındaki çıkışı. Yüzde olasılık değildir.",
            "IsCalibrated = false. Üretim için bir kalibratör seçilmedi.",
            "Göreli risk endeksi: skorun, seçilen profilin eğitim dağılımındaki yüzdelik sırası. "
                + "Farklı profillerin endeksleri doğrudan karşılaştırılamaz.",
            "Hedef SZZ tabanlı otomatik bir etikettir; etiketin kendisi de bir tahmindir.",
            "0,5 kararı: ham skorun kendi orta noktası.",
            "Eğitim eşiği kararı: modelin eğitim bölümünde seçilmiş eşik.",
            "Statik analiz bulguları model skoruna dahil değildir.",
            "İnsan doğrulaması sınırlıdır; kör örneklemde model-pozitif precision işareti 1/14'tür.",
            "C# dosyası değiştirmeyen commit'ler tasarım gereği pozitif etiketlenemiyor.",
        ]
        :
        [
            "Raw model score: the logistic regression output between 0 and 1. It is not a percentage probability.",
            "IsCalibrated = false. No calibrator was selected for production.",
            "Relative risk index: the score's percentile within the training distribution of the selected profile. "
                + "Indices of different profiles cannot be compared directly.",
            "The target is an automatic SZZ-based label; the label itself is also an estimate.",
            "Decision at 0.5: the midpoint of the raw score.",
            "Decision at the training threshold: the threshold selected on the model's training split.",
            "Static analysis findings are not part of the model score.",
            "Human validation is limited; in the blind sample the model-positive precision signal is 1 out of 14.",
            "Commits that change no C# file cannot be labelled positive by design.",
        ];

    // 4 - Zaman cizelgesi

    public string TimelineHeading => Turkish ? "Risk zaman çizelgesi" : "Risk timeline";

    public string TimelineNote => Turkish
        ? "Dikey eksen göreli risk endeksi (0-100). Yatay eksen commit tarihi. "
            + "İki yatay çizgi iki karar eşiğinin endeks karşılığıdır."
        : "The vertical axis is the relative risk index (0-100). The horizontal axis is the commit date. "
            + "The two horizontal lines are the index positions of the two decision thresholds.";

    public string ThresholdAt05 => Turkish ? "0,5 eşiği" : "0.5 threshold";

    public string ThresholdAtTrain => Turkish ? "Eğitim eşiği" : "Training threshold";

    // 5 - Dosya etkinligi

    public string FilesHeading => Turkish ? "Dosya etkinlik özeti" : "File activity summary";

    public string FilesNote => Turkish
        ? "Bir dosyanın endeksi, seçilen pencerede o dosyaya dokunan commit'lerin göreli "
            + "endekslerinin ortalamasıdır. Dosyanın kendisi skorlanmaz ve bu sayı dosyanın "
            + "kusurlu olduğunu göstermez."
        : "A file's index is the mean of the relative indices of the commits that touched it "
            + "within the selected window. The file itself is not scored and this number does "
            + "not mean the file is defective.";

    public string Path => Turkish ? "Yol" : "Path";

    public string TouchCount => Turkish ? "Dokunuş" : "Touches";

    public string Churn => Turkish ? "Değişen satır" : "Churn";

    public string Mean => Turkish ? "Ortalama" : "Mean";

    public string Maximum => Turkish ? "En yüksek" : "Max";

    public string Latest => Turkish ? "Son" : "Latest";

    public string Findings => Turkish ? "Bulgu" : "Findings";

    public string SortedBy => Turkish ? "Sıralama" : "Sorted by";

    public string ShownOf => Turkish ? "gösterilen / toplam dosya" : "shown / total files";

    // 6 - Commit listesi

    public string CommitsHeading => Turkish
        ? "Önceliklendirilmiş commit listesi"
        : "Prioritised commit list";

    public string CommitsNote => Turkish
        ? "Göreli endekse göre azalan; eşitlikte tarih, sonra SHA. Bu bir hata listesi değil, "
            + "bakılacakların sırasıdır."
        : "Ordered by relative index, then by date, then by SHA. This is not a defect list; "
            + "it is a reading order.";

    public string ShortSha => Turkish ? "Kısa SHA" : "Short SHA";

    public string Date => Turkish ? "Tarih" : "Date";

    public string Subject => Turkish ? "Başlık" : "Subject";

    public string Index => Turkish ? "Endeks" : "Index";

    public string RawScore => Turkish ? "Ham skor" : "Raw score";

    public string Decision05 => Turkish ? "0,5" : "0.5";

    public string DecisionTrain => Turkish ? "Eşik" : "Thr.";

    public string CsFiles => Turkish ? "C# dosya" : "C# files";

    public string Warnings => Turkish ? "Uyarılar" : "Warnings";

    public string Yes => Turkish ? "evet" : "yes";

    public string No => Turkish ? "hayır" : "no";

    // 7 - Statik bulgular

    public string StaticHeading => Turkish ? "Statik bulgular" : "Static findings";

    public string StaticNotRun => Turkish
        ? "Statik analiz bu rapora eklenmedi. Bu, sıfır bulgu bulunduğu anlamına gelmez."
        : "Static analysis was not included in this report. That does not mean zero findings were found.";

    public string StaticSeparate => Turkish
        ? "Statik bulgular ham model skoruna dahil değildir."
        : "Static findings are not part of the raw model score.";

    public string SourceHead => Turkish ? "Taranan sürüm" : "Scanned revision";

    public string TreeState => Turkish ? "Çalışma ağacı" : "Working tree";

    public string Verified => Turkish ? "Doğrulandı" : "Verified";

    public string Suppressed => Turkish ? "Susturulan" : "Suppressed";

    public string Exemptions => Turkish ? "Muafiyet" : "Exemptions";

    public string Rule => Turkish ? "Kural" : "Rule";

    public string Severity => Turkish ? "Seviye" : "Severity";

    public string Line => Turkish ? "Satır" : "Line";

    public string Message => Turkish ? "Mesaj" : "Message";

    public string RuleDistribution => Turkish ? "Kural dağılımı" : "Rule distribution";

    // 8 - Model aciklamasi

    public string ExplanationHeading => Turkish ? "Model açıklaması" : "Model explanation";

    public string ExplanationNote => Turkish
        ? "Katkılar skorun hangi yöne itildiğini gösterir; bir özniteliğin hatayı "
            + "oluşturduğunu söylemez. Katkıların toplamı modelin kendi değeriyle "
            + "sayısal tolerans içinde karşılaştırılır."
        : "Contributions show which way the score was pushed; they do not say that a feature "
            + "caused a defect. The sum of the contributions is compared against the model's "
            + "own value within a numeric tolerance.";

    public string PositiveContributions => Turkish ? "Skoru yükseltenler" : "Pushing the score up";

    public string NegativeContributions => Turkish ? "Skoru düşürenler" : "Pushing the score down";

    public string Feature => Turkish ? "Öznitelik" : "Feature";

    public string Value => Turkish ? "Değer" : "Value";

    public string Transformed => Turkish ? "Dönüşmüş" : "Transformed";

    public string Coefficient => Turkish ? "Katsayı" : "Coefficient";

    public string Contribution => Turkish ? "Katkı" : "Contribution";

    public string ExplanationVerified => Turkish ? "Açıklama doğrulandı" : "Explanation verified";

    // 9 - Sinirliliklar

    public string LimitationsHeading => Turkish ? "Sınırlılıklar" : "Limitations";

    /// <summary>Sinirlilik kodunun okunabilir karsiligi.</summary>
    public string Limitation(string code) => (code, Turkish) switch
    {
        (RiskWarning.UncalibratedScore, true) =>
            "Skor kalibre edilmedi; olasılık olarak okunamaz.",
        (RiskWarning.UncalibratedScore, false) =>
            "The score is not calibrated and cannot be read as a probability.",
        (RiskWarning.SzzTarget, true) =>
            "Hedef SZZ ile otomatik üretilmiş bir etikettir; etiketin kendisi doğrulanmadı.",
        (RiskWarning.SzzTarget, false) =>
            "The target is an automatic SZZ label; the label itself was not validated.",
        (RiskWarning.StaticAnalysisNotIncluded, true) =>
            "Statik bulgular model skoruna dahil değildir.",
        (RiskWarning.StaticAnalysisNotIncluded, false) =>
            "Static findings are not part of the model score.",
        (RiskWarning.HumanValidationLimited, true) =>
            "İnsan doğrulaması sınırlıdır ve popülasyonu temsil etmez.",
        (RiskWarning.HumanValidationLimited, false) =>
            "Human validation is limited and does not represent the population.",
        (RiskWarning.CsLabelCoverageLimit, true) =>
            "C# dosyası değiştirmeyen commit'ler pozitif etiketlenemiyor; etiket kapsamı dardır.",
        (RiskWarning.CsLabelCoverageLimit, false) =>
            "Commits that change no C# file cannot be labelled positive; label coverage is narrow.",
        (RiskWarning.OutsideTrainRange, true) =>
            "En az bir öznitelik eğitimde görülen aralığın dışındadır.",
        (RiskWarning.OutsideTrainRange, false) =>
            "At least one feature lies outside the range seen during training.",
        (ReportLimitation.RepositorySpecificModel, true) =>
            "Model bu depoya özeldir; başka bir depoda aynı davranacağı ölçülmedi.",
        (ReportLimitation.RepositorySpecificModel, false) =>
            "The model is repository-specific; its behaviour on another repository was not measured.",
        (ReportLimitation.CrossRepositoryTransfer, true) =>
            "Repolar arası aktarım tutarsız çıktı; genel bir model seçilmedi.",
        (ReportLimitation.CrossRepositoryTransfer, false) =>
            "Cross-repository transfer was inconsistent; no general model was selected.",
        (ReportLimitation.RightCensoring, true) =>
            "En yeni commit'ler henüz düzeltilmemiş olabilir; etiketleri eksik kalabilir.",
        (ReportLimitation.RightCensoring, false) =>
            "The newest commits may not have been fixed yet, so their labels can be incomplete.",
        (ReportLimitation.BotSensitivity, true) =>
            "Bot commit'lerinin sonuca etkisi ayrıştırılmadı.",
        (ReportLimitation.BotSensitivity, false) =>
            "The effect of bot commits on the result was not separated out.",
        (ReportLimitation.FileMeanIndexDefinition, true) =>
            "Dosya endeksi, o dosyaya dokunan commit'lerin endekslerinin ortalamasıdır; "
                + "dosyanın kendi skoru değildir.",
        (ReportLimitation.FileMeanIndexDefinition, false) =>
            "A file's index is the mean of the indices of the commits that touched it, "
                + "not a score of the file itself.",
        (ReportLimitation.PartialResult, true) =>
            "Rapor kısmi bir sonuçtan üretildi; yalnızca kaydedilmiş satırları kapsar.",
        (ReportLimitation.PartialResult, false) =>
            "The report was produced from a partial result and covers only the written rows.",
        _ => code,
    };

    // 10 - Kaynak ve butunluk

    public string ProvenanceHeading => Turkish ? "Kaynak ve bütünlük" : "Provenance and integrity";

    public string ManifestChecksum => Turkish ? "Manifest özeti (SHA-256)" : "Manifest checksum (SHA-256)";

    public string ScoreReferenceChecksum => Turkish ? "Skor referansı özeti" : "Score reference checksum";

    public string ModelResultsChecksum => Turkish ? "Model sonuçları özeti" : "Model results checksum";

    public string SchemaVersion => Turkish ? "Şema sürümü" : "Schema version";

    public string GeneratorVersion => Turkish ? "Üretici sürümü" : "Generator version";

    /// <summary>PDF kendi ozetini iceremez; bunun sebebi raporda yaziyor.</summary>
    public string PdfChecksumNote => Turkish
        ? "PDF'in kendi SHA-256 özeti bu belgenin içinde yazamaz: özet dosyanın son "
            + "halinden hesaplanıyor ve içine yazmak onu değiştirirdi. Özet, indirme "
            + "metadata'sında ve indirme cevabının ETag başlığında sunulur."
        : "The PDF's own SHA-256 cannot be printed inside this document: the checksum is "
            + "computed over the final file and writing it inside would change it. The "
            + "checksum is served in the download metadata and in the ETag header.";

    public string Page => Turkish ? "Sayfa" : "Page";

    public string Of => Turkish ? "/" : "/";

    public string NoData => Turkish ? "Gösterilecek veri yok." : "No data to show.";

    /// <summary>Tarih bicimi; iki kulturde de gun-ay-yil sirasi degil, kulturun kendi sirasi.</summary>
    public string Day(DateTimeOffset value) =>
        value.UtcDateTime.ToString("d MMMM yyyy", Formatting);

    public string Moment(DateTimeOffset value) =>
        value.UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

    public string Number(double value, int decimals) =>
        value.ToString("F" + decimals.ToString(CultureInfo.InvariantCulture), Formatting);

    public string Count(int value) => value.ToString("N0", Formatting);
}
