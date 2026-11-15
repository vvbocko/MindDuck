using System.Threading;
using System.Threading.Tasks;

namespace MindDuck.Persistence;

public interface IPlayerRepository
{
    Task<Player> GetPlayerAsync(int id, CancellationToken cancellationToken = default);
    Task SavePlayerAsync(Player player, CancellationToken cancellationToken = default);
    Task UpdateStatsAsync(int playerId, bool win, CancellationToken cancellationToken = default);
}