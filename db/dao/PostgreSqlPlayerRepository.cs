using System;
using System.Collections.Generic;
using Npgsql;


public sealed class PostgreSqlPlayerRepository : IPlayerRepository
{
    private readonly NpgsqlDataSource _dataSource;

    public PostgreSqlPlayerRepository(NpgsqlDataSource dataSource)
        => _dataSource = dataSource;

    public Player GetPlayer(int id)
    {
        using var connection = _dataSource.OpenConnection();
        using var command = new NpgsqlCommand(
            "SELECT id, name, wins, losses FROM players WHERE id = @id",
            connection);
        command.Parameters.AddWithValue("id", id);
        using var reader = command.ExecuteReader();

        if (!reader.Read())
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

    public void SavePlayer(Player player)
    {
        ArgumentNullException.ThrowIfNull(player);

        using var connection = _dataSource.OpenConnection();
        using var command = new NpgsqlCommand(
            "INSERT INTO players (id, name, wins, losses) VALUES (@id, @name, @wins, @losses) " +
            "ON CONFLICT (id) DO UPDATE SET name = EXCLUDED.name, wins = EXCLUDED.wins, losses = EXCLUDED.losses",
            connection);
        command.Parameters.AddWithValue("id", player.Id);
        command.Parameters.AddWithValue("name", player.Name);
        command.Parameters.AddWithValue("wins", player.Wins);
        command.Parameters.AddWithValue("losses", player.Losses);
        command.ExecuteNonQuery();
    }

    public void UpdateStats(int playerId, bool win)
    {
        var statColumn = win ? "wins" : "losses";
        using var connection = _dataSource.OpenConnection();
        using var command = new NpgsqlCommand(
            $"UPDATE players SET {statColumn} = {statColumn} + 1 WHERE id = @id",
            connection);
        command.Parameters.AddWithValue("id", playerId);

        if (command.ExecuteNonQuery() == 0)
        {
            throw new KeyNotFoundException($"Player {playerId} was not found.");
        }
    }
}