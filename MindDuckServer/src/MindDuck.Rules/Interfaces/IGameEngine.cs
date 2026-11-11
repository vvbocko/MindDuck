using System.Collections.Generic;
using MindDuck.Rules.Commands;
using MindDuck.Rules.Models;
using MindDuck.Rules.Results;

namespace MindDuck.Rules.Interfaces
{
    public interface IGameEngine {
        // rozpoczyna nową grę dla graczy player1 i player2 i zwraca początkowy stan gry
        GameState StartNewGame(Player player1, Player player2);

        // wykonuje polecenie gracza
        ApplyResult Apply(GameState state, Player player, GameCommand command);

        // zwraca wszystkie legalne akcje gracza
        IReadOnlyList<GameCommand> GetLegalActions(Player player);
    }
}