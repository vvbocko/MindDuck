using MindDuck.Rules.Commands;
using MindDuck.Rules.Models;
using MindDuck.Rules.Results;

namespace MindDuck.Rules.Tests.Keywords;

public class SneakyTests
{
    [Fact]
    public void SneakyAttacker_ShouldOnlyBeBlockedBySneakyCreatures()
    {
        var (engine, state, playerOne, playerTwo) = KeywordTestFactory.CreateGame();

        Card attacker = KeywordTestFactory.AddCreature(state, playerOne, 800, 5, Keyword.Sneaky);

        Card normalDefender = KeywordTestFactory.AddCreature(state, playerTwo, 900, 2);

        Card sneakyDefender = KeywordTestFactory.AddCreature(state, playerTwo, 901, 2, Keyword.Sneaky);

        ApplyResult attackResult = engine.Apply(state, playerOne, new AttackCommand(attacker.InstanceId));

        Assert.True(attackResult.IsSuccess);

        PendingDecision pending = Assert.IsType<PendingDecision>(state.PendingDecision);

        IReadOnlyList<GameCommand> legalActions = engine.GetLegalActions(state, playerTwo);

        // Refusing to block, or creating a block via Sneaky.
        Assert.Equal(2, legalActions.Count);

        Assert.DoesNotContain(legalActions, action =>
            action is SubmitDecisionCommand decision &&
            decision.DefenderCardId == normalDefender.InstanceId);

        Assert.Contains(legalActions, action =>
            action is SubmitDecisionCommand decision &&
            decision.DefenderCardId == sneakyDefender.InstanceId);

        ApplyResult invalidBlock = engine.Apply(state, playerTwo, new SubmitDecisionCommand(
            pending.DecisionId,
            DecisionType.Block,
            true,
            normalDefender.InstanceId));

        Assert.False(invalidBlock.IsSuccess);
        Assert.NotNull(state.PendingDecision);

        ApplyResult validBlock = engine.Apply(state, playerTwo, new SubmitDecisionCommand(
            pending.DecisionId,
            DecisionType.Block,
            true,
            sneakyDefender.InstanceId));

        Assert.True(validBlock.IsSuccess);

        Assert.Contains(normalDefender, playerTwo.Board.Cards);
        Assert.DoesNotContain(sneakyDefender, playerTwo.Board.Cards);
    }

    [Fact]
    public void SneakyAttacker_WithNoSneakyBlockers_ShouldDealDirectDamage()
    {
        var (engine, state, playerOne, playerTwo) = KeywordTestFactory.CreateGame();

        Card attacker = KeywordTestFactory.AddCreature(state, playerOne, 801, 5, Keyword.Sneaky);

        KeywordTestFactory.AddCreature(state, playerTwo, 902, 2);

        int opponentStartingLife = playerTwo.Life;

        ApplyResult result = engine.Apply(state, playerOne, new AttackCommand(attacker.InstanceId));

        Assert.True(result.IsSuccess);

        Assert.Equal(opponentStartingLife - 1, playerTwo.Life);
        Assert.Null(state.PendingDecision);
        Assert.Equal(playerTwo.Id, state.ActivePlayerId);
    }
}