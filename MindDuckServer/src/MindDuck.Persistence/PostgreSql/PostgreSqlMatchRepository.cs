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


    public async Task CreateMatchAsync(string matchId, CancellationToken cancellationToken = default)
    {
        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            "INSERT INTO matches (match_id) VALUES (@match_id)",
            connection);
        command.Parameters.AddWithValue("match_id", matchId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task AppendEventsAsync(
        string matchId,
        List<string> events,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(events);

        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            "INSERT INTO match_events (match_id, event) VALUES (@match_id, @event)",
            connection,
            transaction);
        var matchIdParameter = command.Parameters.Add("match_id", NpgsqlTypes.NpgsqlDbType.Text);
        var eventParameter = command.Parameters.Add("event", NpgsqlTypes.NpgsqlDbType.Text);
        matchIdParameter.Value = matchId;

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
            "SELECT match_id, winner FROM matches WHERE finished_at IS NOT NULL ORDER BY finished_at DESC, match_id LIMIT @limit",
            connection);
        command.Parameters.AddWithValue("limit", limit);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var matches = new List<MatchResult>();
        while (await reader.ReadAsync(cancellationToken))
        {
            matches.Add(new MatchResult
            {
                MatchId = reader.GetString(0),
                Winner = reader.IsDBNull(1) ? null : reader.GetString(1)
            });
        }

        return matches;
    }
}