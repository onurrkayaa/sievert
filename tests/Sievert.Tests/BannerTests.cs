using Sievert.Core;

namespace Sievert.Tests;

public class BannerTests
{
    [Fact]
    public void Render_ReturnsNonEmptyText()
    {
        string text = string.Join(Environment.NewLine, Banner.Render("1.0.0", ".NET 10.0").Select(line => line.Text));

        Assert.False(string.IsNullOrWhiteSpace(text));
    }
}
