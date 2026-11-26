using MindDuck.Rules.Models;

namespace MindDuck.Rules.Tests.Keywords;

public class ToughTests
{
    [Fact]
    public void Tough_ShouldPreventFirstDefeat()
    {
        Card attacker = KeywordTestFactory.CreateCard(300, 8);

        Card defender = KeywordTestFactory.CreateCard(301, 3, Keyword.Tough);

        CombatResult result = CombatRules.Resolve(attacker, defender);

        Assert.False(result.DefenderDefeated);
        Assert.True(defender.ToughUsed);
    }

    [Fact]
    public void Tough_ShouldNotPreventSecondDefeat()
    {
        Card attacker = KeywordTestFactory.CreateCard(302, 8);

        Card defender = KeywordTestFactory.CreateCard(303, 3, Keyword.Tough);

        CombatResult firstResult = CombatRules.Resolve(attacker, defender);

        CombatResult secondResult = CombatRules.Resolve(attacker, defender);

        Assert.False(firstResult.DefenderDefeated);
        Assert.True(defender.ToughUsed);

        Assert.True(secondResult.DefenderDefeated);
    }

    [Fact]
    public void Tough_ShouldPreventDefeatCausedByPoisonous()
    {
        Card poisonousAttacker = KeywordTestFactory.CreateCard(304, 1, Keyword.Poisonous);

        Card toughDefender = KeywordTestFactory.CreateCard(305, 3, Keyword.Tough);

        CombatResult result = CombatRules.Resolve(poisonousAttacker, toughDefender);

        Assert.True(result.AttackerDefeated);
        Assert.False(result.DefenderDefeated);
        Assert.True(toughDefender.ToughUsed);
    }

    [Fact]
    public void Tough_ShouldRemainUsedWhenTurnStarts()
    {
        Card toughCreature = KeywordTestFactory.CreateCard(306, 3, Keyword.Tough);

        Assert.True(toughCreature.TryPreventDefeat());

        toughCreature.StartTurn();

        Assert.True(toughCreature.ToughUsed);
    }

    [Fact]
    public void Tough_ShouldResetWhenCreatureIsPreparedForPlayAgain()
    {
        Card toughCreature = KeywordTestFactory.CreateCard(307, 3, Keyword.Tough);

        Assert.True(toughCreature.TryPreventDefeat());

        toughCreature.PrepareForPlay(5);

        Assert.False(toughCreature.ToughUsed);
    }

    [Fact]
    public void CreatureWithoutTough_ShouldNotPreventDefeat()
    {
        Card attacker = KeywordTestFactory.CreateCard(308, 8);

        Card defender = KeywordTestFactory.CreateCard(309, 3);

        CombatResult result = CombatRules.Resolve(attacker, defender);

        Assert.True(result.DefenderDefeated);
        Assert.False(defender.ToughUsed);
    }
}