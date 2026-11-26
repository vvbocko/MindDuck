using MindDuck.Rules.Commands;
using MindDuck.Rules.Models;

namespace MindDuck.Rules.Tests;

public class MindDuckTests
{
    [Fact]
    public void PlayCard_WhenOpponentHasMindDucks_ShouldCreatePendingDecision()
    {
        var (engine, state, playerOne, playerTwo) = CreateGame();

        Card card = playerOne.Hand.Cards[0];

        var result = engine.Apply(state, playerOne, new PlayCardCommand(card.InstanceId));

        Assert.True(result.IsSuccess);
        Assert.Equal(GamePhase.AwaitingDecision, state.Phase);
        Assert.NotNull(state.PendingDecision);

        Assert.Equal(DecisionType.MindDuck, state.PendingDecision!.Type);
        Assert.Equal(playerOne.Id, state.PendingDecision.RequestingPlayer.Id);
        Assert.Equal(playerTwo.Id, state.PendingDecision.RespondingPlayer.Id);

        Assert.Null(playerOne.Hand.FindByInstanceId(card.InstanceId));
        Assert.Equal(5, playerOne.Hand.Count);
        Assert.Equal(4, playerOne.DrawPile.Count);

        Assert.DoesNotContain(card, playerOne.Board.Cards);
        Assert.Equal(2, playerTwo.MindDucksLeft);
    }

    [Fact]
    public void AcceptMindDuck_ShouldTransferCardToOpponent()
    {
        var (engine, state, playerOne, playerTwo) = CreateGame();

        Card card = playerOne.Hand.Cards[0];

        engine.Apply(state, playerOne, new PlayCardCommand(card.InstanceId));

        PendingDecision pending = state.PendingDecision!;

        var result = engine.Apply(
            state,
            playerTwo,
            new SubmitDecisionCommand(pending.DecisionId, DecisionType.MindDuck, true));

        Assert.True(result.IsSuccess);

        Assert.Contains(card, playerTwo.Board.Cards);
        Assert.DoesNotContain(card, playerOne.Board.Cards);

        Assert.Equal(1, playerTwo.MindDucksLeft);
        Assert.Equal(playerOne.Id, state.ActivePlayerId);

        Assert.Equal(GamePhase.Main, state.Phase);
        Assert.Null(state.PendingDecision);
    }

    [Fact]
    public void RejectMindDuck_ShouldLeaveCardWithOriginalPlayer()
    {
        var (engine, state, playerOne, playerTwo) = CreateGame();

        Card card = playerOne.Hand.Cards[0];

        engine.Apply(state, playerOne, new PlayCardCommand(card.InstanceId));

        PendingDecision pending = state.PendingDecision!;

        var result = engine.Apply(
            state,
            playerTwo,
            new SubmitDecisionCommand(pending.DecisionId, DecisionType.MindDuck, false));

        Assert.True(result.IsSuccess);

        Assert.Contains(card, playerOne.Board.Cards);
        Assert.DoesNotContain(card, playerTwo.Board.Cards);

        Assert.Equal(2, playerTwo.MindDucksLeft);
        Assert.Equal(playerTwo.Id, state.ActivePlayerId);

        Assert.Equal(GamePhase.Main, state.Phase);
        Assert.Null(state.PendingDecision);
    }

    [Fact]
    public void AcceptMindDuck_ShouldGiveOriginalPlayerAnotherTurn()
    {
        var (engine, state, playerOne, playerTwo) = CreateGame();

        Card card = playerOne.Hand.Cards[0];

        engine.Apply(state, playerOne, new PlayCardCommand(card.InstanceId));

        PendingDecision pending = state.PendingDecision!;

        engine.Apply(
            state,
            playerTwo,
            new SubmitDecisionCommand(pending.DecisionId, DecisionType.MindDuck, true));

        Assert.Equal(playerOne.Id, state.ActivePlayerId);
        Assert.Equal(2, state.Turn);
    }

    [Fact]
    public void RejectMindDuck_ShouldPassTurnToOpponent()
    {
        var (engine, state, playerOne, playerTwo) = CreateGame();

        Card card = playerOne.Hand.Cards[0];

        engine.Apply(state, playerOne, new PlayCardCommand(card.InstanceId));

        PendingDecision pending = state.PendingDecision!;

        engine.Apply(
            state,
            playerTwo,
            new SubmitDecisionCommand(pending.DecisionId, DecisionType.MindDuck, false));

        Assert.Equal(playerTwo.Id, state.ActivePlayerId);
        Assert.Equal(2, state.Turn);
    }

    [Fact]
    public void PlayerWhoDidNotPlayCard_ShouldNotRespondToMindDuck()
    {
        var (engine, state, playerOne, playerTwo) = CreateGame();

        Card card = playerOne.Hand.Cards[0];

        engine.Apply(state, playerOne, new PlayCardCommand(card.InstanceId));

        PendingDecision pending = state.PendingDecision!;

        var result = engine.Apply(
            state,
            playerOne,
            new SubmitDecisionCommand(pending.DecisionId, DecisionType.MindDuck, true));

        Assert.False(result.IsSuccess);
        Assert.NotNull(state.PendingDecision);
        Assert.Equal(2, playerTwo.MindDucksLeft);
    }

    [Fact]
    public void InvalidDecisionId_ShouldNotResolveMindDuck()
    {
        var (engine, state, playerOne, playerTwo) = CreateGame();

        Card card = playerOne.Hand.Cards[0];

        engine.Apply(state, playerOne, new PlayCardCommand(card.InstanceId));

        PendingDecision pending = state.PendingDecision!;

        var result = engine.Apply(
            state,
            playerTwo,
            new SubmitDecisionCommand(pending.DecisionId + 1, DecisionType.MindDuck, true));

        Assert.False(result.IsSuccess);
        Assert.NotNull(state.PendingDecision);
        Assert.Equal(2, playerTwo.MindDucksLeft);
        Assert.DoesNotContain(card, playerTwo.Board.Cards);
    }

    [Fact]
    public void PlayCard_WhenOpponentHasNoMindDucks_ShouldNotCreatePendingDecision()
    {
        var (engine, state, playerOne, playerTwo) = CreateGame();

        playerTwo.MindDucksLeft = 0;

        Card card = playerOne.Hand.Cards[0];

        var result = engine.Apply(state, playerOne, new PlayCardCommand(card.InstanceId));

        Assert.True(result.IsSuccess);
        Assert.Null(state.PendingDecision);
        Assert.Equal(GamePhase.Main, state.Phase);

        Assert.Contains(card, playerOne.Board.Cards);
        Assert.Equal(playerTwo.Id, state.ActivePlayerId);
    }

    [Fact]
    public void GetLegalActions_DuringMindDuck_ShouldReturnDecisionOptionsOnlyToOpponent()
    {
        var (engine, state, playerOne, playerTwo) = CreateGame();

        Card card = playerOne.Hand.Cards[0];

        engine.Apply(state, playerOne, new PlayCardCommand(card.InstanceId));

        var opponentActions = engine.GetLegalActions(state, playerTwo)
            .OfType<SubmitDecisionCommand>()
            .ToList();

        var requestingPlayerActions = engine.GetLegalActions(state, playerOne);

        Assert.Equal(2, opponentActions.Count);
        Assert.Contains(opponentActions, action => !action.Accepted);
        Assert.Contains(opponentActions, action => action.Accepted);

        Assert.Empty(requestingPlayerActions);
    }

    [Fact]
    public void Apply_WhenUsingPlayerCopy_ShouldModifyPlayerStoredInGameState()
    {
        var (engine, state, playerOne, playerTwo) = CreateGame();

        Player playerCopy = new(playerOne.Id, playerOne.Name);
        Card card = playerOne.Hand.Cards[0];

        var result = engine.Apply(state, playerCopy, new PlayCardCommand(card.InstanceId));

        Assert.True(result.IsSuccess);
        Assert.NotNull(state.PendingDecision);
        Assert.Equal(4, playerOne.DrawPile.Count);
    }

    [Fact]
    public void AcceptMindDuck_WhenUsingPlayerCopy_ShouldModifyOriginalPlayer()
    {
        var (engine, state, playerOne, playerTwo) = CreateGame();

        Card card = playerOne.Hand.Cards[0];

        engine.Apply(state, playerOne, new PlayCardCommand(card.InstanceId));

        PendingDecision pending = state.PendingDecision!;

        Player playerCopy = new(playerTwo.Id, playerTwo.Name);

        var result = engine.Apply(
            state,
            playerCopy,
            new SubmitDecisionCommand(pending.DecisionId, DecisionType.MindDuck, true));

        Assert.True(result.IsSuccess);
        Assert.Equal(1, playerTwo.MindDucksLeft);
        Assert.Contains(card, playerTwo.Board.Cards);
    }

    private static (GameEngine Engine, GameState State, Player PlayerOne, Player PlayerTwo) CreateGame()
    {
        CardDefinition definition = new("test-card", "Test Card", 3);

        Player playerOne = new(1, "Player One", CreateDeck(definition, 1));
        Player playerTwo = new(2, "Player Two", CreateDeck(definition, 11));

        GameEngine engine = new();

        GameState state = engine.StartNewGame(new GameSetup(playerOne, playerTwo));

        return (engine, state, playerOne, playerTwo);
    }

    private static Card[] CreateDeck(CardDefinition definition, int firstInstanceId)
    {
        return Enumerable.Range(firstInstanceId, 10)
            .Select(id => new Card(id, definition))
            .ToArray();
    }
}