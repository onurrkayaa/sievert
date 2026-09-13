using System.Globalization;

namespace Sievert.Demo;

/// <summary>Demo aracinin secenekleri.</summary>
/// <param name="OpenBrowser">Hazir olunca tarayici acilsin mi.</param>
/// <param name="KeepDatabase">Cikista veritabani container'i birakilsin mi.</param>
/// <param name="SmokeTest">Acip kontrol edip kapatma kipi; jury demosu degil, CI icin.</param>
public sealed record DemoArguments(
    bool OpenBrowser,
    bool KeepDatabase,
    int? ApiPort,
    int? WebPort,
    int StartupTimeoutSeconds,
    bool SmokeTest,
    bool Verbose)
{
    public const int DefaultStartupTimeoutSeconds = 180;

    public static DemoArguments Parse(string[] args)
    {
        bool open = !args.Contains("--no-open");
        bool smoke = args.Contains("--smoke-test");

        return new DemoArguments(
            // Duman testinde tarayici acilmiyor: CI'da acilacak bir tarayici yok ve
            // acilirsa surec kapanmaz.
            OpenBrowser: open && !smoke,
            KeepDatabase: args.Contains("--keep-database"),
            ApiPort: Number(args, "--api-port"),
            WebPort: Number(args, "--web-port"),
            StartupTimeoutSeconds: Number(args, "--startup-timeout-seconds") ?? DefaultStartupTimeoutSeconds,
            SmokeTest: smoke,
            Verbose: args.Contains("--verbose"));
    }

    private static int? Number(string[] args, string name)
    {
        int index = Array.IndexOf(args, name);

        return index >= 0 && index + 1 < args.Length
            && int.TryParse(args[index + 1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int value)
                ? value
                : null;
    }
}
