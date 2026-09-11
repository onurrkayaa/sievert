using Microsoft.CodeAnalysis;
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
    /// Tek bir dosyanin sozdizimi agacini inceler. Bulgu da muafiyet de yoksa
    /// <see cref="RuleResult.Empty"/> doner. <paramref name="filePath"/> bulgularda
    /// oldugu gibi yaziliyor, yani tarama kokune gore goreli gelmesi bekleniyor.
    /// </summary>
    RuleResult InspectFile(SyntaxTree tree, string filePath);
}
