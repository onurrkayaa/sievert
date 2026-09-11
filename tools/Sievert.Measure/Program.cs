using Sievert.Data;
using Sievert.Data.Metrics;
using Sievert.Measure;
using Sievert.Mining;

// Olcum programi. Urunun parcasi degil: raporlardaki sayilari uretmek icin var ve
// davranis degistirmiyor, sadece okuyup karsilastiriyor. Cikti dogrudan
// docs/olcumler/ altindaki dosyalara giriyor.
if (args.Length < 2)
{
    Console.Error.WriteLine("Kullanim: measure <isfix|bot|szz> <repo-adi>");
    return 2;
}

string? connection = Environment.GetEnvironmentVariable(ConnectionString.EnvironmentVariable);

if (string.IsNullOrWhiteSpace(connection))
{
    Console.Error.WriteLine($"{ConnectionString.EnvironmentVariable} tanimli degil.");
    return 2;
}

using SievertContext context = SievertContextBuilder.Create(connection);
MetricsRunner runner = new(context);

if (runner.FindRepository(args[1]) is not { } repository)
{
    Console.Error.WriteLine($"Veritabaninda boyle bir depo yok: {args[1]}");
    return 2;
}

List<CommitForMetrics> commits = [.. runner.Read(repository.Id)];
HashSet<int> bots = [.. context.Commits.Where(row => row.RepositoryId == repository.Id && row.IsBot).Select(row => row.Id)];

switch (args[0])
{
    case "isfix":
        IsFixRecall.Report(commits);
        return 0;

    case "bot":
        BotImpact.Report(commits, bots);
        return 0;

    case "szz":
        if (repository.LocalPath is not string path || !Directory.Exists(path))
        {
            Console.Error.WriteLine("Deponun yerel klasoru kayitli degil; once mine --db calistir.");
            return 2;
        }

        SzzVariants.Report(
            path,
            [.. new LabelStore(context).Fixes(repository.Id).Select(fix => new SzzFix(fix.Sha, fix.Date))]);

        return 0;

    default:
        Console.Error.WriteLine($"Bilinmeyen olcum: {args[0]}");
        return 2;
}
