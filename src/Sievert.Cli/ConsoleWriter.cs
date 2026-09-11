namespace Sievert.Cli;

/// <summary>Bicimlendirilmis satirlari ekrana basar. Renk sadece burada devreye giriyor.</summary>
public static class ConsoleWriter
{
    /// <summary>Cikti dosyaya ya da boruya yonlendirildiyse renk kapanir.</summary>
    public static bool UseColor() => !Console.IsOutputRedirected;

    public static void Write(IReadOnlyList<OutputLine> lines, bool colored)
    {
        foreach (OutputLine line in lines)
        {
            if (!colored)
            {
                Console.WriteLine(line.PlainText);
                continue;
            }

            foreach (OutputSpan span in line.Spans)
            {
                Console.ForegroundColor = ToConsoleColor(span.Color);
                Console.Write(span.Text);
            }

            Console.ResetColor();
            Console.WriteLine();
        }
    }

    private static ConsoleColor ToConsoleColor(OutputColor color) => color switch
    {
        OutputColor.Dim => ConsoleColor.DarkGray,
        OutputColor.Heading => ConsoleColor.Blue,
        OutputColor.Tag => ConsoleColor.Cyan,
        OutputColor.Warning => ConsoleColor.Yellow,
        _ => ConsoleColor.Gray,
    };
}
