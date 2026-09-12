using Microsoft.EntityFrameworkCore;

using Sievert.Data.Entities;

namespace Sievert.Data;

/// <summary>
/// Veritabani baglami. Ilk dort tablo git'ten okunan ham veri ve ondan turetilen
/// olculer. Son uc tablo Asama 6 Adim 3'te eklendi: arka plan isleri ve o islerin
/// urettigi sonuclar.
/// </summary>
public sealed class SievertContext(DbContextOptions<SievertContext> options) : DbContext(options)
{
    public DbSet<RepositoryRow> Repositories => Set<RepositoryRow>();

    public DbSet<CommitRow> Commits => Set<CommitRow>();

    public DbSet<CommitFileRow> CommitFiles => Set<CommitFileRow>();

    public DbSet<CommitMetricRow> CommitMetrics => Set<CommitMetricRow>();

    public DbSet<AnalysisJobRow> AnalysisJobs => Set<AnalysisJobRow>();

    public DbSet<StaticAnalysisFindingRow> StaticAnalysisFindings => Set<StaticAnalysisFindingRow>();

    public DbSet<CommitRiskSnapshotRow> CommitRiskSnapshots => Set<CommitRiskSnapshotRow>();

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

        builder.Entity<AnalysisJobRow>(job =>
        {
            // Durum ve tur metin olarak yaziliyor. Sayi olsaydi veritabanina bakan biri
            // 3'un ne demek oldugunu koda gitmeden bilemezdi.
            job.Property(row => row.Kind).HasConversion<string>().HasMaxLength(40);
            job.Property(row => row.Status).HasConversion<string>().HasMaxLength(20);
            job.Property(row => row.CurrentPhase).HasMaxLength(60);
            job.Property(row => row.ErrorCode).HasMaxLength(60);
            job.Property(row => row.ErrorMessage).HasMaxLength(1000);
            job.Property(row => row.IdempotencyKey).HasMaxLength(128);
            job.Property(row => row.ActiveDeduplicationKey).HasMaxLength(80);
            job.Property(row => row.WorkerInstanceId).HasMaxLength(80);

            // PostgreSQL'in satir surumu; iki worker ayni isi alamasin diye.
            job.Property(row => row.Version).IsRowVersion();

            // Tekilligin gercek dayanagi burasi. PostgreSQL'de NULL'lar birbirinden
            // farkli sayildigi icin terminal isler bu kisiti hic gormuyor.
            job.HasIndex(row => row.ActiveDeduplicationKey).IsUnique();

            // Ayni anahtar iki ayri ise verilemez; verilirse istek reddediliyor.
            job.HasIndex(row => row.IdempotencyKey).IsUnique();

            job.HasIndex(row => new { row.RepositoryId, row.RequestedAtUtc });
            job.HasIndex(row => row.Status);

            // Cascade DEGIL: bir depo silindiginde is gecmisi sessizce yok olmamali.
            // Silme gercekten isteniyorsa once isler acikca ele alinmali.
            job.HasOne(row => row.Repository)
                .WithMany()
                .HasForeignKey(row => row.RepositoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<StaticAnalysisFindingRow>(finding =>
        {
            finding.Property(row => row.RuleCode).HasMaxLength(10);
            finding.Property(row => row.Severity).HasMaxLength(10);
            finding.Property(row => row.RelativePath).HasMaxLength(1000);
            finding.Property(row => row.MemberName).HasMaxLength(400);
            finding.Property(row => row.Message).HasMaxLength(2000);
            finding.Property(row => row.Rationale).HasMaxLength(2000);

            finding.HasIndex(row => new { row.AnalysisJobId, row.RuleCode });

            // Is silinirse bulgulari da gider; bulgu isten bagimsiz bir anlam tasimiyor.
            finding.HasOne(row => row.AnalysisJob)
                .WithMany()
                .HasForeignKey(row => row.AnalysisJobId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<CommitRiskSnapshotRow>(snapshot =>
        {
            snapshot.Property(row => row.ModelProfile).HasMaxLength(40);
            snapshot.Property(row => row.ModelChecksum).HasMaxLength(64);
            snapshot.Property(row => row.WarningCodes).HasMaxLength(1000);

            // Ayni is ayni commit'i iki kez yazamaz. Bir batch yeniden denenirse
            // ciftlenmeyi veritabani engelliyor, kodun dikkatli olmasi degil.
            snapshot.HasIndex(row => new { row.AnalysisJobId, row.CommitId }).IsUnique();

            // "En riskli" siralamasi bu indeksten karsilaniyor.
            snapshot.HasIndex(row => new { row.AnalysisJobId, row.RawModelScore });

            snapshot.HasOne(row => row.AnalysisJob)
                .WithMany()
                .HasForeignKey(row => row.AnalysisJobId)
                .OnDelete(DeleteBehavior.Cascade);

            snapshot.HasOne(row => row.Commit)
                .WithMany()
                .HasForeignKey(row => row.CommitId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
