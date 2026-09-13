using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace Sievert.Demo;

/// <summary>
/// Demo icin baslatilan bir alt surec (API ya da panel).
///
/// Cikti bellekte tutuluyor ama ekrana **basilmiyor**: gunluk satirlari baglanti dizesi
/// ve dosya yolu tasiyabiliyor. Bir sey ters giderse son birkac satir gosteriliyor ve
/// oradan once baglanti dizesi temizleniyor.
/// </summary>
public sealed class DemoProcess : IDisposable
{
    private readonly Process process;

    private readonly List<string> output = [];

    private readonly Lock gate = new();

    private DemoProcess(Process process, string name)
    {
        this.process = process;
        Name = name;
    }

    public string Name { get; }

    public bool HasExited => process.HasExited;

    /// <summary>Son satirlar; baglanti dizesi ve yol temizlenmis halde.</summary>
    public IReadOnlyList<string> Tail(int count)
    {
        lock (gate)
        {
            return [.. output.TakeLast(count).Select(Scrub)];
        }
    }

    public static DemoProcess Start(
        string name,
        string projectPath,
        string url,
        string connectionString,
        string? apiUrl,
        string repositoryRoot,
        bool verbose)
    {
        ProcessStartInfo start = new("dotnet")
        {
            Arguments = $"run --project \"{projectPath}\" --no-launch-profile --urls {url}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = repositoryRoot,
        };

        start.Environment["SIEVERT_DB"] = connectionString;
        start.Environment["DOTNET_ENVIRONMENT"] = "Production";
        start.Environment["ASPNETCORE_ENVIRONMENT"] = "Production";

        if (apiUrl is not null)
        {
            start.Environment["SIEVERT_API_URL"] = apiUrl;
        }

        DemoProcess demo = new(
            Process.Start(start) ?? throw new InvalidOperationException($"{name} baslatilamadi."),
            name);

        demo.process.OutputDataReceived += demo.OnOutput;
        demo.process.ErrorDataReceived += demo.OnOutput;
        demo.process.BeginOutputReadLine();
        demo.process.BeginErrorReadLine();

        if (verbose)
        {
            Console.WriteLine($"  {name} baslatildi: {url}");
        }

        return demo;
    }

    /// <summary>
    /// Once nazik kapanma, sonra zorla.
    ///
    /// <c>dotnet run</c> araya bir surec koydugu icin butun agac kapatiliyor; yalniz
    /// ust sureci oldurmek, arkada calisan uygulamayi ve portu birakirdi.
    /// </summary>
    public void Stop(TimeSpan patience)
    {
        try
        {
            if (process.HasExited)
            {
                return;
            }

            process.Kill(entireProcessTree: true);
            process.WaitForExit((int)patience.TotalMilliseconds);
        }
        catch (InvalidOperationException)
        {
            // Surec zaten kapanmis.
        }
    }

    public void Dispose()
    {
        Stop(TimeSpan.FromSeconds(10));
        process.Dispose();
    }

    /// <summary>Bos bir loopback portu bulur.</summary>
    public static int FreePort()
    {
        using TcpListener listener = new(IPAddress.Loopback, 0);

        listener.Start();

        int port = ((IPEndPoint)listener.LocalEndpoint).Port;

        listener.Stop();

        return port;
    }

    /// <summary>Baglanti dizesi ve mutlak yol gunluk satirindan cikariliyor.</summary>
    private static string Scrub(string line)
    {
        string cleaned = line;

        foreach (string secret in new[] { "Password=", "password=", "Host=", "Username=" })
        {
            int index = cleaned.IndexOf(secret, StringComparison.OrdinalIgnoreCase);

            if (index >= 0)
            {
                cleaned = cleaned[..index] + "[baglanti dizesi gizlendi]";
            }
        }

        return cleaned;
    }

    private void OnOutput(object sender, DataReceivedEventArgs message)
    {
        if (message.Data is not string line)
        {
            return;
        }

        lock (gate)
        {
            output.Add(line);

            // Bellekte sinirsiz birikmesin; son birkac yuz satir yeter.
            if (output.Count > 400)
            {
                output.RemoveRange(0, 200);
            }
        }
    }
}
