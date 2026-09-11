using Microsoft.EntityFrameworkCore;

using Sievert.Data.Entities;

namespace Sievert.Data;

/// <summary>
/// Veritabani baglami. Dort tablo: uc tanesi git'ten okunan ham veri, dorduncusu
/// (CommitMetrics) Adim 3'te doldurulacak turetilmis olculer icin bos duruyor.
/// </summary>
public sealed class SievertContext(DbContextOptions<SievertContext> options) : DbContext(options)
{
    public DbSet<RepositoryRow> Repositories => Set<RepositoryRow>();

    public DbSet<CommitRow> Commits => Set<CommitRow>();

    public DbSet<CommitFileRow> CommitFiles => Set<CommitFileRow>();

    public DbSet<CommitMetricRow> CommitMetrics => Set<CommitMetricRow>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<RepositoryRow>(repository =>
        {
            // Eslestirme kimlik uzerinden, o yuzden benzersiz.
            repository.HasIndex(row => row.Identity).IsUnique();
            repository.Property(row => row.Identity).HasMaxLength(1000);
            repository.Property(row => row.IdentitySource).HasMaxLength(20);
            repository.Property(row => row.Name).HasMaxLength(400);
            repository.Property(row => row.RemoteUrl).HasMaxLength(1000);
            repository.Property(row => row.ScannedSha).HasMaxLength(40);
            repository.Property(row => row.LocalPath).HasMaxLength(1000);
        });

        builder.Entity<CommitRow>(commit =>
        {
            // Ayni depoda ayni sha iki kez olamaz. Idempotent yazmanin dayanagi bu:
            // "zaten var mi" sorusunun cevabi veritabaninin kendi garantisi, kodun
            // dikkatli olmasi degil. Ayrica Adim 5'te "su sha'nin verisi" sorgusu
            // dogrudan bu indeksten karsilanacak.
            commit.HasIndex(row => new { row.RepositoryId, row.Sha }).IsUnique();

            // Adim 5 zaman pencereleriyle calisacak: "son 90 gunde bu dosyaya kac commit
            // dokundu" gibi sorularin hepsi tarihe gore araliktan geciyor.
            commit.HasIndex(row => row.AuthorDateUtc);

            commit.Property(row => row.Sha).HasMaxLength(40);
            commit.Property(row => row.AuthorName).HasMaxLength(400);
            commit.Property(row => row.AuthorEmail).HasMaxLength(400);
            commit.Property(row => row.MessageSubject).HasMaxLength(1000);
            commit.Property(row => row.LabelSource).HasMaxLength(20);

            // Asama 5 egitim kumesini bu sutundan suzecek.
            commit.HasIndex(row => row.IsBugIntroducing);

            commit.HasOne(row => row.Repository)
                .WithMany(repository => repository.Commits)
                .HasForeignKey(row => row.RepositoryId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<CommitFileRow>(file =>
        {
            // Dosya gecmisi Adim 5'in ana sorgusu: "bu dosya gecmiste kac kez degisti".
            // Yol uzerinden indekssiz gitmek butun tabloyu taramak demek.
            file.HasIndex(row => row.Path);

            file.Property(row => row.Path).HasMaxLength(1000);
            file.Property(row => row.OldPath).HasMaxLength(1000);
            file.Property(row => row.ChangeKind).HasMaxLength(20);

            file.HasOne(row => row.Commit)
                .WithMany(commit => commit.Files)
                .HasForeignKey(row => row.CommitId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<CommitMetricRow>(metric =>
        {
            metric.HasIndex(row => row.CommitId).IsUnique();

            metric.HasOne(row => row.Commit)
                .WithMany()
                .HasForeignKey(row => row.CommitId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
