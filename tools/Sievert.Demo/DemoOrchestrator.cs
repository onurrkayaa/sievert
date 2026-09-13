using System.Diagnostics;
using System.Text.Json;

using DotNet.Testcontainers.Builders;

using Microsoft.EntityFrameworkCore;

using Sievert.Data;

using Testcontainers.PostgreSql;

namespace Sievert.Demo;

/// <summary>
/// Tek komutluk demo ortami.
///
/// Yaptigi sey elle yapilacak seyin aynisi: bir PostgreSQL acmak, migration uygulamak,
/// sabit demo verisini yazmak, API'yi ve paneli baslatmak. Farki yalnizca hepsini
/// sirayla ve temizligi garanti ederek yapmasi.
///
/// Onkosul eksikse **acik** hata veriyor: "Docker calismiyor" cumlesi, yigin izinden
/// cok daha faydali.
/// </summary>
public static class DemoOrchestrator
{
    private const string PostgresImage = "postgres:17";

    public static async Task<int> RunAsync(DemoArguments options, CancellationToken cancellation)
    {
        if (FindRepositoryRoot(Directory.GetCurrentDirectory()) is not string root)
        {
            Console.Error.WriteLine(
                "Sievert.slnx bulunamadi. Komutu depo icinden calistir.");

            return 2;
        }

        string seedDirectory = Path.Combine(root, "data", "asama6", "demo");

        if (!File.Exists(Path.Combine(seedDirectory, "demo-seed.json")))
        {
            Console.Error.WriteLine(
                $"Demo verisi yok: {Path.Combine("data", "asama6", "demo", "demo-seed.json")}");

            return 2;
        }

        if (!await DockerIsAvailableAsync(cancellation))
        {
            Console.Error.WriteLine(
                "Docker'a ulasilamiyor. Demo bir PostgreSQL container'i aciyor; "
                + "Docker Desktop'i baslatip tekrar dene.");

            return 2;
        }

        PostgreSqlContainer database = new PostgreSqlBuilder(PostgresImage)
            .WithDatabase("sievert_demo")
            .WithUsername("sievert")
            .WithPassword("sievert-demo")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilCommandIsCompleted("pg_isready"))
            .Build();

        DemoProcess? api = null;
        DemoProcess? web = null;
        int exitCode = 0;

        Stopwatch total = Stopwatch.StartNew();

        try
        {
            Console.WriteLine("PostgreSQL baslatiliyor...");

            Stopwatch step = Stopwatch.StartNew();

            await database.StartAsync(cancellation);

            string connection = database.GetConnectionString();

            Report("veritabani hazir", step);

            step.Restart();

            await using (SievertContext context = SievertContextBuilder.Create(connection))
            {
                await context.Database.MigrateAsync(cancellation);
            }

            Report("migration uygulandi", step);

            step.Restart();

            ImportResult imported;

            await using (SievertContext context = SievertContextBuilder.Create(connection))
            {
                imported = await SeedImporter.ImportAsync(context, seedDirectory, cancellation);
            }

            Report($"demo verisi yazildi ({imported.Commits} commit, {imported.Findings} bulgu)", step);

            int apiPort = options.ApiPort ?? DemoProcess.FreePort();
            int webPort = options.WebPort ?? DemoProcess.FreePort();

            string apiUrl = $"http://127.0.0.1:{apiPort}";
            string webUrl = $"http://127.0.0.1:{webPort}";

            step.Restart();

            api = DemoProcess.Start(
                "API",
                Path.Combine(root, "src", "Sievert.Api"),
                apiUrl,
                connection,
                apiUrl: null,
                root,
                options.Verbose);

            using HttpClient client = new() { Timeout = TimeSpan.FromSeconds(30) };

            if (!await WaitAsync(client, $"{apiUrl}/api/v1/health", api, options, cancellation))
            {
                return Fail(api, "API hazir olmadi.");
            }

            Report("API hazir", step);

            step.Restart();

            web = DemoProcess.Start(
                "Panel",
                Path.Combine(root, "src", "Sievert.Web"),
                webUrl,
                connection,
                apiUrl,
                root,
                options.Verbose);

            if (!await WaitAsync(client, webUrl, web, options, cancellation))
            {
                return Fail(web, "Panel hazir olmadi.");
            }

            Report("panel hazir", step);

            if (!await VerifyAsync(client, apiUrl, imported, cancellation))
            {
                Console.Error.WriteLine("Demo verisi beklenen sekilde gorunmuyor.");

                return 1;
            }

            Console.WriteLine();
            Console.WriteLine($"Panel : {webUrl}");
            Console.WriteLine($"API   : {apiUrl}/api/v1/health");
            Console.WriteLine($"Depo  : {webUrl}/repositories/{imported.RepositoryId}");
            Console.WriteLine();
            Console.WriteLine("Bu veri kamuya acik Polly gecmisinden alinmis sabit bir alt kume; "
                + "canli bir depo analizi degil.");
            Console.WriteLine($"Hazir olma suresi: {total.Elapsed.TotalSeconds:F1} saniye");

            if (options.SmokeTest)
            {
                exitCode = await DemoSmokeTest.RunAsync(client, apiUrl, webUrl, imported, cancellation);
            }
            else
            {
                if (options.OpenBrowser)
                {
                    OpenBrowser(webUrl);
                }

                Console.WriteLine();
                Console.WriteLine("Kapatmak icin Ctrl+C.");

                await WaitForInterruptAsync(cancellation);
            }
        }
        catch (OperationCanceledException)
        {
            // Ctrl+C: asagidaki temizlik yine calisiyor.
        }
        catch (Exception error)
        {
            Console.Error.WriteLine($"Demo baslatilamadi: {error.Message}");

            if (options.Verbose)
            {
                Console.Error.WriteLine(error);
            }

            exitCode = 1;
        }
        finally
        {
            Console.WriteLine();
            Console.WriteLine("Kapatiliyor...");

            // Sira onemli: once panel (API'ye bagli), sonra API, en son veritabani.
            web?.Stop(TimeSpan.FromSeconds(10));
            api?.Stop(TimeSpan.FromSeconds(10));

            web?.Dispose();
            api?.Dispose();

            if (options.KeepDatabase)
            {
                Console.WriteLine($"Veritabani birakildi: {database.Id}");
            }
            else
            {
                try
                {
                    await database.DisposeAsync();
                }
                catch (Exception error)
                {
                    Console.Error.WriteLine($"Container kapatilamadi: {error.Message}");
                }
            }

            Console.WriteLine("Kapandi.");
        }

        return exitCode;
    }

    /// <summary>Cozum dosyasi bulunana kadar yukari cikar; calisma dizinine guvenmiyor.</summary>
    public static string? FindRepositoryRoot(string start)
    {
        DirectoryInfo? directory = new(Path.GetFullPath(start));

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Sievert.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        return null;
    }

    private static async Task<bool> DockerIsAvailableAsync(CancellationToken cancellation)
    {
        try
        {
            using Process? probe = Process.Start(new ProcessStartInfo("docker", "info")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            });

            if (probe is null)
            {
                return false;
            }

            await probe.WaitForExitAsync(cancellation);

            return probe.ExitCode == 0;
        }
        catch (Exception error) when (error is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            return false;
        }
    }

    private static async Task<bool> WaitAsync(
        HttpClient client,
        string url,
        DemoProcess process,
        DemoArguments options,
        CancellationToken cancellation)
    {
        DateTimeOffset deadline = DateTimeOffset.UtcNow.AddSeconds(options.StartupTimeoutSeconds);

        while (DateTimeOffset.UtcNow < deadline)
        {
            if (process.HasExited)
            {
                return false;
            }

            try
            {
                using HttpResponseMessage response = await client.GetAsync(url, cancellation);

                if (response.IsSuccessStatusCode)
                {
                    return true;
                }
            }
            catch (HttpRequestException)
            {
                // Henuz dinlemiyor.
            }

            await Task.Delay(250, cancellation);
        }

        return false;
    }

    /// <summary>Demo verisinin gercekten gorundugunu kontrol eder.</summary>
    private static async Task<bool> VerifyAsync(
        HttpClient client, string apiUrl, ImportResult imported, CancellationToken cancellation)
    {
        using JsonDocument document = JsonDocument.Parse(
            await client.GetStringAsync($"{apiUrl}/api/v1/repositories/{imported.RepositoryId}", cancellation));

        JsonElement repository = document.RootElement.GetProperty("repository");

        return repository.GetProperty("isDemoData").GetBoolean()
            && repository.GetProperty("commitCount").GetInt32() > 0;
    }

    private static int Fail(DemoProcess process, string message)
    {
        Console.Error.WriteLine(message);
        Console.Error.WriteLine($"{process.Name} sureci son satirlari:");

        foreach (string line in process.Tail(8))
        {
            Console.Error.WriteLine($"  {line}");
        }

        return 1;
    }

    private static void Report(string message, Stopwatch step) =>
        Console.WriteLine($"  {message} ({step.Elapsed.TotalSeconds:F1} sn)");

    private static void OpenBrowser(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch (Exception error) when (error is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            Console.WriteLine($"Tarayici acilamadi; adresi elle ac: {url}");
        }
    }

    private static async Task WaitForInterruptAsync(CancellationToken cancellation)
    {
        TaskCompletionSource interrupted = new();

        ConsoleCancelEventHandler handler = (_, args) =>
        {
            args.Cancel = true;
            interrupted.TrySetResult();
        };

        Console.CancelKeyPress += handler;

        try
        {
            await using (cancellation.Register(() => interrupted.TrySetResult()))
            {
                await interrupted.Task;
            }
        }
        finally
        {
            Console.CancelKeyPress -= handler;
        }
    }
}
