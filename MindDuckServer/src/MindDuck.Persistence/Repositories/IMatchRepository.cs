using System.Threading;
using System.Threading.Tasks;

namespace MindDuck.Persistence;

public interface IMatchRepository
{
    // tworzy nowy wpis gry w bazie
    Task CreateMatchAsync(string matchId, CancellationToken cancellationToken = default);

    // dopisuje ruchy z przebiegu gry
    Task AppendEventsAsync(string matchId, List<string> events, CancellationToken cancellationToken = default);

    // zapisuje wynik/zwycięzcę gry
    Task FinishMatchAsync(string matchId, string winner, CancellationToken cancellationToken = default);

    // pobiera ostatnie mecze
    Task<List<MatchResult>> GetRecentAsync(int limit, CancellationToken cancellationToken = default);
}