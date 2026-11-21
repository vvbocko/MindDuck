namespace MindDuck.Rules.Models;

public sealed class PendingDecision(int decisionId, DecisionType type, Player requestingPlayer, Player respondingPlayer, Card? card = null, int? forcedDefenderCardId = null)
{
    public int DecisionId { get; } = decisionId;

    public DecisionType Type { get; } = type;

    public Player RequestingPlayer { get; } = requestingPlayer;

    public Player RespondingPlayer { get; } = respondingPlayer;

    public Card? Card { get; } = card;

    // used with Hunter
    public int? ForcedDefenderCardId { get; } = forcedDefenderCardId;
}