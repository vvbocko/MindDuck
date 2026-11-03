public class Card
{
    public int CardId { get; set; }
    public string Name { get; set; }
    public int Power { get; set; }
    public string Ability { get; set; }
}

public class MatchResult
{
    public string MatchId { get; set; }
    public string Winner { get; set; }
}

public class Player
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int Wins { get; set; }
    public int Losses { get; set; }
}