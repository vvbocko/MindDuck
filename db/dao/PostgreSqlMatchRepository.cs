using System;
using System.Collections.Generic;
using Npgsql;


public sealed class PostgreSqlMatchRepository : IMatchRepository
{
    private readonly NpgsqlDataSource _dataSource;

    public PostgreSqlMatchRepository(NpgsqlDataSource dataSource)
        => _dataSource = dataSource;


    public void CreateMatch(string matchId)
    {
        using var connection = _dataSource.OpenConnection();
        using var command = new NpgsqlCommand(
            "INSERT INTO matches (match_id) VALUES (@match_id)",
            connection);
        command.Parameters.AddWithValue("match_id", matchId);
        command.ExecuteNonQuery();
    }

    public void AppendEvents(string matchId, List<string> events)
    {
        ArgumentNullException.ThrowIfNull(events);

        using var connection = _dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction();
        using var command = new NpgsqlCommand(
            "INSERT INTO match_events (match_id, event) VALUES (@match_id, @event)",
            connection,
            transaction);
        var matchIdParameter = command.Parameters.Add("match_id", NpgsqlTypes.NpgsqlDbType.Text);
        var eventParameter = command.Parameters.Add("event", NpgsqlTypes.NpgsqlDbType.Text);
        matchIdParameter.Value = matchId;

        foreach (var matchEvent in events)
        {
            eventParameter.Value = matchEvent;
            command.ExecuteNonQuery();
        }

        transaction.Commit();
    }

    public void FinishMatch(string matchId, string winner)
    {
        using var connection = _dataSource.OpenConnection();
        using var command = new NpgsqlCommand(
            "UPDATE matches SET winner = @winner, finished_at = CURRENT_TIMESTAMP WHERE match_id = @match_id",
            connection);
        command.Parameters.AddWithValue("match_id", matchId);
        command.Parameters.AddWithValue("winner", winner);

        if (command.ExecuteNonQuery() == 0)
        {
            throw new KeyNotFoundException($"Match '{matchId}' was not found.");
        }
    }

    public List<MatchResult> GetRecent(int limit)
    {
        if (limit < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(limit));
        }

        using var connection = _dataSource.OpenConnection();
        using var command = new NpgsqlCommand(
            "SELECT match_id, winner FROM matches WHERE finished_at IS NOT NULL ORDER BY finished_at DESC, match_id LIMIT @limit",
            connection);
        command.Parameters.AddWithValue("limit", limit);
        using var reader = command.ExecuteReader();

        var matches = new List<MatchResult>();
        while (reader.Read())
        {
            matches.Add(new MatchResult
            {
                MatchId = reader.GetString(0),
                Winner = reader.GetString(1)
            });
        }

        return matches;
    }
}