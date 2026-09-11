namespace Sievert.Core;

/// <summary>Açılış ekranının tek bir satırı ve nasıl renklendirileceği.</summary>
public readonly record struct BannerLine(string Text, bool IsTitle);

/// <summary>Cli'nin açılışta bastığı ASCII çerçeveli karşılama ekranını üretir.</summary>
public static class Banner
{
    private const string Title = "SIEVERT";
    private const string Description = "Commit-level defect risk analysis for .NET";

    public static IReadOnlyList<BannerLine> Render(string version, string runtime)
    {
        string[] content =
        [
            Title,
            Description,
            $"Surum : {version}",
            $".NET  : {runtime}",
        ];

        int width = content.Max(line => line.Length) + 4;
        string border = "+" + new string('-', width) + "+";

        List<BannerLine> lines = [new BannerLine(border, IsTitle: false)];
        for (int i = 0; i < content.Length; i++)
        {
            lines.Add(new BannerLine("|  " + content[i].PadRight(width - 2) + "|", IsTitle: i == 0));
        }
        lines.Add(new BannerLine(border, IsTitle: false));

        return lines;
    }
}
