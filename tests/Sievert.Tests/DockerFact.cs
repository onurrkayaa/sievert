using System.Diagnostics;

namespace Sievert.Tests;

/// <summary>
/// Docker var mi. Veritabani testleri gercek bir PostgreSQL konteyneri istiyor; Docker
/// yoksa bu testler atlanmali ama ATLANDIKLARI GORULMELI. Sessizce gecmeleri en kotusu
/// olurdu: yerelde hicbir sey calismadan yesil, CI'da ilk kez kirmizi.
/// </summary>
internal static class Docker
{
    public const string SkipReason =
        "Docker calismiyor, veritabani testleri atlandi. Bu testler gercek bir PostgreSQL "
        + "konteyneri istiyor; Docker'i baslatip tekrar calistir.";

    private static readonly Lazy<bool> Probe = new(Check);

    public static bool Available => Probe.Value;

    private static bool Check()
    {
        try
        {
            using Process? process = Process.Start(new ProcessStartInfo("docker", "info")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            });

            if (process is null)
            {
                return false;
            }

            return process.WaitForExit(TimeSpan.FromSeconds(20)) && process.ExitCode == 0;
        }
        catch (Exception)
        {
            // docker komutu hic yoksa da atlama yoluna gidiyoruz.
            return false;
        }
    }
}

/// <summary>Docker yoksa atlanan test. Atlama sebebi test ciktisinda gorunuyor.</summary>
public sealed class DockerFactAttribute : FactAttribute
{
    public DockerFactAttribute()
    {
        if (!Docker.Available)
        {
            Skip = Docker.SkipReason;
        }
    }
}
