using System;
using System.Collections.Generic;
using System.Linq;

namespace MindDuck.Rules.Models;
public class GameState
{
    public int Turn { get; internal set; } = 1;

    public GamePhase Phase { get; internal set; } = GamePhase.Main;

    public int? ActivePlayerId { get; internal set; }

    public int? WinnerPlayerId { get; internal set; }

    public bool IsDraw { get; internal set; }
        
    public IReadOnlyList<Player> Players { get; internal set; }

    public Player ActivePlayer => Players.First(p => p.Id == ActivePlayerId);

    public PendingDecision? PendingDecision { get; set; }

    public int? FrenzyContinuationCardInstanceId { get; set; }

    public GameState(Player playerOne, Player playerTwo)
    {
        if (playerOne == null || playerTwo == null) 
            throw new ArgumentNullException("Players cannot be null.");

        if (playerOne.Id == playerTwo.Id) 
            throw new ArgumentException("Players must have different IDs.");

        Players = new List<Player> { playerOne, playerTwo };
        ActivePlayerId = playerOne.Id;
    }

    public Player GetOpponent(Player player)
    {
        return Players.First(p => p.Id != player.Id);
    }

    public bool ContainsPlayer(Player player)
    {
        return Players.Any(p => p.Id == player.Id);
    }

    public void SetActivePlayer(Player player)
    {
        if (!ContainsPlayer(player)) 
            throw new InvalidOperationException("Player does not belong to this game.");
        
        ActivePlayerId = player.Id;
    }

    public void AdvanceTurn()
    {
        Turn++;
    }

    public void Finish(Player? winner)
    {
        Phase = GamePhase.Finished;
        WinnerPlayerId = winner?.Id;
        IsDraw = winner is null;
        PendingDecision = null;
    }
}
