using System.Collections.Generic;

namespace MindDuck.Contracts
{
    /// <summary>
    /// The game as ONE player is allowed to see it. The server builds a separate one for each player
    /// (GameViewMapper), so the opponent's hand is never sent — only its count.
    /// </summary>
    public sealed class GameStateView
    {
        public string MatchId { get; set; }
        public PlayerSeat YourSeat { get; set; }
        /// <summary>Whose turn it is.</summary>
        public PlayerSeat ActiveSeat { get; set; }
        public GamePhase Phase { get; set; }
        /// <summary>Who the game is waiting for right now (can be the non-active player during a decision). null when GameOver.</summary>
        public PlayerSeat? WaitingForSeat { get; set; }
        public int TurnNumber { get; set; }

        public PlayerView You { get; set; }
        public OpponentView Opponent { get; set; }

        /// <summary>What YOU may do right now. Only for highlighting — the server re-checks every command.</summary>
        public LegalActionsDto LegalActions { get; set; } = new LegalActionsDto();
    }

    /// <summary>Your own side: you see your hand.</summary>
    public sealed class PlayerView
    {
        public string Nickname { get; set; }
        public int Life { get; set; }
        public int MindbugsLeft { get; set; }
        public List<CardRefDto> Hand { get; set; } = new List<CardRefDto>();
        public int DrawPileCount { get; set; }
        public List<CardRefDto> Discard { get; set; } = new List<CardRefDto>();
        public List<CreatureDto> Board { get; set; } = new List<CreatureDto>();
    }

    /// <summary>The opponent's side: same as PlayerView, but the hand is only a number.</summary>
    public sealed class OpponentView
    {
        public string Nickname { get; set; }
        public int Life { get; set; }
        public int MindbugsLeft { get; set; }
        public int HandCount { get; set; }
        public int DrawPileCount { get; set; }
        public List<CardRefDto> Discard { get; set; } = new List<CardRefDto>();
        public List<CreatureDto> Board { get; set; } = new List<CreatureDto>();
    }

    /// <summary>One physical card. InstanceId is unique in the match (1–40); DefinitionId says which card it is ("ember-wolf").</summary>
    public sealed class CardRefDto
    {
        public int InstanceId { get; set; }
        public string DefinitionId { get; set; }
    }

    /// <summary>A card lying on the board as a creature.</summary>
    public sealed class CreatureDto
    {
        public int InstanceId { get; set; }
        public string DefinitionId { get; set; }
        /// <summary>Tough creature that already survived once.</summary>
        public bool IsExhausted { get; set; }
    }

    public sealed class LegalActionsDto
    {
        public List<int> PlayableCardIds { get; set; } = new List<int>();
        public List<int> AttackerIds { get; set; } = new List<int>();
    }
}
