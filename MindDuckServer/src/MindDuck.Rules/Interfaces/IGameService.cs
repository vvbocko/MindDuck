using System;
using MindDuck.Rules.Commands;
using MindDuck.Rules.Models;
using MindDuck.Rules.Results;
using System.Threading;
using System.Threading.Tasks;

namespace MindDuck.Rules.Interfaces;

public interface IGameService
{
    Task<Match> StartMatchAsync(Player playerOne, Player playerTwo, CancellationToken cancellationToken = default);

    ApplyResult ExecuteCommand(Guid matchId, Player player, GameCommand command);
}