using MindDuck.Rules.Commands;

namespace MindDuck.Rules.Models;

public class Match
{
    public Guid MatchId { get; set; }

    public required GameState GameState { get; init; }

    public List<GameCommand> History { get; set; } = new();
}