using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Sievert.Data;

/// <summary>
/// Baglam uretimi tek yerde. <c>dotnet ef</c> de burayi kullaniyor; migration uretirken
/// calisan bir veritabani gerekmiyor, sadece saglayici bilgisi gerekiyor.
/// </summary>
public static class SievertContextBuilder
{
    public static SievertContext Create(string connectionString) =>
        new(new DbContextOptionsBuilder<SievertContext>()
            .UseNpgsql(connectionString)
            .Options);
}

/// <summary>
/// <c>dotnet ef migrations add</c> komutunun baglami nasil uretecegi. Baglanti dizesi
/// yine koda yazilmiyor: ortam degiskeni yoksa migration uretmeye yetecek sahte bir
/// dize kullaniliyor, bu dizeyle hicbir veritabanina baglanilmiyor.
/// </summary>
public sealed class DesignTimeContextFactory : IDesignTimeDbContextFactory<SievertContext>
{
    public SievertContext CreateDbContext(string[] args) =>
        SievertContextBuilder.Create(
            ConnectionString.Find(Directory.GetCurrentDirectory()).Value
            ?? "Host=localhost;Database=sievert;Username=postgres");
}
