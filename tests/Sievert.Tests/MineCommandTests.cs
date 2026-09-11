using Sievert.Cli;

namespace Sievert.Tests;

[Collection(ConsoleCollection.Name)]
public class MineCommandTests : IDisposable
{
    private readonly TextWriter stdout = Console.Out;
    private readonly TextWriter stderr = Console.Error;

    [Fact]
    public void Mine_WritesOneJsonLinePerCommit()
    {
        using TemporaryRepository repository = new();
        repository.Commit("a.cs", "class A;", "bir");
        repository.Commit("b.cs", "class B;", "iki");

        string output = Path.Combine(Path.GetTempPath(), "sievert-mine-" + Guid.NewGuid().ToString("n") + ".jsonl");

        try
        {
            Assert.Equal(ExitCodes.Clean, Run("mine", repository.Path, "--out", output));

            string[] lines = File.ReadAllLines(output);

            Assert.Equal(2, lines.Length);
            Assert.All(lines, line => Assert.StartsWith("{", line, StringComparison.Ordinal));
            Assert.All(lines, line => Assert.Contains("\"sha\"", line, StringComparison.Ordinal));
        }
        finally
        {
            File.Delete(output);
        }
    }

    [Fact]
    public void Mine_PrintsTheSummary()
    {
        using TemporaryRepository repository = new();
        repository.Commit("a.cs", "class A;", "tek commit");

        string printed = Capture(() => Run("mine", repository.Path));

        Assert.Contains("commit", printed, StringComparison.Ordinal);
        Assert.Contains("atlanan birlestirme", printed, StringComparison.Ordinal);
        Assert.Contains("farkli yazar", printed, StringComparison.Ordinal);
    }

    [Fact]
    public void MineOnSomethingThatIsNotARepository_IsAToolError()
    {
        string directory = Path.Combine(Path.GetTempPath(), "sievert-bos-" + Guid.NewGuid().ToString("n"));
        Directory.CreateDirectory(directory);

        try
        {
            Console.SetError(TextWriter.Null);
            Assert.Equal(ExitCodes.ToolError, Run("mine", directory));
        }
        finally
        {
            Directory.Delete(directory);
        }
    }

    [Fact]
    public void MineStillRejectsTheOldJsonFlag()
    {
        // --json artik mine'da yok. Sessiz uyumluluk birakmadim: eski bayragi yazan bir
        // betik hata almali, yoksa ciktiyi hic yazmadan basariyla bitmis gorunur.
        using TemporaryRepository repository = new();
        repository.Commit("a.cs", "class A;", "bir");

        Console.SetError(TextWriter.Null);

        Assert.Equal(ExitCodes.ToolError, Run("mine", repository.Path, "--json", "cikti.jsonl"));
        Assert.False(File.Exists("cikti.jsonl"));
    }

    [Fact]
    public void MineWithoutAPath_IsAUsageError()
    {
        Console.SetError(TextWriter.Null);
        Assert.Equal(ExitCodes.ToolError, Run("mine"));
    }

    private static int Run(params string[] args) => CommandRunner.Run(args);

    private static string Capture(Action action)
    {
        StringWriter writer = new();
        Console.SetOut(writer);
        action();

        return writer.ToString();
    }

    public void Dispose()
    {
        Console.SetOut(stdout);
        Console.SetError(stderr);
    }
}
