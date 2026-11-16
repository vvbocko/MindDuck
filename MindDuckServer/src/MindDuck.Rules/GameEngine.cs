using System;
using System.Collections.Generic;
using MindDuck.Rules.Commands;
using MindDuck.Rules.Interfaces;
using MindDuck.Rules.Models;
using MindDuck.Rules.Results;

namespace MindDuck.Rules;
public sealed class GameEngine : IGameEngine 
{
    public GameState StartNewGame(GameSetup setup)
    {
        ArgumentNullException.ThrowIfNull(setup);

        Player playerOne = setup.PlayerOne;
        Player playerTwo = setup.PlayerTwo;

        if (playerOne.Id == playerTwo.Id) 
            throw new ArgumentException("Players must have different IDs.");

        if (setup.StartingLife <= 0)
            throw new ArgumentException("Starting life must be greater than zero.");

        if (setup.StartingHandSize < 0) 
            throw new ArgumentException("Starting hand size cannot be negative.");

        if (playerOne.DrawPile.Count < setup.StartingHandSize ||
            playerTwo.DrawPile.Count < setup.StartingHandSize)
                throw new InvalidOperationException("Not enough cards in a player's deck.");

        playerOne.Life = setup.StartingLife;
        playerTwo.Life = setup.StartingLife;

        playerOne.MindDucksLeft = 2;
        playerTwo.MindDucksLeft = 2;

        playerOne.DrawPile.Shuffle();
        playerTwo.DrawPile.Shuffle();

        playerOne.DrawCards(setup.StartingHandSize);
        playerTwo.DrawCards(setup.StartingHandSize);

        return new GameState(playerOne, playerTwo);
    }

    public ApplyResult Apply(GameState state, Player player, GameCommand command)
    {
        // TODO: Implement command execution.

        return ApplyResult.Failure(ApplyErrorCode.InvalidCommand, "Game engine is not fully implemented yet.");
    }

    public IReadOnlyList<GameCommand> GetLegalActions(GameState state, Player player)
    {
        if (state.Phase != GamePhase.Main || state.ActivePlayerId != player.Id) 
            return Array.Empty<GameCommand>();

        List<GameCommand> actions = new();

        // Playing cards
        foreach (Card card in player.Hand.Cards)
        {
            actions.Add(new PlayCardCommand(card.InstanceId));
        }

        // Attacking with creatures 
        foreach (Card card in player.Board.Cards)
        {
            if (card.CanAttack(state.Turn))
            {
                actions.Add(new AttackCommand(card.InstanceId));
            }
        }

        return actions;
    }
}