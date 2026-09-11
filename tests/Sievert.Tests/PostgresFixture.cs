using Microsoft.EntityFrameworkCore;

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
        container = new PostgreSqlBuilder("postgres:17").Build();
        await container.StartAsync();
        AdminConnectionString = container.GetConnectionString();
    }

    /// <summary>Bos bir veritabani acip semayi migration ile kurar ve baglamini verir.</summary>
    public SievertContext NewDatabase()
    {
        string name = "sievert_test_" + Guid.NewGuid().ToString("n");

        using (NpgsqlConnection admin = new(AdminConnectionString))
        {
            admin.Open();
            using NpgsqlCommand create = admin.CreateCommand();
            create.CommandText = $"CREATE DATABASE \"{name}\"";
            create.ExecuteNonQuery();
        }

        NpgsqlConnectionStringBuilder builder = new(AdminConnectionString) { Database = name };
        SievertContext context = SievertContextBuilder.Create(builder.ConnectionString);

        // EnsureCreated degil Migrate: repoya eklenen migration'in gercek bir PostgreSQL'de
        // uygulanabildigi de boylece test edilmis oluyor.
        context.Database.Migrate();

        return context;
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
