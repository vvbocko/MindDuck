// Client -> Server (WebSocket). The client only sends INTENTIONS; the server decides what happens.

namespace MindDuck.Contracts
{
    /// <summary>First message after the socket opens. Answer: RoomJoined (or Error).</summary>
    public sealed class JoinRoom : ClientMessage
    {
        public string RoomCode { get; set; }
        public string Nickname { get; set; }
    }

    /// <summary>Come back after a disconnect (within Protocol.ReconnectSeconds) with the token from RoomJoined.</summary>
    public sealed class Rejoin : ClientMessage
    {
        public string RoomCode { get; set; }
        public string PlayerToken { get; set; }
    }

    /// <summary>Play a card from your hand. Only valid in phase AwaitingAction on your turn.</summary>
    public sealed class PlayCard : ClientMessage
    {
        public int CardInstanceId { get; set; }
    }

    /// <summary>Attack with one of your creatures. Only valid in phase AwaitingAction on your turn.</summary>
    public sealed class Attack : ClientMessage
    {
        public int CreatureInstanceId { get; set; }
    }

    /// <summary>Answer to a DecisionRequired: copy its DecisionId and pick one option's OptionId.</summary>
    public sealed class SubmitDecision : ClientMessage
    {
        public int DecisionId { get; set; }
        public string OptionId { get; set; }
    }

    /// <summary>Player quits the room/match. Opponent gets MatchEnded(OpponentLeft).</summary>
    public sealed class LeaveRoom : ClientMessage
    {
    }
}
