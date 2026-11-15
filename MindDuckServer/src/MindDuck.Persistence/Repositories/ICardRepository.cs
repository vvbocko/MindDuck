using System.Threading;
using System.Threading.Tasks;

namespace MindDuck.Persistence;

public interface ICardRepository
{
    // pobiera karte od danym id
    Task<Card> GetCardAsync(string id, CancellationToken cancellationToken = default);
    // pobiera wszystkie karty z bazy
    Task<List<Card>> GetAllCardsAsync(CancellationToken cancellationToken = default);
    // pobiera umiejetnosci karty o dany id
    Task<List<CardAbility>> GetCardAbilitiesAsync(string cardId, CancellationToken cancellationToken = default);
}