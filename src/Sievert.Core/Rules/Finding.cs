using Sievert.Core.Analysis;

namespace Sievert.Core.Rules;

/// <summary>
/// Bir kuralin tek bir yerde urettigi bulgu. Kendi basina anlamli olacak kadar bilgi tasiyor:
/// bulguyu ekrana basan ya da JSON'a yazan taraf kural listesine bakmak zorunda kalmiyor.
/// </summary>
/// <param name="RuleCode">Kuralin kodu, ornegin "SV001".</param>
/// <param name="Title">Kisa baslik. Ayni kuralin butun bulgularinda ayni.</param>
/// <param name="Description">Bu bulguya ozel aciklama; hangi metot, ne yapilmali.</param>
/// <param name="Rationale">Bu kuralin neden onemli oldugu. Kuralin kendi aciklamasinin kopyasi.</param>
/// <param name="FilePath">Bulgunun bulundugu dosya, tarama kokune gore goreli.</param>
/// <param name="Line">Bulgunun basladigi satir (1'den baslar).</param>
/// <param name="MethodName">Bulgunun icinde oldugu metodun adi.</param>
/// <param name="Severity">Bulgunun ciddiyeti.</param>
public sealed record Finding(
    string RuleCode,
    string Title,
    string Description,
    string Rationale,
    string FilePath,
    int Line,
    string MethodName,
    Severity Severity)
{
    /// <summary>
    /// Bulgu test kodunda mi. Yoldan turetiliyor, kural bunu ayrica hesaplamiyor; olcut
    /// Asama 1'de yazilan test/uretim heuristiginin ta kendisi, ikinci bir tahmin yok.
    ///
    /// Isaret var ama muafiyet yok: bazi kaliplar (ornegin SV002'nin aradigi .Result)
    /// test kodunda mesru olabiliyor, ama "test kodu" kesin bir bilgi degil, yola ve ada
    /// bakan bir tahmin. Tahmine dayanip bulguyu gizlemek yerine bulguyu isaretleyip
    /// karari okuyana birakiyorum.
    /// </summary>
    public bool IsTestCode => FileAnalysis.IsTestCodePath(FilePath);
}
