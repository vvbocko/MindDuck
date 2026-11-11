using MindDuck.Rules.Commands;
using MindDuck.Rules.Models;
using MindDuck.Rules.Results;

namespace MindDuck.Rules.Interfaces
{
    public interface IGameService
    {
        void ExecuteCommand(GameState state, GameCommand command);
    }
}