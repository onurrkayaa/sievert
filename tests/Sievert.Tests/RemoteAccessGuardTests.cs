using Sievert.Api;

namespace Sievert.Tests;

/// <summary>
/// Uzak erisim korumasinin testleri.
///
/// API'de kimlik dogrulama yok. Kimlik dogrulamasi olmayan bir servisin varsayilan
/// olarak butun arayuzlerde dinlemesi, onu ag uzerinden herkese acmak demek; o yuzden
/// varsayilan loopback ve disari acmak acik bir ayar istiyor.
/// </summary>
public sealed class RemoteAccessGuardTests
{
    [Theory]
    [InlineData("http://127.0.0.1:5000")]
    [InlineData("http://localhost:5000")]
    [InlineData("http://[::1]:5000")]
    [InlineData("https://localhost:5001;http://127.0.0.1:5000")]
    [InlineData("")]
    [InlineData(null)]
    public void LoopbackAddressesAreAllowedWithoutAnySetting(string? urls)
    {
        Assert.Null(RemoteAccessGuard.Check(urls, allowRemote: null));
    }

    [Theory]
    [InlineData("http://0.0.0.0:5000")]
    [InlineData("http://*:5000")]
    [InlineData("http://+:5000")]
    [InlineData("http://192.168.1.20:5000")]
    [InlineData("http://[::]:5000")]
    [InlineData("http://127.0.0.1:5000;http://0.0.0.0:8080")]
    public void RemoteAddressesAreRefusedWhenTheSettingIsMissing(string urls)
    {
        string? error = RemoteAccessGuard.Check(urls, allowRemote: null);

        Assert.NotNull(error);
        Assert.Contains(RemoteAccessGuard.EnvironmentVariable, error, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("true")]
    [InlineData("TRUE")]
    [InlineData("True")]
    public void RemoteAddressesAreAllowedOnceTheSettingSaysSo(string value)
    {
        Assert.Null(RemoteAccessGuard.Check("http://0.0.0.0:5000", value));
    }

    [Theory]
    [InlineData("1")]
    [InlineData("yes")]
    [InlineData("evet")]
    [InlineData("false")]
    [InlineData(" ")]
    public void OnlyTheWordTrueOpensIt(string value)
    {
        // "1" ya da "yes" sessizce kabul edilmiyor: yanlis yazilmis bir ayarin acmis gibi
        // gorunmesindense hic acmamasi daha iyi.
        Assert.NotNull(RemoteAccessGuard.Check("http://0.0.0.0:5000", value));
    }

    [Fact]
    public void TheWarningSaysThereIsNoAuthentication()
    {
        Assert.Contains("kimlik dogrulama", RemoteAccessGuard.RemoteWarning, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AnUnparseableUrlIsTreatedAsRemote()
    {
        // Anlayamadigim bir adresi guvenli varsaymak, yanlis tarafta hata yapmak olurdu.
        Assert.NotNull(RemoteAccessGuard.Check("bu bir adres degil", allowRemote: null));
    }
}
