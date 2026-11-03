// Every enum is sent over the network as a STRING ("P1", "UseMindbug"), never as a number.
// So: renaming a value = protocol change. Adding a value at the end is safe.

namespace MindDuck.Contracts
{
    /// <summary>Which of the two players. P1 = first to join the room.</summary>
    public enum PlayerSeat
    {
        P1,
        P2
    }

    /// <summary>What the match is waiting for right now (see GameStateView.WaitingForSeat).</summary>
    public enum GamePhase
    {
        /// <summary>Active player must play a card or attack.</summary>
        AwaitingAction,
        /// <summary>Some player must answer a DecisionRequired (Mindbug, block, target, Frenzy).</summary>
        AwaitingDecision,
        /// <summary>Match is over, a MatchEnded message was sent.</summary>
        GameOver
    }

    public enum DecisionKind
    {
        UseMindbug,
        Block,
        ChooseTarget,
        FrenzyAttackAgain
    }

    public enum MatchEndReason
    {
        LifeZero,
        CannotAct,
        OpponentDisconnected,
        OpponentLeft
    }

    public enum RoomStatus
    {
        WaitingForPlayers,
        InProgress,
        Finished
    }

    public enum Keyword
    {
        Poisonous,
        Tough,
        Frenzy
    }

    public enum AbilityTrigger
    {
        Play,
        Attack,
        Defeated
    }

    public enum AbilityEffect
    {
        GainLife,
        DefeatEnemyCreature,
        OpponentDiscards
    }

    /// <summary>What just happened — the client uses these for animations and the log line.</summary>
    public enum GameEventType
    {
        TurnStarted,
        CardPlayed,
        CardsDrawn,
        CardMindbugged,
        CreatureEntered,
        AbilityTriggered,
        AttackDeclared,
        CreatureBlocked,
        AttackUnblocked,
        CreatureExhausted,
        CreatureDefeated,
        CardsDiscarded,
        LifeChanged,
        MatchEnded
    }

    /// <summary>
    /// Why something was refused. Sent in ActionRejected (a move broke a rule), Error (a problem outside a move)
    /// and ErrorResponse (REST). Unity can react to each code, Message is only for logs / the UI text.
    /// </summary>
    public enum ErrorCode
    {
        // ---- sent in Error (WebSocket) or ErrorResponse (REST) ----

        /// <summary>No room with this code.</summary>
        RoomNotFound,

        /// <summary>The room already has two players.</summary>
        RoomFull,

        /// <summary>Nickname empty or longer than Protocol.MaxNicknameLength.</summary>
        InvalidNickname,

        /// <summary>Rejoin with a player token that is unknown or expired.</summary>
        InvalidToken,

        /// <summary>A game message was sent before JoinRoom / Rejoin.</summary>
        NotInRoom,

        /// <summary>The "type" in the envelope is not a known message.</summary>
        UnknownMessage,

        /// <summary>The text could not be read as JSON.</summary>
        BadJson,

        // ---- sent in ActionRejected ----

        /// <summary>It is not this player's turn (or not their decision).</summary>
        NotYourTurn,

        /// <summary>The move breaks the rules (card not in hand, creature can't attack...).</summary>
        IllegalMove,

        /// <summary>DecisionId or OptionId does not match the open decision.</summary>
        InvalidDecision
    }
}
