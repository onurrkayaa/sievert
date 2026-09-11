namespace Sievert.Core.Rules;

/// <summary>
/// Bir bulgunun <c>// sievert:disable</c> yorumuyla susturulmus olmasi. Muafiyetlerle
/// (bkz. <see cref="Exemption"/>) ayni mantik: susturulan bir bulgu sessizce yok olmuyor,
/// sebebiyle birlikte kaydediliyor ve sayiliyor. "Kac bulgu susturuldu" olculebilir bir
/// sayi olmali, yoksa susturma gizli bir kolaylik hâline gelir.
/// </summary>
/// <param name="RuleCode">Susturulan kuralin kodu.</param>
/// <param name="FilePath">Dosya, tarama kokune gore goreli.</param>
/// <param name="Line">Susturulan bulgunun satiri (yorumun bir altindaki satir).</param>
/// <param name="Reason">Yorumda yazilan gerekce. Gerekcesiz susturma zaten gecerli degil.</param>
public sealed record Suppression(string RuleCode, string FilePath, int Line, string Reason);
