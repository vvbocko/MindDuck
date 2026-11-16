namespace MindDuck.Rules.Models;

public class CardZone
{
    private readonly List<Card> _cards = new();

    public IReadOnlyList<Card> Cards => _cards;

    public int Count => _cards.Count;

    public void Add(Card card)
    {
        if (card == null) 
            throw new ArgumentNullException(nameof(card));

        if (_cards.Any(c => c.InstanceId == card.InstanceId))
            throw new InvalidOperationException("Card already exists in this zone.");

        _cards.Add(card);
    }

    public bool Remove(Card card)
    {
        return _cards.Remove(card);
    }

    public Card? FindByInstanceId(int instanceId)
    {
        return _cards.FirstOrDefault(card => card.InstanceId == instanceId);
    }

    public Card? DrawTop()
    {
        if (_cards.Count == 0) return null;

        Card card = _cards[0];

        _cards.RemoveAt(0);

        return card;
    }

    public void Shuffle(Random? random = null)
    {
        random ??= Random.Shared;

        for (int i = _cards.Count - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);

            (_cards[i], _cards[j]) = (_cards[j], _cards[i]);
        }
    }
}