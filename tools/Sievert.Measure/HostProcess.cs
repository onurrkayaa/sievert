using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Sievert.Measure;

/// <summary>
/// Olcum icin Release derlemesinden baslatilan bir surec (API ya da panel).
///
/// <see cref="ApiProcess"/>'ten ayri duruyor: o Debug derlemesini kullaniyor ve arka plan
/// islerinin bellegini olcuyor. Burada olculen sey panel sureleri ve panelin API'ye
/// kac istek attigi, o yuzden Release ve gunlukten sayim.
/// </summary>
public sealed partial class HostProcess : IDisposable
{
    private readonly Process process;

    private readonly object gate = new();

    private int apiRequests;

    private int databaseCommands;

    private double slowestApi;

    private HostProcess(Process process)
    {
        this.process = process;
    }

    /// <summary>Panelin API'ye attigi istek sayisi; gunlukteki satirlardan sayiliyor.</summary>
    public int ApiRequestCount
    {
        get
        {
            lock (gate)
            {
                return apiRequests;
            }
        }
    }

    /// <summary>
    /// EF Core'un calistirdigi komut sayisi. Yalniz komut gunlugu acikken artiyor;
    /// varsayilan gunluk seviyesinde 0 kalir.
    /// </summary>
    public int DatabaseCommandCount
    {
        get
        {
            lock (gate)
            {
                return databaseCommands;
            }
        }
    }

    /// <summary>En yavas API cagrisinin suresi.</summary>
    public double SlowestApiMilliseconds
    {
        get
        {
            lock (gate)
            {
                return slowestApi;
            }
        }
    }

    /// <summary>Sayaci sifirlar; her sayfa kendi en yavas cagrisini raporluyor.</summary>
    public void ResetSlowest()
    {
        lock (gate)
        {
            slowestApi = 0;
        }
    }

    /// <summary>
    /// Projeyi Release olarak yayimlar ve yayim ciktisinin dll yolunu verir.
    ///
    /// Neden yayim: <c>bin/Release</c> altindan dogrudan kosarken statik varliklar
    /// sunulamiyor - cerceve "yayim ciktisina karsi kosmuyorsun" diyip 500 donuyor.
    /// Olculen sey gercek bir kullanicinin gorecegi sayfa olmali.
    /// </summary>
    public static string Publish(string repositoryRoot, string project, string outputRoot)
    {
        string output = Path.Combine(outputRoot, project);

        using Process publish = Process.Start(new ProcessStartInfo("dotnet")
        {
            Arguments = $"publish \"{Path.Combine(repositoryRoot, "src", project)}\" -c Release -o \"{output}\" --nologo",
            WorkingDirectory = repositoryRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        }) ?? throw new InvalidOperationException($"{project} yayimlanamadi.");

        publish.WaitForExit();

        if (publish.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"{project} yayimi basarisiz: {publish.StandardError.ReadToEnd()}");
        }

        return Path.Combine(output, project + ".dll");
    }

    /// <param name="dll">Yayimlanmis derlemenin yolu.</param>
    /// <param name="contentRoot">
    /// API icin kanit dosyalarinin bulundugu kok; panel icin null, cunku panelin icerik
    /// koku kendi yayim klasoru olmali (statik varliklar orada).
    /// </param>
    /// <param name="environment">Surece eklenecek ortam degiskenleri; gunluk seviyesi gibi.</param>
    public static HostProcess Start(
        string dll,
        string url,
        string? contentRoot,
        string? apiUrl,
        IReadOnlyDictionary<string, string>? environment = null)
    {
        if (!File.Exists(dll))
        {
            throw new FileNotFoundException("Yayimlanmis derleme bulunamadi.", dll);
        }

        string arguments = $"\"{dll}\" --urls {url}";

        if (contentRoot is not null)
        {
            arguments += $" --contentRoot \"{contentRoot}\"";
        }

        ProcessStartInfo start = new("dotnet")
        {
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = Path.GetDirectoryName(dll)!,
        };

        start.Environment["DOTNET_ENVIRONMENT"] = "Production";
        start.Environment["ASPNETCORE_ENVIRONMENT"] = "Production";

        if (apiUrl is not null)
        {
            start.Environment["SIEVERT_API_URL"] = apiUrl;
        }

        foreach ((string key, string value) in environment
            ?? new Dictionary<string, string>(StringComparer.Ordinal))
        {
            start.Environment[key] = value;
        }

        HostProcess host = new(Process.Start(start)
            ?? throw new InvalidOperationException($"{dll} baslatilamadi."));

        host.process.OutputDataReceived += host.OnOutput;
        host.process.ErrorDataReceived += host.OnOutput;
        host.process.BeginOutputReadLine();
        host.process.BeginErrorReadLine();

        return host;
    }

    public void Dispose()
    {
        if (!process.HasExited)
        {
            process.Kill(entireProcessTree: true);
            process.WaitForExit(10_000);
        }

        process.Dispose();
    }

    private void OnOutput(object sender, DataReceivedEventArgs message)
    {
        if (message.Data is not string line)
        {
            return;
        }

        if (line.Contains("Executed DbCommand", StringComparison.Ordinal))
        {
            lock (gate)
            {
                databaseCommands++;
            }

            return;
        }

        if (line.Contains("Start processing HTTP request", StringComparison.Ordinal))
        {
            lock (gate)
            {
                apiRequests++;
            }

            return;
        }

        if (Duration().Match(line) is { Success: true } match)
        {
            double milliseconds = double.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);

            lock (gate)
            {
                slowestApi = Math.Max(slowestApi, milliseconds);
            }
        }
    }

    [GeneratedRegex(@"Received HTTP response headers after ([\d.,]+)ms")]
    private static partial Regex Duration();
}
