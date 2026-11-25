using MindDuck.Rules.Commands;
using MindDuck.Rules.Models;
using MindDuck.Rules.Results;

namespace MindDuck.Rules.Tests.Keywords;

public class HunterTests
{
    [Fact]
    public void Hunter_ShouldForceSelectedCreatureToBlock()
    {
        var (engine, state, playerOne, playerTwo) = KeywordTestFactory.CreateGame();

        Card hunter = KeywordTestFactory.AddCreature(state, playerOne, 600, 5, Keyword.Hunter);

        Card selectedDefender = KeywordTestFactory.AddCreature(state, playerTwo, 700, 2);

        KeywordTestFactory.AddCreature(state, playerTwo, 701, 2);

        ApplyResult result = engine.Apply(state, playerOne, new AttackCommand(hunter.InstanceId, selectedDefender.InstanceId));

        Assert.True(result.IsSuccess);

        PendingDecision pending = Assert.IsType<PendingDecision>(state.PendingDecision);

        Assert.Equal(selectedDefender.InstanceId, pending.ForcedDefenderCardId);

        IReadOnlyList<GameCommand> legalActions = engine.GetLegalActions(state, playerTwo);

        SubmitDecisionCommand legalBlock = Assert.IsType<SubmitDecisionCommand>(Assert.Single(legalActions));

        Assert.True(legalBlock.Accepted);
        Assert.Equal(selectedDefender.InstanceId, legalBlock.DefenderCardId);
    }

    [Fact]
    public void Hunter_ShouldNotAllowForcedBlockToBeRefused()
    {
        var (engine, state, playerOne, playerTwo) = KeywordTestFactory.CreateGame();

        Card hunter = KeywordTestFactory.AddCreature(state, playerOne, 601, 5, Keyword.Hunter);

        Card defender = KeywordTestFactory.AddCreature(state, playerTwo, 701, 2);

        ApplyResult attackResult = engine.Apply(state, playerOne, new AttackCommand(hunter.InstanceId, defender.InstanceId));

        Assert.True(attackResult.IsSuccess);

        PendingDecision pending = Assert.IsType<PendingDecision>(state.PendingDecision);

        ApplyResult refusal = engine.Apply(state, playerTwo, new SubmitDecisionCommand(
            pending.DecisionId,
            DecisionType.Block,
            false));

        Assert.False(refusal.IsSuccess);
        Assert.NotNull(state.PendingDecision);
        Assert.Equal(GamePhase.AwaitingDecision, state.Phase);
    }

    [Fact]
    public void HunterAndSneaky_ShouldBeAbleToForceNormalCreatureToBlock()
    {
        var (engine, state, playerOne, playerTwo) = KeywordTestFactory.CreateGame();

        Card hunter = KeywordTestFactory.AddCreature(state, playerOne, 602, 5, Keyword.Hunter, Keyword.Sneaky);

        Card normalDefender = KeywordTestFactory.AddCreature(state, playerTwo, 702, 2);

        ApplyResult attackResult = engine.Apply(state, playerOne, new AttackCommand(hunter.InstanceId, normalDefender.InstanceId));

        Assert.True(attackResult.IsSuccess);

        PendingDecision pending = Assert.IsType<PendingDecision>(state.PendingDecision);

        IReadOnlyList<GameCommand> legalActions = engine.GetLegalActions(state, playerTwo);

        SubmitDecisionCommand forcedBlock = Assert.IsType<SubmitDecisionCommand>(Assert.Single(legalActions));

        Assert.Equal(normalDefender.InstanceId, forcedBlock.DefenderCardId);

        ApplyResult blockResult = engine.Apply(state, playerTwo, new SubmitDecisionCommand(
            pending.DecisionId,
            DecisionType.Block,
            true,
            normalDefender.InstanceId));

        Assert.True(blockResult.IsSuccess);
        Assert.DoesNotContain(normalDefender, playerTwo.Board.Cards);
    }

    [Fact]
    public void NonHunter_ShouldNotBeAbleToSelectForcedBlocker()
    {
        var (engine, state, playerOne, playerTwo) = KeywordTestFactory.CreateGame();

        Card attacker = KeywordTestFactory.AddCreature(state, playerOne, 603, 5);

        Card defender = KeywordTestFactory.AddCreature(state, playerTwo, 703, 2);

        ApplyResult result = engine.Apply(state, playerOne, new AttackCommand(attacker.InstanceId, defender.InstanceId));

        Assert.False(result.IsSuccess);
        Assert.Null(state.PendingDecision);
    }
}