public interface ICardRepository
{
    // pobiera karte od danym id
    Card GetCard(int id);
    // pobiera wszystkie karty z bazy
    List<Card> GetAllCards();
    // pobiera umiejetnosci karty o dany id
    List<int> GetCardAbilities(int cardId);
}