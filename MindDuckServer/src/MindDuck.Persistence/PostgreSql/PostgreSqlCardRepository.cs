using System;
using System.Collections.Generic;
using Npgsql;
using MindDuck.Contracts;

namespace MindDuck.Persistence;

public sealed class PostgreSqlCardRepository : ICardRepository
{
    private readonly NpgsqlDataSource _dataSource;

    public PostgreSqlCardRepository(NpgsqlDataSource dataSource)
        => _dataSource = dataSource;


    public Card GetCard(string id)
    {
        using var connection = _dataSource.OpenConnection();
        using var command = new NpgsqlCommand(
            "SELECT id, name, power, keyword, art_key FROM cards WHERE id = @id",
            connection);
        command.Parameters.AddWithValue("id", id);
        Card card;
        using (var reader = command.ExecuteReader()) {
            if (!reader.Read())
            {
                throw new KeyNotFoundException($"Card {id} was not found.");
            }
            card = ReadCard(reader);
        }

        card.Abilities = GetCardAbilities(card.CardId);

        return card;
    }

    public List<Card> GetAllCards()
    {
        var cards = new List<Card>();

        using var connection = _dataSource.OpenConnection();
        using var command = new NpgsqlCommand(
            "SELECT id, name, power, keyword, art_key FROM cards ORDER BY id",
            connection);

        using var reader = command.ExecuteReader();

        while (reader.Read())
        {
            cards.Add(ReadCard(reader));
        }

        foreach (var card in cards)
        {
            card.Abilities = GetCardAbilities(card.CardId);
        }

        return cards;
    }

    public List<CardAbility> GetCardAbilities(string cardId)
    {
        using var connection = _dataSource.OpenConnection();
        using var command = new NpgsqlCommand(
            "SELECT trigger, effect, value FROM card_abilities WHERE card_id = @cardId",
            connection);
        command.Parameters.AddWithValue("cardId", cardId);

        using var reader = command.ExecuteReader();
        var abilities = new List<CardAbility>();
        while (reader.Read())
        {
            abilities.Add(new CardAbility
            {
                Trigger = (AbilityTrigger)reader.GetInt32(0),
                Effect = (AbilityEffect)reader.GetInt32(1),
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
            : (Keyword?)reader.GetInt32(3),
            ArtKey = reader.IsDBNull(4) ? null : reader.GetString(4),
            Abilities = new List<CardAbility>() // Abilities will be populated separately
        };
    }
}