namespace Sievert.Mining;

/// <summary>
/// Tarihin ne kadarinin okunacagi.
/// </summary>
/// <param name="Since">Bu tarihten once yazilmis commit'ler atlanir. Null ise sinir yok.</param>
/// <param name="MaxCommits">En fazla bu kadar commit okunur. Null ise sinir yok.</param>
public sealed record MiningOptions(DateTimeOffset? Since = null, int? MaxCommits = null)
{
    public static readonly MiningOptions All = new();
}
