using LibGit2Sharp;

namespace Sievert.Tests;

/// <summary>
/// Test icin diskte gecici bir git deposu kurar. Her test kendi deposunu aciyor ve
/// bitince siliyor; testler paralel kostugu icin paylasilan tek bir depo olamaz.
/// Disaridan hicbir sey klonlanmiyor, tarihin tamami burada uretiliyor.
/// </summary>
internal sealed class TemporaryRepository : IDisposable
{
    private readonly Repository repository;

    public TemporaryRepository()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "sievert-test-" + Guid.NewGuid().ToString("n"));
        Repository.Init(Path);
        repository = new Repository(Path);
    }

    public string Path { get; }

    /// <summary>Dosyayi yazip tek dosyalik bir commit atar ve commit'i doner.</summary>
    public Commit Commit(string relativePath, string content, string message, Signature? author = null)
    {
        string absolute = System.IO.Path.Combine(Path, relativePath);
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(absolute)!);
        File.WriteAllText(absolute, content);
        Commands.Stage(repository, relativePath);

        Signature who = author ?? Person("Onur", "onur@example.com");

        return repository.Commit(message, who, who);
    }

    /// <summary>Birden fazla dosyayi tek commit'te yazar.</summary>
    public Commit CommitMany(IReadOnlyDictionary<string, string> contents, string message, Signature? author = null)
    {
        foreach ((string relativePath, string content) in contents)
        {
            string absolute = System.IO.Path.Combine(Path, relativePath);
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(absolute)!);
            File.WriteAllText(absolute, content);
            Commands.Stage(repository, relativePath);
        }

        Signature who = author ?? Person("Onur", "onur@example.com");

        return repository.Commit(message, who, who);
    }

    /// <summary>Dosyayi yeni yola tasir ve commit atar. Icerik aynen korunuyor.</summary>
    public Commit Rename(string oldPath, string newPath, string message)
    {
        string from = System.IO.Path.Combine(Path, oldPath);
        string to = System.IO.Path.Combine(Path, newPath);
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(to)!);
        File.Move(from, to);
        Commands.Stage(repository, oldPath);
        Commands.Stage(repository, newPath);

        Signature who = Person("Onur", "onur@example.com");

        return repository.Commit(message, who, who);
    }

    /// <summary>Hicbir dosya degistirmeyen bir commit.</summary>
    public Commit EmptyCommit(string message)
    {
        Signature who = Person("Onur", "onur@example.com");

        return repository.Commit(message, who, who, new CommitOptions { AllowEmptyCommit = true });
    }

    /// <summary>
    /// Iki ebeveynli bir commit yazip HEAD'i oraya tasir. Gercek bir birlestirme akisi
    /// yurutmuyorum; test icin gereken tek sey ebeveyn sayisinin iki olmasi.
    /// </summary>
    public Commit MergeCommit(Commit second, string message)
    {
        Signature who = Person("Onur", "onur@example.com");
        Commit head = repository.Head.Tip;

        Commit merge = repository.ObjectDatabase.CreateCommit(
            who,
            who,
            message,
            head.Tree,
            [head, second],
            prettifyMessage: false);

        repository.Refs.UpdateTarget(repository.Refs.Head.ResolveToDirectReference(), merge.Id);

        return merge;
    }

    /// <summary>HEAD'in disinda duran, ebeveyni verilen bir commit uretir.</summary>
    public Commit DetachedCommit(Commit parent, string message)
    {
        Signature who = Person("Onur", "onur@example.com");

        return repository.ObjectDatabase.CreateCommit(
            who,
            who,
            message,
            parent.Tree,
            [parent],
            prettifyMessage: false);
    }

    public static Signature Person(string name, string email) =>
        new(name, email, new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.FromHours(3)));

    public void Dispose()
    {
        repository.Dispose();
        Delete(Path);
    }

    /// <summary>Git nesne dosyalari salt okunur yaziliyor; silmeden once bayragi kaldiriyoruz.</summary>
    private static void Delete(string directory)
    {
        if (!Directory.Exists(directory))
        {
            return;
        }

        foreach (string file in Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories))
        {
            File.SetAttributes(file, FileAttributes.Normal);
        }

        Directory.Delete(directory, recursive: true);
    }
}
