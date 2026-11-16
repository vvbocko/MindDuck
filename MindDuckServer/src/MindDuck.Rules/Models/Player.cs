namespace MindDuck.Rules.Models
{
    public class Player
    {
        public int Id { get; }

        public string Name { get; set; }

        public int Life { get; set; } = 20;

        public int MindDucksLeft { get; set; }

        public Hand Hand { get; } = new();

        public Board Board { get; } = new();

        public DrawPile DrawPile { get; } = new();

        public DiscardPile DiscardPile { get; } = new();

        public Player(int id, string name, IEnumerable<Card>? startingDeck = null)
        {
            Id = id;
            Name = name;

            if (startingDeck == null) return;

            foreach (Card card in startingDeck)
            {
                DrawPile.Add(card);
            }
        }

        public int DrawCards(int amount)
        {
            int drawnCards = 0;

            for (int i = 0; i < amount; i++)
            {
                Card? card = DrawPile.DrawTop();

                if (card == null) break;

                Hand.Add(card);
                drawnCards++;
            }

            return drawnCards;
        }
    }
}