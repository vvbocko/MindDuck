// Server -> Client (WebSocket).

using System.Collections.Generic;

namespace MindDuck.Contracts
{
    /// <summary>You are in the room. Keep PlayerToken (also in PlayerPrefs) — it is your key for Rejoin.</summary>
    public sealed class RoomJoined : ServerMessage
    {
        public string RoomCode { get; set; }
        public PlayerSeat Seat { get; set; }
        public string PlayerToken { get; set; }
        /// <summary>null while you are alone in the room.</summary>
        public string OpponentNickname { get; set; }
    }

    /// <summary>Both players are in; the game has been set up.</summary>
    public sealed class MatchStarted : ServerMessage
    {
        public string MatchId { get; set; }
        public PlayerSeat StartingSeat { get; set; }
    }

    /// <summary>
    /// The COMPLETE game as THIS player may see it, plus what just happened.
    /// The client simply redraws everything from State; Events are only for animations / log.
    /// </summary>
    public sealed class StateUpdate : ServerMessage
    {
        /// <summary>Increases with every update. Ignore an update older than the last one drawn.</summary>
        public int Sequence { get; set; }
        public GameStateView State { get; set; }
        public List<GameEventDto> Events { get; set; } = new List<GameEventDto>();
    }

    /// <summary>YOU must choose one of Options. Sent only to the deciding player.</summary>
    public sealed class DecisionRequired : ServerMessage
    {
        public int DecisionId { get; set; }
        public DecisionKind Kind { get; set; }
        /// <summary>
        /// The card the question is about, so the client can highlight it: the played card (UseMindbug),
        /// the attacker (Block, FrenzyAttackAgain) or the creature whose ability is used (ChooseTarget).
        /// </summary>
        public int? SubjectCardInstanceId { get; set; }
        /// <summary>Human-readable question, e.g. "Ala played Ember Wolf (7). Use a Mindbug?"</summary>
        public string Prompt { get; set; }
        public List<DecisionOptionDto> Options { get; set; } = new List<DecisionOptionDto>();
    }

    /// <summary>Your command broke a rule. Nothing changed. Code: NotYourTurn, IllegalMove or InvalidDecision.</summary>
    public sealed class ActionRejected : ServerMessage
    {
        public ErrorCode Code { get; set; }
        /// <summary>Human-readable reason for logs and the UI, e.g. "Card 17 is not in your hand".</summary>
        public string Message { get; set; }
    }

    public sealed class OpponentDisconnected : ServerMessage
    {
        public int SecondsToReconnect { get; set; }
    }

    public sealed class OpponentReconnected : ServerMessage
    {
    }

    public sealed class MatchEnded : ServerMessage
    {
        /// <summary>null = no winner (should not happen in the MVP, but keep it safe).</summary>
        public PlayerSeat? Winner { get; set; }
        public MatchEndReason Reason { get; set; }
    }

    /// <summary>Problem outside a move: unknown room, room full, bad nickname, bad JSON…</summary>
    public sealed class Error : ServerMessage
    {
        public ErrorCode Code { get; set; }
        public string Message { get; set; }
    }
}
