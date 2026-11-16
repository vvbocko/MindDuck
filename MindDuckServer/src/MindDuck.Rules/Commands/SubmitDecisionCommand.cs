using MindDuck.Rules.Models;

namespace MindDuck.Rules.Commands;
public class SubmitDecisionCommand(int decitionId, DecisionType decisionType, bool accepted, int? defenderCardId = null) : GameCommand
{
    public int DecisionId { get; } = decitionId;

    public DecisionType DecisionType { get; } = decisionType;

    public bool Accepted { get; } = accepted;

    public int? DefenderCardId { get; } = defenderCardId;
}