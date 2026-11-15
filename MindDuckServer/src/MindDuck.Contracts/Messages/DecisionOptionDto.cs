namespace MindDuck.Contracts
{
    /// <summary>One choice presented to a player in a DecisionRequired message.</summary>
    public sealed class DecisionOptionDto
    {
        /// <summary>Send this value back in SubmitDecision.OptionId.</summary>
        public string OptionId { get; set; }

        /// <summary>Instance ID of the card this option targets, when applicable.</summary>
        public int? TargetInstanceId { get; set; }

        /// <summary>Human-readable label shown to the player.</summary>
        public string Label { get; set; }
    }
}
