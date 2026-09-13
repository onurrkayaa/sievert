using Sievert.Demo;

// Demo araci. Varsayilan davranis: tek komutla calisan bir demo ortami acmak.
//
//   dotnet run --project tools/Sievert.Demo
//
// Diger fiiller yardimci: seed uretme ve ice aktarma. Ikisi de gercek veri uzerinde
// calisiyor; demo verisi uydurma degil.
if (args.Length > 0 && args[0] == "export-seed")
{
    string output = args.Length > 1
        ? args[1]
        : Path.Combine("data", "asama6", "demo");

    string commit = args.Length > 2 ? args[2] : "bilinmiyor";

    return await SeedExporter.RunAsync(output, commit, CancellationToken.None);
}

if (args.Length > 0 && args[0] == "help" || args.Contains("--help"))
{
    Console.WriteLine("Kullanim:");
    Console.WriteLine("  dotnet run --project tools/Sievert.Demo");
    Console.WriteLine("      Demo ortamini acar: PostgreSQL, migration, demo verisi, API ve panel.");
    Console.WriteLine();
    Console.WriteLine("  Secenekler:");
    Console.WriteLine("      --no-open                 Tarayiciyi acma");
    Console.WriteLine("      --keep-database           Cikista veritabani container'ini birak");
    Console.WriteLine("      --api-port <port>         API portu (varsayilan: bos bir port)");
    Console.WriteLine("      --web-port <port>         Panel portu (varsayilan: bos bir port)");
    Console.WriteLine("      --startup-timeout-seconds <saniye>");
    Console.WriteLine("      --smoke-test              Acar, kontrol eder, kapatir");
    Console.WriteLine("      --verbose                 Ayrintili gunluk");
    Console.WriteLine();
    Console.WriteLine("  dotnet run --project tools/Sievert.Demo -- export-seed <klasor> <commit>");
    Console.WriteLine("      Gercek veritabanindan demo alt kumesini uretir.");

    return 0;
}

return await DemoOrchestrator.RunAsync(DemoArguments.Parse(args), CancellationToken.None);
