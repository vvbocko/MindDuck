public interface ICardRepository
{
    // pobiera karte od danym id
    Card GetCard(string id);
    // pobiera wszystkie karty z bazy
    List<Card> GetAllCards();
    // pobiera umiejetnosci karty o dany id
    List<CardAbility> GetCardAbilities(string cardId);
}