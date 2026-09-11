namespace Sievert.Cli;

/// <summary>
/// Komutlarin dondugu cikis kodlari. Uc ayri kod var ki CI'da "arac bozuldu" ile
/// "bulgu var" birbirinden ayirt edilebilsin. Gerekcesi ADR 0006'da.
/// </summary>
public static class ExitCodes
{
    /// <summary>Komut calisti, esigi gecen bulgu yok.</summary>
    public const int Clean = 0;

    /// <summary>Komut calisti, esigi gecen bulgu var.</summary>
    public const int FindingsFound = 1;

    /// <summary>Arac calisamadi: yol bulunamadi, gecersiz bayrak ya da beklenmeyen hata.</summary>
    public const int ToolError = 2;
}
