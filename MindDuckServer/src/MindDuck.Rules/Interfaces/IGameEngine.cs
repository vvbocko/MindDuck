using MindDuck.Rules.Commands;
using MindDuck.Rules.Models;
using MindDuck.Rules.Results;

namespace MindDuck.Rules.Interfaces;
public interface IGameEngine {
    // starts a new game for players and returns the initial game state
    GameState StartNewGame(GameSetup setup);

    // executes the player's command
    ApplyResult Apply(GameState state, Player player, GameCommand command);

    // returns all legal actions for the player
    IReadOnlyList<GameCommand> GetLegalActions(GameState state, Player player);
}