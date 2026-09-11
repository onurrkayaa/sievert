using Sievert.Core.Rules;

namespace Sievert.Analysis.Rules;

/// <summary>
/// Bir kuralin bir calistirmada urettigi sonuc. Bulgularin yaninda muaf tutulanlar da
/// donuyor, cunku bir kuralin ciktisini cogu zaman istisnasi belirliyor ve "bu metot
/// neden listede yok" sorusunun cevabi bir yerde durmali.
/// </summary>
/// <param name="Findings">Uretilen bulgular.</param>
/// <param name="Exemptions">Kuralin bulgu uretmeden gectigi metotlar ve sebepleri.</param>
public sealed record RuleResult(IReadOnlyList<Finding> Findings, IReadOnlyList<Exemption> Exemptions)
{
    /// <summary>Ne bulgu ne muafiyet.</summary>
    public static readonly RuleResult Empty = new([], []);
}
