using System;
using System.Collections.Generic;
using Npgsql;



public sealed class PostgreSqlCardRepository : ICardRepository
{
    private readonly NpgsqlDataSource _dataSource;

    public PostgreSqlCardRepository(NpgsqlDataSource dataSource)
        => _dataSource = dataSource;


    public Card GetCard(int id)
    {
        using var connection = _dataSource.OpenConnection();
        using var command = new NpgsqlCommand(
            "SELECT card_id, name, power FROM cards WHERE card_id = @id",
            connection);
        command.Parameters.AddWithValue("id", id);

        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            throw new KeyNotFoundException($"Card {id} was not found.");
        }

        return ReadCard(reader);
    }

    public List<Card> GetAllCards()
    {
        using var connection = _dataSource.OpenConnection();
        using var command = new NpgsqlCommand(
            "SELECT card_id, name, power FROM cards ORDER BY card_id",
            connection);
        using var reader = command.ExecuteReader();

        var cards = new List<Card>();
        while (reader.Read())
        {
            cards.Add(ReadCard(reader));
        }

        return cards;
    }

    public List<int> GetCardAbilities(int cardId)
    {
        using var connection = _dataSource.OpenConnection();
        using var command = new NpgsqlCommand(
            "SELECT ability_id FROM card_abilities WHERE card_id = @cardId",
            connection);
        command.Parameters.AddWithValue("cardId", cardId);

        using var reader = command.ExecuteReader();
        var abilities = new List<int>();
        while (reader.Read())
        {
            abilities.Add(reader.GetInt32(0));
        }

        return abilities;
    }

    private static Card ReadCard(NpgsqlDataReader reader)
    {
        return new Card
        {
            CardId = reader.GetInt32(0),
            Name = reader.GetString(1),
            Power = reader.GetInt32(2)
        };
    }
}