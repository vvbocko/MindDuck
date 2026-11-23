using MindDuck.Contracts;
namespace MindDuck.Persistence;

public class Card
{
    public string CardId { get; set; }

    public string Name { get; set; }

    public int Power { get; set; }

    public Keyword? Keyword { get; set; }

    public ICollection<CardAbility> Abilities { get; set; } = new List<CardAbility>();
    public string ArtKey { get; set; }
}

public class CardAbility
{
    public int Id { get; set; }

    public string CardId { get; set; }

    public AbilityTrigger Trigger { get; set; }

    public AbilityEffect Effect { get; set; }

    public int? Value { get; set; }

    public Card Card { get; set; }
}

public class MatchResult
{
    public string MatchId { get; set; }
    public int? Player1Id { get; set; }
    public int? Player2Id { get; set; }
    public string? Winner { get; set; }
}

public class Player
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int Wins { get; set; }
    public int Losses { get; set; }
}