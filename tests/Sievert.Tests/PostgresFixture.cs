using Microsoft.EntityFrameworkCore;

using Microsoft.Extensions.Logging.Abstractions;

using Npgsql;

using Sievert.Data;

using Testcontainers.PostgreSql;

namespace Sievert.Tests;

/// <summary>
/// Testler icin gercek bir PostgreSQL. Tek konteyner aciliyor, her test kendi
/// veritabanini yaratiyor; boylece testler paylasilan durum birakmadan paralel kosabiliyor.
/// Docker yoksa konteyner hic baslatilmiyor ve testler <see cref="DockerFactAttribute"/>
/// ile atlaniyor.
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private PostgreSqlContainer? container;

    public string? AdminConnectionString { get; private set; }

    public async Task InitializeAsync()
    {
        if (!Docker.Available)
        {
            return;
        }

        // Gelistirme konteyneriyle ayni surum: postgres:17.
        //
        // Gunluk susturuluyor. Testcontainers varsayilan olarak Console'a yaziyor ve
        // Console bu sureçte paylasilan bir kaynak: ConsoleCollection'daki testler
        // Console.Out'u kendilerine cevirip ciktiyi JSON olarak ayristiriyor. Iki
        // koleksiyon paralel kostugu icin konteyner gunlugunun bir satiri o tamponun
        // icine dusebiliyor ve JSON ayristirmasi kiriliyor. CI'da tam olarak bu oldu.
        container = new PostgreSqlBuilder("postgres:17")
            .WithLogger(NullLogger.Instance)
            .Build();
        await container.StartAsync();
        AdminConnectionString = container.GetConnectionString();
    }

    /// <summary>Bos bir veritabani acip semayi migration ile kurar ve baglamini verir.</summary>
    public SievertContext NewDatabase() => SievertContextBuilder.Create(NewDatabaseConnectionString());

    /// <summary>
    /// Ayni isi yapar ama baglam yerine baglanti dizesini verir. API testleri baglami
    /// kendi servis kabinden aldigi icin dizeye ihtiyac duyuyor.
    /// </summary>
    public string NewDatabaseConnectionString()
    {
        string name = "sievert_test_" + Guid.NewGuid().ToString("n");

        using (NpgsqlConnection admin = new(AdminConnectionString))
        {
            admin.Open();
            using NpgsqlCommand create = admin.CreateCommand();
            create.CommandText = $"CREATE DATABASE \"{name}\"";
            create.ExecuteNonQuery();
        }

        // Havuz kucuk tutuluyor. Her test kendi veritabanini aciyor ve her baglanti dizesi
        // kendi havuzunu kuruyor; varsayilan boyutla konteynerin max_connections siniri
        // doluyor ve testler "too many clients already" ile dusuyor. Bir kez oldu.
        NpgsqlConnectionStringBuilder builder = new(AdminConnectionString)
        {
            Database = name,
            MaxPoolSize = 4,
            ConnectionIdleLifetime = 5,
            ConnectionPruningInterval = 1,
        };

        using SievertContext context = SievertContextBuilder.Create(builder.ConnectionString);

        // EnsureCreated degil Migrate: repoya eklenen migration'in gercek bir PostgreSQL'de
        // uygulanabildigi de boylece test edilmis oluyor.
        context.Database.Migrate();

        return builder.ConnectionString;
    }

    public async Task DisposeAsync()
    {
        if (container is not null)
        {
            await container.DisposeAsync();
        }
    }
}

/// <summary>Konteyner tek sefer aciliyor; butun veritabani testleri ayni kovada.</summary>
[CollectionDefinition(Name)]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>
{
    public const string Name = "postgres";
}
