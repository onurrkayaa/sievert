namespace Sievert.Core.Rules;

/// <summary>Bir bulgunun ne kadar ciddi oldugu.</summary>
public enum Severity
{
    /// <summary>Bilgi amacli, duzeltilmesi sart degil.</summary>
    Info,

    /// <summary>Suphe uyandiran bir durum, bakilmasi iyi olur.</summary>
    Warning,

    /// <summary>Muhtemel bir hata, duzeltilmesi gerekiyor.</summary>
    Error,
}
