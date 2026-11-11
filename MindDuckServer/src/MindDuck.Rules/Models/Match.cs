using MindDuck.Rules.Commands;

namespace MindDuck.Rules.Models 
{
    public class Match
    {
        public Guid MatchId { get; set; }

        public GameState GameState { get; set; }

        public List<GameCommand> History { get; set; } = new();
    }
}