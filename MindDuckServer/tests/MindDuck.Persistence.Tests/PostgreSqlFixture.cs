using Npgsql;
using Testcontainers.PostgreSql;

namespace MindDuck.Persistence.Tests;

public sealed class PostgreSqlFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17")
        .WithDatabase("mindduck_tests")
        .WithUsername("mindduck")
        .WithPassword("mindduck_test_password")
        .Build();

    public NpgsqlDataSource DataSource { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        DataSource = NpgsqlDataSource.Create(_container.GetConnectionString());

        string schemaPath = Path.Combine(AppContext.BaseDirectory, "schema.sql");
        string schema = await File.ReadAllTextAsync(schemaPath);

        await using var connection = await DataSource.OpenConnectionAsync();
        await using var command = new NpgsqlCommand(schema, connection);
        await command.ExecuteNonQueryAsync();
    }

    public async Task ResetAsync()
    {
        await using var connection = await DataSource.OpenConnectionAsync();
        await using var command = new NpgsqlCommand(
            "TRUNCATE TABLE match_events, matches, players, card_abilities, cards RESTART IDENTITY CASCADE",
            connection);
        await command.ExecuteNonQueryAsync();
    }

    public async Task DisposeAsync()
    {
        if (DataSource is not null)
        {
            await DataSource.DisposeAsync();
        }

        await _container.DisposeAsync();
    }
}

[CollectionDefinition("PostgreSQL")]
public sealed class PostgreSqlCollection : ICollectionFixture<PostgreSqlFixture>
{
}
