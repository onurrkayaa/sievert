using LibGit2Sharp;

using Sievert.Core.Mining;
using Sievert.Mining;

namespace Sievert.Tests;

public class RepositoryMinerTests
{
    [Fact]
    public void AMergeCommit_IsLeftOutButCounted()
    {
        using TemporaryRepository repository = new();
        Commit first = repository.Commit("a.cs", "class A;", "ilk commit");
        Commit yan = repository.DetachedCommit(first, "yan dal");
        repository.Commit("b.cs", "class B;", "ikinci commit");
        repository.MergeCommit(yan, "dallari birlestir");

        RepositoryMiner miner = new();
        List<CommitRecord> commits = [.. miner.Read(repository.Path, MiningOptions.All)];

        Assert.Equal(1, miner.SkippedMergeCount);
        Assert.DoesNotContain(commits, commit => commit.MessageSubject == "dallari birlestir");
        Assert.All(commits, commit => Assert.True(commit.ParentCount <= 1));
    }

    [Fact]
    public void ARename_CarriesTheOldPath()
    {
        using TemporaryRepository repository = new();

        // Ad degisimi benzerlikle bulunuyor; icerik ayni kalirsa benzerlik %100 oluyor.
        repository.Commit("eski/Hasta.cs", "class Hasta { int Yas; string Ad; }", "dosyayi ekle");
        repository.Rename("eski/Hasta.cs", "yeni/Hasta.cs", "dosyayi tasi");

        FileChange change = Read(repository)
            .Single(commit => commit.MessageSubject == "dosyayi tasi")
            .Files
            .Single();

        Assert.Equal(FileChangeKind.Renamed, change.Kind);
        Assert.Equal("yeni/Hasta.cs", change.Path);
        Assert.Equal("eski/Hasta.cs", change.OldPath);
    }

    [Fact]
    public void CoAuthoredByLines_AreParsedOutOfTheMessage()
    {
        using TemporaryRepository repository = new();
        repository.Commit(
            "a.cs",
            "class A;",
            "iki kisi yazdi\n\nCo-Authored-By: Ayse Yilmaz <ayse@example.com>\nCo-authored-by: Mehmet <MEHMET@Example.COM>\n");

        CommitRecord commit = Read(repository).Single();

        Assert.Equal(2, commit.CoAuthors.Count);
        Assert.Equal("Ayse Yilmaz", commit.CoAuthors[0].Name);
        Assert.Equal("ayse@example.com", commit.CoAuthors[0].Email);

        // Kucuk harfe ceviriliyor, cunku kimlik epostayla belirleniyor.
        Assert.Equal("mehmet@example.com", commit.CoAuthors[1].Email);

        // Mesajin kendisine dokunulmuyor.
        Assert.Contains("Co-Authored-By:", commit.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ABotAuthor_IsFlaggedButKept()
    {
        using TemporaryRepository repository = new();
        repository.Commit(
            "paket.json",
            "{}",
            "bagimliligi guncelle",
            TemporaryRepository.Person("dependabot[bot]", "49699333+dependabot[bot]@users.noreply.github.com"));
        repository.Commit("a.cs", "class A;", "elle yazilmis commit");

        IReadOnlyList<CommitRecord> commits = Read(repository);

        Assert.Equal(2, commits.Count);
        Assert.True(commits.Single(commit => commit.MessageSubject == "bagimliligi guncelle").AuthorLooksLikeBot);
        Assert.False(commits.Single(commit => commit.MessageSubject == "elle yazilmis commit").AuthorLooksLikeBot);
    }

    [Fact]
    public void AnEmptyCommit_IsReadWithoutFiles()
    {
        using TemporaryRepository repository = new();
        repository.Commit("a.cs", "class A;", "ilk commit");
        repository.EmptyCommit("hicbir sey degismedi");

        CommitRecord commit = Read(repository).Single(found => found.MessageSubject == "hicbir sey degismedi");

        Assert.Empty(commit.Files);
        Assert.Equal(0, commit.Summary.LinesAdded);
        Assert.Equal(0, commit.Summary.LinesDeleted);
        Assert.Equal(0, commit.Summary.ChangedFileCount);
        Assert.Equal(0, commit.Summary.ChangedCSharpFileCount);
    }

    [Fact]
    public void EveryPath_IsRelativeToTheRepositoryRoot()
    {
        using TemporaryRepository repository = new();
        repository.Commit("src/Derin/Klasor/Hasta.cs", "class Hasta;", "derin dosya ekle");

        FileChange change = Read(repository).Single().Files.Single();

        Assert.Equal("src/Derin/Klasor/Hasta.cs", change.Path);
        Assert.False(Path.IsPathRooted(change.Path));
        Assert.DoesNotContain(repository.Path, change.Path, StringComparison.Ordinal);
    }

    [Fact]
    public void OnlyCSharpFiles_AreCountedSeparately()
    {
        using TemporaryRepository repository = new();
        repository.Commit("a.cs", "class A;\nclass B;\n", "kod ekle");
        repository.Commit("okuma.md", "# baslik\n", "doküman ekle");

        CommitRecord kod = Read(repository).Single(commit => commit.MessageSubject == "kod ekle");
        CommitRecord dokuman = Read(repository).Single(commit => commit.MessageSubject == "doküman ekle");

        Assert.Equal(1, kod.Summary.ChangedCSharpFileCount);
        Assert.Equal(2, kod.Summary.LinesAdded);
        Assert.Equal(1, dokuman.Summary.ChangedFileCount);
        Assert.Equal(0, dokuman.Summary.ChangedCSharpFileCount);
    }

    [Fact]
    public void MaxCommits_StopsTheWalk()
    {
        using TemporaryRepository repository = new();
        repository.Commit("a.cs", "class A;", "bir");
        repository.Commit("b.cs", "class B;", "iki");
        repository.Commit("c.cs", "class C;", "uc");

        List<CommitRecord> commits = [.. new RepositoryMiner().Read(repository.Path, new MiningOptions(MaxCommits: 2))];

        // Tarih en yeniden eskiye yuruyor, yani ilk gelen son commit.
        Assert.Equal(2, commits.Count);
        Assert.Equal("uc", commits[0].MessageSubject);
    }

    [Fact]
    public void Since_LeavesOutOlderCommits()
    {
        using TemporaryRepository repository = new();
        Signature eski = new("Onur", "onur@example.com", new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero));
        repository.Commit("a.cs", "class A;", "eski commit", eski);
        repository.Commit("b.cs", "class B;", "yeni commit");

        List<CommitRecord> commits =
            [.. new RepositoryMiner().Read(repository.Path, new MiningOptions(Since: new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero)))];

        Assert.Single(commits);
        Assert.Equal("yeni commit", commits[0].MessageSubject);
    }

    [Fact]
    public void AuthorDate_IsConvertedToUtc()
    {
        using TemporaryRepository repository = new();

        // Yardimci +03:00 ile yaziyor; UTC'ye cevrilince saat 09:00 olmali.
        repository.Commit("a.cs", "class A;", "tek commit");

        CommitRecord commit = Read(repository).Single();

        Assert.Equal(TimeSpan.Zero, commit.AuthorDateUtc.Offset);
        Assert.Equal(9, commit.AuthorDateUtc.Hour);
    }

    private static IReadOnlyList<CommitRecord> Read(TemporaryRepository repository) =>
        [.. new RepositoryMiner().Read(repository.Path, MiningOptions.All)];
}
