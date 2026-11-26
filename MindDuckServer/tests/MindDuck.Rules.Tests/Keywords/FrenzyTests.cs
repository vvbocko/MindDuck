using MindDuck.Rules.Commands;
using MindDuck.Rules.Models;
using MindDuck.Rules.Results;

namespace MindDuck.Rules.Tests.Keywords;

public class FrenzyTests
{
    [Fact]
    public void FrenzyCreature_ShouldAttackTwiceInOneTurn()
    {
        var (engine, state, playerOne, playerTwo) = KeywordTestFactory.CreateGame();

        Card attacker = KeywordTestFactory.AddCreature(state, playerOne, 400, 3, Keyword.Frenzy);

        int opponentStartingLife = playerTwo.Life;

        ApplyResult firstAttack = engine.Apply(state, playerOne, new AttackCommand(attacker.InstanceId));

        Assert.True(firstAttack.IsSuccess);
        Assert.Equal(opponentStartingLife - 1, playerTwo.Life);
        Assert.Equal(playerOne.Id, state.ActivePlayerId);
        Assert.Equal(attacker.InstanceId, state.FrenzyContinuationCardInstanceId);

        IReadOnlyList<GameCommand> legalActions = engine.GetLegalActions(state, playerOne);

        Assert.Contains(legalActions, action => 
            action is AttackCommand attack && attack.AttackerInstanceId == attacker.InstanceId);

        Assert.Contains(legalActions, action => action is EndTurnCommand);

        Assert.DoesNotContain(legalActions, action => action is PlayCardCommand);

        ApplyResult secondAttack = engine.Apply(state, playerOne, new AttackCommand(attacker.InstanceId));

        Assert.True(secondAttack.IsSuccess);
        Assert.Equal(opponentStartingLife - 2, playerTwo.Life);
        Assert.Equal(playerTwo.Id, state.ActivePlayerId);
        Assert.Null(state.FrenzyContinuationCardInstanceId);
    }

    [Fact]
    public void FrenzyCreature_ShouldNotAttackAgainWhenDefeatedByBlocker()
    {
        var (engine, state, playerOne, playerTwo) = KeywordTestFactory.CreateGame();

        Card attacker = KeywordTestFactory.AddCreature(state, playerOne, 401, 1, Keyword.Frenzy);

        Card defender = KeywordTestFactory.AddCreature(state, playerTwo, 501, 5);

        ApplyResult attackResult = engine.Apply(state, playerOne, new AttackCommand(attacker.InstanceId));

        Assert.True(attackResult.IsSuccess);

        PendingDecision pending = Assert.IsType<PendingDecision>(state.PendingDecision);

        ApplyResult blockResult = engine.Apply(state, playerTwo, new SubmitDecisionCommand(
            pending.DecisionId,
            DecisionType.Block,
            true,
            defender.InstanceId));

        Assert.True(blockResult.IsSuccess);

        Assert.DoesNotContain(attacker, playerOne.Board.Cards);
        Assert.Null(state.FrenzyContinuationCardInstanceId);
        Assert.Equal(playerTwo.Id, state.ActivePlayerId);
    }
}