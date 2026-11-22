using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;

namespace MindDuck.Persistence;

public sealed class PostgreSqlPlayerRepository : IPlayerRepository
{
    private readonly NpgsqlDataSource _dataSource;

    public PostgreSqlPlayerRepository(NpgsqlDataSource dataSource)
        => _dataSource = dataSource;

    public async Task<Player> CreatePlayerAsync(string name, CancellationToken cancellationToken = default)
    {
        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            "INSERT INTO players (name) VALUES (@name) RETURNING id, name, wins, losses",
            connection);
        command.Parameters.AddWithValue("name", name);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
        {
            throw new InvalidOperationException("Creating the player did not return a row.");
        }

        return new Player
        {
            Id = reader.GetInt32(0),
            Name = reader.GetString(1),
            Wins = reader.GetInt32(2),
            Losses = reader.GetInt32(3)
        };
    }

    public async Task<Player> GetPlayerAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            "SELECT id, name, wins, losses FROM players WHERE id = @id",
            connection);
        command.Parameters.AddWithValue("id", id);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
        {
            throw new KeyNotFoundException($"Player {id} was not found.");
        }

        return new Player
        {
            Id = reader.GetInt32(0),
            Name = reader.GetString(1),
            Wins = reader.GetInt32(2),
            Losses = reader.GetInt32(3)
        };
    }

    public async Task SavePlayerAsync(Player player, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(player);

        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            "INSERT INTO players (id, name, wins, losses) VALUES (@id, @name, @wins, @losses) " +
            "ON CONFLICT (id) DO UPDATE SET name = EXCLUDED.name, wins = EXCLUDED.wins, losses = EXCLUDED.losses",
            connection);
        command.Parameters.AddWithValue("id", player.Id);
        command.Parameters.AddWithValue("name", player.Name);
        command.Parameters.AddWithValue("wins", player.Wins);
        command.Parameters.AddWithValue("losses", player.Losses);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task UpdateStatsAsync(int playerId, bool win, CancellationToken cancellationToken = default)
    {
        var statColumn = win ? "wins" : "losses";
        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            $"UPDATE players SET {statColumn} = {statColumn} + 1 WHERE id = @id",
            connection);
        command.Parameters.AddWithValue("id", playerId);

        if (await command.ExecuteNonQueryAsync(cancellationToken) == 0)
        {
            throw new KeyNotFoundException($"Player {playerId} was not found.");
        }
    }
}