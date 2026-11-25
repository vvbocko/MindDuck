using System;
using MindDuck.Rules.Commands;
using MindDuck.Rules.Interfaces;
using MindDuck.Rules.Models;
using MindDuck.Rules.Results;
using System.Threading;
using System.Threading.Tasks;

namespace MindDuck.Rules;

public sealed class GameService(MatchManager matchManager, IGameEngine gameEngine) : IGameService
{
    public Task<Match> StartMatchAsync(Player playerOne, Player playerTwo, CancellationToken cancellationToken = default)
    {
        var match = new Match
        {
            MatchId = Guid.NewGuid(),
            GameState = gameEngine.StartNewGame(new GameSetup(playerOne, playerTwo))
        };

        return Task.FromResult(matchManager.AddMatch(match));
    }

    public ApplyResult ExecuteCommand(Guid matchId, Player player, GameCommand command)
    {
        var match = matchManager.GetMatch(matchId);

        return gameEngine.Apply(match.GameState, player, command);
    }
}