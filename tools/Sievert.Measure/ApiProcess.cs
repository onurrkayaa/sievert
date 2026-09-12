using System.Diagnostics;

namespace Sievert.Measure;

/// <summary>
/// Olcum icin baslatilan gercek API sureci.
///
/// Bellek ici bir test sunucusu degil: olculen sure icinde Kestrel, JSON yazimi ve
/// gercek bir yerel baglanti da var. Bellek de bu surecin kendi bellegi.
///
/// macOS ve Linux'ta <c>PeakWorkingSet64</c> desteklenmiyor, o yuzden bellek 50 ms'de
/// bir orneklenip en buyugu tutuluyor. Bu "en yuksek deger" degil, "olculen en yuksek
/// deger"; raporda oyle yaziyor.
/// </summary>
public sealed class ApiProcess : IDisposable
{
    private const string DatabaseCommandMarker = "Executed DbCommand";

    private readonly Process process;

    private readonly CancellationTokenSource sampling = new();

    private int commandCount;

    private long peak;

    private ApiProcess(Process process)
    {
        this.process = process;
    }

    /// <summary>EF Core'un calistirdigi komut sayisi. Gunluk kapaliysa 0 kalir.</summary>
    public int DatabaseCommandCount => Volatile.Read(ref commandCount);

    public long PeakWorkingSetBytes => Interlocked.Read(ref peak);

    public static ApiProcess Start(string repositoryRoot, bool logQueries)
    {
        string dll = Path.Combine(
            repositoryRoot, "src", "Sievert.Api", "bin", "Debug", "net10.0", "Sievert.Api.dll");

        if (!File.Exists(dll))
        {
            throw new FileNotFoundException(
                "API derlemesi yok. Once 'dotnet build' calistir.", dll);
        }

        ProcessStartInfo start = new("dotnet")
        {
            // Icerik koku acikca veriliyor: surecin calisma dizinine guvenmek, model
            // dosyalarini bulamamak demek olurdu.
            Arguments = $"\"{dll}\" --urls http://127.0.0.1:5199 --contentRoot \"{repositoryRoot}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = repositoryRoot,
        };

        start.Environment["DOTNET_ENVIRONMENT"] = "Production";
        start.Environment["Logging__LogLevel__Microsoft.EntityFrameworkCore.Database.Command"] =
            logQueries ? "Information" : "Warning";

        ApiProcess api = new(Process.Start(start)
            ?? throw new InvalidOperationException("API sureci baslatilamadi."));

        api.process.OutputDataReceived += api.OnOutput;
        api.process.ErrorDataReceived += api.OnOutput;
        api.process.BeginOutputReadLine();
        api.process.BeginErrorReadLine();

        _ = Task.Run(api.SampleMemoryAsync);

        return api;
    }

    // sievert:disable SV006 kendi zaman asimi var (30 sn); disaridan iptal edilecek bir cagiran yok
    public async Task WaitUntilHealthyAsync(HttpClient client)
    {
        for (int attempt = 0; attempt < 300; attempt++)
        {
            try
            {
                using HttpResponseMessage response = await client.GetAsync("/api/v1/health");

                if (response.IsSuccessStatusCode)
                {
                    return;
                }
            }
            catch (HttpRequestException)
            {
                // Sunucu henuz dinlemiyor; beklemeye devam.
            }

            await Task.Delay(100);
        }

        throw new TimeoutException("API 30 saniyede saglik ucuna cevap vermedi.");
    }

    public void Stop()
    {
        sampling.Cancel();

        if (!process.HasExited)
        {
            process.Kill(entireProcessTree: true);
            process.WaitForExit(10_000);
        }
    }

    public void Dispose()
    {
        Stop();
        sampling.Dispose();
        process.Dispose();
    }

    private void OnOutput(object sender, DataReceivedEventArgs message)
    {
        if (message.Data?.Contains(DatabaseCommandMarker, StringComparison.Ordinal) == true)
        {
            Interlocked.Increment(ref commandCount);
        }
    }

    private async Task SampleMemoryAsync()
    {
        while (!sampling.IsCancellationRequested)
        {
            try
            {
                process.Refresh();

                long current = process.WorkingSet64;

                if (current > Interlocked.Read(ref peak))
                {
                    Interlocked.Exchange(ref peak, current);
                }
            }
            catch (InvalidOperationException)
            {
                return;
            }

            try
            {
                await Task.Delay(50, sampling.Token);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }
}
