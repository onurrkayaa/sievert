using Sievert.Mining;

namespace Sievert.Tests;

public class BotAuthorTests
{
    [Theory]
    [InlineData("dependabot[bot]", "49699333+dependabot[bot]@users.noreply.github.com")]
    [InlineData("github-actions[bot]", "github-actions[bot]@users.noreply.github.com")]
    [InlineData("polly-updater-bot[bot]", "polly-updater-bot@users.noreply.github.com")]
    [InlineData("Yayin Araci", "bot@example.com")]
    public void ABotAuthor_IsFlagged(string name, string email) =>
        Assert.True(BotAuthor.Looks(name, email));

    [Theory]
    // Polly'nin tarihindeki gercek kisi; eski tanim bunu bot saniyordu.
    [InlineData("Jason Botwick", "jason@example.com")]
    [InlineData("Talbot Kurtuluş", "talbot@example.com")]
    [InlineData("Onur Kaya", "onur@example.com")]
    public void ARealPerson_IsNotFlagged(string name, string email) =>
        Assert.False(BotAuthor.Looks(name, email));
}
