using System;
using System.Collections.Generic;
using MindDuck.Rules.Commands;
using MindDuck.Rules.Interfaces;
using MindDuck.Rules.Models;
using MindDuck.Rules.Results;

namespace MindDuck.Rules
{
    public class GameEngine : IGameEngine {
        public GameState StartNewGame(Player player1, Player player2)
        {
            // TODO: impl. stworzenie nowego stanu gry i przypisanie graczy
            GameState initState = new GameState();
            
            initState.ActivePlayerName = player1.Name;
            initState.Players = new List<Player> { player1, player2 };

            return initState;
        }

        public ApplyResult Apply(Player player, GameCommand command)
        {
            // TODO: impl. wykonywania akcji

            return ApplyResult.Failure(ApplyErrorCode.InvalidCommand, "Nieskonczony game engine");
        }

        public IReadOnlyList<GameCommand> GetLegalActions(Player player)
        {
            // TODO: impl. sprawdzania legalnych akcji
            
            return new List<GameCommand>();
        }
    }
}