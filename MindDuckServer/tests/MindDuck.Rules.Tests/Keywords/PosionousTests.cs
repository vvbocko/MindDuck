using MindDuck.Rules.Commands;
using MindDuck.Rules.Models;
using MindDuck.Rules.Results;

namespace MindDuck.Rules.Tests.Keywords;

public class PoisonousTests
{
    [Fact]
    public void PoisonousAttacker_ShouldDefeatStrongerBlockingCreature()
    {
        var (engine, state, playerOne, playerTwo) = KeywordTestFactory.CreateGame();

        Card attacker = KeywordTestFactory.AddCreature(state, playerOne, 100, 1, Keyword.Poisonous);

        Card defender = KeywordTestFactory.AddCreature(state, playerTwo, 200, 8);

        ApplyAttackAndBlock(engine, state, playerOne, playerTwo, attacker, defender);

        Assert.DoesNotContain(attacker, playerOne.Board.Cards);
        Assert.DoesNotContain(defender, playerTwo.Board.Cards);
    }

    [Fact]
    public void PoisonousDefender_ShouldDefeatStrongerAttacker()
    {
        var (engine, state, playerOne, playerTwo) = KeywordTestFactory.CreateGame();

        Card attacker = KeywordTestFactory.AddCreature(state, playerOne, 101, 8);

        Card defender = KeywordTestFactory.AddCreature(state, playerTwo, 201, 1, Keyword.Poisonous);

        ApplyAttackAndBlock(engine, state, playerOne, playerTwo, attacker, defender);

        Assert.DoesNotContain(attacker, playerOne.Board.Cards);
        Assert.DoesNotContain(defender, playerTwo.Board.Cards);
    }

    private static void ApplyAttackAndBlock(
        GameEngine engine,
        GameState state,
        Player attackerPlayer,
        Player defenderPlayer,
        Card attacker,
        Card defender)
    {
        ApplyResult attackResult = engine.Apply(state, attackerPlayer, new AttackCommand(attacker.InstanceId));
        Assert.True(attackResult.IsSuccess);

        PendingDecision pending = Assert.IsType<PendingDecision>(state.PendingDecision);
        Assert.Equal(DecisionType.Block, pending.Type);

        ApplyResult blockResult = engine.Apply(state, defenderPlayer, new SubmitDecisionCommand(
            pending.DecisionId,
            DecisionType.Block,
            true,
            defender.InstanceId));
        Assert.True(blockResult.IsSuccess);
    }
}