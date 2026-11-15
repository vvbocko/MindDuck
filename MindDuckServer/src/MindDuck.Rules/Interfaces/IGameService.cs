using MindDuck.Rules.Commands;
using MindDuck.Rules.Models;
using MindDuck.Rules.Results;

namespace MindDuck.Rules.Interfaces
{
    public interface IGameService
    {
        Match StartMatch(Player player1, Player player2);
        ApplyResult ExecuteCommand(Guid matchId, Player player, GameCommand command);
    }
}