namespace Sievert.Data.Entities;

/// <summary>
/// Taranmis bir depo. Ayni depo ikinci kez islendiginde yeni satir acilmiyor, bu satir
/// guncelleniyor; hangi commit'in hangi tarama kosusundan geldigi <see cref="ScannedAt"/>
/// ve <see cref="ScannedSha"/> ile izleniyor.
/// </summary>
public sealed class RepositoryRow
{
    public int Id { get; set; }

    /// <summary>
    /// Deponun karsilastirilabilir kimligi. Uzak adres varsa ondan normalize edilerek
    /// uretiliyor (`github.com/app-vnext/polly`), yoksa klasor adina dusuluyor. Ayni
    /// depo iki farkli klasor adiyla taranirsa tek satir kalsin diye eslestirme buradan.
    /// </summary>
    public required string Identity { get; set; }

    /// <summary>
    /// Kimligin nereden geldigi: <c>remote</c> ya da <c>folder</c>. Klasor adina
    /// dusuldugunde ciftlenme hala mumkun; bu alan o durumun gorunur olmasi icin var.
    /// </summary>
    public required string IdentitySource { get; set; }

    /// <summary>Deponun adi, yani klasor adi. Gosterim icin; eslestirmede kullanilmiyor.</summary>
    public required string Name { get; set; }

    /// <summary>origin uzak adresi. Yerel bir depoda uzak yoksa null.</summary>
    public string? RemoteUrl { get; set; }

    /// <summary>Bu depodan yazilan commit sayisi. Birlestirme commit'leri buna dahil degil.</summary>
    public int TotalCommits { get; set; }

    public DateTimeOffset? FirstCommitDate { get; set; }

    public DateTimeOffset? LastCommitDate { get; set; }

    /// <summary>Son tarama kosusunun zamani.</summary>
    public DateTimeOffset ScannedAt { get; set; }

    /// <summary>Tarama sirasinda HEAD'in gosterdigi commit. Hangi surumun okundugu belli olsun diye.</summary>
    public string? ScannedSha { get; set; }

    /// <summary>
    /// Deponun bu makinedeki klasoru. Etiketleme git blame calistirdigi icin yerel bir
    /// klona ihtiyac duyuyor ve komut sadece depo adi aliyor. Makineye ozel bir deger
    /// oldugu icin baska bir makinede gecersiz olabilir; sinirliliklarda yaziyor.
    /// </summary>
    public string? LocalPath { get; set; }

    /// <summary>
    /// Bu depo sabit bir demo veri kumesinden mi geldi.
    ///
    /// Yalniz **kaynak sunumu** icin: model skorunu, siralamayi ya da hicbir API
    /// sonucunu degistirmiyor. Amaci tek cumle - demo ortamini acan kisi ekranda
    /// gordugu seyin canli bir depo analizi olmadigini gorsun.
    /// </summary>
    public bool IsDemoData { get; set; }

    public List<CommitRow> Commits { get; } = [];
}
