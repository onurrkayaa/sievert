namespace Sievert.Analysis.Rules;

/// <summary>
/// Tek bir tespit kurali. Bu arayuz Core'da degil Analysis'te duruyor, cunku kurallar
/// Roslyn'in SyntaxTree'sini aliyor ve ADR 0004 Roslyn tiplerini Core'dan uzak tutuyor.
/// </summary>
public interface IRule
{
    /// <summary>Kural kodu, ornegin "SV001".</summary>
    string Code { get; }

    /// <summary>Kuralin kisa adi. Bulgunun basligi olarak kullaniliyor.</summary>
    string Name { get; }

    /// <summary>Kuralin ne aradigini anlatan tek cumle.</summary>
    string Description { get; }

    /// <summary>
    /// Tek bir dosyayi inceler. Bulgu da muafiyet de yoksa <see cref="RuleResult.Empty"/>
    /// doner. Kural diske bakmaz: ihtiyaci olan her sey baglamda duruyor, boylece tarama
    /// disi birakilan bir dosya sonuca karisamiyor.
    /// </summary>
    RuleResult Inspect(RuleContext context);
}
