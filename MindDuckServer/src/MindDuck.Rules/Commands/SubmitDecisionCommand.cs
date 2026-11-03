namespace MindDuck.Rules.Commands
{
    public class SubmitDecisionCommand : GameCommand
    {
        public string DecisionType { get; set; } // użycie mindbuga lub obrona, "Mindbug", "Block"
        public bool Accepted { get; set; } // tak lub nie
        public int DefenderCardId { get; set; } // id karty broniącej
    }
}