using Sievert.Mining;

namespace Sievert.Tests;

public class RemoteIdentityTests
{
    [Theory]
    [InlineData("https://github.com/App-vNext/Polly.git")]
    [InlineData("https://github.com/App-vNext/Polly")]
    [InlineData("https://github.com/App-vNext/Polly/")]
    [InlineData("git@github.com:App-vNext/Polly.git")]
    [InlineData("ssh://git@github.com/App-vNext/Polly.git")]
    [InlineData("HTTPS://GitHub.com/App-vNext/Polly.GIT")]
    public void TheSameRepositoryWrittenDifferently_NormalisesToOneIdentity(string url) =>
        Assert.Equal("github.com/app-vnext/polly", RemoteIdentity.Normalize(url));

    [Fact]
    public void TwoDifferentRepositories_DoNotCollide() =>
        Assert.NotEqual(
            RemoteIdentity.Normalize("https://github.com/App-vNext/Polly.git"),
            RemoteIdentity.Normalize("https://github.com/ShareX/ShareX.git"));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void NoRemote_GivesNoIdentity(string? url) => Assert.Null(RemoteIdentity.Normalize(url));
}
