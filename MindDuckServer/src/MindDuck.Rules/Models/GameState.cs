namespace MindDuck.Rules.Models
{
    public class GameState
    {
        public string ActivePlayerName { get; set; }
        public List<Player> Players { get; set; } = new List<Player>();
    }
}