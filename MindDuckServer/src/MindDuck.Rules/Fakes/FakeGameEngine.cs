// FakeGameEngine - a stand-in for the real rules engine, so the server can run whole matches NOW.
//
// WHY IT EXISTS
//   The real GameEngine.Apply isn't finished yet. Without an engine the server can't answer any
//   move, so Unity <-> server can't be tested. This class implements the SAME IGameEngine as the real
//   engine, with very simple "rules":
//     - StartNewGame : 3 life, 2 Mindbugs, 10 cards per player (if the server gave none), draw 5
//     - PlayCard     : card goes from hand to board, player draws back to 5, turn passes
//     - Attack       : opponent loses 1 life (no blocking), turn passes; 0 life = game over
//     - decisions    : none yet (needs PendingDecision in ApplyResult - see the IGameEngine agreement)
//   The server switches between fake and real engine in ONE place (Program.cs). Nothing else changes.
//
// WHY IT LIVES IN MindDuck.Rules
//   GameState's setters (Turn, ActivePlayerId, Phase, WinnerPlayerId) are "internal" = only code inside
//   MindDuck.Rules may change them. So the fake has to sit next to the real engine.
//
// WHEN TO DELETE IT
//   Never needed in the final game, but keep it for server tests: it is predictable (no shuffling),
//   so a test always gets the same cards.

using MindDuck.Rules.Commands;
using MindDuck.Rules.Interfaces;
using MindDuck.Rules.Models;
using MindDuck.Rules.Results;

namespace MindDuck.Rules.Fakes;

public sealed class FakeGameEngine : IGameEngine
{
    private const int MindDucksAtStart = 2;
    private const int HandSize = 5;
    private const int DeckSize = 10;

    private readonly IReadOnlyList<CardDefinition> _cards;

    /// <param name="cards">
    /// Card types to build decks from (e.g. loaded from data/cards/cards.json).
    /// Leave it out and the fake uses a few built-in cards whose ids also exist in cards.json.
    /// </param>
    public FakeGameEngine(IReadOnlyList<CardDefinition>? cards = null)
    {
        _cards = cards is { Count: > 0 } ? cards : DemoCards();
    }

    public GameState StartNewGame(GameSetup setup)
    {
        ArgumentNullException.ThrowIfNull(setup);

        Player playerOne = setup.PlayerOne;
        Player playerTwo = setup.PlayerTwo;

        // If the server didn't give the players any cards, deal 10 each.
        // Instance ids must be unique in the match: P1 gets 1-10, P2 gets 11-20.
        if (playerOne.DrawPile.Count == 0) FillDeck(playerOne, firstInstanceId: 1);
        if (playerTwo.DrawPile.Count == 0) FillDeck(playerTwo, firstInstanceId: DeckSize + 1);

        foreach (Player player in new[] { playerOne, playerTwo })
        {
            player.Life = setup.StartingLife;
            player.MindDucksLeft = MindDucksAtStart;
            player.DrawCards(setup.StartingHandSize);
        }

        return new GameState(playerOne, playerTwo); // player one starts
    }

    public ApplyResult Apply(GameState state, Player player, GameCommand command)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(command);

        if (state.Phase == GamePhase.Finished)
            return ApplyResult.Failure(ApplyErrorCode.InvalidGamePhase, "The game is over.");

        // Use the Player object stored in the state (the server might pass a different copy)
        Player? me = FindPlayer(state, player);
        if (me == null)
            return ApplyResult.Failure(ApplyErrorCode.InvalidPlayer, $"Player {player.Id} is not in this game.");

        if (state.ActivePlayerId != me.Id)
            return ApplyResult.Failure(ApplyErrorCode.NotPlayersTurn, $"It is not {me.Name}'s turn.");

        Player opponent = state.GetOpponent(me);

        switch (command)
        {
            case PlayCardCommand play:
                return PlayCard(state, me, opponent, play.CardInstanceId);

            case AttackCommand attack:
                return Attack(state, me, opponent, attack.AttackerInstanceId);

            case EndTurnCommand:
                PassTurn(state, opponent);
                return ApplyResult.Success(state);

            case SubmitDecisionCommand:
                return ApplyResult.Failure(ApplyErrorCode.MissingDecision, "The fake engine doesn't ask for decisions yet.");

            default:
                return ApplyResult.Failure(ApplyErrorCode.InvalidCommand, $"Unknown command {command.GetType().Name}.");
        }
    }

    public IReadOnlyList<GameCommand> GetLegalActions(GameState state, Player player)
    {
        Player? me = FindPlayer(state, player);

        if (me == null || state.Phase != GamePhase.Main || state.ActivePlayerId != me.Id)
            return Array.Empty<GameCommand>();

        var actions = new List<GameCommand>();

        foreach (Card card in me.Hand.Cards)
            actions.Add(new PlayCardCommand(card.InstanceId));

        foreach (Card creature in me.Board.Cards)
            actions.Add(new AttackCommand(creature.InstanceId));

        return actions;
    }

    // ----- fake "rules" ------------------------------------------------------

    private static ApplyResult PlayCard(GameState state, Player me, Player opponent, int cardInstanceId)
    {
        Card? card = me.Hand.FindByInstanceId(cardInstanceId);
        if (card == null)
            return ApplyResult.Failure(ApplyErrorCode.IllegalAction, $"Card {cardInstanceId} is not in your hand.");

        me.Hand.Remove(card);
        card.PrepareForPlay(state.Turn);
        me.Board.Add(card);
        me.DrawCards(HandSize - me.Hand.Count); // draw back up to 5 (rules section 3)

        PassTurn(state, opponent);
        return ApplyResult.Success(state);
    }

    private static ApplyResult Attack(GameState state, Player me, Player opponent, int attackerInstanceId)
    {
        Card? attacker = me.Board.FindByInstanceId(attackerInstanceId);
        if (attacker == null)
            return ApplyResult.Failure(ApplyErrorCode.IllegalAction, $"Creature {attackerInstanceId} is not on your board.");

        opponent.Life = Math.Max(0, opponent.Life - 1); // always unblocked in the fake

        if (opponent.Life == 0)
        {
            state.Phase = GamePhase.Finished;
            state.WinnerPlayerId = me.Id;
            return ApplyResult.Success(state);
        }

        PassTurn(state, opponent);
        return ApplyResult.Success(state);
    }

    private static void PassTurn(GameState state, Player nextPlayer)
    {
        state.Turn++;
        state.ActivePlayerId = nextPlayer.Id;
    }

    // --- helpers ------------------------------------------------------------------------

    private static Player? FindPlayer(GameState state, Player player)
    {
        return state.Players.FirstOrDefault(p => p.Id == player.Id);
    }

    private void FillDeck(Player player, int firstInstanceId)
    {
        for (int i = 0; i < DeckSize; i++)
        {
            // P1 starts at the first card type, P2 further down the list, so they get different cards.
            CardDefinition definition = _cards[(firstInstanceId - 1 + i) % _cards.Count];
            player.DrawPile.Add(new Card(firstInstanceId + i, definition));
        }
    }

    /// <summary>A few cards from data/cards/cards.json (same ids, names, power and keyword).</summary>
    private static IReadOnlyList<CardDefinition> DemoCards()
    {
        return new List<CardDefinition>
        {
            new CardDefinition("quackoon", "Quackoon", 3),
            new CardDefinition("duckobra", "Duckobra", 3, new[] { Keyword.Poisonous }),
            new CardDefinition("quacksmith", "Quacksmith", 4),
            new CardDefinition("wereduck", "Wereduck", 6, new[] { Keyword.Frenzy }),
            new CardDefinition("mallardillo", "Mallardillo", 3, new[] { Keyword.Tough }),
            new CardDefinition("quacken", "Quacken", 9, new[] { Keyword.Tough }),
            new CardDefinition("beakgeist", "Beakgeist", 4),
            new CardDefinition("quackini", "Quackini", 2, new[] { Keyword.Tough }),
            new CardDefinition("honk-kong", "Honk Kong", 8),
            new CardDefinition("ducktopus", "Ducktopus", 5, new[] { Keyword.Poisonous }),
            new CardDefinition("quacktus", "Quacktus", 2, new[] { Keyword.Poisonous }),
            new CardDefinition("quackrobat", "Quackrobat", 4, new[] { Keyword.Frenzy }),
            new CardDefinition("quackula", "Quackula", 5),
            new CardDefinition("eggsploder", "Eggsploder", 1),
            new CardDefinition("duckydra", "Duckydra", 6, new[] { Keyword.Frenzy }),
            new CardDefinition("drakenstein", "Drakenstein", 7, new[] { Keyword.Tough }),
            new CardDefinition("pteroducktyl", "Pteroducktyl", 5),
            new CardDefinition("quackodile", "Quackodile", 7, new[] { Keyword.Frenzy }),
            new CardDefinition("ducklops", "Ducklops", 8),
            new CardDefinition("duckasaurus-rex", "Duckasaurus Rex", 10)
        };
    }
}
