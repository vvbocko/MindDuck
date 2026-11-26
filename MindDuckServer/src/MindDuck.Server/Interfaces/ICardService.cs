using MindDuck.Rules.Models;
using System.Threading;
using System.Threading.Tasks;

namespace MindDuck.Rules.Interfaces;

public interface ICardService
{
    Task PrepareDecksAsync(Player playerOne, Player playerTwo, CancellationToken cancellationToken = default);
}