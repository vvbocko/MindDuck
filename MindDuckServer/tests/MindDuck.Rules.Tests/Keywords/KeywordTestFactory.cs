using MindDuck.Rules.Models;

namespace MindDuck.Rules.Tests.Keywords;

internal static class KeywordTestFactory
{
    public static (GameEngine Engine, GameState State, Player PlayerOne, Player PlayerTwo) CreateGame()
    {
        CardDefinition deckCardDefinition = new("deck-card", "Deck Card", 2);

        Player playerOne = new(1, "Player One", CreateDeck(deckCardDefinition, 1));
        Player playerTwo = new(2, "Player Two", CreateDeck(deckCardDefinition, 11));

        GameEngine engine = new();
        GameState state = engine.StartNewGame(new GameSetup(playerOne, playerTwo));

        return (engine, state, playerOne, playerTwo);
    }

    public static Card AddCreature(GameState state, Player player, int instanceId, int power, params Keyword[] keywords)
    {
        Card card = CreateCard(instanceId, power, keywords);
        card.PrepareForPlay(state.Turn - 1);
        player.Board.Add(card);

        return card;
    }

    public static Card CreateCard(int instanceId, int power, params Keyword[] keywords)
    {
        CardDefinition definition = new($"card-{instanceId}", $"Creature {instanceId}", power, keywords);

        return new Card(instanceId, definition);
    }

    private static Card[] CreateDeck(CardDefinition definition, int firstInstanceId)
    {
        return Enumerable.Range(firstInstanceId, 10)
            .Select(id => new Card(id, definition))
            .ToArray();
    }
}