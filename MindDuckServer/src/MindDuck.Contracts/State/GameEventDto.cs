namespace MindDuck.Contracts
{
    /// <summary>A game event used by the client for animations and the match log.</summary>
    public sealed class GameEventDto
    {
        public GameEventType Type { get; set; }

        /// <summary>The player associated with the event, if applicable.</summary>
        public PlayerSeat? Seat { get; set; }

        /// <summary>The card associated with the event, if applicable.</summary>
        public int? CardInstanceId { get; set; }

        /// <summary>A target card associated with the event, if applicable.</summary>
        public int? TargetCardInstanceId { get; set; }

        /// <summary>A count or numeric change associated with the event, if applicable.</summary>
        public int? Amount { get; set; }
    }
}
