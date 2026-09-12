using Sievert.Data;
using Sievert.Data.Metrics;
using Sievert.Measure;
using Sievert.Mining;

// Olcum programi. Urunun parcasi degil: raporlardaki sayilari uretmek icin var ve
// davranis degistirmiyor, sadece okuyup karsilastiriyor. Cikti dogrudan
// docs/olcumler/ altindaki dosyalara giriyor.
if (args.Length < 2)
{
    Console.Error.WriteLine("Kullanim: measure <isfix|bot|szz|blame-w|dogrulama|sizinti|satir-kontrol|malzeme|sayim> <repo-adi>\n"
        + "         measure snapshot <cikti-dosyasi>\n"
        + "         measure split <anlik-goruntu.csv> <ozet.sha256> <manifest.csv>\n"
        + "         measure history-check <anlik-goruntu.csv> <ozet.sha256>\n"
        + "         measure baseline <anlik.csv> <anlik.sha256> <manifest.csv> <manifest.sha256> <commit> <cikti.json>");
    return 2;
}

// Sayim veritabanina hic bakmiyor, dosya okuyor; baglanti aranmadan once ele aliniyor.
if (args[0] == "sayim")
{
    Tally.Report(args[1]);
    return 0;
}

// Bolme de veritabanina bakmiyor: dondurulmus dosyayi okuyup manifest yaziyor.
// args: split <anlik-goruntu.csv> <ozet.sha256> <manifest.csv>
// Taban cizgileri de veritabanina bakmiyor: iki dondurulmus dosyayi okuyup sonuc yaziyor.
// args: baseline <anlik.csv> <anlik.sha256> <manifest.csv> <manifest.sha256> <commit> <cikti.json>
if (args[0] == "baseline")
{
    if (args.Length < 7)
    {
        Console.Error.WriteLine(
            "Kullanim: measure baseline <anlik.csv> <anlik.sha256> <manifest.csv> <manifest.sha256> <commit> <cikti.json>");
        return 2;
    }

    return BaselineCommand.Run(args[1], args[2], args[3], args[4], args[5], args[6]);
}

// Model de dondurulmus dosyalari okuyor.
// args: model <anlik.csv> <anlik.sha256> <manifest.csv> <manifest.sha256> <baseline.sha256> <commit> <cikti-klasoru>
if (args[0] == "model")
{
    if (args.Length < 8)
    {
        Console.Error.WriteLine(
            "Kullanim: measure model <anlik.csv> <anlik.sha256> <manifest.csv> <manifest.sha256> "
            + "<baseline.sha256> <commit> <cikti-klasoru>");
        return 2;
    }

    return ModelCommand.Run(args);
}

// Adim 3b: kalibrasyon ve bootstrap. args: calibration <veri-klasoru> <commit> <grafik-klasoru>
if (args[0] == "calibration")
{
    if (args.Length < 4)
    {
        Console.Error.WriteLine("Kullanim: measure calibration <veri-klasoru> <commit> <grafik-klasoru>");
        return 2;
    }

    return CalibrationCommand.Run(args);
}

// Adim 4: duyarlilik deneyleri. args: sensitivity <veri-klasoru> <commit>
if (args[0] == "sensitivity")
{
    if (args.Length < 3)
    {
        Console.Error.WriteLine("Kullanim: measure sensitivity <veri-klasoru> <commit>");
        return 2;
    }

    return SensitivityCommand.Run(args);
}

// Adim 5: repo-arasi genelleme. args: generalization <veri-klasoru> <commit>
if (args[0] == "generalization")
{
    if (args.Length < 3)
    {
        Console.Error.WriteLine("Kullanim: measure generalization <veri-klasoru> <commit>");
        return 2;
    }

    return GeneralizationCommand.Run(args);
}

// Adim 4.5: ad degisimi duyarliligi. args: rename <veri> <commit> <cikti> <kimlik=yol;...>
if (args[0] == "rename")
{
    if (args.Length < 5)
    {
        Console.Error.WriteLine("Kullanim: measure rename <veri-klasoru> <commit> <cikti-klasoru> <kimlik=yol;...>");
        return 2;
    }

    return RenameSensitivity.Run(args);
}

// Adim 5b: sentetik gurultuyu taban oranindan ayir. args: noise-normalized <veri> <commit>
if (args[0] == "noise-normalized")
{
    if (args.Length < 3)
    {
        Console.Error.WriteLine("Kullanim: measure noise-normalized <veri-klasoru> <commit>");
        return 2;
    }

    return NoiseNormalizedCommand.Run(args);
}

if (args[0] == "split")
{
    if (args.Length < 4)
    {
        Console.Error.WriteLine("Kullanim: measure split <anlik-goruntu.csv> <ozet.sha256> <manifest.csv>");
        return 2;
    }

    return SplitWriter.Write(args[1], args[2], args[3]);
}

string? connection = Environment.GetEnvironmentVariable(ConnectionString.EnvironmentVariable);

if (string.IsNullOrWhiteSpace(connection))
{
    Console.Error.WriteLine($"{ConnectionString.EnvironmentVariable} tanimli degil.");
    return 2;
}

using SievertContext context = SievertContextBuilder.Create(connection);
MetricsRunner runner = new(context);

// Snapshot butun repolari birden disari aktariyor, tek bir depo adi almiyor;
// o yuzden depo aramasindan once ele aliniyor. args[1] cikti dosyasi.
if (args[0] == "snapshot")
{
    return Snapshot.Write(context, args[1]);
}

// Tarihsel oznitelik kontrolu de uc repoyu birden geziyor.
// args: history-check <anlik-goruntu.csv> <ozet.sha256>
if (args[0] == "history-check")
{
    if (args.Length < 3)
    {
        Console.Error.WriteLine("Kullanim: measure history-check <anlik-goruntu.csv> <ozet.sha256>");
        return 2;
    }

    return HistoryRecheck.Report(context, args[1], args[2]);
}

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

    case "blame-w":
        if (repository.LocalPath is not string blamePath || !Directory.Exists(blamePath))
        {
            Console.Error.WriteLine("Deponun yerel klasoru kayitli degil; once mine --db calistir.");
            return 2;
        }

        BlameEquivalence.Report(
            blamePath,
            [.. new LabelStore(context).Fixes(repository.Id).Select(fix => new SzzFix(fix.Sha, fix.Date))],
            sampleSize: 50);

        return 0;

    case "dogrulama":
        if (repository.LocalPath is not string listPath || !Directory.Exists(listPath))
        {
            Console.Error.WriteLine("Deponun yerel klasoru kayitli degil; once mine --db calistir.");
            return 2;
        }

        VerificationList.Report(
            listPath,
            repository.RemoteUrl ?? repository.Name,
            [.. new LabelStore(context).Fixes(repository.Id).Select(fix => new SzzFix(fix.Sha, fix.Date))],
            rowsPerKind: 5);

        return 0;

    case "malzeme":
        if (repository.LocalPath is not string materialPath || !Directory.Exists(materialPath))
        {
            Console.Error.WriteLine("Deponun yerel klasoru kayitli degil; once mine --db calistir.");
            return 2;
        }

        Material.Report(
            materialPath,
            args[2],
            args[3],
            int.Parse(args[4], System.Globalization.CultureInfo.InvariantCulture));

        return 0;

    case "satir-kontrol":
        if (repository.LocalPath is not string recheckPath || !Directory.Exists(recheckPath))
        {
            Console.Error.WriteLine("Deponun yerel klasoru kayitli degil; once mine --db calistir.");
            return 2;
        }

        RowRecheck.Report(recheckPath, args[2], args[3]);
        return 0;

    case "sizinti":
        LeakCheck.Report(context, runner, repository.Id, sampleSize: 20);
        return 0;

    default:
        Console.Error.WriteLine($"Bilinmeyen olcum: {args[0]}");
        return 2;
}
