using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;
using MindDuck.Contracts;

namespace MindDuck.Persistence;

public sealed class PostgreSqlCardRepository : ICardRepository
{
    private readonly NpgsqlDataSource _dataSource;

    public PostgreSqlCardRepository(NpgsqlDataSource dataSource)
        => _dataSource = dataSource;


    public async Task<Card> GetCardAsync(string id, CancellationToken cancellationToken = default)
    {
        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            "SELECT id, name, power, keyword, art_key FROM cards WHERE id = @id",
            connection);
        command.Parameters.AddWithValue("id", id);
        Card card;
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            if (!await reader.ReadAsync(cancellationToken))
            {
                throw new KeyNotFoundException($"Card {id} was not found.");
            }
            card = ReadCard(reader);
        }

        card.Abilities = await GetCardAbilitiesAsync(card.CardId, cancellationToken);

        return card;
    }

    public async Task<List<Card>> GetAllCardsAsync(CancellationToken cancellationToken = default)
    {
        var cards = new List<Card>();

        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            "SELECT id, name, power, keyword, art_key FROM cards ORDER BY id",
            connection);

        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                cards.Add(ReadCard(reader));
            }
        }

        foreach (var card in cards)
        {
            card.Abilities = await GetCardAbilitiesAsync(card.CardId, cancellationToken);
        }

        return cards;
    }

    public async Task<List<CardAbility>> GetCardAbilitiesAsync(
        string cardId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            "SELECT trigger, effect, value FROM card_abilities WHERE card_id = @cardId",
            connection);
        command.Parameters.AddWithValue("cardId", cardId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var abilities = new List<CardAbility>();
        while (await reader.ReadAsync(cancellationToken))
        {
            abilities.Add(new CardAbility
            {
                Trigger = Enum.Parse<AbilityTrigger>(reader.GetString(0)),
                Effect = Enum.Parse<AbilityEffect>(reader.GetString(1)),
                Value = reader.IsDBNull(2)
                    ? null
                    : reader.GetInt32(2)
            });
        }

        return abilities;
    }

    private static Card ReadCard(NpgsqlDataReader reader)
    {
        return new Card
        {
            CardId = reader.GetString(0),
            Name = reader.GetString(1),
            Power = reader.GetInt32(2),
            Keyword = reader.IsDBNull(3)
            ? null
            : Enum.Parse<Keyword>(reader.GetString(3)),
            ArtKey = reader.IsDBNull(4) ? null : reader.GetString(4),
            Abilities = new List<CardAbility>() // Abilities will be populated separately
        };
    }
}