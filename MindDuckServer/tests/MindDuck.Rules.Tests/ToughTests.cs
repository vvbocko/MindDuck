using MindDuck.Rules;
using MindDuck.Rules.Models;
using Xunit;

public class ToughTests
{
    [Fact]
    public void Tough_PreventsFirstDefeat()
    {
        var toughCard = CreateCard(3, Keyword.Tough);
        var attacker = CreateCard(5);

        var result = CombatRules.Resolve(attacker, toughCard);

        Assert.False(result.AttackerDefeated);
        Assert.False(result.DefenderDefeated);
        Assert.True(toughCard.ToughUsed);
    }

    [Fact]
    public void Tough_DoesNotPreventSecondDefeat()
    {
        var toughCard = CreateCard(3, Keyword.Tough);
        var attacker = CreateCard(5);

        CombatRules.Resolve(attacker, toughCard);

        var result = CombatRules.Resolve(attacker, toughCard);

        Assert.False(result.AttackerDefeated);
        Assert.True(result.DefenderDefeated);
    }

    private static Card CreateCard(int power, params Keyword[] keywords)
    {
        var definition = new CardDefinition(
            Guid.NewGuid().ToString(),
            "Test Card",
            power,
            keywords);

        return new Card(
            Random.Shared.Next(1, int.MaxValue),
            definition);
    }
}