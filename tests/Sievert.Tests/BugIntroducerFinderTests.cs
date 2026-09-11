using LibGit2Sharp;

using Sievert.Mining;

namespace Sievert.Tests;

public class BugIntroducerFinderTests
{
    [Fact]
    public void AFixThatChangesAnEarlierLine_BlamesThatCommit()
    {
        using TemporaryRepository repository = new();
        Commit guilty = repository.Commit("src/a.cs", "class A\n{\n    int Yas = 1;\n}\n", "ozellik ekle");
        Commit fix = repository.Commit("src/a.cs", "class A\n{\n    int Yas = 2;\n}\n", "fix yanlis deger");

        SzzOutcome outcome = Run(repository, fix);

        Assert.Contains(guilty.Sha, outcome.BlamedShas);
    }

    [Fact]
    public void AFixThatOnlyAddsANewFile_BlamesNobody()
    {
        using TemporaryRepository repository = new();
        repository.Commit("src/a.cs", "class A;\n", "ozellik ekle");
        Commit fix = repository.Commit("src/b.cs", "class B;\n", "fix yeni dosya");

        Assert.Empty(Run(repository, fix).BlamedShas);
    }

    [Fact]
    public void AFixTouchingMoreFilesThanTheLimit_IsSkipped()
    {
        using TemporaryRepository repository = new();
        Commit fix = WriteFiles(repository, 51, "fix buyuk temizlik");

        SzzOutcome outcome = Run(repository, fix);

        Assert.Equal(1, outcome.SkippedLargeFixes);
        Assert.Empty(outcome.BlamedShas);
    }

    [Fact]
    public void AFixExactlyAtTheLimit_IsNotSkipped()
    {
        using TemporaryRepository repository = new();
        Commit fix = WriteFiles(repository, 50, "fix tam sinirda");

        Assert.Equal(0, Run(repository, fix).SkippedLargeFixes);
    }

    [Fact]
    public void ABlamedCommitNewerThanTheFix_IsDropped()
    {
        using TemporaryRepository repository = new();

        // Suclanan commit duzeltmeden SONRA yazilmis gorunuyor: git yazar tarihi
        // serbestce verilebiliyor, rebase ve cherry-pick de bunu uretebiliyor.
        Signature late = new("Onur", "onur@example.com", new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero));
        repository.Commit("src/a.cs", "class A\n{\n    int Yas = 1;\n}\n", "gelecekten commit", late);

        Signature early = new("Onur", "onur@example.com", new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero));
        Commit fix = repository.Commit("src/a.cs", "class A\n{\n    int Yas = 2;\n}\n", "fix deger", early);

        SzzOutcome outcome = Run(repository, fix);

        Assert.Empty(outcome.BlamedShas);
        Assert.Equal(1, outcome.DroppedByTime);
    }

    [Fact]
    public void RunningTwice_GivesTheSameResult()
    {
        using TemporaryRepository repository = new();
        repository.Commit("src/a.cs", "class A\n{\n    int Yas = 1;\n}\n", "ozellik ekle");
        Commit fix = repository.Commit("src/a.cs", "class A\n{\n    int Yas = 2;\n}\n", "fix deger");

        SzzOutcome first = Run(repository, fix);
        SzzOutcome second = Run(repository, fix);

        Assert.Equal(first.BlamedShas.Order(StringComparer.Ordinal), second.BlamedShas.Order(StringComparer.Ordinal));
        Assert.Equal(first.BlamedLines, second.BlamedLines);
    }

    [Fact]
    public void OnlyCSharpFilesAreBlamed()
    {
        using TemporaryRepository repository = new();
        repository.Commit("okuma.md", "birinci satir\n", "dokuman ekle");
        Commit fix = repository.Commit("okuma.md", "ikinci satir\n", "fix dokuman");

        Assert.Empty(Run(repository, fix).BlamedShas);
    }

    [Fact]
    public void WhitespaceOnlyChanges_AreNotBlamedWhenIgnored()
    {
        using TemporaryRepository repository = new();
        repository.Commit("src/a.cs", "class A\n{\n    int Yas = 1;\n}\n", "ozellik ekle");

        // Sadece girinti degisti; satirin kendisi ayni.
        Commit fix = repository.Commit("src/a.cs", "class A\n{\n        int Yas = 1;\n}\n", "fix girinti");

        Assert.Empty(Run(repository, fix, new SzzOptions(IgnoreWhitespace: true)).BlamedShas);
        Assert.NotEmpty(Run(repository, fix, new SzzOptions(IgnoreWhitespace: false)).BlamedShas);
    }

    /// <summary>Tek commit'te verilen sayida dosya degistirir; sinir testleri icin.</summary>
    private static Commit WriteFiles(TemporaryRepository repository, int count, string message)
    {
        for (int i = 0; i < count; i++)
        {
            repository.Commit($"src/d{i}.cs", $"class D{i} {{ int X = 1; }}\n", $"dosya {i}");
        }

        return repository.CommitMany(
            Enumerable.Range(0, count).ToDictionary(i => $"src/d{i}.cs", i => $"class D{i} {{ int X = 2; }}\n"),
            message);
    }

    private static SzzOutcome Run(TemporaryRepository repository, Commit fix, SzzOptions? options = null) =>
        new BugIntroducerFinder().Find(
            repository.Path,
            [new SzzFix(fix.Sha, fix.Author.When.ToUniversalTime())],
            options ?? SzzOptions.Default);
}
