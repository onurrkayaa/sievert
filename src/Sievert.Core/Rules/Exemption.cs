namespace Sievert.Core.Rules;

/// <summary>Bir metodun kuraldan neden muaf tutuldugu.</summary>
public enum ExemptionReason
{
    /// <summary>Imzasi event handler kalibina uyuyor.</summary>
    Signature,

    /// <summary>
    /// Ayni dosyada ya da ayni partial sinifin baska bir parcasinda
    /// <c>+= MetotAdi</c> biciminde bir abonelik var.
    /// </summary>
    Subscription,

    /// <summary>
    /// Uygulamanin giris noktasi (<c>Main</c>). SV002 icin: konsol girisinde bloklamak
    /// mesru olabiliyor, orada tikanacak bir senkronizasyon baglami yok.
    /// </summary>
    EntryPoint,

    /// <summary>
    /// Sonuc <c>_ =</c> ile bilincli olarak atilmis. SV003 icin: yazan kisi donen gorevi
    /// umursamadigini acikca soylemis.
    /// </summary>
    DiscardedResult,

    /// <summary>
    /// Cagri <c>Task.Run(...)</c> icinde, yani zaten ayri bir goreve verilmis.
    /// SV003 icin: cagiranin onu beklemesi beklenmiyor.
    /// </summary>
    FireAndForget,
}

/// <summary>
/// Kuralin bulgu uretmeden gectigi bir metot. Bulgu listesinde gorunmeyen bir metodun
/// neden gorunmedigi sorulabilsin diye tutuluyor: ekrana basilmiyor, sadece --json
/// ciktisinda opsiyonel bir alan olarak yer aliyor.
/// </summary>
/// <param name="RuleCode">Muafiyeti veren kuralin kodu, ornegin "SV001".</param>
/// <param name="FilePath">Metodun bulundugu dosya, tarama kokune gore goreli.</param>
/// <param name="Line">Metodun basladigi satir (1'den baslar).</param>
/// <param name="MethodName">Muaf tutulan metodun adi.</param>
/// <param name="Reason">Muafiyetin hangi kanit kaynagindan geldigi.</param>
public sealed record Exemption(
    string RuleCode,
    string FilePath,
    int Line,
    string MethodName,
    ExemptionReason Reason);
