using LibGit2Sharp;

namespace Sievert.Mining;

/// <summary>
/// Bir deponun o andaki durumu: hangi commit'te duruyor ve calisma agacinda
/// kaydedilmemis degisiklik var mi.
///
/// Neden gerekli: statik tarama diskteki dosyalari okuyor, veritabanindaki commit
/// tarihini degil. Hangi agacin tarandigi yazilmazsa sonuc tekrar uretilemez; "203 bulgu"
/// cumlesi hangi kodu anlattigini soylemez.
/// </summary>
/// <param name="State">
/// <see cref="Clean"/>, <see cref="Dirty"/> ya da <see cref="Unavailable"/>.
/// <see cref="ChangedDuringAnalysis"/> buradan gelmez; onu iki okumayi karsilastiran
/// taraf yazar.
/// </param>
/// <param name="HeadSha">HEAD commit'inin tam ozeti; okunamadiysa null.</param>
/// <param name="DirtyFileCount">Kaydedilmemis degisiklik sayisi; okunamadiysa null.</param>
/// <param name="Identity">Deponun uzak adresi; yoksa null. Dosya yolu DEGIL.</param>
/// <param name="IsGitRepository">
/// Yol bir git deposu mu. <see cref="WorktreeState.Unavailable"/> iki ayri sebepten
/// gelebiliyor - git deposu degil, ya da git deposu ama hic commit'i yok - ve cagiran
/// taraf bu ikisine farkli hata kodu veriyor.
/// </param>
public sealed record WorktreeSnapshot(
    string State,
    string? HeadSha,
    int? DirtyFileCount,
    string? Identity,
    bool IsGitRepository)
{
    /// <summary>HEAD'in ilk 12 karakteri; gosterimde tam ozet yerine bu kullaniliyor.</summary>
    public string? ShortSha => HeadSha is { Length: >= 12 } sha ? sha[..12] : HeadSha;

    public bool IsClean => State == WorktreeState.Clean;
}

/// <summary>
/// Calisma agacinin durumunu okur.
///
/// Bu sinif git **tarihini** okumuyor: tek commit'in ozeti ve <c>git status</c>'un
/// karsiligi. Klonlama, tarih madenciligi ve SZZ hala <see cref="RepositoryMiner"/>
/// tarafinda ve API onlari cagirmiyor.
/// </summary>
public static class WorktreeState
{
    public const string Clean = "clean";

    public const string Dirty = "dirty";

    /// <summary>Tarama sirasinda HEAD ya da calisma agaci degisti.</summary>
    public const string ChangedDuringAnalysis = "changed-during-analysis";

    /// <summary>Klasor yok, git deposu degil ya da HEAD okunamadi.</summary>
    public const string Unavailable = "unavailable";

    /// <summary>
    /// Depoyu okur ve durumunu dondurur. Istisna firlatmiyor: cagiran taraf bunu bir
    /// HTTP cevabina cevirecek ve LibGit2Sharp'in istisna metni dosya yolu tasiyor.
    /// </summary>
    public static WorktreeSnapshot Read(string path)
    {
        try
        {
            if (!Repository.IsValid(path))
            {
                return new WorktreeSnapshot(Unavailable, null, null, null, IsGitRepository: false);
            }

            using Repository repository = new(path);

            if (repository.Head.Tip?.Sha is not string head)
            {
                // Commit'i olmayan yeni depo. Taranacak bir surum yok.
                return new WorktreeSnapshot(Unavailable, null, null, Identity(repository), IsGitRepository: true);
            }

            // git status'un varsayilani ile ayni kume: takip edilen degisiklikler ve
            // yok sayilmayan yeni dosyalar. Yok sayilanlar (bin, obj) sayilmiyor; onlar
            // zaten taramaya da girmiyor.
            RepositoryStatus status = repository.RetrieveStatus(new StatusOptions
            {
                IncludeIgnored = false,
                IncludeUntracked = true,
                RecurseUntrackedDirs = true,
            });

            int dirty = status.Count(entry => entry.State != FileStatus.Ignored);

            return new WorktreeSnapshot(
                dirty == 0 ? Clean : Dirty,
                head,
                dirty,
                Identity(repository),
                IsGitRepository: true);
        }
        catch (LibGit2SharpException)
        {
            return new WorktreeSnapshot(Unavailable, null, null, null, IsGitRepository: false);
        }
    }

    private static string? Identity(Repository repository) => repository.Network.Remotes["origin"]?.Url;
}
