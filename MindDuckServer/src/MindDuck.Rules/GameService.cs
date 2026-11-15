using MindDuck.Rules.Commands;
using MindDuck.Rules.Interfaces;
using MindDuck.Rules.Models;
using MindDuck.Rules.Results;

public class GameService : IGameService
{
    private readonly MatchManager _matchManager;
    private readonly IGameEngine _gameEngine;

    public GameService(MatchManager matchManager, IGameEngine gameEngine)
    {
        _matchManager = matchManager;
        _gameEngine = gameEngine;
    }

    public Match StartMatch(Player player1, Player player2)
    {
        var match = new Match
        {
            MatchId = Guid.NewGuid(),
            GameState = _gameEngine.StartNewGame(player1, player2)
        };
        return _matchManager.AddMatch(match);
    }


    public ApplyResult ExecuteCommand(Guid matchId, Player player, GameCommand command)
    {
        var match = _matchManager.GetMatch(matchId);

        return _gameEngine.Apply(match.GameState, player,command);
    }

}