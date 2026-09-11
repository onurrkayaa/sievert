using System.Reflection;
using System.Runtime.InteropServices;

using Sievert.Analysis;
using Sievert.Analysis.Rules;
using Sievert.Cli;
using Sievert.Core;
using Sievert.Core.Analysis;
using Sievert.Core.Rules;

ParseResult result = ArgumentParser.Parse(args);

if (result.Options is null)
{
    if (args.Length == 0)
    {
        string version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.0.0";
        ConsoleWriter.Write(
            Banner.Render(version, RuntimeInformation.FrameworkDescription)
                .Select(line => new OutputLine([new OutputSpan(line.Text, line.IsTitle ? OutputColor.Heading : OutputColor.Dim)]))
                .ToList(),
            ConsoleWriter.UseColor());
        Console.WriteLine();
        Console.WriteLine(ArgumentParser.HelpText);
        return 0;
    }

    Console.Error.WriteLine(result.Error);
    Console.Error.WriteLine();
    Console.Error.WriteLine(ArgumentParser.HelpText);
    return 1;
}

CommandOptions options = result.Options;

if (!File.Exists(options.TargetPath) && !Directory.Exists(options.TargetPath))
{
    Console.Error.WriteLine($"Bulunamadi: {options.TargetPath}");
    return 1;
}

IReadOnlyList<string> files = SourceFileFinder.Find(options.TargetPath);

if (files.Count == 0)
{
    Console.Error.WriteLine($"Taranacak .cs dosyasi yok: {options.TargetPath}");
    return 1;
}

string root = ScanRoot.Find(options.TargetPath);

return options switch
{
    ScanOptions scan => RunScan(scan, files, root),
    CheckOptions check => RunCheck(check, files, root),
    _ => throw new InvalidOperationException("Bilinmeyen komut turu."),
};

static int RunScan(ScanOptions options, IReadOnlyList<string> files, string root)
{
    IReadOnlyList<FileAnalysis> analyses = ScanRoot.MakePathsRelative(
        files.Select(FileAnalyzer.AnalyzeFile).ToList(),
        root);

    ScanSummary summary = Summarizer.Summarize(analyses);
    IReadOnlyList<MethodLocation> longest = options.TopCount is int count
        ? Summarizer.LongestMethods(analyses, count)
        : [];

    if (options.Json)
    {
        Console.Out.WriteLine(JsonFormatter.Format(root, analyses, summary, longest));
        return 0;
    }

    ConsoleWriter.Write(
        TreeFormatter.Format(analyses, summary, longest),
        ConsoleWriter.UseColor());

    return 0;
}

static int RunCheck(CheckOptions options, IReadOnlyList<string> files, string root)
{
    // Kural listesi simdilik burada duruyor. Yol haritasinda JSON'dan okumak var.
    RuleRunner runner = new([new AsyncVoidRule()]);

    IReadOnlyList<Finding> findings = runner.Run(files, root);
    CheckSummary summary = CheckSummary.Of(files.Count, findings);

    if (options.Json)
    {
        Console.Out.WriteLine(JsonFormatter.FormatCheck(root, findings, summary));
    }
    else
    {
        ConsoleWriter.Write(
            DiagnosticCardFormatter.Format(findings, summary),
            ConsoleWriter.UseColor());
    }

    return CheckCommand.ExitCode(findings, options.FailOn);
}
