using MindDuck.Rules.Models;

namespace MindDuck.Rules;

public readonly record struct CombatResult(bool AttackerDefeated, bool DefenderDefeated);

public static class CombatRules
{
    public static CombatResult Resolve(Card attacker, Card defender)
    {
        bool attackerDefeated =
            defender.Power >= attacker.Power ||
            defender.HasKeyword(Keyword.Poisonous);

        bool defenderDefeated =
            attacker.Power >= defender.Power ||
            attacker.HasKeyword(Keyword.Poisonous);

        if (attackerDefeated && attacker.TryPreventDefeat())
            attackerDefeated = false;

        if (defenderDefeated && defender.TryPreventDefeat())
            defenderDefeated = false;

        return new CombatResult(attackerDefeated, defenderDefeated);
    }
}