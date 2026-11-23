using MindDuck.Rules.Interfaces;
using MindDuck.Rules.Models;

namespace MindDuck.Rules.Effects;

public sealed class DemagePlayerEffects(CardEffectTrigger trigger, int amount) : ICardEffect
{
    public CardEffectTrigger Trigger { get; } = trigger;

    public void Execute(GameState state, Player controller, Player opponent, Card source)
    {
        opponent.Life -= amount;
    }
}