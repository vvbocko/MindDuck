using MindDuck.Rules.Effects;
using MindDuck.Rules.Models;

namespace MindDuck.Rules.Interfaces;

public interface ICardEffect
{
    CardEffectTrigger Trigger { get; }

    void Execute(GameState state, Player controller, Player opponent, Card source);
}