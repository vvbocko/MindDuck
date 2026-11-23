using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;

namespace MindDuck.Persistence;

public sealed class PostgreSqlMatchRepository : IMatchRepository
{
    private readonly NpgsqlDataSource _dataSource;

    public PostgreSqlMatchRepository(NpgsqlDataSource dataSource)
        => _dataSource = dataSource;


    public async Task CreateMatchAsync(
        string matchId,
        int player1Id,
        int player2Id,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            "INSERT INTO matches (match_id, player1_id, player2_id) " +
            "VALUES (@match_id, @player1_id, @player2_id)",
            connection);
        command.Parameters.AddWithValue("match_id", matchId);
        command.Parameters.AddWithValue("player1_id", player1Id);
        command.Parameters.AddWithValue("player2_id", player2Id);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task AppendEventsAsync(
        string matchId,
        int actorPlayerId,
        List<string> events,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(events);

        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            "INSERT INTO match_events (match_id, actor_player_id, event) " +
            "VALUES (@match_id, @actor_player_id, @event)",
            connection,
            transaction);
        var matchIdParameter = command.Parameters.Add("match_id", NpgsqlTypes.NpgsqlDbType.Text);
        var actorPlayerIdParameter = command.Parameters.Add("actor_player_id", NpgsqlTypes.NpgsqlDbType.Integer);
        var eventParameter = command.Parameters.Add("event", NpgsqlTypes.NpgsqlDbType.Text);
        matchIdParameter.Value = matchId;
        actorPlayerIdParameter.Value = actorPlayerId;
        foreach (var matchEvent in events)
        {
            eventParameter.Value = matchEvent;
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    public async Task FinishMatchAsync(
        string matchId,
        string winner,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            "UPDATE matches SET winner = @winner, finished_at = CURRENT_TIMESTAMP WHERE match_id = @match_id",
            connection);
        command.Parameters.AddWithValue("match_id", matchId);
        command.Parameters.AddWithValue("winner", winner);

        if (await command.ExecuteNonQueryAsync(cancellationToken) == 0)
        {
            throw new KeyNotFoundException($"Match '{matchId}' was not found.");
        }
    }

    public async Task<List<MatchResult>> GetRecentAsync(int limit, CancellationToken cancellationToken = default)
    {
        if (limit < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(limit));
        }

        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            "SELECT match_id, player1_id, player2_id, winner " +
            "FROM matches WHERE finished_at IS NOT NULL " +
            "ORDER BY finished_at DESC, match_id LIMIT @limit",
            connection);
        command.Parameters.AddWithValue("limit", limit);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var matches = new List<MatchResult>();
        while (await reader.ReadAsync(cancellationToken))
        {
            matches.Add(new MatchResult
            {
                MatchId = reader.GetString(0),
                Player1Id = reader.IsDBNull(1) ? null : reader.GetInt32(1),
                Player2Id = reader.IsDBNull(2) ? null : reader.GetInt32(2),
                Winner = reader.IsDBNull(3) ? null : reader.GetString(3)
            });
        }

        return matches;
    }
}