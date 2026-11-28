using MindDuck.Contracts;
using MindDuck.Persistence;
using Npgsql;

namespace MindDuck.Persistence.Tests;

[Collection("PostgreSQL")]
public sealed class PostgreSqlRepositoriesTests : IAsyncLifetime
{
    private readonly PostgreSqlFixture _fixture;

    public PostgreSqlRepositoriesTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync() => _fixture.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetCardAsync_ReturnsCardAndItsAbilities()
    {
        await InsertCardAsync("test-card", "Test Card", 7, "Frenzy", "test-art");
        await InsertAbilityAsync("test-card", "Play", "GainLife", 2);

        var repository = new PostgreSqlCardRepository(_fixture.DataSource);

        Card card = await repository.GetCardAsync("test-card");

        Assert.Equal("test-card", card.CardId);
        Assert.Equal("Test Card", card.Name);
        Assert.Equal(7, card.Power);
        Assert.Equal(Keyword.Frenzy, card.Keyword);
        Assert.Equal("test-art", card.ArtKey);

        CardAbility ability = Assert.Single(card.Abilities);
        Assert.Equal(AbilityTrigger.Play, ability.Trigger);
        Assert.Equal(AbilityEffect.GainLife, ability.Effect);
        Assert.Equal(2, ability.Value);
    }

    [Fact]
    public async Task GetCardAsync_WhenCardDoesNotExist_ThrowsKeyNotFoundException()
    {
        var repository = new PostgreSqlCardRepository(_fixture.DataSource);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => repository.GetCardAsync("missing-card"));
    }

    [Fact]
    public async Task GetAllCardsAsync_ReturnsCardsOrderedById()
    {
        await InsertCardAsync("z-card", "Z Card", 3, null, null);
        await InsertCardAsync("a-card", "A Card", 5, null, null);
        await InsertAbilityAsync("a-card", "Attack", "OpponentDiscards", 1);

        var repository = new PostgreSqlCardRepository(_fixture.DataSource);

        List<Card> cards = await repository.GetAllCardsAsync();

        Assert.Collection(
            cards,
            card =>
            {
                Assert.Equal("a-card", card.CardId);
                Assert.Single(card.Abilities);
            },
            card =>
            {
                Assert.Equal("z-card", card.CardId);
                Assert.Empty(card.Abilities);
            });
    }

    [Fact]
    public async Task PlayerRepository_CreatesReadsSavesAndUpdatesStats()
    {
        var repository = new PostgreSqlPlayerRepository(_fixture.DataSource);

        Player created = await repository.CreatePlayerAsync("Duck");

        Assert.True(created.Id > 0);
        Assert.Equal("Duck", created.Name);
        Assert.Equal(0, created.Wins);
        Assert.Equal(0, created.Losses);

        created.Name = "Updated Duck";
        created.Wins = 3;
        await repository.SavePlayerAsync(created);
        await repository.UpdateStatsAsync(created.Id, win: true);
        await repository.UpdateStatsAsync(created.Id, win: false);

        Player loaded = await repository.GetPlayerAsync(created.Id);

        Assert.Equal("Updated Duck", loaded.Name);
        Assert.Equal(4, loaded.Wins);
        Assert.Equal(1, loaded.Losses);
    }

    [Fact]
    public async Task GetPlayerAsync_WhenPlayerDoesNotExist_ThrowsKeyNotFoundException()
    {
        var repository = new PostgreSqlPlayerRepository(_fixture.DataSource);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => repository.GetPlayerAsync(-1));
    }

    [Fact]
    public async Task UpdateStatsAsync_WhenPlayerDoesNotExist_ThrowsKeyNotFoundException()
    {
        var repository = new PostgreSqlPlayerRepository(_fixture.DataSource);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => repository.UpdateStatsAsync(-1, win: true));
    }

    [Fact]
    public async Task MatchRepository_CreatesAppendsFinishesAndListsMatches()
    {
        var players = new PostgreSqlPlayerRepository(_fixture.DataSource);
        Player playerOne = await players.CreatePlayerAsync("Player One");
        Player playerTwo = await players.CreatePlayerAsync("Player Two");
        var repository = new PostgreSqlMatchRepository(_fixture.DataSource);

        await repository.CreateMatchAsync("finished-match", playerOne.Id, playerTwo.Id);
        await repository.CreateMatchAsync("unfinished-match", playerOne.Id, playerTwo.Id);
        await repository.AppendEventsAsync(
            "finished-match",
            playerOne.Id,
            new List<string> { """{"type":"PlayCard"}""", """{"type":"Attack"}""" });
        await repository.FinishMatchAsync("finished-match", "Player One");

        List<MatchResult> recent = await repository.GetRecentAsync(10);

        MatchResult result = Assert.Single(recent);
        Assert.Equal("finished-match", result.MatchId);
        Assert.Equal(playerOne.Id, result.Player1Id);
        Assert.Equal(playerTwo.Id, result.Player2Id);
        Assert.Equal("Player One", result.Winner);

        List<string> events = await GetEventsAsync("finished-match");
        Assert.Equal(new[] { """{"type":"PlayCard"}""", """{"type":"Attack"}""" }, events);
    }

    [Fact]
    public async Task MatchRepository_RejectsEventsWithUnknownActor()
    {
        var players = new PostgreSqlPlayerRepository(_fixture.DataSource);
        Player playerOne = await players.CreatePlayerAsync("Player One");
        Player playerTwo = await players.CreatePlayerAsync("Player Two");
        var repository = new PostgreSqlMatchRepository(_fixture.DataSource);
        await repository.CreateMatchAsync("test-match", playerOne.Id, playerTwo.Id);

        await Assert.ThrowsAsync<PostgresException>(
            () => repository.AppendEventsAsync(
                "test-match",
                actorPlayerId: -1,
                events: new List<string> { """{"type":"PlayCard"}""" }));

        Assert.Empty(await GetEventsAsync("test-match"));
    }

    [Fact]
    public async Task FinishMatchAsync_WhenMatchDoesNotExist_ThrowsKeyNotFoundException()
    {
        var repository = new PostgreSqlMatchRepository(_fixture.DataSource);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => repository.FinishMatchAsync("missing-match", "Nobody"));
    }

    private async Task InsertCardAsync(string id, string name, int power, string? keyword, string? artKey)
    {
        await using var connection = await _fixture.DataSource.OpenConnectionAsync();
        await using var command = new NpgsqlCommand(
            "INSERT INTO cards (id, name, power, keyword, art_key) " +
            "VALUES (@id, @name, @power, @keyword, @art_key)",
            connection);
        command.Parameters.AddWithValue("id", id);
        command.Parameters.AddWithValue("name", name);
        command.Parameters.AddWithValue("power", power);
        command.Parameters.AddWithValue("keyword", (object?)keyword ?? DBNull.Value);
        command.Parameters.AddWithValue("art_key", (object?)artKey ?? DBNull.Value);
        await command.ExecuteNonQueryAsync();
    }

    private async Task InsertAbilityAsync(string cardId, string trigger, string effect, int? value)
    {
        await using var connection = await _fixture.DataSource.OpenConnectionAsync();
        await using var command = new NpgsqlCommand(
            "INSERT INTO card_abilities (card_id, trigger, effect, value) " +
            "VALUES (@card_id, @trigger, @effect, @value)",
            connection);
        command.Parameters.AddWithValue("card_id", cardId);
        command.Parameters.AddWithValue("trigger", trigger);
        command.Parameters.AddWithValue("effect", effect);
        command.Parameters.AddWithValue("value", (object?)value ?? DBNull.Value);
        await command.ExecuteNonQueryAsync();
    }

    private async Task<List<string>> GetEventsAsync(string matchId)
    {
        await using var connection = await _fixture.DataSource.OpenConnectionAsync();
        await using var command = new NpgsqlCommand(
            "SELECT event FROM match_events WHERE match_id = @match_id ORDER BY event_id",
            connection);
        command.Parameters.AddWithValue("match_id", matchId);

        await using var reader = await command.ExecuteReaderAsync();
        var events = new List<string>();
        while (await reader.ReadAsync())
        {
            events.Add(reader.GetString(0));
        }

        return events;
    }
}
