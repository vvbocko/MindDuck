using MindDuck.Rules.Interfaces;
using MindDuck.Rules.Models;

namespace MindDuck.Rules.Effects;

public static class CardEffectResolver
{
    public static void Resolve(GameState state, Player controller, Card source, CardEffectTrigger trigger)
    {
        Player opponent = state.GetOpponent(controller);

        foreach (ICardEffect effect in source.Definition.Effects)
        {
            if (effect.Trigger == trigger)
                effect.Execute(state, controller, opponent, source);
        }
    }
}