using System.Reflection;
using System.Runtime.InteropServices;

using Sievert.Analysis;
using Sievert.Analysis.Rules;
using Sievert.Core;
using Sievert.Core.Analysis;
using Sievert.Core.Rules;

namespace Sievert.Cli;

/// <summary>
/// Argumanlari alip isi yurutur ve cikis kodunu dondurur. Program.cs sadece burayi cagiriyor;
/// boylece cikis kodlari testten dogrulanabiliyor.
/// </summary>
public static class CommandRunner
{
    public static int Run(string[] args)
    {
        try
        {
            return Dispatch(args);
        }
        catch (Exception error)
        {
            // Beklenmeyen her sey arac hatasi sayilir; bulgu varmis gibi gorunmesin.
            Console.Error.WriteLine($"Beklenmeyen hata: {error.Message}");
            return ExitCodes.ToolError;
        }
    }

    private static int Dispatch(string[] args)
    {
        ParseResult result = ArgumentParser.Parse(args);

        if (result.Options is null)
        {
            return args.Length == 0 ? WriteBanner() : WriteUsageError(result.Error);
        }

        CommandOptions options = result.Options;

        if (!File.Exists(options.TargetPath) && !Directory.Exists(options.TargetPath))
        {
            Console.Error.WriteLine($"Bulunamadi: {options.TargetPath}");
            return ExitCodes.ToolError;
        }

        IReadOnlyList<string> files = SourceFileFinder.Find(options.TargetPath);

        if (files.Count == 0)
        {
            Console.Error.WriteLine($"Taranacak .cs dosyasi yok: {options.TargetPath}");
            return ExitCodes.ToolError;
        }

        string root = ScanRoot.Find(options.TargetPath);

        return options switch
        {
            ScanOptions scan => RunScan(scan, files, root),
            CheckOptions check => RunCheck(check, files, root),
            _ => throw new InvalidOperationException("Bilinmeyen komut turu."),
        };
    }

    private static int RunScan(ScanOptions options, IReadOnlyList<string> files, string root)
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
            return ExitCodes.Clean;
        }

        ConsoleWriter.Write(
            TreeFormatter.Format(analyses, summary, longest),
            ConsoleWriter.UseColor());

        // scan kural calistirmiyor, o yuzden bulgu uretemez.
        return ExitCodes.Clean;
    }

    private static int RunCheck(CheckOptions options, IReadOnlyList<string> files, string root)
    {
        // Kural listesi simdilik burada duruyor. Yol haritasinda JSON'dan okumak var.
        RuleRunner runner = new([new AsyncVoidRule()]);

        RuleResult result = runner.Run(files, root);
        IReadOnlyList<Finding> findings = result.Findings;
        CheckSummary summary = CheckSummary.Of(files.Count, findings);

        if (options.Json)
        {
            Console.Out.WriteLine(JsonFormatter.FormatCheck(root, findings, result.Exemptions, summary));
        }
        else
        {
            ConsoleWriter.Write(
                DiagnosticCardFormatter.Format(findings, summary),
                ConsoleWriter.UseColor());
        }

        return CheckCommand.ExitCode(findings, options.FailOn);
    }

    private static int WriteBanner()
    {
        string version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.0.0";

        ConsoleWriter.Write(
            Banner.Render(version, RuntimeInformation.FrameworkDescription)
                .Select(line => new OutputLine([new OutputSpan(line.Text, line.IsTitle ? OutputColor.Heading : OutputColor.Dim)]))
                .ToList(),
            ConsoleWriter.UseColor());

        Console.WriteLine();
        Console.WriteLine(ArgumentParser.HelpText);

        return ExitCodes.Clean;
    }

    private static int WriteUsageError(string? error)
    {
        Console.Error.WriteLine(error);
        Console.Error.WriteLine();
        Console.Error.WriteLine(ArgumentParser.HelpText);

        return ExitCodes.ToolError;
    }
}
