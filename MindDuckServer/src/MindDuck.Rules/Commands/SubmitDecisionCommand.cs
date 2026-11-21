using MindDuck.Rules.Models;

namespace MindDuck.Rules.Commands;
public class SubmitDecisionCommand(int decitionId, DecisionType decisionType, bool accepted, int? defenderCardId = null) : GameCommand
{
    public int DecisionId { get; } = decitionId;

    public DecisionType DecisionType { get; } = decisionType;

    // MindDuck: opponent takes the card
    // Block: does the defender decide to block
    public bool Accepted { get; } = accepted;

    // blocking creature
    public int? DefenderCardId { get; } = defenderCardId;
}