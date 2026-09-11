namespace Sievert.Core.Rules;

/// <summary>Bir kuralin tek bir yerde urettigi bulgu.</summary>
/// <param name="RuleCode">Kuralin kodu, ornegin "SV001".</param>
/// <param name="Title">Kisa baslik. Ayni kuralin butun bulgularinda ayni.</param>
/// <param name="Description">Bu bulguya ozel aciklama; hangi metot, neden sorun.</param>
/// <param name="FilePath">Bulgunun bulundugu dosya, tarama kokune gore goreli.</param>
/// <param name="Line">Bulgunun basladigi satir (1'den baslar).</param>
/// <param name="MethodName">Bulgunun icinde oldugu metodun adi.</param>
/// <param name="Severity">Bulgunun ciddiyeti.</param>
public sealed record Finding(
    string RuleCode,
    string Title,
    string Description,
    string FilePath,
    int Line,
    string MethodName,
    Severity Severity);
